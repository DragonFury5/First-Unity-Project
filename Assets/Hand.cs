using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    public List<HandSlot> slots = new List<HandSlot>();

    public int Capacity => slots.Count;

    public int Count
    {
        get
        {
            int n = 0;
            foreach (var s in slots) if (s != null && s.IsOccupied) n++;
            return n;
        }
    }

    public HandSlot FindFirstEmptySlot()
    {
        foreach (var s in slots)
            if (s != null && !s.IsOccupied) return s;
        return null;
    }

    public bool AddCard(Card card)
    {
        if (card == null) return false;

        HandSlot slot = FindFirstEmptySlot();
        if (slot == null) return false;

        Draggable d = card.GetComponent<Draggable>();
        if (d == null) return false;

        slot.Occupy(d);
        card.transform.position = slot.AnchorPosition;
        d.SetHome(slot.AnchorPosition, slot);
        return true;
    }

    public void ClearAndDiscard()
    {
        foreach (var slot in slots)
        {
            if (slot == null || slot.occupant == null) continue;
            GameObject go = slot.occupant.gameObject;
            slot.Vacate(slot.occupant);
            if (go != null) Destroy(go);
        }
    }
}