using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DropZone : MonoBehaviour
{
    [Header("Slot Bounds")]
    [Tooltip("Width and Height of the drop target in world units.")]
    public Vector2 size = new Vector2(1.5f, 2.2f);

    [Header("Ownership")]
    [Tooltip("Only cards owned by this team may be placed here. Use Team.Player or Team.Enemy.")]
    public int ownerTeam = Team.Player;

    [Header("Visual Alpha")]
    [Range(0f, 1f)] public float visibleAlpha = 0.4f;
    public float fadeSpeed = 10f;

    [Header("Occupancy")]
    [HideInInspector] public Draggable occupant;

    private SpriteRenderer sr;
    private float targetAlpha;

    public bool IsOccupied => occupant != null;

    public bool CanAccept(Draggable d)
    {
        if (d == null) return false;
        if (occupant != null && occupant != d) return false;

        Card card = d.GetComponent<Card>();
        if (card == null) return true; // No Card component → no team constraint

        return card.ownerId == ownerTeam;
    }

    public void Occupy(Draggable d) { occupant = d; }
    public void Vacate(Draggable d) { if (occupant == d) occupant = null; }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        SetAlpha(0f);
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

        // Only fade in for draggers that belong to this zone's team.
        Card card = d.GetComponent<Card>();
        if (card != null && card.ownerId != ownerTeam) return;

        targetAlpha = visibleAlpha;
    }

    void HandleDragEnded(Draggable d) => targetAlpha = 0f;

    void Update()
    {
        Color c = sr.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * fadeSpeed);
        sr.color = c;
    }

    void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
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
        if (TryGetComponent(out SpriteRenderer rend) && rend.sprite != null)
        {
            Vector2 spriteBounds = rend.sprite.bounds.size;
            if (spriteBounds.x > 0f && spriteBounds.y > 0f)
            {
                transform.localScale = new Vector3(
                    size.x / spriteBounds.x,
                    size.y / spriteBounds.y,
                    1f
                );
            }
        }
    }
}