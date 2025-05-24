using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public int Moves { get; private set; }
    public float TimeElapsed { get; private set; }
    public int Deaths { get; private set; }

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Update()
    {
        if (GameStateManager.Instance.IsPlaying)
            TimeElapsed += Time.deltaTime;
    }

    public void AddMove() => Moves++;
    public void AddDeath() => Deaths++;
}
