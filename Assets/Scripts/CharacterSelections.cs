using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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

    public void ShowGirlAbilityText()
    {
        girlAbilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void HideGirlAbilityText()
    {
        girlAbilityText.SetActive(false);
    }

    public void ShowMystAbilityText()
    {
        mystAbilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void HideMystAbilityText()
    {
        mystAbilityText.SetActive(false);
    }

    public void ShowOldAbilityText()
    {
        oldAbilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void HideOldAbilityText()
    {
        oldAbilityText.SetActive(false);
    }

    public void ShowGaleAbilityText()
    {
        galeAbilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void HideGaleAbilityText()
    {
        galeAbilityText.SetActive(false);
    }

    public void ShowGoblinAbilityText()
    {
        goblinAbilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);  
    }

    public void HideGoblinAbilityText()
    {
        goblinAbilityText.SetActive(false);
    }

    public void SelectSound()
    {
        AudioManager.Instance.PlaySound(AudioManager.SoundType.CharacterPick, 1f);
    }    
}

