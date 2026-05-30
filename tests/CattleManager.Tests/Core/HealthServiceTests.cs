using CattleManager.Core.Models;
using CattleManager.Core.Services;
using FluentAssertions;
using Xunit;

namespace CattleManager.Tests.Core;

public class HealthServiceTests
{
    private readonly HealthService _sut = new();

    private static AnimalDto MakeAnimal(
        DateTime? lastWorming = null,
        DateTime? lastVaccination = null,
        DateTime? lastHealthCheck = null,
        bool isPregnant = false,
        DateTime? dueDate = null) => new()
    {
        BarnName = "Test",
        Gender = Gender.Female,
        BirthDate = DateTime.Today.AddYears(-2),
        LastWormingDate = lastWorming,
        LastVaccinationDate = lastVaccination,
        LastHealthCheckDate = lastHealthCheck,
        IsPregnant = isPregnant,
        ExpectedDueDate = dueDate
    };

    [Fact]
    public void IsOverdueForWorming_NullDate_ReturnsTrue()
        => _sut.IsOverdueForWorming(MakeAnimal()).Should().BeTrue();

    [Fact]
    public void IsOverdueForWorming_RecentDate_ReturnsFalse()
        => _sut.IsOverdueForWorming(MakeAnimal(lastWorming: DateTime.Today.AddWeeks(-4)))
            .Should().BeFalse();

    [Fact]
    public void IsOverdueForWorming_OldDate_ReturnsTrue()
        => _sut.IsOverdueForWorming(MakeAnimal(lastWorming: DateTime.Today.AddWeeks(-12)))
            .Should().BeTrue();

    [Fact]
    public void IsOverdueForVaccination_NullDate_ReturnsTrue()
        => _sut.IsOverdueForVaccination(MakeAnimal()).Should().BeTrue();

    [Fact]
    public void IsOverdueForVaccination_WithinYear_ReturnsFalse()
        => _sut.IsOverdueForVaccination(MakeAnimal(lastVaccination: DateTime.Today.AddDays(-180)))
            .Should().BeFalse();

    [Fact]
    public void IsOverdueForVaccination_OverYear_ReturnsTrue()
        => _sut.IsOverdueForVaccination(MakeAnimal(lastVaccination: DateTime.Today.AddDays(-400)))
            .Should().BeTrue();

    [Fact]
    public void IsOverdueForHusbandry_EitherOverdue_ReturnsTrue()
    {
        // Worming overdue, vaccination recent
        var a = MakeAnimal(lastWorming: DateTime.Today.AddDays(-100), lastVaccination: DateTime.Today.AddDays(-30));
        _sut.IsOverdueForHusbandry(a).Should().BeTrue();
    }

    [Fact]
    public void IsOverdueForHusbandry_BothCurrent_ReturnsFalse()
    {
        var a = MakeAnimal(lastWorming: DateTime.Today.AddDays(-20), lastVaccination: DateTime.Today.AddDays(-30));
        _sut.IsOverdueForHusbandry(a).Should().BeFalse();
    }

    [Fact]
    public void GetUpcomingTasks_PregnantAnimal_IncludesDueDate()
    {
        var dueDate = DateTime.Today.AddDays(30);
        var a = MakeAnimal(lastWorming: DateTime.Today.AddDays(-10), lastVaccination: DateTime.Today.AddDays(-10),
            isPregnant: true, dueDate: dueDate);
        var tasks = _sut.GetUpcomingTasks(a);
        tasks.Should().Contain(t => t.Contains("30 day"));
    }

    [Fact]
    public void GetUpcomingTasks_OverdueCalving_IncludesOverdueMessage()
    {
        var pastDue = DateTime.Today.AddDays(-5);
        var a = MakeAnimal(lastWorming: DateTime.Today.AddDays(-10), lastVaccination: DateTime.Today.AddDays(-10),
            isPregnant: true, dueDate: pastDue);
        var tasks = _sut.GetUpcomingTasks(a);
        tasks.Should().Contain(t => t.Contains("overdue"));
    }

    [Fact]
    public void GetUpcomingTasks_NoIssues_ReturnsEmpty()
    {
        var a = MakeAnimal(lastWorming: DateTime.Today.AddDays(-10), lastVaccination: DateTime.Today.AddDays(-10));
        _sut.GetUpcomingTasks(a).Should().BeEmpty();
    }

    [Fact]
    public void GetUpcomingTasks_WormingNeverDone_IncludesNeverMessage()
    {
        var a = MakeAnimal(lastVaccination: DateTime.Today.AddDays(-10));
        var tasks = _sut.GetUpcomingTasks(a);
        tasks.Should().Contain(t => t.Contains("Never"));
    }
}

public static class DateTimeExtensions
{
    public static DateTime AddWeeks(this DateTime date, int weeks) => date.AddDays(weeks * 7);
}
