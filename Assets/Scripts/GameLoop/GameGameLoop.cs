using System.Threading;
using UnityEngine;
using GameScene;
public class GameGameLoop:MonoBehaviour
{
    private Facade facade;
    void Start()
    {
        EnsureFacade();
        GameVoiceManager.EnsureExists(gameObject);
        GameVoiceManager.EnterGameScene();
        RoguelikeGameManager.EnsureExists(gameObject);
        RoguelikeHudCanvas.EnsureExists(gameObject);
    }
    void Update()
    {
        EnsureFacade();
        facade.GameUpdate();
    }

    private void EnsureFacade()
    {
        if (facade == null)
        {
            facade = new Facade();
        }
    }
}
