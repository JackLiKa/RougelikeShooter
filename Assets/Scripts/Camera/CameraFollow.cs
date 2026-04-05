using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // 要跟随的目标
    public Vector3 offset = new Vector3(0, 0, -10); // 相机偏移
    
    // 相机边界限制（瓦片地图边界）
    // 地图中心：(960, 540), 尺寸：1920x1080
    public float mapLeft = 0f;        // 960 - 1920/2
    public float mapRight = 1920f;    // 960 + 1920/2
    public float mapBottom = 0f;      // 540 - 1080/2
    public float mapTop = 1080f;      // 540 + 1080/2
    
    // Start is called before the first frame update
    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        // 相机尺寸设置：显示 10x10 个 Player 大小
        // Player 尺寸为 10x10，所以相机视野应该是 100x100
        // 正交相机尺寸 = 视野高度的一半
        float playerSize = 10f;  // Player 尺寸
        int playerCount = 10;    // 显示 10 个 Player
        float cameraViewSize = playerSize * playerCount;  // 100
        float orthographicSize = cameraViewSize / 2f;  // 50
        
        Camera camera = GetComponent<Camera>();
        camera.orthographicSize = orthographicSize;
        
       // Debug.Log($"CameraFollow.Start() - 设置相机 orthographicSize = {orthographicSize}, 实际值 = {camera.orthographicSize}");
    }
    
    // LateUpdate is called after all Update functions have been called
    void LateUpdate()
    {
        if (target != null)
        {
            // 获取相机半高（正交相机）
            float cameraHalfHeight = GetComponent<Camera>().orthographicSize;
            float cameraHalfWidth = cameraHalfHeight * Screen.width / Screen.height;
            
            // 计算目标位置
            Vector3 targetPosition = target.position + offset;
            
            // 限制相机在地图边界内
            targetPosition.x = Mathf.Clamp(targetPosition.x, 
                mapLeft + cameraHalfWidth, 
                mapRight - cameraHalfWidth);
            targetPosition.y = Mathf.Clamp(targetPosition.y, 
                mapBottom + cameraHalfHeight, 
                mapTop - cameraHalfHeight);
            
            transform.position = targetPosition;
        }
    }
}
