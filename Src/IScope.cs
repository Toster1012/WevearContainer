namespace Wevear;

public interface IScope : IDisposable, IAsyncDisposable
{
    object Resolve(in Type serviceType);
    T Resolve<T>();
}
