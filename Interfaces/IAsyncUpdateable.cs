namespace Brandmauer;

public interface IAsyncUpdateable
{
    public bool ShouldUpdate { get; }
    public Task UpdateAsync();
}
