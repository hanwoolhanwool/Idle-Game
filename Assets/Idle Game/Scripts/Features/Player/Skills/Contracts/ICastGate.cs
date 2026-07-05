public interface ICastGate
{
    bool IsCasting { get; }
    void EnterCast();
    void ExitCast();
}