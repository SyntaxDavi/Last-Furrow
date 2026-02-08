using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor para CardDropData.
/// Apresenta os campos de forma organizada com validação visual.
/// 
/// FEATURES:
/// - Seções colapsáveis com ícones
/// - Campos inativos (valor 0) aparecem em cinza
/// - Campos ativos aparecem destacados
/// - Preview das regras ativas
/// </summary>
[CustomEditor(typeof(CardDropData))]
public class CardDropDataEditor : Editor
{
    // Serialized Properties
    private SerializedProperty _cardID;
    private SerializedProperty _weight;
    private SerializedProperty _rarity;
    private SerializedProperty _maxCopiesInDeck;
    private SerializedProperty _maxPerDraw;
    private SerializedProperty _guaranteeAfterDays;
    private SerializedProperty _guaranteePriority;
    private SerializedProperty _tags;

    // Foldout states
    private static bool _showIdentification = true;
    private static bool _showDeckBalance = true;
    private static bool _showDrawRules = true;
    private static bool _showCategorization = false;

    // Styles
    private GUIStyle _headerStyle;
    private GUIStyle _activeRuleStyle;
    private GUIStyle _inactiveRuleStyle;
    private GUIStyle _summaryBoxStyle;

    private void OnEnable()
    {
        _cardID = serializedObject.FindProperty("CardID");
        _weight = serializedObject.FindProperty("Weight");
        _rarity = serializedObject.FindProperty("Rarity");
        _maxCopiesInDeck = serializedObject.FindProperty("MaxCopiesInDeck");
        _maxPerDraw = serializedObject.FindProperty("MaxPerDraw");
        _guaranteeAfterDays = serializedObject.FindProperty("GuaranteeAfterDays");
        _guaranteePriority = serializedObject.FindProperty("GuaranteePriority");
        _tags = serializedObject.FindProperty("Tags");
    }

    private void InitStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
        }

        if (_activeRuleStyle == null)
        {
            _activeRuleStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
        }

        if (_inactiveRuleStyle == null)
        {
            _inactiveRuleStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
        }

        if (_summaryBoxStyle == null)
        {
            _summaryBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 8, 8),
                fontSize = 11,
                richText = true
            };
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InitStyles();

        var data = (CardDropData)target;

        // ===== HEADER COM NOME =====
        DrawHeader(data);

        EditorGUILayout.Space(8);

        // ===== SEÇÃO: IDENTIFICAÇÃO =====
        _showIdentification = EditorGUILayout.BeginFoldoutHeaderGroup(_showIdentification, "🏷️ Identificação");
        if (_showIdentification)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_cardID);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ===== SEÇÃO: BALANCEAMENTO DE DECK =====
        _showDeckBalance = EditorGUILayout.BeginFoldoutHeaderGroup(_showDeckBalance, "⚖️ Balanceamento de Deck");
        if (_showDeckBalance)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_weight, new GUIContent("Peso", "Maior peso = mais cópias no deck"));
            EditorGUILayout.PropertyField(_rarity);
            EditorGUILayout.PropertyField(_maxCopiesInDeck, new GUIContent("Máx. no Deck", "Limite de cópias desta carta no deck completo"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ===== SEÇÃO: REGRAS DE DRAW =====
        _showDrawRules = EditorGUILayout.BeginFoldoutHeaderGroup(_showDrawRules, "🎴 Regras de Draw Diário");
        if (_showDrawRules)
        {
            EditorGUI.indentLevel++;
            
            // Max Per Draw
            DrawRuleField(
                _maxPerDraw, 
                "Máx. por Draw", 
                "Limite desta carta por draw diário. 0 = usa default (2).",
                _maxPerDraw.intValue > 0
            );

            EditorGUILayout.Space(2);

            // Guarantee After Days
            DrawRuleField(
                _guaranteeAfterDays, 
                "Garantir Após (dias)", 
                "Garante esta carta se não aparecer por X dias. 0 = desativado.",
                _guaranteeAfterDays.intValue > 0
            );

            // Priority (só mostra se garantia está ativa)
            if (_guaranteeAfterDays.intValue > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_guaranteePriority, new GUIContent("↳ Prioridade", "Menor = garante primeiro"));
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            // Summary Box
            DrawRulesSummary(data);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ===== SEÇÃO: CATEGORIZAÇÃO =====
        _showCategorization = EditorGUILayout.BeginFoldoutHeaderGroup(_showCategorization, "🏷️ Categorização");
        if (_showCategorization)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_tags);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader(CardDropData data)
    {
        EditorGUILayout.BeginHorizontal();
        
        // Ícone baseado na raridade
        string icon = data.Rarity switch
        {
            CardRarity.Common => "📦",
            CardRarity.Uncommon => "🌟",
            CardRarity.Rare => "💎",     
            CardRarity.Legendary => "👑",
            _ => "📦"
        };

        var headerContent = $"{icon} {(data.CardID.IsValid ? data.CardID.Value : "Sem ID")}";
        EditorGUILayout.LabelField(headerContent, EditorStyles.boldLabel, GUILayout.Height(24));
        
        // Badge de raridade
        var rarityColor = data.Rarity switch
        {
            CardRarity.Common => new Color(0.7f, 0.7f, 0.7f),
            CardRarity.Uncommon => new Color(0.3f, 0.8f, 0.3f),
            CardRarity.Rare => new Color(0.3f, 0.5f, 1f),
            CardRarity.Legendary => new Color(1f, 0.7f, 0.25f),
            _ => Color.white
        };

        var prevColor = GUI.backgroundColor;
        GUI.backgroundColor = rarityColor;
        GUILayout.Label(data.Rarity.ToString(), EditorStyles.miniButton, GUILayout.Width(80));
        GUI.backgroundColor = prevColor;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRuleField(SerializedProperty property, string label, string tooltip, bool isActive)
    {
        var prevColor = GUI.color;
        
        if (!isActive)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
        }

        EditorGUILayout.BeginHorizontal();
        
        // Indicador visual
        var statusIcon = isActive ? "✓" : "○";
        var statusColor = isActive ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
        
        var prevContentColor = GUI.contentColor;
        GUI.contentColor = statusColor;
        EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));
        GUI.contentColor = prevContentColor;

        EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
        
        EditorGUILayout.EndHorizontal();

        GUI.color = prevColor;
    }

    private void DrawRulesSummary(CardDropData data)
    {
        bool hasAnyRule = data.HasCustomMaxPerDraw || data.HasGuarantee;
        
        if (!hasAnyRule)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("Nenhuma regra customizada. Usando defaults globais.", MessageType.None);
            return;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical(_summaryBoxStyle);
        
        EditorGUILayout.LabelField("📋 Regras Ativas:", EditorStyles.boldLabel);
        
        if (data.HasCustomMaxPerDraw)
        {
            EditorGUILayout.LabelField($"  • Máximo {data.MaxPerDraw} desta carta por draw");
        }
        
        if (data.HasGuarantee)
        {
            EditorGUILayout.LabelField($"  • Garantida após {data.GuaranteeAfterDays} dias sem aparecer");
            EditorGUILayout.LabelField($"    (Prioridade: {data.GuaranteePriority})");
        }

        EditorGUILayout.EndVertical();
    }
}
