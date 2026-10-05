using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public static EnemyAI Instance { get; private set; }

    [Header("Enemy Setup")]
    public GameObject cardPrefab;
    public List<CardData> enemyDeckData = new List<CardData>();
    public List<DropZone> enemyDropZones = new List<DropZone>();

    [Header("Damage Visual Prefab")]
    public GameObject damageTextPrefab;

    [Header("Turn Timing")]
    public float preTurnDelay = 0.5f;
    public float betweenAttacksDelay = 0.3f;
    public float postTurnDelay = 0.3f;

    void Awake() { Instance = this; }

    void OnEnable()  { PhaseManager.OnPhaseChanged += HandlePhaseChanged; }
    void OnDisable() { PhaseManager.OnPhaseChanged -= HandlePhaseChanged; }

    private void HandlePhaseChanged(GamePhase newPhase)
    {
        if (newPhase == GamePhase.Setup) SpawnEnemyCards();
        if (newPhase == GamePhase.EnemyTurn) StartCoroutine(EnemyTurnRoutine());
    }

    public void SpawnEnemyCards()
    {
        if (cardPrefab == null || enemyDeckData.Count == 0) return;

        foreach (var zone in enemyDropZones)
        {
            if (zone == null || zone.IsOccupied) continue;

            CardData data = enemyDeckData[Random.Range(0, enemyDeckData.Count)];
            Vector3 spawnPos = new Vector3(zone.transform.position.x, zone.transform.position.y, 0f);

            GameObject go = Instantiate(cardPrefab, spawnPos, Quaternion.identity);

            Card card = go.GetComponent<Card>();
            if (card != null)
            {
                card.Apply(data);
                card.ownerId = Team.Enemy;
            }

            Draggable draggable = go.GetComponent<Draggable>();
            if (draggable != null) zone.Occupy(draggable);
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(preTurnDelay);
        yield return EnemyAttackRoutine();
        yield return new WaitForSeconds(postTurnDelay);

        if (PhaseManager.Instance != null)
            PhaseManager.Instance.AdvancePhase(); // EnemyTurn → Setup
    }

    private IEnumerator EnemyAttackRoutine()
    {
        while (true)
        {
            Card attacker = FindFirstReadyEnemy();
            if (attacker == null) yield break;

            int cost = attacker.Cost;
            if (EnergyManager.Instance != null &&
                !EnergyManager.Instance.CanAfford(Team.Enemy, cost))
                yield break;

            Card target = FindLowestHpPlayerCard();
            if (target == null) yield break;

            if (EnergyManager.Instance != null)
                EnergyManager.Instance.TrySpend(Team.Enemy, cost);

            attacker.SetExhausted(true);

            if (CardSelector.Instance != null)
                yield return CardSelector.Instance.PlayAttackAnimation(attacker, target);

            yield return new WaitForSeconds(betweenAttacksDelay);
        }
    }

    private Card FindFirstReadyEnemy()
    {
        Card[] all = FindObjectsByType<Card>(FindObjectsInactive.Exclude);
        foreach (Card c in all)
        {
            if (c != null && c.ownerId == Team.Enemy && !c.exhausted && !c.IsDead)
                return c;
        }
        return null;
    }

    private Card FindLowestHpPlayerCard()
    {
        Card[] all = FindObjectsByType<Card>(FindObjectsInactive.Exclude);
        Card best = null;
        int bestHp = int.MaxValue;

        foreach (Card c in all)
        {
            if (c == null || c.ownerId != Team.Player || c.IsDead) continue;
            if (c.currentHealth < bestHp)
            {
                bestHp = c.currentHealth;
                best = c;
            }
        }
        return best;
    }

    public void ShowDamagePopUp(Vector3 targetPos, int damage)
    {
        if (damageTextPrefab == null) return;

        Vector3 spawnPos = targetPos + new Vector3(0f, 0.5f, -1f);
        GameObject popUp = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);

        DamageText text = popUp.GetComponent<DamageText>();
        if (text != null) text.Setup(damage);
    }
}