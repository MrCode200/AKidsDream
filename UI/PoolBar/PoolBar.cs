#nullable enable
using Godot;
using System;
using AKidsDream.Abilities;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;

public partial class PoolBar : Control
{
    [Export] public PackedScene PoolItem = null!;
    [Export] public Node PoolItemsContainer = null!;

    private Unit? _selectedUnit;
    private readonly System.Collections.Generic.Dictionary<StringName, PoolItem> _poolItemsMap = new();

    // -- LOGIC --
    public override void _Ready()
    {
        EventBus.Instance.AbilityCostUpdated += OnAbilityCostUpdated;
        EventBus.Instance.NewTileHovered += OnNewTileHovered;
        EventBus.Instance.AbilityDeselected += OnAbilityDeselected;
    }

    public override void _ExitTree()
    {
        EventBus.Instance.AbilityCostUpdated -= OnAbilityCostUpdated;
        EventBus.Instance.NewTileHovered -= OnNewTileHovered;
        EventBus.Instance.AbilityDeselected -= OnAbilityDeselected;
    }

    public void SetPool(Unit unit)
    {
        if (_selectedUnit?.UnitId == unit.UnitId)
        {
            SetPoolItems(setAll: true);
            return;
        }

        _selectedUnit = unit;
        _poolItemsMap.Clear();

        foreach (var child in PoolItemsContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var (_, poolData) in unit.AbilityC.Pools)
        {
            var poolItem = PoolItem.Instantiate() as PoolItem;
            PoolItem.ResourceName = poolData.Name;
            poolItem!.SetPoolItem(poolData);
            PoolItemsContainer.AddChild(poolItem);
            _poolItemsMap[poolData.Name] = poolItem;
        }
    }

    private void SetPoolItems(StringName[]? poolsToSet = null, bool setAll = false)
    {
        if (_selectedUnit == null) return;

        foreach (var (_, poolData) in _selectedUnit.AbilityC.Pools)
        {
            if (!setAll && !poolsToSet.Contains(poolData.Name)) continue;
            if (_poolItemsMap.TryGetValue(poolData.Name, out var poolItem))
            {
                poolItem.SetPoolItem(poolData);
            }
        }
    }

    // -- EVENT HANDLERS --

    private void OnNewTileHovered(Unit caster, AbilityContext ctx, AbilityPayload payload)
    {
        if (_selectedUnit?.UnitId != caster.UnitId || 
            !_poolItemsMap.TryGetValue(ctx.Ability.PoolName, out var poolItem)) return;

        if (payload.AccumulatedTargets.Count == 0)
        {
            ResetPoolPreviews();
            return;
        }
        
        var previewCost = ctx.Ability.GetCost(ctx, payload);

        poolItem.UpdatePoolPreview(previewCost);
    }

    private void OnAbilityCostUpdated(Unit unit, AbilityData ability, int newCount)
    {
        if (_selectedUnit?.UnitId == unit.UnitId)
        {
            SetPoolItems([ability.PoolName]);
        }
    }

    private void OnAbilityDeselected(Unit unit)
    {
        if (_selectedUnit?.UnitId == unit.UnitId)
            ResetPoolPreviews();
    }

    private void ResetPoolPreviews()
    {
        foreach (var poolItem in _poolItemsMap.Values)
        {
            poolItem.ResetPoolPreview();
        }
    }
}