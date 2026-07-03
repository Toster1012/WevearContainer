namespace Wevear;

public abstract class ServiceDescriptor
{
    public Type ServiceType { get; init; }
    public LifeTime LifeTime { get; init; }
}

public sealed class TypeBasedServiceDescriptor : ServiceDescriptor
{
    public Type ImplementationType { get; init; }
}

public sealed class FactoryBasedServiceDescriptor : ServiceDescriptor
{
    public Func<IScope, object> Factory { get; init; }
}

public sealed class InstanceBasedServiceDescriptor : ServiceDescriptor
{
    public object Instance { get; init; }

    public InstanceBasedServiceDescriptor(in Type serviceType, in object instance)
    {
        ServiceType = serviceType;
        LifeTime = LifeTime.Single;
        Instance = instance;
    }
}