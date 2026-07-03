using System.Reflection;

namespace Wevear;

public sealed class Container : IContainer
{
    private readonly Dictionary<Type, ServiceDescriptor> _descriptors;

    public Container(IEnumerable<ServiceDescriptor> descriptors)
    {
        _descriptors = descriptors.ToDictionary(d => d.ServiceType);
    }

    public IScope CreateScope()
    {
        return new Scope(this);
    }

    private object CreateInstance(Type service)
    {
        if (!_descriptors.TryGetValue(service, out var descriptor))
            throw new InvalidOperationException($"Service: {service.Name} not found");

        ConstructorInfo constructor = service.GetConstructors().Single();
        ParameterInfo[] parameters = constructor.GetParameters();
        object[] createdParameters = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
            createdParameters[i] = CreateInstance(parameters[i].ParameterType);

        return constructor.Invoke(createdParameters);
    }

    private T CreateInstance<T>()
        => (T)CreateInstance(typeof(T));

    private sealed class Scope : IScope
    {
        private readonly Container _container;

        public Scope(Container container)
        {
            _container = container;
        }

        public object Resolve(Type serviceType)
        {
            return _container.CreateInstance(serviceType);
        }

        public T Resolve<T>()
        {
            return _container.CreateInstance<T>();
        }
    }
}