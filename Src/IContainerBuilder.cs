namespace Wevear;

public interface IContainerBuilder
{
    void Register(in ServiceDescriptor serviceDescriptor);
    IContainer Build();
}