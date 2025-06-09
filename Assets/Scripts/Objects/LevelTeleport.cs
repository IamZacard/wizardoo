using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening; // Don't forget to import DOTween if you haven't!

public class PortalBehaviour : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Total duration of the player's rotation animation before teleport.")]
    [SerializeField] private float totalAnimationDuration = 2f;
    [Tooltip("Time at which the X/Y rotation starts (within totalAnimationDuration).")]
    [SerializeField] private float xyRotationStartTime = 1f;
    [Tooltip("Rotation speed around Z-axis during the first phase.")]
    [SerializeField] private float rotationSpeedZPhase1 = 180f; // Degrees per second
    [Tooltip("Rotation speed around Z-axis during the second phase.")]
    [SerializeField] private float rotationSpeedZPhase2 = 360f; // Degrees per second
    [Tooltip("Rotation speed around X and Y axes during the second phase.")]
    [SerializeField] private float rotationSpeedXYPhase2 = 180f; // Degrees per second
    [Tooltip("Ease type for the rotational animation.")]
    [SerializeField] private Ease rotationEase = Ease.Linear;
    [Tooltip("Optional: Scale player down during the animation.")]
    [SerializeField] private bool scalePlayerDown = true;
    [SerializeField] private float scaleDuration = 0.5f;
    [SerializeField] private Ease scaleEase = Ease.InBack;

    [Header("Transition Settings")]
    [Tooltip("Should the screen fade to black before loading the next scene?")]
    [SerializeField] private bool useFadeTransition = true;
    [Tooltip("Duration of the screen fade.")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Next Level")]
    [Tooltip("The name of the next scene to load. Leave empty to load next scene in build order.")]
    [SerializeField] private string nextSceneName = "";

    private bool isTeleporting = false; // Prevents multiple triggers

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTeleporting) return; // Already in transition

        if (other.gameObject.CompareTag("Player"))
        {
            isTeleporting = true;
            StartCoroutine(TeleportSequence(other.gameObject));
        }
    }

    private IEnumerator TeleportSequence(GameObject player)
    {
        // Play portal sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(AudioManager.SoundType.PortalSound, 1f);
        }

        // Store original player properties
        Vector3 originalPlayerScale = player.transform.localScale;
        Quaternion originalPlayerRotation = player.transform.rotation;

        // Create a DOTween sequence for animation
        Sequence teleportAnimSequence = DOTween.Sequence();

        // Phase 1: Z-rotation only
        teleportAnimSequence.Append(
            player.transform.DORotate(new Vector3(0, 0, rotationSpeedZPhase1 * (xyRotationStartTime)), xyRotationStartTime, RotateMode.LocalAxisAdd)
                .SetEase(rotationEase)
        );

        // Phase 2: X, Y, Z rotation
        teleportAnimSequence.Append(
            player.transform.DORotate(new Vector3(rotationSpeedXYPhase2 * (totalAnimationDuration - xyRotationStartTime),
                                                  rotationSpeedXYPhase2 * (totalAnimationDuration - xyRotationStartTime),
                                                  rotationSpeedZPhase2 * (totalAnimationDuration - xyRotationStartTime)),
                                      totalAnimationDuration - xyRotationStartTime, RotateMode.LocalAxisAdd)
                .SetEase(rotationEase)
        );

        // Optional: Scale down the player during the animation
        if (scalePlayerDown)
        {
            teleportAnimSequence.Join(player.transform.DOScale(0f, scaleDuration).SetEase(scaleEase));
        }

        // Wait for the animation to complete
        yield return teleportAnimSequence.WaitForCompletion();

        player.transform.localScale = originalPlayerScale; // Reset scale for next level
        player.transform.rotation = originalPlayerRotation; // Reset rotation for next level

        // Handle scene transition
        if (useFadeTransition)
        {
            Debug.Log("Fading screen for transition...");
            yield return new WaitForSeconds(fadeDuration); // Placeholder wait
        }

        LoadNextLevel();
    }

    private void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex + 1);
        }
    }

    // Call this if you need to reset the portal's state without reloading the scene
    public void ResetPortal()
    {
        isTeleporting = false;
    }
}