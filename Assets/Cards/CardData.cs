using UnityEngine;

public enum CardType
{
    Unit,
    Support
}

[System.Serializable]
public class CardData
{
    public string cardName = "New Card";
    public CardType type = CardType.Unit;
    public Sprite sprite;
    public Color tint = Color.white;

    [Header("Combat & Stats")]
    public int maxHealth = 10;
    public int damage = 2;
    public int shield = 0;
    public int cost = 1;
}