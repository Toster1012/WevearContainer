using Wevear;

var service = new Service();
var controller = new Controller(service);

IContainerBuilder builder = new ContainerBuilder();

using (IContainer container = builder.RegisterSingleton<IService, Service>()
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

class Controller(IService service)
{
    private readonly IService _service = service;

    public void Do() { }
}

interface IHelper
{

}

class Helper : IHelper
{

}

interface IService : IDisposable
{

}
class Service : IService
{
    public void Dispose()
    {
        Console.WriteLine("Services disposed");
    }
}
