using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.Services.Abstractions
{
    public interface IStaffService
    {
        Task<List<StaffRecord>> GetAllAsync();
        Task SaveAllAsync(List<StaffRecord> staff);

        Task AddOrUpdateAsync(StaffRecord staff);
        Task SetEnabledAsync(string employeeId, bool isEnabled);
    }
}
