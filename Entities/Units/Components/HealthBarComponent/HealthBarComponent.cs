using Godot;
using System;
using AKidsDream.Common.Components.TweenComponent.Resources;

namespace AKidsDream.Common.Components.TweenComponent.Resources;

[GlobalClass]
public partial class HealthBarComponent : Node
{
	[Export] public HealthComponent HealthComponent;
}
