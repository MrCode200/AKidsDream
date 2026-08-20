#nullable enable
using Godot;
using System;
using AKidsDream.Abilities;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;

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
	}
	
	public override void _ExitTree()
	{
		EventBus.Instance.AbilityCostUpdated -= OnAbilityCostUpdated;
	}
	
	public void SetPool(Unit unit)
	{
		if (_selectedUnit?.UnitId == unit.UnitId)
		{
			UpdatePoolItems(updateAll: true);
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
	
	private void UpdatePoolItems(StringName[]? poolsToUpdate = null, bool updateAll = false)
	{
		if (_selectedUnit == null) return;
		
		foreach (var (_, poolData) in _selectedUnit.AbilityC.Pools)
		{
			if (!updateAll && !poolsToUpdate.Contains(poolData.Name)) continue;
			if (_poolItemsMap.TryGetValue(poolData.Name, out var poolItem))
			{
				poolItem.SetPoolItem(poolData);
			}
		}
	}
	
	private void OnAbilityCostUpdated(Unit unit, AbilityData ability, int newCount)
	{
		if (_selectedUnit?.UnitId == unit.UnitId)
		{
			UpdatePoolItems([ability.PoolName]);
		}
	}
}
