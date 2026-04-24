using UnityEngine;

public class PlayerRuntimeStats : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private int baseMaxHp;
    [SerializeField] private int baseAttack;
    [SerializeField] private float baseMoveSpeed;
    [SerializeField] private float baseShootSpeed;
    [SerializeField] private int bonusMaxHp;
    [SerializeField] private int bonusAttack;
    [SerializeField] private float bonusMoveSpeed;
    [SerializeField] private float bonusShootSpeed;
    [SerializeField] private float environmentMoveSpeedModifier;
    [SerializeField] private int currentHp;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public int MaxHp => Mathf.Max(1, baseMaxHp + bonusMaxHp);
    public int CurrentHp => Mathf.Clamp(currentHp, 0, MaxHp);
    public int Attack => Mathf.Max(1, baseAttack + bonusAttack);
    public float MoveSpeed => Mathf.Max(0.1f, baseMoveSpeed + bonusMoveSpeed + environmentMoveSpeedModifier);
    public float ShootSpeed => Mathf.Max(0.1f, baseShootSpeed + bonusShootSpeed);
    public float HealthRatio => MaxHp <= 0 ? 0f : (float)CurrentHp / MaxHp;
    public int BaseMaxHp => Mathf.Max(1, baseMaxHp);
    public int BaseAttack => Mathf.Max(1, baseAttack);
    public float BaseMoveSpeed => Mathf.Max(0.1f, baseMoveSpeed);
    public float BaseShootSpeed => Mathf.Max(0.1f, baseShootSpeed);

    public void ApplyProfile(PlayerProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        displayName = profile.DisplayName;
        baseMaxHp = Mathf.Max(1, profile.MaxHp);
        baseAttack = Mathf.Max(1, profile.Attack);
        baseMoveSpeed = Mathf.Max(0.1f, profile.MoveSpeed);
        baseShootSpeed = Mathf.Max(0.1f, profile.ShootSpeed);
        environmentMoveSpeedModifier = 0f;
        ResetSessionBonuses();
        currentHp = MaxHp;
    }

    public void ResetSessionBonuses()
    {
        bonusMaxHp = 0;
        bonusAttack = 0;
        bonusMoveSpeed = 0f;
        bonusShootSpeed = 0f;
        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);
    }

    public void ApplySessionBonuses(int maxHpBonus, int attackBonus, float moveSpeedBonus, float shootSpeedBonus)
    {
        float previousRatio = HealthRatio;
        int previousMaxHp = MaxHp;

        bonusMaxHp = maxHpBonus;
        bonusAttack = attackBonus;
        bonusMoveSpeed = moveSpeedBonus;
        bonusShootSpeed = shootSpeedBonus;

        if (previousMaxHp <= 0)
        {
            currentHp = MaxHp;
            return;
        }

        currentHp = Mathf.Clamp(Mathf.RoundToInt(MaxHp * previousRatio), 0, MaxHp);
        if (MaxHp > previousMaxHp)
        {
            currentHp = Mathf.Clamp(currentHp + (MaxHp - previousMaxHp), 0, MaxHp);
        }
    }

    public void SetEnvironmentMoveSpeedModifier(float modifier)
    {
        environmentMoveSpeedModifier = modifier;
    }

    public void SetCurrentHp(int hp)
    {
        currentHp = Mathf.Clamp(hp, 0, MaxHp);
    }

    public void ModifyCurrentHp(int delta)
    {
        SetCurrentHp(currentHp + delta);
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
