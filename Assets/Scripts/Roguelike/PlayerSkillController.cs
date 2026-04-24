using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerSkillController : MonoBehaviour
{
    private sealed class Player3Projectile
    {
        public GameObject GameObject;
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector2 Direction;
        public Vector2 PreviousPosition;
        public readonly HashSet<int> HitEnemyIds = new HashSet<int>();
    }

    private const float Player2FollowDistance = 5f;
    private const float Player2VerticalOffset = 1f;
    private const float Player2AttackDistance = 5.4f;
    private const int SkillSortingOffset = 4;
    private const float Player3ProjectileSpeed = 95f;
    private const float Player3ProjectileScale = 1.45f;
    private const float Player3ProjectileHitRadius = 1.1f;

    private readonly List<Player3Projectile> player3Projectiles = new List<Player3Projectile>();

    private RoguelikeGameManager owner;
    private Transform playerTransform;
    private PlayerRuntimeStats playerStats;
    private PlayerSkillProfile profile;
    private Transform skillRoot;
    private Transform spriteRoot;
    private SpriteRenderer spriteRenderer;
    private Animation legacyAnimation;
    private AnimationClip legacyClip;
    private Animator animator;
    private AnimationClip player2IdleClip;
    private AnimationClip player2RunClip;
    private float effectRemaining;
    private float animationTime;
    private float player2AttackCooldown;
    private float player3ShotTimer;
    private int player3ShotsRemaining;
    private bool player3MoveBuffApplied;

    public static PlayerSkillController EnsureExists(GameObject host)
    {
        PlayerSkillController existing = host.GetComponent<PlayerSkillController>();
        return existing != null ? existing : host.AddComponent<PlayerSkillController>();
    }

    public void Initialize(RoguelikeGameManager owner, Transform playerTransform, PlayerRuntimeStats playerStats, PlayerType playerType)
    {
        this.owner = owner;
        this.playerTransform = playerTransform;
        this.playerStats = playerStats;
        profile = PlayerSkillRepository.GetProfile(playerType);
        ResolveSkillObjects(playerType);
        ResetVisualState();

        if (profile.AutoActivate)
        {
            owner.SetSkillCooldown(float.PositiveInfinity);
        }
    }

    public void Tick(float deltaTime)
    {
        if (owner == null || playerTransform == null || profile == null)
        {
            return;
        }

        UpdatePlayer3Projectiles(deltaTime);
        UpdateEffect(deltaTime);

        if (profile.PlayerType == PlayerType.Player2)
        {
            UpdatePlayer2Companion(deltaTime);
            return;
        }

        if (!owner.CanAcceptPlayerInput || !owner.IsSkillReady)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ActivateSkill();
        }
    }

    private void ActivateSkill()
    {
        switch (profile.PlayerType)
        {
            case PlayerType.Player1:
                owner.EnableForcedPickupMagnet(Mathf.Max(1.2f, profile.Duration), 6f);
                owner.SetSkillCooldown(profile.Cooldown);
                BeginOverlayEffect(Mathf.Max(1.2f, profile.Duration), 1.25f, Vector2.zero, 0f);
                break;
            case PlayerType.Player3:
                owner.SetSkillCooldown(profile.Cooldown);
                owner.SetTemporarySkillBonuses(0, Mathf.Max(0.1f, playerStats.BaseMoveSpeed * profile.BuffMoveSpeedPercent), 0f);
                player3MoveBuffApplied = true;
                effectRemaining = Mathf.Max(0.05f, profile.Duration);
                player3ShotsRemaining = Mathf.Max(1, profile.RepeatedShots);
                player3ShotTimer = Mathf.Max(0.01f, profile.RepeatInterval);
                LaunchPlayer3Projectile(GetAimDirection());
                player3ShotsRemaining = Mathf.Max(0, player3ShotsRemaining - 1);
                break;
            case PlayerType.Player4:
                int maxHpBonus = Mathf.RoundToInt(playerStats.BaseMaxHp * profile.BuffMaxHpPercent) + profile.BuffMaxHpBonus;
                int attackBonus = Mathf.RoundToInt(playerStats.BaseAttack * profile.BuffAttackPercent) + profile.BuffAttackBonus;
                owner.AddPermanentSkillBonuses(maxHpBonus, attackBonus, profile.BuffMoveSpeedBonus, profile.BuffShootSpeedBonus);
                owner.SetSkillCooldown(profile.Cooldown);
                BeginOverlayEffect(Mathf.Max(0.85f, profile.Duration), 1.5f, Vector2.zero, 0f);
                break;
        }
    }

    private void UpdateEffect(float deltaTime)
    {
        if (effectRemaining <= 0f)
        {
            if (player3MoveBuffApplied)
            {
                owner.SetTemporarySkillBonuses(0, 0f, 0f);
                player3MoveBuffApplied = false;
            }

            if (profile != null && profile.PlayerType != PlayerType.Player2)
            {
                SetOverlayVisible(false);
            }

            return;
        }

        effectRemaining = Mathf.Max(0f, effectRemaining - deltaTime);

        switch (profile.PlayerType)
        {
            case PlayerType.Player1:
                UpdateLegacyOverlay(deltaTime, 1.25f, Vector2.zero, 0f);
                break;
            case PlayerType.Player3:
                UpdatePlayer3Skill(deltaTime);
                break;
            case PlayerType.Player4:
                UpdateLegacyOverlay(deltaTime, 1.5f, Vector2.zero, 0f);
                break;
        }
    }

    private void UpdatePlayer2Companion(float deltaTime)
    {
        if (skillRoot == null || spriteRoot == null)
        {
            return;
        }

        if (!skillRoot.gameObject.activeSelf)
        {
            skillRoot.gameObject.SetActive(true);
        }

        if (!spriteRoot.gameObject.activeSelf)
        {
            spriteRoot.gameObject.SetActive(true);
        }

        player2AttackCooldown = Mathf.Max(0f, player2AttackCooldown - deltaTime);
        EnsureSpriteRenderer();
        SetOverlayVisible(true);

        EnemyActor targetEnemy = owner.GetNearestEnemy(playerTransform.position);
        Vector3 currentPosition = skillRoot.position;
        float facing = Mathf.Sign(playerTransform.localScale.x == 0f ? 1f : playerTransform.localScale.x);
        Vector3 roamAnchor = playerTransform.position + new Vector3(facing * Player2FollowDistance, Player2VerticalOffset, 0f);
        Vector3 targetPosition = targetEnemy != null
            ? targetEnemy.transform.position + new Vector3(0f, 0.35f, 0f)
            : roamAnchor;

        float moveSpeed = Mathf.Max(20f, playerStats.MoveSpeed + 8f);
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * deltaTime);
        bool isRunning = (nextPosition - currentPosition).sqrMagnitude > 0.0001f;
        skillRoot.position = nextPosition;
        skillRoot.rotation = Quaternion.identity;
        skillRoot.localScale = new Vector3(
            Mathf.Sign((targetPosition - currentPosition).x == 0f ? facing : (targetPosition - currentPosition).x) * Mathf.Abs(playerTransform.localScale.x) * 0.82f,
            Mathf.Abs(playerTransform.localScale.y) * 0.82f,
            1f);

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = owner.GetActivePlayerSortingOrder() + SkillSortingOffset;
        }

        SamplePlayer2Animation(isRunning, deltaTime);

        if (targetEnemy == null)
        {
            return;
        }

        if (Vector2.Distance(skillRoot.position, targetEnemy.Position) > Player2AttackDistance || player2AttackCooldown > 0f)
        {
            return;
        }

        float attacksPerSecond = Mathf.Max(0.1f, owner.CurrentFireRate);
        player2AttackCooldown = 1f / attacksPerSecond;
        int damage = Mathf.Max(1, Mathf.CeilToInt(targetEnemy.MaxHp * profile.CompanionDamagePercentOfTargetMaxHp));
        targetEnemy.TakeDamage(damage);
    }

    private void BeginOverlayEffect(float duration, float scaleMultiplier, Vector2 offset, float rotationZ)
    {
        effectRemaining = Mathf.Max(0.05f, duration);
        animationTime = 0f;
        UpdateOverlayTransform(scaleMultiplier, offset, rotationZ);
        SetOverlayVisible(true);
    }

    private void UpdatePlayer3Skill(float deltaTime)
    {
        SetOverlayVisible(false);

        if (player3ShotsRemaining <= 0)
        {
            return;
        }

        player3ShotTimer -= deltaTime;
        while (player3ShotsRemaining > 0 && player3ShotTimer <= 0f)
        {
            LaunchPlayer3Projectile(GetAimDirection());
            player3ShotsRemaining--;
            player3ShotTimer += Mathf.Max(0.05f, profile.RepeatInterval);
        }
    }

    private void LaunchPlayer3Projectile(Vector2 direction)
    {
        if (spriteRoot == null)
        {
            return;
        }

        Vector2 normalizedDirection = direction.sqrMagnitude <= 0.001f ? Vector2.right : direction.normalized;
        GameObject projectileObject = Instantiate(spriteRoot.gameObject, skillRoot.parent != null ? skillRoot.parent : null);
        projectileObject.name = "Player3ProjectileRuntime";
        projectileObject.SetActive(true);

        Transform projectileTransform = projectileObject.transform;
        Vector2 spawnPosition = (Vector2)playerTransform.position + (normalizedDirection * Mathf.Max(2.2f, Mathf.Abs(playerTransform.localScale.x) * 0.5f));
        projectileTransform.position = spawnPosition;
        projectileTransform.rotation = Quaternion.Euler(0f, 0f, GetDirectionRotation(normalizedDirection));
        projectileTransform.localScale = new Vector3(
            Mathf.Abs(playerTransform.localScale.x) * Player3ProjectileScale,
            Mathf.Abs(playerTransform.localScale.y) * Player3ProjectileScale,
            1f);

        Animator projectileAnimator = projectileObject.GetComponent<Animator>();
        if (projectileAnimator != null)
        {
            projectileAnimator.enabled = false;
        }

        Animation projectileAnimation = projectileObject.GetComponent<Animation>();
        if (projectileAnimation != null)
        {
            projectileAnimation.enabled = false;
        }

        SpriteRenderer projectileRenderer = projectileObject.GetComponent<SpriteRenderer>();
        if (projectileRenderer == null)
        {
            projectileRenderer = projectileObject.AddComponent<SpriteRenderer>();
        }

        projectileRenderer.enabled = true;
        projectileRenderer.sortingOrder = owner.GetActivePlayerSortingOrder() + SkillSortingOffset + 1;

        player3Projectiles.Add(new Player3Projectile
        {
            GameObject = projectileObject,
            Transform = projectileTransform,
            Renderer = projectileRenderer,
            Direction = normalizedDirection,
            PreviousPosition = spawnPosition
        });
    }

    private void UpdatePlayer3Projectiles(float deltaTime)
    {
        for (int index = player3Projectiles.Count - 1; index >= 0; index--)
        {
            Player3Projectile projectile = player3Projectiles[index];
            if (projectile == null || projectile.Transform == null)
            {
                player3Projectiles.RemoveAt(index);
                continue;
            }

            projectile.PreviousPosition = projectile.Transform.position;
            projectile.Transform.position += (Vector3)(projectile.Direction * Player3ProjectileSpeed * deltaTime);
            projectile.Transform.rotation = Quaternion.Euler(0f, 0f, GetDirectionRotation(projectile.Direction));

            DealPlayer3ProjectileDamage(projectile);

            if (owner.IsInsideMapBounds(projectile.Transform.position, 0.2f))
            {
                continue;
            }

            Destroy(projectile.GameObject);
            player3Projectiles.RemoveAt(index);
        }
    }

    private void DealPlayer3ProjectileDamage(Player3Projectile projectile)
    {
        IReadOnlyList<EnemyActor> enemies = owner.ActiveEnemies;
        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyActor enemy = enemies[index];
            if (enemy == null || enemy.CurrentHp <= 0 || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            int enemyId = enemy.GetInstanceID();
            if (projectile.HitEnemyIds.Contains(enemyId))
            {
                continue;
            }

            if (!SegmentCircleIntersects(projectile.PreviousPosition, projectile.Transform.position, enemy.Position, enemy.HitRadius + Player3ProjectileHitRadius))
            {
                continue;
            }

            int damage = Mathf.Max(1, Mathf.CeilToInt(enemy.MaxHp * profile.DamagePercentOfTargetMaxHp));
            enemy.TakeDamage(damage);
            projectile.HitEnemyIds.Add(enemyId);
        }
    }

    private static bool SegmentCircleIntersects(Vector2 start, Vector2 end, Vector2 center, float radius)
    {
        Vector2 segment = end - start;
        float segmentLengthSqr = segment.sqrMagnitude;
        if (segmentLengthSqr <= 0.0001f)
        {
            return (center - start).sqrMagnitude <= radius * radius;
        }

        float hitT = Mathf.Clamp01(Vector2.Dot(center - start, segment) / segmentLengthSqr);
        Vector2 closestPoint = start + (segment * hitT);
        return (center - closestPoint).sqrMagnitude <= radius * radius;
    }

    private void UpdateLegacyOverlay(float deltaTime, float scaleMultiplier, Vector2 offset, float rotationZ)
    {
        UpdateOverlayTransform(scaleMultiplier, offset, rotationZ);
        EnsureSpriteRenderer();
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.enabled = true;
        if (legacyClip == null)
        {
            return;
        }

        animationTime += deltaTime;
        float clipLength = Mathf.Max(0.05f, legacyClip.length);
        legacyClip.SampleAnimation(spriteRoot.gameObject, Mathf.Repeat(animationTime, clipLength));
    }

    private void SamplePlayer2Animation(bool isRunning, float deltaTime)
    {
        AnimationClip clip = isRunning ? player2RunClip : player2IdleClip;
        if (clip == null || spriteRoot == null)
        {
            return;
        }

        animationTime += deltaTime;
        float clipLength = Mathf.Max(0.05f, clip.length);
        clip.SampleAnimation(spriteRoot.gameObject, Mathf.Repeat(animationTime, clipLength));
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private void UpdateOverlayTransform(float scaleMultiplier, Vector2 offset, float rotationZ)
    {
        if (skillRoot == null || playerTransform == null)
        {
            return;
        }

        skillRoot.position = playerTransform.position + (Vector3)offset;
        skillRoot.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        skillRoot.localScale = new Vector3(
            Mathf.Abs(playerTransform.localScale.x) * scaleMultiplier,
            Mathf.Abs(playerTransform.localScale.y) * scaleMultiplier,
            1f);

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = owner.GetActivePlayerSortingOrder() + SkillSortingOffset;
        }
    }

    private void ResolveSkillObjects(PlayerType playerType)
    {
        Transform skillsRoot = GameObject.Find("Skills")?.transform;
        skillRoot = skillsRoot != null ? skillsRoot.Find(playerType + "Skill") : null;
        spriteRoot = skillRoot != null ? skillRoot.Find("Sprite") : null;
        if (skillRoot == null || spriteRoot == null)
        {
            return;
        }

        skillRoot.gameObject.SetActive(true);
        spriteRoot.gameObject.SetActive(true);

        spriteRenderer = spriteRoot.GetComponent<SpriteRenderer>();
        legacyAnimation = spriteRoot.GetComponent<Animation>();
        legacyClip = legacyAnimation != null ? legacyAnimation.clip : null;
        if (legacyClip == null)
        {
            legacyClip = profile.LoadAnimationClip();
        }

        if (legacyClip != null && !legacyClip.legacy)
        {
            legacyClip.legacy = true;
        }

        if (legacyAnimation != null)
        {
            legacyAnimation.enabled = false;
        }

        animator = spriteRoot.GetComponent<Animator>();
        EnsureSpriteRenderer();
        if (animator != null)
        {
            ResolvePlayer2Clips();
            animator.enabled = false;
        }

        if (profile.PlayerType == PlayerType.Player3 && spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    private void ResolvePlayer2Clips()
    {
        player2IdleClip = null;
        player2RunClip = null;
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int index = 0; index < clips.Length; index++)
        {
            AnimationClip clip = clips[index];
            if (clip == null)
            {
                continue;
            }

            string clipName = clip.name.ToLowerInvariant();
            if (player2IdleClip == null && clipName.Contains("idle"))
            {
                player2IdleClip = clip;
            }
            else if (player2RunClip == null && clipName.Contains("run"))
            {
                player2RunClip = clip;
            }
        }
    }

    private void EnsureSpriteRenderer()
    {
        if (spriteRoot == null)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = spriteRoot.gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.enabled = profile != null && profile.PlayerType == PlayerType.Player2;
        if (owner != null)
        {
            spriteRenderer.sortingOrder = owner.GetActivePlayerSortingOrder() + SkillSortingOffset;
        }
    }

    private void ResetVisualState()
    {
        effectRemaining = 0f;
        animationTime = 0f;
        player2AttackCooldown = 0f;
        player3ShotTimer = 0f;
        player3ShotsRemaining = 0;
        player3MoveBuffApplied = false;
        owner?.SetTemporarySkillBonuses(0, 0f, 0f);
        ClearPlayer3Projectiles();

        if (profile != null && profile.PlayerType == PlayerType.Player2)
        {
            if (skillRoot != null && playerTransform != null)
            {
                skillRoot.position = playerTransform.position + new Vector3(0f, Player2VerticalOffset, 0f);
            }

            UpdatePlayer2Companion(0f);
            return;
        }

        SetOverlayVisible(false);
    }

    private void ClearPlayer3Projectiles()
    {
        for (int index = player3Projectiles.Count - 1; index >= 0; index--)
        {
            if (player3Projectiles[index]?.GameObject != null)
            {
                Destroy(player3Projectiles[index].GameObject);
            }
        }

        player3Projectiles.Clear();
    }

    private void SetOverlayVisible(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
    }

    private Vector2 GetAimDirection()
    {
        Camera camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (camera == null)
        {
            return playerTransform.right;
        }

        Vector3 mouse = Input.mousePosition;
        mouse.z = -camera.transform.position.z;
        Vector3 world = camera.ScreenToWorldPoint(mouse);
        Vector2 direction = world - playerTransform.position;
        return direction.sqrMagnitude <= 0.001f ? Vector2.right : direction.normalized;
    }

    private static float GetDirectionRotation(Vector2 direction)
    {
        Vector2 normalizedDirection = direction.sqrMagnitude <= 0.001f ? Vector2.right : direction.normalized;
        return Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg - 90f;
    }
}
