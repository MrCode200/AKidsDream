using System.IO;
using Godot;

namespace AKidsDream.Globals;

// TODO:
// 1. Change Unit Stats name to be sth that holds general Unit Data (stats and teams and co)
// 2.5 Seperate either logic of MoveComponent into Move and Attack (which can get disabled separately)
// Or let move and attack move through action/unit which is the guard of what can go through and what not
// 3. Add logic for Action Component and GameLoopManager in such a way that MoveActions and AttackActions are tracked seperately
// Visualizer should thus only show move or attack but not both...
// 4. Before Continuing you need to know what game you want to make 
// cuz after attack still move or not or what ...
// Add UI which shows the Turn and stops player turn and thus turn events 
public class Global
{
    /* (Add the comments to be able to add Godot Functionality to the class
    also make Global partial and inherit from Node
    then add it to the Globals autolaod)
    
    public static Global Instance { get; private set; }
    */
    
    // [Export(PropertyHint.Range, "1,1,1,or_greater,suffix:px")] 
    public static int TileSize = 16;
    public static string SavePath = Path.Combine(OS.GetUserDataDir(), "saves");
    
    public enum Groups
    {
        [FieldStringValue( "Units" )]
        Units,
        [FieldStringValue("EnemyUnits")]
        EnemyUnits,
        [FieldStringValue("PlayerUnits")]
        PlayerUnits
    }
    
    public enum UnitTeam
    {
        Player,
        Enemy
    }
    
    public enum UnitName
    {
        [FieldStringValue("Soldier")]
        Soldier
    }
    
    /* 
    public override void _Ready()
    {
        Instance = this;
    }
    */
}