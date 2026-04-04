using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuButtons : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("MainMenuButtons Start");
        Buttons();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Buttons(){
        OnGUI();
    }
    private void OnGUI(){
            // 添加垂直间距
         GUILayout.Space(50); // 向下推 50 像素
    
        // 添加水平间距
         GUILayout.BeginHorizontal();
         GUILayout.Space(100); // 向右推 100 像素

         if (GUILayout.Button("开始游戏",GUILayout.Height(40),GUILayout.Width(100))){
            StartGame();
         }
        GUILayout.EndHorizontal();
        GUILayout.Space(20); // 按钮之间的间距


        GUILayout.BeginHorizontal();
        GUILayout.Space(100);
          if (GUILayout.Button("角色面板",GUILayout.Height(40),GUILayout.Width(100))){
            CharacterPanel();
         }

        GUILayout.EndHorizontal();
        GUILayout.Space(20); // 按钮之间的间距


        GUILayout.BeginHorizontal();
        GUILayout.Space(100);
          if (GUILayout.Button("退出游戏",GUILayout.Height(40),GUILayout.Width(100))){
            ExitGame();
         }
        GUILayout.EndHorizontal();
    }

    void ExitGame(){
        
    }
    void StartGame(){
        
    }
    void CharacterPanel(){
        
    }
}
