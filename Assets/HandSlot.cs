using UnityEngine;

/// <summary>
/// A fixed position where a card waits in the player's hand.
/// Purely a marker — no bounds, no click handling. The card is moved here by Hand.AddCard.
/// </summary>
public class HandSlot : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Optional anchor child. If null, uses this transform.")]
    public Transform anchor;

    [Header("Occupancy")]
    [HideInInspector] public Draggable occupant;

    public bool IsOccupied => occupant != null;
    public bool CanAccept(Draggable d) => occupant == null || occupant == d;

    public Vector3 AnchorPosition => (anchor != null ? anchor : transform).position;

    public void Occupy(Draggable d) { occupant = d; }
    public void Vacate(Draggable d) { if (occupant == d) occupant = null; }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.5f);
        Gizmos.DrawWireCube(AnchorPosition, new Vector3(1.2f, 1.7f, 0.1f));
    }
}