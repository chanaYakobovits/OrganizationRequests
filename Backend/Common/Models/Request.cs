using Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class Request
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string OrganizationName { get; set; } = null!;
        public RequestStatus Status { get; set; }
        public RequestPriority Priority { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = null!;

        
    }
}
