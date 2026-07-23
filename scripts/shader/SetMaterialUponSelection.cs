using AKidsDream.Components;
using Godot;

public partial class SetMaterialUponSelection : Sprite2D
{
	[Export] public ShaderMaterial SelectionMaterial;
	[Export] public SelectableComponent SelectableC;
	
	public override void _Ready()
	{
		SelectableC.Selected += (selected) => { Material = selected ? SelectionMaterial : null; };
	}
}
