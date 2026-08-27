using Godot;
using System;
using AKidsDream.Core.Managers;
using AKidsDream.Managers;
using AKidsDream.Managers.SaveSystems;

namespace AKidsDream.UI.RoundCounter;

public partial class RoundCounter : Control
{
    [Export] public Label RoundLabel;
    
    [Signal] public delegate void UpdatedRoundCounterEventHandler(); // Used for tween as signal wrapper
    
    public override void _Ready()
    {
        EventBus.Instance.NewRoundStarted += OnNewRoundStarted;
    }
    
    private void OnNewRoundStarted(int playerIdInt, int newRound)
    {
        RoundLabel.Text = "Round " + newRound;
        EmitSignal(SignalName.UpdatedRoundCounter);
    }
}
