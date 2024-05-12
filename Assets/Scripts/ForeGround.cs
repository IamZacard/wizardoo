using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForeGround : MonoBehaviour
{
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // Change alpha to 0.1
            SetAlpha(0.1f);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // Reset alpha to 1.0 (full black)
            SetAlpha(1.0f);
        }
    }

    private void SetAlpha(float alpha)
    {
        // Get the current color
        Color currentColor = sr.color;

        // Set the alpha value
        currentColor.a = alpha;

        // Assign the new color back to the sprite renderer
        sr.color = currentColor;
    }
}
