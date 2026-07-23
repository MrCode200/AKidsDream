using System.Linq;
using Godot;
using AKidsDream.Components;
using AKidsDream.GameBoard;
using AKidsDream.Scripts;
using AKidsDream.Units;
using TileData = AKidsDream.GameBoard.TileData;

public partial class InputHandler : Node2D
{
	[Export] public Visualizer MovementVisualizer;
	private Unit _currentSelectedUnit;
	
	public override void _Ready()
	{
		EventBus.Instance.UnitSelected += OnUnitSelected;
		EventBus.Instance.UnitDeselected += OnUnitDeselected;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("LeftClick"))
		{
			OnLeftClick();
		}
	}

	public void OnLeftClick()
	{
		if (_currentSelectedUnit is not null)
		{
			TileData tile = Board.Instance.WorldPositionToTile(GetGlobalMousePosition());
			if (tile is null || !_currentSelectedUnit.MoveC.ValidTiles().Contains(tile.TileLocation))
			{
				GD.Print("Invalid tile");
				return;
			};
			if (tile.Unit is null)
			{
				_currentSelectedUnit.MoveC.Move(tile.TileLocation);
			}
			else
			{
				_currentSelectedUnit.GetNode<AttackComponent>("AttackComponent").Attack(tile.Unit);
			}
			MovementVisualizer.Tilemap.Clear();
			_currentSelectedUnit.GetNode<SelectableComponent>("SelectableComponent").IsSelected = false;
			_currentSelectedUnit = null;
		}
	}
	
	public void OnUnitSelected(Unit unit)
	{
		if (_currentSelectedUnit != null)
		{
			_currentSelectedUnit.GetNode<SelectableComponent>("SelectableComponent").IsSelected = false;
		}
		_currentSelectedUnit = unit;
		DeathComponent deathComponent = _currentSelectedUnit.GetNodeOrNull<DeathComponent>("DeathComponent");
		if (deathComponent is not null)
		{
			deathComponent.UnitKilled += OnUnitKilled;	
		}
		MovementVisualizer.ShowUnitValidMoves(unit);
	}
	
	public void OnUnitKilled(Unit unit)
	{
		_currentSelectedUnit = null;	
	}
	
	public void OnUnitDeselected(Unit unit)
	{
		if (unit != _currentSelectedUnit) return;
		DeathComponent deathComponent = _currentSelectedUnit.GetNodeOrNull<DeathComponent>("DeathComponent");
		if (deathComponent is not null) deathComponent.UnitKilled -= OnUnitKilled;
		_currentSelectedUnit = null;
		MovementVisualizer.Tilemap.Clear();
	}
}
