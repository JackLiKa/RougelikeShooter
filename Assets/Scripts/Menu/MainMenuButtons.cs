using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class MainMenuButtons : MonoBehaviour
{
    private bool showExitConfirm = false;
    private Rect dialogRect;
    
    void Start()
    {
        dialogRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 150);
    }

    void Update()
    {
        
    }

    void OnGUI()
    {
        GUILayout.Space(50);
    
        GUILayout.BeginHorizontal();
        GUILayout.Space(100);

        if (GUILayout.Button("开始游戏", GUILayout.Height(40), GUILayout.Width(100)))
        {
            // Debug.Log("开始游戏按钮被点击！");
            StartGame();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.Space(100);
        
        if (GUILayout.Button("角色面板", GUILayout.Height(40), GUILayout.Width(100)))
        {
            // Debug.Log("角色面板按钮被点击！");
            CharacterPanel();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.Space(100);
        
        if (GUILayout.Button("退出游戏", GUILayout.Height(40), GUILayout.Width(100))||Input.GetKeyDown(KeyCode.Escape))
        {
            // Debug.Log("退出游戏按钮被点击！");
            showExitConfirm = true;
        }
        GUILayout.EndHorizontal();
        
        if (showExitConfirm)
        {
            ShowExitDialog();
        }
    }
    
    void ShowExitDialog()
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
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.black }
        };
        
        GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            normal = { textColor = Color.black }
        };
        
        GUI.Label(new Rect(dialogRect.x + 10, dialogRect.y + 10, 280, 30), "确认退出", titleStyle);
        
        GUI.Label(new Rect(dialogRect.x + 10, dialogRect.y + 40, 280, 30), "确定要退出游戏吗？", messageStyle);
        
        Color confirmButtonColor = new Color(1f, 0.6f, 0.2f, 1f);
        Color cancelButtonColor = new Color(0.4f, 0.8f, 0.4f, 1f);
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.black },
            hover = { textColor = Color.white }
        };
        
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

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    void StartGame()
    {
        // Debug.Log("StartGame - 加载场景");
        SceneManager.LoadScene("GameScene");
    }
    
    void CharacterPanel()
    {
        Debug.Log("打开角色面板");
    }
}
