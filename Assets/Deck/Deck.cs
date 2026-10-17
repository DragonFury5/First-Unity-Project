using System.Collections.Generic;
using UnityEngine;

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

    private List<CardData> remaining;

    void Start() { ResetDeck(); }

    public void ResetDeck()
    {
        remaining = new List<CardData>();

        // Filter out any null/invalid entries so DrawCard can trust the list.
        foreach (var c in cards)
        {
            if (c == null)
            {
                Debug.LogWarning($"[Deck] Null entry in cards list on {gameObject.name}. Skipping.", this);
                continue;
            }
            remaining.Add(c);
        }
    }

    public Card DrawCard()
    {
        if (cardPrefab == null)
        {
            Debug.LogError($"[Deck] No cardPrefab assigned on {gameObject.name}.", this);
            return null;
        }

        if (remaining == null || remaining.Count == 0) ResetDeck();

        if (remaining == null || remaining.Count == 0)
        {
            Debug.LogWarning($"[Deck] No valid CardData in deck on {gameObject.name}.", this);
            return null;
        }

        int index = Random.Range(0, remaining.Count);
        CardData picked = remaining[index];
        remaining.RemoveAt(index);

        if (picked == null)
        {
            // Belt and suspenders — should never happen after ResetDeck filtering.
            Debug.LogWarning("[Deck] Picked a null CardData. Skipping this draw.", this);
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

        if (targetHand != null && !targetHand.AddCard(card))
        {
            if (debugLogs) Debug.Log("[Deck] Hand full — draw burned.");
            Destroy(go);
            return null;
        }

        return card;
    }

    // Warn in the Inspector if the list has blanks, so you catch it before playing.
    void OnValidate()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null)
                Debug.LogWarning($"[Deck] Empty slot at index {i} in Cards list on {gameObject.name}.", this);
        }
    }
}