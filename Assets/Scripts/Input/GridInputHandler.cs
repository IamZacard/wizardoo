using UnityEngine;

public class GridInputHandler : MonoBehaviour, IInputReceiver
{
    [SerializeField] private LayerMask cellMask;

    private void Start()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.RegisterReceiver(this);
        else
            Debug.LogWarning("InputManager.Instance is null! InputManager must exist in scene.");
    }

    public void HandleInput(InputData inputData)
    {
        RaycastHit2D hit = Physics2D.Raycast(inputData.WorldPosition, Vector2.zero, 0f, cellMask);
        if (!hit) return;

        var cell = hit.collider.GetComponent<GridCell>();
        if (cell == null) return;

        if (InputValidator.IsValid(cell))
        {
            if (inputData.IsLeftClick)
                GameBoard.Instance.HandleCellReveal(cell);
            else if (inputData.IsRightClick)
                GameBoard.Instance.HandleCellFlag(cell);
        }
    }
}
