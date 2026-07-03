using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

namespace Wevear;

public sealed class Container : IContainer
{
    private readonly ImmutableDictionary<Type, ServiceDescriptor> _descriptors;
    private readonly ConcurrentDictionary<Type, Func<IScope, object>> _cachedActivators;

    public Container(in IEnumerable<ServiceDescriptor> descriptors)
    {
        _descriptors = descriptors.ToImmutableDictionary(d => d.ServiceType);
        _cachedActivators = new();
    }

    public IScope CreateScope()
    {
        return new Scope(this);
    }

    private Func<IScope, object> BuildActivation(Type service)
    {
        if (!_descriptors.TryGetValue(service, out var descriptor))
            throw new InvalidOperationException($"Service: {service.Name} not found");

        if (descriptor is InstanceBasedServiceDescriptor instance)
            return _ => instance.Instance;
        if (descriptor is FactoryBasedServiceDescriptor factory)
            return factory.Factory;

        TypeBasedServiceDescriptor typeBasedServiceDescriptor = (TypeBasedServiceDescriptor)descriptor;
        ConstructorInfo constructor = typeBasedServiceDescriptor.ImplementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        ParameterInfo[] parameters = constructor.GetParameters();

        return scope =>
        {
            object[] createdParameters = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                createdParameters[i] = CreateInstance(parameters[i].ParameterType, scope);

            return constructor.Invoke(createdParameters);
        };
    }

    private object CreateInstance(in Type service, in IScope scope)
    {
        return _cachedActivators.GetOrAdd(service, BuildActivation(service)).Invoke(scope);
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