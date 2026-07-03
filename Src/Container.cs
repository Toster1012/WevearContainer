using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

namespace Wevear;

public sealed class Container : IContainer
{
    private readonly ImmutableDictionary<Type, ServiceDescriptor> _descriptors;
    private readonly ConcurrentDictionary<Type, Func<IScope, object>> _cachedActivators;
    private readonly Scope _rootScope;

    public Container(in IEnumerable<ServiceDescriptor> descriptors)
    {
        _descriptors = descriptors.ToImmutableDictionary(d => d.ServiceType);
        _cachedActivators = new();
        _rootScope = new(this);
    }

    public IScope CreateScope()
    {
        return new Scope(this);
    }

    public void Dispose() => _rootScope.Dispose();

    public ValueTask DisposeAsync() => _rootScope.DisposeAsync();

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

    private ServiceDescriptor FindDescriptor(in Type serviceType)
    {
        _descriptors.TryGetValue(serviceType, out var descriptor);
        return descriptor;
    }

    private sealed class Scope : IScope
    {
        private readonly Container _container;
        private readonly ConcurrentDictionary<Type, object> _scopedInstances;
        private readonly ConcurrentStack<object> _disposables;

        public Scope(Container container)
        {
            _container = container;
            _scopedInstances = new();
            _disposables = new();

        }

        public object Resolve(in Type serviceType)
        {
            ServiceDescriptor descriptor = _container.FindDescriptor(serviceType);

            if (descriptor.LifeTime == LifeTime.Transient)
                return CreateInstanceInternal(serviceType);

            if (descriptor.LifeTime == LifeTime.Scoped || _container._rootScope == this)
                return _scopedInstances.GetOrAdd(serviceType, CreateInstanceInternal(serviceType));
            else
                return _container._rootScope.Resolve(serviceType);
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                if (disposable is IDisposable disposedObject)
                    disposedObject.Dispose();
                else if (disposable is IAsyncDisposable asyncDisposedObject)
                    asyncDisposedObject.DisposeAsync().GetAwaiter().GetResult();
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var disposable in _disposables)
            {
                if (disposable is IAsyncDisposable asyncDisposedObject)
                    await asyncDisposedObject.DisposeAsync();
                else if (disposable is IDisposable disposedObject)
                    disposedObject.Dispose();
            }
        }

        private object CreateInstanceInternal(Type serviceType)
        {
            object service = _container.CreateInstance(serviceType, this);

            if (service is IDisposable || service is IAsyncDisposable)
                _disposables.Push(service);

            return service;
        }
    }
}