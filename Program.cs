using Wevear;

var service = new Service();
var controller = new Controller(service);

IContainerBuilder builder = new ContainerBuilder();
builder.RegisterSingleton<IService, Service>()
    .RegisterTransient<IHelper, Helper>()
    .Build();

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
