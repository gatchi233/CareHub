using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.ViewModels
{
    public class FloorPlanViewModel : INotifyPropertyChanged
    {
        private readonly IResidentService _residentService;

        public ObservableCollection<RoomTile> Rooms { get; } = new();
        public ObservableCollection<ResidentPreview> SelectedResidents { get; } = new();

        private RoomTile? _selectedRoom;
        public RoomTile? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (_selectedRoom == value) return;
                _selectedRoom = value;
                OnPropertyChanged();
                LoadSelectedResidents();
                UpdateSelection();
            }
        }

        public string SelectedRoomTitle =>
            SelectedRoom == null ? "Select a room" : $"Room {SelectedRoom.RoomLabel}";

        private int _floor = 1;
        public int Floor
        {
            get => _floor;
            set
            {
                if (_floor == value) return;
                _floor = value;
                OnPropertyChanged();
                _ = LoadAsync();
            }
        }

        public FloorPlanViewModel(IResidentService residentService)
        {
            _residentService = residentService;
        }

        public async Task LoadAsync()
        {
            Rooms.Clear();
            SelectedResidents.Clear();
            SelectedRoom = null;

            var residents = await _residentService.LoadAsync();

            // 12 rooms per floor
            var roomNumbers = Floor == 1
                ? Enumerable.Range(101, 12)
                : Enumerable.Range(201, 12);

            // 7 Single, 5 Double (D positions)
            bool IsDouble(int index) => index is 3 or 4 or 6 or 8 or 9;

            var grouped = residents
                .Where(r => !string.IsNullOrWhiteSpace(r.RoomNumber))
                .GroupBy(r => r.RoomNumber!)
                .ToDictionary(g => g.Key, g => g.ToList());

            int i = 0;
            foreach (var num in roomNumbers)
            {
                grouped.TryGetValue(num.ToString(), out var list);
                Rooms.Add(new RoomTile(num.ToString(), IsDouble(i + 1), list ?? new()));
                i++;
            }

            SelectedRoom = Rooms.FirstOrDefault(r => r.IsOccupied) ?? Rooms.FirstOrDefault();
        }

        private void LoadSelectedResidents()
        {
            SelectedResidents.Clear();
            if (SelectedRoom == null) return;

            foreach (var r in SelectedRoom.Residents)
                SelectedResidents.Add(new ResidentPreview(r));
        }

        private void UpdateSelection()
        {
            foreach (var r in Rooms)
                r.IsSelected = r == SelectedRoom;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class RoomTile : INotifyPropertyChanged
    {
        public string RoomLabel { get; }
        public bool IsDouble { get; }
        public List<Resident> Residents { get; }

        public bool IsOccupied => Residents.Count > 0;
        public string KindShort => IsDouble ? "D" : "S";
        public string OccupancyText => IsOccupied
            ? string.Join(", ", Residents.Select(r => r.FullName))
            : "Empty";

        public bool IsSelected { get; set; }

        public RoomTile(string roomLabel, bool isDouble, List<Resident> residents)
        {
            RoomLabel = roomLabel;
            IsDouble = isDouble;
            Residents = residents;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class ResidentPreview
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Gender { get; }
        public string AgeText { get; }

        public ResidentPreview(Resident r)
        {
            Id = r.Id;
            Name = r.FullName;
            Gender = string.IsNullOrWhiteSpace(r.Gender) ? "Unknown" : r.Gender;
            AgeText = $"Age: {CalculateAge(r.DOB)}";
        }

        private static string CalculateAge(string dob)
        {
            if (!DateTime.TryParse(dob, out var d)) return "Unknown";
            var today = DateTime.Today;
            int age = today.Year - d.Year;
            if (d > today.AddYears(-age)) age--;
            return age < 0 ? "Unknown" : age.ToString();
        }
    }
}
