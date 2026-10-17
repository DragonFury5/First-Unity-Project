using UnityEngine;

public class SupportSlot : MonoBehaviour
{
    [Header("Ownership")]
    public int ownerTeam = Team.Player;

    [Header("Visuals")]
    public SpriteRenderer auraRenderer; // A purple square/glow
    [Range(0f, 1f)] public float idleAlpha = 0.2f;
    [Range(0f, 1f)] public float dragAlpha = 0.6f;
    public float fadeSpeed = 10f;

    [Header("Occupancy")]
    [HideInInspector] public Draggable occupant;

    private float currentAlpha;
    public bool IsOccupied => occupant != null;

    public bool CanAccept(Draggable d)
    {
        if (d == null) return false;
        if (occupant == d) return true; // Already here, just drop back
        if (occupant != null) return false; // Slot full

        Card card = d.GetComponent<Card>();
        if (card == null || card.data == null) return false;

        // Only accept Support cards from the matching team
        return card.data.type == CardType.Support && card.ownerId == ownerTeam;
    }

    public void Occupy(Draggable d) { occupant = d; }
    public void Vacate(Draggable d) { if (occupant == d) occupant = null; }

    void Awake()
    {
        currentAlpha = idleAlpha;
        ApplyAlpha(currentAlpha);
    }

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

    public bool Contains(Vector2 point)
    {
        // Adjust this based on your Support slot size (e.g., 1.5 x 2.2)
        Vector2 pos = transform.position;
        return point.x >= pos.x - 0.75f && point.x <= pos.x + 0.75f &&
               point.y >= pos.y - 1.1f && point.y <= pos.y + 1.1f;
    }
}