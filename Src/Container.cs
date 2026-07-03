using System.Reflection;

namespace Wevear;

public sealed class Container : IContainer
{
    private readonly Dictionary<Type, ServiceDescriptor> _descriptors;

    public Container(in IEnumerable<ServiceDescriptor> descriptors)
    {
        _descriptors = descriptors.ToDictionary(d => d.ServiceType);
    }

    public IScope CreateScope()
    {
        return new Scope(this);
    }

    private object CreateInstance(in Type service, in IScope scope)
    {
        if (!_descriptors.TryGetValue(service, out var descriptor))
            throw new InvalidOperationException($"Service: {service.Name} not found");

        if (descriptor is InstanceBasedServiceDescriptor instance)
            return instance.Instance;
        if (descriptor is FactoryBasedServiceDescriptor factory)
            return factory.Factory.Invoke(scope);

        TypeBasedServiceDescriptor typeBasedServiceDescriptor = (TypeBasedServiceDescriptor)descriptor;
        ConstructorInfo constructor = typeBasedServiceDescriptor.ImplementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        ParameterInfo[] parameters = constructor.GetParameters();
        object[] createdParameters = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
            createdParameters[i] = CreateInstance(parameters[i].ParameterType, scope);

        return constructor.Invoke(createdParameters);
    }

    private T CreateInstance<T>(in IScope scope)
        => (T)CreateInstance(typeof(T), scope);

    private sealed class Scope : IScope
    {
        private readonly Container _container;

        public Scope(Container container)
        {
            _container = container;
        }

        public object Resolve(in Type serviceType)
        {
            return _container.CreateInstance(serviceType, this);
        }

        public T Resolve<T>()
        {
            return _container.CreateInstance<T>(this);
        }
    }
}