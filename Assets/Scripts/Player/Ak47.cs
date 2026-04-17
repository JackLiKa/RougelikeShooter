using UnityEngine;

public class Ak47 : MonoBehaviour
{
    public GameObject bullet;
    public Transform muzzleTransform;
    public Camera mainCamera;

    private Vector3 mousePosition;
    private Vector2 gunDirection = Vector2.right;
    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Vector3 baseLocalScale;
    private float nextShootTime;

    void Start()
    {
        playerTransform = transform.parent;
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseLocalScale = transform.localScale;
        RefreshCameraReference();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            playerTransform = transform.parent;
        }

        if (baseLocalScale == Vector3.zero)
        {
            baseLocalScale = transform.localScale;
        }

        RefreshCameraReference();
        UpdateDirection();
        TryShoot();
    }

    private void TryShoot()
    {
        RoguelikeGameManager manager = RoguelikeGameManager.Instance;
        if (manager == null || !manager.CanAcceptPlayerInput || muzzleTransform == null)
        {
            return;
        }

        float shootInterval = 1f / Mathf.Max(0.1f, manager.CurrentFireRate);
        if (!Input.GetMouseButton(0) || Time.unscaledTime < nextShootTime)
        {
            return;
        }

        if (!manager.TryFireWeapon(muzzleTransform.position, gunDirection))
        {
            return;
        }

        nextShootTime = Time.unscaledTime + shootInterval;
    }

    private void UpdateDirection()
    {
        if (mainCamera == null)
        {
            return;
        }

        mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;
        mousePosition = mainCamera.ScreenToWorldPoint(mousePosition);

        Vector3 aimOrigin = muzzleTransform != null ? muzzleTransform.position : transform.position;
        gunDirection = ((Vector2)(mousePosition - aimOrigin)).normalized;
        if (gunDirection.sqrMagnitude <= 0.001f)
        {
            gunDirection = Vector2.right;
        }

        float angle = Mathf.Atan2(gunDirection.y, gunDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        float parentMirrorCompensation = playerTransform != null && playerTransform.lossyScale.x < 0f ? -1f : 1f;
        transform.localScale = new Vector3(Mathf.Abs(baseLocalScale.x) * parentMirrorCompensation, Mathf.Abs(baseLocalScale.y), baseLocalScale.z);
        if (spriteRenderer != null)
        {
            spriteRenderer.flipY = gunDirection.x < 0f;
        }
    }

    private void RefreshCameraReference()
    {
        if (mainCamera != null)
        {
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return;
        }

        GameObject namedCamera = GameObject.Find("Main Camera");
        if (namedCamera != null)
        {
            mainCamera = namedCamera.GetComponent<Camera>();
        }

        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }
    }
}
