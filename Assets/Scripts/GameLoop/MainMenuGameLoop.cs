using System.Threading;
using UnityEngine;
using MainMenuScene;
public class MainMenuGameLoop:MonoBehaviour
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