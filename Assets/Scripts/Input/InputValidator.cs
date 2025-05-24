public static class InputValidator
{
    public static bool IsValid(GridCell cell)
    {
        return !cell.IsRevealed;
    }
}
