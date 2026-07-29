using System;
using Godot;
using System.Collections.Generic;

namespace AKidsDream.StateMachines;

public interface IState
{
	public Action<IState, string, bool> ChangeState { get; set; }
	public virtual void Enter() { }
	public virtual void Exit() { }
	public virtual void Update(object payload) { }
	public virtual void PhysicsUpdate(double delta) { }
}



public partial class StateMachine : Node
{
	private Dictionary<string, IState> _states = new();
	public string CurrentStateName => _currentState?.GetType().Name;
	private IState _currentState;
	private bool _isRegisteringNodeStatesFromReady;


	public override void _Ready()
	{
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
			_currentState.Exit();
			_currentState.ChangeState = null;
			_currentState = null;
		}
	}

	/// <summary>
	/// Adds a non-Node state to the state machine.
	/// Node states must be added as children in the scene tree and are automatically registered in _Ready.
	/// </summary>
	/// <param name="state">The state to add. Must not be a Node.</param>
	public void AddState(IState state)
	{
		if (state == null)
		{
			GD.PushError("Cannot add a null state.");
			return;
		}

		if (state is Node && !_isRegisteringNodeStatesFromReady)
		{
			GD.PushError("Cannot add Node states through AddState. Node states must be added as children in the scene tree.");
			return;
		}

		string stateName = state.GetType().Name;

		if (_states.ContainsKey(stateName))
		{
			GD.PushWarning($"State {stateName} already exists. Overwriting...");
			RemoveState(stateName);
		}
		
		state.ChangeState = ChangeState;
		_states.Add(stateName, state);
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
			GD.PushError("Cannot remove Node states through RemoveState. Node states are managed by the scene tree.");
			return;
		}
		_states[stateName].ChangeState = null;
		_states.Remove(stateName);
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
	public void ChangeState(IState state, string stateName, bool force = false)
	{
		if (!force && state.GetType().Name != _currentState?.GetType().Name)
		{
			GD.PushError($"Trying to change state to {stateName}, but caller: {state.GetType().Name} is not current state: {_currentState?.GetType().Name}.");
			return;
		}
		if (!_states.TryGetValue(stateName, out var value))
		{
			GD.PushError($"State {stateName} does not exist. Not changing state.");
			return;
		}

		_currentState?.Exit();

		_currentState = value; 
		
		_currentState.Enter();
	}
	
	/// <summary>
	/// Calls Update on the current state.
	/// </summary>
	public void Update(object payload = null)
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
