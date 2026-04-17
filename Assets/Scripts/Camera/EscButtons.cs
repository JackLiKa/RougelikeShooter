using UnityEngine;

public class EscButtons : MonoBehaviour
{
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (RoguelikeGameManager.Instance != null)
        {
            RoguelikeGameManager.Instance.TogglePauseMenu();
        }
    }
}
