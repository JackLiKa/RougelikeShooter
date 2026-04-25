using System.Collections.Generic;
using UnityEngine;

public class EnemyActor : MonoBehaviour
{
    private const float DeathAnimationDuration = 0.85f;
    private static readonly float[] AvoidanceProbeAngles = { 20f, 35f, 50f, 70f, 95f, 125f, 155f };

    private SpriteRenderer[] spriteRenderers;
    private CharacterAnimationBridge animationBridge;

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
    private float activeSlowPercent;
    private float slowDurationRemaining;
    private float avoidanceDirectionSign = 1f;
    private float avoidancePersistTimer;
    private bool isOccludedByStatue;
    private bool isElite;
    private bool isDying;
    private float deathTimer;

    public string EnemyKey => profile != null ? profile.EnemyKey : string.Empty;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public float HealthRatio => maxHp <= 0 ? 0f : (float)currentHp / maxHp;
    public bool IsElite => isElite;
    public bool IsOccludedByStatue => isOccludedByStatue;
    public float HitRadius => hitRadius;
    public float UiHeadOffset => visualHalfHeight + Mathf.Max(0.75f, visualHalfHeight * 0.15f);
    public Vector2 Position => transform.position;

    private void Awake()
    {
        CacheSpriteRenderers();
        animationBridge = CharacterAnimationBridge.GetOrCreate(gameObject);
    }

    private void OnEnable()
    {
        CacheSpriteRenderers();
        animationBridge = CharacterAnimationBridge.GetOrCreate(gameObject);
        animationBridge?.ResetState();
        isOccludedByStatue = false;
        ApplySpriteVisibility(true);
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
        float collisionScale = GetCollisionScale(sourceProfile);
        this.contactRange = Mathf.Max(0.5f, contactRange * collisionScale);
        this.isElite = isElite;
        attackCooldown = 0f;
        terrainDamageTimer = 0f;
        isDying = false;
        deathTimer = 0f;
        activeSlowPercent = 0f;
        slowDurationRemaining = 0f;
        avoidanceDirectionSign = Random.value < 0.5f ? -1f : 1f;
        avoidancePersistTimer = 0f;
        isOccludedByStatue = false;
        transform.localScale = new Vector3(scale, scale, 1f);
        hitRadius = Mathf.Max(0.5f, CalculateVisualHitRadius(scale) * collisionScale);
        visualHalfHeight = CalculateVisualHalfHeight(scale);
        ApplySpriteVisibility(true);
        animationBridge?.ResetState();
    }

    public void RestoreState(int hp)
    {
        currentHp = Mathf.Clamp(hp, 0, maxHp);
        if (currentHp <= 0)
        {
            EnterDeathState();
            return;
        }

        isDying = false;
        deathTimer = 0f;
        isOccludedByStatue = false;
        ApplySpriteVisibility(true);
        animationBridge?.ResetState();
    }

    public bool Tick(float deltaTime, Vector3 playerPosition)
    {
        if (isDying)
        {
            deathTimer -= deltaTime;
            animationBridge?.SetRunning(false);
            return deathTimer > 0f;
        }

        attackCooldown -= deltaTime;
        avoidancePersistTimer = Mathf.Max(0f, avoidancePersistTimer - deltaTime);
        slowDurationRemaining = Mathf.Max(0f, slowDurationRemaining - deltaTime);
        if (slowDurationRemaining <= 0f)
        {
            activeSlowPercent = 0f;
        }

        float slowMultiplier = 1f - Mathf.Clamp(activeSlowPercent, 0f, 0.8f);
        moveSpeed = Mathf.Max(0.2f, (baseMoveSpeed * slowMultiplier) + owner.GetTerrainMoveSpeedModifier(transform.position));

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

        if (isDying || currentHp <= 0)
        {
            EnterDeathState();
            return deathTimer > 0f;
        }

        Vector2 toPlayer = playerPosition - transform.position;
        float distance = toPlayer.magnitude;
        bool isRunning = distance > 0.05f;
        if (isRunning)
        {
            Vector2 movement = toPlayer.normalized * moveSpeed * deltaTime;
            Vector2 resolvedPosition = ResolveMovement(transform.position, movement, playerPosition);
            isRunning = (resolvedPosition - (Vector2)transform.position).sqrMagnitude > 0.0001f;
            transform.position = resolvedPosition;
            if (Mathf.Abs(toPlayer.x) > 0.01f)
            {
                float sign = Mathf.Sign(toPlayer.x);
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * sign, Mathf.Abs(transform.localScale.y), 1f);
            }
        }

