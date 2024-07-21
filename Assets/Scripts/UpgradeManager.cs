using UnityEngine;
using UnityEngine.Events;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }
    public UnityEvent OnUpgradeReady;
    public UnityEvent ZephyrTrigger;
    public UnityEvent SorayaTrigger;

    private void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Make it persistent between scenes
        }
        else
        {
            Destroy(gameObject);
        }

        if (OnUpgradeReady == null)
            OnUpgradeReady = new UnityEvent();

        if (ZephyrTrigger == null)
            ZephyrTrigger = new UnityEvent();

        if (SorayaTrigger == null)
            SorayaTrigger = new UnityEvent();
    }
}
