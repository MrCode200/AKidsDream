using System.Linq;
using Godot;
using AKidsDream.GameBoard;
using AKidsDream.Scripts;
using AKidsDream.Components;

namespace AKidsDream.Units;

[GlobalClass]
public partial class SoldierMovement : MoveComponent
{
	public override Vector2I[] ValidMoves()
	{
		return Enumerable.Range(0, 9)
			.Select(i => new Vector2I(i % 3 - 1, i / 3 - 1))
			.Where(tile => tile != Vector2I.Zero)
			.Select(offset => TileLocation + offset)
			.Where(move => Board.Instance.TileInBoard(move))
			.Where(tile => Board.Instance.GetUnitAt(tile) == null)
			.ToArray();
	}

	public override Vector2I[] ValidAttacks()
	{
		return Enumerable.Range(0, 9)
			.Select(i => new Vector2I(i % 3 - 1, i / 3 - 1))
			.Where(tile => tile != Vector2I.Zero)
			.Select(offset => TileLocation + offset)
			.Where(move => Board.Instance.TileInBoard(move))
			.Where(tile => Board.Instance.GetUnitAt(tile)?.Stats.Team == Utils.UnitTeam.Enemy)
			.ToArray();
	}
}
