namespace Brandmauer;

[AttributeUsage(AttributeTargets.Class)]
public class DelayAttribute(int seconds) : Attribute
{
    public readonly int seconds = seconds;
}
