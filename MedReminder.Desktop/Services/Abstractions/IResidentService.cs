using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedReminder.Models;

namespace MedReminder.Services.Abstractions
{
    public interface IResidentService
    {
        Task<List<Resident>> LoadAsync();
        Task UpsertAsync(Resident item);
        Task DeleteAsync(Resident item);
        Task ReplaceAllAsync(List<Resident> items);
    }
}
