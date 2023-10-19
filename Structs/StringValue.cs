namespace Brandmauer;

public struct StringValue
{
    public string Value { get; set; }

    public StringValue()
    {
        Value = string.Empty;
    }

    public StringValue(string value)
    {
        Value = value;
    }

    public override readonly string ToString() => Value;

    public static explicit operator StringValue(string s) => new(s);
}
