using Godot;
using AKidsDream.Core.Controllers;

namespace AKidsDream.Units.Resources;

[GlobalClass]
[Tool]
public partial class PoolData : Resource
{
	private int _currentCount;
	private int _maxCount;

	[Export] public int CurrentCount
	{
		get => _currentCount;
		set => _currentCount = Mathf.Clamp(value, 0, MaxCount);
	}	
	[Export] public int MaxCount
	{
		get => _maxCount;
		set
		{
			_maxCount = Mathf.Max(0, value);
			if (_currentCount > _maxCount || Engine.IsEditorHint())
				_currentCount = _maxCount;

			NotifyPropertyListChanged();
		}
	}	
	
	[Export] public StringName Name;
	[Export] public Texture2D Icon;
}
