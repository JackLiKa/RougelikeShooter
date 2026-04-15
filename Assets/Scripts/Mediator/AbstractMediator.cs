using System.Collections.Generic;

public abstract class AbstractMediator
{
    private List<AbstractController>controllers;
    public AbstractMediator()
    {
        controllers=new List<AbstractController>();
    }
    public void RegisterController(AbstractController controller)
    {
        controllers.Add(controller);
    }
    public T GetController<T>() where T:AbstractController
    {
        foreach(AbstractController controller in controllers)
        {
            if(controller is T)
            {
                return controller as T;
            }
        }
        return default(T);
    }
}