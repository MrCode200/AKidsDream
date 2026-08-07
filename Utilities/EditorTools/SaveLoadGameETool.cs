using System;
using System.IO;
using System.Linq;
using AKidsDream.Core.Controllers;
using AKidsDream.Core.Managers;
using AKidsDream.Core.Teams;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystem.Resources;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;
using Godot;
using Godot.Collections;

namespace AKidsDream.editorTools;

[Tool]
[GlobalClass]
public partial class SaveLoadGameETool : EditorScript
{
    private const string DefaultSaveName = "DevBoardSave.tres";
    private const string DefaultSceneBoardNodeName = "Board";
    private const string DefaultSceneEntityLayerNodeName = "EntityLayer";

    private string _fileName = DefaultSaveName;
    private string _loadFileName = DefaultSaveName;
    private bool _includeBoardState = true;
    private bool _includeUnits = true;
    private bool _reassignUnitOwnersAfterLoad = true;
    private bool _loadWithoutGameManager = false;
    
    // Manual input fields for player/team data
    private string _playerDataJson = "";
    private string _teamDataJson = "";

    private enum CustomStateKind
    {
        None,
        BoardOnly,
        UnitsOnly,
        FullGameState
    }

    public override void _Run()
    {
        _showDialog();
    }

    private void _showDialog()
    {
        var window = new Window
        {
            Title = "Save / Load Game State",
            MinSize = new Vector2I(520, 700)
        };

        EditorInterface.Singleton.PopupDialog(window, new Rect2I(new Vector2I(100, 100), window.MinSize));

        var container = new VBoxContainer();
        window.AddChild(container);

        container.AddChild(_createLabeledLineEdit("Save File Name", DefaultSaveName, text => _fileName = _normalizeSaveName(text)));
        container.AddChild(_createLabeledLineEdit("Load File Name", DefaultSaveName, text => _loadFileName = _normalizeSaveName(text)));

        container.AddChild(_createDropdown("Custom State Mode", Enum.GetNames(typeof(CustomStateKind)), 2, index =>
        {
            // Currently reserved for future expansion; this keeps the UI extensible
            // without breaking the current save/load flow.
        }));

        container.AddChild(_createToggle("Include Board State", _includeBoardState, value => _includeBoardState = value));
        container.AddChild(_createToggle("Include Units", _includeUnits, value => _includeUnits = value));
        container.AddChild(_createToggle("Reassign Unit Owners After Load", _reassignUnitOwnersAfterLoad, value => _reassignUnitOwnersAfterLoad = value));
        container.AddChild(_createToggle("Load Without GameManager", _loadWithoutGameManager, value => _loadWithoutGameManager = value));
        
        // Manual input section for player/team data
        var separator = new HSeparator();
        container.AddChild(separator);
        
        container.AddChild(_createLabeledTextEdit("Player Data (JSON)", "Enter player data as JSON array", text => _playerDataJson = text));
        container.AddChild(_createLabeledTextEdit("Team Data (JSON)", "Enter team data as JSON array", text => _teamDataJson = text));

        var customHint = new Label
        {
            Text = "Custom state extension points are reserved here for future save-state inputs.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        container.AddChild(customHint);

        var buttonRow = new HBoxContainer();
        container.AddChild(buttonRow);

        buttonRow.AddChild(_createButton("Save", SaveCurrentScene));
        buttonRow.AddChild(_createButton("Load", LoadSavedScene));

        window.CloseRequested += window.QueueFree;
    }

    private static VBoxContainer _createLabeledLineEdit(string labelText, string placeholder, Action<string> onChanged)
    {
        var root = new VBoxContainer();

        root.AddChild(new Label { Text = labelText });

        var input = new LineEdit
        {
            PlaceholderText = placeholder,
            Text = placeholder
        };
        input.TextChanged += (newText) => onChanged(newText);
        root.AddChild(input);

        return root;
    }

    private static CheckBox _createToggle(string labelText, bool defaultValue, Action<bool> onToggled)
    {
        var check = new CheckBox
        {
            Text = labelText,
            ButtonPressed = defaultValue
        };
        check.Toggled += (bool toggle) => onToggled(toggle);
        return check;
    }

    private static VBoxContainer _createDropdown(string labelText, string[] items, int defaultIndex, Action<int> onSelected)
    {
        var root = new VBoxContainer();
        root.AddChild(new Label { Text = labelText });

        var option = new OptionButton();
        for (var i = 0; i < items.Length; i++)
        {
            option.AddItem(items[i], i);
        }

        option.Selected = Mathf.Clamp(defaultIndex, 0, Math.Max(0, items.Length - 1));
        option.ItemSelected += (itemIndex) => onSelected((int)itemIndex);
        root.AddChild(option);

        return root;
    }

    private static VBoxContainer _createLabeledTextEdit(string labelText, string placeholder, Action<string> onChanged)
    {
        var root = new VBoxContainer();

        root.AddChild(new Label { Text = labelText });

        var input = new TextEdit
        {
            PlaceholderText = placeholder,
            CustomMinimumSize = new Vector2(0, 80)
        };
        input.TextChanged += () => onChanged(input.Text);
        root.AddChild(input);

        return root;
    }

    private static Button _createButton(string text, Action onPressed)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(140, 36)
        };
        button.Pressed += onPressed;
        return button;
    }

    private void SaveCurrentScene()
    {
        var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
        if (sceneRoot == null)
        {
            GD.PrintErr("No edited scene root found.");
            return;
        }

        var board = ResolveBoard(sceneRoot);
        if (board == null)
        {
            GD.PrintErr("No Board found in the edited scene.");
            return;
        }

        var state = BuildGameStateData(sceneRoot, board);
        EnsureSaveDirectoryExists();

        var savePath = Path.Combine(Global.SavePath, _fileName);
        ResourceIO.Save(state, savePath);

        GD.Print($"Game state saved to '{savePath}'");
    }

    private void LoadSavedScene()
    {
        var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
        if (sceneRoot == null)
        {
            GD.PrintErr("No edited scene root found.");
            return;
        }

        var board = ResolveBoard(sceneRoot);
        if (board == null)
        {
            GD.PrintErr("No Board found in the edited scene.");
            return;
        }

        var entityLayer = ResolveEntityLayer(sceneRoot);
        if (entityLayer == null)
        {
            GD.PrintErr("EntityLayer not found in scene.");
            return;
        }

        if (!_loadWithoutGameManager && GameManager.Instance == null)
        {
            GD.PrintErr("GameManager instance is not available. Load should be triggered in a game context or enable 'Load Without GameManager'.");
            return;
        }

        SaveLoadManager.LoadGameState(
            _loadFileName,
            board,
            entityLayer);

        if (_reassignUnitOwnersAfterLoad)
        {
            foreach (var unit in board.GetAllUnits() ?? [])
            {
                unit.Owner = sceneRoot;
            }
        }

        GD.Print($"Game state loaded from '{_loadFileName}'");
    }

    private GameStateData BuildGameStateData(Node sceneRoot, Board board)
    {
        var state = new GameStateData();

        if (_includeBoardState)
        {
            state.BoardStateData = board.StateData;
        }

        if (_includeUnits)
        {
            var units = FindUnits(sceneRoot);
            foreach (var unit in units)
            {
                if (unit == null)
                    continue;

                unit.TileLocation = Board.WorldPositionToTilePosition(unit.Position);
                state.UnitStateResources.Add(UnitStateData.Create(unit));
            }
        }

        // Parse manual input for player/team data
        if (!string.IsNullOrEmpty(_playerDataJson))
        {
            try
            {
                var variant = Json.ParseString(_playerDataJson);
                if (variant.VariantType == Variant.Type.Array)
                {
                    var playerArray = variant.AsGodotArray();
                    state.PlayerData = new Array<PlayerData>();
                    foreach (var item in playerArray)
                    {
                        if (item.VariantType == Variant.Type.Dictionary)
                        {
                            var dict = item.AsGodotDictionary();
                            var playerData = new PlayerData
                            {
                                PlayerIdInt = dict.ContainsKey("PlayerIdInt") ? dict["PlayerIdInt"].AsInt32() : 0,
                                PlayerName = dict.ContainsKey("PlayerName") ? dict["PlayerName"].AsString() : "",
                                TeamIdInt = dict.ContainsKey("TeamIdInt") ? dict["TeamIdInt"].AsInt32() : 0,
                                ControllerType = dict.ContainsKey("ControllerType") ? (ControllerType)dict["ControllerType"].AsInt32() : ControllerType.PlayerInteractionController
                            };
                            state.PlayerData.Add(playerData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to parse PlayerData JSON:\n{ex.Message}");
            }
        }
        
        if (!string.IsNullOrEmpty(_teamDataJson))
        {
            try
            {
                var variant = Json.ParseString(_teamDataJson);
                if (variant.VariantType == Variant.Type.Array)
                {
                    var teamArray = variant.AsGodotArray();
                    state.TeamData = new Array<TeamData>();
                    foreach (var item in teamArray)
                    {
                        if (item.VariantType == Variant.Type.Dictionary)
                        {
                            var dict = item.AsGodotDictionary();
                            var teamData = new TeamData
                            {
                                TeamIdInt = dict.ContainsKey("TeamIdInt") ? dict["TeamIdInt"].AsInt32() : 0
                            };
                            state.TeamData.Add(teamData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to parse TeamData JSON:\n{ex.Message}");
            }
        }
        
        // Note: TeamRelations requires complex type conversion from Godot Dictionary to 
        // Dictionary<(Id<TeamIdTag>, Id<TeamIdTag>), TeamRelation>
        // This is not currently supported via manual JSON input

        return state;
    }

    private static Board? ResolveBoard(Node sceneRoot)
    {
        if (sceneRoot is Board board)
            return board;

        return sceneRoot.FindChild(DefaultSceneBoardNodeName, recursive: true) as Board;
    }

    private static Node? ResolveEntityLayer(Node sceneRoot)
    {
        return sceneRoot.FindChild(DefaultSceneEntityLayerNodeName, recursive: true);
    }

    private static Unit[] FindUnits(Node sceneRoot)
    {
        return sceneRoot.FindChildren("*", nameof(Unit)).Cast<Unit>().ToArray();
    }

    private static void EnsureSaveDirectoryExists()
    {
        if (!Directory.Exists(Global.SavePath))
        {
            Directory.CreateDirectory(Global.SavePath);
        }
    }

    private static string _normalizeSaveName(string fileName)
    {
        var trimmed = fileName.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return DefaultSaveName;

        if (!trimmed.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
        {
            trimmed += ".tres";
        }

        return trimmed;
    }
}