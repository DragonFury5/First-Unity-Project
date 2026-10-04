using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Card : MonoBehaviour
{
    [Header("Runtime State")]
    public CardData data;
    public int ownerId = Team.Neutral;
    public int damage;
    public int currentHealth;
    public int currentShield;
    public bool exhausted;

    public bool IsDead => currentHealth <= 0;
    public int Cost => data != null ? data.cost : 0;

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
        exhausted = false;

        gameObject.name = "Card_" + newData.cardName;
        RefreshVisual();
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

    public void SetExhausted(bool value)
    {
        if (exhausted == value) return;
        exhausted = value;
        RefreshVisual();
    }

    /// <summary>Hook called during Recover phase. Both teams receive this call.
    /// Support card effects will hook here later.</summary>
    public void OnRecoverPhase()
    {
        // No mechanical effect yet.
    }

    private void Die()
    {
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }

    private void RefreshVisual()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Color c = (data != null) ? data.tint : Color.white;
        if (c.a <= 0.01f) c.a = 1f;

        if (exhausted)
        {
            c.r *= 0.5f;
            c.g *= 0.5f;
            c.b *= 0.5f;
        }

        sr.color = c;
    }
}