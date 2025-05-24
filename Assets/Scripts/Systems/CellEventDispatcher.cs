using System.Collections.Generic;
using UnityEngine;

public class CellEventDispatcher : MonoBehaviour
{
    private List<ICellInteractable> listeners = new();

    public void Register(ICellInteractable listener)
    {
        listeners.Add(listener);
    }

    public void DispatchCellRevealed(GridCell cell)
    {
        foreach (var listener in listeners)
            listener.OnCellRevealed(cell);
    }

    public void DispatchCellFlagged(GridCell cell)
    {
        foreach (var listener in listeners)
            listener.OnCellFlagged(cell);
    }
}
