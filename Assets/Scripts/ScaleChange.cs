using System.Collections;
using UnityEngine;

public class ScaleChange : MonoBehaviour
{
    public float scaleFactor = 0.9f; // The scale factor for the smaller scale
    public float changeDuration = 0.5f; // The duration for each scaling phase
    public float delayBetweenChanges = 1.0f; // The delay between scale changes

    private Vector3 initialScale;
    private Coroutine scaleCoroutine;

    private void Start()
    {
        initialScale = transform.localScale;
        StartScaling();
    }

    private void StartScaling()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleObject());
    }

    private IEnumerator ScaleObject()
    {
        while (true)
        {
            yield return ScaleTo(scaleFactor, changeDuration);
            yield return new WaitForSeconds(delayBetweenChanges);
            yield return ScaleTo(1.0f, changeDuration);
            yield return new WaitForSeconds(delayBetweenChanges);
        }
    }

    private IEnumerator ScaleTo(float targetScale, float duration)
    {
        float timer = 0.0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = initialScale * targetScale;

        while (timer < duration)
        {
            float scaleFactor = Mathf.Lerp(0, 1, timer / duration);
            transform.localScale = Vector3.Lerp(startScale, endScale, scaleFactor);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = endScale;
    }
}
