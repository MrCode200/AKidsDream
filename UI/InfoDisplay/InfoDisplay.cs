using System.Data;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.VFX;
using AKidsDream.Core.Teams;
using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Res.Common.Components.TweenComponent.Resources;
using AKidsDream.Util.Identifiers;

namespace AKidsDream.UI.RoundCounter;

public partial class InfoDisplay : Control
{
	[Export] public Label RoundLabel;
	[Export] public Label PlayersTurnLabel;
	[Export] public Label ManaLabel;
	[Export] public FloatingText ManaFloatingText;

	[Export] public TweenComponent PlayerLabelTweenComp;
	[Export] public TweenComponent RoundCounterTweenComp;

	public override void _Ready()
	{
		EventBus.Instance.NewRoundStarted += OnNewRoundStarted;
		EventBus.Instance.TurnStarted += OnTurnStarted;
		EventBus.Instance.ManaChanged += OnManaChanged;
		EventBus.Instance.TurnStarted += OnTurnStarted;
		ManaFloatingText.DefaultColor = Color.Color8(104, 194, 245);
	}

	public override void _ExitTree()
	{
		EventBus.Instance.NewRoundStarted -= OnNewRoundStarted;
		EventBus.Instance.TurnStarted -= OnTurnStarted;
		EventBus.Instance.ManaChanged -= OnManaChanged;
	}

	private void OnManaChanged(int oldMana, int newMana)
	{
		ManaLabel.Text = $"Mana: {newMana}";

		var manaChange = newMana - oldMana;
		var textOperator =
			manaChange > 0 ? "+" :
			manaChange < 0 ? "" :
			"=";

		ManaFloatingText.SetNewFloatingText($"{textOperator}{manaChange}");
	}

	private void OnNewRoundStarted(int playerIdInt, int newRound)
	{
		RoundLabel.Text = "Round " + newRound;
		RoundCounterTweenComp.PlayTween(TweenAnimationIdentifiers.BumbUpRoundCounter);
	}

	private void OnTurnStarted(PlayerData player, int round)
	{
		PlayersTurnLabel.Text = $"{player.Name}'s Turn";
		PlayerLabelTweenComp.PlayTween(TweenAnimationIdentifiers.BumbUpPlayerName);

		if (!player.UsesPIC) return;
		
		ManaFloatingText.SetNewFloatingText($"+{Global.ManaPerRound}");
		ManaLabel.Text = $"Mana: {player.Mana}";
	}
}
