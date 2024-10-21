using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface ICharacter
{
    void ShowAbility();
    void HideAbility();
}
public class CharacterButton : MonoBehaviour
{
    [SerializeField] private GameObject abilityText;
    [SerializeField] private GameObject button;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = button.GetComponent<RectTransform>().localScale;
    }

    public void ShowAbilityText()
    {
        ScaleButton(1.25f);
        abilityText.SetActive(true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void HideAbilityText()
    {
        ScaleButton(1f);
        abilityText.SetActive(false);
    }

    private void ScaleButton(float scale)
    {
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        rectTransform.localScale = originalScale * scale;
    }
}

