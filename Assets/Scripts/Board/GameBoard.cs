// GameBoard.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    public static GameBoard Instance { get; private set; }

    public static Action<GridCell> OnCellRevealed;
    public static Action<GridCell> OnTrapTriggered;
    public static Action<GridCell> OnCellFlagged;
    public static Action OnLevelCompleted;
    public static Action OnGameOver;

    [SerializeField] private GameObject cellPrefab;
    [SerializeField] public LevelConfiguration levelConfig;
    [SerializeField] private BoardVisualizer boardVisualizer;

    private GridCell[,] grid;
    private int width, height;
    private int totalTraps;
    private int revealedCount;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private void Start()
    {
        InitializeBoard();
    }

    public void InitializeBoard()
    {
        width = levelConfig.Width;
        height = levelConfig.Height;
        totalTraps = DifficultyPreset.CalculateTrapCount(width, height, levelConfig.DifficultyMultiplier);

        grid = new GridCell[width, height];
        BoardGenerator.Generate(this, cellPrefab, width, height);
        TrapManager.PlaceTraps(this, totalTraps);
        CellRevealSystem.Setup(this);

        boardVisualizer.Initialize(grid);
    }

    internal void RegisterCell(int x, int y, GridCell cell)
    {
        grid[x, y] = cell;

    }

    public void HandleCellReveal(GridCell cell)
    {
        if (cell.IsFlagged) return;
        if (cell.IsTrapped)
        {
            OnTrapTriggered?.Invoke(cell);
            OnGameOver?.Invoke();
        }
        else
        {
            CellRevealSystem.RevealCells(this, cell);
            CheckWin();
        }
    }
    public void IncrementRevealed()
    {
        revealedCount++;
        CheckWin();
    }

    public void HandleCellFlag(GridCell cell)
    {
        cell.ToggleFlag();
    }

    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        return grid[x, y];
    }


    private void CheckWin()
    {
        revealedCount++;
        if (revealedCount >= width * height - totalTraps)
            OnLevelCompleted?.Invoke();
    }
}