using System.ComponentModel.DataAnnotations;
using Common.DTOs;
using Common.Models;

namespace API.Tests;

public class UpdateStatusRequestTests
{
    private static List<ValidationResult> Validate(UpdateStatusRequest r)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(r, new ValidationContext(r), results, validateAllProperties: true);
        return results;
    }

    private static string ValidVersion() => Convert.ToBase64String(new byte[8]);

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var r = new UpdateStatusRequest
        {
            NewStatus = RequestStatus.InProgress,
            Version = ValidVersion(),
            ChangedBy = "מטפל 01"
        };

        Assert.Empty(Validate(r));
    }

    [Theory]
    [InlineData("not-base64!!")]
    [InlineData("AAAA")]   
    public void InvalidVersion_ReturnsError(string version)
    {
        var r = new UpdateStatusRequest
        {
            NewStatus = RequestStatus.InProgress,
            Version = version,
            ChangedBy = "x"
        };

        Assert.NotEmpty(Validate(r));
    }

    [Fact]
    public void UndefinedStatus_ReturnsError()
    {
        var r = new UpdateStatusRequest
        {
            NewStatus = (RequestStatus)99,
            Version = ValidVersion(),
            ChangedBy = "x"
        };

        Assert.NotEmpty(Validate(r));
    }
}