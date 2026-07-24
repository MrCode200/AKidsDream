using System.Linq;
using Godot;
using AKidsDream.Components;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
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
			if (tile is null)
			{
				GD.Print("Invalid tile");
				return;
			}

			if (_currentSelectedUnit.MoveC.ValidateMove(tile.TileLocation))
			{
				_currentSelectedUnit.MoveC.Move(tile.TileLocation);
			}
			else if (
				tile.Unit is not null &&
				_currentSelectedUnit.AttackC.ValidateAttack(tile.TileLocation)
			)
			{
				_currentSelectedUnit.AttackC.Attack(tile.Unit);
			}
			else
			{
				GD.Print("Invalid move");
				return;
			}

			MovementVisualizer.Tilemap.Clear();
			_currentSelectedUnit.SelectableC.IsSelected = false;
			_currentSelectedUnit = null;
		}
	}

	public void OnUnitSelected(Unit unit)
	{
		if (_currentSelectedUnit != null)
		{
			_currentSelectedUnit.SelectableC.IsSelected = false;
		}

		_currentSelectedUnit = unit;
		if (unit.DeathC is not null)
		{
			unit.DeathC.UnitKilled += OnUnitKilled;
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
		if (unit.DeathC is not null) unit.DeathC.UnitKilled -= OnUnitKilled;
		_currentSelectedUnit = null;
		MovementVisualizer.Tilemap.Clear();
	}
}
