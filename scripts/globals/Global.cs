using System.Collections.Generic;
using System.IO;
using Godot;

namespace AKidsDream.Globals;

// TODO:
// 1. Create Ui, on select of Unit display UI, then show available actions, uses AbilityData.Icon;
// 2. Update Inputhanlder and visualizer respectively
// 3. Update so it Abilities can choose multiple tiles
// 4. Change Unit Stats name to be sth that holds general Unit Data (stats and teams and co)
// 5. Before Continuing you need to know what game you want to make 
// cuz after attack still move or not or what ...
// Add UI which shows the Turn and stops player turn and thus turn events 
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
        RedTile
    }
    
    public static readonly Dictionary<AtlasCoordsSprite, Vector2I> AtlasCoordsSpriteVectors = new()
    {
        { AtlasCoordsSprite.TransparentTile, new Vector2I(0, 0) },
        { AtlasCoordsSprite.GreenTile, new Vector2I(4, 12) },
        { AtlasCoordsSprite.RedTile, new Vector2I(5, 12) }
    };
    
    /* 
    public override void _Ready()
    {
        Instance = this;
    }
    */
}