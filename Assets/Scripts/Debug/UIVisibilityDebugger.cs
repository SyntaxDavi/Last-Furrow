using UnityEngine;

/// <summary>
/// DEBUG TOOL: Mostra informações detalhadas sobre visibilidade de UI.
/// 
/// INSTALAÇÃO:
/// 1. Adicione este script em HeartContainer
/// 2. Play
/// 3. Pressione F2 para ver relatório completo
/// </summary>
public class UIVisibilityDebugger : MonoBehaviour
{
    [Header("Hotkey")]
    [SerializeField] private KeyCode _debugKey = KeyCode.F2;

    private void Update()
    {
        if (Input.GetKeyDown(_debugKey))
        {
            GenerateReport();
        }
    }

    private void GenerateReport()
    {
        Debug.Log("========== UI VISIBILITY REPORT ==========");
        
        // 1. Informações do próprio GameObject
        Debug.Log($"?? GameObject: {gameObject.name}");
        Debug.Log($"   Active: {gameObject.activeInHierarchy} (self: {gameObject.activeSelf})");
        
        // 2. Transform
        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            Debug.Log($"   Position: {rt.anchoredPosition}");
            Debug.Log($"   LocalPosition: {rt.localPosition}");
            Debug.Log($"   Size: {rt.sizeDelta}");
            Debug.Log($"   Scale: {rt.localScale}");
            Debug.Log($"   Pivot: {rt.pivot}");
            Debug.Log($"   Anchors: Min({rt.anchorMin}) Max({rt.anchorMax})");
        }
        
        // 3. Canvas
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"   Canvas: {canvas.name}");
            Debug.Log($"   Canvas RenderMode: {canvas.renderMode}");
            Debug.Log($"   Canvas Enabled: {canvas.enabled}");
            Debug.Log($"   Canvas SortingOrder: {canvas.sortingOrder}");
        }
        else
        {
            Debug.LogError("   ? PROBLEMA: Nenhum Canvas encontrado na hierarquia!");
        }
        
        // 4. CanvasGroup
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log($"   CanvasGroup Alpha: {canvasGroup.alpha}");
            Debug.Log($"   CanvasGroup BlocksRaycasts: {canvasGroup.blocksRaycasts}");
            Debug.Log($"   CanvasGroup Interactable: {canvasGroup.interactable}");
        }
        
        // 5. Filhos (corações)
        Debug.Log($"?? Filhos ({transform.childCount}):");
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            Debug.Log($"   [{i}] {child.name} - Active: {child.gameObject.activeInHierarchy}");
            
            var childRt = child.GetComponent<RectTransform>();
            if (childRt != null)
            {
                Debug.Log($"       Position: {childRt.anchoredPosition}");
                Debug.Log($"       Scale: {childRt.localScale}");
            }
            
            var childCG = child.GetComponent<CanvasGroup>();
            if (childCG != null)
            {
                Debug.Log($"       CanvasGroup Alpha: {childCG.alpha}");
            }
            
            var image = child.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                Debug.Log($"       Image Enabled: {image.enabled}");
                Debug.Log($"       Image Color: {image.color}");
                Debug.Log($"       Image Sprite: {(image.sprite != null ? image.sprite.name : "NULL")}");
            }
        }
        
        Debug.Log("========================================");
    }
}
