using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CharacterSelection : MonoBehaviour
{
    public GameObject[] characterPrefabs; // Array of character prefabs to choose from
    private int selectedCharacterIndex = -1; // Index of the selected character prefab

    [SerializeField] private GameObject girlAbilityText;
    [SerializeField] private GameObject mystAbilityText;
    [SerializeField] private GameObject oldAbilityText;
    [SerializeField] private GameObject galeAbilityText;
    [SerializeField] private GameObject goblinAbilityText;
    [SerializeField] private GameObject noCharacterSelectedText;

    [SerializeField] private GameObject girlButton;
    [SerializeField] private GameObject mystButton;
    [SerializeField] private GameObject oldButton;
    [SerializeField] private GameObject galeButton;
    [SerializeField] private GameObject goblinButton;

    [SerializeField] private GameObject startButton;

    private Vector3 originalScaleGirl;
    private Vector3 originalScaleMyst;
    private Vector3 originalScaleOld;
    private Vector3 originalScaleGale;
    private Vector3 originalScaleGoblin;
    private Vector3 originalScaleStart;

    private void Awake()
    {
        // Save the original scale for each button
        originalScaleGirl = girlButton.GetComponent<RectTransform>().localScale;
        originalScaleMyst = mystButton.GetComponent<RectTransform>().localScale;
        originalScaleOld = oldButton.GetComponent<RectTransform>().localScale;
        originalScaleGale = galeButton.GetComponent<RectTransform>().localScale;
        originalScaleGoblin = goblinButton.GetComponent<RectTransform>().localScale;

        originalScaleStart = startButton.GetComponent<RectTransform>().localScale;
    }

    public void SelectCharacter(int characterIndex)
    {
        selectedCharacterIndex = characterIndex;
    }

    public void ProceedToNextScene()
    {
        if (selectedCharacterIndex != -1 && selectedCharacterIndex < characterPrefabs.Length)
        {
            // Store the selected character prefab in a static variable to pass it to the next scene
            CharacterManager.SelectedCharacterPrefab = characterPrefabs[selectedCharacterIndex];
            CharacterManager.selectedCharacterIndex = selectedCharacterIndex;

            // Get the current scene's build index
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Load the next scene by incrementing the current build index
            SceneManager.LoadScene(currentSceneIndex + 1);
        }
        else
        {
            StartCoroutine(NoSelectedCharacter());
            AudioManager.Instance.PlaySound(AudioManager.SoundType.ErrorSound, 1f);
            Debug.LogWarning("No character selected!");
        }
    }

    private IEnumerator NoSelectedCharacter()
    {
        noCharacterSelectedText.SetActive(true);
        yield return new WaitForSeconds(2);
        noCharacterSelectedText.SetActive(false);
    }

    private void ScaleButton(GameObject button, Vector3 originalScale, float scale)
    {
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale * scale;
        }
    }

    public void ShowGirlAbilityText()
    {
        ScaleButton(girlButton, originalScaleGirl, 1.25f);
        girlAbilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void HideGirlAbilityText()
    {
        ScaleButton(girlButton, originalScaleGirl, 1f);
        girlAbilityText.SetActive(false);
    }

    public void ShowMystAbilityText()
    {
        ScaleButton(mystButton, originalScaleMyst, 1.25f);
        mystAbilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void HideMystAbilityText()
    {
        ScaleButton(mystButton, originalScaleMyst, 1f);
        mystAbilityText.SetActive(false);
    }

    public void ShowOldAbilityText()
    {
        ScaleButton(oldButton, originalScaleOld, 1.25f);
        oldAbilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void HideOldAbilityText()
    {
        ScaleButton(oldButton, originalScaleOld, 1f);
        oldAbilityText.SetActive(false);
    }

    public void ShowGaleAbilityText()
    {
        ScaleButton(galeButton, originalScaleGale, 1.25f);
        galeAbilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void HideGaleAbilityText()
    {
        ScaleButton(galeButton, originalScaleGale, 1f);
        galeAbilityText.SetActive(false);
    }

    public void ShowGoblinAbilityText()
    {
        ScaleButton(goblinButton, originalScaleGoblin, 1.25f);
        goblinAbilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void HideGoblinAbilityText()
    {
        ScaleButton(goblinButton, originalScaleGoblin, 1f);
        goblinAbilityText.SetActive(false);
    }

    public void StartHoverOver()
    {
        ScaleButton(startButton, originalScaleStart, 1.25f);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void StartHoverExit()
    {
        ScaleButton(startButton, originalScaleStart, 1f);
    }

    public void SelectSound()
    {
        AudioManager.Instance.PlaySound(AudioManager.SoundType.CharacterPick, 1f);
    }
}
