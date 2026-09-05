using AKidsDream.Common.Components.TweenComponent.Resources;
using Godot;

namespace AKidsDream.Shaders;

[Tool]
public partial class SetMaterialUponSelection : Node
{
	[Export] public Node2D TargetNode2D;
	[Export] public ShaderMaterial SelectionMaterial;
	[Export] public SelectableComponent SelectableC;
	
	// -- EDITOR TOOLS --
	[ExportToolButton("ToggleMaterial")] public Callable ToggleMaterialOnObject => Callable.From(ToggleMaterial);
	
	public override void _Ready()
	{
		if (Engine.IsEditorHint() || TargetNode2D is null || SelectableC is null) return;
		
		TargetNode2D.Material = null;
		SelectableC.Selected += (selected) => { TargetNode2D.Material = selected ? SelectionMaterial : null; };
	}
	
	public void ToggleMaterial()
	{
		TargetNode2D.Material = TargetNode2D.Material is null ? SelectionMaterial : null;
	}
}
