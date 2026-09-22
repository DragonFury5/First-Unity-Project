using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    public int ownerId = 0;
    public List<CardData> cards = new List<CardData>();

    [Header("Spawning")]
    public GameObject cardPrefab;
    public Transform spawnPoint;

    private List<CardData> remaining;

    void Start()
    {
        ResetDeck();
    }

    public void ResetDeck()
    {
        remaining = new List<CardData>(cards);
    }

    

    public Card DrawCard()
    {
        if (cardPrefab == null)
        {
            Debug.LogError("Assign CardPrefab to the Deck script on " + gameObject.name);
            return null;
        }

        if (remaining == null || remaining.Count == 0)
        {
            ResetDeck();
        }

        if (remaining == null || remaining.Count == 0)
        {
            Debug.LogWarning("Deck has no CardData assigned!");
            return null;
        }

Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
    
    // DEBUG 1: Print world position of spawn
    Debug.Log($"[Deck Debug] Spawning card at World Position: {pos}");

    // DEBUG 2: Verify Main Camera reference
    if (Camera.main == null)
    {
        Debug.LogError("[Deck Debug] NO CAMERA TAGGED AS 'MainCamera' IN SCENE!");
    }
    else
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(pos);
        Debug.Log($"[Deck Debug] Viewport Pos: {viewportPos} (Values outside 0-1 mean off-screen!)");
    }

    GameObject go = Instantiate(cardPrefab, pos, Quaternion.identity);

        int index = Random.Range(0, remaining.Count);
        CardData picked = remaining[index];
        remaining.RemoveAt(index);

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject go = Instantiate(cardPrefab, pos, Quaternion.identity);

        Card card = go.GetComponent<Card>();
        if (card != null)
        {
            card.Apply(picked);
            card.ownerId = ownerId;
        }

    

        return card;
    }

    void OnMouseDown()
{
    // Draw the card
    Card drawn = DrawCard();
    if (drawn == null) return;

    // Immediately hand the drag action to the card
    Draggable d = drawn.GetComponent<Draggable>();
    if (d != null)
    {
        d.autoBeginDrag = true;
        d.BeginDrag();
        
    }

    
}



}