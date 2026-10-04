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

    void Awake()
    {
        Instance = this;
    }

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

        if (clickedCard == null)
        {
            DeselectCard();
            return;
        }

        if (clickedCard.ownerId == Team.Player)
        {
            SelectPlayerCard(clickedCard);
        }
        else if (clickedCard.ownerId == Team.Enemy && selectedCard != null)
        {
            StartCoroutine(AttackRoutine(selectedCard, clickedCard));
        }
    }

    /// <summary>Closest Card whose collider contains the point, or null.</summary>
    private Card GetCardAtPoint(Vector2 point)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(point);
        if (hits == null || hits.Length == 0) return null;

        Card closest = null;
        float closestSqrDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            // GetComponentInParent so a child collider still maps back to the Card
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
        if (selectedCard == card) return;   // Already selected

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

    private IEnumerator AttackRoutine(Card attacker, Card target)
    {
        if (attacker == null || target == null)
        {
            DeselectCard();
            yield break;
        }

        isAttacking = true;

        Vector3 startPos = attacker.transform.position;
        Vector3 targetPos = target.transform.position + (Vector3.down * attackOffset);
        targetPos.z = startPos.z - 0.5f;

        // 1. Dash to target
        while (attacker != null && Vector3.Distance(attacker.transform.position, targetPos) > 0.05f)
        {
            attacker.transform.position = Vector3.MoveTowards(
                attacker.transform.position, targetPos, flySpeed * Time.deltaTime);
            yield return null;
        }

        if (attacker == null) { DeselectCard(); isAttacking = false; yield break; }

        attacker.transform.position = targetPos;

        // 2. Deal damage & popup
        if (target != null)
        {
            int dmgValue = attacker.damage;
            if (EnemyAI.Instance != null)
                EnemyAI.Instance.ShowDamagePopUp(target.transform.position, dmgValue);

            target.TakeDamage(dmgValue);
        }

        yield return new WaitForSeconds(0.25f);

        // 3. Return home
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

        DeselectCard();
        isAttacking = false;
    }
}