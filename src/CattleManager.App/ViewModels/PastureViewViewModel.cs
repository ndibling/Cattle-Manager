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

    // Pasture add state (shared, one at a time)
    [ObservableProperty] private string _newPastureName = string.Empty;

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
            var allPastures = (await _pastures.GetAllAsync()).ToList();

            HerdFilterOptions = new ObservableCollection<HerdDto>(_allHerds);
            RebuildBoard(allPastures);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedHerdChanged(HerdDto? value) => _ = ReloadBoardAsync();

    private async Task ReloadBoardAsync()
    {
        var allPastures = (await _pastures.GetAllAsync()).ToList();
        RebuildBoard(allPastures);
    }

    private void RebuildBoard(List<PastureDto> allPastures)
    {
        var groups = new ObservableCollection<HerdBoardGroup>();
        var herds  = SelectedHerd is null ? _allHerds : _allHerds.Where(h => h.HerdId == SelectedHerd.HerdId).ToList();
        var assignedNames = allPastures.Select(p => p.PastureName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var herd in herds)
        {
            var herdAnimals = _allAnimals.Where(a => a.HerdId == herd.HerdId).ToList();
            var group = new HerdBoardGroup { Herd = herd };

            foreach (var pasture in allPastures)
            {
                var lane = new PastureLane { Pasture = pasture, PastureName = pasture.PastureName };
                foreach (var animal in herdAnimals.Where(a =>
                    string.Equals(a.PastureLocation, pasture.PastureName, StringComparison.OrdinalIgnoreCase)))
                    lane.Animals.Add(animal);
                group.Lanes.Add(lane);
            }

            // Unassigned: animals with no pasture or a pasture name not in the defined list
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
        // Remove from current lane in every group
        foreach (var group in HerdGroups)
            foreach (var lane in group.Lanes)
                lane.Animals.Remove(animal);

        // Update the animal in the master list
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

    [RelayCommand]
    public async Task AddPastureToGroupAsync(HerdBoardGroup group)
    {
        var name = NewPastureName.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        var dto = await _pastures.AddAsync(new PastureDto { PastureName = name });
        var lane = new PastureLane { Pasture = dto, PastureName = dto.PastureName };
        // Insert before the Unassigned lane
        var unassignedIdx = group.Lanes.Count - 1;
        if (unassignedIdx >= 0)
            group.Lanes.Insert(unassignedIdx, lane);
        else
            group.Lanes.Add(lane);

        // Also add the lane to all other herd groups (same pasture list applies globally)
        foreach (var otherGroup in HerdGroups)
        {
            if (otherGroup == group) continue;
            var otherLane = new PastureLane { Pasture = dto, PastureName = dto.PastureName };
            var idx = otherGroup.Lanes.Count - 1;
            if (idx >= 0) otherGroup.Lanes.Insert(idx, otherLane);
            else otherGroup.Lanes.Add(otherLane);
        }

        NewPastureName = string.Empty;
    }

    public async Task CommitRenameAsync(PastureLane lane)
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

        // Update all animal cards across all groups
        foreach (var group in HerdGroups)
            foreach (var l in group.Lanes)
                foreach (var animal in l.Animals.Where(a =>
                    string.Equals(a.PastureLocation, oldName, StringComparison.OrdinalIgnoreCase)).ToList())
                {
                    animal.PastureLocation = newName;
                    await _animals.UpdateAsync(animal);
                }

        // Update all lane headers in every herd group
        foreach (var group in HerdGroups)
            foreach (var l in group.Lanes.Where(l => l.Pasture?.PastureId == lane.Pasture.PastureId))
                l.PastureName = newName;

        lane.IsRenaming = false;
    }

    public async Task DeletePastureAsync(PastureLane lane)
    {
        if (lane.Pasture is null) return;

        int count = HerdGroups.Sum(g => g.Lanes
            .Where(l => l.Pasture?.PastureId == lane.Pasture.PastureId)
            .Sum(l => l.Animals.Count));

        if (count > 0)
        {
            string msg = $"{count} animal(s) are in \"{lane.PastureName}\". They will be moved to Unassigned. Continue?";
            if (!_dialog.Confirm(msg, "Delete Pasture")) return;

            foreach (var group in HerdGroups)
            {
                var matchingLane = group.Lanes.FirstOrDefault(l => l.Pasture?.PastureId == lane.Pasture.PastureId);
                if (matchingLane is null) continue;
                var unassigned = group.Lanes.FirstOrDefault(l => l.IsUnassigned);
                foreach (var animal in matchingLane.Animals.ToList())
                {
                    animal.PastureLocation = null;
                    unassigned?.Animals.Add(animal);
                    await _animals.UpdateAsync(animal);
                }
            }
        }

        await _pastures.DeleteAsync(lane.Pasture.PastureId);

        foreach (var group in HerdGroups)
        {
            var toRemove = group.Lanes.Where(l => l.Pasture?.PastureId == lane.Pasture.PastureId).ToList();
            foreach (var l in toRemove) group.Lanes.Remove(l);
        }
    }
}
