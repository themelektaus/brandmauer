namespace Brandmauer;

[AttributeUsage(AttributeTargets.Class)]
public class IntervalAttribute(int seconds) : Attribute
{
    public readonly int seconds = seconds;
}
