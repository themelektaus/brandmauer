namespace Brandmauer;

public struct Identifier
{
    static long lastId;

    public long Id { get; set; }

    public static long NextId() => ++lastId;

    public readonly void UpdateLastId() => lastId = Math.Max(lastId, Id);
}
