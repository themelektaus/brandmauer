namespace Brandmauer;

public struct StringValue
{
    public string Value { get; set; }
    public string Description { get; set; }

    public StringValue() : this(default, default) { }
    public StringValue(string value) : this(value, default) { }
    public StringValue(string value, string description)
    {
        Value = value ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public override readonly string ToString() => Value;

    public static explicit operator StringValue(string s) => new(s);
}
