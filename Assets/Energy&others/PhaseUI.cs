using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhaseUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The big phase label (TMP).")]
    public TextMeshProUGUI phaseLabel;

    [Tooltip("Optional — the 'Next Phase' button. If assigned, it will be disabled during Enemy Turn lock.")]
    public Button nextPhaseButton;

    [Tooltip("Optional — the TMP label on the Next Phase button. Leave null to leave the button's text untouched.")]
    public TextMeshProUGUI nextPhaseButtonLabel;

    [Header("Phase Colors")]
    public Color setupColor     = new Color(0.7f, 0.9f, 1f);   // light blue
    public Color battleColor    = new Color(1f, 0.3f, 0.3f);   // red
    public Color supportColor   = new Color(0.4f, 1f, 0.4f);   // green
    public Color enemyTurnColor = new Color(1f, 0.6f, 0.2f);   // orange

    [Header("Round Counter (optional)")]
    [Tooltip("Leave OFF until you have UI space. When ON, prepends 'ROUND N — ' to the phase text.")]
    public bool showRound = false;
    public string roundPrefix = "ROUND ";
    public string roundSeparator = " — ";

    [Header("Loading Dots (Enemy Turn)")]
    [Tooltip("Seconds between each '.' during the locked Enemy Turn.")]
    public float dotInterval = 0.28f;

    [Header("Pulse Animation")]
    public float pulseScaleMultiplier = 1.12f;
    public float pulseDuration = 0.12f;

    [Header("Button Text (only if nextPhaseButtonLabel is assigned)")]
    [Tooltip("Format string. {0} = next phase text, e.g. 'BATTLE'.")]
    public string nextButtonFormat = "NEXT: {0}";
    public string lockedButtonText = "WAIT";

    private Vector3 originalLabelScale;
    private Coroutine pulseCoroutine;
    private float dotTimer = 0f;
    private GamePhase lastPhase = (GamePhase)(-1);
    private bool hasInitialized = false;

    void Awake()
    {
        if (phaseLabel != null)
            originalLabelScale = phaseLabel.transform.localScale;
    }

    void OnEnable()
    {
        PhaseManager.OnPhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        PhaseManager.OnPhaseChanged -= HandlePhaseChanged;
        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        if (phaseLabel != null) phaseLabel.transform.localScale = originalLabelScale;
    }

    void Update()
    {
        if (PhaseManager.Instance == null) return;

        GamePhase phase = PhaseManager.Instance.currentPhase;

        // Safety net: if the event was somehow missed (e.g., late subscription),
        // pick up the change here. Cheap — one enum comparison per frame.
        if (phase != lastPhase)
            HandlePhaseChanged(phase);

        bool locked = PhaseManager.Instance.isTransitionLocked;

        // Loading dots during locked Enemy Turn
        bool animateDots = (phase == GamePhase.EnemyTurn) && locked;

        if (phaseLabel != null)
        {
            if (animateDots)
            {
                dotTimer += Time.deltaTime;
                int dotCount = ((int)(dotTimer / Mathf.Max(0.01f, dotInterval))) % 4;
                phaseLabel.text = BuildBaseText(phase) + new string('.', dotCount);
            }
            else
            {
                dotTimer = 0f;
                phaseLabel.text = BuildBaseText(phase);
            }
        }

        // Next-phase button state
        if (nextPhaseButton != null)
        {
            nextPhaseButton.interactable = !locked;

            if (nextPhaseButtonLabel != null)
            {
                nextPhaseButtonLabel.text = locked
                    ? lockedButtonText
                    : string.Format(nextButtonFormat, GetPhaseText(GetNextPhase(phase)));
            }
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        lastPhase = phase;

        if (phaseLabel != null)
        {
            phaseLabel.text = BuildBaseText(phase);
            phaseLabel.color = GetPhaseColor(phase);
        }

        // No pulse on the very first initialization, only on real phase changes.
        if (hasInitialized) TriggerPulse();
        else hasInitialized = true;
    }

    private string BuildBaseText(GamePhase phase)
    {
        string text = GetPhaseText(phase);
        if (showRound && PhaseManager.Instance != null)
            text = roundPrefix + PhaseManager.Instance.RoundNumber + roundSeparator + text;
        return text;
    }

    private void TriggerPulse()
    {
        if (phaseLabel == null) return;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        phaseLabel.transform.localScale = originalLabelScale;
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        Vector3 peak = originalLabelScale * pulseScaleMultiplier;

        float t = 0f;
        while (t < pulseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / pulseDuration);
            phaseLabel.transform.localScale = Vector3.Lerp(originalLabelScale, peak, k);
            yield return null;
        }

        t = 0f;
        while (t < pulseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / pulseDuration);
            phaseLabel.transform.localScale = Vector3.Lerp(peak, originalLabelScale, k);
            yield return null;
        }

        phaseLabel.transform.localScale = originalLabelScale;
        pulseCoroutine = null;
    }

    private static string GetPhaseText(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Setup:     return "SETUP";
            case GamePhase.Battle:    return "BATTLE";
            case GamePhase.Support:   return "SUPPORT";
            case GamePhase.EnemyTurn: return "ENEMY TURN";
            default:                  return phase.ToString().ToUpperInvariant();
        }
    }

    private Color GetPhaseColor(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Setup:     return setupColor;
            case GamePhase.Battle:    return battleColor;
            case GamePhase.Support:   return supportColor;
            case GamePhase.EnemyTurn: return enemyTurnColor;
            default:                  return Color.white;
        }
    }

    private static GamePhase GetNextPhase(GamePhase current)
    {
        switch (current)
        {
            case GamePhase.Setup:     return GamePhase.Battle;
            case GamePhase.Battle:    return GamePhase.Support;
            case GamePhase.Support:   return GamePhase.EnemyTurn;
            case GamePhase.EnemyTurn: return GamePhase.Setup;
            default:                  return GamePhase.Setup;
        }
    }
}