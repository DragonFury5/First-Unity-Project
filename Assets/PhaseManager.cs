using System;
using UnityEngine;

public enum GamePhase
{
    Setup,
    Battle,
    Support,   // Renamed from Recover
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
            case GamePhase.Battle:    StartPhase(GamePhase.Support);   break;
            case GamePhase.Support:   StartPhase(GamePhase.EnemyTurn); break;
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
            case GamePhase.Support:   HandleSupportPhase();   break;
            case GamePhase.EnemyTurn: HandleEnemyTurn();      break;
        }
    }

    private void HandleSetupPhase()
    {
        setupCount++;

        if (setupCount > 1 && EnergyManager.Instance != null)
            EnergyManager.Instance.RefillForTeam(Team.Player);

        ClearExhaustForTeam(Team.Player);

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

    private void HandleSupportPhase()
    {
        // 1. Clear all temporary buffs from the previous round
        Card[] allCards = FindObjectsByType<Card>(FindObjectsInactive.Exclude);
        foreach (Card c in allCards)
        {
            if (c != null) c.ClearTemporaryBuffs();
        }

        // 2. Trigger OnRecover skills for both players
        foreach (Card c in allCards)
        {
            if (c != null && c.IsSupport && !c.IsDead)
            {
                foreach (var skill in c.data.skills)
                {
                    if (skill.trigger == SupportTrigger.OnRecover)
                    {
                        SkillResolver.ApplySkill(skill, c);
                    }
                }
            }
        }
    }

    private void HandleEnemyTurn()
    {
        if (EnergyManager.Instance != null)
            EnergyManager.Instance.RefillForTeam(Team.Enemy);

        ClearExhaustForTeam(Team.Enemy);

        // EnemyAI listens to the PhaseChanged event and will automatically attack and advance the phase.
    }

    private void ClearExhaustForTeam(int team)
    {
        Card[] allCards = FindObjectsByType<Card>(FindObjectsInactive.Exclude);
        foreach (Card c in allCards)
        {
            if (c != null && c.ownerId == team) c.SetExhausted(false);
        }
    }
}