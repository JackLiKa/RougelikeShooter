using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    
    // 地图边界（显式设置）
    private float mapLeft = 0f;
    private float mapRight = 1920f;
    private float mapBottom = 0f;
    private float mapTop = 1080f;
    private float playerSize = 10f;  // Player 尺寸

    // Start is called before the first frame update
    void Start()
    {
        // Debug.Log($"地图边界：Left={mapLeft}, Right={mapRight}, Bottom={mapBottom}, Top={mapTop}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   private void FixedUpdate(){
        Move();
   }

    void Move(){
        float horizontal = 0f;
        float vertical = 0f;
        
        if (Input.GetKey(KeyCode.A))
        {
            horizontal = -1f;
            this.transform.localScale = new Vector3(-playerSize, playerSize, 1f);
        }
        if (Input.GetKey(KeyCode.D))
        {
            horizontal = 1f;
            this.transform.localScale = new Vector3(playerSize, playerSize, 1f);
        }
        if (Input.GetKey(KeyCode.W))
        {
            vertical = 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            vertical = -1f;
        }
        
        // 计算目标位置
        Vector3 newPosition = transform.position + new Vector3(horizontal * moveSpeed * Time.fixedDeltaTime, vertical * moveSpeed * Time.fixedDeltaTime, 0);
        
        // 限制 Player 在地图范围内
        float halfPlayerSize = playerSize / 2f;
        newPosition.x = Mathf.Clamp(newPosition.x, mapLeft + halfPlayerSize, mapRight - halfPlayerSize);
        newPosition.y = Mathf.Clamp(newPosition.y, mapBottom + halfPlayerSize, mapTop - halfPlayerSize);
        
        transform.position = newPosition;
    }
}
