using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor customizado para CropData.
/// Nível indústria: Usa nameof para robustez e delega validação para o domínio.
/// </summary>
[CustomEditor(typeof(CropData))]
public class CropDataEditor : Editor
{
    #region Constants & Styles
    private static class Styles
    {
        public static readonly GUIStyle HeaderBox = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(0, 0, 5, 10)
        };

        public static readonly GUIStyle SectionHeader = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            margin = new RectOffset(0, 0, 10, 5)
        };

        public static readonly Color ColorValid = new Color(0.3f, 0.8f, 0.4f, 1f);
        public static readonly Color ColorWarning = new Color(1f, 0.7f, 0.2f, 1f);
        public static readonly Color ColorError = new Color(0.9f, 0.3f, 0.3f, 1f);
        public static readonly Color PreviewBgColor = new Color(0.15f, 0.15f, 0.15f, 1f);

        public const float SpritePreviewSize = 72f;
        public const float SpritePadding = 6f;
    }
    #endregion

    #region Serialized Properties
    private readonly Dictionary<string, SerializedProperty> _props = new Dictionary<string, SerializedProperty>();
    
    private static readonly string[] PropertyFieldNames = 
    {
        nameof(CropData.ID), 
        nameof(CropData.Name), 
        nameof(CropData.Description), 
        nameof(CropData.DaysToMature), 
        nameof(CropData.FreshnessWindow),
        nameof(CropData.BasePassiveScore), 
        nameof(CropData.MatureScoreMultiplier), 
        nameof(CropData.BaseSellValue),
        nameof(CropData.SeedSprite), 
        nameof(CropData.GrowthStages), 
        nameof(CropData.MatureSprite), 
        nameof(CropData.NearlyOverripeSprite),
        nameof(CropData.OverripeSprite), 
        nameof(CropData.WitheredSprite)
    };
    #endregion

    private CropData _targetCrop;
    private List<CropValidationIssue> _cachedIssues = new List<CropValidationIssue>();

    private void OnEnable()
    {
        _targetCrop = (CropData)target;
        CacheProperties();
        RefreshValidation();
    }

    private void CacheProperties()
    {
        _props.Clear();
        foreach (var name in PropertyFieldNames)
        {
            var prop = serializedObject.FindProperty(name);
            if (prop != null) _props[name] = prop;
            else Debug.LogWarning($"[CropDataEditor] Propriedade '{name}' não encontrada no CropData.");
        }
    }

    private void RefreshValidation()
    {
        if (_targetCrop != null)
        {
            _cachedIssues = _targetCrop.GetValidationIssues();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawEditorHeader();
        
        using (new EditorGUILayout.VerticalScope(Styles.HeaderBox))
        {
            DrawSection("Identidade", "Informações básicas e identificadores.", () => {
                EditorGUILayout.PropertyField(_props[nameof(CropData.ID)]);
                EditorGUILayout.PropertyField(_props[nameof(CropData.Name)]);
                EditorGUILayout.PropertyField(_props[nameof(CropData.Description)], GUILayout.Height(60));
            });

            DrawDivider();

            DrawSection("Ciclo de Vida", "Configurações de maturação e tempo.", () => {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_props[nameof(CropData.DaysToMature)]);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_targetCrop);
                }
                EditorGUILayout.PropertyField(_props[nameof(CropData.FreshnessWindow)]);
            });

            DrawDivider();

            DrawSection("Economia & Pontuação", "Valores base e multiplicadores.", () => {
                EditorGUILayout.PropertyField(_props[nameof(CropData.BasePassiveScore)]);
                EditorGUILayout.PropertyField(_props[nameof(CropData.MatureScoreMultiplier)]);
                EditorGUILayout.PropertyField(_props[nameof(CropData.BaseSellValue)]);
            });

            DrawDivider();

            DrawVisualsSection();
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            RefreshValidation();
        }
    }

    #region Layout Helpers
    private void DrawEditorHeader()
    {
        EditorGUILayout.Space(5);
        var id = _props.ContainsKey(nameof(CropData.ID)) ? GetPropertyStringValue(_props[nameof(CropData.ID)]) : "N/A";
        var name = _props.ContainsKey(nameof(CropData.Name)) ? GetPropertyStringValue(_props[nameof(CropData.Name)]) : "Nova Planta";
        
        EditorGUILayout.LabelField($"Planta: {name} [{id}]", EditorStyles.whiteLargeLabel);
        EditorGUILayout.Space(2);
    }

    private string GetPropertyStringValue(SerializedProperty prop)
    {
        if (prop == null) return "null";

        return prop.propertyType switch
        {
            SerializedPropertyType.String => prop.stringValue,
            SerializedPropertyType.Enum => prop.enumDisplayNames[prop.enumValueIndex],
            SerializedPropertyType.Integer => prop.intValue.ToString(),
            SerializedPropertyType.ObjectReference => prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "None",
            _ => prop.displayName
        };
    }

    private void DrawSection(string title, string subtitle, System.Action content)
    {
        EditorGUILayout.LabelField(title, Styles.SectionHeader);
        if (!string.IsNullOrEmpty(subtitle))
        {
            EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
            EditorGUILayout.Space(2);
        }
        
        content?.Invoke();
        EditorGUILayout.Space(5);
    }

    private void DrawDivider()
    {
        EditorGUILayout.Space(5);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.2f));
        EditorGUILayout.Space(10);
    }
    #endregion

    #region Visuals Section
    private void DrawVisualsSection()
    {
        DrawSection("Ativos Visuais", "Gerenciamento de sprites por estágio (Validado pelo Domínio).", () => {
            
            DrawValidationSummary();

            EditorGUILayout.Space(5);

            // Seed Stage
            DrawPropertyWithValidation(nameof(CropData.SeedSprite), "Semente (Dia 0)");

            // Growth Stages
            var growthStages = _props[nameof(CropData.GrowthStages)];
            int expectedGrowth = Mathf.Max(0, _targetCrop.DaysToMature - 1);
            
            EditorGUI.indentLevel++;
            for (int i = 0; i < expectedGrowth; i++)
            {
                if (i < growthStages.arraySize)
                {
                    var prop = growthStages.GetArrayElementAtIndex(i);
                    bool hasError = _cachedIssues.Any(issue => issue.PropertyName == nameof(CropData.GrowthStages) && issue.Message.Contains(i.ToString()));
                    DrawPropertyFieldWithIcon(prop, $"Crescimento {i} (Dia {i + 1})", hasError ? Styles.ColorError : (prop.objectReferenceValue != null ? Styles.ColorValid : Styles.ColorError));
                }
            }
            EditorGUI.indentLevel--;

            // Culmination
            DrawPropertyWithValidation(nameof(CropData.MatureSprite), $"Madura (Dia {_targetCrop.DaysToMature})");
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Estados Especiais", EditorStyles.miniBoldLabel);
            DrawPropertyWithValidation(nameof(CropData.NearlyOverripeSprite), "Quase Passada (NearlyOverripe)");
            DrawPropertyWithValidation(nameof(CropData.OverripeSprite), "Passada (Overripe)");
            DrawPropertyWithValidation(nameof(CropData.WitheredSprite), "Morta (Withered)");

            EditorGUILayout.Space(10);
            DrawSequencePreview(expectedGrowth);
        });
    }

    private void DrawPropertyWithValidation(string propertyName, string label)
    {
        var prop = _props[propertyName];
        var issue = _cachedIssues.FirstOrDefault(i => i.PropertyName == propertyName);
        
        Color indicatorColor = Styles.ColorValid;
        if (issue.Message != null)
        {
            indicatorColor = issue.Severity == CropValidationSeverity.Error ? Styles.ColorError : Styles.ColorWarning;
        }
        else if (prop.objectReferenceValue == null)
        {
            indicatorColor = Styles.ColorWarning;
        }

        DrawPropertyFieldWithIcon(prop, label, indicatorColor);
    }

    private void DrawPropertyFieldWithIcon(SerializedProperty prop, string label, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        var rect = EditorGUILayout.GetControlRect(GUILayout.Width(15));
        rect.y += 2;
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4, 14), color);
        
        EditorGUILayout.PropertyField(prop, new GUIContent(label));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawValidationSummary()
    {
        if (_cachedIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("✓ Todos os ativos e regras de domínio validados.", MessageType.Info);
            return;
        }

        var errors = _cachedIssues.Where(i => i.Severity == CropValidationSeverity.Error).ToList();
        var warnings = _cachedIssues.Where(i => i.Severity == CropValidationSeverity.Warning).ToList();

        if (errors.Count > 0)
        {
            EditorGUILayout.HelpBox($"{errors.Count} Erro(s) Crítico(s):\n" + string.Join("\n", errors.Select(e => "• " + e.Message)), MessageType.Error);
        }
        
        if (warnings.Count > 0)
        {
            EditorGUILayout.HelpBox($"{warnings.Count} Aviso(s):\n" + string.Join("\n", warnings.Select(w => "• " + w.Message)), MessageType.Warning);
        }
    }
    #endregion

    #region Preview Logic
    private void DrawSequencePreview(int expectedGrowth)
    {
        EditorGUILayout.LabelField("Preview da Progressão", EditorStyles.miniBoldLabel);
        
        int totalFrames = 1 + expectedGrowth + 1 + 3; 
        float areaWidth = EditorGUIUtility.currentViewWidth - 40;
        int cols = Mathf.FloorToInt(areaWidth / (Styles.SpritePreviewSize + Styles.SpritePadding));
        cols = Mathf.Max(1, cols);
        int rows = Mathf.CeilToInt((float)totalFrames / cols);

        Rect containerRect = GUILayoutUtility.GetRect(areaWidth, rows * (Styles.SpritePreviewSize + Styles.SpritePadding) + 10);
        EditorGUI.DrawRect(containerRect, Styles.PreviewBgColor);

        int currentIndex = 0;

        DrawSinglePreview(containerRect, _props[nameof(CropData.SeedSprite)], "Seed", currentIndex++, cols);

        var growthStages = _props[nameof(CropData.GrowthStages)];
        for (int i = 0; i < expectedGrowth; i++)
        {
            var prop = i < growthStages.arraySize ? growthStages.GetArrayElementAtIndex(i) : null;
            DrawSinglePreview(containerRect, prop, $"G{i}", currentIndex++, cols);
        }

        DrawSinglePreview(containerRect, _props[nameof(CropData.MatureSprite)], "Mature", currentIndex++, cols);
        DrawSinglePreview(containerRect, _props[nameof(CropData.NearlyOverripeSprite)], "NR-Over", currentIndex++, cols);
        DrawSinglePreview(containerRect, _props[nameof(CropData.OverripeSprite)], "Overripe", currentIndex++, cols);
        DrawSinglePreview(containerRect, _props[nameof(CropData.WitheredSprite)], "Withered", currentIndex++, cols);
    }

    private void DrawSinglePreview(Rect container, SerializedProperty prop, string label, int index, int cols)
    {
        int row = index / cols;
        int col = index % cols;

        Rect cellRect = new Rect(
            container.x + col * (Styles.SpritePreviewSize + Styles.SpritePadding) + Styles.SpritePadding,
            container.y + row * (Styles.SpritePreviewSize + Styles.SpritePadding) + Styles.SpritePadding,
            Styles.SpritePreviewSize,
            Styles.SpritePreviewSize
        );

        EditorGUI.DrawRect(cellRect, new Color(0, 0, 0, 0.2f));

        Sprite sprite = (prop?.objectReferenceValue as Sprite);
        if (sprite != null)
        {
            DrawSpriteInRect(cellRect, sprite);
        }
        else
        {
            GUI.Label(cellRect, "MISSING", new GUIStyle(EditorStyles.miniLabel) { 
                alignment = TextAnchor.MiddleCenter, 
                normal = { textColor = Styles.ColorError } 
            });
        }

        Rect labelRect = new Rect(cellRect.x, cellRect.yMax - 16, cellRect.width, 16);
        EditorGUI.DrawRect(labelRect, new Color(0,0,0,0.6f));
        GUI.Label(labelRect, label, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
    }

    private void DrawSpriteInRect(Rect rect, Sprite sprite)
    {
        if (sprite == null) return;
        Texture2D tex = sprite.texture;
        Rect sRect = sprite.textureRect;
        Rect texCoords = new Rect(sRect.x / tex.width, sRect.y / tex.height, sRect.width / tex.width, sRect.height / tex.height);
        GUI.DrawTextureWithTexCoords(rect, tex, texCoords, true);
    }
    #endregion
}