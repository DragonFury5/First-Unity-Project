using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Draggable : MonoBehaviour
{
    [Header("Scale Settings")]
    public float grabScaleMultiplier = 1.15f;
    public float scaleSpeed = 12f;

    [Header("Drag Settings")]
    public float zLift = -0.5f;
    public float returnSpeed = 12f;

    [HideInInspector] public bool autoBeginDrag;

    public static event System.Action<Draggable> OnDragStarted;
    public static event System.Action<Draggable> OnDragEnded;

    public bool IsInHand => currentHandSlot != null;

    public bool IsInHand => currentHandSlot != null;
public bool IsInSupportSlot => currentSupportSlot != null;   // <-- add this

    private Vector3 originalScale;
    private Vector3 homePosition;
    private Vector3 grabOffset;
    private float originalZ;

    private DropZone currentZone;
    private HandSlot currentHandSlot;
    private SupportSlot currentSupportSlot;

    private bool isDragging;
    private bool isReturning;
    private bool initialized;

    void Awake() { Initialize(); }

    void Start()
    {
        Initialize();
        if (autoBeginDrag) BeginDrag();
    }

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        originalScale = transform.localScale;
        homePosition = transform.position;
        originalZ = transform.position.z;
    }

    public void SetHome(Vector3 newHome, HandSlot handSlot)
    {
        homePosition = new Vector3(newHome.x, newHome.y, originalZ);
        currentHandSlot = handSlot;
        currentZone = null;
        currentSupportSlot = null;
    }

    void OnDestroy()
    {
        DragState.Remove(this);
        if (currentHandSlot != null) currentHandSlot.Vacate(this);
        if (currentZone != null) currentZone.Vacate(this);
        if (currentSupportSlot != null) currentSupportSlot.Vacate(this);
    }

    void Update()
    {
        bool isBattlePhase = PhaseManager.Instance != null &&
                             PhaseManager.Instance.currentPhase == GamePhase.Battle;

        if (!isBattlePhase)
        {
            float currentMultiplier = isDragging ? grabScaleMultiplier : 1f;
            Vector3 targetScale = originalScale * currentMultiplier;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }

        if (isDragging)
        {
            transform.position = GetMouseWorldPosition() + grabOffset;
            if (Input.GetMouseButtonUp(0)) EndDrag();
        }
        else if (isReturning)
        {
            transform.position = Vector3.Lerp(transform.position, homePosition, Time.deltaTime * returnSpeed);
            if (Vector3.Distance(transform.position, homePosition) < 0.01f)
            {
                transform.position = homePosition;
                isReturning = false;
            }
        }
    }

    void OnMouseDown()
    {
        if (CanDragInCurrentPhase()) BeginDrag();
    }

    public void BeginDrag()
    {
        if (!CanDragInCurrentPhase()) return;
        if (isDragging) return;

        Initialize();
        isReturning = false;
        isDragging = true;

        if (autoBeginDrag)
        {
            grabOffset = Vector3.zero;
            autoBeginDrag = false;
        }
        else
        {
            grabOffset = transform.position - GetMouseWorldPosition();
        }

        Vector3 pos = transform.position;
        pos.z = originalZ + zLift;
        transform.position = pos;

        OnDragStarted?.Invoke(this);
        DragState.Add(this);
    }

    public void EndDrag()
    {
        if (!isDragging) return;
        isDragging = false;

        Vector3 pos = transform.position;
        pos.z = originalZ;
        transform.position = pos;

        DragState.Remove(this);

        // 1) Discard zone
        DiscardZone discard = FindContainingDiscardZone(transform.position);
        if (discard != null)
        {
            bool wasOnBoard = (currentHandSlot == null) &&
                              (currentZone != null || currentSupportSlot != null);

            if (wasOnBoard)
            {
                Card card = GetComponent<Card>();
                if (card != null && card.ownerId == Team.Player && EnergyManager.Instance != null)
                {
                    int refund;
                    if (card.IsSupport)
                        refund = Mathf.Max(0, (card.Cost - 1) / 2);   // Q5: (cost-1)/2 int-div
                    else
                        refund = Mathf.CeilToInt(card.Cost / 2f);     // Q1: ceil(cost/2)

                    EnergyManager.Instance.Refund(Team.Player, refund);
                }
            }

            if (currentHandSlot != null) currentHandSlot.Vacate(this);
            if (currentZone != null) currentZone.Vacate(this);
            if (currentSupportSlot != null) currentSupportSlot.Vacate(this);

            Card selfCard = GetComponent<Card>();
            if (selfCard != null) selfCard.ReturnToDeck();

            OnDragEnded?.Invoke(this);
            Destroy(gameObject);
            return;
        }

        // 2) Support slot
        SupportSlot matchedSupport = FindContainingSupportSlot(transform.position);
        if (matchedSupport != null)
        {
            bool movingFromHand = (currentHandSlot != null);
            if (movingFromHand)
            {
                Card card = GetComponent<Card>();
                if (card != null && EnergyManager.Instance != null)
                {
                    if (!EnergyManager.Instance.TrySpend(Team.Player, card.Cost))
                    {
                        isReturning = true;
                        OnDragEnded?.Invoke(this);
                        return;
                    }
                }
            }

            if (currentZone != null) currentZone.Vacate(this);
            if (currentHandSlot != null) currentHandSlot.Vacate(this);
            if (currentSupportSlot != null && currentSupportSlot != matchedSupport)
                currentSupportSlot.Vacate(this);

            currentHandSlot = null;
            currentZone = null;
            currentSupportSlot = matchedSupport;
            matchedSupport.Occupy(this);

            // OnDeploy skill firing removed per Q2 — only OnRecover fires now.

            homePosition = new Vector3(matchedSupport.transform.position.x, matchedSupport.transform.position.y, originalZ);
            transform.position = homePosition;
            isReturning = false;
            OnDragEnded?.Invoke(this);
            return;
        }

        // 3) Battlefield zone
        DropZone matchedZone = FindContainingZone(transform.position);
        if (matchedZone != null)
        {
            bool movingFromHand = (currentHandSlot != null);
            if (movingFromHand)
            {
                Card card = GetComponent<Card>();
                if (card != null && EnergyManager.Instance != null)
                {
                    if (!EnergyManager.Instance.TrySpend(Team.Player, card.Cost))
                    {
                        isReturning = true;
                        OnDragEnded?.Invoke(this);
                        return;
                    }
                }
            }

            if (currentZone != null && currentZone != matchedZone) currentZone.Vacate(this);
            if (currentHandSlot != null) currentHandSlot.Vacate(this);
            if (currentSupportSlot != null) currentSupportSlot.Vacate(this);

            currentHandSlot = null;
            currentSupportSlot = null;
            currentZone = matchedZone;
            matchedZone.Occupy(this);

            homePosition = new Vector3(matchedZone.transform.position.x, matchedZone.transform.position.y, originalZ);
            transform.position = homePosition;
            isReturning = false;
        }
        else
        {
            isReturning = true;
        }

        OnDragEnded?.Invoke(this);
    }

    private bool CanDragInCurrentPhase()
    {
        if (PhaseManager.Instance == null) return true;
        GamePhase phase = PhaseManager.Instance.currentPhase;
        if (phase != GamePhase.Setup && phase != GamePhase.Support) return false;

        Card card = GetComponent<Card>();
        if (card != null && card.ownerId != Team.Player) return false;
        return true;
    }

    private DropZone FindContainingZone(Vector3 worldPos)
    {
        foreach (var zone in FindObjectsByType<DropZone>(FindObjectsInactive.Exclude))
        {
            if (zone != null && zone.Contains(worldPos) && zone.CanAccept(this))
                return zone;
        }
        return null;
    }

    private SupportSlot FindContainingSupportSlot(Vector3 worldPos)
    {
        foreach (var slot in FindObjectsByType<SupportSlot>(FindObjectsInactive.Exclude))
        {
            if (slot != null && slot.Contains(worldPos) && slot.CanAccept(this))
                return slot;
        }
        return null;
    }

    private DiscardZone FindContainingDiscardZone(Vector3 worldPos)
    {
        foreach (var dz in FindObjectsByType<DiscardZone>(FindObjectsInactive.Exclude))
        {
            if (dz != null && dz.Contains(worldPos)) return dz;
        }
        return null;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null) return transform.position;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}