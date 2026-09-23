using System.Collections;
using System.Collections.Generic;
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
    private bool isAttacking = false;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (PhaseManager.Instance == null || PhaseManager.Instance.currentPhase != GamePhase.Battle) return;
        if (isAttacking) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        if (hit.collider != null)
        {
            Card clickedCard = hit.collider.GetComponent<Card>();

            if (clickedCard != null)
            {
                // Player Card (ownerId == 1) -> Select & Enlarge
                if (clickedCard.ownerId == 1)
                {
                    SelectPlayerCard(clickedCard);
                }
                // Enemy Card (ownerId == 2) while player card is selected -> Attack!
                else if (clickedCard.ownerId == 2 && selectedCard != null)
                {
                    StartCoroutine(AttackRoutine(selectedCard, clickedCard));
                }
            }
        }
        else
        {
            DeselectCard();
        }
    }

    private void SelectPlayerCard(Card card)
    {
        if (selectedCard != null)
        {
            DeselectCard();
        }

        selectedCard = card;
        originalScale = selectedCard.transform.localScale;

        float multiplier = (selectScaleMultiplier <= 1f) ? 1.3f : selectScaleMultiplier;
        selectedCard.transform.localScale = originalScale * multiplier;
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
        isAttacking = true;

        Vector3 startPos = attacker.transform.position;
        Vector3 targetPos = target.transform.position + (Vector3.down * attackOffset);
        targetPos.z = startPos.z - 0.5f;

        // 1. Dash to target
        while (Vector3.Distance(attacker.transform.position, targetPos) > 0.05f)
        {
            attacker.transform.position = Vector3.MoveTowards(attacker.transform.position, targetPos, flySpeed * Time.deltaTime);
            yield return null;
        }

        attacker.transform.position = targetPos;

        // 2. Deal damage & spawn pop-up
        int dmgValue = attacker != null ? attacker.damage : 3;

        if (EnemyAI.Instance != null)
        {
            EnemyAI.Instance.ShowDamagePopUp(target.transform.position, dmgValue);
        }

        target.TakeDamage(dmgValue);

        yield return new WaitForSeconds(0.25f);

        // 3. Return to position
        while (Vector3.Distance(attacker.transform.position, startPos) > 0.05f)
        {
            attacker.transform.position = Vector3.MoveTowards(attacker.transform.position, startPos, flySpeed * Time.deltaTime);
            yield return null;
        }

        attacker.transform.position = startPos;

        DeselectCard();
        isAttacking = false;
    }
}   