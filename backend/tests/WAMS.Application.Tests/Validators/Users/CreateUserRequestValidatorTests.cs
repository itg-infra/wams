namespace WAMS.Application.Tests.Validators;

using System.Collections;
using FluentAssertions;
using WAMS.Application.DTOs.Users;
using WAMS.Application.Validators.Users;
using Xunit;

/// <summary>
/// Table-driven tests for CreateUserRequestValidator using ClassData for complex object params.
/// </summary>
public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Theory]
    [ClassData(typeof(CreateUserRequestValidatorData))]
    public void Validate_AllScenarios_ReturnsExpectedValidity(
        string desc,
        string email,
        string password,
        string fullname,
        List<long>? warehouseIds,
        long? primaryWarehouseId,
        bool isValid)
    {
        var request = new CreateUserRequest(email, password, fullname, null,
            WarehouseIds: warehouseIds,
            PrimaryWarehouseId: primaryWarehouseId);

        var result = _validator.Validate(request);

        result.IsValid.Should().Be(isValid, because: desc);
    }

    // Additional focused tests
    [Fact]
    public void Validate_EmailTooLong_IsInvalid()
    {
        var longEmail = new string('a', 250) + "@b.com";
        var request = new CreateUserRequest(longEmail, "Pass1234!", "Alice", null);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_FullnameTooLong_IsInvalid()
    {
        var request = new CreateUserRequest("a@b.com", "Pass1234!", new string('x', 101), null);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}

#pragma warning disable CS8625 // null literal in object[] is fine for test data
public class CreateUserRequestValidatorData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        // desc, email, password, fullname, warehouseIds, primaryWarehouseId, isValid
        yield return new object[] { "Valid - minimal",
            "alice@example.com", "Pass1234!", "Alice",
            (List<long>?)null, (long?)null, true };

        yield return new object[] { "Valid - with warehouses",
            "alice@example.com", "Pass1234!", "Alice",
            new List<long> { 1, 2 }, (long?)1L, true };

        yield return new object[] { "Invalid - bad email format",
            "not-an-email", "Pass1234!", "Alice",
            (List<long>?)null, (long?)null, false };

        yield return new object[] { "Invalid - empty email",
            "", "Pass1234!", "Alice",
            (List<long>?)null, (long?)null, false };

        yield return new object[] { "Invalid - password too short (< 8 chars)",
            "alice@example.com", "abc", "Alice",
            (List<long>?)null, (long?)null, false };

        yield return new object[] { "Invalid - empty password",
            "alice@example.com", "", "Alice",
            (List<long>?)null, (long?)null, false };

        yield return new object[] { "Invalid - empty fullname",
            "alice@example.com", "Pass1234!", "",
            (List<long>?)null, (long?)null, false };

        yield return new object[] { "Invalid - warehouseIds empty list",
            "alice@example.com", "Pass1234!", "Alice",
            new List<long>(), (long?)null, false };

        yield return new object[] { "Invalid - duplicate warehouse ids",
            "alice@example.com", "Pass1234!", "Alice",
            new List<long> { 1, 1 }, (long?)null, false };

        yield return new object[] { "Invalid - primaryWarehouseId not in warehouseIds",
            "alice@example.com", "Pass1234!", "Alice",
            new List<long> { 1, 2 }, (long?)99L, false };

        yield return new object[] { "Valid - primaryWarehouseId matches one of warehouseIds",
            "alice@example.com", "Pass1234!", "Alice",
            new List<long> { 1, 2 }, (long?)2L, true };

        yield return new object[] { "Valid - null warehouses with primaryWarehouseId null",
            "alice@example.com", "Pass1234!", "Alice",
            (List<long>?)null, (long?)null, true };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
#pragma warning restore CS8625
