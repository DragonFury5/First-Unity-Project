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
    public Hand playerHand;

    public static event Action<GamePhase> OnPhaseChanged;

    private int setupCount = 0;
    private int enemyTurnCount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() { StartPhase(GamePhase.Setup); }

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

        if (setupCount > 1 && EnergyManager.Instance != null)
            EnergyManager.Instance.RefillForTeam(Team.Player);

        ClearExhaustForTeam(Team.Player);

        // Per your rule: at the start of Setup, discard whatever is left in hand,
        // then draw a fresh set. The hand is "replaced" every round.
        if (playerHand != null) playerHand.ClearAndDiscard();

        if (playerDeck != null)
        {
            for (int i = 0; i < cardsToDrawInSetup; i++) playerDeck.DrawCard();
        }
        else
        {
            Debug.LogWarning("[PhaseManager] Player Deck is not assigned!");
        }
    }

    private void HandleBattlePhase() { }

    private void HandleRecoverPhase()
    {
        Card[] all = FindObjectsByType<Card>(FindObjectsInactive.Exclude);
        foreach (Card c in all) if (c != null) c.OnRecoverPhase();
    }

    private void HandleEnemyTurn()
    {
        enemyTurnCount++;

        if (enemyTurnCount > 1 && EnergyManager.Instance != null)
            EnergyManager.Instance.RefillForTeam(Team.Enemy);

        ClearExhaustForTeam(Team.Enemy);
    }

    private void ClearExhaustForTeam(int team)
    {
        Card[] all = FindObjectsByType<Card>(FindObjectsInactive.Exclude);
        foreach (Card c in all)
            if (c != null && c.ownerId == team) c.SetExhausted(false);
    }
}