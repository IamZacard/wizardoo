using UnityEngine;

public class Pillar : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    public float originalColorNumber = 150f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer not found on Pillar object.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SetSpriteAlpha(100);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SetSpriteAlpha(originalColorNumber);
        }
    }

    private void SetSpriteAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color newColor = spriteRenderer.color;
            newColor.a = alpha / originalColorNumber; // Convert alpha to a value between 0 and 1
            spriteRenderer.color = newColor;
        }
    }
}
