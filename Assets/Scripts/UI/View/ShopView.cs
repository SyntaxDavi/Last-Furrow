using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Herda de UIView para ter o Fade In/Out automático
public class ShopView : UIView
{
    [Header("Estrutura")]
    [SerializeField] private Transform _itemsContainer; // Onde os itens vão aparecer (Content do Grid Layout)
    [SerializeField] private ShopItemView _itemPrefab;  // O prefab do passo 2
    [SerializeField] private TextMeshProUGUI _shopTitleText;
    [SerializeField] private Button _closeButton;       // Botão "Ir Trabalhar"

    private List<GameObject> _spawnedItems = new List<GameObject>();
        
    protected override void Awake()
    {
        base.Awake();

        if (_closeButton)
            _closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            // Ouve atualizações do estoque
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

    // Sobrescreve o Show() para garantir que a UI atualize ao abrir
    public override void Show()
    {
        base.Show();
        RefreshUI();
    }

    private void RefreshUI()
    {
        // 1. Limpa itens antigos
        foreach (var item in _spawnedItems) Destroy(item);
        _spawnedItems.Clear();

        // 2. Atualiza Título
        if (_shopTitleText)
            _shopTitleText.text = AppCore.Instance.ShopService.CurrentShopTitle;

        // 3. Cria novos itens baseado no Estoque Real
        var stock = AppCore.Instance.ShopService.CurrentStock;

        if (stock != null)
        {
            foreach (var item in stock)
            {
                ShopItemView newItem = Instantiate(_itemPrefab, _itemsContainer);
                newItem.Setup(item);
                _spawnedItems.Add(newItem.gameObject);
            }
        }
    }

    private void OnCloseClicked()
    {
        // Chama o RunManager para avançar para a próxima semana
        // (Como você fez no CheatManager ou RunManager.StartNextWeek)
        var run = AppCore.Instance.SaveManager.Data.CurrentRun;
        AppCore.Instance.RunManager.StartNextWeek(run);
    }
}