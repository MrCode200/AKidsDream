using System.Collections.Generic;
using System.IO;
using AKidsDream.Common.Registry;
using AKidsDream.Utilities;
using AKidsDream.Core.Content;
using AKidsDream.Core.Managers.GameModeManager;
using AKidsDream.Core.Managers.ModManager;
using AKidsDream.Entities.Cards;
using AKidsDream.Util.Identifiers;
using AKidsDream.Utilities.Registry;
using AKidsDream.Utilities.Registry.TypeExtensions;
using Godot;

namespace AKidsDream.Managers.SaveSystems;

public static class Global
{
	/* (Add the comments to be able to add Godot Functionality to the class
	also make Global partial and inherit from Node
	then add it to the Globals autolaod)
	
	public static Global Instance { get; private set; }
	*/
	
	// REGISTRIES
	public static readonly Registry<NamespacedKey, ModManifest> ModRegistry = new("ModRegistry");
	public static readonly Registry<NamespacedKey, UnitDefinition> UnitRegistry = new("UnitRegistry");
	public static readonly Registry<NamespacedKey, CardDefinition> CardRegistry = new("CardRegistry");
	public static readonly Registry<NamespacedKey, GameModeDefinition> GameModeRegistry = new("GameModeRegistry");
	public static readonly Registry<NamespacedKey, ControllerDefinition> ControllerRegistry = new("ControllerRegistry");
	public static readonly Registry<NamespacedKey, ConsoleCommandDefinition> ConsoleCommandRegistry = new("ConsoleCommandRegistry");
	
	// [Export(PropertyHint.Range, "1,1,1,or_greater,suffix:px")] 
	// NOTE: could result in floating point error for specific numbers
	public const float TileMapScale = 3.25f;
	public const int TileSize = (int)(16 * TileMapScale); // 16 * 3.25(scale) = 52
	public static readonly string SavePath = Path.Combine(OS.GetUserDataDir(), "saves");
	public static readonly string ModsPath = ProjectSettings.GlobalizePath("res://Mods");

	public enum Groups
	{
		Board,
		Units,
		EnemyUnits,
		PlayerUnits
	}

	public enum CollisionMasks
	{
		[FieldValue<uint>(1)] // Mask 2 ^0 = 1
		UnitsMask,
		[FieldValue<uint>(32)] // Mask 2 ^5 = 32
		CardsMask
	}
	
	public enum UnitColor
	{
		Blue,
		Red
	}
	
	public enum UnitType
	{
		Unassigned,
		Timmy,
		Soldier,
		King
	}

	public enum InputMapActions
	{
		Select,
		Deselect
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

	public const int ManaPerRound = 1;
	public const int CardsPerRound = 3;

	/*
	public override void _Ready()
	{
		Instance = this;
	}
	*/
}
