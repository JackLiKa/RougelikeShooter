using System.Threading;
using UnityEngine;
using GameScene;
public class GameGameLoop:MonoBehaviour
{
    private Facade facade;
    void Start()
    {
        EnsureFacade();
    }
    void Update()
    {
        EnsureFacade();
        facade.GameUpdate();
    }

    void OnGUI()
    {
        EnsureFacade();
        facade.DrawGUI();
    }

    private void EnsureFacade()
    {
        if (facade == null)
        {
            facade = new Facade();
        }
    }
}
