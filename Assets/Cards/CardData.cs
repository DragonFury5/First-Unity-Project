using UnityEngine;

public enum CardType
{
    Unit,
    Support
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Card Game/Card Data", order = 0)]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardName = "New Card";
    public CardType type = CardType.Unit;
    public Sprite sprite;
    public Color tint = Color.white;

    [Header("Combat & Stats")]
    public int maxHealth = 10;
    public int damage = 2;
    public int shield = 0;
    public int cost = 1;

    [Header("Flavor")]
    [TextArea(2, 4)] public string description = "";
}