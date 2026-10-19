using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SupportSlot : MonoBehaviour
{
    [Header("Ownership")]
    public int ownerTeam = Team.Player;

    [Header("Visuals")]
    public SpriteRenderer auraRenderer;
    [Range(0f, 1f)] public float idleAlpha = 0.2f;
    [Range(0f, 1f)] public float dragAlpha = 0.6f;
    public float fadeSpeed = 10f;

    [Header("Occupancy")]
    [HideInInspector] public Draggable occupant;

    private float currentAlpha;
    private Collider2D col;

    public bool IsOccupied => occupant != null;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        currentAlpha = idleAlpha;
        ApplyAlpha(currentAlpha);
    }

    public bool CanAccept(Draggable d)
    {
        if (d == null) return false;
        if (occupant == d) return true;
        if (occupant != null) return false;

        Card card = d.GetComponent<Card>();
        if (card == null || card.data == null) return false;

        return card.data.type == CardType.Support && card.ownerId == ownerTeam;
    }

    public void Occupy(Draggable d) { occupant = d; }
    public void Vacate(Draggable d) { if (occupant == d) occupant = null; }

    void Update()
    {
        bool dragging = DragState.AnyMatchingDragActive(ownerTeam);
        float target = dragging ? dragAlpha : idleAlpha;
        
        currentAlpha = Mathf.Lerp(currentAlpha, target, Time.deltaTime * fadeSpeed);
        ApplyAlpha(currentAlpha);
    }

    private void ApplyAlpha(float a)
    {
        if (auraRenderer != null)
        {
            Color c = auraRenderer.color;
            c.a = a;
            auraRenderer.color = c;
        }
    }

    // NEW: Uses the Collider instead of hardcoded math
    public bool Contains(Vector2 point)
    {
        return col != null && col.OverlapPoint(point);
    }

    void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null)
        {
            Bounds b = c.bounds;
            Gizmos.color = new Color(0.7f, 0f, 1f, 0.35f); // Purple Gizmo
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = new Color(0.7f, 0f, 1f, 1f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}