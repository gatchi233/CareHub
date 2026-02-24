using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedReminder.Models;

public enum StaffRole
{
    Admin,
    Nurse,
    CareStaff
}

public class Staff
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = "";
    public string Password { get; set; } = ""; // plaintext for M1 only
    public string FullName { get; set; } = "";
    public StaffRole Role { get; set; }
}
