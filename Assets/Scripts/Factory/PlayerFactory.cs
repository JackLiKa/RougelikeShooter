using UnityEngine;

public enum PlayerType
{
    Player1,
    Player2,
    Player3,
    Player4,
}


public class PlayerFactory
{
    private static PlayerFactory instance;
    public static PlayerFactory Instance
    {
        get
        {
            if(instance==null)
            {
                instance=new PlayerFactory();
            }
            return instance;
        }
    }
    public IPlayer GetPlayer(PlayerType type)
    {
        GameObject obj=GameObject.Find(type.ToString());
        IPlayer player=null;
        switch(type)
        {
            case PlayerType.Player1:
                player=new Player1(obj);
                break;
            case PlayerType.Player2:
                player=new Player2(obj);
                break;
            case PlayerType.Player3:
                player=new Player3(obj);
                break;
            case PlayerType.Player4:
                player=new Player4(obj);
                break;
            default:
                return null;
        }
        return player;
    }
}
