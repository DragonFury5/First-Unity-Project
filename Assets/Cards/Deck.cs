using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    [Header("Ownership")]
    [Tooltip("Team that owns cards drawn from this deck. Use Team.Player / Team.Enemy / Team.Neutral.")]
    public int ownerId = Team.Player;

    [Header("Cards")]
    public List<CardData> cards = new List<CardData>();

    [Header("Spawning")]
    public GameObject cardPrefab;
    public Transform spawnPoint;

    [Header("Debug")]
    public bool debugLogs = false;

    private List<CardData> remaining;

    void Start()
    {
        ResetDeck();
    }

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
        return card;
    }

    void OnMouseDown()
    {
        // Only allow manual draw in Setup / Recover (matches Draggable's phase gate).
        if (!CanDrawInCurrentPhase()) return;

        Card drawn = DrawCard();
        if (drawn == null) return;

        Draggable d = drawn.GetComponent<Draggable>();
        if (d != null)
        {
            d.autoBeginDrag = true;
            d.BeginDrag();
        }
    }

    private bool CanDrawInCurrentPhase()
    {
        if (PhaseManager.Instance == null) return true;

        GamePhase phase = PhaseManager.Instance.currentPhase;
        return phase == GamePhase.Setup || phase == GamePhase.Recover;
    }
}