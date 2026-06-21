using CattleManager.App.Services;
using CattleManager.Core.Models;
using CattleManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;

namespace CattleManager.App.ViewModels;

public partial class HerdBoardGroup : ObservableObject
{
    public HerdDto Herd { get; init; } = null!;
    public ObservableCollection<PastureLane> Lanes { get; } = [];
    public int TotalAnimalCount => Lanes.Sum(l => l.Animals.Count);

    [ObservableProperty] private string _newPastureName = string.Empty;
}

public partial class PastureLane : ObservableObject
{
    public PastureDto? Pasture { get; init; }

    [ObservableProperty] private string _pastureName = string.Empty;
    [ObservableProperty] private bool _isRenaming;
    [ObservableProperty] private bool _isDragOver;
    [ObservableProperty] private string _renameText = string.Empty;

    public bool IsUnassigned => Pasture is null;
    public ObservableCollection<AnimalDto> Animals { get; } = [];
}

public partial class PastureViewViewModel : ObservableObject
{
    private readonly IPastureRepository _pastures;
    private readonly IAnimalRepository _animals;
    private readonly IHerdRepository _herds;
    private readonly DialogService _dialog;

    private List<AnimalDto> _allAnimals = [];
    private List<HerdDto> _allHerds = [];

    [ObservableProperty] private ObservableCollection<HerdBoardGroup> _herdGroups = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<HerdDto> _herdFilterOptions = [];
    [ObservableProperty] private HerdDto? _selectedHerd;

    public PastureViewViewModel(IPastureRepository pastures, IAnimalRepository animals,
        IHerdRepository herds, DialogService dialog)
    {
        _pastures = pastures;
        _animals  = animals;
        _herds    = herds;
        _dialog   = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allHerds   = (await _herds.GetAllAsync()).ToList();
            _allAnimals = (await _animals.GetAllAsync()).ToList();

            HerdFilterOptions = new ObservableCollection<HerdDto>(_allHerds);
            await RebuildBoardAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedHerdChanged(HerdDto? value) => _ = RebuildBoardAsync();

    private async Task RebuildBoardAsync()
    {
        var groups = new ObservableCollection<HerdBoardGroup>();
        var herds  = SelectedHerd is null ? _allHerds : _allHerds.Where(h => h.HerdId == SelectedHerd.HerdId).ToList();

        foreach (var herd in herds)
        {
            var herdPastures = (await _pastures.GetByHerdAsync(herd.HerdId)).ToList();
            var herdAnimals  = _allAnimals.Where(a => a.HerdId == herd.HerdId).ToList();
            var group        = new HerdBoardGroup { Herd = herd };

            var assignedNames = herdPastures.Select(p => p.PastureName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var pasture in herdPastures)
            {
                var lane = new PastureLane { Pasture = pasture, PastureName = pasture.PastureName };
                foreach (var animal in herdAnimals.Where(a =>
                    string.Equals(a.PastureLocation, pasture.PastureName, StringComparison.OrdinalIgnoreCase)))
                    lane.Animals.Add(animal);
                group.Lanes.Add(lane);
            }

            var unassigned = new PastureLane { Pasture = null, PastureName = "Unassigned" };
            foreach (var animal in herdAnimals.Where(a =>
                string.IsNullOrWhiteSpace(a.PastureLocation) || !assignedNames.Contains(a.PastureLocation)))
                unassigned.Animals.Add(animal);
            group.Lanes.Add(unassigned);

            groups.Add(group);
        }

        HerdGroups = groups;
    }

    public async Task MoveAnimalAsync(AnimalDto animal, PastureLane targetLane, HerdBoardGroup targetGroup)
    {
        foreach (var group in HerdGroups)
            foreach (var lane in group.Lanes)
                lane.Animals.Remove(animal);

        animal.HerdId = targetGroup.Herd.HerdId;
        animal.PastureLocation = targetLane.IsUnassigned ? null : targetLane.PastureName;
        targetLane.Animals.Add(animal);

        try
        {
            await _animals.UpdateAsync(animal);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist animal move");
            _dialog.ShowError($"Failed to save move: {ex.Message}");
        }
    }

    public async Task AddPastureToGroupAsync(HerdBoardGroup group)
    {
        var name = group.NewPastureName.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        var dto  = await _pastures.AddAsync(new PastureDto { HerdId = group.Herd.HerdId, PastureName = name });
        var lane = new PastureLane { Pasture = dto, PastureName = dto.PastureName };

        // Insert before the Unassigned lane
        var insertIdx = group.Lanes.Count - 1;
        if (insertIdx >= 0) group.Lanes.Insert(insertIdx, lane);
        else group.Lanes.Add(lane);

        group.NewPastureName = string.Empty;
    }

    public async Task CommitRenameAsync(PastureLane lane, HerdBoardGroup ownerGroup)
    {
        if (lane.Pasture is null) return;
        var newName = lane.RenameText.Trim();
        if (string.IsNullOrWhiteSpace(newName) || newName == lane.PastureName)
        {
            lane.IsRenaming = false;
            return;
        }

        var oldName = lane.PastureName;
        lane.Pasture.PastureName = newName;
        await _pastures.UpdateAsync(lane.Pasture);

        // Update PastureLocation only on animals belonging to the same herd
        foreach (var l in ownerGroup.Lanes)
            foreach (var animal in l.Animals.Where(a =>
                string.Equals(a.PastureLocation, oldName, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                animal.PastureLocation = newName;
                await _animals.UpdateAsync(animal);
            }

        lane.PastureName = newName;
        lane.IsRenaming  = false;
    }

    public async Task DeletePastureAsync(PastureLane lane, HerdBoardGroup ownerGroup)
    {
        if (lane.Pasture is null) return;

        int count = lane.Animals.Count;
        if (count > 0)
        {
            string msg = $"{count} animal(s) are in \"{lane.PastureName}\". They will be moved to Unassigned. Continue?";
            if (!_dialog.Confirm(msg, "Delete Pasture")) return;

            var unassigned = ownerGroup.Lanes.FirstOrDefault(l => l.IsUnassigned);
            foreach (var animal in lane.Animals.ToList())
            {
                animal.PastureLocation = null;
                unassigned?.Animals.Add(animal);
                await _animals.UpdateAsync(animal);
            }
        }

        await _pastures.DeleteAsync(lane.Pasture.PastureId);
        ownerGroup.Lanes.Remove(lane);
    }
}
