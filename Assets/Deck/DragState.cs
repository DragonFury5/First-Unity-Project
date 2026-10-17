using System.Collections.Generic;

/// <summary>
/// Tracks which Draggables are currently being dragged, globally.
/// Zones query this to decide whether to show their fill.
/// </summary>
public static class DragState
{
    private static readonly HashSet<Draggable> active = new HashSet<Draggable>();

    public static void Add(Draggable d) { if (d != null) active.Add(d); }
    public static void Remove(Draggable d) { if (d != null) active.Remove(d); }

    public static bool AnyMatchingDragActive(int ownerTeam)
    {
        foreach (var d in active)
        {
            if (d == null) continue;
            var card = d.GetComponent<Card>();
            if (card == null || card.ownerId == ownerTeam) return true;
        }
        return false;
    }
}