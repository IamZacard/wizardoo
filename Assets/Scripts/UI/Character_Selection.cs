using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Character_Selection : MonoBehaviour
{
    public GameObject[] characterPrefabs;
    private int selectedCharacterIndex = -1;

    [SerializeField] private GameObject noCharacterSelectedText;
    [SerializeField] private GameObject startButton;
    private Vector3 originalScaleStart;

    private void Awake()
    {
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
            CharacterManager.SelectedCharacterPrefab = characterPrefabs[selectedCharacterIndex];
            CharacterManager.selectedCharacterIndex = selectedCharacterIndex;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else
        {
            StartCoroutine(NoSelectedCharacter());
            AudioManager.Instance.PlaySound(AudioManager.SoundType.ErrorSound, 1f);
        }
    }

    private IEnumerator NoSelectedCharacter()
    {
        noCharacterSelectedText.SetActive(true);
        yield return new WaitForSeconds(2);
        noCharacterSelectedText.SetActive(false);
    }

    public void StartHoverOver()
    {
        ScaleButton(1.25f);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void StartHoverExit()
    {
        ScaleButton(1f);
    }

    private void ScaleButton(float scale)
    {
        RectTransform rectTransform = startButton.GetComponent<RectTransform>();
        rectTransform.localScale = originalScaleStart * scale;
    }

    public void SelectSound()
    {
        AudioManager.Instance.PlaySound(AudioManager.SoundType.CharacterPick, 1f);
    }
}
