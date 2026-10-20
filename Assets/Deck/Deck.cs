using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Infinite-deck Deck (tutorial build).
/// - DrawCard picks from `cards` with replacement — the pool never depletes.
/// - The visible deck sprite is cosmetic. When you add the "card flies from
///   deck sprite to hand" animation, do it in the DrawCard path (or a tween
///   spawned right after Instantiate).
/// - The discard pile is also cosmetic. ReturnCard() is the hook — call your
///   "card flies to discard sprite" tween from there.
/// </summary>
public class Deck : MonoBehaviour
{
    [Header("Ownership")]
    public int ownerId = Team.Player;

    [Header("Cards (drag CardData assets here)")]
    public List<CardData> cards = new List<CardData>();

    [Header("Spawning")]
    public GameObject cardPrefab;
    public Transform spawnPoint;

    [Header("Hand Routing (optional)")]
    public Hand targetHand;

    [Header("Debug")]
    public bool debugLogs = false;

    /// <summary>Discard hook. Currently a no-op — the deck is infinite.
    /// Future: play the "fly to discard pile" animation here.</summary>
    public void ReturnCard(CardData data)
    {
        if (debugLogs && data != null)
            Debug.Log($"[Deck] Discarded '{data.cardName}' (infinite deck — no data change).");
    }

    public Card DrawCard()
    {
        if (cardPrefab == null)
        {
            Debug.LogError($"[Deck] No cardPrefab assigned on {gameObject.name}.", this);
            return null;
        }

        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning($"[Deck] No CardData in deck on {gameObject.name}.", this);
            return null;
        }

        CardData picked = cards[Random.Range(0, cards.Count)];

        if (picked == null)
        {
            Debug.LogWarning($"[Deck] Picked a null CardData on {gameObject.name}. Check the cards list in the Inspector.", this);
            return null;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;

        if (debugLogs)
            Debug.Log($"[Deck] Drawing '{picked.cardName}' at {pos} (owner {ownerId}).");

        GameObject go = Instantiate(cardPrefab, pos, Quaternion.identity);
        Card card = go.GetComponent<Card>();
        if (card == null)
        {
            Debug.LogError("[Deck] cardPrefab has no Card component!", this);
            Destroy(go);
            return null;
        }

        card.Apply(picked);
        card.ownerId = ownerId;
        card.sourceDeck = this;

        if (targetHand != null && !targetHand.AddCard(card))
        {
            if (debugLogs) Debug.Log("[Deck] Hand full — draw burned.");
            Destroy(go);
            return null;
        }

        return card;
    }

    // Backward-compat shim. Nothing needs to "reset" anymore — the pool never depletes.
    public void ResetDeck() { }

    void OnValidate()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null)
                Debug.LogWarning($"[Deck] Empty slot at index {i} in Cards list on {gameObject.name}.", this);
        }
    }
}