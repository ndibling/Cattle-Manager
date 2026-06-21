using CattleManager.Core.Models;
using CattleManager.Core.Services;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace CattleManager.Tests.Core;

public class ColumnConfigServiceTests
{
    private readonly Mock<IAppSettingsRepository> _settings = new();
    private ColumnConfigService CreateSut() => new(_settings.Object);

    [Fact]
    public async Task LoadAsync_ReturnsDefaults_WhenNoSettingExists()
    {
        _settings.Setup(s => s.GetAsync("HerdListColumns")).ReturnsAsync((string?)null);

        var result = await CreateSut().LoadAsync();

        // Default-ON columns
        result.ShowRegisteredName.Should().BeTrue();
        result.ShowBreed.Should().BeTrue();
        result.ShowGender.Should().BeTrue();
        result.ShowAge.Should().BeTrue();
        result.ShowBirthDate.Should().BeTrue();
        result.ShowWeight.Should().BeTrue();
        result.ShowHeight.Should().BeTrue();
        result.ShowLastWorming.Should().BeTrue();
        result.ShowStatus.Should().BeTrue();

        // Default-OFF columns
        result.ShowTagNumber.Should().BeFalse();
        result.ShowDateAcquired.Should().BeFalse();
        result.ShowPurchasePrice.Should().BeFalse();
        result.ShowCurrentValue.Should().BeFalse();
        result.ShowAskingPrice.Should().BeFalse();
        result.ShowSalePrice.Should().BeFalse();
        result.ShowSoldDate.Should().BeFalse();
        result.ShowBuyerName.Should().BeFalse();
        result.ShowPastureAddress.Should().BeFalse();
        result.ShowLastVaccination.Should().BeFalse();
        result.ShowLastHealthCheck.Should().BeFalse();
        result.ShowLastHoofTrimming.Should().BeFalse();
        result.ShowSireName.Should().BeFalse();
        result.ShowDamName.Should().BeFalse();
        result.ShowExpectedDueDate.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_DeserializesStoredConfig()
    {
        var stored = new ColumnConfig
        {
            ShowRegisteredName = false,
            ShowTagNumber      = true,
            ShowPurchasePrice  = true,
            ShowStatus         = false,
        };
        _settings.Setup(s => s.GetAsync("HerdListColumns"))
                 .ReturnsAsync(JsonSerializer.Serialize(stored));

        var result = await CreateSut().LoadAsync();

        result.ShowRegisteredName.Should().BeFalse();
        result.ShowTagNumber.Should().BeTrue();
        result.ShowPurchasePrice.Should().BeTrue();
        result.ShowStatus.Should().BeFalse();
        // Unmodified defaults remain intact
        result.ShowBreed.Should().BeTrue();
        result.ShowGender.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaults_WhenJsonIsCorrupt()
    {
        _settings.Setup(s => s.GetAsync("HerdListColumns")).ReturnsAsync("not-valid-json{{");

        var result = await CreateSut().LoadAsync();

        result.ShowRegisteredName.Should().BeTrue();
        result.ShowTagNumber.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_WritesSerializedJsonToCorrectKey()
    {
        string? capturedKey   = null;
        string? capturedValue = null;
        _settings.Setup(s => s.SetAsync(It.IsAny<string>(), It.IsAny<string>()))
                 .Callback<string, string>((k, v) => { capturedKey = k; capturedValue = v; })
                 .Returns(Task.CompletedTask);

        await CreateSut().SaveAsync(new ColumnConfig { ShowTagNumber = true, ShowRegisteredName = false });

        capturedKey.Should().Be("HerdListColumns");
        capturedValue.Should().NotBeNullOrEmpty();
        var deserialized = JsonSerializer.Deserialize<ColumnConfig>(capturedValue!);
        deserialized!.ShowTagNumber.Should().BeTrue();
        deserialized.ShowRegisteredName.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTrips_AllFields()
    {
        var original = new ColumnConfig
        {
            ShowRegisteredName   = false,
            ShowBreed            = false,
            ShowGender           = false,
            ShowAge              = false,
            ShowBirthDate        = false,
            ShowWeight           = false,
            ShowHeight           = false,
            ShowLastWorming      = false,
            ShowStatus           = false,
            ShowTagNumber        = true,
            ShowDateAcquired     = true,
            ShowPurchasePrice    = true,
            ShowCurrentValue     = true,
            ShowAskingPrice      = true,
            ShowSalePrice        = true,
            ShowSoldDate         = true,
            ShowBuyerName        = true,
            ShowPastureAddress   = true,
            ShowLastVaccination  = true,
            ShowLastHealthCheck  = true,
            ShowLastHoofTrimming = true,
            ShowSireName         = true,
            ShowDamName          = true,
            ShowExpectedDueDate  = true,
        };

        string? stored = null;
        _settings.Setup(s => s.SetAsync("HerdListColumns", It.IsAny<string>()))
                 .Callback<string, string>((_, v) => stored = v)
                 .Returns(Task.CompletedTask);
        _settings.Setup(s => s.GetAsync("HerdListColumns"))
                 .ReturnsAsync(() => stored);

        var sut = CreateSut();
        await sut.SaveAsync(original);
        var loaded = await sut.LoadAsync();

        loaded.ShowRegisteredName.Should().BeFalse();
        loaded.ShowTagNumber.Should().BeTrue();
        loaded.ShowDateAcquired.Should().BeTrue();
        loaded.ShowPurchasePrice.Should().BeTrue();
        loaded.ShowCurrentValue.Should().BeTrue();
        loaded.ShowAskingPrice.Should().BeTrue();
        loaded.ShowSalePrice.Should().BeTrue();
        loaded.ShowSoldDate.Should().BeTrue();
        loaded.ShowBuyerName.Should().BeTrue();
        loaded.ShowPastureAddress.Should().BeTrue();
        loaded.ShowLastVaccination.Should().BeTrue();
        loaded.ShowLastHealthCheck.Should().BeTrue();
        loaded.ShowLastHoofTrimming.Should().BeTrue();
        loaded.ShowSireName.Should().BeTrue();
        loaded.ShowDamName.Should().BeTrue();
        loaded.ShowExpectedDueDate.Should().BeTrue();
    }
}
