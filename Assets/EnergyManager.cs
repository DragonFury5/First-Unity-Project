using System;
using UnityEngine;

[System.Serializable]
public class EnergyPool
{
    public int current;

    [Header("Progression")]
    public int startingEnergy = 10;
    public int regenPerTurn = 2;
    public int cap = 30;

    [HideInInspector] public int turnCount = 0;

    public void Initialize()
    {
        turnCount = 0;
        current = startingEnergy;
    }

    public void Refill()
    {
        turnCount++;
        int target = Mathf.Min(startingEnergy + turnCount * regenPerTurn, cap);
        current = target;
    }

    public bool CanAfford(int cost) => cost <= 0 || current >= cost;

    public bool Spend(int cost)
    {
        if (cost <= 0) return true;
        if (current < cost) return false;
        current -= cost;
        return true;
    }

    public int NextTurnTarget()
    {
        return Mathf.Min(startingEnergy + (turnCount + 1) * regenPerTurn, cap);
    }
}

public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }

    [Header("Pools")]
    public EnergyPool playerEnergy = new EnergyPool();
    public EnergyPool enemyEnergy  = new EnergyPool();

    /// <summary>Fired whenever a pool changes (spend, refund, refill). Passes the affected team.</summary>
    public static event Action<int> OnEnergyChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        playerEnergy.Initialize();
        enemyEnergy.Initialize();
    }

    public EnergyPool GetPool(int team)
    {
        if (team == Team.Player) return playerEnergy;
        if (team == Team.Enemy)  return enemyEnergy;
        return null;
    }

    public void RefillForTeam(int team)
    {
        EnergyPool p = GetPool(team);
        if (p == null) return;
        p.Refill();
        OnEnergyChanged?.Invoke(team);
    }

    public bool CanAfford(int team, int cost)
    {
        EnergyPool p = GetPool(team);
        return p != null && p.CanAfford(cost);
    }

    public bool TrySpend(int team, int cost)
    {
        EnergyPool p = GetPool(team);
        if (p == null || !p.Spend(cost)) return false;
        OnEnergyChanged?.Invoke(team);
        return true;
    }

    /// <summary>Adds energy back (e.g. discarding a battlefield card). Clamped at cap.</summary>
    public void Refund(int team, int amount)
    {
        if (amount <= 0) return;
        EnergyPool p = GetPool(team);
        if (p == null) return;
        p.current = Mathf.Min(p.current + amount, p.cap);
        OnEnergyChanged?.Invoke(team);
    }
}