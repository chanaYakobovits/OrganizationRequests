using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class RequestStatusHistory
    {
        public long Id { get; set; }
        public int RequestId { get; set; }
        public RequestStatus PreviousStatus { get; set; }
        public RequestStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = null!;
        public Request Request { get; set; } = null!;
    }
}
