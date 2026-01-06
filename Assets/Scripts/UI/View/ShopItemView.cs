using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private TextMeshProUGUI _descText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private CanvasGroup _canvasGroup;

    private IPurchasable _item;

    // Chamado pela ShopView para preencher os dados
    public void Setup(IPurchasable item)
    {
        _item = item;

        // Preenche visual
        if (_nameText) _nameText.text = item.DisplayName;
        if (_descText) _descText.text = item.Description;
        if (_priceText) _priceText.text = $"${item.Price}";

        if (_iconImage)
        {
            _iconImage.sprite = item.Icon;
            _iconImage.enabled = item.Icon != null;
        }

        // Configura o clique
        _buyButton.onClick.RemoveAllListeners();
        _buyButton.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        AppCore.Instance.ShopService.TryPurchase(_item);
    }
}