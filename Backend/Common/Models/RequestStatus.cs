using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public enum RequestStatus : byte { New = 1, InProgress = 2, Waiting = 3, Completed = 4 }
}
