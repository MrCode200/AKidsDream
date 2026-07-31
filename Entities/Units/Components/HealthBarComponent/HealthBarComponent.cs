using Godot;
using System;
using AKidsDream.Units.Resources.Components;

namespace AKidsDream.Units.Resources.Components;

[GlobalClass]
public partial class HealthBarComponent : Node
{
	[Export] public HealthComponent HealthComponent;
}
