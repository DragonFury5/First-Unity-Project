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

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        PhaseManager.OnPhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        PhaseManager.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(GamePhase newPhase)
    {
        if (newPhase == GamePhase.Setup)
        {
            SpawnEnemyCards();
        }
    }

    public void SpawnEnemyCards()
    {
        if (cardPrefab == null || enemyDeckData.Count == 0) return;

        foreach (var zone in enemyDropZones)
        {
            // Only spawn if the slot is empty
            if (zone != null && zone.IsOccupied == false)
            {
                // Choose a random card from enemy deck
                CardData data = enemyDeckData[Random.Range(0, enemyDeckData.Count)];

                Vector3 spawnPos = new Vector3(zone.transform.position.x, zone.transform.position.y, 0f);
                GameObject go = Instantiate(cardPrefab, spawnPos, Quaternion.identity);

                Card card = go.GetComponent<Card>();
                if (card != null)
                {
                    card.Apply(data);
                    card.ownerId = 1; // 1 = Enemy
                }

                Draggable draggable = go.GetComponent<Draggable>();
                if (draggable != null)
                {
                    zone.Occupy(draggable);
                }
            }
        }
    }

    public void ShowDamagePopUp(Vector3 targetPos, int damage)
    {
        if (damageTextPrefab == null) return;

        Vector3 spawnPos = targetPos + new Vector3(0f, 0.5f, -1f);
        GameObject popUp = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
        
        DamageText text = popUp.GetComponent<DamageText>();
        if (text != null)
        {
            text.Setup(damage);
        }
    }
}