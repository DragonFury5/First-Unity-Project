using System;
using UnityEngine;

public enum GamePhase
{
    Setup,    // Phase 1: Draw cards, place onto DropZones
    Battle,   // Phase 2: Select units and attack targets
    Recover,  // Phase 3: Play support cards, clear unused hand cards
    EnemyTurn // Enemy AI / opponent turn
}

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance { get; private set; }

    [Header("Phase Settings")]
    public GamePhase currentPhase = GamePhase.Setup;
    public int cardsToDrawInSetup = 3;

    [Header("References")]
    public Deck playerDeck;

    // Events for other scripts to listen to phase changes
    public static event Action<GamePhase> OnPhaseChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Start the game in Phase 1 (Setup)
        StartPhase(GamePhase.Setup);
    }

    /// <summary>
    /// Advances to the next logical phase in sequence.
    /// </summary>
    public void AdvancePhase()
    {
        switch (currentPhase)
        {
            case GamePhase.Setup:
                StartPhase(GamePhase.Battle);
                break;

            case GamePhase.Battle:
                StartPhase(GamePhase.Recover);
                break;

            case GamePhase.Recover:
                StartPhase(GamePhase.EnemyTurn);
                break;

            case GamePhase.EnemyTurn:
                StartPhase(GamePhase.Setup);
                break;
        }
    }

    public void StartPhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"[PhaseManager] Entering Phase: {currentPhase}");

        OnPhaseChanged?.Invoke(currentPhase);

        switch (currentPhase)
        {
            case GamePhase.Setup:
                HandleSetupPhase();
                break;

            case GamePhase.Battle:
                HandleBattlePhase();
                break;

            case GamePhase.Recover:
                HandleRecoverPhase();
                break;

            case GamePhase.EnemyTurn:
                HandleEnemyTurn();
                break;
        }
    }

    private void HandleSetupPhase()
    {
        // Automatically draw starting cards for Phase 1
        if (playerDeck != null)
        {
            for (int i = 0; i < cardsToDrawInSetup; i++)
            {
                playerDeck.DrawCard();
            }
        }
        else
        {
            Debug.LogWarning("[PhaseManager] Player Deck is not assigned!");
        }
    }

    private void HandleBattlePhase()
    {
        // Gating card drawing/placement if necessary, enabling combat input
    }

    private void HandleRecoverPhase()
    {
        // Cleanup or support card interactions before ending player turn
    }

    private void HandleEnemyTurn()
    {
        // Placeholder for AI actions, then automatically return to Setup
        Debug.Log("[PhaseManager] Enemy turn running...");
        // For testing: automatically pass enemy turn after 1.5 seconds
        Invoke(nameof(EndEnemyTurn), 1.5f);
    }

    private void EndEnemyTurn()
    {
        StartPhase(GamePhase.Setup);
    }
}