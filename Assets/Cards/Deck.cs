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
    [Tooltip("If set, drawn cards are auto-placed into this hand's first empty slot.")]
    public Hand targetHand;

    [Header("Debug")]
    public bool debugLogs = false;

    private List<CardData> remaining;

    void Start() { ResetDeck(); }

    public void ResetDeck()
    {
        remaining = new List<CardData>(cards);
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
            Debug.LogWarning($"[Deck] No CardData assigned on {gameObject.name}.", this);
            return null;
        }

        int index = Random.Range(0, remaining.Count);
        CardData picked = remaining[index];
        remaining.RemoveAt(index);

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
            // Hand full — burn the draw (destroy it). Change this if you want different behavior.
            if (debugLogs) Debug.Log("[Deck] Hand full — draw burned.");
            Destroy(go);
            return null;
        }

        return card;
    }
}