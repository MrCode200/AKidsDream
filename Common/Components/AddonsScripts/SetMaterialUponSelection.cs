using AKidsDream.Units.Resources.Components;
using Godot;

namespace AKidsDream.Shaders;

[Tool]
public partial class SetMaterialUponSelection : Sprite2D
{
	[Export] public ShaderMaterial SelectionMaterial;
	[Export] public SelectableComponent SelectableC;
	
	// -- EDITOR TOOLS --
	[ExportToolButton("ToggleMaterial")] public Callable ToggleMaterialOnObject => Callable.From(ToggleMaterial);
	
	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;
		
		Material = null;
		SelectableC.Selected += (selected) => { Material = selected ? SelectionMaterial : null; };
	}
	
	public void ToggleMaterial()
	{
		Material = Material is null ? SelectionMaterial : null;
	}
}
