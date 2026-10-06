using System.Collections;
using UnityEngine;

public class CardSelector : MonoBehaviour
{
    public static CardSelector Instance { get; private set; }

    [Header("Selection Visuals")]
    public float selectScaleMultiplier = 1.3f;
    public float flySpeed = 15f;
    public float attackOffset = 0.8f;

    private Card selectedCard;
    private Vector3 originalScale;
    private bool isAttacking;

    void Awake() { Instance = this; }

    void Update()
    {
        if (PhaseManager.Instance == null) return;
        if (PhaseManager.Instance.currentPhase != GamePhase.Battle) return;
        if (isAttacking) return;

        if (Input.GetMouseButtonDown(0)) HandleClick();
    }

    private void HandleClick()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);

        Card clickedCard = GetCardAtPoint(mousePos2D);

        if (clickedCard == null) { DeselectCard(); return; }

        if (clickedCard.ownerId == Team.Player)
        {
            SelectPlayerCard(clickedCard);
        }
        else if (clickedCard.ownerId == Team.Enemy && selectedCard != null)
        {
            if (!CanPlayerAttack(selectedCard)) return;
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
        if (card.exhausted) return;

        // Cannot select cards sitting in hand.
        Draggable d = card.GetComponent<Draggable>();
        if (d != null && d.IsInHand) return;

        if (selectedCard == card) return;

        if (selectedCard != null) DeselectCard();

        selectedCard = card;
        originalScale = card.transform.localScale;

        float multiplier = selectScaleMultiplier > 0f ? selectScaleMultiplier : 1.3f;
        card.transform.localScale = originalScale * multiplier;
    }

    public void DeselectCard()
    {
        if (selectedCard != null)
        {
            selectedCard.transform.localScale = originalScale;
            selectedCard = null;
        }
    }

    private bool CanPlayerAttack(Card attacker)
    {
        if (attacker == null || attacker.exhausted) return false;
        if (EnergyManager.Instance == null) return true;
        return EnergyManager.Instance.CanAfford(Team.Player, attacker.Cost);
    }

    private IEnumerator AttackRoutine(Card attacker, Card target)
    {
        if (attacker == null || target == null) { DeselectCard(); yield break; }

        int cost = attacker.Cost;
        if (EnergyManager.Instance != null && !EnergyManager.Instance.TrySpend(Team.Player, cost))
        {
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
            int dmg = attacker.damage;
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