using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    private PlayerMovement controls;

    void Awake() => controls = new PlayerMovement();
    void OnEnable()
    {
        controls.Enable();
        controls.Main.Flag.performed += ctx => Debug.LogWarning("Flag action works!");
    }
    void OnDisable()
    {
        controls.Main.Flag.performed -= ctx => Debug.LogWarning("Flag action works!");
        controls.Disable();
    }
}