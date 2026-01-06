using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class ShopView : UIView
{
    [Header("Estrutura")]
    [SerializeField] private Transform _itemsContainer;
    [SerializeField] private ShopItemView _itemPrefab;
    [SerializeField] private TextMeshProUGUI _shopTitleText;
    [SerializeField] private Button _closeButton;

    // --- ESTADO & CACHE ---
    // Cache do serviço para evitar chamar AppCore.Instance toda hora
    private ShopService _shopService;

    // Object Pooling: Lista de itens criados (ativos e inativos)
    private List<ShopItemView> _itemPool = new List<ShopItemView>();

    // Evento para avisar o mundo externo que queremos sair
    public event Action OnExitRequested;

    protected override void Awake()
    {
        base.Awake();

        // Fail Fast: Validação de referências obrigatórias
        if (_itemPrefab == null) Debug.LogError("[ShopView] Prefab do item não atribuído!");
        if (_itemsContainer == null) Debug.LogError("[ShopView] Container não atribuído!");

        if (_closeButton)
            _closeButton.onClick.AddListener(HandleCloseClick);
    }

    private void Start()
    {
        // Cache seguro das dependências
        if (AppCore.Instance != null)
        {
            _shopService = AppCore.Instance.ShopService;
        }
    }

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.ShopService.OnStockRefreshed += RefreshUI;
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.ShopService.OnStockRefreshed -= RefreshUI;
        }
    }

    public override void Show()
    {
        base.Show();
        // Garante que o serviço esteja cacheado se o Start ainda não rodou (caso raro de Race Condition)
        if (_shopService == null && AppCore.Instance != null)
            _shopService = AppCore.Instance.ShopService;

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_shopService == null) return;

        // 1. Limpeza via Pooling (Desativar em vez de Destruir)
        foreach (var item in _itemPool)
        {
            item.gameObject.SetActive(false);
        }

        // 2. Atualiza Título
        if (_shopTitleText)
            _shopTitleText.text = _shopService.CurrentShopTitle;

        // 3. Popula Itens
        var stock = _shopService.CurrentStock;
        if (stock != null)
        {
            for (int i = 0; i < stock.Count; i++)
            {
                // Pega ou Cria (Get from Pool)
                ShopItemView itemView = GetItemView(i);

                // Configura
                itemView.Setup(stock[i]);
                itemView.gameObject.SetActive(true);
            }
        }
    }

    // Lógica simples de Pool: Reusa existentes, cria novos se faltar
    private ShopItemView GetItemView(int index)
    {
        if (index < _itemPool.Count)
        {
            return _itemPool[index];
        }
        else
        {
            var newItem = Instantiate(_itemPrefab, _itemsContainer);
            _itemPool.Add(newItem);
            return newItem;
        }
    }

    private void HandleCloseClick()
    {
        // Desacoplamento: A View apenas avisa "Quero sair".
        // Quem decide o que acontece (avançar semana, fechar janela) é o UIManager ou RunManager.
        OnExitRequested?.Invoke();
    }
}