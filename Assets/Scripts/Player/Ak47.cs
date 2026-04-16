using UnityEngine;

public class Ak47 : MonoBehaviour
{
    public GameObject bullet;
    public Transform muzzleTransform;
    public Camera mainCamera;

    private Vector3 mousePosition;
    private Vector2 gunDirection;
    private Transform playerTransform;
    private bool isFlipped;
    private float nextShootTime;

    void Start()
    {
        playerTransform = transform.parent;
        RefreshCameraReference();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            playerTransform = transform.parent;
        }

        RefreshCameraReference();
        UpdateDirection();
        if (bullet == null || muzzleTransform == null)
        {
            return;
        }

        float shootSpeed = PlayerRuntimeStats.GetShootSpeed(playerTransform != null ? playerTransform.gameObject : null, 1f);
        float shootInterval = 1f / Mathf.Max(0.1f, shootSpeed);
        if (!Input.GetMouseButton(0) || Time.time < nextShootTime)
        {
            return;
        }

        nextShootTime = Time.time + shootInterval;
        float bulletAngle = Mathf.Atan2(gunDirection.y, gunDirection.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0f, 0f, bulletAngle);
        Instantiate(bullet, muzzleTransform.position, bulletRotation);
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

        gunDirection = (mousePosition - transform.position).normalized;
        float angle = Mathf.Atan2(gunDirection.y, gunDirection.x) * Mathf.Rad2Deg;

        if (playerTransform != null && playerTransform.localScale.x < 0f)
        {
            angle += 180f;
        }

        transform.eulerAngles = new Vector3(0f, 0f, angle);

        if (playerTransform == null)
        {
            return;
        }

        bool playerFacingRight = playerTransform.localScale.x > 0f;
        bool mouseOnRight = gunDirection.x > 0f;
        bool shouldFlip = (playerFacingRight && !mouseOnRight) || (!playerFacingRight && mouseOnRight);
        if (shouldFlip == isFlipped)
        {
            return;
        }

        float scaleY = shouldFlip ? -Mathf.Abs(transform.localScale.y) : Mathf.Abs(transform.localScale.y);
        transform.localScale = new Vector3(transform.localScale.x, scaleY, transform.localScale.z);
        isFlipped = shouldFlip;
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
