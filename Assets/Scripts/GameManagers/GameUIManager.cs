using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using Cinemachine;
using System.Collections; // Required for Coroutines

public class GameUIManager : MonoBehaviour
{
    [Header("UI Panels (in Level1 scene)")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [Header("Gameplay UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI flagCountText;

    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    private float baseOrthoSize;
    private float zoomedOrthoSize;
    [SerializeField] private float zoomDuration = 1f;

    [Header("Win Effects")]
    [Tooltip("The Particle System Prefab to instantiate for the salute effect.")]
    [SerializeField] private GameObject saluteParticlePrefab;
    [Tooltip("The TextMeshProUGUI element in the WinPanel to position particles around.")]
    [SerializeField] private TextMeshProUGUI winPanelText; // Assign your "You Win!" text here
    [SerializeField] private float particleOffsetFromTextX = 150f; // Adjust based on your UI scale
    [SerializeField] private float particleOffsetY = 0f; // Adjust for vertical positioning

    private GameBoard gameBoard;
    private bool isAnimating = false;

    private void Awake()
    {
        gameBoard = FindObjectOfType<GameBoard>();
        HideAllPanelsInstant();

        if (virtualCamera == null)
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (virtualCamera != null)
        {
            baseOrthoSize = virtualCamera.m_Lens.OrthographicSize;
            zoomedOrthoSize = baseOrthoSize - 1f;
        }
    }

    private void Update()
    {
        HandlePauseToggle();

        if (GameStateManager.Instance.CurrentState == GameState.Playing && !pausePanel.activeSelf)
        {
            UpdateGameplayUI();
        }
    }

    private void UpdateGameplayUI()
    {
        if (timerText != null)
            timerText.text = GameStateManager.Instance.GetFormattedGameTime();

        UpdateFlagCount();
    }

    private void HandlePauseToggle()
    {
        if (Input.GetKeyDown(KeyCode.P) && !isAnimating)
        {
            GameState currentState = GameStateManager.Instance.CurrentState;

            if (currentState == GameState.Playing)
            {
                if (gameBoard != null && gameBoard.IsFloodFilling)
                {
                    Debug.Log("Cannot pause during flood fill operation");
                    return;
                }

                OpenPauseMenu();
            }
            else if (currentState == GameState.Paused && pausePanel.activeSelf)
            {
                ClosePauseMenu();
            }
        }
    }

    private void OpenPauseMenu()
    {
        Debug.Log("Opening pause menu");
        GameStateManager.Instance.PauseGame();
        ShowPanel(pausePanel);

        if (virtualCamera != null)
        {
            DOTween.To(
                () => virtualCamera.m_Lens.OrthographicSize,
                x => virtualCamera.m_Lens.OrthographicSize = x,
                zoomedOrthoSize,
                zoomDuration
            )
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
        }
    }

    private void ClosePauseMenu()
    {
        Debug.Log("Closing pause menu");
        HidePanel(pausePanel);
        GameStateManager.Instance.ResumeGame();

        if (virtualCamera != null)
        {
            DOTween.To(
                () => virtualCamera.m_Lens.OrthographicSize,
                x => virtualCamera.m_Lens.OrthographicSize = x,
                baseOrthoSize,
                zoomDuration
            )
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
        }
    }

    private void OnEnable()
    {
        GameStateManager.OnGameWon += HandleGameWon;
        GameStateManager.OnGameLost += HandleGameLost;
        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameStateManager.OnGameWon -= HandleGameWon;
        GameStateManager.OnGameLost -= HandleGameLost;
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void OnDestroy()
    {
        GameStateManager.OnGameWon -= HandleGameWon;
        GameStateManager.OnGameLost -= HandleGameLost;
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState previousState, GameState newState)
    {
        Debug.Log($"UI Manager: State changed from {previousState} to {newState}");

        switch (newState)
        {
            case GameState.Playing:
                if (previousState == GameState.Paused)
                {
                    HidePanel(pausePanel);
                }
                break;

            case GameState.Paused:
                break;

            case GameState.Won:
            case GameState.Lost:
                // Panels are shown via ShowEndGamePanel, no need to hide here
                break;
        }
    }

    private void HandleGameWon(float gameTime)
    {
        Debug.Log($"Game won in {gameTime:F1} seconds");
        ShowEndGamePanel(winPanel);
        TriggerWinParticles(); // New method to call particle effects!
    }

    private void HandleGameLost()
    {
        Debug.Log("Game lost");
        ShowEndGamePanel(losePanel);
    }

    private void ShowEndGamePanel(GameObject panel)
    {
        HideAllPanelsInstant();
        ShowPanel(panel);
        Debug.Log($"Showing end game panel: {panel.name}");
    }

    private void HideAllPanelsInstant()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("Attempted to show null panel");
            return;
        }

        panel.SetActive(true);
        AnimateChildren(panel);

        var cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
        cg.interactable = true;
        cg.blocksRaycasts = true;

        Debug.Log($"Panel {panel.name} shown and interaction enabled");
    }

    private void HidePanel(GameObject panel)
    {
        if (panel == null) return;

        panel.SetActive(false);
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        Debug.Log($"Panel {panel.name} hidden and interaction disabled");
    }

    private void AnimateChildren(GameObject panel)
    {
        isAnimating = true;
        var children = panel.GetComponentsInChildren<Transform>(true);

        foreach (var t in children)
        {
            if (t == panel.transform) continue;
            t.localScale = Vector3.zero;
        }

        var seq = DOTween.Sequence();
        seq.SetUpdate(true);

        foreach (var t in children)
        {
            if (t == panel.transform) continue;
            seq.Join(t.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true));
        }
        foreach (var t in children)
        {
            if (t == panel.transform) continue;
            seq.Join(t.DOScale(1f, 0.2f).SetUpdate(true));
        }
        seq.OnComplete(() => {
            isAnimating = false;
            Debug.Log($"Panel animation completed for {panel.name}");
        });
    }

    private void UpdateFlagCount()
    {
        if (flagCountText == null || gameBoard?.Grid == null) return;

        int totalTraps = 0;
        int usedFlags = 0;

        foreach (var cell in gameBoard.Grid.GetAllCells())
        {
            if (cell.type == Cell.CellType.Trap) totalTraps++;
            if (cell.flagged) usedFlags++;
        }

        int remainingFlags = totalTraps - usedFlags;
        flagCountText.text = $"Flags: {remainingFlags}";
    }

    private void TriggerWinParticles()
    {
        if (saluteParticlePrefab == null || winPanelText == null)
        {
            Debug.LogWarning("Salute particle prefab or WinPanel text is not assigned.");
            return;
        }

        if (!winPanel.activeInHierarchy)
        {
            Debug.LogWarning("WinPanel is not active. Cannot position particles.");
            return;
        }

        Canvas canvas = winPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("Canvas not found. Cannot convert UI to world space.");
            return;
        }

        RectTransform textRect = winPanelText.rectTransform;
        Vector3 textCenter = textRect.position;

        // Compute left/right screen positions relative to text
        Vector3 leftScreenPos = textCenter - new Vector3(winPanelText.preferredWidth / 2f + particleOffsetFromTextX, particleOffsetY, 0);
        Vector3 rightScreenPos = textCenter + new Vector3(winPanelText.preferredWidth / 2f + particleOffsetFromTextX, particleOffsetY, 0);

        // Convert screen positions to world space
        Vector3 leftWorldPos, rightWorldPos;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Camera is not needed
            leftWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(leftScreenPos.x, leftScreenPos.y, 10f));
            rightWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(rightScreenPos.x, rightScreenPos.y, 10f));
        }
        else
        {
            // Use camera-based raycasting
            RectTransformUtility.ScreenPointToWorldPointInRectangle(textRect, leftScreenPos, Camera.main, out leftWorldPos);
            RectTransformUtility.ScreenPointToWorldPointInRectangle(textRect, rightScreenPos, Camera.main, out rightWorldPos);
        }

        // Instantiate and play left particle system
        GameObject leftParticles = Instantiate(saluteParticlePrefab, leftWorldPos, Quaternion.identity);
        if (leftParticles.TryGetComponent<ParticleSystem>(out ParticleSystem leftPs))
        {
            leftPs.Play();
            Destroy(leftParticles, leftPs.main.duration);
        }

        // Instantiate and play right particle system
        GameObject rightParticles = Instantiate(saluteParticlePrefab, rightWorldPos, Quaternion.identity);
        if (rightParticles.TryGetComponent<ParticleSystem>(out ParticleSystem rightPs))
        {
            rightPs.Play();
            Destroy(rightParticles, rightPs.main.duration);
        }
    }


    public void OnResumeButtonClicked()
    {
        if (GameStateManager.Instance.CurrentState == GameState.Paused)
        {
            ClosePauseMenu();
        }
    }

    public void OnRestartButtonClicked()
    {
        HideAllPanelsInstant();
        GameStateManager.Instance.RestartGame();
    }

    public void OnMainMenuButtonClicked()
    {
        HideAllPanelsInstant();
        GameStateManager.Instance.ReturnToMenu();
    }
}