#nullable enable
using System;
using Godot;
using System.Collections.Generic;
using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.StateMachines;

public interface IState
{
	public Action<IState, string, bool> ChangeState { get; set; }
	public void Enter() { }
	public void Exit() { }
	public void Update(object? payload) { }
	public void PhysicsUpdate(double delta) { }
}

public partial class StateMachine : Node
{
	private readonly Dictionary<string, IState> _states = new();
	
	private IState? _currentState;
	public string? CurrentStateName => _currentState?.GetType().Name;
	
	private bool _isRegisteringNodeStatesFromReady;
	private ILogger _log = GameLogger.For<StateMachine>();


	public override void _Ready()
	{
		_log = _log.ForContext("StateMachinePath", GetPath());
		_log.Here().Debug("StateMachine initializing");

		_isRegisteringNodeStatesFromReady = true;

		foreach (var child in GetChildren())
		{
			if (child is IState state) AddState(state);
		}
		
		_isRegisteringNodeStatesFromReady = false;
	}

	public override void _ExitTree()
	{
		if (_currentState != null)
		{
			_log.Here().Debug("Exiting state '{CurrentState}'", CurrentStateName);
			_currentState.Exit();
			_currentState.ChangeState = null!;
			_currentState = null;
		}
		_log.Here().Debug("StateMachine destroyed");
	}

	/// <summary>
	/// Adds a non-Node state to the state machine.
	/// Node states must be added as children in the scene tree and are automatically registered in _Ready.
	/// </summary>
	/// <param name="state">The state to add. Must not be a Node.</param>
	public void AddState(IState state)
	{
		if (state == null!)
		{
			_log.Here().Error("Cannot add a null state");
			return;
		}

		if (state is Node && !_isRegisteringNodeStatesFromReady)
		{
			_log.Here().Error("Cannot add Node states through AddState. Node states must be added as children in the scene tree.");
			return;
		}

		string stateName = state.GetType().Name;

		if (_states.ContainsKey(stateName))
		{
			_log.Here().Warn("State '{StateName}' already exists. Overwriting...", stateName);
			RemoveState(stateName);
		}
		
		state.ChangeState = ChangeState;
		_states.Add(stateName, state);
		_log.Here().Debug("Added state '{StateName}'", stateName);
	}

	/// <summary>
	/// Removes a non-Node state from the state machine.
	/// Node states are managed by the scene tree and cannot be removed through this method.
	/// </summary>
	/// <param name="stateName">The name of the state to remove.</param>
	public void RemoveState(string stateName)
	{
		if (!_states.ContainsKey(stateName)) return;
		if (_states[stateName] is Node)
		{
			_log.Here().Error("Cannot remove Node states through RemoveState. Node states are managed by the scene tree.");
			return;
		}
		_states[stateName].ChangeState = null!;
		_states.Remove(stateName);
		_log.Here().Debug("Removed state '{StateName}'", stateName);
	}

	/// <summary>
	/// Removes all non-Node states from the state machine.
	/// Node states are not affected as they are managed by the scene tree.
	/// </summary>
	public void ClearStates()
	{
		var names = new List<string>(_states.Keys);
		foreach (var n in names)
		{
			RemoveState(n);
		}
	}
	
	/// <summary>
	/// Transitions to the specified state.
	/// </summary>
	/// <param name="state">The state that called the transition. Gets ignored if <see cref="force"/> is true</param>
	/// <param name="stateName">The name of the state to transition to.</param>
	/// <param name="force">If false, checks if the function call comes from the currently active state.</param>
	public void ChangeState(IState? state, string stateName, bool force = false)
	{
		if (!force && state?.GetType().Name != _currentState?.GetType().Name)
		{
			_log.Here().Error(
				"Trying to change state to '{TargetState}', but caller '{CallerState}' is not current state '{CurrentState}'",
				stateName,
				state?.GetType().Name,
				_currentState?.GetType().Name);
			return;
		}
		if (!_states.TryGetValue(stateName, out var value))
		{
			_log.Here().Error("State '{StateName}' does not exist. Not changing state.", stateName);
			return;
		}

		var previousState = CurrentStateName;
		_currentState?.Exit();

		_currentState = value; 
		
		_currentState.Enter();

		_log.Here().Debug(
			"Changed state from '{PreviousState}' to '{NewState}'",
			previousState ?? "None",
			stateName);
	}
	
	/// <summary>
	/// Calls Update on the current state.
	/// </summary>
	public void Update(object? payload = null)
	{
		_currentState?.Update(payload);
	}
	
	/// <summary>
	/// Calls PhysicsUpdate on the current state.
	/// </summary>
	/// <param name="delta">Time elapsed since the last frame.</param>
	public void PhysicsUpdate(double delta)
	{
		_currentState?.PhysicsUpdate(delta);
	}
}
