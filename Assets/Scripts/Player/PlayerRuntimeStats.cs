using UnityEngine;

public class PlayerRuntimeStats : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private int maxHp;
    [SerializeField] private int currentHp;
    [SerializeField] private int attack;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float shootSpeed;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public int MaxHp => maxHp;
    public int CurrentHp => Mathf.Clamp(currentHp, 0, maxHp);
    public int Attack => attack;
    public float MoveSpeed => moveSpeed;
    public float ShootSpeed => shootSpeed;
    public float HealthRatio => maxHp <= 0 ? 0f : (float)CurrentHp / maxHp;

    public void ApplyProfile(PlayerProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        displayName = profile.DisplayName;
        maxHp = Mathf.Max(1, profile.MaxHp);
        currentHp = maxHp;
        attack = Mathf.Max(0, profile.Attack);
        moveSpeed = Mathf.Max(0.1f, profile.MoveSpeed);
        shootSpeed = Mathf.Max(0.1f, profile.ShootSpeed);
    }

    public void SetCurrentHp(int hp)
    {
        currentHp = Mathf.Clamp(hp, 0, maxHp);
    }

    public static PlayerRuntimeStats Get(GameObject playerObject)
    {
        return playerObject == null ? null : playerObject.GetComponent<PlayerRuntimeStats>();
    }

    public static float GetMoveSpeed(GameObject playerObject, float fallback)
    {
        PlayerRuntimeStats stats = Get(playerObject);
        return stats != null ? stats.MoveSpeed : fallback;
    }

    public static float GetShootSpeed(GameObject playerObject, float fallback)
    {
        PlayerRuntimeStats stats = Get(playerObject);
        return stats != null ? stats.ShootSpeed : fallback;
    }
}
