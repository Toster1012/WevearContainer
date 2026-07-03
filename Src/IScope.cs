namespace Wevear;

public interface IScope
{
    object Resolve(Type serviceType);
    T Resolve<T>();
}
