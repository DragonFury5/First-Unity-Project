using UnityEngine;

public class DropZone : MonoBehaviour
{
    [Header("Slot Bounds")]
    [Tooltip("Width and Height of the drop target in world units. Should match your card size (slightly larger).")]
    public Vector2 size = new Vector2(1.5f, 2.2f);

    [Header("Ownership")]
    [Tooltip("Only cards owned by this team may be placed here.")]
    public int ownerTeam = Team.Player;

    [Header("Renderers (children)")]
    [Tooltip("Hollow border sprite. Always visible.")]
    public SpriteRenderer outlineRenderer;

    [Tooltip("Solid fill sprite. Fades in only during a matching drag.")]
    public SpriteRenderer fillRenderer;

    [Header("Alpha")]
    [Range(0f, 1f)] public float outlineAlpha = 0.5f;
    [Range(0f, 1f)] public float fillAlpha = 0.25f;
    public float fadeSpeed = 10f;

    [Header("Colors")]
    public Color outlineColor = new Color(1f, 1f, 1f, 1f);
    public Color fillColor = new Color(1f, 1f, 1f, 1f);

    [Header("Occupancy")]
    [HideInInspector] public Draggable occupant;

    private float currentFillAlpha;

    public bool IsOccupied => occupant != null;

    public bool CanAccept(Draggable d)
    {
        if (d == null) return false;
        if (occupant != null && occupant != d) return false;

        Card card = d.GetComponent<Card>();
        if (card == null) return true;
        return card.ownerId == ownerTeam;
    }

    public void Occupy(Draggable d) { occupant = d; }
    public void Vacate(Draggable d) { if (occupant == d) occupant = null; }

    void Awake()
    {
        currentFillAlpha = 0f;
        ApplyOutlineColor();
        ApplyFillAlpha(0f);
    }

    void OnEnable()
    {
        Draggable.OnDragStarted += HandleDragStarted;
        Draggable.OnDragEnded += HandleDragEnded;
    }

    void OnDisable()
    {
        Draggable.OnDragStarted -= HandleDragStarted;
        Draggable.OnDragEnded -= HandleDragEnded;
    }

    void HandleDragStarted(Draggable d)
    {
        if (d == null) return;

        // Only brighten for draggers that belong to this zone's team.
        Card card = d.GetComponent<Card>();
        if (card != null && card.ownerId != ownerTeam) return;

        // Lerp target — handled in Update.
    }

    void HandleDragEnded(Draggable d)
    {
        // Lerp target — handled in Update.
    }

    void Update()
    {
        bool dragging = IsMatchingDragActive();
        float target = dragging ? fillAlpha : 0f;

        currentFillAlpha = Mathf.Lerp(currentFillAlpha, target, Time.deltaTime * fadeSpeed);
        ApplyFillAlpha(currentFillAlpha);
    }

    private bool IsMatchingDragActive()
    {
        // Fallback: scan for any active drag from the matching team.
        // Simpler and stateless; fine for a handful of zones.
        // Hooked via events would be faster, but requires tracking per-zone state.
        return DragState.AnyMatchingDragActive(ownerTeam);
    }

    private void ApplyFillAlpha(float a)
    {
        if (fillRenderer == null) return;
        Color c = fillColor;
        c.a = a;
        fillRenderer.color = c;
    }

    private void ApplyOutlineColor()
    {
        if (outlineRenderer == null) return;
        Color c = outlineColor;
        c.a = outlineAlpha;
        outlineRenderer.color = c;
    }

    public bool Contains(Vector2 point)
    {
        Vector2 pos = transform.position;
        Vector2 halfSize = size * 0.5f;

        return point.x >= pos.x - halfSize.x && point.x <= pos.x + halfSize.x &&
               point.y >= pos.y - halfSize.y && point.y <= pos.y + halfSize.y;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawCube(transform.position, new Vector3(size.x, size.y, 0.1f));
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0.1f));
    }

    void OnValidate()
    {
        ScaleRendererToSize(outlineRenderer);
        ScaleRendererToSize(fillRenderer);
        ApplyOutlineColor();
    }

    private void ScaleRendererToSize(SpriteRenderer rend)
    {
        if (rend == null || rend.sprite == null) return;
        Vector2 b = rend.sprite.bounds.size;
        if (b.x <= 0f || b.y <= 0f) return;
        rend.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, 1f);
    }
}