namespace Brandmauer;

public struct MemoryValue
{
    public int Value { get; set; }
    public enum _Unit
    {
        Default = -1,
        Byte = 0,
        KB = 1,
        MB = 2,
        GB = 3,
        TB = 4
    }
    public _Unit Unit { get; set; }

    public long? GetBytes()
    {
        if (Unit == _Unit.Default)
            return null;

        var bytes = Value;
        var unit = (int) Unit;

        for (int i = 0; i < unit; i++)
            bytes *= 1000;

        return bytes;
    }
}
