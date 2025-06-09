// MagicBarrier.cs
using UnityEngine;
using System.Collections;
using DG.Tweening;

public class MagicBarrier : MonoBehaviour
{
    [SerializeField] private GameObject destructionEffectPrefab;
    [SerializeField] private float animationDuration = 3f;
    [SerializeField] private float shakeStrength = 0.5f;
    [SerializeField] private int shakeVibrato = 20;
    [SerializeField] private float shakeRandomness = 150f;

    private Collider barrierCollider;
    private MeshRenderer barrierRenderer;
    private bool isDestroyed = false;
    private Vector3 originalScale;

    void Awake()
    {
        barrierCollider = GetComponent<Collider>();
        barrierRenderer = GetComponent<MeshRenderer>();
        originalScale = transform.localScale;

        if (barrierRenderer != null) barrierRenderer.enabled = true;
        if (barrierCollider != null) barrierCollider.enabled = true;
        isDestroyed = false;
    }

    void OnEnable()
    {
        if (GameBoard.Instance != null)
        {
            GameBoard.Instance.Actions.OnGameWon.AddListener(OnGameWonAction);
        }
        else
        {
            StartCoroutine(TrySubscribeWithDelay());
        }
    }

    void OnDisable()
    {
        if (GameBoard.Instance != null)
        {
            GameBoard.Instance.Actions.OnGameWon.RemoveListener(OnGameWonAction);
        }
        transform.DOKill();
    }

    private void OnGameWonAction()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log("Magic barrier received 'Game Won' event! Initiating destruction sequence with animation.");

        if (barrierCollider != null) barrierCollider.enabled = false;

        Sequence destructionSequence = DOTween.Sequence();

        // Optional: short pause before effect starts
        destructionSequence.AppendInterval(0.3f);

        // Optional: flash emission glow
        if (barrierRenderer != null && barrierRenderer.material.HasProperty("_EmissionColor"))
        {
            Color originalEmission = barrierRenderer.material.GetColor("_EmissionColor");
            Color intenseEmission = originalEmission * 5f;
            destructionSequence.Append(DOTween.To(() => originalEmission, x => barrierRenderer.material.SetColor("_EmissionColor", x), intenseEmission, 0.3f));
        }

        // Dramatic scale-up
        destructionSequence.Append(transform.DOScale(originalScale * 2.5f, animationDuration)
                                    .SetEase(Ease.OutBack));

        // Strong shake effect
        destructionSequence.Append(transform.DOShakeScale(animationDuration * 0.5f,
                                    strength: shakeStrength * 2f,
                                    vibrato: shakeVibrato + 10,
                                    randomness: shakeRandomness * 2f,
                                    fadeOut: false)
                                    .SetEase(Ease.InOutSine));

        // Destroy + FX
        destructionSequence.AppendCallback(() => {
            if (destructionEffectPrefab != null)
            {
                Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Optional: fade out mesh or dissolve
            if (barrierRenderer != null) barrierRenderer.enabled = false;

            // Optional: Camera shake via your camera system

            Destroy(gameObject);
            Debug.Log("Magic barrier completely destroyed and removed from scene.");
        });

        destructionSequence.Play();
    }


    public void ResetBarrier()
    {
        transform.DOKill();

        if (barrierRenderer != null) barrierRenderer.enabled = true;
        if (barrierCollider != null) barrierCollider.enabled = true;
        transform.localScale = originalScale;
        isDestroyed = false;
        gameObject.SetActive(true);
        Debug.Log("Magic barrier reset for new game.");
    }

    private IEnumerator TrySubscribeWithDelay()
    {
        yield return new WaitForSeconds(0.1f);
        if (GameBoard.Instance != null)
        {
            GameBoard.Instance.Actions.OnGameWon.AddListener(OnGameWonAction);
            Debug.Log("MagicBarrier successfully SUBSCRIBED to OnGameWon after delay.", this);
        }
        else
        {
            Debug.LogError("MagicBarrier still FAILED to subscribe after delay. GameBoard might not be in scene or its Awake is not guaranteed before this.", this);
        }
    }
}