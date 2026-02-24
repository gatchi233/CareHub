using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedReminder.Models;

namespace MedReminder.Services
{
    public class AuthService
    {
        private readonly List<Staff> _staff = new()
        {
            new Staff
            {
                Username = "admin",
                Password = "1234",
                FullName = "System Admin",
                Role = StaffRole.Admin
            },
            new Staff
            {
                Username = "nurse",
                Password = "1234",
                FullName = "Registered Nurse",
                Role = StaffRole.Nurse
            },
            new Staff
            {
                Username = "care",
                Password = "1234",
                FullName = "Care Staff",
                Role = StaffRole.CareStaff
            }
        };

        public Staff? CurrentUser { get; private set; }

        public bool IsLoggedIn => CurrentUser != null;

        public bool Login(string username, string password)
        {
            var user = _staff.FirstOrDefault(s =>
                s.Username == username &&
                s.Password == password);

            if (user == null)
                return false;

            CurrentUser = user;
            return true;
        }

        public void Logout()
        {
            CurrentUser = null;
        }

        public bool HasRole(params StaffRole[] roles)
        {
            if (CurrentUser == null)
                return false;

            return roles.Contains(CurrentUser.Role);
        }
    }
}