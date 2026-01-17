using UnityEngine;
using UnityEditor;
using System.IO;

public class ProjectDocumentationWindow : EditorWindow
{
    private Vector2 _scrollPosition;
    private string _markdownContent;
    private const string DOC_PATH = "Assets/Documentation/PROJECT_ARCHITECTURE.md";

    [MenuItem("Tools/Project Documentation")]
    public static void ShowWindow()
    {
        GetWindow<ProjectDocumentationWindow>("Project Architecture");
    }

    private void OnEnable()
    {
        LoadDocumentation();
    }

    private void LoadDocumentation()
    {
        if (File.Exists(DOC_PATH))
        {
            _markdownContent = File.ReadAllText(DOC_PATH);
        }
        else
        {
            _markdownContent = "Documentation file not found at " + DOC_PATH;
        }
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Reload Documentation"))
        {
            LoadDocumentation();
        }

        EditorGUILayout.Space();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        
        // Use a style that supports rich text and wrapping
        GUIStyle textStyle = new GUIStyle(EditorStyles.textArea);
        textStyle.wordWrap = true;
        textStyle.richText = true;
        
        // Simple Markdown-ish parsing for better display (optional but nice)
        // For now, we just display the raw text which reads well enough.
        EditorGUILayout.TextArea(_markdownContent, textStyle, GUILayout.ExpandHeight(true));

        EditorGUILayout.EndScrollView();
    }
}
