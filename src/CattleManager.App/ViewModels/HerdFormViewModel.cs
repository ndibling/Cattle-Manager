using CattleManager.App.Services;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CattleManager.App.ViewModels;

public partial class HerdFormViewModel : ObservableObject
{
    private readonly IHerdRepository _herds;
    private readonly IFarmRepository _farms;
    private readonly NavigationService _nav;

    public int HerdId { get; set; }
    public bool IsNewHerd => HerdId == 0;

    [ObservableProperty] private string _formTitle = "Add Herd";
    [ObservableProperty] private string _herdName = string.Empty;
    [ObservableProperty] private string _herdType = string.Empty;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string? _validationError;
    [ObservableProperty] private bool _isLoading;

    public IReadOnlyList<string> HerdTypeOptions { get; } =
    [
        "Cow-Calf", "Seedstock / Purebred", "Stocker / Backgrounder",
        "Dairy", "Show", "Feedlot", "Miniature Cattle", "Other"
    ];

    public HerdFormViewModel(IHerdRepository herds, IFarmRepository farms, NavigationService nav)
    {
        _herds = herds;
        _farms = farms;
        _nav   = nav;
    }

    public async Task LoadAsync()
    {
        if (!IsNewHerd)
        {
            FormTitle = "Edit Herd";
            IsLoading = true;
            try
            {
                var herd = await _herds.GetByIdAsync(HerdId);
                if (herd is not null)
                {
                    HerdName = herd.HerdName;
                    HerdType = herd.HerdType;
                    IsActive = herd.IsActive;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationError = Validate();
        if (ValidationError is not null) return;

        IsLoading = true;
        try
        {
            if (IsNewHerd)
            {
                var farm = await _farms.GetDefaultAsync();
                await _herds.AddAsync(new HerdDto
                {
                    FarmId   = farm?.FarmId ?? 0,
                    HerdName = HerdName.Trim(),
                    HerdType = HerdType.Trim(),
                    IsActive = IsActive,
                });
            }
            else
            {
                await _herds.UpdateAsync(new HerdDto
                {
                    HerdId   = HerdId,
                    HerdName = HerdName.Trim(),
                    HerdType = HerdType.Trim(),
                    IsActive = IsActive,
                });
            }
            _nav.GoBack();
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _nav.GoBack();

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(HerdName)) return "Herd Name is required.";
        if (string.IsNullOrWhiteSpace(HerdType)) return "Herd Type is required.";
        return null;
    }
}
