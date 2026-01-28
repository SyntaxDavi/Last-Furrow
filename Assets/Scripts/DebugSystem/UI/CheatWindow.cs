using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System;

public class CheatWindow
{
    private readonly Dictionary<string, ICheatCommand> _commands;
    private readonly VisualElement _root;
    private readonly TextField _searchField;
    private readonly ScrollView _content;
    private readonly Label _statusLabel;
    
    private bool _isVisible;

    public CheatWindow(Dictionary<string, ICheatCommand> commands, VisualElement root)
    {
        _commands = commands;
        
        if (root == null)
        {
            Debug.LogError("[CheatWindow] Root VisualElement é nulo! O UIDocument pode estar sem PanelSettings.");
            return;
        }

        _root = root.Q<VisualElement>("cheat-container");
        if (_root == null)
        {
            Debug.LogWarning("[CheatWindow] Container 'cheat-container' não encontrado no UIDocument.");
            return;
        }

        _searchField = _root.Q<TextField>("search-field");
        _content = _root.Q<ScrollView>("commands-scroll");
        _statusLabel = _root.Q<Label>("status-label");

        _searchField.RegisterValueChangedCallback(evt => FilterCommands(evt.newValue));
        
        BuildCommandList();
        SetVisibility(false);
    }

    public void Toggle()
    {
        _isVisible = !_isVisible;
        SetVisibility(_isVisible);
        if (_isVisible) _searchField.Focus();
    }

    private void SetVisibility(bool visible)
    {
        if (_root == null) return;
        _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BuildCommandList()
    {
        if (_content == null) return;
        _content.Clear();

        var groups = _commands.Values.GroupBy(c => c.Category).OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            var foldout = new Foldout { text = group.Key, value = true };
            foldout.AddToClassList("category-foldout");

            foreach (var command in group)
            {
                foldout.Add(CreateCommandRow(command));
            }

            _content.Add(foldout);
        }
    }

    private VisualElement CreateCommandRow(ICheatCommand command)
    {
        var row = new VisualElement();
        row.AddToClassList("cheat-row");
        row.name = $"row-{command.Id}";

        var btn = new Button(() => ExecuteCommand(command, row)) { text = command.Id };
        btn.AddToClassList("cheat-btn");
        btn.tooltip = command.Description;

        var argsField = new TextField { name = "args-input" };
        argsField.AddToClassList("args-field");
        // argsField.placeholderText = "args..."; // Not supported in this Unity version

        row.Add(btn);
        row.Add(argsField);
        return row;
    }

    private void ExecuteCommand(ICheatCommand command, VisualElement row)
    {
        var argsInput = row.Q<TextField>("args-input").value ?? "";
        var args = argsInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (CheatManager.Instance.ExecuteCommand(command.Id, args, out string feedback))
        {
            ShowStatus(feedback, Color.green);
        }
        else
        {
            ShowStatus(feedback, Color.red);
        }
    }

    private void ShowStatus(string message, Color color)
    {
        if (_statusLabel == null) return;
        _statusLabel.text = message;
        _statusLabel.style.color = color;
    }

    private void FilterCommands(string term)
    {
        term = term.ToLower();
        var rows = _root.Query<VisualElement>(className: "cheat-row").ToList();
        
        foreach (var row in rows)
        {
            bool match = row.name.Contains(term);
            row.style.display = match ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        // Ocultar foldouts vazios
        var foldouts = _root.Query<Foldout>().ToList();
        foreach (var foldout in foldouts)
        {
            bool hasVisibleChildren = foldout.Query<VisualElement>(className: "cheat-row")
                .Where(r => r.style.display == DisplayStyle.Flex).ToList().Any();
            foldout.style.display = hasVisibleChildren ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}