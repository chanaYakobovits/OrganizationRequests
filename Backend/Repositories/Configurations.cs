using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repositories
{
    public class RequestConfiguration : IEntityTypeConfiguration<Request>
    {
        public void Configure(EntityTypeBuilder<Request> b)
        {
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.OrganizationName).HasMaxLength(200).IsRequired();
            b.Property(x => x.AssignedTo).HasMaxLength(100);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => new { x.CreatedAt, x.Id })
             .IsDescending(true, false)
             .HasDatabaseName("IX_Requests_CreatedAt_Id");
        }
    }

    public class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
    {
        public void Configure(EntityTypeBuilder<RequestStatusHistory> b)
        {
            b.Property(x => x.ChangedBy).HasMaxLength(100).IsRequired();
            b.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId);
            b.HasIndex(x => new { x.RequestId, x.ChangedAt });
        }
    }
}
