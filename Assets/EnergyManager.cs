using UnityEngine;

[System.Serializable]
public class EnergyPool
{
    [Tooltip("Current usable energy.")]
    public int current;

    [Header("Progression")]
    [Tooltip("Energy available on the very first turn of this pool's owner.")]
    public int startingEnergy = 3;

    [Tooltip("Extra energy granted at the start of each subsequent turn.")]
    public int regenPerTurn = 1;

    [Tooltip("Hard cap on energy.")]
    public int cap = 10;

    // Internal: how many refills this pool has received.
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
}

public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }

    [Header("Pools")]
    public EnergyPool playerEnergy = new EnergyPool();
    public EnergyPool enemyEnergy  = new EnergyPool();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
        if (p != null) p.Refill();
    }

    public bool CanAfford(int team, int cost)
    {
        EnergyPool p = GetPool(team);
        return p != null && p.CanAfford(cost);
    }

    public bool TrySpend(int team, int cost)
    {
        EnergyPool p = GetPool(team);
        return p != null && p.Spend(cost);
    }
}