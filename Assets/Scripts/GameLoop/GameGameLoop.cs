using System.Threading;
using UnityEngine;
using GameScene;
public class GameGameLoop:MonoBehaviour
{
    private Facade facade;
    void Start()
    {
        facade=new Facade();
    }
    void Update()
    {
        facade.GameUpdate();
    }
}