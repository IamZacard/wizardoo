using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Include TextMeshPro namespace
using UnityEngine.UI; // Include UI namespace

public class EnterWord : MonoBehaviour
{
    public TextMeshProUGUI textBox;             // Reference to the TextMeshPro Text component
    public string text1;                        // The first text to be typed out
    public string text2;                        // The second text to be typed out
    public float typingSpeed = 0.05f;           // Speed of typing in seconds
    public Image firstPicture;                  // The first Image component (initially black)
    public Image secondPicture;                 // The second Image component (to fade in)
    public Image thirdPicture;                  // The third Image component (to fade in)
    public List<AudioClip> soundClips;          // List of typing sound clips
    public float fadeDuration = 2.0f;           // Duration of the fade effect
    public float delayBetweenSteps = 1.0f;      // Delay between steps

    private void Start()
    {
        StartCoroutine(SceneSequence()); // Start the sequence
    }

    private IEnumerator SceneSequence()
    {
        // Step 1: Wait for 1 second
        yield return new WaitForSeconds(1f);

        // Step 2: Fade in first picture from black to white and type text1
        yield return StartCoroutine(FadeToColor(firstPicture, Color.white, fadeDuration));
        yield return StartCoroutine(TypeTextCoroutine(text1));

        // Step 3: Fade out first picture and text1
        yield return StartCoroutine(FadeOut(firstPicture));
        yield return StartCoroutine(FadeOutText());

        // Disable first picture
        firstPicture.gameObject.SetActive(false);

        // Step 4: Clear text1 and wait for 1 second
        textBox.text = ""; // Clear text1
        yield return new WaitForSeconds(delayBetweenSteps);

        // Step 5: Fade in second picture and text1 to full alpha
        secondPicture.gameObject.SetActive(true);
        yield return StartCoroutine(FadeToColor(secondPicture, new Color(1, 1, 1, 1), fadeDuration));
        yield return StartCoroutine(FadeToAlpha(textBox, 1f, fadeDuration)); // Fade text back in

        // Start typing text2
        yield return StartCoroutine(TypeTextCoroutine(text2));

        // Step 6: Enable third picture and fade in, fade out second picture and disable it
        thirdPicture.gameObject.SetActive(true);
        yield return StartCoroutine(FadeToColor(thirdPicture, Color.white, fadeDuration));
        yield return StartCoroutine(FadeOut(secondPicture));
        secondPicture.gameObject.SetActive(false);

        // Step 7: Disable text2 and wait for 1 second
        yield return StartCoroutine(FadeOutText()); // Fade out text2
        textBox.text = ""; // Clear text2
        yield return new WaitForSeconds(delayBetweenSteps);

        // Step 8: Fade out third picture from full alpha to 0 and disable it
        yield return StartCoroutine(FadeOut(thirdPicture));
        thirdPicture.gameObject.SetActive(false);
    }

    private IEnumerator TypeTextCoroutine(string textToType)
    {
        textBox.text = ""; // Clear the text box at the start

        foreach (char letter in textToType)
        {
            textBox.text += letter;

            // Play typing sound every few letters
            if (soundClips != null && soundClips.Count > 0)
            {
                int randomIndex = Random.Range(0, soundClips.Count);
                float randomPitch = Random.Range(0.7f, 1f);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(soundClips[randomIndex], randomPitch);
                }
            }

            // If user clicks while typing, stop and display the full sentence
            if (Input.GetMouseButtonDown(0))
            {
                textBox.text = textToType; // Display full text
                break;
            }

            yield return new WaitForSeconds(typingSpeed); // Wait for typing speed
        }
    }

    private IEnumerator FadeOut(Image img)
    {
        Color color = img.color;
        float startAlpha = color.a;
        float endAlpha = 0f;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / fadeDuration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);
            img.color = color;
            yield return null; // Wait for the next frame
        }

        color.a = endAlpha; // Ensure the final alpha is set to 0
        img.color = color;
    }

    private IEnumerator FadeOutText()
    {
        Color color = textBox.color;
        float startAlpha = color.a;
        float endAlpha = 0f;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / fadeDuration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);
            textBox.color = color;
            yield return null; // Wait for the next frame
        }

        color.a = endAlpha; // Ensure the final alpha is set to 0
        textBox.color = color;
    }

    private IEnumerator FadeToColor(Image img, Color targetColor, float duration)
    {
        Color startColor = img.color;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float normalizedTime = t / duration;
            img.color = Color.Lerp(startColor, targetColor, normalizedTime);
            yield return null; // Wait for the next frame
        }

        img.color = targetColor; // Ensure the final color is set
    }

    private IEnumerator FadeToAlpha(TextMeshProUGUI text, float targetAlpha, float duration)
    {
        Color startColor = text.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float normalizedTime = t / duration;
            text.color = Color.Lerp(startColor, targetColor, normalizedTime);
            yield return null; // Wait for the next frame
        }

        text.color = targetColor; // Ensure the final alpha is set
    }
}
