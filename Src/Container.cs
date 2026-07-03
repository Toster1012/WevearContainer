using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

namespace Wevear;

public sealed class Container : IContainer
{
    private readonly ImmutableDictionary<Type, ServiceDescriptor> _descriptors;
    private readonly ConcurrentDictionary<Type, Func<IScope, HashSet<Type>, object>> _cachedActivators;
    private readonly HashSet<IScope> _disposedScope;
    private readonly Scope _rootScope;

    public Container(in IEnumerable<ServiceDescriptor> descriptors)
    {
        _descriptors = descriptors.ToImmutableDictionary(d => d.ServiceType);
        _cachedActivators = new();
        _disposedScope = new();
        _rootScope = new(this);
    }

    public IScope CreateScope()
    {
        IScope scope = new Scope(this);
        _disposedScope.Add(scope);
        return scope;
    }

    public void Dispose()
    {
        if (_disposedScope.Count > 0)
        {
            foreach (var scope in _disposedScope)
                scope.Dispose();
        }

        _rootScope.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposedScope.Count > 0)
        {
            foreach (var scope in _disposedScope)
            {
                await scope.DisposeAsync();
                scope.Dispose();
            }
        }

        await _rootScope.DisposeAsync();
    }

    private Func<IScope, HashSet<Type>, object> BuildActivation(in Type service)
    {
        if (!_descriptors.TryGetValue(service, out var descriptor))
            throw new InvalidOperationException($"Service: {service.Name} not found");

        if (descriptor is InstanceBasedServiceDescriptor instance)
            return (_, _) => instance.Instance;
        if (descriptor is FactoryBasedServiceDescriptor factory)
            return (scope, _) => factory.Factory;

        TypeBasedServiceDescriptor typeBasedServiceDescriptor = (TypeBasedServiceDescriptor)descriptor;
        ConstructorInfo constructor = typeBasedServiceDescriptor.ImplementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        ParameterInfo[] parameters = constructor.GetParameters();

        return (scope, resolvingTypes) =>
        {
            object[] createdParameters = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                createdParameters[i] = CreateInstance(parameters[i].ParameterType, scope, resolvingTypes);

            return constructor.Invoke(createdParameters);
        };
    }

    private object CreateInstance(in Type service, in IScope scope, in HashSet<Type> resolvingTypes)
    {
        if (!resolvingTypes.Add(service))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected! Cycle: {string.Join(" -> ", resolvingTypes)} -> {service.Name}");
        }

        try
        {
            var activator = _cachedActivators.GetOrAdd(service, BuildActivation(service));
            return activator.Invoke(scope, resolvingTypes);
        }
        finally
        {
            resolvingTypes.Remove(service);
        }
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
            return ResolveInternal(serviceType, new HashSet<Type>());
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

        private object ResolveInternal(in Type serviceType, HashSet<Type> resolvingTypes)
        {
            ServiceDescriptor descriptor = _container.FindDescriptor(serviceType);

            if (descriptor.LifeTime == LifeTime.Transient)
                return CreateInstanceInternal(serviceType, resolvingTypes);

            if (descriptor.LifeTime == LifeTime.Scoped || _container._rootScope == this)
                return _scopedInstances.GetOrAdd(serviceType, t => CreateInstanceInternal(t, resolvingTypes));
            else
                return _container._rootScope.ResolveInternal(serviceType, resolvingTypes);
        }

        private object CreateInstanceInternal(in Type serviceType, in HashSet<Type> resolvingTypes)
        {
            object service = _container.CreateInstance(serviceType, this, resolvingTypes);

            if (service is IDisposable || service is IAsyncDisposable)
                _disposables.Push(service);

            return service;
        }
    }
}