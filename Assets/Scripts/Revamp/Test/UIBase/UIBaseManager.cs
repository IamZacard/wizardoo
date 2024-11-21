using TMPro;
using UnityEngine;

public class UIBaseManager : MonoBehaviour
{
    public TextMeshProUGUI flagText;
    public GameObject lostPanel;
    public GameObject solvedPanel;

    [SerializeField] private GameObject stepEffect;
    [SerializeField] private GameObject revealEffect;
    [SerializeField] private GameObject flagEffect;
    [SerializeField] private GameObject explodeEffect;

    private void OnEnable()
    {
        if (PlayerEventManager.Instance != null)
        {
            PlayerEventManager.MoveEvent += OnMove;
            PlayerEventManager.RevealEvent += OnReveal;
            PlayerEventManager.FlagEvent += OnFlag;
            PlayerEventManager.ExplodeEvent += OnExplode;
            PlayerEventManager.InteractEvent += OnInteract;
        }
    }

    private void OnDisable()
    {
        if (PlayerEventManager.Instance != null)
        {
            PlayerEventManager.MoveEvent -= OnMove;
            PlayerEventManager.RevealEvent -= OnReveal;
            PlayerEventManager.FlagEvent -= OnFlag;
            PlayerEventManager.ExplodeEvent -= OnExplode;
            PlayerEventManager.InteractEvent -= OnInteract;
        }
    }

    private void OnMove()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player"); // Ensure Player has this tag
        if (player != null)
        {
            Vector3 stepPosition = player.transform.position + new Vector3(0f, -0.24f, 0f);
            Instantiate(stepEffect, stepPosition, Quaternion.identity);
        }
    }

    // Similar methods for other events:
    private void OnReveal() {  }
    private void OnFlag() {  }
    private void OnExplode()
    {        
        Instantiate(explodeEffect, transform.position, Quaternion.identity);
    }
    private void OnInteract() {  }    

    public void ShowGameOverPanel()
    {
        lostPanel.SetActive(true);
    }

    public void ShowLevelCompletePanel()
    {
        solvedPanel.SetActive(true);
    }
}

