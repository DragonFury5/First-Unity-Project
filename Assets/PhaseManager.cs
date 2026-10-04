using System;
using UnityEngine;

public enum GamePhase
{
    Setup,
    Battle,
    Recover,
    EnemyTurn
}

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance { get; private set; }

    [Header("Phase Settings")]
    public GamePhase currentPhase = GamePhase.Setup;
    public int cardsToDrawInSetup = 3;

    [Header("References")]
    public Deck playerDeck;

    public static event Action<GamePhase> OnPhaseChanged;

    private int setupCount = 0;
    private int enemyTurnCount = 0;

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
        StartPhase(GamePhase.Setup);
    }

    public void AdvancePhase()
    {
        switch (currentPhase)
        {
            case GamePhase.Setup:     StartPhase(GamePhase.Battle);    break;
            case GamePhase.Battle:    StartPhase(GamePhase.Recover);   break;
            case GamePhase.Recover:   StartPhase(GamePhase.EnemyTurn); break;
            case GamePhase.EnemyTurn: StartPhase(GamePhase.Setup);     break;
        }
    }

    public void StartPhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"[PhaseManager] Entering Phase: {currentPhase}");

        OnPhaseChanged?.Invoke(currentPhase);

        switch (currentPhase)
        {
            case GamePhase.Setup:     HandleSetupPhase();     break;
            case GamePhase.Battle:    HandleBattlePhase();    break;
            case GamePhase.Recover:   HandleRecoverPhase();   break;
            case GamePhase.EnemyTurn: HandleEnemyTurn();      break;
        }
    }

    private void HandleSetupPhase()
    {
        setupCount++;

        // Refill player energy on every Setup except the very first (starting energy covers that).
        if (setupCount > 1 && EnergyManager.Instance != null)
            EnergyManager.Instance.RefillForTeam(Team.Player);

        // Clear player exhaustion at the start of their turn.
        ClearExhaustForTeam(Team.Player);

        // Auto-draw for the player.
        if (playerDeck != null)
        {
            for (int i = 0; i < cardsToDrawInSetup; i++) playerDeck.DrawCard();
        }
        else
        {
            Debug.LogWarning("[PhaseManager] Player Deck is not assigned!");
        }
    }

    private void HandleBattlePhase()
    {
        // Player attacks via CardSelector during this phase.
    }

    private void HandleRecoverPhase()
    {
        // Recover applies to BOTH teams equally.
        Card[] allCards = FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (Card c in allCards)
        {
            if (c != null) c.OnRecoverPhase();
        }
    }

    private void HandleEnemyTurn()
    {
        enemyTurnCount++;

        if (enemyTurnCount > 1 && EnergyManager.Instance != null)
            EnergyManager.Instance.RefillForTeam(Team.Enemy);

        ClearExhaustForTeam(Team.Enemy);

        // EnemyAI listens to OnPhaseChanged and runs the actual turn.
        // It calls PhaseManager.AdvancePhase() when done → back to Setup.
    }

    private void ClearExhaustForTeam(int team)
    {
        Card[] allCards = FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (Card c in allCards)
        {
            if (c != null && c.ownerId == team) c.SetExhausted(false);
        }
    }
}