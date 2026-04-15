// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class PlayerMovement : MonoBehaviour
// {
//     public float moveSpeed = 5f;
    
//     // 地图边界（显式设置）
//     private float mapLeft = 0f;
//     private float mapRight = 1920f;
//     private float mapBottom = 0f;
//     private float mapTop = 1080f;
//     private float playerSize = 10f;  // Player 尺寸


//     private  Rigidbody2D myRigidbody;           // Start is called before the first frame update
//     private Animator myAnimator;
//     void Start()
//     {
//         myRigidbody=GetComponent<Rigidbody2D>();
//         myAnimator=GetComponent<Animator>();

//         // Debug.Log($"地图边界：Left={mapLeft}, Right={mapRight}, Bottom={mapBottom}, Top={mapTop}");
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }

//    private void FixedUpdate(){
//         Move();
//    }

//     void Move(){
//         float horizontal = 0f;
//         float vertical = 0f;
        
//         // 获取水平输入（-1 到 1 之间）
//         float moveDir = Input.GetAxis("Horizontal");
        
//         // 应用物理速度（只处理水平方向）
//         if (myRigidbody != null)
//         {
//             Vector2 playerVel = new Vector2(moveDir * moveSpeed, myRigidbody.velocity.y);
//             myRigidbody.velocity = playerVel;
//         }

//         // 处理键盘输入（用于翻转和垂直移动）
//         if (Input.GetKey(KeyCode.A))
//         {
//             horizontal = -1f;
//             this.transform.localScale = new Vector3(-playerSize, playerSize, 1f);
//         }
//         if (Input.GetKey(KeyCode.D))
//         {
//             horizontal = 1f;
//             this.transform.localScale = new Vector3(playerSize, playerSize, 1f);
//         }
//         if (Input.GetKey(KeyCode.W))
//         {
//             vertical = 1f;
//         }
//         if (Input.GetKey(KeyCode.S))
//         {
//             vertical = -1f;
//         }
        
//         // 计算目标位置（只处理垂直方向，水平方向由 Rigidbody2D 处理）
//         Vector3 newPosition = transform.position + new Vector3(0, vertical * moveSpeed * Time.fixedDeltaTime, 0);
        
//         // 限制 Player 在地图范围内
//         float halfPlayerSize = playerSize / 2f;
//         newPosition.x = Mathf.Clamp(newPosition.x, mapLeft + halfPlayerSize, mapRight - halfPlayerSize);
//         newPosition.y = Mathf.Clamp(newPosition.y, mapBottom + halfPlayerSize, mapTop - halfPlayerSize);
        
//         transform.position = newPosition;
        
//         // 更新动画状态：水平或垂直有移动时都触发 isRun
//         if (myAnimator != null)
//         {
//             // 检测是否有水平或垂直移动
//             bool hasHorizontalInput = Mathf.Abs(horizontal) > Mathf.Epsilon || Mathf.Abs(moveDir) > Mathf.Epsilon;
//             bool hasVerticalInput = Mathf.Abs(vertical) > Mathf.Epsilon;
            
//             // 只要有任何方向移动就触发跑步动画
//             bool isRunning = hasHorizontalInput || hasVerticalInput;
//             myAnimator.SetBool("isRun", isRunning);
//         }
//     }
// }
