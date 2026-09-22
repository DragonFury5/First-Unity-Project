using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Card : MonoBehaviour
{
    public CardData data;
    public int ownerId = 0; // 0 = Player, 1 = Enemy
    public int currentHealth;
    public int currentShield;

    public bool IsDead => currentHealth <= 0;

    public void Apply(CardData newData)
    {
        data = newData;
        currentHealth = newData.maxHealth;
        currentShield = newData.shield;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (newData.sprite != null)
            sr.sprite = newData.sprite;

        Color c = newData.tint;
        if (c.a <= 0.01f) c.a = 1f;
        sr.color = c;

        gameObject.name = "Card_" + newData.cardName;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead) return;

        int absorbed = Mathf.Min(currentShield, amount);
        currentShield -= absorbed;

        int leftover = amount - absorbed;
        currentHealth -= leftover;

        if (currentHealth <= 0)
            Destroy(gameObject);
    }
}