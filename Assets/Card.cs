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

    [Header("Deck Origin")]
    public Deck sourceDeck;
    [HideInInspector] public bool returnedToDeck;

    [Header("Buffs")]
    public int temporaryDamageBonus = 0;
    public int temporaryDamagePenalty = 0;

    public bool IsDead => currentHealth <= 0;
    public int Cost => data != null ? data.cost : 0;
    public bool IsSupport => data != null && data.type == CardType.Support;
    public int EffectiveDamage => Mathf.Max(0, damage + temporaryDamageBonus - temporaryDamagePenalty);

    public event Action<Card> OnDeath;

    public void Apply(CardData newData)
    {
        if (newData == null) return;

        data = newData;
        damage = newData.damage;
        currentHealth = newData.maxHealth;
        currentShield = newData.shield;
        exhausted = false;
        temporaryDamageBonus = 0;
        temporaryDamagePenalty = 0;
        returnedToDeck = false;

        gameObject.name = "Card_" + newData.cardName;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (newData.sprite != null) sr.sprite = newData.sprite;
            else Debug.LogWarning("CardData " + newData.cardName + " has NO SPRITE assigned!");

            Color c = newData.tint;
            if (c.a <= 0.01f) c.a = 1f;
            sr.color = c;
        }
        else
        {
            Debug.LogError("CARD PREFAB IS MISSING A SPRITE RENDERER!");
        }
    }

    // Idempotent — safe to call from anywhere.
    public void ReturnToDeck()
    {
        if (returnedToDeck) return;
        returnedToDeck = true;
        if (sourceDeck != null && data != null)
            sourceDeck.ReturnCard(data);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead || IsSupport) return;

        int absorbed = Mathf.Min(currentShield, amount);
        currentShield -= absorbed;
        currentHealth -= (amount - absorbed);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead || IsSupport || data == null) return;
        currentHealth = Mathf.Min(currentHealth + amount, data.maxHealth);
    }

    public void AddShield(int amount)
    {
        if (amount <= 0 || IsDead || IsSupport) return;
        currentShield += amount;
    }

    public void SetExhausted(bool value)
    {
        if (exhausted == value) return;
        exhausted = value;
        RefreshVisual();
    }

    public void ClearTemporaryBuffs()
    {
        temporaryDamageBonus = 0;
        temporaryDamagePenalty = 0;
    }

    private void Die()
    {
        ReturnToDeck();          // synchronous, before anyone listens
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        ReturnToDeck();          // fallback for direct Destroy() calls
    }

    private void RefreshVisual()
{
    SpriteRenderer sr = GetComponent<SpriteRenderer>();
    if (sr == null) return;

    Color c = (data != null) ? data.tint : Color.white;
    if (c.a <= 0.01f) c.a = 1f;

    // Only the player needs to see which of their cards are exhausted.
    // Enemy exhaustion is AI-internal state and shouldn't be surfaced.
    if (exhausted && ownerId == Team.Player)
    {
        c.r *= 0.5f;
        c.g *= 0.5f;
        c.b *= 0.5f;
    }

    sr.color = c;
}
}