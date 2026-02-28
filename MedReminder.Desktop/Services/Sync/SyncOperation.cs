using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedReminder.Desktop.Services.Sync;

public enum SyncOperation
{
    Create = 0,
    Update = 1,
    Delete = 2
}