using System.Collections.Generic;
using System.Linq;
using AKidsDream.Abilities;
using AKidsDream.Abilities.Effects;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;
using Godot;
using Godot.Collections;

namespace AKidsDream.Core.Managers;

public enum BlockingStrategy
{
    BlockOnAbilityCast,
    BlockOnBlockingTrigger,
    BlockOnEffectApply
}

public interface IBlockable
{
    public bool IsBlocked { get; set; }
    public Array<BlockingStrategy> Strategies { get; set; }
    void SetBlocked(bool block);
}

public partial class BlockingManager : Node
{
    public static BlockingManager Instance { get; private set; } = null!;

    private readonly System.Collections.Generic.Dictionary<BlockingStrategy, HashSet<IBlockable>> _stratToObjects =
        new();

    // If it becomes much, move registration of new Strat, the first time it is called in SetBlocking/Registration of newObj (dynamic registration)
    private readonly System.Collections.Generic.Dictionary<BlockingStrategy, int> _activeCounts = new()
    {
        { BlockingStrategy.BlockOnAbilityCast, 0 },
        { BlockingStrategy.BlockOnBlockingTrigger, 0 },
        { BlockingStrategy.BlockOnEffectApply, 0 }
    };

    // -- SIGNAL REGISTRATION/LOGIC --
    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        EventBus.Instance.AbilityCastStart += OnAbilityCastStart;
        EventBus.Instance.AbilityCastEnd += OnAbilityCastEnd;

        EventBus.Instance.EffectTriggerStart += OnEffectTriggerStart;
        EventBus.Instance.EffectTriggerEnd += OnEffectTriggerEnd;

        EventBus.Instance.EffectApplyStart += OnEffectApplyStart;
        EventBus.Instance.EffectApplyEnd += OnEffectApplyEnd;
    }

    public override void _ExitTree()
    {
        Instance = null;
        _stratToObjects.Clear();
        _activeCounts.Clear();

        EventBus.Instance.AbilityCastStart -= OnAbilityCastStart;
        EventBus.Instance.AbilityCastEnd -= OnAbilityCastEnd;

        EventBus.Instance.EffectTriggerStart -= OnEffectTriggerStart;
        EventBus.Instance.EffectTriggerEnd -= OnEffectTriggerEnd;

        EventBus.Instance.EffectApplyStart -= OnEffectApplyStart;
        EventBus.Instance.EffectApplyEnd -= OnEffectApplyEnd;
    }

    private void OnAbilityCastStart(Unit _, AbilityData __) => SetBlocking(BlockingStrategy.BlockOnAbilityCast, true);

    private void OnAbilityCastEnd(Unit _, AbilityData __, EffectResult ___) =>
        SetBlocking(BlockingStrategy.BlockOnAbilityCast, false);

    private void OnEffectTriggerStart(Unit _, AbilityData __, EffectData effectData)
    {
        if (effectData.BlockOnTrigger)
            SetBlocking(BlockingStrategy.BlockOnBlockingTrigger, true);
    }

    private void OnEffectTriggerEnd(Unit _, AbilityData __, EffectData effectData)
    {
        if (effectData.BlockOnTrigger)
            SetBlocking(BlockingStrategy.BlockOnBlockingTrigger, false);
    }

    private void OnEffectApplyStart(Unit _, AbilityData __, EffectData ___) =>
        SetBlocking(BlockingStrategy.BlockOnEffectApply, true);

    private void OnEffectApplyEnd(Unit _, AbilityData __, EffectData ___, EffectResult ____) =>
        SetBlocking(BlockingStrategy.BlockOnEffectApply, false);

    // -- REGISTRATION LOGIC --

    public void Register(IBlockable blockable)
    {
        foreach (var strategy in blockable.Strategies)
        {
            if (!_stratToObjects.TryGetValue(strategy, out var objects))
            {
                objects = new HashSet<IBlockable>();
                _stratToObjects.Add(strategy, objects);
            }

            objects.Add(blockable);
        }

        Refresh(blockable);
    }

    public void UnregisterIBlockable(IBlockable obj)
    {
        foreach (var strat in obj.Strategies)
        {
            _stratToObjects[strat].Remove(obj);
        }
    }


    // -- BLOCKING LOGIC --

    private void SetBlocking(BlockingStrategy strat, bool block)
    {
        if (block) _activeCounts[strat]++;
        else _activeCounts[strat] = _activeCounts[strat] - 1 <= 0 ? 0 : _activeCounts[strat] - 1;

        if (!_stratToObjects.TryGetValue(strat, out var objects) || objects.Count == 0)
            return;

        foreach (var obj in objects)
        {
            // If block = true, skip checks for all of its strategies and set blocking
            if (block)
            {
                if (obj.IsBlocked) continue;
                obj.SetBlocked(true);
            }

            Refresh(obj);
        }
    }

    private void Refresh(IBlockable obj)
    {
        bool shouldBlock = obj.Strategies.Any(strat => _activeCounts.GetValueOrDefault(strat) > 0);
        if (obj.IsBlocked != shouldBlock)
            obj.SetBlocked(shouldBlock);
    }
}