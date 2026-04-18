using System.Threading;
using UnityEngine;
using MainMenuScene;
public class MainMenuGameLoop:MonoBehaviour
{
    private Facade facade;
    void Start()
    {
        EnsureFacade();
        GameVoiceManager.EnsureExists(gameObject);
        GameVoiceManager.EnterMainMenuScene();
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
