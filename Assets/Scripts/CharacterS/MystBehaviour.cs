using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class MystBehaviour : MonoBehaviour
{
    public bool invincible;
    public int stepsOfInvi = 5;
    private int moveCount;
    private bool abilityUsed;
    private Game gameRules;

    [SerializeField] private Image spellIcon;
    public Color readyColor = Color.white;
    public Color notReadyColor = Color.grey;
    public float pulseDuration = 2f;

    private Coroutine pulseCoroutine;
    private TextMeshProUGUI charactersText;
    private Animator anim;

    public GameObject inviEffectPrefab;
    private GameObject inviEffectInstance;

    private void Start()
    {
        invincible = false;
        moveCount = 0;
        abilityUsed = false;

        anim = GetComponent<Animator>();
        gameRules = GameObject.FindGameObjectWithTag("GameRules")?.GetComponent<Game>();

        charactersText = FindTextComponentWithTag("AbilityText");

        if (charactersText != null)
        {
            charactersText.text = "Activate ability to become INVINCIBLE";
        }

        if (gameRules == null)
        {
            Debug.LogWarning("No GameObject with tag 'GameRules' found!");
        }

        if (charactersText == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }

        UpdateSpellIcon();
    }

    private void Update()
    {
        if (invincible && IsMovementKeyPressed())
        {
            moveCount++;
            charactersText.text = "Steps left: " + (stepsOfInvi - moveCount);

            if (moveCount >= stepsOfInvi)
            {
                DisableInvincibility();
            }

            UpdateSpellIcon();
        }

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && !invincible && !abilityUsed && !gameRules.gameover)
        {
            ActivateInvincibility();
        }

        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ResetAbility();
        }
    }

    private void ActivateInvincibility()
    {
        invincible = true;
        moveCount = 0;
        abilityUsed = true;

        anim.SetBool("Invincible", true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.MysticInvincible, 1f);
        charactersText.text = "You are INVINCIBLE NOW!";

        if (inviEffectPrefab != null)
        {
            inviEffectInstance = Instantiate(inviEffectPrefab, transform);
            inviEffectInstance.transform.localPosition = Vector3.zero;
        }
    }

    private void DisableInvincibility()
    {
        invincible = false;
        moveCount = 0;

        anim.SetBool("Invincible", false);
        charactersText.text = "Care! You are not invincible anymore!";

        if (inviEffectInstance != null)
        {
            Destroy(inviEffectInstance);
        }
    }

    private TextMeshProUGUI FindTextComponentWithTag(string tag)
    {
        TextMeshProUGUI[] textComponents = FindObjectsOfType<TextMeshProUGUI>();
        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            if (textComponent.CompareTag(tag))
            {
                return textComponent;
            }
        }
        return null;
    }

    private bool IsMovementKeyPressed()
    {
        return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) ||
               Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) ||
               Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
    }

    public void ResetAbility()
    {
        abilityUsed = false;
        invincible = false;
        moveCount = 0;

        anim.SetBool("Invincible", false);
        charactersText.text = "Activate ability to become INVINCIBLE";

        if (inviEffectInstance != null)
        {
            Destroy(inviEffectInstance);
        }

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        pulseCoroutine = StartCoroutine(PulseIcon());
    }

    private void UpdateSpellIcon()
    {
        spellIcon.color = abilityUsed ? notReadyColor : readyColor;

        if (!abilityUsed && pulseCoroutine == null)
        {
            pulseCoroutine = StartCoroutine(PulseIcon());
        }
        else if (abilityUsed && pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    private IEnumerator PulseIcon()
    {
        while (true)
        {
            float elapsedTime = 0f;
            while (elapsedTime < pulseDuration)
            {
                spellIcon.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.2f, Mathf.PingPong(elapsedTime, pulseDuration / 2));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }
}
