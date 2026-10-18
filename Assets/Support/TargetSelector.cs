using System;
using System.Collections.Generic;
using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    public static TargetSelector Instance { get; private set; }

    private bool isSelecting = false;
    private bool targetAllies;
    private int selectingTeam;
    private Action<Card> onTargetSelected;
    
    private List<Card> validTargets = new List<Card>();
    private List<Vector3> originalScales = new List<Vector3>();

    void Awake() { Instance = this; }

    public void BeginSelection(int team, bool allies, Action<Card> callback)
    {
        if (isSelecting) return;

        isSelecting = true;
        selectingTeam = team;
        targetAllies = allies;
        onTargetSelected = callback;

        validTargets.Clear();
        originalScales.Clear();

        // Find every card on the board
        Card[] allCards = FindObjectsByType<Card>(FindObjectsInactive.Exclude);
        
        foreach (var c in allCards)
        {
            if (c == null || c.IsDead || c.IsSupport) continue; // Can't target Support cards

            bool isAlly = c.ownerId == selectingTeam;
            
            // If we want allies, and it's an ally. Or if we want enemies, and it's an enemy.
            if ((targetAllies && isAlly) || (!targetAllies && !isAlly))
            {
                validTargets.Add(c);
                originalScales.Add(c.transform.localScale);
                
                // Visual Highlight: Grow the card slightly so the player knows they can click it
                c.transform.localScale *= 1.15f; 
            }
        }

        if (validTargets.Count == 0)
        {
            Debug.Log("[TargetSelector] No valid targets found. Cancelling.");
            EndSelection();
        }
    }

    void Update()
    {
        if (!isSelecting) return;

        // Right-click cancels targeting
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("[TargetSelector] Cancelled targeting.");
            EndSelection();
            return;
        }

        // Left-click confirms targeting
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);

            Collider2D[] hits = Physics2D.OverlapPointAll(mousePos2D);
            foreach (var hit in hits)
            {
                Card clickedCard = hit.GetComponentInParent<Card>();
                
                if (clickedCard != null && validTargets.Contains(clickedCard))
                {
                    var cb = onTargetSelected;
                    var target = clickedCard;
                    
                    EndSelection(); // Reset visuals first
                    cb?.Invoke(target);  // Then fire the skill
                    return;
                }
            }
        }
    }

    private void EndSelection()
    {
        isSelecting = false;
        for (int i = 0; i < validTargets.Count; i++)
        {
            if (validTargets[i] != null)
                validTargets[i].transform.localScale = originalScales[i];
        }
        validTargets.Clear();
        onTargetSelected = null;
    }
}