using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Ak47 : MonoBehaviour
{

    public GameObject bullet;
    public Transform muzzleTransform;





    private Vector3 mousePosition;
    private Vector2 gunDirection;
    public Camera camera;
    private Transform playerTransform;
    private bool isFlipped = false;  // 记录当前翻转状态

    // Start is called before the first frame update
    void Start()
    {
        // 如果没有分配 camera，使用主相机
        if (camera == null)
        {
            camera = Camera.main;
        }
        
        // 获取父对象（Player）的 Transform
        playerTransform = this.transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAk47Direction();
        shoot();
    }

    void UpdateAk47Direction()
    {
        // 获取鼠标位置（使用旧的 Input 系统）
        mousePosition = Input.mousePosition;
        mousePosition.z = -camera.transform.position.z; // 设置深度
        mousePosition = camera.ScreenToWorldPoint(mousePosition);
        
        // 计算枪口方向
        gunDirection = (mousePosition - transform.position).normalized;
        
        // 计算角度并旋转
        float angle = Mathf.Atan2(gunDirection.y, gunDirection.x) * Mathf.Rad2Deg;
        
        // 如果 Player 被翻转（scale.x 为负），需要抵消翻转
        if (playerTransform != null && playerTransform.localScale.x < 0)
        {
            // Player 面向左时，枪口角度需要翻转 180 度
            angle += 180f;
        }
        
        transform.eulerAngles = new Vector3(0, 0, angle);
        
        // 控制 Ak47 图片上下翻转（沿 X 轴翻转，即 Y 轴缩放取负）
        FlipAk47Sprite(angle);
    }
    
    /// <summary>
    /// 根据 Player 朝向和鼠标位置控制 Ak47 图片上下翻转
    /// </summary>
    /// <param name="currentAngle">当前枪口角度</param>
    void FlipAk47Sprite(float currentAngle)
    {
        if (playerTransform == null) return;
        
        // 判断 Player 朝向（通过 scale.x 正负）
        bool playerFacingRight = playerTransform.localScale.x > 0;
        
        // 判断鼠标相对于 Player 的位置
        bool mouseOnRight = gunDirection.x > 0;
        
        // 条件判断：
        // 1. Player 向右看（面向右），鼠标在左边 → 需要翻转
        // 2. Player 向左看（面向左），鼠标在右边 → 需要翻转
        bool shouldFlip = false;
        
        if (playerFacingRight && !mouseOnRight)
        {
            // Player 向右，鼠标向左 → 需要翻转
            shouldFlip = true;
        }
        else if (!playerFacingRight && mouseOnRight)
        {
            // Player 向左，鼠标向右 → 需要翻转
            shouldFlip = true;
        }
        
        // 只在翻转状态改变时才执行翻转
        if (shouldFlip != isFlipped)
        {
            // 应用翻转（沿 X 轴翻转 = Y 轴缩放取负）
            float scaleY = shouldFlip ? -Mathf.Abs(transform.localScale.y) : Mathf.Abs(transform.localScale.y);
            transform.localScale = new Vector3(transform.localScale.x, scaleY, transform.localScale.z);
            
            // 更新翻转状态
            isFlipped = shouldFlip;
        }
    }
    void shoot()
    {
        // 使用 Input.GetMouseButtonDown 检测鼠标左键点击
        if (Input.GetMouseButtonDown(0))  // 0 代表鼠标左键
        {
            // 射击逻辑
            Debug.Log("射击！");
            
            // 子弹方向永远不翻转，直接跟随鼠标方向
            // 使用 gunDirection 计算出的角度作为子弹方向
            float bulletAngle = Mathf.Atan2(gunDirection.y, gunDirection.x) * Mathf.Rad2Deg;
            Quaternion bulletRotation = Quaternion.Euler(0, 0, bulletAngle);
            
            // 生成子弹，子弹方向与鼠标方向一致，永不翻转
            Instantiate(bullet, muzzleTransform.position, bulletRotation);
        }
    }
}
