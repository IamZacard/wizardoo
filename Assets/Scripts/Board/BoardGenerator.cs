// BoardGenerator.cs
using UnityEngine;

public static class BoardGenerator
{
    public static void Generate(GameBoard board, GameObject prefab, int width, int height)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var go = Object.Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity);
                var cell = go.GetComponent<GridCell>();
                board.RegisterCell(x, y, cell);
            }
    }
}