using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float fadeSpeed = 2f;
    public float lifeTime = 0.8f;

    private TextMeshPro textMesh;
    private Color textColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textColor = textMesh.color;
        }
    }

    public void Setup(int damageAmount)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
        textMesh.text = $"-{damageAmount} ⚔️";
        textColor = Color.red;
        textMesh.color = textColor;
        
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Float upward
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Fade out alpha
        if (textMesh != null)
        {
            textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;
        }
    }
}