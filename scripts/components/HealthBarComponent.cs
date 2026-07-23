using Godot;
using System;
using AKidsDream.Components;

namespace AKidsDream.Components;

[GlobalClass]
public partial class HealthBarComponent : Node
{
	[Export] public HealthComponent HealthComponent;
}
