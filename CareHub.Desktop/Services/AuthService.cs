using System.Net.Http.Json;
using System.Text.Json;
using CareHub.Desktop.Services.Sync;
using CareHub.Models;

namespace CareHub.Services
{
    public class AuthService
    {
        private readonly string _apiBaseUrl;

        // Local fallback credentials (used when API is offline)
        private readonly List<Staff> _localStaff = new()
        {
            new Staff { Username = "admin",     Password = "admin123",    StaffName = "System Admin",      Role = StaffRole.Admin },
            new Staff { Username = "staff1",   Password = "staff123",   StaffName = "Staff User",        Role = StaffRole.Nurse },
            new Staff { Username = "observer1", Password = "observer123", StaffName = "Observer User",    Role = StaffRole.CareStaff }
        };

        public Staff? CurrentUser { get; private set; }
        public string? AccessToken { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;

        public AuthService(string apiBaseUrl = "http://localhost:5001/")
        {
            _apiBaseUrl = apiBaseUrl;
        }

        /// <summary>
        /// Attempts API login first. Falls back to local credentials if the API is unreachable.
        /// </summary>
        public async Task<bool> LoginAsync(string username, string password)
        {
            // Try API login first
            if (ConnectivityHelper.IsOnline())
            {
                try
                {
                    using var http = new HttpClient
                    {
                        BaseAddress = new Uri(_apiBaseUrl),
                        Timeout = TimeSpan.FromSeconds(5)
                    };

                    var payload = new { username, password };
                    var resp = await http.PostAsJsonAsync("api/auth/login", payload);

                    if (resp.IsSuccessStatusCode)
                    {
                        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

                        AccessToken = body.GetProperty("accessToken").GetString();
                        var role = body.GetProperty("role").GetString() ?? "";
                        var displayName = body.GetProperty("displayName").GetString() ?? username;

                        CurrentUser = new Staff
                        {
                            Username = username,
                            Password = "", // don't store password
                            StaffName = displayName,
                            Role = MapApiRole(role)
                        };

                        ConnectivityHelper.MarkOnline();
                        return true;
                    }

                    // 401 = bad credentials — don't fall back to local
                    if ((int)resp.StatusCode == 401)
                        return false;
                }
                catch
                {
                    // Network error — fall through to local login
                    ConnectivityHelper.MarkOffline();
                }
            }

            // Offline fallback: local credentials
            return LoginLocal(username, password);
        }

        /// <summary>
        /// Synchronous local-only login (legacy support).
        /// </summary>
        public bool Login(string username, string password)
        {
            return LoginLocal(username, password);
        }

        private bool LoginLocal(string username, string password)
        {
            var user = _localStaff.FirstOrDefault(s =>
                s.Username == username && s.Password == password);

            if (user == null)
                return false;

            CurrentUser = user;
            AccessToken = null; // no token in offline mode
            return true;
        }

        public void Logout()
        {
            CurrentUser = null;
            AccessToken = null;
        }

        public bool HasRole(params StaffRole[] roles)
        {
            if (CurrentUser == null)
                return false;

            return roles.Contains(CurrentUser.Role);
        }

        private static StaffRole MapApiRole(string apiRole)
        {
            return apiRole.ToLowerInvariant() switch
            {
                "admin" => StaffRole.Admin,
                "nurse" or "staff" => StaffRole.Nurse,
                "carestaff" or "observer" => StaffRole.CareStaff,
                _ => StaffRole.CareStaff
            };
        }
    }
}
