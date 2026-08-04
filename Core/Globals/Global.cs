using System.Collections.Generic;
using System.IO;
using Godot;

namespace AKidsDream.Managers.SaveSystems;

public static class Global
{
	/* (Add the comments to be able to add Godot Functionality to the class
	also make Global partial and inherit from Node
	then add it to the Globals autolaod)
	
	public static Global Instance { get; private set; }
	*/
	
	// [Export(PropertyHint.Range, "1,1,1,or_greater,suffix:px")] 
	// NOTE: could result in floating point error for specific numbers
	public const float TileMapScale = 3.25f;
	public const int TileSize = (int)(16 * TileMapScale); // 16 * 3.25(scale) = 52
	public static readonly string SavePath = Path.Combine(OS.GetUserDataDir(), "saves");

	public enum Groups
	{
		Units,
		EnemyUnits,
		PlayerUnits
	}
	
	public enum UnitColor
	{
		Blue,
		Red
	}
	
	public enum UnitName
	{ 
		Soldier
	}

	public enum InputMapActions
	{
		LeftClick
	}

	public enum AtlasCoordsSprite
	{
		TransparentTile,
		BeigeTile,
		DarkVioletTile,
		// FrameTile,
		GreenTile,
		RedTile,
		PurpleTile
	}
	
	public static readonly Dictionary<AtlasCoordsSprite, Vector2I> AtlasCoordsSpriteVectors = new()
	{
		{ AtlasCoordsSprite.TransparentTile, new Vector2I(0, 0) },
		{ AtlasCoordsSprite.BeigeTile, new Vector2I(1, 0) },
		{ AtlasCoordsSprite.DarkVioletTile, new Vector2I(2, 0) },
		// { AtlasCoordsSprite.FrameTile, new Vector2I(3, 0) },
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
