#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LastFurrow.Traditions.Editor
{
    [CustomPropertyDrawer(typeof(TraditionID))]
    public class TraditionIDDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Busca o campo de string dentro do struct
            SerializedProperty valueProp = property.FindPropertyRelative("_value");

            if (valueProp != null)
            {
                // Desenha como um campo de texto normal
                EditorGUI.BeginChangeCheck();
                
                string current = valueProp.stringValue;
                string next = EditorGUI.TextField(position, label, current);
                
                if (EditorGUI.EndChangeCheck())
                {
                    // Força maiúsculo e underscore (regra de ID)
                    valueProp.stringValue = next?.ToUpperInvariant().Replace(" ", "_") ?? string.Empty;
                }
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Error: _value not found in TraditionID");
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
