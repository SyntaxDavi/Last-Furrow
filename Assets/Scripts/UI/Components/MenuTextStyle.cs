using UnityEngine;
using DG.Tweening;

namespace LastFurrow.UI.Components
{
    /// <summary>
    /// ScriptableObject defining visual style for animated menu items.
    /// Centralizes all appearance and timing configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "MenuTextStyle", menuName = "Last Furrow/UI/Menu Text Style")]
    public class MenuTextStyle : ScriptableObject
    {
        [Header("Colors")]
        [Tooltip("Text color when not selected")]
        public Color NormalColor = Color.white;
        
        [Tooltip("Text color when selected/hovered")]
        public Color SelectedColor = new Color(1f, 0.85f, 0.4f); // Gold
        
        [Tooltip("Text color when disabled")]
        public Color DisabledColor = new Color(0.5f, 0.5f, 0.5f);
        
        [Header("Scale")]
        [Tooltip("Scale when not selected")]
        public float NormalScale = 1f;
        
        [Tooltip("Scale when selected")]
        public float SelectedScale = 1.15f;
        
        [Tooltip("Scale during click pulse")]
        public float ClickScale = 1.25f;
        
        [Header("Animation Timing")]
        [Tooltip("Duration of selection transition")]
        public float SelectDuration = 0.15f;
        
        [Tooltip("Duration of click pulse")]
        public float ClickDuration = 0.1f;
        
        [Tooltip("Easing for selection animation")]
        public Ease SelectEase = Ease.OutQuad;
        
        [Tooltip("Easing for click pulse")]
        public Ease ClickEase = Ease.OutBack;
        
        [Header("Selection Arrow")]
        [Tooltip("Horizontal offset from text anchor")]
        public float ArrowOffsetX = -40f;
        
        [Tooltip("Duration of arrow move animation")]
        public float ArrowMoveDuration = 0.15f;
        
        [Tooltip("Arrow bob amplitude (breathing effect)")]
        public float ArrowBobAmount = 3f;
        
        [Tooltip("Arrow bob cycle duration")]
        public float ArrowBobDuration = 1.2f;
        
        [Header("Audio")]
        [Tooltip("SFX when navigating between items")]
        public AudioClip NavigateSFX;
        
        [Tooltip("SFX when confirming selection")]
        public AudioClip ConfirmSFX;
        
        [Tooltip("Volume for menu SFX")]
        [Range(0f, 1f)]
        public float SFXVolume = 0.5f;
    }
}
