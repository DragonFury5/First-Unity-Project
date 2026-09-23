using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float fadeSpeed = 2f;
    public float lifeTime = 0.8f;

    [Header("Rendering Settings")]
    public string sortingLayerName = "Default"; // Change to your Card sorting layer if using custom layers
    public int sortingOrder = 999;

    private TextMeshPro textMesh;
    private Color textColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textColor = textMesh.color;
            ApplySorting();
        }
    }

    public void Setup(int damageAmount)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
        
        textMesh.text = $"-{damageAmount} DMG";
        textColor = Color.red;
        textMesh.color = textColor;
        
        ApplySorting();
        
        Destroy(gameObject, lifeTime);
    }

    private void ApplySorting()
    {
        if (textMesh != null)
        {
            textMesh.sortingOrder = sortingOrder;
            if (!string.IsNullOrEmpty(sortingLayerName))
            {
                textMesh.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            }
        }
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        if (textMesh != null)
        {
            textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;
        }
    }
}