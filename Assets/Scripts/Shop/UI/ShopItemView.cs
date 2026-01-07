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
        // SEGURANÇA DE EVENTOS:
        // Adicionamos o listener apenas UMA vez no ciclo de vida.
        // Nunca usamos RemoveAllListeners, pois designers podem ter colocado sons no Inspector.
        if (_selectionButton)
        {
            _selectionButton.onClick.AddListener(HandleClick);
        }
        else
        {
            Debug.LogError($"[ShopItemView] Botão de seleção não atribuído no objeto {gameObject.name}");
        }
    }

    private void OnDestroy()
    {
        if (_selectionButton) _selectionButton.onClick.RemoveListener(HandleClick);
    }

    public void Setup(IPurchasable item, Action<ShopItemView> onSelected)
    {
        // 1. GUARD CLAUSES (Validação Defensiva)
        if (item == null)
        {
            Debug.LogError("[ShopItemView] Setup recebeu item nulo! Ignorando.");
            gameObject.SetActive(false); // Esconde para não mostrar lixo na tela
            return;
        }

        if (onSelected == null)
        {
            Debug.LogWarning("[ShopItemView] Setup recebeu callback nulo. O item não será clicável.");
        }

        // 2. Atualização de Dados
        _data = item;
        _onClickCallback = onSelected;

        // 3. Atualização Visual (Data Binding)
        // Aqui extraímos os dados primitivos (string/int) imediatamente
        if (_nameText) _nameText.text = item.DisplayName;
        if (_priceText) _priceText.text = $"${item.Price}";

        if (_iconImage)
        {
            _iconImage.sprite = item.Icon;
            _iconImage.enabled = item.Icon != null;
        }

        // 4. Reset de Estado
        // Força atualização visual inicial garantindo que comece desmarcado
        _isSelected = true; // Hack lógico para forçar o SetSelected(false) a rodar visualmente
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        // OTIMIZAÇÃO DE ESTADO:
        // Se já estiver no estado desejado, não faz nada. Evita redraw desnecessário.
        if (_isSelected == isSelected) return;

        _isSelected = isSelected;

        if (_highlightBorder)
            _highlightBorder.SetActive(_isSelected);

        // Futuro: Tocar animação ou som de seleção aqui
    }

    private void HandleClick()
    {
        // Se não tiver dados (clique antes do setup), ignora
        if (_data == null) return;

        // Avisa quem estiver ouvindo (ShopView)
        _onClickCallback?.Invoke(this);
    }
}