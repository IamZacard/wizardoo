using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIBaseManager : MonoBehaviour
{
    public TextMeshProUGUI trapText;
    public TextMeshProUGUI flagText;
    public GameObject lostPanel;
    public GameObject solvedPanel;

    public void UpdateTrapCount(int traps)
    {
        trapText.text = "Traps: " + traps;
    }

    public void ShowGameOverPanel()
    {
        lostPanel.SetActive(true);
    }

    public void ShowLevelCompletePanel()
    {
        solvedPanel.SetActive(true);
    }
}

