using UnityEngine;
using TMPro;

public class EnergyUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI label;

    [Header("Display Options")]
    public int teamToDisplay = Team.Player;
    public string prefix = "Energy: ";
    public bool showMax = true;

    void OnEnable()
    {
        EnergyManager.OnEnergyChanged += HandleEnergyChanged;
        PhaseManager.OnPhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        EnergyManager.OnEnergyChanged -= HandleEnergyChanged;
        PhaseManager.OnPhaseChanged -= HandlePhaseChanged;
    }

    void Start()
    {
        Refresh();
    }

    private void HandleEnergyChanged(int team)
    {
        if (team == teamToDisplay) Refresh();
    }

    private void HandlePhaseChanged(GamePhase _) => Refresh();

    public void Refresh()
    {
        if (label == null) return;
        if (EnergyManager.Instance == null) return;

        EnergyPool pool = EnergyManager.Instance.GetPool(teamToDisplay);
        if (pool == null) return;

        int displayMax = Mathf.Min(pool.NextTurnTarget(), pool.cap);
        label.text = showMax
            ? $"{prefix}{pool.current}/{displayMax}"
            : $"{prefix}{pool.current}";
    }
}