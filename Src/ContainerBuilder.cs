namespace Wevear;

public sealed class ContainerBuilder : IContainerBuilder
{
    private readonly List<ServiceDescriptor> _descriptors = new();

    public void Register(in ServiceDescriptor serviceDescriptor)
    {
        _descriptors.Add(serviceDescriptor);
    }

    public IContainer Build()
    {
        return new Container(_descriptors);
    }
}