namespace Brandmauer;

public struct Identifier
{
	static long lastId;

	public long Id { get; set; }

	public static long NextId() => ++lastId;

	public void UpdateLastId() => lastId = Math.Max(lastId, Id);
}
