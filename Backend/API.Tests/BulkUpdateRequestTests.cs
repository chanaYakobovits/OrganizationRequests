using System.ComponentModel.DataAnnotations;
using Common.DTOs;
using Common.Models;

namespace API.Tests;

public class BulkUpdateRequestTests
{
    private static string V() => Convert.ToBase64String(new byte[8]);

    private static List<ValidationResult> Validate(BulkUpdateStatusRequest r)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(r, new ValidationContext(r), results, validateAllProperties: true);
        return results;
    }

    private static BulkUpdateStatusRequest Make(params BulkStatusItem[] items) => new()
    {
        NewStatus = RequestStatus.InProgress,
        ChangedBy = "בדיקה",
        Items = items.ToList()
    };

    [Fact]
    public void ValidRequest_HasNoErrors()
        => Assert.Empty(Validate(Make(
            new BulkStatusItem { Id = 1, Version = V() },
            new BulkStatusItem { Id = 2, Version = V() })));

    [Fact]
    public void EmptyItems_ReturnsError()
        => Assert.NotEmpty(Validate(Make()));

    [Fact]
    public void MoreThan100Items_ReturnsError()
    {
        var items = Enumerable.Range(1, 101)
            .Select(i => new BulkStatusItem { Id = i, Version = V() }).ToArray();

        Assert.NotEmpty(Validate(Make(items)));
    }

    [Fact]
    public void Exactly100Items_IsAllowed()
    {
        var items = Enumerable.Range(1, 100)
            .Select(i => new BulkStatusItem { Id = i, Version = V() }).ToArray();

        Assert.Empty(Validate(Make(items)));
    }

    [Fact]
    public void DuplicateIds_ReturnsError()
        => Assert.NotEmpty(Validate(Make(
            new BulkStatusItem { Id = 5, Version = V() },
            new BulkStatusItem { Id = 5, Version = V() })));

    [Fact]
    public void InvalidVersionInItem_ReturnsError()
        => Assert.NotEmpty(Validate(Make(
            new BulkStatusItem { Id = 1, Version = "garbage!!" })));
}