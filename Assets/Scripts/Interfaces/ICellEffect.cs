public interface ICellEffect
{
    void OnRevealed();
    void OnPlayerEntered(CharacterBase character);
    void OnFlagged();
    void OnUnflagged();
}