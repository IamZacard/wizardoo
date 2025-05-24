using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class GridCell : MonoBehaviour, IPointerClickHandler
{
    public bool IsRevealed { get; private set; }
    public bool IsFlagged { get; private set; }
    public bool IsTrapped { get; internal set; }
    public int AdjacentTrapCount { get; internal set; }
    public int X { get; set; }
    public int Y { get; set; }

    private ICellInteractable _interactable;

    public void SetInteractable(ICellInteractable interactable)
    {
        _interactable = interactable;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            GameBoard.Instance.HandleCellReveal(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            GameBoard.Instance.HandleCellFlag(this);
        }
    }

    public void Reveal()
    {
        if (IsRevealed || IsFlagged) return;
        IsRevealed = true;

        transform.DOScale(Vector3.one * 1.1f, 0.1f).SetLoops(2, LoopType.Yoyo);
        GameBoard.Instance.IncrementRevealed();
        _interactable?.OnCellRevealed(this);
    }

    public void ToggleFlag()
    {
        if (IsRevealed) return;
        IsFlagged = !IsFlagged;

        transform.DOPunchRotation(Vector3.forward * 10f, 0.3f, 5);
        _interactable?.OnCellFlagged(this);
    }
}
