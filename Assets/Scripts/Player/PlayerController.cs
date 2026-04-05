using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    
    // 显式设置 Player 初始位置（地图中心）
    private Vector3 initialPosition = new Vector3(960f, 540f, 0);

    // Start is called before the first frame update
    void Start()
    {
     //   Debug.Log("=== PlayerController Start ===");
        spriteRenderer = GetComponent<SpriteRenderer>();
        
     //   Debug.Log($"地图范围：(0, 0) 到 (1920, 1080)");
      //  Debug.Log($"Player 初始位置（地图中心）：{initialPosition}");
        
        initPlayer();
    }
    
    void initPlayer()
    {
        // 将 Player 初始化为地图中心位置（显式设置）
        transform.position = initialPosition;
        transform.localScale = new Vector3(10, 10, 1);  // Player 尺寸：10x10 单位
        transform.rotation = Quaternion.identity;
        
     //   Debug.Log($"✓ Player 初始化位置：{transform.position}");
       // Debug.Log($"✓ Player 尺寸：{transform.localScale}");
     //   Debug.Log($"✓ Player 在地图正中心 (960, 540)");
        
        // 检查 SpriteRenderer 是否已有 sprite，如果没有则使用默认设置
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            Debug.LogWarning("⚠ 请在 Inspector 中为 SpriteRenderer 的 Sprite 字段赋值");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

}