        UpdateStatueOcclusion();
        animationBridge?.SetRunning(isRunning);

        if (distance <= contactRange + owner.PlayerHitRadius && attackCooldown <= 0f)
        {
            attackCooldown = attackInterval;
            owner.DamagePlayer(attack);
        }

        return currentHp > 0;
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        if (isDying || currentHp <= 0)
        {
            return;
        }

        activeSlowPercent = Mathf.Clamp(activeSlowPercent + Mathf.Max(0f, slowPercent), 0f, 0.5f);
        slowDurationRemaining = Mathf.Max(slowDurationRemaining, Mathf.Max(0.1f, duration));
    }

    public bool TakeDamage(int damage, bool showDamageText = true)
    {
        if (isDying || currentHp <= 0)
        {
            return false;
        }

        int actualDamage = Mathf.Max(1, damage);
        currentHp = Mathf.Clamp(currentHp - actualDamage, 0, maxHp);
        if (showDamageText)
        {
            owner?.SpawnDamageText(transform.position + new Vector3(0f, hitRadius * 0.8f, 0f), actualDamage, false);
        }

        if (currentHp <= 0)
        {
            EnterDeathState();
            return false;
        }

        return currentHp > 0;
    }

    private void EnterDeathState()
    {
        if (isDying)
        {
            return;
        }

        isDying = true;
        currentHp = 0;
        attackCooldown = 0f;
        deathTimer = DeathAnimationDuration;
        isOccludedByStatue = false;
        ApplySpriteVisibility(true);
        animationBridge?.SetRunning(false);
        animationBridge?.SetDying(true);
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

    private void UpdateStatueOcclusion()
    {
        IReadOnlyList<StoneStatueEffect> statues = StoneStatueEffect.ActiveStatues;
        bool shouldOcclude = false;
        for (int index = 0; index < statues.Count; index++)
        {
            StoneStatueEffect statue = statues[index];
            if (statue == null || !statue.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!statue.ShouldOccludePoint(Position, hitRadius * 0.45f))
            {
                continue;
            }

            shouldOcclude = true;
            break;
        }

        if (isOccludedByStatue == shouldOcclude)
        {
            return;
        }

        isOccludedByStatue = shouldOcclude;
        ApplySpriteVisibility(!shouldOcclude);
    }

    private void ApplySpriteVisibility(bool visible)
    {
        CacheSpriteRenderers();
        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[index];
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }
    }

    private static float GetCollisionScale(EnemyProfile sourceProfile)
    {
        return sourceProfile != null
            && string.Equals(sourceProfile.EnemyKey, "DireEnemy1", System.StringComparison.OrdinalIgnoreCase)
            ? 0.75f
            : 1f;
    }

    private Vector2 ResolveMovement(Vector2 currentPosition, Vector2 delta, Vector2 playerPosition)
    {
        if (delta.sqrMagnitude <= 0.000001f)
        {
            return currentPosition;
        }

        Vector2 directTarget = currentPosition + delta;
        if (!IsBlockedAt(directTarget))
        {
            avoidancePersistTimer = 0f;
            return directTarget;
        }

        Vector2 desiredDirection = delta.normalized;
        Vector2 toPlayer = (playerPosition - currentPosition).normalized;
        float stepDistance = delta.magnitude;
        bool foundBypass = false;
        Vector2 bestPosition = currentPosition;
        float bestScore = float.NegativeInfinity;

        TryAxisFallback(currentPosition, delta, ref bestPosition, ref bestScore, ref foundBypass, toPlayer);

        float primarySign = avoidancePersistTimer > 0f ? avoidanceDirectionSign : (Random.value < 0.5f ? -1f : 1f);
        EvaluateAvoidanceCandidates(currentPosition, desiredDirection, toPlayer, stepDistance, primarySign, ref bestPosition, ref bestScore, ref foundBypass);
        EvaluateAvoidanceCandidates(currentPosition, desiredDirection, toPlayer, stepDistance, -primarySign, ref bestPosition, ref bestScore, ref foundBypass);

        if (foundBypass)
        {
            return bestPosition;
        }

        Vector2 shortStep = desiredDirection * Mathf.Max(0.05f, stepDistance * 0.45f);
        Vector2 shortTarget = currentPosition + shortStep;
        if (!IsBlockedAt(shortTarget))
        {
            return shortTarget;
        }

        return currentPosition;
    }

    private void TryAxisFallback(
        Vector2 currentPosition,
        Vector2 delta,
        ref Vector2 bestPosition,
        ref float bestScore,
        ref bool foundBypass,
        Vector2 toPlayer)
    {
        Vector2 xTarget = currentPosition + new Vector2(delta.x, 0f);
        EvaluateCandidate(xTarget, currentPosition, toPlayer, Mathf.Sign(delta.x), ref bestPosition, ref bestScore, ref foundBypass);

        Vector2 yTarget = currentPosition + new Vector2(0f, delta.y);
        EvaluateCandidate(yTarget, currentPosition, toPlayer, Mathf.Sign(delta.y), ref bestPosition, ref bestScore, ref foundBypass);
    }

    private void EvaluateAvoidanceCandidates(
        Vector2 currentPosition,
        Vector2 desiredDirection,
        Vector2 toPlayer,
        float stepDistance,
        float turnSign,
        ref Vector2 bestPosition,
        ref float bestScore,
        ref bool foundBypass)
    {
        for (int index = 0; index < AvoidanceProbeAngles.Length; index++)
        {
            float angle = AvoidanceProbeAngles[index] * turnSign;
            Vector2 candidateDirection = Quaternion.Euler(0f, 0f, angle) * desiredDirection;
            Vector2 candidatePosition = currentPosition + (candidateDirection * stepDistance);
            EvaluateCandidate(candidatePosition, currentPosition, toPlayer, turnSign, ref bestPosition, ref bestScore, ref foundBypass);
        }
    }

    private void EvaluateCandidate(
        Vector2 candidatePosition,
        Vector2 currentPosition,
        Vector2 toPlayer,
        float turnSign,
        ref Vector2 bestPosition,
        ref float bestScore,
        ref bool foundBypass)
    {
        if (IsBlockedAt(candidatePosition))
        {
            return;
        }

        Vector2 candidateDelta = candidatePosition - currentPosition;
        if (candidateDelta.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Vector2 candidateDirection = candidateDelta.normalized;
        float alignmentScore = Vector2.Dot(candidateDirection, toPlayer) * 4f;
        float distanceScore = -Vector2.Distance(candidatePosition, currentPosition);
        float sideBias = Mathf.Approximately(turnSign, avoidanceDirectionSign) ? 0.12f : 0f;
        float score = alignmentScore + distanceScore + sideBias;
        if (foundBypass && score <= bestScore)
        {
            return;
        }

        foundBypass = true;
        bestScore = score;
        bestPosition = candidatePosition;
        if (Mathf.Abs(turnSign) > 0.01f)
        {
            avoidanceDirectionSign = Mathf.Sign(turnSign);
            avoidancePersistTimer = 0.35f;
        }
    }

    private bool IsBlockedAt(Vector2 position)
    {
        Vector2 overlapSize = new Vector2(Mathf.Max(0.45f, hitRadius * 1.15f), Mathf.Max(0.4f, hitRadius * 0.9f));
        Collider2D[] hits = Physics2D.OverlapBoxAll(position, overlapSize, 0f);
        for (int index = 0; index < hits.Length; index++)
        {
            Collider2D hit = hits[index];
            if (hit == null || hit.isTrigger)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform) || transform.IsChildOf(hit.transform))
            {
                continue;
            }

            if (!ObstacleMarker.IsObstacle(hit))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
