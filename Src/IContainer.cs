namespace Wevear;

public interface IContainer : IDisposable, IAsyncDisposable
{
    IScope CreateScope();
}
