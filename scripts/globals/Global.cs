using System.Collections.Generic;
using System.IO;
using Godot;

namespace AKidsDream.Globals;

[Tool]
public class Global
{
	/* (Add the comments to be able to add Godot Functionality to the class
	also make Global partial and inherit from Node
	then add it to the Globals autolaod)
	
	public static Global Instance { get; private set; }
	*/
	
	// [Export(PropertyHint.Range, "1,1,1,or_greater,suffix:px")] 
	public const int TileSize = 16;
	public static readonly string SavePath = Path.Combine(OS.GetUserDataDir(), "saves");
	public const string UnitScenePath = "res://scenes/units";

	public enum Groups
	{
		[FieldValue<string>( "Units" )]
		Units,
		[FieldValue<string>("EnemyUnits")]
		EnemyUnits,
		[FieldValue<string>("PlayerUnits")]
		PlayerUnits
	}
	
	public enum UnitTeam
	{
		Player,
		Enemy
	}
	
	public enum UnitName
	{
		[FieldValue<string>("Soldier")]
		Soldier
	}

	public enum AtlasCoordsSprite
	{
		TransparentTile,
		GreenTile,
		RedTile,
		PurpleTile
	}
	
	public static readonly Dictionary<AtlasCoordsSprite, Vector2I> AtlasCoordsSpriteVectors = new()
	{
		{ AtlasCoordsSprite.TransparentTile, new Vector2I(0, 0) },
		{ AtlasCoordsSprite.GreenTile, new Vector2I(4, 0) },
		{ AtlasCoordsSprite.RedTile, new Vector2I(5, 0) },
		{ AtlasCoordsSprite.PurpleTile, new Vector2I(6, 0) },
	};
	
	/* 
	public override void _Ready()
	{
		Instance = this;
	}
	*/
}
