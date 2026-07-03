namespace Wevear;

public interface IScope
{
    object Resolve(in Type serviceType);
    T Resolve<T>();
}
