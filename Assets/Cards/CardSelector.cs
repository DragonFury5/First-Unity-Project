using System.Collections;
using UnityEngine;

public class CardSelector : MonoBehaviour
{
    public static CardSelector Instance { get; private set; }

    [Header("Selection Visuals")]
    public float selectScaleMultiplier = 1.3f;
    public float flySpeed = 15f;
    public float attackOffset = 0.8f;

    [Header("Diagnostics")]
    [Tooltip("Enable to see click / selection / reject logs in the Console.")]
    public bool debugLogs = true;

    private Card selectedCard;
    private Vector3 originalScale;
    private bool isAttacking;

    void Awake()
    {
        Instance = this;

        // Defensive: clamp bad inspector values back to sane defaults.
        if (selectScaleMultiplier <= 1.01f) selectScaleMultiplier = 1.3f;
        if (flySpeed <= 0f) flySpeed = 15f;
    }

    void OnEnable()  { PhaseManager.OnPhaseChanged += HandlePhaseChanged; }
    void OnDisable() { PhaseManager.OnPhaseChanged -= HandlePhaseChanged; }

    void Update()
    {
        if (PhaseManager.Instance == null) return;
        if (PhaseManager.Instance.currentPhase != GamePhase.Battle) return;
        if (isAttacking) return;

        if (Input.GetMouseButtonDown(0)) HandleClick();
    }

    private void HandleClick()
    {
        if (Camera.main == null)
        {
            if (debugLogs) Debug.LogWarning("[CardSelector] Camera.main is null — is your camera tagged 'MainCamera'?");
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);

        Card clickedCard = GetCardAtPoint(mousePos2D);

        if (clickedCard == null)
        {
            if (debugLogs) Debug.Log("[CardSelector] Clicked empty space — deselecting.");
            DeselectCard();
            return;
        }

        if (debugLogs)
            Debug.Log($"[CardSelector] Clicked '{clickedCard.name}' (owner={clickedCard.ownerId}, support={clickedCard.IsSupport}, exhausted={clickedCard.exhausted}).");

        if (clickedCard.ownerId == Team.Player)
        {
            SelectPlayerCard(clickedCard);
        }
        else if (clickedCard.ownerId == Team.Enemy)
        {
            if (selectedCard == null)
            {
                if (debugLogs) Debug.Log("[CardSelector] Clicked enemy with nothing selected — no-op.");
                return;
            }

            if (!CanPlayerAttack(selectedCard, clickedCard)) return;
            StartCoroutine(AttackRoutine(selectedCard, clickedCard));
        }
    }

    private Card GetCardAtPoint(Vector2 point)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(point);
        if (hits == null || hits.Length == 0) return null;

        Card closest = null;
        float closestSqrDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Card c = hits[i].GetComponentInParent<Card>();
            if (c == null) continue;

            float sqrDist = ((Vector2)c.transform.position - point).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = c;
            }
        }

        return closest;
    }

    private void SelectPlayerCard(Card card)
    {
        if (card == null) return;

        if (card.IsSupport)
        {
            if (debugLogs) Debug.Log($"[CardSelector] '{card.name}' is a support — not selectable.");
            return;
        }

        if (card.exhausted)
        {
            if (debugLogs) Debug.Log($"[CardSelector] '{card.name}' is exhausted — not selectable.");
            return;
        }

        Draggable d = card.GetComponent<Draggable>();
        if (d != null && d.IsInHand)
        {
            if (debugLogs) Debug.Log($"[CardSelector] '{card.name}' is in hand — not selectable.");
            return;
        }

        if (selectedCard == card) return;

        if (selectedCard != null) DeselectCard();

        selectedCard = card;
        originalScale = card.transform.localScale;

        card.transform.localScale = originalScale * selectScaleMultiplier;

        if (debugLogs)
            Debug.Log($"[CardSelector] Selected '{card.name}'. Scale {originalScale} → {card.transform.localScale} (x{selectScaleMultiplier}).");
    }

    public void DeselectCard()
    {
        if (selectedCard == null) return;
        selectedCard.transform.localScale = originalScale;
        if (debugLogs) Debug.Log($"[CardSelector] Deselected '{selectedCard.name}'.");
        selectedCard = null;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.Battle)
            DeselectCard();
    }

    private bool CanPlayerAttack(Card attacker, Card target)
    {
        if (attacker == null || attacker.exhausted)
        {
            if (debugLogs) Debug.Log("[CardSelector] Attacker missing or exhausted.");
            return false;
        }

        if (attacker.IsSupport)
        {
            if (debugLogs) Debug.Log($"[CardSelector] '{attacker.name}' is a support — can't attack.");
            return false;
        }

        if (target == null || target.IsSupport)
        {
            if (debugLogs) Debug.Log("[CardSelector] Target is null or a support — can't be attacked.");
            return false;
        }

        if (EnergyManager.Instance != null && !EnergyManager.Instance.CanAfford(Team.Player, attacker.Cost))
        {
            if (debugLogs) Debug.Log("[CardSelector] Not enough energy.");
            return false;
        }

        return true;
    }

    private IEnumerator AttackRoutine(Card attacker, Card target)
    {
        if (attacker == null || target == null) { DeselectCard(); yield break; }

        int cost = attacker.Cost;
        if (EnergyManager.Instance != null && !EnergyManager.Instance.TrySpend(Team.Player, cost))
        {
            if (debugLogs) Debug.Log("[CardSelector] Attack aborted — TrySpend failed.");
            DeselectCard();
            yield break;
        }
        attacker.SetExhausted(true);

        isAttacking = true;
        yield return PlayAttackAnimation(attacker, target);
        DeselectCard();
        isAttacking = false;
    }

    public IEnumerator PlayAttackAnimation(Card attacker, Card target)
    {
        if (attacker == null || target == null) yield break;

        Vector3 startPos = attacker.transform.position;
        Vector3 targetPos = target.transform.position + (Vector3.down * attackOffset);
        targetPos.z = startPos.z - 0.5f;

        while (attacker != null &&
               Vector3.Distance(attacker.transform.position, targetPos) > 0.05f)
        {
            attacker.transform.position = Vector3.MoveTowards(
                attacker.transform.position, targetPos, flySpeed * Time.deltaTime);
            yield return null;
        }

        if (attacker == null) yield break;
        attacker.transform.position = targetPos;

        if (target != null)
        {
            int dmg = attacker.EffectiveDamage;
            if (EnemyAI.Instance != null)
                EnemyAI.Instance.ShowDamagePopUp(target.transform.position, dmg);

            target.TakeDamage(dmg);
        }

        yield return new WaitForSeconds(0.25f);

        if (attacker != null)
        {
            while (Vector3.Distance(attacker.transform.position, startPos) > 0.05f)
            {
                attacker.transform.position = Vector3.MoveTowards(
                    attacker.transform.position, startPos, flySpeed * Time.deltaTime);
                yield return null;
            }
            attacker.transform.position = startPos;
        }
    }
}