using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Factories/Restore Life Item")]
public class RestoreLifeFactorySO : ShopItemFactorySO
{
    [Header("Configuração do Item")]
    [SerializeField] private string _itemName = "Chá Revigorante";
    [SerializeField][TextArea] private string _description = "Recupera 1 Coração.";
    [SerializeField] private Sprite _icon; // Agora podemos definir ícone no asset

    [Header("Efeitos")]
    [SerializeField][Min(1)] private int _healAmount = 1;
    [SerializeField] private int _price = 50;

    public override IPurchasable CreateItem(RunData context)
    {
        // Precisamos atualizar o RestoreLifeItem para aceitar esses parâmetros
        return new RestoreLifeItem(_itemName, _description, _icon, _healAmount, _price);
    }
}