using System.ComponentModel.DataAnnotations;
using Common.Models;

namespace Common.DTOs;

public class UpdateStatusRequest : IValidatableObject
{
    [EnumDataType(typeof(RequestStatus))]
    public RequestStatus NewStatus { get; set; }

    [Required]
    public string Version { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 1)]
    public string ChangedBy { get; set; } = null!;

    public bool TryGetRowVersion(out byte[] rowVersion)
    => RowVersionParser.TryParse(Version, out rowVersion);


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Version) && !TryGetRowVersion(out _))
        {
            yield return new ValidationResult(
                "Version is not a valid row version.",
                new[] { nameof(Version) });
        }
    }
}