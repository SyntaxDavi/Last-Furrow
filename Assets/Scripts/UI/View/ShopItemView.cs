using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopItemView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _priceText;

    [Header("Interação")]
    [SerializeField] private Button _selectionButton; // O botão que cobre o card inteiro
    [SerializeField] private GameObject _highlightBorder; // Objeto visual de seleção (borda/brilho)

    private IPurchasable _item;
    private Action<ShopItemView, IPurchasable> _onSelectedCallback;

    public void Setup(IPurchasable item, Action<ShopItemView, IPurchasable> onSelected)
    {
        _item = item;
        _onSelectedCallback = onSelected;

        // Visual
        if (_nameText) _nameText.text = item.DisplayName;
        if (_priceText) _priceText.text = $"${item.Price}";

        if (_iconImage)
        {
            _iconImage.sprite = item.Icon;
            _iconImage.enabled = item.Icon != null;
        }

        // Configura Seleção
        if (_selectionButton)
        {
            _selectionButton.onClick.RemoveAllListeners();
            _selectionButton.onClick.AddListener(HandleClick);
        }

        // Reseta estado visual
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (_highlightBorder) _highlightBorder.SetActive(isSelected);
    }

    private void HandleClick()
    {
        // Avisa a ShopView: "Fui clicado! Eu sou este item."
        _onSelectedCallback?.Invoke(this, _item);
    }
}