using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DTOs;
using Common.Models;

namespace Common
{
    public static class RequestMapper
    {
        public static RequestDto ToDto(this Request r) => new(
            r.Id, r.Title, r.OrganizationName, r.Status, r.Priority,
            r.AssignedTo, r.CreatedAt, r.UpdatedAt,
            Convert.ToBase64String(r.RowVersion));
    }
}
