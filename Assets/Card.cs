using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Card : MonoBehaviour
{
    [Header("Runtime State")]
    public CardData data;
    public int ownerId = Team.Neutral;   // Use Team.Player / Team.Enemy / Team.Neutral
    public int damage;
    public int currentHealth;
    public int currentShield;

    public bool IsDead => currentHealth <= 0;

    /// <summary>Fired just before this card is destroyed. Listen for cleanup / effects.</summary>
    public event Action<Card> OnDeath;

    public void Apply(CardData newData)
    {
        if (newData == null)
        {
            Debug.LogWarning("[Card] Apply called with null CardData.", this);
            return;
        }

        data = newData;
        damage = newData.damage;
        currentHealth = newData.maxHealth;
        currentShield = newData.shield;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (newData.sprite != null) sr.sprite = newData.sprite;

            Color c = newData.tint;
            if (c.a <= 0.01f) c.a = 1f;   // Guard against accidentally invisible cards
            sr.color = c;
        }

        gameObject.name = "Card_" + newData.cardName;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead) return;

        int absorbed = Mathf.Min(currentShield, amount);
        currentShield -= absorbed;
        currentHealth -= (amount - absorbed);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }
}