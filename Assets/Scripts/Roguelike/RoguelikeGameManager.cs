using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RoguelikeGameManager : MonoBehaviour
{
    private sealed class ActiveWaveSpawn
    {
        public EnemyProfile Profile;
        public int RemainingCount;
        public float SpawnInterval;
        public float Timer;
        public bool IsElite;
    }

    private sealed class BulletHitInfo
    {
        public EnemyActor Enemy;
        public float HitT;
    }

    private sealed class PendingBurstShot
    {
        public Vector3 MuzzlePosition;
        public Vector2 Direction;
        public int Damage;
        public float Speed;
        public float Lifetime;
        public int Pierce;
        public float Scale;
        public float DelayRemaining;
    }

    public sealed class OwnedPowerCardInfo
    {
        public PowerCardData Card;
        public int StackCount;
    }

    private sealed class SessionBonuses
    {
        public int BonusHp;
        public int BonusAttack;
        public float BonusMoveSpeed;
        public float BonusShootRate;
        public float BonusBulletSpeed;
        public int BonusProjectileCount;
        public int BonusVolleyCount;
        public int BonusBurstCount;
        public int BonusPierce;
        public float BonusPickupRadius;
        public int BonusHealOnPickup;

        public void Clear()
        {
            BonusHp = 0;
            BonusAttack = 0;
            BonusMoveSpeed = 0f;
            BonusShootRate = 0f;
            BonusBulletSpeed = 0f;
            BonusProjectileCount = 0;
            BonusVolleyCount = 0;
            BonusBurstCount = 0;
            BonusPierce = 0;
            BonusPickupRadius = 0f;
            BonusHealOnPickup = 0;
        }
    }

    public static RoguelikeGameManager Instance { get; private set; }

    private readonly Dictionary<string, ComponentPool<EnemyActor>> enemyPools = new Dictionary<string, ComponentPool<EnemyActor>>(System.StringComparer.OrdinalIgnoreCase);
    private readonly List<EnemyActor> activeEnemies = new List<EnemyActor>();
    private readonly List<Bullet> activeBullets = new List<Bullet>();
    private readonly List<PendingBurstShot> pendingBurstShots = new List<PendingBurstShot>();
    private readonly List<ExperiencePickup> activePickups = new List<ExperiencePickup>();
    private readonly List<FloatingDamageText> activeDamageTexts = new List<FloatingDamageText>();
    private readonly List<ActiveWaveSpawn> activeWaveSpawns = new List<ActiveWaveSpawn>();
    private readonly Dictionary<string, int> cardStacks = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
    private readonly List<PowerCardData> currentCardChoices = new List<PowerCardData>();
    private readonly List<OwnedPowerCardInfo> ownedPowerCards = new List<OwnedPowerCardInfo>();
    private readonly SessionBonuses sessionBonuses = new SessionBonuses();

    private InGameConfigData inGameConfig;
    private MapConfigData mapConfig;
    private SaveConfigData saveConfig;
    private WeaponProfile weaponProfile;
    private ComponentPool<Bullet> bulletPool;
    private ComponentPool<ExperiencePickup> pickupPool;
    private ComponentPool<FloatingDamageText> damageTextPool;
    private Transform poolRoot;
    private Transform bulletTemplate;
    private Transform enemyTemplateRoot;
    private Transform activePlayer;
    private PlayerRuntimeStats playerStats;
    private Ak47 activeWeapon;
    private CameraFollow cameraFollow;
    private MapGenerator mapGenerator;
    private CharacterAnimationBridge playerAnimation;
    private AdaptiveDifficultyMonitor adaptiveDifficulty;

    private float elapsedTime;
    private float nextSnapshotTimer;
    private float nextAutoSaveTimer;
    private float reloadRemaining;
    private float activeSkillCooldownRemaining;
    private float playerTerrainDamageTimer;
    private float playerHitRadius = 1.05f;
    private float expToNextLevel;
    private float currentExp;
    private int currentWave = 1;
    private int playerLevel = 1;
    private int currentAmmo;
    private int rewindUsesRemaining;
    private int pendingLevelChoices;
    private int earnedGold;
    private int earnedUserExp;

    private bool initialized;
    private bool loadingFromSave;
    private bool isPaused;
    private bool showPauseMenu;
    private bool showSettlement;
    private bool showUpgradeChoices;
    private bool showWaterEffectPrompt;
    private bool showRewindCountdown;
    private bool isRestoringSnapshot;
    private bool hasSettledRewards;
    private bool hasShownWaterEffectPrompt;
    private bool wasPlayerInWaterLastFrame;
    private bool isPlayerDeathSequenceRunning;
    private int rewindCountdownValue;

    private const float EnemySpawnDistanceMultiplier = 4f;
    private const float EnemyBaseHpMultiplier = 2.4f;
    private const float BurstShotDelayStep = 0.06f;
    private const float BurstSpeedLeadMultiplier = 1.08f;
    private const float BurstSpeedTrailMultiplier = 0.82f;
    private const int RewindCountdownSeconds = 3;
    private const float EnemyWaveHpScalePerWave = 0.18f;
    private const float EnemyWaveAttackScalePerWave = 0.11f;
    private const float EnemyWaveMoveSpeedGain = 0.09f;
    private const float EnemyWaveAttackIntervalReduction = 0.035f;
    private const float EnemyWaveContactRangeGain = 0.035f;
    private const float EnemyWaveScaleGain = 0.012f;
    private const int MaxAdditionalVolleyCount = 2;

    public PlayerRuntimeStats PlayerStats => playerStats;
    public float PlayerHitRadius => playerHitRadius;
    public float ElapsedTime => elapsedTime;
    public int CurrentWave => currentWave;
    public float NextWaveIn => Mathf.Max(0f, inGameConfig != null ? inGameConfig.WaveDuration - (elapsedTime % inGameConfig.WaveDuration) : 0f);
    public int PlayerLevel => playerLevel;
    public float CurrentExp => currentExp;
    public float ExpToNextLevel => Mathf.Max(1f, expToNextLevel);
    public float ExpRatio => Mathf.Clamp01(ExpToNextLevel <= 0f ? 0f : CurrentExp / ExpToNextLevel);
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => weaponProfile != null ? weaponProfile.MaxAmmo : 0;
    public int CurrentExtraPierce => Mathf.Max(0, (weaponProfile != null ? weaponProfile.Pierce : 0) + sessionBonuses.BonusPierce);
    public int CurrentVolleyCount => 1 + Mathf.Clamp(sessionBonuses.BonusProjectileCount + sessionBonuses.BonusVolleyCount, 0, MaxAdditionalVolleyCount);
    public int CurrentBurstCount => 1 + Mathf.Max(0, sessionBonuses.BonusBurstCount);
    public float CurrentFireRate => weaponProfile != null
        ? Mathf.Max(0.1f, weaponProfile.ShootRate + (playerStats != null ? playerStats.ShootSpeed * 0.25f : 0f))
        : 1f;
    public float ReloadRemaining => reloadRemaining;
    public bool IsReloading => reloadRemaining > 0f;
    public int RewindUsesRemaining => rewindUsesRemaining;
    public bool IsPaused => isPaused;
    public bool ShowPauseMenu => showPauseMenu;
    public bool ShowSettlement => showSettlement;
    public bool ShowUpgradeChoices => showUpgradeChoices;
    public bool ShowWaterEffectPrompt => showWaterEffectPrompt;
    public bool ShowRewindCountdown => showRewindCountdown;
    public int RewindCountdownValue => rewindCountdownValue;
    public bool CanAcceptPlayerInput => initialized
        && !isPaused
        && !showPauseMenu
        && !showSettlement
        && !showUpgradeChoices
        && !showWaterEffectPrompt
        && !showRewindCountdown
        && !isRestoringSnapshot
        && !isPlayerDeathSequenceRunning
        && (playerStats == null || playerStats.CurrentHp > 0);
    public IReadOnlyList<EnemyActor> ActiveEnemies => activeEnemies;
    public IReadOnlyList<PowerCardData> CurrentCardChoices => currentCardChoices;
    public IReadOnlyList<OwnedPowerCardInfo> OwnedPowerCards => ownedPowerCards;
    public int EarnedGold => earnedGold;
    public int EarnedUserExp => earnedUserExp;
    public float BehaviorEntropy => adaptiveDifficulty != null ? adaptiveDifficulty.CurrentEntropy : 0.5f;
    public AdaptiveDifficultyMonitor.DifficultyBand CurrentDifficultyBand => adaptiveDifficulty != null ? adaptiveDifficulty.CurrentBand : AdaptiveDifficultyMonitor.DifficultyBand.Normal;
    public int CurrentPlayerUpgradeLevel => UserProgressRepository.GetUpgradeLevel(GameSelectionConfig.CurrentPlayerType);
    public Vector2 PlayerPosition => activePlayer != null ? activePlayer.position : Vector2.zero;
    public TerrainSurfaceType CurrentPlayerTerrainType => activePlayer != null ? GetTerrainType(activePlayer.position) : TerrainSurfaceType.Ground;
    public float MapMinX => TryGetPlayableMapBounds(out float minX, out _, out _, out _) ? minX : -1f;
    public float MapMaxX => TryGetPlayableMapBounds(out _, out float maxX, out _, out _) ? maxX : 1f;
    public float MapMinY => TryGetPlayableMapBounds(out _, out _, out float minY, out _) ? minY : -1f;
    public float MapMaxY => TryGetPlayableMapBounds(out _, out _, out _, out float maxY) ? maxY : 1f;

    public bool IsInsideMapBounds(Vector2 point, float padding = 0f)
    {
        if (!TryGetPlayableMapBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            return true;
        }

        float safePaddingX = Mathf.Min(Mathf.Max(0f, padding), Mathf.Max(0f, (maxX - minX) * 0.5f));
        float safePaddingY = Mathf.Min(Mathf.Max(0f, padding), Mathf.Max(0f, (maxY - minY) * 0.5f));
        return point.x >= minX + safePaddingX
            && point.x <= maxX - safePaddingX
            && point.y >= minY + safePaddingY
            && point.y <= maxY - safePaddingY;
    }

    public int GetCardStack(string cardKey)
    {
        return cardStacks.TryGetValue(cardKey, out int stackCount) ? stackCount : 0;
    }

    public static RoguelikeGameManager EnsureExists(GameObject host)
    {
        if (Instance != null)
        {
            return Instance;
        }

        RoguelikeGameManager existing = host.GetComponent<RoguelikeGameManager>();
        return existing != null ? existing : host.AddComponent<RoguelikeGameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        TryInitialize();
        if (!initialized)
        {
            return;
        }

        if (showWaterEffectPrompt)
        {
            if (Input.anyKeyDown)
            {
                CloseWaterEffectPrompt();
            }

            return;
        }

        if (showSettlement || showRewindCountdown || isRestoringSnapshot || isPlayerDeathSequenceRunning)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            BeginRewind();
        }

        UpdateReload(Time.unscaledDeltaTime);
        activeSkillCooldownRemaining = Mathf.Max(0f, activeSkillCooldownRemaining - Time.unscaledDeltaTime);

        if (showPauseMenu || showUpgradeChoices || isPaused)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        elapsedTime += deltaTime;
        nextSnapshotTimer += deltaTime;
        nextAutoSaveTimer += deltaTime;
        UpdateAdaptiveDifficulty(deltaTime);

        UpdateWaveState();
        UpdateWaveSpawns(deltaTime);
        UpdatePlayerTerrainEffects(deltaTime);
        if (showSettlement || showWaterEffectPrompt || isPaused)
        {
            return;
        }

        UpdatePendingBurstShots(deltaTime);
        UpdateBullets(deltaTime);
        UpdateEnemies(deltaTime);
        UpdatePickups(deltaTime);
        UpdateDamageTexts(deltaTime);

        if (nextSnapshotTimer >= saveConfig.SnapshotInterval)
        {
            nextSnapshotTimer = 0f;
            SessionSaveRepository.WriteSnapshot(CaptureSnapshot(), saveConfig.SnapshotKeepCount);
        }

        if (nextAutoSaveTimer >= saveConfig.AutoSaveSeconds)
        {
            nextAutoSaveTimer = 0f;
            SaveSession();
        }
    }

    private void TryInitialize()
    {
        if (initialized)
        {
            return;
        }

        inGameConfig = RoguelikeDataRepository.GetInGameConfig();
        mapConfig = RoguelikeDataRepository.GetMapConfig();
        saveConfig = RoguelikeDataRepository.GetSaveConfig();

        if (!ResolvePlayerContext())
        {
            return;
        }

        EnsurePoolRoot();
        PrepareTemplates();
        PreparePools();
        ResetForNewRun();

        if (SessionSaveRepository.HasPendingSaveSelection() || SessionSaveRepository.HasSavedSession())
        {
            loadingFromSave = true;
            LoadSavedSession();
        }
        else
        {
            QueueWave(currentWave);
        }

        initialized = true;
    }

    private bool ResolvePlayerContext()
    {
        Transform playersRoot = GameObject.Find("Players")?.transform;
        if (playersRoot == null)
        {
            return false;
        }

        for (int index = 0; index < playersRoot.childCount; index++)
        {
            Transform candidate = playersRoot.GetChild(index);
            if (!candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            activePlayer = candidate;
            break;
        }

        if (activePlayer == null)
        {
            return false;
        }

        playerStats = activePlayer.GetComponent<PlayerRuntimeStats>();
        if (playerStats == null)
        {
            playerStats = activePlayer.gameObject.AddComponent<PlayerRuntimeStats>();
        }

        playerAnimation = CharacterAnimationBridge.GetOrCreate(activePlayer.gameObject);

        weaponProfile = RoguelikeDataRepository.GetWeaponProfile(GameSelectionConfig.CurrentWeaponType);
        activeWeapon = GameSceneSelectionApplier.FindSelectedWeapon(activePlayer);
        cameraFollow = FindAnyObjectByType<CameraFollow>();
        mapGenerator = FindAnyObjectByType<MapGenerator>();
        if (cameraFollow != null)
        {
            cameraFollow.target = activePlayer;
        }

        return true;
    }

    private void PrepareTemplates()
    {
        enemyTemplateRoot = GameObject.Find("Enemys")?.transform;
        bulletTemplate = GameObject.Find("bullet")?.transform;

        if (enemyTemplateRoot != null)
        {
            for (int index = 0; index < enemyTemplateRoot.childCount; index++)
            {
                enemyTemplateRoot.GetChild(index).gameObject.SetActive(false);
            }
        }

        if (bulletTemplate != null)
        {
            bulletTemplate.gameObject.SetActive(false);
        }
    }

    private void PreparePools()
    {
        if (bulletPool == null)
        {
            bulletPool = new ComponentPool<Bullet>(CreateBulletInstance);
            bulletPool.Warm(Mathf.Max(24, weaponProfile.PoolSize));
        }

        if (pickupPool == null)
        {
            pickupPool = new ComponentPool<ExperiencePickup>(CreatePickupInstance);
            pickupPool.Warm(48);
        }

        if (damageTextPool == null)
        {
            damageTextPool = new ComponentPool<FloatingDamageText>(CreateDamageTextInstance);
            damageTextPool.Warm(12);
        }

        enemyPools.Clear();
        EnemyProfile normalEnemy = RoguelikeDataRepository.GetEnemyProfile("Enemy1");
        EnemyProfile eliteEnemy = RoguelikeDataRepository.GetDefaultEliteProfile();
        CreateEnemyPool(normalEnemy);
        CreateEnemyPool(eliteEnemy);
    }

    private void ResetForNewRun()
    {
        sessionBonuses.Clear();
        PlayerProfile profile = RoguelikeDataRepository.GetPlayerProfile(GameSelectionConfig.CurrentPlayerType);
        playerStats.ApplyProfile(profile);
        ApplyBonusesToPlayer();

        currentWave = 1;
        playerLevel = 1;
        currentExp = 0f;
        expToNextLevel = GetRequiredExpForNextLevel(playerLevel);
        currentAmmo = weaponProfile.MaxAmmo;
        reloadRemaining = 0f;
        activeSkillCooldownRemaining = 0f;
        playerTerrainDamageTimer = 0f;
        rewindUsesRemaining = inGameConfig.RewindUsesPerRun;
        elapsedTime = 0f;
        nextSnapshotTimer = 0f;
        nextAutoSaveTimer = 0f;
        pendingLevelChoices = 0;
        earnedGold = 0;
        earnedUserExp = 0;
        hasSettledRewards = false;
        loadingFromSave = false;
        isPaused = false;
        showPauseMenu = false;
        showSettlement = false;
        showUpgradeChoices = false;
        showWaterEffectPrompt = false;
        showRewindCountdown = false;
        isRestoringSnapshot = false;
        isPlayerDeathSequenceRunning = false;
        hasShownWaterEffectPrompt = false;
        wasPlayerInWaterLastFrame = false;
        rewindCountdownValue = 0;
        cardStacks.Clear();
        ownedPowerCards.Clear();
        currentCardChoices.Clear();
        ReleaseAllBullets();
        ReleaseAllEnemies();
        ReleaseAllPickups();
        ReleaseAllDamageTexts();
        activeWaveSpawns.Clear();

        activePlayer.position = GetInitialPlayerSpawnPosition();
        ResetPlayerAnimationState();
        InitializeAdaptiveDifficulty();
    }

    public bool TryFireWeapon(Vector3 muzzlePosition, Vector2 direction)
    {
        if (!CanAcceptPlayerInput || playerStats == null || weaponProfile == null)
        {
            return false;
        }

        if (reloadRemaining > 0f)
        {
            return false;
        }

        if (currentAmmo <= 0)
        {
            BeginReload();
            return false;
        }

        int volleyCount = CurrentVolleyCount;
        int burstCount = CurrentBurstCount;
        float spread = Mathf.Max(8f, weaponProfile.SpreadAngle);
        int totalDamage = Mathf.Max(1, weaponProfile.Damage + playerStats.Attack + sessionBonuses.BonusAttack);
        float bulletSpeed = weaponProfile.ProjectileSpeed + sessionBonuses.BonusBulletSpeed;
        int totalPierce = Mathf.Max(0, weaponProfile.Pierce + sessionBonuses.BonusPierce);
        Vector2 fireDirection = direction.sqrMagnitude <= 0.001f ? Vector2.right : direction.normalized;

        for (int burstIndex = 0; burstIndex < burstCount; burstIndex++)
        {
            float burstDelay = burstIndex * BurstShotDelayStep;
            float burstSpeed = bulletSpeed * GetBurstSpeedMultiplier(burstIndex, burstCount);
            for (int volleyIndex = 0; volleyIndex < volleyCount; volleyIndex++)
            {
                float angleOffset = volleyCount == 1 ? 0f : Mathf.Lerp(-spread, spread, volleyIndex / (float)(volleyCount - 1));
                Quaternion spreadRotation = Quaternion.Euler(0f, 0f, angleOffset);
                Vector2 finalDirection = spreadRotation * fireDirection;

                if (burstDelay <= 0f)
                {
                    SpawnBullet(
                        muzzlePosition,
                        finalDirection,
                        totalDamage,
                        burstSpeed,
                        weaponProfile.BulletLifetime,
                        totalPierce,
                        weaponProfile.ProjectileScale);
                    continue;
                }

                pendingBurstShots.Add(new PendingBurstShot
                {
                    MuzzlePosition = muzzlePosition,
                    Direction = finalDirection,
                    Damage = totalDamage,
                    Speed = burstSpeed,
                    Lifetime = weaponProfile.BulletLifetime,
                    Pierce = totalPierce,
                    Scale = weaponProfile.ProjectileScale,
                    DelayRemaining = burstDelay
                });
            }
        }

        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        if (currentAmmo <= 0)
        {
            BeginReload();
        }

        return true;
    }

    public bool ProcessBulletHit(Bullet bullet)
    {
        Vector2 segmentStart = bullet.PreviousPosition;
        Vector2 segmentEnd = bullet.Position;
        float hitDistance = bullet.HitRadius;
        List<BulletHitInfo> hitInfos = new List<BulletHitInfo>();

        for (int index = activeEnemies.Count - 1; index >= 0; index--)
        {
            EnemyActor enemy = activeEnemies[index];
            if (bullet.HasHitEnemy(enemy))
            {
                continue;
            }

            float combinedRadius = hitDistance + enemy.HitRadius;
            if (!SegmentCircleIntersects(segmentStart, segmentEnd, enemy.Position, combinedRadius, out float hitT))
            {
                continue;
            }

            hitInfos.Add(new BulletHitInfo
            {
                Enemy = enemy,
                HitT = hitT
            });
        }

        if (hitInfos.Count == 0)
        {
            return true;
        }

        hitInfos.Sort((left, right) => left.HitT.CompareTo(right.HitT));
        for (int index = 0; index < hitInfos.Count; index++)
        {
            EnemyActor targetEnemy = hitInfos[index].Enemy;
            if (targetEnemy == null || bullet.HasHitEnemy(targetEnemy))
            {
                continue;
            }

            bullet.RegisterEnemyHit(targetEnemy);
            Vector3 hitPosition = Vector3.Lerp(segmentStart, segmentEnd, hitInfos[index].HitT);
            Vector3 textPosition = hitPosition + new Vector3(0f, Mathf.Max(1.2f, targetEnemy.HitRadius * 0.55f), 0f);
            targetEnemy.TakeDamage(bullet.Damage, false);
            SpawnDamageText(textPosition, bullet.Damage, false);

            if (bullet.RemainingPierce <= 0)
            {
                return false;
            }

            bullet.ConsumePierce();
        }

        return true;
    }

    public void DamagePlayer(int damage)
    {
        if (showSettlement || isPlayerDeathSequenceRunning || playerStats == null)
        {
            return;
        }

        int actualDamage = Mathf.Max(1, damage);
        playerStats.ModifyCurrentHp(-actualDamage);
        SpawnDamageText(activePlayer.position + new Vector3(0f, 1.35f, 0f), actualDamage, true);
        if (playerStats.CurrentHp > 0)
        {
            return;
        }

        StartPlayerDeathSequence();
    }

    public void CollectPickup(ExperiencePickup pickup)
    {
        if (pickup == null)
        {
            return;
        }

        if (pickup.GrantsFreeLevel)
        {
            AddExperience(ExpToNextLevel * 0.25f);
        }
        else
        {
            AddExperience(pickup.ExperienceValue);
            if (sessionBonuses.BonusHealOnPickup > 0)
            {
                playerStats.ModifyCurrentHp(sessionBonuses.BonusHealOnPickup);
            }
        }

    }

    public void TogglePauseMenu()
    {
        if (!initialized || showSettlement || showUpgradeChoices || showRewindCountdown || isRestoringSnapshot)
        {
            return;
        }

        showPauseMenu = !showPauseMenu;
        SetPaused(showPauseMenu);
    }

    public void ChooseUpgradeCard(int cardIndex)
    {
        if (!showUpgradeChoices || cardIndex < 0 || cardIndex >= currentCardChoices.Count)
        {
            return;
        }

        PowerCardData card = currentCardChoices[cardIndex];
        int currentStack = cardStacks.TryGetValue(card.CardKey, out int stackValue) ? stackValue : 0;
        if (currentStack < card.MaxStacks)
        {
            cardStacks[card.CardKey] = currentStack + 1;
            RecalculateCardBonuses();
        }

        pendingLevelChoices = Mathf.Max(0, pendingLevelChoices - 1);
        currentCardChoices.Clear();
        showUpgradeChoices = false;

        if (pendingLevelChoices > 0)
        {
            OpenUpgradeChoicesIfNeeded();
            return;
        }

        SetPaused(false);
    }

    public void BeginRewind()
    {
        if (!CanAcceptPlayerInput || rewindUsesRemaining <= 0)
        {
            return;
        }

        if (!SessionSaveRepository.TryLoadLatestSnapshot(out SessionSnapshotData snapshot))
        {
            return;
        }

        adaptiveDifficulty?.RecordRewind();
        int remainingAfterRewind = Mathf.Max(0, rewindUsesRemaining - 1);
        StartCoroutine(RewindCountdownRoutine(snapshot, remainingAfterRewind));
    }

    public void SaveSession()
    {
        SessionSnapshotData snapshot = CaptureSnapshot();
        if (!SessionSaveRepository.TrySaveSession(snapshot, false, out _, out _))
        {
            Debug.LogError("SaveSession failed.");
        }
    }

    public void CreateManualSave()
    {
        SessionSaveRepository.CreateManualSave(CaptureSnapshot());
    }

    public void FinalizeRun()
    {
        BeginSettlement(true);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    public void QuitGameWithSave()
    {
        SessionSnapshotData snapshot = CaptureSnapshot();
        if (!SessionSaveRepository.TrySaveSession(snapshot, true, out _, out _))
        {
            Debug.LogError("QuitGameWithSave aborted because the save failed.");
            return;
        }

        UserProgressRepository.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public SessionSnapshotData CaptureSnapshot()
    {
        SessionSnapshotData snapshot = new SessionSnapshotData
        {
            PlayerType = GameSelectionConfig.CurrentPlayerType,
            WeaponType = GameSelectionConfig.CurrentWeaponType,
            ElapsedTime = elapsedTime,
            CurrentWave = currentWave,
            PlayerLevel = playerLevel,
            CurrentExp = currentExp,
            ExpToNextLevel = expToNextLevel,
            PlayerPositionX = activePlayer.position.x,
            PlayerPositionY = activePlayer.position.y,
            PlayerHp = playerStats.CurrentHp,
            CurrentAmmo = currentAmmo,
            ReloadRemaining = reloadRemaining,
            SkillCooldownRemaining = activeSkillCooldownRemaining,
            RewindUsesRemaining = rewindUsesRemaining,
            PendingLevelUpChoices = pendingLevelChoices
        };

        foreach (KeyValuePair<string, int> pair in cardStacks)
        {
            snapshot.Cards.Add(new SessionCardState
            {
                CardKey = pair.Key,
                StackCount = pair.Value
            });
        }

        for (int index = 0; index < activeEnemies.Count; index++)
        {
            EnemyActor enemy = activeEnemies[index];
            snapshot.Enemies.Add(new SessionEnemyState
            {
                EnemyKey = enemy.EnemyKey,
                PositionX = enemy.Position.x,
                PositionY = enemy.Position.y,
                CurrentHp = enemy.CurrentHp,
                IsElite = enemy.IsElite
            });
        }

        for (int index = 0; index < activePickups.Count; index++)
        {
            ExperiencePickup pickup = activePickups[index];
            snapshot.Pickups.Add(new SessionPickupState
            {
                PositionX = pickup.Position.x,
                PositionY = pickup.Position.y,
                ExperienceValue = pickup.ExperienceValue,
                GrantsFreeLevel = pickup.GrantsFreeLevel
            });
        }

        return snapshot;
    }

    private IEnumerator RewindCountdownRoutine(SessionSnapshotData snapshot, int forcedRewindUsesRemaining)
    {
        showPauseMenu = false;
        showUpgradeChoices = false;
        showSettlement = false;
        showWaterEffectPrompt = false;
        showRewindCountdown = true;
        rewindCountdownValue = RewindCountdownSeconds;
        SetPaused(true);

        for (int countdown = RewindCountdownSeconds; countdown >= 1; countdown--)
        {
            rewindCountdownValue = countdown;
            yield return new WaitForSecondsRealtime(1f);
        }

        rewindCountdownValue = 0;
        showRewindCountdown = false;
        yield return StartCoroutine(RestoreSnapshotRoutine(snapshot, forcedRewindUsesRemaining));
    }

    private void LoadSavedSession()
    {
        if (!SessionSaveRepository.TryLoadRequestedSession(out SessionSnapshotData snapshot))
        {
            loadingFromSave = false;
            QueueWave(currentWave);
            return;
        }

        StartCoroutine(RestoreSnapshotRoutine(snapshot, -1));
    }

    private IEnumerator RestoreSnapshotRoutine(SessionSnapshotData snapshot, int forcedRewindUsesRemaining)
    {
        isRestoringSnapshot = true;
        showPauseMenu = false;
        showUpgradeChoices = false;
        showSettlement = false;
        showRewindCountdown = false;
        rewindCountdownValue = 0;
        SetPaused(true);

        ReleaseAllBullets();
        yield return null;

        ReleaseAllEnemies();
        yield return null;

        ReleaseAllPickups();
        yield return null;

        GameSelectionConfig.CurrentPlayerType = snapshot.PlayerType;
        GameSelectionConfig.CurrentWeaponType = snapshot.WeaponType;
        GameSceneSelectionApplier.Apply(snapshot.PlayerType, snapshot.WeaponType);
        ResolvePlayerContext();

        weaponProfile = RoguelikeDataRepository.GetWeaponProfile(snapshot.WeaponType);
        currentWave = Mathf.Max(1, snapshot.CurrentWave);
        playerLevel = Mathf.Max(1, snapshot.PlayerLevel);
        currentExp = Mathf.Max(0f, snapshot.CurrentExp);
        expToNextLevel = Mathf.Max(1f, snapshot.ExpToNextLevel);
        currentAmmo = Mathf.Clamp(snapshot.CurrentAmmo, 0, weaponProfile.MaxAmmo);
        reloadRemaining = Mathf.Max(0f, snapshot.ReloadRemaining);
        activeSkillCooldownRemaining = Mathf.Max(0f, snapshot.SkillCooldownRemaining);
        playerTerrainDamageTimer = 0f;
        rewindUsesRemaining = Mathf.Max(0, snapshot.RewindUsesRemaining);
        if (forcedRewindUsesRemaining >= 0)
        {
            rewindUsesRemaining = Mathf.Min(rewindUsesRemaining, forcedRewindUsesRemaining);
        }
        pendingLevelChoices = Mathf.Max(0, snapshot.PendingLevelUpChoices);
        elapsedTime = Mathf.Max(0f, snapshot.ElapsedTime);
        nextSnapshotTimer = 0f;
        nextAutoSaveTimer = 0f;
        earnedGold = currentWave * inGameConfig.RewardGoldPerWave;
        earnedUserExp = currentWave * inGameConfig.RewardExpPerWave;
        cardStacks.Clear();
        for (int index = 0; index < snapshot.Cards.Count; index++)
        {
            cardStacks[snapshot.Cards[index].CardKey] = snapshot.Cards[index].StackCount;
        }

        RecalculateCardBonuses();
        activePlayer.position = ClampToPlayableMapBounds(new Vector3(snapshot.PlayerPositionX, snapshot.PlayerPositionY, 0f), playerHitRadius);
        playerStats.SetCurrentHp(snapshot.PlayerHp);
        isPlayerDeathSequenceRunning = false;
        ResetPlayerAnimationState();
        adaptiveDifficulty?.CaptureCurrentState(playerLevel, CurrentFireRate, GetCurrentAttackPower());
        activeWaveSpawns.Clear();
        QueueWave(currentWave);

        for (int index = 0; index < snapshot.Enemies.Count; index++)
        {
            SessionEnemyState enemyState = snapshot.Enemies[index];
            EnemyProfile profile = RoguelikeDataRepository.GetEnemyProfile(enemyState.EnemyKey);
            EnemyActor enemy = SpawnEnemy(profile, enemyState.IsElite, new Vector3(enemyState.PositionX, enemyState.PositionY, 0f));
            enemy.RestoreState(enemyState.CurrentHp);
            if (index % 4 == 0)
            {
                yield return null;
            }
        }

        for (int index = 0; index < snapshot.Pickups.Count; index++)
        {
            SessionPickupState pickupState = snapshot.Pickups[index];
            SpawnPickup(new Vector3(pickupState.PositionX, pickupState.PositionY, 0f), pickupState.ExperienceValue, pickupState.GrantsFreeLevel);
            if (index % 8 == 0)
            {
                yield return null;
            }
        }

        isRestoringSnapshot = false;
        loadingFromSave = false;
        SetPaused(false);
        OpenUpgradeChoicesIfNeeded();
    }

    private void UpdatePlayerTerrainEffects(float deltaTime)
    {
        if (playerStats == null || activePlayer == null)
        {
            return;
        }

        TerrainSurfaceType terrainType = GetTerrainType(activePlayer.position);
        bool isInWater = terrainType == TerrainSurfaceType.Water;

        if (isInWater && !wasPlayerInWaterLastFrame && !hasShownWaterEffectPrompt)
        {
            OpenWaterEffectPrompt();
            wasPlayerInWaterLastFrame = true;
            playerStats.SetEnvironmentMoveSpeedModifier(GetTerrainMoveSpeedModifier(activePlayer.position));
            return;
        }

        wasPlayerInWaterLastFrame = isInWater;
        float moveModifier = GetTerrainMoveSpeedModifier(activePlayer.position);
        playerStats.SetEnvironmentMoveSpeedModifier(moveModifier);

        float damagePerSecond = terrainType == TerrainSurfaceType.Water ? 1f : 0f;
        if (damagePerSecond <= 0f)
        {
            playerTerrainDamageTimer = 0f;
            return;
        }

        playerTerrainDamageTimer += deltaTime * damagePerSecond;
        while (playerTerrainDamageTimer >= 1f)
        {
            playerTerrainDamageTimer -= 1f;
            DamagePlayer(1);
            if (showSettlement)
            {
                break;
            }
        }
    }

    private void OpenWaterEffectPrompt()
    {
        hasShownWaterEffectPrompt = true;
        showWaterEffectPrompt = true;
        showPauseMenu = false;
        showRewindCountdown = false;
        GameVoiceManager.PlayFirstInWater();
        SetPaused(true);
    }

    private void CloseWaterEffectPrompt()
    {
        showWaterEffectPrompt = false;
        SetPaused(false);
    }

    public float GetTerrainMoveSpeedModifier(Vector3 position)
    {
        TerrainSurfaceType terrainType = GetTerrainType(position);
        if (terrainType == TerrainSurfaceType.Water)
        {
            return -2f;
        }

        if (terrainType == TerrainSurfaceType.Grass)
        {
            return 2f;
        }

        return 0f;
    }

    public float GetTerrainDamagePerSecond(Vector3 position)
    {
        return GetTerrainType(position) == TerrainSurfaceType.Water ? 1f : 0f;
    }

    private TerrainSurfaceType GetTerrainType(Vector3 position)
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindAnyObjectByType<MapGenerator>();
        }

        return mapGenerator != null ? mapGenerator.GetTerrainType(position) : TerrainSurfaceType.Ground;
    }

    private void UpdateWaveState()
    {
        int calculatedWave = Mathf.FloorToInt(elapsedTime / inGameConfig.WaveDuration) + 1;
        if (calculatedWave <= currentWave)
        {
            return;
        }

        currentWave = calculatedWave;
        QueueWave(currentWave);
    }

    private void QueueWave(int waveIndex)
    {
        activeWaveSpawns.Clear();
        List<WaveProfile> waveProfiles = RoguelikeDataRepository.GetWaveProfilesFor(waveIndex);
        float densityMultiplier = adaptiveDifficulty != null ? adaptiveDifficulty.CurrentEnemyDensityMultiplier : 1f;
        bool useSkilledProfile = adaptiveDifficulty != null && adaptiveDifficulty.CurrentBand == AdaptiveDifficultyMonitor.DifficultyBand.Skilled;
        float targetEliteRatio = adaptiveDifficulty != null ? adaptiveDifficulty.CurrentEliteRatio : 0f;

        for (int index = 0; index < waveProfiles.Count; index++)
        {
            WaveProfile wave = waveProfiles[index];
            EnemyProfile enemyProfile = RoguelikeDataRepository.GetEnemyProfile(wave.EnemyKey);
            int normalCount = ScaleSpawnCount(wave.SpawnCount, densityMultiplier);
            int eliteCount = 0;
            EnemyProfile eliteProfile = ResolveEliteProfile(wave);

            if (useSkilledProfile)
            {
                int totalCount = ScaleSpawnCount(wave.SpawnCount + Mathf.Max(0, wave.EliteCount), densityMultiplier);
                if (totalCount > 0)
                {
                    eliteCount = Mathf.Clamp(Mathf.RoundToInt(totalCount * targetEliteRatio), 0, totalCount);
                    if (eliteCount == 0)
                    {
                        eliteCount = 1;
                    }

                    normalCount = Mathf.Max(0, totalCount - eliteCount);
                }
            }
            else if (waveIndex % 5 == 0 && wave.EliteCount > 0)
            {
                eliteCount = ScaleSpawnCount(wave.EliteCount, densityMultiplier);
            }

            if (normalCount > 0)
            {
                activeWaveSpawns.Add(new ActiveWaveSpawn
                {
                    Profile = enemyProfile,
                    RemainingCount = normalCount,
                    SpawnInterval = wave.SpawnInterval,
                    Timer = 0f,
                    IsElite = false
                });
            }

            if (eliteCount > 0 && eliteProfile != null)
            {
                activeWaveSpawns.Add(new ActiveWaveSpawn
                {
                    Profile = eliteProfile,
                    RemainingCount = eliteCount,
                    SpawnInterval = Mathf.Max(0.85f, wave.SpawnInterval * (useSkilledProfile ? 1.35f : 2f)),
                    Timer = 0.25f,
                    IsElite = true
                });
            }
        }

        earnedGold = currentWave * inGameConfig.RewardGoldPerWave;
        earnedUserExp = currentWave * inGameConfig.RewardExpPerWave;
    }

    private void UpdateWaveSpawns(float deltaTime)
    {
        for (int index = 0; index < activeWaveSpawns.Count; index++)
        {
            ActiveWaveSpawn spawn = activeWaveSpawns[index];
            if (spawn.RemainingCount <= 0)
            {
                continue;
            }

            spawn.Timer -= deltaTime;
            if (spawn.Timer > 0f)
            {
                continue;
            }

            spawn.Timer = spawn.SpawnInterval;
            spawn.RemainingCount--;
            SpawnEnemy(spawn.Profile, spawn.IsElite, GetSpawnPositionAroundPlayer());
        }
    }

    private void UpdateBullets(float deltaTime)
    {
        for (int index = activeBullets.Count - 1; index >= 0; index--)
        {
            if (activeBullets[index].Tick(deltaTime))
            {
                continue;
            }

            ReleaseBullet(activeBullets[index]);
        }
    }

    private void UpdateEnemies(float deltaTime)
    {
        Vector3 playerPosition = activePlayer.position;
        for (int index = activeEnemies.Count - 1; index >= 0; index--)
        {
            if (activeEnemies[index].Tick(deltaTime, playerPosition))
            {
                continue;
            }

            HandleEnemyDefeated(activeEnemies[index]);
        }
    }

    private void UpdatePickups(float deltaTime)
    {
        float pickupRadius = inGameConfig.PickupRadius + sessionBonuses.BonusPickupRadius;
        Vector3 playerPosition = activePlayer.position;
        for (int index = activePickups.Count - 1; index >= 0; index--)
        {
            if (activePickups[index].Tick(deltaTime, playerPosition, pickupRadius))
            {
                continue;
            }

            ReleasePickup(activePickups[index]);
        }
    }

    private void UpdateDamageTexts(float deltaTime)
    {
        for (int index = activeDamageTexts.Count - 1; index >= 0; index--)
        {
            if (activeDamageTexts[index].Tick(deltaTime))
            {
                continue;
            }

            ReleaseDamageText(activeDamageTexts[index]);
        }
    }

    private void AddExperience(float amount)
    {
        currentExp += Mathf.Max(0f, amount);
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            playerLevel++;
            expToNextLevel = GetRequiredExpForNextLevel(playerLevel);
            pendingLevelChoices++;
        }

        OpenUpgradeChoicesIfNeeded();
    }

    private void OpenUpgradeChoicesIfNeeded()
    {
        if (pendingLevelChoices <= 0 || showUpgradeChoices || showSettlement)
        {
            return;
        }

        currentCardChoices.Clear();
        List<PowerCardData> allCards = RoguelikeDataRepository.GetPowerCards();
        List<PowerCardData> candidates = new List<PowerCardData>();
        for (int index = 0; index < allCards.Count; index++)
        {
            PowerCardData card = allCards[index];
            int stackCount = cardStacks.TryGetValue(card.CardKey, out int value) ? value : 0;
            if (stackCount >= card.MaxStacks || IsPowerCardAtGlobalCap(card))
            {
                continue;
            }

            int weight = Mathf.Max(1, card.Weight);
            for (int repeat = 0; repeat < weight; repeat++)
            {
                candidates.Add(card);
            }
        }

        int choiceCount = Mathf.Min(inGameConfig.CardChoiceCount, candidates.Count);
        while (currentCardChoices.Count < choiceCount && candidates.Count > 0)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            PowerCardData selected = candidates[randomIndex];
            if (!currentCardChoices.Contains(selected))
            {
                currentCardChoices.Add(selected);
            }

            candidates.RemoveAll(card => card.CardKey == selected.CardKey);
        }

        if (currentCardChoices.Count == 0)
        {
            pendingLevelChoices = 0;
            return;
        }

        showUpgradeChoices = true;
        SetPaused(true);
    }

    private void RecalculateCardBonuses()
    {
        sessionBonuses.Clear();
        ownedPowerCards.Clear();
        List<PowerCardData> cards = RoguelikeDataRepository.GetPowerCards();
        Dictionary<string, PowerCardData> lookup = new Dictionary<string, PowerCardData>(System.StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < cards.Count; index++)
        {
            lookup[cards[index].CardKey] = cards[index];
        }

        foreach (KeyValuePair<string, int> pair in cardStacks)
        {
            if (!lookup.TryGetValue(pair.Key, out PowerCardData card))
            {
                continue;
            }

            ownedPowerCards.Add(new OwnedPowerCardInfo
            {
                Card = card,
                StackCount = pair.Value
            });
            sessionBonuses.BonusHp += card.BonusHp * pair.Value;
            sessionBonuses.BonusAttack += card.BonusAttack * pair.Value;
            sessionBonuses.BonusMoveSpeed += card.BonusMoveSpeed * pair.Value;
            sessionBonuses.BonusShootRate += card.BonusShootRate * pair.Value;
            sessionBonuses.BonusBulletSpeed += card.BonusBulletSpeed * pair.Value;
            sessionBonuses.BonusProjectileCount += card.BonusProjectileCount * pair.Value;
            sessionBonuses.BonusVolleyCount += card.BonusVolleyCount * pair.Value;
            sessionBonuses.BonusBurstCount += card.BonusBurstCount * pair.Value;
            sessionBonuses.BonusPierce += card.BonusPierce * pair.Value;
            sessionBonuses.BonusPickupRadius += card.BonusPickupRadius * pair.Value;
            sessionBonuses.BonusHealOnPickup += card.BonusHealOnPickup * pair.Value;
        }

        ownedPowerCards.Sort((left, right) => string.CompareOrdinal(left.Card?.Title, right.Card?.Title));

        ApplyBonusesToPlayer();
    }

    private bool IsPowerCardAtGlobalCap(PowerCardData card)
    {
        if (card == null)
        {
            return true;
        }

        if (card.BonusProjectileCount <= 0 && card.BonusVolleyCount <= 0)
        {
            return false;
        }

        int additionalVolleyCount = sessionBonuses.BonusProjectileCount + sessionBonuses.BonusVolleyCount;
        return additionalVolleyCount >= MaxAdditionalVolleyCount;
    }

    private void ApplyBonusesToPlayer()
    {
        if (playerStats == null)
        {
            return;
        }

        playerStats.ApplySessionBonuses(
            sessionBonuses.BonusHp,
            sessionBonuses.BonusAttack,
            sessionBonuses.BonusMoveSpeed,
            sessionBonuses.BonusShootRate);
    }

    private void InitializeAdaptiveDifficulty()
    {
        if (weaponProfile == null || playerStats == null)
        {
            return;
        }

        if (adaptiveDifficulty == null)
        {
            adaptiveDifficulty = new AdaptiveDifficultyMonitor();
        }

        adaptiveDifficulty.Initialize(playerLevel, CurrentFireRate, GetCurrentAttackPower());
    }

    private void UpdateAdaptiveDifficulty(float deltaTime)
    {
        if (adaptiveDifficulty == null || weaponProfile == null || playerStats == null)
        {
            return;
        }

        adaptiveDifficulty.Tick(deltaTime, playerLevel, CurrentFireRate, GetCurrentAttackPower());
    }

    private int GetCurrentAttackPower()
    {
        if (weaponProfile == null || playerStats == null)
        {
            return 1;
        }

        return Mathf.Max(1, weaponProfile.Damage + playerStats.Attack + sessionBonuses.BonusAttack);
    }

    private void ResetPlayerAnimationState()
    {
        if (activePlayer == null)
        {
            return;
        }

        if (playerAnimation == null)
        {
            playerAnimation = CharacterAnimationBridge.GetOrCreate(activePlayer.gameObject);
        }

        playerAnimation?.ResetState();
    }

    private void StartPlayerDeathSequence()
    {
        if (showSettlement || isPlayerDeathSequenceRunning)
        {
            return;
        }

        isPlayerDeathSequenceRunning = true;
        playerAnimation?.SetRunning(false);
        playerAnimation?.SetDying(true);
        StartCoroutine(PlayerDeathSequenceRoutine());
    }

    private IEnumerator PlayerDeathSequenceRoutine()
    {
        yield return new WaitForSecondsRealtime(0.75f);
        BeginSettlement(false);
    }

    private static int ScaleSpawnCount(int baseCount, float multiplier)
    {
        if (baseCount <= 0)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.RoundToInt(baseCount * multiplier));
    }

    private static EnemyProfile ResolveEliteProfile(WaveProfile wave)
    {
        if (wave != null && !string.IsNullOrWhiteSpace(wave.EliteEnemyKey))
        {
            return RoguelikeDataRepository.GetEnemyProfile(wave.EliteEnemyKey);
        }

        return RoguelikeDataRepository.GetDefaultEliteProfile();
    }

    private void HandleEnemyDefeated(EnemyActor enemy)
    {
        if (enemy == null)
        {
            return;
        }

        adaptiveDifficulty?.RecordKill();
        GameVoiceManager.PlayEnemyDeath(enemy.EnemyKey);

        if (enemy.IsElite)
        {
            SpawnPickup(enemy.transform.position, 0f, true);
        }
        else
        {
            EnemyProfile profile = RoguelikeDataRepository.GetEnemyProfile(enemy.EnemyKey);
            SpawnPickup(enemy.transform.position, profile.ExperienceDrop, false);
        }

        ReleaseEnemy(enemy);
    }

    private void SpawnPickup(Vector3 position, float experienceValue, bool grantsFreeLevel)
    {
        ExperiencePickup pickup = pickupPool.Get();
        pickup.Configure(this, position, experienceValue, grantsFreeLevel, grantsFreeLevel ? 4.4f : 3.4f);
        activePickups.Add(pickup);
    }

    private EnemyActor SpawnEnemy(EnemyProfile profile, bool forceElite, Vector3 position)
    {
        CreateEnemyPool(profile);
        EnemyActor enemy = enemyPools[profile.EnemyKey].Get();
        enemy.transform.position = position;

        int waveScaling = Mathf.Max(0, currentWave - 1);
        bool isElite = forceElite || profile.IsElite;
        float waveScaleFactor = 1f + (waveScaling * EnemyWaveHpScalePerWave);
        float attackScaleFactor = 1f + (waveScaling * EnemyWaveAttackScalePerWave);
        float adaptiveHpMultiplier = adaptiveDifficulty != null ? adaptiveDifficulty.CurrentEnemyHpMultiplier : 1f;
        float adaptiveAttackMultiplier = adaptiveDifficulty != null ? adaptiveDifficulty.CurrentEnemyAttackMultiplier : 1f;
        float adaptiveAttackIntervalMultiplier = adaptiveDifficulty != null ? adaptiveDifficulty.CurrentEnemyAttackIntervalMultiplier : 1f;
        float hpMultiplier = (isElite ? 1.75f : 1f) * EnemyBaseHpMultiplier * adaptiveHpMultiplier;
        float attackMultiplier = (isElite ? 1.45f : 1f) * adaptiveAttackMultiplier;
        float moveMultiplier = isElite ? 1.2f : 1f;
        float scaleMultiplier = (isElite ? 1.15f : 1f) * (1f + Mathf.Min(0.3f, waveScaling * EnemyWaveScaleGain));
        float scaledAttackInterval = Mathf.Max(0.45f, (profile.AttackInterval - (waveScaling * EnemyWaveAttackIntervalReduction)) * adaptiveAttackIntervalMultiplier);
        float scaledContactRange = profile.ContactRange + (waveScaling * EnemyWaveContactRangeGain);
        float scaledMoveSpeed = (profile.MoveSpeed * (1f + (waveScaling * 0.03f))) + (waveScaling * EnemyWaveMoveSpeedGain);

        enemy.Configure(
            this,
            profile,
            Mathf.RoundToInt(((profile.MaxHp * waveScaleFactor) + (waveScaling * 14f)) * hpMultiplier),
            Mathf.RoundToInt(((profile.Attack + (waveScaling * 2f)) * attackScaleFactor) * attackMultiplier),
            scaledMoveSpeed * moveMultiplier,
            scaledAttackInterval,
            scaledContactRange,
            profile.Scale * scaleMultiplier,
            isElite);

        activeEnemies.Add(enemy);
        return enemy;
    }

    private Vector3 GetSpawnPositionAroundPlayer()
    {
        Vector3 playerPosition = activePlayer.position;
        float minDistance = Mathf.Max(0.5f, inGameConfig.SpawnMinDistance * EnemySpawnDistanceMultiplier);
        float maxDistance = Mathf.Max(minDistance + 0.5f, inGameConfig.SpawnMaxDistance * EnemySpawnDistanceMultiplier);
        float safeMargin = mapConfig != null ? mapConfig.SafeMargin : 0f;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.1f)
            {
                direction = Vector2.right;
            }

            float distance = Random.Range(minDistance, maxDistance);
            Vector3 position = playerPosition + (Vector3)(direction * distance);
            position = ClampToPlayableMapBounds(position, safeMargin);

            float actualDistance = Vector2.Distance(position, playerPosition);
            if (actualDistance >= minDistance - 0.5f && actualDistance <= maxDistance + 0.5f)
            {
                return position;
            }
        }

        Vector3 fallback = new Vector3(playerPosition.x + minDistance, playerPosition.y, 0f);
        return ClampToPlayableMapBounds(fallback, safeMargin);
    }

    private Vector3 GetInitialPlayerSpawnPosition()
    {
        Vector3 fallback = TryGetPlayableMapBounds(out float minX, out float maxX, out float minY, out float maxY)
            ? new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f)
            : Vector3.zero;
        if (mapGenerator == null)
        {
            mapGenerator = FindAnyObjectByType<MapGenerator>();
        }

        Vector3 spawnPosition = mapGenerator != null ? mapGenerator.FindGrassSpawnPosition(fallback) : fallback;
        return ClampToPlayableMapBounds(spawnPosition, mapConfig != null ? mapConfig.SafeMargin : 0f);
    }

    private Vector3 ClampToPlayableMapBounds(Vector3 position, float padding = 0f)
    {
        if (!TryGetPlayableMapBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            position.z = 0f;
            return position;
        }

        float safePaddingX = Mathf.Min(Mathf.Max(0f, padding), Mathf.Max(0f, (maxX - minX) * 0.5f));
        float safePaddingY = Mathf.Min(Mathf.Max(0f, padding), Mathf.Max(0f, (maxY - minY) * 0.5f));
        position.x = Mathf.Clamp(position.x, minX + safePaddingX, maxX - safePaddingX);
        position.y = Mathf.Clamp(position.y, minY + safePaddingY, maxY - safePaddingY);
        position.z = 0f;
        return position;
    }

    private bool TryGetPlayableMapBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindAnyObjectByType<MapGenerator>();
        }

        if (mapGenerator != null && mapGenerator.TryGetWorldBounds(out Bounds worldBounds))
        {
            minX = worldBounds.min.x;
            maxX = worldBounds.max.x;
            minY = worldBounds.min.y;
            maxY = worldBounds.max.y;
            return true;
        }

        if (mapConfig != null)
        {
            minX = mapConfig.MinX;
            maxX = mapConfig.MaxX;
            minY = mapConfig.MinY;
            maxY = mapConfig.MaxY;
            return true;
        }

        minX = -1f;
        maxX = 1f;
        minY = -1f;
        maxY = 1f;
        return false;
    }

    private void BeginReload()
    {
        reloadRemaining = Mathf.Max(0.2f, weaponProfile.ReloadDuration);
    }

    private void UpdateReload(float deltaTime)
    {
        if (reloadRemaining <= 0f)
        {
            return;
        }

        reloadRemaining = Mathf.Max(0f, reloadRemaining - deltaTime);
        if (reloadRemaining <= 0f)
        {
            currentAmmo = weaponProfile.MaxAmmo;
        }
    }

    private void BeginSettlement(bool manualSettlement)
    {
        if (showSettlement)
        {
            return;
        }

        isPlayerDeathSequenceRunning = false;
        SetPaused(true);
        showPauseMenu = false;
        showUpgradeChoices = false;
        showSettlement = true;
        if (!hasSettledRewards)
        {
            UserProgressRepository.AddMatchRewards(earnedGold, earnedUserExp);
            hasSettledRewards = true;
        }

        SessionSaveRepository.ClearSavedSession();
        SessionSaveRepository.ClearSnapshots();
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    private float GetRequiredExpForNextLevel(int level)
    {
        return inGameConfig.BaseExpToLevel + ((level - 1) * inGameConfig.ExpGrowthPerLevel);
    }

    private void CreateEnemyPool(EnemyProfile profile)
    {
        if (profile == null || enemyPools.ContainsKey(profile.EnemyKey))
        {
            return;
        }

        enemyPools[profile.EnemyKey] = new ComponentPool<EnemyActor>(() => CreateEnemyInstance(profile));
        enemyPools[profile.EnemyKey].Warm(Mathf.Max(8, profile.PoolSize));
    }

    private Bullet CreateBulletInstance()
    {
        GameObject source = bulletTemplate != null ? bulletTemplate.gameObject : new GameObject("bullet");
        GameObject instance = Instantiate(source, poolRoot);
        instance.name = "PooledBullet";
        Bullet bullet = instance.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = instance.AddComponent<Bullet>();
        }

        return bullet;
    }

    private ExperiencePickup CreatePickupInstance()
    {
        GameObject source = bulletTemplate != null ? bulletTemplate.gameObject : new GameObject("pickup");
        GameObject instance = Instantiate(source, poolRoot);
        instance.name = "ExperiencePickup";
        ExperiencePickup pickup = instance.GetComponent<ExperiencePickup>();
        if (pickup == null)
        {
            pickup = instance.AddComponent<ExperiencePickup>();
        }

        return pickup;
    }

    private FloatingDamageText CreateDamageTextInstance()
    {
        GameObject instance = new GameObject("FloatingDamageText");
        instance.transform.SetParent(poolRoot);
        return instance.AddComponent<FloatingDamageText>();
    }

    private EnemyActor CreateEnemyInstance(EnemyProfile profile)
    {
        Transform template = enemyTemplateRoot != null ? enemyTemplateRoot.Find(profile.TemplateName) : null;
        GameObject source = template != null ? template.gameObject : new GameObject(profile.TemplateName);
        GameObject instance = Instantiate(source, poolRoot);
        instance.name = profile.TemplateName + "_Runtime";
        EnemyActor actor = instance.GetComponent<EnemyActor>();
        if (actor == null)
        {
            actor = instance.AddComponent<EnemyActor>();
        }

        return actor;
    }

    private void EnsurePoolRoot()
    {
        if (poolRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("RuntimePools");
        root.transform.SetParent(transform);
        poolRoot = root.transform;
    }

    private void ReleaseBullet(Bullet bullet)
    {
        activeBullets.Remove(bullet);
        bulletPool.Release(bullet);
    }

    private void ReleasePickup(ExperiencePickup pickup)
    {
        activePickups.Remove(pickup);
        pickupPool.Release(pickup);
    }

    private void ReleaseEnemy(EnemyActor enemy)
    {
        activeEnemies.Remove(enemy);
        if (enemyPools.TryGetValue(enemy.EnemyKey, out ComponentPool<EnemyActor> pool))
        {
            pool.Release(enemy);
        }
        else
        {
            enemy.gameObject.SetActive(false);
        }
    }

    private void ReleaseAllBullets()
    {
        pendingBurstShots.Clear();

        for (int index = activeBullets.Count - 1; index >= 0; index--)
        {
            bulletPool.Release(activeBullets[index]);
        }

        activeBullets.Clear();
    }

    private void ReleaseAllPickups()
    {
        for (int index = activePickups.Count - 1; index >= 0; index--)
        {
            pickupPool.Release(activePickups[index]);
        }

        activePickups.Clear();
    }

    private void ReleaseDamageText(FloatingDamageText damageText)
    {
        activeDamageTexts.Remove(damageText);
        damageTextPool.Release(damageText);
    }

    private void ReleaseAllDamageTexts()
    {
        for (int index = activeDamageTexts.Count - 1; index >= 0; index--)
        {
            damageTextPool.Release(activeDamageTexts[index]);
        }

        activeDamageTexts.Clear();
    }

    private void ReleaseAllEnemies()
    {
        for (int index = activeEnemies.Count - 1; index >= 0; index--)
        {
            ReleaseEnemy(activeEnemies[index]);
        }

        activeEnemies.Clear();
    }

    private void UpdatePendingBurstShots(float deltaTime)
    {
        for (int index = pendingBurstShots.Count - 1; index >= 0; index--)
        {
            PendingBurstShot shot = pendingBurstShots[index];
            shot.DelayRemaining -= deltaTime;
            if (shot.DelayRemaining > 0f)
            {
                continue;
            }

            SpawnBullet(
                shot.MuzzlePosition,
                shot.Direction,
                shot.Damage,
                shot.Speed,
                shot.Lifetime,
                shot.Pierce,
                shot.Scale);
            pendingBurstShots.RemoveAt(index);
        }
    }

    private void SpawnBullet(
        Vector3 muzzlePosition,
        Vector2 direction,
        int damage,
        float speed,
        float lifetime,
        int pierce,
        float scale)
    {
        Bullet bullet = bulletPool.Get();
        bullet.Fire(
            this,
            muzzlePosition,
            direction,
            damage,
            speed,
            lifetime,
            pierce,
            scale);
        activeBullets.Add(bullet);
    }

    private static float GetBurstSpeedMultiplier(int burstIndex, int burstCount)
    {
        if (burstCount <= 1)
        {
            return 1f;
        }

        float t = burstIndex / (float)(burstCount - 1);
        return Mathf.Lerp(BurstSpeedLeadMultiplier, BurstSpeedTrailMultiplier, t);
    }

    private static bool SegmentCircleIntersects(Vector2 segmentStart, Vector2 segmentEnd, Vector2 center, float radius, out float hitT)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float segmentLengthSqr = segment.sqrMagnitude;
        if (segmentLengthSqr <= 0.0001f)
        {
            hitT = 0f;
            return (center - segmentStart).sqrMagnitude <= radius * radius;
        }

        hitT = Mathf.Clamp01(Vector2.Dot(center - segmentStart, segment) / segmentLengthSqr);
        Vector2 closestPoint = segmentStart + (segment * hitT);
        return (center - closestPoint).sqrMagnitude <= radius * radius;
    }

    public void SpawnDamageText(Vector3 worldPosition, int amount, bool isPlayerDamage)
    {
        if (damageTextPool == null || amount <= 0)
        {
            return;
        }

        FloatingDamageText damageText = damageTextPool.Get();
        Camera camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        Color color = isPlayerDamage ? new Color(1f, 0.28f, 0.28f, 1f) : new Color(1f, 0.92f, 0.32f, 1f);
        Vector3 velocity = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(1.55f, 2.25f), 0f);
        damageText.Configure(camera, worldPosition, "-" + amount, color, isPlayerDamage ? 1.2f : 1f, velocity, isPlayerDamage ? 0.95f : 0.75f);
        activeDamageTexts.Add(damageText);
    }
}
