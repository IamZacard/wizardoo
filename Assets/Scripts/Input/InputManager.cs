using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    private IInputReceiver inputReceiver;

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterReceiver(IInputReceiver receiver)
    {
        inputReceiver = receiver;
    }

    private void Update()
    {
        if (Camera.main == null || inputReceiver == null) return;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var data = new InputData(
                new Vector2(worldPos.x, worldPos.y),
                Input.GetMouseButtonDown(0),
                Input.GetMouseButtonDown(1)
            );
            inputReceiver.HandleInput(data);
        }
    }
}
