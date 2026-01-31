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

    [Header("Intera��o")]
    [SerializeField] private Button _selectionButton;
    [SerializeField] private GameObject _highlightBorder;

    // Estado Interno (State)
    private bool _isSelected;
    private IPurchasable _data; 

    // Evento Simplificado: A View avisa "Fui clicada", o Pai decide o que fazer.
    private Action<ShopItemView> _onClickCallback;

    // Propriedade para o pai ler o dado de volta sem passar no evento
    public IPurchasable Data => _data;

    private void Awake()
    {
        // SEGURAN�A DE EVENTOS:
        // Adicionamos o listener apenas UMA vez no ciclo de vida.
        // Nunca usamos RemoveAllListeners, pois designers podem ter colocado sons no Inspector.
        if (_selectionButton)
        {
            _selectionButton.onClick.AddListener(HandleClick);
        }
        else
        {
            Debug.LogError($"[ShopItemView] Bot�o de sele��o n�o atribu�do no objeto {gameObject.name}");
        }
    }

    private void OnDestroy()
    {
        if (_selectionButton) _selectionButton.onClick.RemoveListener(HandleClick);
    }

    public void Setup(IPurchasable item, Action<ShopItemView> onSelected)
    {
        // 1. GUARD CLAUSES (Valida��o Defensiva)
        if (item == null)
        {
            Debug.LogError("[ShopItemView] Setup recebeu item nulo! Ignorando.");
            gameObject.SetActive(false); // Esconde para n�o mostrar lixo na tela
            return;
        }

        if (onSelected == null)
        {
            Debug.LogWarning("[ShopItemView] Setup recebeu callback nulo. O item n�o ser� clic�vel.");
        }

        // 2. Atualiza��o de Dados
        _data = item;
        _onClickCallback = onSelected;

        // 3. Atualiza��o Visual (Data Binding)
        // Aqui extra�mos os dados primitivos (string/int) imediatamente
        if (_nameText) _nameText.text = item.DisplayName;

        if (_priceText)
        {
            var shop = AppCore.Instance?.Services?.Shop;
            int finalPrice = shop?.GetFinalPrice(item) ?? item.Price;
            _priceText.text = $"${finalPrice}";
        }

        if (_iconImage)
        {
            _iconImage.sprite = item.Icon;
            _iconImage.enabled = item.Icon != null;
        }

        // 4. Reset de Estado
        // For�a atualiza��o visual inicial garantindo que comece desmarcado
        _isSelected = true; // Hack l�gico para for�ar o SetSelected(false) a rodar visualmente
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        // OTIMIZA��O DE ESTADO:
        // Se j� estiver no estado desejado, n�o faz nada. Evita redraw desnecess�rio.
        if (_isSelected == isSelected) return;

        _isSelected = isSelected;

        if (_highlightBorder)
            _highlightBorder.SetActive(_isSelected);

        // Futuro: Tocar anima��o ou som de sele��o aqui
    }

    private void HandleClick()
    {
        // Se n�o tiver dados (clique antes do setup), ignora
        if (_data == null) return;

        // Avisa quem estiver ouvindo (ShopView)
        _onClickCallback?.Invoke(this);
    }
}