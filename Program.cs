using Wevear;

var service = new Service();
IContainerBuilder builder = new ContainerBuilder();

await using (IContainer container = builder
    .RegisterTransient<IService, Service>()
    .Build())
{
    IScope scope = container.CreateScope();
    IScope scope2 = container.CreateScope();

    IService service1 = scope.Resolve<IService>();
    IService service2 = scope.Resolve<IService>();
    IService service3 = scope2.Resolve<IService>();

    Console.WriteLine(service1 == service2);
    Console.WriteLine(service1 == service3);

    Console.ReadKey();
}

Console.ReadKey();

interface IService : IDisposable, IAsyncDisposable
{

}

class Service : IService
{
    public void Dispose()
    {
        Console.WriteLine("Services disposed");
    }

    public ValueTask DisposeAsync()
    {
        Console.WriteLine("Services async disposed");

        return default;
    }
}
