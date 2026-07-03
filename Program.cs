using Wevear;

var service = new Service();
var controller = new Controller(service);

IContainerBuilder builder = new ContainerBuilder();
IContainer container = builder.RegisterSingleton<IService, Service>()
    .Build();

IScope scope = container.CreateScope();

Console.WriteLine(scope.Resolve<IService>() == null);
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


interface IService
{

}
class Service : IService
{

}
