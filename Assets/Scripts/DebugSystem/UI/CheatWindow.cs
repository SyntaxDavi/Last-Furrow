// CheatWindow.cs
public class CheatWindow
{
    private readonly Dictionary<string, ICheatCommand> _commands;
    private readonly VisualElement _root;
    private readonly TextField _searchField;
    private readonly ScrollView _categories;
    
    public event Action<string> OnCommandExecuted;
    
    public CheatWindow(Dictionary<string, ICheatCommand> commands)
    {
        _commands = commands;
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Cheats/UI/CheatWindow.uxml");
        _root = visualTree.CloneTree();
        
        _searchField = _root.Q<TextField>("search");
        _searchField.RegisterValueChangedCallback(OnSearchChanged);
        
        _categories = _root.Q<ScrollView>("categories");
        BuildCategories();
    }
    
    void BuildCategories()
    {
        var grouped = _commands.Values.GroupBy(c => c.Category);
        
        foreach (var group in grouped)
        {
            var categoryFoldout = new Foldout { text = group.Key, value = true };
            categoryFoldout.AddToClassList("category-foldout");
            
            foreach (var command in group)
            {
                var row = CreateCheatRow(command);
                categoryFoldout.Add(row);
            }
            
            _categories.Add(categoryFoldout);
        }
    }
    
    VisualElement CreateCheatRow(ICheatCommand cmd)
    {
        var row = new VisualElement();
        row.AddToClassList("cheat-row");
        
        // Botão principal
        var btn = new Button(() => ExecuteCommand(cmd)) { text = cmd.Id };
        btn.AddToClassList("cheat-btn");
        
        // Tooltip na hover
        btn.tooltip = cmd.Description;
        
        // Campo de args (se necessário)
        var argsField = new TextField { name = "args" };
        argsField.AddToClassList("args-field");
        
        row.Add(btn);
        row.Add(argsField);
        return row;
    }
    
    void ExecuteCommand(ICheatCommand cmd)
    {
        // Busca argumentos do campo associado
        var row = /* encontrar row */;
        var argsInput = row.Q<TextField>("args").value;
        var args = argsInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (!cmd.ValidateArgs(args, out string error))
        {
            ShowFeedback($"{error}", Color.red);
            return;
        }
        
        bool success = cmd.Execute(args, out string feedback);
        ShowFeedback(feedback, success ? Color.green : Color.red);
        OnCommandExecuted?.Invoke(cmd.Id);
    }
    
    void OnSearchChanged(ChangeEvent<string> evt)
    {
        var term = evt.newValue.ToLower();
        // Filtra visibilidade das categorias/chers baseado no termo
    }
}