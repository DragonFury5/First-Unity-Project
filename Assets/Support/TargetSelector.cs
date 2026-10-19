using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

        Card[] allCards = FindObjectsByType<Card>(FindObjectsInactive.Exclude);
        
        foreach (var c in allCards)
        {
            if (c == null || c.IsDead || c.IsSupport) continue;

            Draggable d = c.GetComponent<Draggable>();
            if (d != null && d.IsInHand) continue;

            bool isAlly = c.ownerId == selectingTeam;
            
            if ((targetAllies && isAlly) || (!targetAllies && !isAlly))
            {
                validTargets.Add(c);
                originalScales.Add(c.transform.localScale);
                
                // 50% BIGGER so you can clearly see it's targetable!
                c.transform.localScale *= 1.5f; 
            }
        }

        if (validTargets.Count == 0)
        {
            Debug.LogWarning("[TargetSelector] No valid targets on the battlefield. Cancelling.");
            EndSelection();
        }
        else
        {
            Debug.Log($"[TargetSelector] Targeting Mode STARTED. {validTargets.Count} cards are glowing/bigger. Click one!");
        }
    }

    void Update()
    {
        if (!isSelecting) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("[TargetSelector] Right-click detected. Cancelling.");
            EndSelection();
            return;
        }

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
                    Debug.Log($"[TargetSelector] Valid target clicked! Applying effect to {clickedCard.name}");
                    
                    var cb = onTargetSelected;
                    var target = clickedCard;
                    
                    EndSelection();
                    cb?.Invoke(target);
                    return;
                }
            }
            Debug.Log("[TargetSelector] Clicked, but missed any valid targets.");
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