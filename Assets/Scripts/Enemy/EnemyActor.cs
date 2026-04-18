using UnityEngine;

public class EnemyActor : MonoBehaviour
{
    private SpriteRenderer[] spriteRenderers;

    private RoguelikeGameManager owner;
    private EnemyProfile profile;
    private int maxHp;
    private int currentHp;
    private int attack;
    private float moveSpeed;
    private float baseMoveSpeed;
    private float attackInterval;
    private float contactRange;
    private float attackCooldown;
    private float hitRadius;
    private float visualHalfHeight;
    private float terrainDamageTimer;
    private bool isElite;

    public string EnemyKey => profile != null ? profile.EnemyKey : string.Empty;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public float HealthRatio => maxHp <= 0 ? 0f : (float)currentHp / maxHp;
    public bool IsElite => isElite;
    public float HitRadius => hitRadius;
    public float UiHeadOffset => visualHalfHeight + Mathf.Max(0.75f, visualHalfHeight * 0.15f);
    public Vector2 Position => transform.position;

    private void Awake()
    {
        CacheSpriteRenderers();
    }

    private void OnEnable()
    {
        CacheSpriteRenderers();
    }

    public void Configure(
        RoguelikeGameManager owner,
        EnemyProfile sourceProfile,
        int hp,
        int attack,
        float moveSpeed,
        float attackInterval,
        float contactRange,
        float scale,
        bool isElite)
    {
        this.owner = owner;
        profile = sourceProfile;
        maxHp = Mathf.Max(1, hp);
        currentHp = maxHp;
        this.attack = Mathf.Max(1, attack);
        baseMoveSpeed = Mathf.Max(0.2f, moveSpeed);
        this.moveSpeed = baseMoveSpeed;
        this.attackInterval = Mathf.Max(0.2f, attackInterval);
        this.contactRange = Mathf.Max(0.5f, contactRange);
        this.isElite = isElite;
        attackCooldown = 0f;
        terrainDamageTimer = 0f;
        transform.localScale = new Vector3(scale, scale, 1f);
        hitRadius = CalculateVisualHitRadius(scale);
        visualHalfHeight = CalculateVisualHalfHeight(scale);
    }

    public void RestoreState(int hp)
    {
        currentHp = Mathf.Clamp(hp, 0, maxHp);
    }

    public bool Tick(float deltaTime, Vector3 playerPosition)
    {
        attackCooldown -= deltaTime;
        moveSpeed = Mathf.Max(0.2f, baseMoveSpeed + owner.GetTerrainMoveSpeedModifier(transform.position));

        float terrainDamagePerSecond = owner.GetTerrainDamagePerSecond(transform.position);
        if (terrainDamagePerSecond > 0f)
        {
            terrainDamageTimer += deltaTime * terrainDamagePerSecond;
            while (terrainDamageTimer >= 1f && currentHp > 0)
            {
                terrainDamageTimer -= 1f;
                TakeDamage(1, false);
            }
        }
        else
        {
            terrainDamageTimer = 0f;
        }

        Vector2 toPlayer = playerPosition - transform.position;
        float distance = toPlayer.magnitude;
        if (distance > 0.001f)
        {
            transform.position += (Vector3)(toPlayer.normalized * moveSpeed * deltaTime);
            if (Mathf.Abs(toPlayer.x) > 0.01f)
            {
                float sign = Mathf.Sign(toPlayer.x);
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * sign, Mathf.Abs(transform.localScale.y), 1f);
            }
        }

        if (distance <= contactRange + owner.PlayerHitRadius && attackCooldown <= 0f)
        {
            attackCooldown = attackInterval;
            owner.DamagePlayer(attack);
        }

        return currentHp > 0;
    }

    public bool TakeDamage(int damage, bool showDamageText = true)
    {
        int actualDamage = Mathf.Max(1, damage);
        currentHp = Mathf.Clamp(currentHp - actualDamage, 0, maxHp);
        if (showDamageText)
        {
            owner?.SpawnDamageText(transform.position + new Vector3(0f, hitRadius * 0.8f, 0f), actualDamage, false);
        }

        return currentHp > 0;
    }

    private void CacheSpriteRenderers()
    {
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            return;
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private float CalculateVisualHitRadius(float scale)
    {
        if (!TryGetCombinedSpriteBounds(out Bounds combinedBounds))
        {
            return Mathf.Max(0.85f, scale * 0.12f);
        }

        return Mathf.Max(0.85f, Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.y));
    }

    private float CalculateVisualHalfHeight(float scale)
    {
        if (!TryGetCombinedSpriteBounds(out Bounds combinedBounds))
        {
            return Mathf.Max(0.85f, scale * 0.12f);
        }

        return Mathf.Max(0.85f, combinedBounds.extents.y);
    }

    private bool TryGetCombinedSpriteBounds(out Bounds combinedBounds)
    {
        CacheSpriteRenderers();

        bool hasBounds = false;
        combinedBounds = default;
        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[index];
            if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = spriteRenderer.bounds;
                hasBounds = true;
                continue;
            }

            combinedBounds.Encapsulate(spriteRenderer.bounds);
        }

        return hasBounds;
    }
}
