using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Draggable : MonoBehaviour
{
    [Header("Scale Settings")]
    [Tooltip("How much the card grows while selected/dragged.")]
    public float grabScaleMultiplier = 1.15f;
    public float scaleSpeed = 12f;

    [Header("Drag Settings")]
    public float zLift = -0.5f;
    public float returnSpeed = 12f;

    [HideInInspector] public bool autoBeginDrag;

    public static event System.Action<Draggable> OnDragStarted;
    public static event System.Action<Draggable> OnDragEnded;

    private Vector3 originalScale;
    private Vector3 homePosition;
    private Vector3 grabOffset;
    private float originalZ;

    private DropZone currentZone;
    private bool isDragging;
    private bool isReturning;

    void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (originalScale != Vector3.zero) return;
        originalScale = transform.localScale;
        homePosition = transform.position;
        originalZ = transform.position.z;
    }

    void Start()
    {
        Initialize();
        if (autoBeginDrag) BeginDrag();
    }

    void Update()
    {
        float currentMultiplier = isDragging ? grabScaleMultiplier : 1f;
        Vector3 targetScale = originalScale * currentMultiplier;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);

        if (isDragging)
        {
            transform.position = GetMouseWorldPosition() + grabOffset;

            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
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
        BeginDrag();
        Debug.Log($"[Draggable Debug] Card '{gameObject.name}' clicked successfully at position {transform.position}!");
    BeginDrag();
    }

    public void BeginDrag()
    {
        if (isDragging) return;
        Initialize();

        if (currentZone != null)
        {
            currentZone.Vacate(this);
            currentZone = null;
        }

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
    }

    public void EndDrag()
    {
        if (!isDragging) return;
        isDragging = false;

        Vector3 pos = transform.position;
        pos.z = originalZ;
        transform.position = pos;

        DropZone matched = FindContainingZone(transform.position);

        if (matched != null)
        {
            currentZone = matched;
            matched.Occupy(this);

            homePosition = new Vector3(matched.transform.position.x, matched.transform.position.y, originalZ);
            transform.position = homePosition;
            isReturning = false;
        }
        else
        {
            isReturning = true;
        }

        OnDragEnded?.Invoke(this);
    }

    private DropZone FindContainingZone(Vector3 worldPos)
    {
        foreach (var zone in FindObjectsByType<DropZone>())
        {
            if (zone != null && zone.Contains(worldPos) && zone.CanAccept(this))
                return zone;
        }
        return null;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}