using CattleManager.Core.Models;
using FluentAssertions;
using Xunit;

namespace CattleManager.Tests.Core;

public class AnimalDtoTests
{
    [Theory]
    [InlineData(0, 6, "6mo")]
    [InlineData(1, 0, "1yr")]
    [InlineData(2, 3, "2yr 3mo")]
    [InlineData(0, 1, "1mo")]
    public void AgeDisplay_CalculatesCorrectly(int years, int months, string expected)
    {
        var dto = new AnimalDto
        {
            BirthDate = DateTime.Today.AddYears(-years).AddMonths(-months)
        };
        dto.AgeDisplay.Should().Be(expected);
    }

    [Fact]
    public void WeightDisplay_Pounds_FormatsCorrectly()
    {
        var dto = new AnimalDto { Weight = 1200, WeightUnit = WeightUnit.Pounds };
        dto.WeightDisplay.Should().Be("1200 lbs");
    }

    [Fact]
    public void WeightDisplay_Kilograms_FormatsCorrectly()
    {
        var dto = new AnimalDto { Weight = 544.3m, WeightUnit = WeightUnit.Kilograms };
        dto.WeightDisplay.Should().Contain("kg");
    }

    [Fact]
    public void WeightDisplay_NullWeight_ReturnsEmpty()
    {
        var dto = new AnimalDto { Weight = null };
        dto.WeightDisplay.Should().BeEmpty();
    }

    [Fact]
    public void HeightDisplay_Hands_FormatsCorrectly()
    {
        var dto = new AnimalDto { Height = 13.2m, HeightUnit = HeightUnit.Hands };
        dto.HeightDisplay.Should().Contain("hh");
    }

    [Fact]
    public void HeightDisplay_Centimeters_FormatsCorrectly()
    {
        var dto = new AnimalDto { Height = 130m, HeightUnit = HeightUnit.Centimeters };
        dto.HeightDisplay.Should().Contain("cm");
    }

    [Fact]
    public void HeightDisplay_Inches_FormatsCorrectly()
    {
        var dto = new AnimalDto { Height = 52m, HeightUnit = HeightUnit.Inches };
        dto.HeightDisplay.Should().Contain("in");
    }

    [Fact]
    public void HeightDisplay_NullHeight_ReturnsEmpty()
    {
        var dto = new AnimalDto { Height = null };
        dto.HeightDisplay.Should().BeEmpty();
    }
}
