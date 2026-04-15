public class GameMediator:AbstractMediator
{
    public static GameMediator instance;
    public static GameMediator Instance
    {
        get
        {
            if(instance==null)
            {
                instance=new GameMediator();
            }
            return instance;
        }
    }
    private GameMediator()
    {

    }
}