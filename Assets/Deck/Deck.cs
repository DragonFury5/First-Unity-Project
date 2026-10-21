using System.Collections;
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

    [Header("Draw Animation (Setup)")]
    [Tooltip("How long each card takes to fly from deck to hand slot.")]
    public float flightDuration = 0.2f;

    [Tooltip("Delay between each card's flight start. Lower = faster deal.")]
    public float flightStagger = 0.08f;

    [Tooltip("Visual offset per stacked card, so the '5 cards on deck' moment is visible.")]
    public float stackOffsetPerCard = 0.03f;

    [Header("Debug")]
    public bool debugLogs = false;

    public void ReturnCard(CardData data)
    {
        if (debugLogs && data != null)
            Debug.Log($"[Deck] Discarded '{data.cardName}' (infinite deck — no data change).");
    }

    // --- Non-animated draw (single card, used by any future non-animated path) ---
    public Card DrawCard()
    {
        if (cardPrefab == null)
        {
            Debug.LogError($"[Deck] No cardPrefab assigned on {gameObject.name}.", this);
            return null;
        }

        CardData picked = PickRandomCard();
        if (picked == null) return null;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Card card = SpawnCard(picked, pos);
        if (card == null) return null;

        if (targetHand != null && !targetHand.AddCard(card))
        {
            if (debugLogs) Debug.Log("[Deck] Hand full — draw burned.");
            Destroy(card.gameObject);
            return null;
        }

        return card;
    }

    // --- Animated batch draw (used by PhaseManager Setup) ---
    /// <summary>
    /// Draws `count` cards. All appear stacked on the deck instantly,
    /// then fly to their hand slots one-by-one with `flightStagger` spacing.
    /// Blocks phase advance via PhaseManager.isTransitionLocked while playing.
    /// </summary>
    public void DrawCardsAnimated(int count, Hand hand)
    {
        if (hand == null) hand = targetHand;
        if (hand == null)
        {
            Debug.LogWarning("[Deck] DrawCardsAnimated: no hand assigned.", this);
            return;
        }
        StartCoroutine(DrawCardsAnimatedRoutine(count, hand));
    }

    private IEnumerator DrawCardsAnimatedRoutine(int count, Hand hand)
    {
        List<FlightPlan> plans = new List<FlightPlan>();

        // --- Phase 1: instantiate all cards at the deck position, stacked ---
        Vector3 baseDeckPos = spawnPoint != null ? spawnPoint.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            HandSlot slot = hand.FindFirstEmptySlot();
            if (slot == null) break;

            CardData picked = PickRandomCard();
            if (picked == null) break;

            Card card = SpawnCard(picked, baseDeckPos);
            if (card == null) continue;

            // Tiny per-card offset so the stack reads as multiple cards
            Vector3 stackOffset = new Vector3(i * stackOffsetPerCard, i * stackOffsetPerCard, 0f);
            card.transform.position = baseDeckPos + stackOffset;

            Draggable d = card.GetComponent<Draggable>();
            if (d != null) d.IsFlying = true;

            slot.Occupy(d);   // reserve slot immediately so next FindFirstEmptySlot skips it

            plans.Add(new FlightPlan
            {
                card = card,
                slot = slot,
                draggable = d,
                from = card.transform.position,
                to = slot.AnchorPosition
            });
        }

        if (plans.Count == 0)
        {
            if (PhaseManager.Instance != null)
                PhaseManager.Instance.isTransitionLocked = false;
            yield break;
        }

        // --- Phase 2: stagger + flight ---
        try
        {
            Coroutine last = null;
            for (int i = 0; i < plans.Count; i++)
            {
                last = StartCoroutine(FlyCardRoutine(plans[i]));
                if (i < plans.Count - 1)
                    yield return new WaitForSeconds(flightStagger);
            }
            if (last != null) yield return last;
        }
        finally
        {
            if (PhaseManager.Instance != null)
                PhaseManager.Instance.isTransitionLocked = false;
        }
    }

    private IEnumerator FlyCardRoutine(FlightPlan plan)
    {
        if (plan.card == null) yield break;

        Vector3 from = plan.from;
        Vector3 to = plan.to;
        float duration = Mathf.Max(0.01f, flightDuration);
        float t = 0f;

        while (t < duration)
        {
            if (plan.card == null) yield break;
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - (1f - k) * (1f - k);   // ease-out quad
            plan.card.transform.position = Vector3.Lerp(from, to, eased);
            yield return null;
        }

        if (plan.card != null)
            plan.card.transform.position = to;

        if (plan.draggable != null)
        {
            plan.draggable.SetHome(to, plan.slot);
            plan.draggable.IsFlying = false;
        }
    }

    private CardData PickRandomCard()
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning($"[Deck] No CardData in deck on {gameObject.name}.", this);
            return null;
        }
        CardData picked = cards[Random.Range(0, cards.Count)];
        if (picked == null)
            Debug.LogWarning("[Deck] Picked a null CardData. Check the cards list.", this);
        return picked;
    }

    private Card SpawnCard(CardData picked, Vector3 position)
    {
        if (cardPrefab == null || picked == null) return null;

        GameObject go = Instantiate(cardPrefab, position, Quaternion.identity);
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
        return card;
    }

    // Backward-compat shim (infinite deck — nothing to reset)
    public void ResetDeck() { }

    void OnValidate()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null)
                Debug.LogWarning($"[Deck] Empty slot at index {i} in Cards list on {gameObject.name}.", this);
        }
    }

    private class FlightPlan
    {
        public Card card;
        public HandSlot slot;
        public Draggable draggable;
        public Vector3 from;
        public Vector3 to;
    }
}