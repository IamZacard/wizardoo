using UnityEngine;

public class BoardVisualizer : MonoBehaviour
{
    [SerializeField] private CellVisualData visualData;

    private void OnEnable()
    {
        GameBoard.OnCellRevealed += UpdateVisual;
        GameBoard.OnTrapTriggered += UpdateVisual;
    }

    private void OnDisable()
    {
        GameBoard.OnCellRevealed -= UpdateVisual;
        GameBoard.OnTrapTriggered -= UpdateVisual;
    }

    public void Initialize(GridCell[,] grid)
    {
        foreach (var cell in grid)
        {
            cell.GetComponent<SpriteRenderer>().sprite = visualData.GetSprite(cell);
        }
    }

    private void UpdateVisual(GridCell cell)
    {
        var sr = cell.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.sprite = visualData.GetSprite(cell);
        sr.color = Color.white; // Ensure traps or flagged colors reset to visible
    }
}
