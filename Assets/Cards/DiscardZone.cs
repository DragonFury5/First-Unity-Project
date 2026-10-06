using UnityEngine;

/// <summary>
/// Any Draggable dropped onto this zone is destroyed.
/// Attach to the deck (or a trash icon) and give it a Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DiscardZone : MonoBehaviour
{
    private Collider2D col;

    void Awake() { col = GetComponent<Collider2D>(); }

    public bool Contains(Vector2 point)
    {
        return col != null && col.OverlapPoint(point);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.35f);
        Collider2D c = GetComponent<Collider2D>();
        if (c != null)
        {
            Bounds b = c.bounds;
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 1f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}