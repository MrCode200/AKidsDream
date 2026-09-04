using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AKidsDream.Common.Generators;
using Godot;

namespace AKidsDream.Util.Identifiers.EditorTools;

public enum ClassOptions
{
    Custom,
    TweenAnimationData
}

[GlobalClass]
[Tool]
public partial class GenerateStaticFieldClassETool : EditorScript
{
    public override void _Run()
    {
        _CreateDialogue();
    }

private void _CreateDialogue()
{
    var window = new Window();
    window.Title = "Generate Static Field Class";
    window.Unresizable = false;
    window.MinSize = new Vector2I(265, 370);
    
    EditorInterface.Singleton.PopupDialog(window, new Rect2I(new Vector2I(100, 100), window.MinSize));
    var viewport3D = EditorInterface.Singleton.GetEditorViewport3D();
    
    var vbox = new VBoxContainer();
    vbox.AddChild(new Label { Text = "Class Options:" });
    
    var classOptions = new OptionButton();
    classOptions.AddItem("Custom", (int)ClassOptions.Custom);
    classOptions.AddItem("TweenAnimationData", (int)ClassOptions.TweenAnimationData);
    classOptions.Selected = 0;
    vbox.AddChild(classOptions);
    
    vbox.AddChild(new Label { Text = "Target Resource:" });
    var resourcePicker = new EditorResourcePicker { BaseType = "Resource" };
    vbox.AddChild(resourcePicker);
    
    vbox.AddChild(new Label { Text = "Field Holder:" });
    var fieldHolderLineEdit = new LineEdit();
    vbox.AddChild(fieldHolderLineEdit);
    
    vbox.AddChild(new Label { Text = "Usings (comma separated, optional):" });
    var usingsLineEdit = new LineEdit { PlaceholderText = "e.g. System, Godot" };
    vbox.AddChild(usingsLineEdit);
    
    Label outputDirLabel = new Label { Text = "Unset OutputDir!" };
    outputDirLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
    outputDirLabel.CustomMaximumSize = window.MinSize;
    vbox.AddChild(outputDirLabel);
    
    var outputDir = "";
    var outputDirPicker = new EditorFileDialog();
    outputDirPicker.FileMode = FileDialog.FileModeEnum.OpenDir;
    outputDirPicker.DirSelected += dir =>
    {
        outputDir = dir;
        outputDirLabel.Text = outputDir;
    };
    viewport3D.AddChild(outputDirPicker);
    
    var outputDirButton = new Button { Text = "Select Output Directory..." };
    outputDirButton.Pressed += () => outputDirPicker.PopupCentered();
    vbox.AddChild(outputDirButton);

    var generateButton = new Button { Text = "Generate" };
    vbox.AddChild(generateButton);
    
    window.AddChild(vbox);
    
    // Handle preset selection
    classOptions.ItemSelected += index =>
    {
        var selectedIndex = (int)index;
        var selectedOption = (ClassOptions)classOptions.GetItemId(selectedIndex);
        if (selectedOption == ClassOptions.TweenAnimationData)
        {
            resourcePicker.BaseType = "TweenAnimationData";
            fieldHolderLineEdit.Text = "Identifiers";
            fieldHolderLineEdit.Editable = false;
            usingsLineEdit.Text = "System, Godot";
            usingsLineEdit.Editable = false;
            outputDir = "res://Common/Components/TweenComponent/Resources";
            outputDirLabel.Text = outputDir;
        }
        else
        {
            resourcePicker.BaseType = "Resource";
            fieldHolderLineEdit.Editable = true;
            fieldHolderLineEdit.Text = "";
        }
    };
    
    generateButton.Pressed += () =>
    {
        var fieldHolder = fieldHolderLineEdit.Text;
        
        if (string.IsNullOrEmpty(fieldHolder))
        {
            OS.Alert("Field Holder cannot be empty", "Error");
            return;
        }
        
        var targetResource = resourcePicker.EditedResource;
        if (targetResource == null)
        {
            OS.Alert("No resource selected", "Error");
            return;
        }
        
        var usingsText = usingsLineEdit.Text;
        var usings = string.IsNullOrEmpty(usingsText)
            ? []
            : usingsText.Split(',', StringSplitOptions.TrimEntries);
        
        if (string.IsNullOrEmpty(outputDir))
        {
            OS.Alert("Output directory cannot be empty", "Error");
            return;
        }
        
        _GenerateStaticFieldClass(targetResource, fieldHolder, usings, outputDir);
    };
    
    window.CloseRequested += window.QueueFree;
    window.PopupCentered();
}
private static void _GenerateStaticFieldClass(Resource target, string fieldHolder, string[] usings, string outputDir)
{
    var field = target.GetType().GetField(fieldHolder, BindingFlags.Public | BindingFlags.Static);
    var fieldHolderValue = field?.GetValue(null); // null for static fields

    if (fieldHolderValue == null)
    {
        OS.Alert($"Failed to get {fieldHolder} value", "Error");
        return;
    }

    if (fieldHolderValue is not IEnumerable<object> fieldHolderEnumerable)
    {
        OS.Alert($"{fieldHolder} is not an IEnumerable", "Error");
        return;
    }

    var fieldHolderList = fieldHolderEnumerable.ToList();
    if (fieldHolderList.Count == 0)
    {
        OS.Alert($"{fieldHolder} is empty", "Info");
        return;
    }

    StaticFieldClassGenerator.Generate(
        outputDir,
        $"{target.GetType().Name}{fieldHolder}",
        fieldHolderList.Select(name => (name.ToString(), name)),
        GetFieldValueLiteral,
        usings
    );
    
    OS.Alert($"Generated {fieldHolder} class", "Success");
}

    private static string GetFieldValueLiteral(object value)
    {
        switch (value)
        {
            case string:
                return $"\"{value}\"";
            case int or float or bool:
                return value.ToString();
            case StringName:
                return $"new StringName(\"{value}\")";
            default:
                throw new ArgumentException($"Unsupported type: {value.GetType()}");
        }
    }
}