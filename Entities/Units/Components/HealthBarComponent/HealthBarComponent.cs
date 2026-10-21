using Godot;
using System;
using TweenComp.Resources.Blueprint;

namespace TweenComp.Resources.Blueprint;

[GlobalClass]
public partial class HealthBarComponent : Node
{
	[Export] public HealthComponent HealthComponent;
}
