using System.ComponentModel.DataAnnotations;
using Common.Models;

namespace Common.DTOs;

public class BulkStatusItem
{
    public int Id { get; set; }
    public string Version { get; set; } = null!;

    public bool TryGetRowVersion(out byte[] rowVersion)
        => RowVersionParser.TryParse(Version, out rowVersion);
}

public class BulkUpdateStatusRequest : IValidatableObject
{
    public const int MaxItems = 100;

    [EnumDataType(typeof(RequestStatus))]
    public RequestStatus NewStatus { get; set; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string ChangedBy { get; set; } = null!;

    [Required, MinLength(1), MaxLength(MaxItems)]
    public List<BulkStatusItem> Items { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var seen = new HashSet<int>();

        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var path = $"{nameof(Items)}[{i}]";

            if (item.Id <= 0)
                yield return new ValidationResult("Id must be positive.", new[] { path });

            if (!item.TryGetRowVersion(out _))
                yield return new ValidationResult("Version is not a valid row version.", new[] { path });

            if (!seen.Add(item.Id))
                yield return new ValidationResult($"Duplicate id {item.Id}.", new[] { path });
        }
    }
}