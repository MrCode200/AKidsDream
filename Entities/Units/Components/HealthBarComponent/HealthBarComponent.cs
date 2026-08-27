using Godot;
using System;
using AKidsDream.Common.Components;

namespace AKidsDream.Common.Components;

[GlobalClass]
public partial class HealthBarComponent : Node
{
	[Export] public HealthComponent HealthComponent;
}
