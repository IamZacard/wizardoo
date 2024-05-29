using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GoblinBehaviour : MonoBehaviour
{
    //goblin bools
    //public bool invincible;
    public int goblinIndex;
    //[Header("UI")]
    private TextMeshProUGUI charactersText;
    void Start()
    {
        //invincible = false;
        goblinIndex = CharacterManager.selectedCharacterIndex;

        // Find the TextMeshProUGUI component with the specified tag
        TextMeshProUGUI[] textComponents = FindObjectsOfType<TextMeshProUGUI>();
        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            if (textComponent.CompareTag("AbilityText"))
            {
                charactersText = textComponent;
                break; // Exit the loop once the desired text is found
            }
        }

        // Log a warning if the TextMeshProUGUI component is not found
        if (charactersText == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }

        charactersText.text = "50% to flag trap";
    }
}
