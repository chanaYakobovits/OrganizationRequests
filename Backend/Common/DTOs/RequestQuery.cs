using System.ComponentModel.DataAnnotations;
using Common.Models;

namespace Common.DTOs;

public class RequestQuery : IValidatableObject
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 20;

    [EnumDataType(typeof(RequestStatus))]
    public RequestStatus? Status { get; set; }

    [EnumDataType(typeof(RequestPriority))]
    public RequestPriority? Priority { get; set; }

    [StringLength(200)]
    public string? OrganizationName { get; set; }

    [StringLength(100)]
    public string? AssignedTo { get; set; }

    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    [EnumDataType(typeof(RequestSortField))]
    public RequestSortField SortBy { get; set; } = RequestSortField.CreatedAt;

    public SortDirection SortDir { get; set; } = SortDirection.Desc;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CreatedFrom.HasValue && CreatedTo.HasValue && CreatedFrom > CreatedTo)
        {
            yield return new ValidationResult(
                "CreatedFrom must not be later than CreatedTo.",
                new[] { nameof(CreatedFrom), nameof(CreatedTo) });
        }
    }
}