using Godot;
namespace AKidsDream.Scripts;

public class Global
{
    /* (Add the comments to be able to add Godot Functionality to the class
    also make Global partial and inherit from Node
    then add it to the Globals autolaod)
    
    public static Global Instance { get; private set; }
    */
    
    // [Export(PropertyHint.Range, "1,1,1,or_greater,suffix:px")] 
    public static int TileSize = 16;
    
    /* 
    public override void _Ready()
    {
        Instance = this;
    }
    */
}