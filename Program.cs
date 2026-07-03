var service = new Service();
var controller = new Controller(service);

controller.Do();


class Controller(IService service)
{
    private readonly IService _service = service;

    public void Do() { }
}

interface IService
{

}
class Service : IService
{

}
