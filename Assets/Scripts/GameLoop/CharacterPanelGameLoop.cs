using UnityEngine;
using CharacterPanelScene;

public class CharacterPanelGameLoop : MonoBehaviour
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
