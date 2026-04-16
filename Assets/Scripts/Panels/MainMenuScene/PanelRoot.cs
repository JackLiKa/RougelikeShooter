using UnityEngine;
using UnityEngine.SceneManagement;

public class PanelRoot : IPanel
{
    private bool showExitConfirm;
    private Rect dialogRect;

    public PanelRoot() : base(null)
    {
    }

    protected override void OnInit()
    {
        /*
         * [AI_COMMENTED_OUT]
         * 原实现尝试在 MainMenu 场景里查找 ButtonStart 并直接给 Unity Button 绑事件，
         * 但当前 MainMenuScene 实际并没有这组 Button 组件，主菜单原有可用逻辑来自 MainMenuButtons.OnGUI。
         * 现在按你的要求统一迁移到 MainMenuGameLoop -> Facade -> UIController -> PanelRoot 这条链，
         * 因此这里保留旧思路说明，实际改为初始化 GameLoop 驱动的 IMGUI 面板。
         */
        gameObject = GameObject.Find("MainMenu");
        rectTransform = gameObject != null ? gameObject.GetComponent<RectTransform>() : null;
        dialogRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 150);
    }

    protected override void OnEnter()
    {
        base.OnEnter();
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            showExitConfirm = true;
        }
    }

    public void DrawGUI()
    {
        GUILayout.Space(50);

        GUILayout.BeginHorizontal();
        GUILayout.Space(100);
        if (GUILayout.Button("开始游戏", GUILayout.Height(40), GUILayout.Width(100)))
        {
            SceneManager.LoadScene("GameScene");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.Space(100);
        if (GUILayout.Button("角色面板", GUILayout.Height(40), GUILayout.Width(100)))
        {
            SceneManager.LoadScene("CharacterPanelScene");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.Space(100);
        if (GUILayout.Button("退出游戏", GUILayout.Height(40), GUILayout.Width(100)))
        {
            showExitConfirm = true;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.Space(100);
        GUILayout.Label(
            $"当前角色: {GameSelectionConfig.GetPlayerDisplayName(GameSelectionConfig.CurrentPlayerType)}    当前武器: {GameSelectionConfig.GetWeaponDisplayName(GameSelectionConfig.CurrentWeaponType)}",
            GUILayout.Width(420),
            GUILayout.Height(30));
        GUILayout.EndHorizontal();

        if (showExitConfirm)
        {
            DrawExitDialog();
        }
    }

    private void DrawExitDialog()
    {
        Color dialogBackgroundColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        GUI.color = dialogBackgroundColor;
        GUI.DrawTexture(dialogRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        Color borderColor = new Color(0.1f, 0.4f, 0.7f, 1f);
        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(dialogRect.x - 2, dialogRect.y - 2, dialogRect.width + 4, dialogRect.height + 4), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = Color.black;

        GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14
        };
        messageStyle.normal.textColor = Color.black;

        GUI.Label(new Rect(dialogRect.x + 10, dialogRect.y + 10, 280, 30), "确认退出", titleStyle);
        GUI.Label(new Rect(dialogRect.x + 10, dialogRect.y + 40, 280, 30), "确定要退出游戏吗？", messageStyle);

        Color confirmButtonColor = new Color(1f, 0.6f, 0.2f, 1f);
        Color cancelButtonColor = new Color(0.4f, 0.8f, 0.4f, 1f);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        buttonStyle.normal.textColor = Color.black;
        buttonStyle.hover.textColor = Color.white;

        GUI.backgroundColor = confirmButtonColor;
        if (GUI.Button(new Rect(dialogRect.x + 30, dialogRect.y + 80, 100, 40), "确定", buttonStyle))
        {
            showExitConfirm = false;
            ExitGame();
        }

        GUI.backgroundColor = cancelButtonColor;
        if (GUI.Button(new Rect(dialogRect.x + 170, dialogRect.y + 80, 100, 40), "取消", buttonStyle))
        {
            showExitConfirm = false;
        }

        GUI.backgroundColor = Color.white;
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
