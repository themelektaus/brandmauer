﻿namespace Brandmauer;

public abstract class Model : IDisposable
{
    public Identifier Identifier { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreationTimestamp { get; set; } = DateTime.UtcNow;

    public virtual string HtmlName => ToString();
    public virtual string HtmlInfo => string.Empty;

    public Model()
    {
        if (Database.Loading)
        {
            Database.Register(this);
            return;
        }

        Identifier = new()
        {
            Id = Identifier.NextId()
        };
    }

    public void Dispose()
    {
        Database.Unregister(this);
    }

    public void PostDeserialize(Database database)
    {
        Identifier.UpdateLastId();

        if (this is IOnDeserialize onDeserialize)
            onDeserialize.OnDeserialize(database);
    }

    public override string ToString()
    {
        if (Name != string.Empty)
            return Name;

        return $"<span data-id=\"{Identifier.Id}\"></span>";
    }
}
