using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;

/// <summary>
/// Orchestrador do sistema de cheats refatorado.
/// Responsável por descobrir comandos via Reflection e gerenciar a UI.
/// </summary>
public class CheatManager : MonoBehaviour
{
    private static CheatManager _instance;
    public static CheatManager Instance => _instance;

    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset _visualTreeAsset;
    [SerializeField] private PanelSettings _panelSettings;

    [Header("Settings")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;

    private Dictionary<string, ICheatCommand> _commands = new();
    private CheatWindow _window;
    private bool _isInitialized;

    private void Awake()
    {
        #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Destroy(gameObject);
            return;
        #endif

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        DiscoverCommands();
        _isInitialized = true;
    }

    private void Start()
    {
        CreateUI();
    }

    private void DiscoverCommands()
    {
        _commands.Clear();
        
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ICheatCommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<CheatAttribute>();
            if (attr == null) continue;

            try
            {
                var instance = (ICheatCommand)Activator.CreateInstance(type);
                _commands[instance.Id] = instance;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CheatManager] Erro ao instanciar comando {type.Name}: {e.Message}");
            }
        }

        Debug.Log($"[CheatManager] {_commands.Count} comandos registrados.");
    }

    private void CreateUI()
    {
        if (_visualTreeAsset == null)
        {
            Debug.LogWarning("[CheatManager] VisualTreeAsset não atribuído! A UI não será criada.");
            return;
        }

        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) uiDoc = gameObject.AddComponent<UIDocument>();
        
        uiDoc.visualTreeAsset = _visualTreeAsset;
        if (_panelSettings != null) uiDoc.panelSettings = _panelSettings;

        _window = new CheatWindow(_commands, uiDoc.rootVisualElement);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _window?.Toggle();
        }
    }

    public bool ExecuteCommand(string id, string[] args, out string feedback)
    {
        if (!_commands.TryGetValue(id, out var command))
        {
            feedback = $"❌ Comando não encontrado: {id}";
            return false;
        }

        if (!command.ValidateArgs(args, out string error))
        {
            feedback = $"❌ {error}";
            return false;
        }

        return command.Execute(args, out feedback);
    }

    public IEnumerable<ICheatCommand> GetAllCommands() => _commands.Values;
}
