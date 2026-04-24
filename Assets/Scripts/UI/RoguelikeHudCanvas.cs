using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RoguelikeHudCanvas : MonoBehaviour
{
    private sealed class MinimapDot
    {
        public RectTransform Rect;
        public Image Image;
    }

    private sealed class EnemyHealthBar
    {
        public RectTransform Root;
        public RectTransform BackgroundRect;
        public RectTransform FillRect;
        public Text ValueText;
        public float DisplayFill;
    }

    private const int MaxCardSlots = 3;
    private const float BarSmoothSpeed = 10f;
    private const float BarSnapThreshold = 0.001f;

    private static readonly Color CardBackground = new Color(0.07f, 0.1f, 0.14f, 0.88f);
    private static readonly Color CardBorder = new Color(0.37f, 0.68f, 0.95f, 1f);
    private static readonly Color Accent = new Color(0.96f, 0.72f, 0.26f, 1f);
    private static readonly Color SoftText = new Color(0.82f, 0.88f, 0.94f, 1f);
    private static readonly Color OverlayTint = new Color(0.02f, 0.03f, 0.05f, 0.84f);
    private static readonly Color HpBarColor = new Color(0.91f, 0.28f, 0.34f, 1f);
    private static readonly Color ExpBarColor = new Color(0.3f, 0.83f, 0.48f, 1f);
    private static readonly Color BarBackground = new Color(0.15f, 0.2f, 0.25f, 1f);
    private static readonly Color MinimapBackground = new Color(0.04f, 0.06f, 0.09f, 0.98f);
    private static readonly Color MinimapGridColor = new Color(0.27f, 0.36f, 0.46f, 0.78f);
    private static readonly Color PlayerDotColor = new Color(0.22f, 0.9f, 0.42f, 1f);
    private static readonly Color EnemyDotColor = new Color(0.94f, 0.28f, 0.28f, 1f);
    private static readonly Color EliteEnemyDotColor = new Color(0.72f, 0.34f, 0.94f, 1f);
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    private readonly List<GameObject> cardRoots = new List<GameObject>(MaxCardSlots);
    private readonly List<Text> cardTitleTexts = new List<Text>(MaxCardSlots);
    private readonly List<Text> cardDescriptionTexts = new List<Text>(MaxCardSlots);
    private readonly List<Text> cardStackTexts = new List<Text>(MaxCardSlots);
    private readonly List<Text> cardSummaryTexts = new List<Text>(MaxCardSlots);
    private readonly Dictionary<int, MinimapDot> enemyMinimapDots = new Dictionary<int, MinimapDot>();
    private readonly Dictionary<int, EnemyHealthBar> enemyHealthBars = new Dictionary<int, EnemyHealthBar>();
    private readonly HashSet<int> activeEnemyIds = new HashSet<int>();
    private readonly List<int> staleEnemyIds = new List<int>();

    private Canvas canvas;
    private Font font;
    private Camera worldCamera;
    private RectTransform rootRect;

    private GameObject pauseOverlay;
    private GameObject upgradeOverlay;
    private GameObject inventoryOverlay;
    private GameObject settlementOverlay;
    private GameObject waterOverlay;
    private GameObject rewindOverlay;

    private Text playerNameText;
    private Text hpValueText;
    private Text attackText;
    private Text moveSpeedText;
    private Text fireRateText;
    private Text pierceText;
    private Text weaponText;
    private RectTransform hpBarBackgroundRect;
    private RectTransform hpFillRect;

    private Text waveValueText;
    private Text waveHintText;
    private Text timerValueText;

    private Text levelText;
    private Text ammoText;
    private Text rewindText;
    private Text expHintText;
    private Text reloadHintText;
    private Text skillNameText;
    private Text skillStateText;
    private Text skillCooldownText;
    private RectTransform expBarBackgroundRect;
    private RectTransform expFillRect;
    private Image skillIconImage;

    private Text settlementWaveText;
    private Text settlementGoldText;
    private Text settlementExpText;
    private Text settlementLevelText;
    private Text settlementCoinsText;
    private Text inventoryCardsText;
    private Text waterBodyText;
    private Text rewindCountdownText;
    private Text terrainNameText;
    private Text terrainEffectText;
    private RectTransform minimapArea;
    private Image minimapPlayerDot;
    private RectTransform enemyBarLayer;
    private float displayedPlayerHpRatio;
    private float displayedExpRatio;
    private bool playerHpBarInitialized;
    private bool expBarInitialized;

    public static RoguelikeHudCanvas EnsureExists(GameObject host)
    {
        RoguelikeHudCanvas existing = FindAnyObjectByType<RoguelikeHudCanvas>();
        if (existing != null)
        {
            return existing;
        }

        GameObject canvasObject = new GameObject("RoguelikeHudCanvas");
        canvasObject.transform.SetParent(null, false);
        canvasObject.transform.localScale = Vector3.one;
        canvasObject.transform.position = Vector3.zero;
        canvasObject.AddComponent<Canvas>();
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvasObject.AddComponent<RoguelikeHudCanvas>();
    }

    private void Awake()
    {
        BuildIfNeeded();
    }

    private void Update()
    {
        BuildIfNeeded();
        RefreshView();
    }

    private void BuildIfNeeded()
    {
        if (canvas != null)
        {
            return;
        }

        font = CjkFontHelper.GetFont();
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1200;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();

        rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = gameObject.AddComponent<RectTransform>();
        }

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        BuildHudPanels(rootRect);
        BuildPauseOverlay(rootRect);
        BuildUpgradeOverlay(rootRect);
        BuildInventoryOverlay(rootRect);
        BuildSettlementOverlay(rootRect);
        BuildWaterOverlay(rootRect);
        BuildRewindOverlay(rootRect);
    }

    private void RefreshView()
    {
        RoguelikeGameManager session = RoguelikeGameManager.Instance;
        if (session == null)
        {
            canvas.enabled = false;
            return;
        }

        canvas.enabled = true;
        PlayerRuntimeStats stats = session.PlayerStats;

        string playerName = stats != null ? stats.DisplayName : GameSelectionConfig.GetPlayerDisplayName(GameSelectionConfig.CurrentPlayerType);
        int currentHp = stats != null ? stats.CurrentHp : 0;
        int maxHp = stats != null ? stats.MaxHp : 1;
        int attack = stats != null ? stats.Attack : 0;
        float moveSpeed = stats != null ? stats.MoveSpeed : 0f;

        playerNameText.text = playerName;
        hpValueText.text = $"{currentHp} / {maxHp}";
        attackText.text = $"\u653b\u51fb {attack}";
        moveSpeedText.text = $"\u79fb\u901f {moveSpeed:0.0}";
        fireRateText.text = $"\u5c04\u901f {session.CurrentFireRate:0.0}/\u79d2";
        pierceText.text = $"\u989d\u5916\u7a7f\u900f {session.CurrentExtraPierce}";
        weaponText.text = $"\u6b66\u5668 {GameSelectionConfig.GetWeaponDisplayName(GameSelectionConfig.CurrentWeaponType)}";
        UpdateSmoothFill(hpBarBackgroundRect, hpFillRect, stats != null ? stats.HealthRatio : 0f, ref displayedPlayerHpRatio, ref playerHpBarInitialized);

        waveValueText.text = $"\u7b2c {session.CurrentWave} \u6ce2";
        waveHintText.text = $"\u4e0b\u4e00\u6ce2\u5012\u8ba1\u65f6 {session.NextWaveIn:0} \u79d2";
        timerValueText.text = FormatTime(session.ElapsedTime);

        levelText.text = $"\u7b49\u7ea7 {session.PlayerLevel}";
        ammoText.text = $"\u5f39\u836f {session.CurrentAmmo}/{session.MaxAmmo}";
        rewindText.text = $"\u56de\u6eaf {session.RewindUsesRemaining}";
        UpdateSmoothFill(expBarBackgroundRect, expFillRect, session.ExpRatio, ref displayedExpRatio, ref expBarInitialized);
        expHintText.text = $"\u7ecf\u9a8c\u503c {session.CurrentExp:0}/{session.ExpToNextLevel:0}    \u6309 R \u952e\u56de\u6eaf\u5230\u6700\u8fd1\u5feb\u7167";
        reloadHintText.text = session.IsReloading ? $"\u6362\u5f39\u4e2d {session.ReloadRemaining:0.0} \u79d2" : string.Empty;
        RefreshSkillInfo(session);

        SetVisible(pauseOverlay, session.ShowPauseMenu && !session.ShowRewindCountdown);
        SetVisible(upgradeOverlay, session.ShowUpgradeChoices && !session.ShowRewindCountdown);
        SetVisible(inventoryOverlay, !session.ShowPauseMenu && !session.ShowUpgradeChoices && !session.ShowSettlement && !session.ShowWaterEffectPrompt && !session.ShowRewindCountdown && Input.GetKey(KeyCode.Tab));
        SetVisible(settlementOverlay, session.ShowSettlement && !session.ShowRewindCountdown);
        SetVisible(waterOverlay, session.ShowWaterEffectPrompt && !session.ShowRewindCountdown);
        SetVisible(rewindOverlay, session.ShowRewindCountdown);
        if (session.ShowRewindCountdown && rewindCountdownText != null)
        {
            rewindCountdownText.text = session.RewindCountdownValue > 0 ? session.RewindCountdownValue.ToString() : string.Empty;
        }

        RefreshUpgradeCards(session);
        RefreshInventory(session);
        RefreshSettlement(session);
        RefreshWaterOverlay();
        RefreshCombatOverlay(session);
    }

    private void RefreshUpgradeCards(RoguelikeGameManager session)
    {
        IReadOnlyList<PowerCardData> cards = session.CurrentCardChoices;
        for (int index = 0; index < cardRoots.Count; index++)
        {
            bool isActive = index < cards.Count;
            cardRoots[index].SetActive(isActive);
            if (!isActive)
            {
                continue;
            }

            PowerCardData card = cards[index];
            cardTitleTexts[index].text = card.Title;
            cardDescriptionTexts[index].text = card.Description;
            cardStackTexts[index].text = $"\u5f53\u524d\u5c42\u6570\uff1a{session.GetCardStack(card.CardKey)} / {card.MaxStacks}";
            cardSummaryTexts[index].text = BuildCardSummary(card);
        }
    }

    private void RefreshSettlement(RoguelikeGameManager session)
    {
        settlementWaveText.text = $"\u5230\u8fbe\u6ce2\u6b21\uff1a\u7b2c {session.CurrentWave} \u6ce2";
        settlementGoldText.text = $"\u83b7\u5f97\u91d1\u5e01\uff1a{session.EarnedGold}";
        settlementExpText.text = $"\u83b7\u5f97\u7528\u6237\u7ecf\u9a8c\uff1a{session.EarnedUserExp}";
        settlementLevelText.text = $"\u5f53\u524d\u5c40\u5916\u7b49\u7ea7\uff1a{UserProgressRepository.GetProgress().Level}";
        settlementCoinsText.text = $"\u5f53\u524d\u91d1\u5e01\uff1a{UserProgressRepository.GetProgress().Coins}";
    }

    private void RefreshInventory(RoguelikeGameManager session)
    {
        if (inventoryCardsText == null)
        {
            return;
        }

        IReadOnlyList<RoguelikeGameManager.OwnedPowerCardInfo> ownedCards = session.OwnedPowerCards;
        if (ownedCards == null || ownedCards.Count == 0)
        {
            inventoryCardsText.text = "\u5f53\u524d\u8fd8\u6ca1\u6709\u83b7\u5f97\u4efb\u4f55\u5f3a\u5316\u3002";
            return;
        }

        Dictionary<string, int> intBonuses = new Dictionary<string, int>();
        Dictionary<string, float> floatBonuses = new Dictionary<string, float>();
        int totalStacks = 0;

        for (int index = 0; index < ownedCards.Count; index++)
        {
            RoguelikeGameManager.OwnedPowerCardInfo info = ownedCards[index];
            if (info?.Card == null)
            {
                continue;
            }

            int stackCount = Mathf.Max(0, info.StackCount);
            totalStacks += stackCount;
            AddInventoryBonus(intBonuses, "生命值", info.Card.BonusHp * stackCount);
            AddInventoryBonus(intBonuses, "攻击力", info.Card.BonusAttack * stackCount);
            AddInventoryBonus(floatBonuses, "移动速度", info.Card.BonusMoveSpeed * stackCount);
            AddInventoryBonus(floatBonuses, "射速", info.Card.BonusShootRate * stackCount);
            AddInventoryBonus(floatBonuses, "子弹速度", info.Card.BonusBulletSpeed * stackCount);
            AddInventoryBonus(intBonuses, "额外子弹", info.Card.BonusProjectileCount * stackCount);
            AddInventoryBonus(intBonuses, "子弹齐射", info.Card.BonusVolleyCount * stackCount);
            AddInventoryBonus(intBonuses, "子弹连射", info.Card.BonusBurstCount * stackCount);
            AddInventoryBonus(intBonuses, "穿透", info.Card.BonusPierce * stackCount);
            AddInventoryBonus(floatBonuses, "拾取范围", info.Card.BonusPickupRadius * stackCount);
            AddInventoryBonus(intBonuses, "拾取回血", info.Card.BonusHealOnPickup * stackCount);
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("已获得强化种类 ").Append(ownedCards.Count).Append("  |  总层数 ").Append(totalStacks);
        AppendInventoryLines(builder, intBonuses);
        AppendInventoryLines(builder, floatBonuses);
        inventoryCardsText.text = builder.ToString();
    }

    private void RefreshWaterOverlay()
    {
        if (waterBodyText == null)
        {
            return;
        }

        waterBodyText.text = "\u4f60\u5df2\u7b2c\u4e00\u6b21\u8e29\u5165\u6c34\u57df\u3002\n\n\u6c34\u9762\u6548\u679c\uff1a\n1. \u89d2\u8272\u4f1a\u6bcf\u79d2\u53d7\u5230 1 \u70b9\u4f24\u5bb3\u3002\n2. \u602a\u7269\u8e29\u5728\u6c34\u91cc\u4e5f\u4f1a\u6bcf\u79d2\u6389 1 \u70b9\u751f\u547d\u3002\n3. \u89d2\u8272\u548c\u602a\u7269\u7684\u79fb\u52a8\u901f\u5ea6\u90fd\u4f1a\u964d\u4f4e 2 \u70b9\u3002\n\n\u6309\u4efb\u610f\u952e\u5173\u95ed\u63d0\u793a\uff0c\u5e76\u7ee7\u7eed\u5f53\u524d\u5bf9\u5c40\u3002";
    }

    private void RefreshTerrainInfo(RoguelikeGameManager session)
    {
        if (terrainNameText == null || terrainEffectText == null)
        {
            return;
        }

        switch (session.CurrentPlayerTerrainType)
        {
            case TerrainSurfaceType.Grass:
                terrainNameText.text = "\u5f53\u524d\u5730\u5f62\uff1a\u8349\u5730";
                terrainEffectText.text = "\u589e\u76ca\uff1a\u79fb\u901f +2\uff0c\u7a7f\u8349\u5730\u53ef\u4ee5\u66f4\u5feb\u62c9\u5f00\u8ddd\u79bb";
                break;
            case TerrainSurfaceType.Water:
                terrainNameText.text = "\u5f53\u524d\u5730\u5f62\uff1a\u6c34\u57df";
                terrainEffectText.text = "\u6548\u679c\uff1a\u79fb\u901f -2\uff0c\u6bcf\u79d2\u6389 1 \u8840\uff0c\u602a\u5728\u6c34\u91cc\u4e5f\u4f1a\u6389\u8840";
                break;
            default:
                terrainNameText.text = "\u5f53\u524d\u5730\u5f62\uff1a\u5e73\u5730";
                terrainEffectText.text = "\u6548\u679c\uff1a\u65e0\u989d\u5916\u5730\u5f62\u589e\u76ca\u6216\u60e9\u7f5a";
                break;
        }
    }

    private void RefreshCombatOverlay(RoguelikeGameManager session)
    {
        if (minimapArea == null || enemyBarLayer == null)
        {
            return;
        }

        RefreshWorldCamera();
        RefreshTerrainInfo(session);
        UpdateMinimapPlayerDot(session);

        activeEnemyIds.Clear();
        IReadOnlyList<EnemyActor> enemies = session.ActiveEnemies;
        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyActor enemy = enemies[index];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            int enemyId = enemy.GetInstanceID();
            activeEnemyIds.Add(enemyId);
            UpdateMinimapEnemyDot(session, enemy, enemyId);
            UpdateEnemyHealthBar(enemy, enemyId);
        }

        CleanupStaleEnemyUi();
    }

    private void RefreshWorldCamera()
    {
        if (worldCamera != null && worldCamera.isActiveAndEnabled)
        {
            return;
        }

        worldCamera = Camera.main;
        if (worldCamera == null)
        {
            worldCamera = FindAnyObjectByType<Camera>();
        }
    }

    private void UpdateMinimapPlayerDot(RoguelikeGameManager session)
    {
        if (minimapPlayerDot == null)
        {
            return;
        }

        minimapPlayerDot.rectTransform.anchoredPosition = WorldToMinimapPosition(session, session.PlayerPosition);
    }

    private void UpdateMinimapEnemyDot(RoguelikeGameManager session, EnemyActor enemy, int enemyId)
    {
        MinimapDot dot = GetOrCreateEnemyDot(enemyId);
        dot.Image.color = enemy.IsElite ? EliteEnemyDotColor : EnemyDotColor;
        dot.Rect.anchoredPosition = WorldToMinimapPosition(session, enemy.Position);
        if (!dot.Rect.gameObject.activeSelf)
        {
            dot.Rect.gameObject.SetActive(true);
        }
    }

    private void UpdateEnemyHealthBar(EnemyActor enemy, int enemyId)
    {
        EnemyHealthBar bar = GetOrCreateEnemyHealthBar(enemyId, enemy.IsElite);
        bar.BackgroundRect.sizeDelta = new Vector2(enemy.IsElite ? 120f : 96f, 16f);
        UpdateSmoothFill(bar.BackgroundRect, bar.FillRect, enemy.HealthRatio, ref bar.DisplayFill);
        bar.ValueText.text = $"{enemy.CurrentHp}/{enemy.MaxHp}";

        bool isVisible = TryGetScreenPosition(enemy.Position + (Vector2.up * enemy.UiHeadOffset), out Vector2 localPoint);
        if (!bar.Root.gameObject.activeSelf && isVisible)
        {
            bar.Root.gameObject.SetActive(true);
        }

        if (!isVisible)
        {
            if (bar.Root.gameObject.activeSelf)
            {
                bar.Root.gameObject.SetActive(false);
            }

            return;
        }

        bar.Root.anchoredPosition = localPoint;
    }

    private bool TryGetScreenPosition(Vector2 worldPosition, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (rootRect == null || worldCamera == null)
        {
            return false;
        }

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f
            || screenPosition.x < -64f
            || screenPosition.x > Screen.width + 64f
            || screenPosition.y < -64f
            || screenPosition.y > Screen.height + 64f)
        {
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screenPosition, null, out localPoint);
    }

    private Vector2 WorldToMinimapPosition(RoguelikeGameManager session, Vector2 worldPosition)
    {
        if (minimapArea == null)
        {
            return Vector2.zero;
        }

        float minX = session.MapMinX;
        float maxX = session.MapMaxX;
        float minY = session.MapMinY;
        float maxY = session.MapMaxY;

        float normalizedX;
        float normalizedY;
        if (maxX - minX <= 0.01f || maxY - minY <= 0.01f)
        {
            Vector2 playerPosition = session.PlayerPosition;
            normalizedX = 0.5f + ((worldPosition.x - playerPosition.x) * 0.03f);
            normalizedY = 0.5f + ((worldPosition.y - playerPosition.y) * 0.03f);
        }
        else
        {
            normalizedX = Mathf.InverseLerp(minX, maxX, worldPosition.x);
            normalizedY = Mathf.InverseLerp(minY, maxY, worldPosition.y);
        }

        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        Rect rect = minimapArea.rect;
        float padding = 8f;
        float localX = Mathf.Lerp(-rect.width * 0.5f + padding, rect.width * 0.5f - padding, normalizedX);
        float localY = Mathf.Lerp(-rect.height * 0.5f + padding, rect.height * 0.5f - padding, normalizedY);
        return new Vector2(localX, localY);
    }

    private MinimapDot GetOrCreateEnemyDot(int enemyId)
    {
        if (enemyMinimapDots.TryGetValue(enemyId, out MinimapDot dot))
        {
            return dot;
        }

        Image dotImage = CreateDot(minimapArea, "EnemyDot_" + enemyId, 7f, EnemyDotColor);
        dot = new MinimapDot
        {
            Rect = dotImage.rectTransform,
            Image = dotImage
        };
        enemyMinimapDots[enemyId] = dot;
        return dot;
    }

    private EnemyHealthBar GetOrCreateEnemyHealthBar(int enemyId, bool isElite)
    {
        if (enemyHealthBars.TryGetValue(enemyId, out EnemyHealthBar bar))
        {
            return bar;
        }

        GameObject rootObject = new GameObject("EnemyHealthBar_" + enemyId, typeof(RectTransform));
        rootObject.transform.SetParent(enemyBarLayer, false);
        RectTransform barRoot = rootObject.GetComponent<RectTransform>();
        barRoot.anchorMin = new Vector2(0.5f, 0.5f);
        barRoot.anchorMax = new Vector2(0.5f, 0.5f);
        barRoot.pivot = new Vector2(0.5f, 0.5f);
        barRoot.sizeDelta = new Vector2(132f, 22f);

        RectTransform backgroundRect = CreateColoredRect(barRoot, "BarBackground", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(isElite ? 120f : 96f, 16f), Vector2.zero, BarBackground);
        AddOutline(backgroundRect.gameObject, isElite ? EliteEnemyDotColor : CardBorder, new Vector2(1f, -1f));

        RectTransform fillRect = CreateSlidingBarFill(backgroundRect, "BarFill", HpBarColor, backgroundRect.sizeDelta.x);

        Text valueText = CreateText(barRoot, "HpValue", 12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132f, 22f));

        bar = new EnemyHealthBar
        {
            Root = barRoot,
            BackgroundRect = backgroundRect,
            FillRect = fillRect,
            ValueText = valueText,
            DisplayFill = 1f
        };
        enemyHealthBars[enemyId] = bar;
        return bar;
    }

    private void CleanupStaleEnemyUi()
    {
        staleEnemyIds.Clear();

        foreach (KeyValuePair<int, MinimapDot> pair in enemyMinimapDots)
        {
            if (!activeEnemyIds.Contains(pair.Key))
            {
                staleEnemyIds.Add(pair.Key);
            }
        }

        foreach (KeyValuePair<int, EnemyHealthBar> pair in enemyHealthBars)
        {
            if (!activeEnemyIds.Contains(pair.Key) && !staleEnemyIds.Contains(pair.Key))
            {
                staleEnemyIds.Add(pair.Key);
            }
        }

        for (int index = 0; index < staleEnemyIds.Count; index++)
        {
            ReleaseEnemyUi(staleEnemyIds[index]);
        }
    }

    private void ReleaseEnemyUi(int enemyId)
    {
        if (enemyMinimapDots.TryGetValue(enemyId, out MinimapDot dot))
        {
            if (dot.Rect != null)
            {
                Destroy(dot.Rect.gameObject);
            }

            enemyMinimapDots.Remove(enemyId);
        }

        if (enemyHealthBars.TryGetValue(enemyId, out EnemyHealthBar bar))
        {
            if (bar.Root != null)
            {
                Destroy(bar.Root.gameObject);
            }

            enemyHealthBars.Remove(enemyId);
        }
    }

    private void BuildHudPanels(RectTransform root)
    {
        RectTransform playerPanel = CreatePanel(root, "PlayerPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, 194f), new Vector2(18f, -18f));
        playerNameText = CreateText(playerPanel, "PlayerName", 22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(18f, -14f), new Vector2(324f, 28f));
        CreateBar(playerPanel, new Vector2(18f, -52f), new Vector2(324f, 18f), HpBarColor, out hpBarBackgroundRect, out hpFillRect);
        CreateText(playerPanel, "HpLabel", 14, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(20f, -76f), new Vector2(40f, 22f)).text = "\u751f\u547d";
        hpValueText = CreateText(playerPanel, "HpValue", 17, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(66f, -74f), new Vector2(120f, 24f));
        attackText = CreateText(playerPanel, "AttackText", 15, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(20f, -108f), new Vector2(120f, 22f));
        moveSpeedText = CreateText(playerPanel, "MoveText", 15, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(168f, -108f), new Vector2(120f, 22f));
        fireRateText = CreateText(playerPanel, "FireText", 15, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(20f, -134f), new Vector2(160f, 22f));
        pierceText = CreateText(playerPanel, "PierceText", 15, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(168f, -134f), new Vector2(160f, 22f));
        weaponText = CreateText(playerPanel, "WeaponText", 15, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(20f, -160f), new Vector2(300f, 22f));

        RectTransform wavePanel = CreatePanel(root, "WavePanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(250f, 92f), new Vector2(0f, -18f));
        CreateText(wavePanel, "WaveTitle", 16, FontStyle.Bold, TextAnchor.MiddleCenter, Accent, new Vector2(0f, -10f), new Vector2(250f, 18f)).text = "\u5f53\u524d\u6ce2\u6b21";
        waveValueText = CreateText(wavePanel, "WaveValue", 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -32f), new Vector2(250f, 32f));
        waveHintText = CreateText(wavePanel, "WaveHint", 13, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText, new Vector2(0f, -60f), new Vector2(250f, 18f));

        RectTransform timerPanel = CreatePanel(root, "TimerPanel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(168f, 72f), new Vector2(-18f, -18f));
        CreateText(timerPanel, "TimerTitle", 14, FontStyle.Bold, TextAnchor.MiddleCenter, Accent, new Vector2(0f, -12f), new Vector2(168f, 18f)).text = "\u751f\u5b58\u65f6\u95f4";
        timerValueText = CreateText(timerPanel, "TimerValue", 26, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -36f), new Vector2(168f, 28f));

        RectTransform progressPanel = CreatePanel(root, "ProgressPanel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(460f, 106f), new Vector2(18f, 18f));
        levelText = CreateText(progressPanel, "LevelText", 18, FontStyle.Bold, TextAnchor.UpperLeft, Accent, new Vector2(18f, -12f), new Vector2(120f, 18f));
        ammoText = CreateText(progressPanel, "AmmoText", 15, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(160f, -14f), new Vector2(130f, 18f));
        rewindText = CreateText(progressPanel, "RewindText", 15, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(320f, -14f), new Vector2(110f, 18f));
        CreateBar(progressPanel, new Vector2(18f, -44f), new Vector2(424f, 18f), ExpBarColor, out expBarBackgroundRect, out expFillRect);
        expHintText = CreateText(progressPanel, "ExpHint", 13, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(18f, -66f), new Vector2(424f, 18f));
        reloadHintText = CreateText(progressPanel, "ReloadHint", 12, FontStyle.Bold, TextAnchor.UpperLeft, Accent, new Vector2(18f, -84f), new Vector2(424f, 16f));

        RectTransform terrainPanel = CreatePanel(root, "TerrainPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, 94f), new Vector2(18f, -226f));
        CreateText(terrainPanel, "TerrainTitle", 15, FontStyle.Bold, TextAnchor.UpperLeft, Accent, new Vector2(18f, -12f), new Vector2(324f, 18f)).text = "\u5730\u5f62\u589e\u76ca";
        terrainNameText = CreateText(terrainPanel, "TerrainName", 18, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(18f, -38f), new Vector2(324f, 22f));
        terrainEffectText = CreateText(terrainPanel, "TerrainEffect", 13, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(18f, -64f), new Vector2(324f, 24f));
        BuildSkillPanel(root);

        RectTransform minimapPanel = CreatePanel(root, "MinimapPanel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(380f, 304f), new Vector2(-18f, 18f));
        CreateText(minimapPanel, "MinimapTitle", 18, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(18f, -12f), new Vector2(224f, 20f)).text = "\u6218\u573a\u5730\u56fe";
        minimapArea = CreateColoredRect(minimapPanel, "MinimapArea", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(344f, 220f), new Vector2(18f, -44f), MinimapBackground);
        AddOutline(minimapArea.gameObject, CardBorder, new Vector2(1f, -1f));
        CreateColoredRect(minimapArea, "HorizontalAxis", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero, MinimapGridColor);
        CreateColoredRect(minimapArea, "VerticalAxis", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(1f, 0f), Vector2.zero, MinimapGridColor);
        minimapPlayerDot = CreateDot(minimapArea, "PlayerDot", 10f, PlayerDotColor);
        CreateText(minimapPanel, "MinimapLegend", 12, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(18f, -274f), new Vector2(328f, 14f)).text = "\u7eff\u8272 Player   \u7ea2\u8272\u602a\u7269   \u7d2b\u8272\u7cbe\u82f1";

        enemyBarLayer = CreateStretchRect(root, "EnemyBarLayer");
    }

    private void BuildSkillPanel(RectTransform root)
    {
        RectTransform skillPanel = CreatePanel(root, "SkillPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, 166f), new Vector2(396f, -18f));
        CreateText(skillPanel, "SkillTitle", 16, FontStyle.Bold, TextAnchor.MiddleCenter, Accent, new Vector2(0f, -10f), new Vector2(140f, 18f)).text = "\u4e3b\u52a8\u6280\u80fd";

        RectTransform iconFrame = CreateColoredRect(
            skillPanel,
            "SkillIconFrame",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(82f, 82f),
            new Vector2(0f, -34f),
            new Color(0.08f, 0.11f, 0.16f, 1f));
        AddOutline(iconFrame.gameObject, CardBorder, new Vector2(1f, -1f));

        RectTransform iconBackground = CreateColoredRect(
            iconFrame,
            "SkillIconBackground",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(72f, 72f),
            Vector2.zero,
            new Color(0.14f, 0.18f, 0.24f, 1f));

        GameObject iconObject = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(iconBackground, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(64f, 64f);
        iconRect.anchoredPosition = Vector2.zero;
        skillIconImage = iconObject.GetComponent<Image>();
        skillIconImage.preserveAspect = true;
        skillIconImage.raycastTarget = false;

        skillCooldownText = CreateText(skillPanel, "SkillCooldown", 26, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -72f), new Vector2(88f, 32f));
        skillNameText = CreateText(skillPanel, "SkillName", 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -118f), new Vector2(140f, 18f));
        skillStateText = CreateText(skillPanel, "SkillState", 11, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText, new Vector2(10f, -138f), new Vector2(120f, 20f));
    }

    private void RefreshSkillInfo(RoguelikeGameManager session)
    {
        PlayerSkillProfile skillProfile = session.CurrentSkillProfile;
        if (skillProfile == null || skillIconImage == null)
        {
            return;
        }

        Sprite icon = skillProfile.LoadIcon();
        skillIconImage.sprite = icon;
        skillIconImage.color = session.IsSkillReady
            ? (icon != null ? Color.white : new Color(0.85f, 0.9f, 0.96f, 0.32f))
            : new Color(0.62f, 0.68f, 0.74f, 0.92f);
        skillNameText.text = skillProfile.SkillName;

        if (session.IsSkillOnInfiniteCooldown)
        {
            skillCooldownText.text = "\u221e";
            skillStateText.text = "\u88ab\u52a8\u5df2\u542f\u52a8";
            return;
        }

        if (session.IsSkillReady)
        {
            skillCooldownText.text = "Q";
            skillStateText.text = skillProfile.UsesAimDirection ? "Q \u91ca\u653e\uff0c\u9f20\u6807\u63a7\u5236\u65b9\u5411" : "Q \u91ca\u653e";
            return;
        }

        skillCooldownText.text = Mathf.CeilToInt(session.SkillCooldownRemaining).ToString();
        skillStateText.text = "\u51b7\u5374\u4e2d";
    }

    private void BuildPauseOverlay(RectTransform root)
    {
        pauseOverlay = CreateOverlay(root, "PauseOverlay");
        RectTransform dialog = CreatePanel(pauseOverlay.transform as RectTransform, "PauseDialog", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(440f, 390f), Vector2.zero);
        CreateText(dialog, "PauseTitle", 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -28f), new Vector2(360f, 28f)).text = "\u6e38\u620f\u6682\u505c";
        CreateText(dialog, "PauseHint", 13, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText, new Vector2(0f, -64f), new Vector2(360f, 22f)).text = "\u53ef\u4ee5\u5728\u8fd9\u91cc\u4fdd\u5b58\u3001\u7ed3\u7b97\u3001\u8fd4\u56de\u4e3b\u83dc\u5355\u6216\u9000\u51fa\u6e38\u620f\u3002";

        CreateActionButton(dialog, "\u4fdd\u5b58\u5bf9\u5c40", new Vector2(220f, 40f), new Vector2(0f, -116f), () => RoguelikeGameManager.Instance?.CreateManualSave());
        CreateActionButton(dialog, "\u7ed3\u675f\u672c\u5c40", new Vector2(220f, 40f), new Vector2(0f, -168f), () => RoguelikeGameManager.Instance?.FinalizeRun());
        CreateActionButton(dialog, "\u8fd4\u56de\u4e3b\u83dc\u5355", new Vector2(220f, 40f), new Vector2(0f, -220f), () => RoguelikeGameManager.Instance?.ReturnToMainMenu());
        CreateActionButton(dialog, "\u4fdd\u5b58\u5e76\u9000\u51fa", new Vector2(220f, 40f), new Vector2(0f, -272f), () => RoguelikeGameManager.Instance?.QuitGameWithSave());
        CreateActionButton(dialog, "\u7ee7\u7eed\u6e38\u620f", new Vector2(220f, 36f), new Vector2(0f, -324f), () => RoguelikeGameManager.Instance?.TogglePauseMenu());

        pauseOverlay.SetActive(false);
    }

    private void BuildUpgradeOverlay(RectTransform root)
    {
        upgradeOverlay = CreateOverlay(root, "UpgradeOverlay");
        CreateText(
            upgradeOverlay.transform as RectTransform,
            "UpgradeTitle",
            28,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.white,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -84f),
            new Vector2(520f, 36f)).text = "\u5347\u7ea7\u5f3a\u5316  |  \u8bf7\u9009\u62e9\u4e00\u5f20\u5361\u724c";

        float cardWidth = 280f;
        float cardSpacing = 36f;
        float totalWidth = (cardWidth * MaxCardSlots) + (cardSpacing * (MaxCardSlots - 1));
        float startX = -totalWidth * 0.5f + (cardWidth * 0.5f);

        for (int index = 0; index < MaxCardSlots; index++)
        {
            float x = startX + index * (cardWidth + cardSpacing);
            RectTransform card = CreatePanel(upgradeOverlay.transform as RectTransform, "UpgradeCard" + index, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(cardWidth, 320f), new Vector2(x, -20f));
            cardRoots.Add(card.gameObject);
            cardTitleTexts.Add(CreateText(card, "CardTitle" + index, 22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(18f, -24f), new Vector2(244f, 28f)));
            cardDescriptionTexts.Add(CreateText(card, "CardDescription" + index, 14, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(18f, -62f), new Vector2(244f, 56f)));
            cardStackTexts.Add(CreateText(card, "CardStack" + index, 13, FontStyle.Bold, TextAnchor.UpperLeft, Accent, new Vector2(18f, -132f), new Vector2(244f, 20f)));
            cardSummaryTexts.Add(CreateText(card, "CardSummary" + index, 13, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(18f, -162f), new Vector2(244f, 92f)));

            int capturedIndex = index;
            CreateActionButton(card, "\u9009\u62e9\u6b64\u9879", new Vector2(220f, 38f), new Vector2(0f, -272f), () => RoguelikeGameManager.Instance?.ChooseUpgradeCard(capturedIndex));
        }

        upgradeOverlay.SetActive(false);
    }

    private void BuildInventoryOverlay(RectTransform root)
    {
        inventoryOverlay = CreateOverlay(root, "InventoryOverlay");
        RectTransform dialog = CreatePanel(inventoryOverlay.transform as RectTransform, "InventoryDialog", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820f, 620f), Vector2.zero);
        CreateText(dialog, "InventoryTitle", 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -28f), new Vector2(520f, 32f)).text = "\u5f53\u524d\u5bf9\u5c40\u5df2\u83b7\u5f97\u5f3a\u5316";
        CreateText(dialog, "InventoryHint", 14, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText, new Vector2(0f, -64f), new Vector2(620f, 20f)).text = "\u6309\u4f4f Tab \u67e5\u770b\uff0c\u91ca\u653e Tab \u540e\u81ea\u52a8\u5173\u95ed\u3002";
        inventoryCardsText = CreateText(dialog, "InventoryCards", 18, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(28f, -108f), new Vector2(764f, 470f));
        inventoryOverlay.SetActive(false);
    }

    private void BuildSettlementOverlay(RectTransform root)
    {
        settlementOverlay = CreateOverlay(root, "SettlementOverlay");
        RectTransform dialog = CreatePanel(settlementOverlay.transform as RectTransform, "SettlementDialog", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460f, 350f), Vector2.zero);
        CreateText(dialog, "SettlementTitle", 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -28f), new Vector2(360f, 30f)).text = "\u672c\u5c40\u7ed3\u7b97";
        settlementWaveText = CreateText(dialog, "SettlementWave", 16, FontStyle.Bold, TextAnchor.MiddleCenter, Accent, new Vector2(0f, -66f), new Vector2(360f, 22f));
        settlementGoldText = CreateText(dialog, "SettlementGold", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -118f), new Vector2(360f, 24f));
        settlementExpText = CreateText(dialog, "SettlementExp", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -150f), new Vector2(360f, 24f));
        settlementLevelText = CreateText(dialog, "SettlementLevel", 14, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText, new Vector2(0f, -190f), new Vector2(360f, 20f));
        settlementCoinsText = CreateText(dialog, "SettlementCoins", 14, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText, new Vector2(0f, -216f), new Vector2(360f, 20f));
        CreateActionButton(dialog, "\u8fd4\u56de\u4e3b\u83dc\u5355", new Vector2(220f, 40f), new Vector2(0f, -284f), () => RoguelikeGameManager.Instance?.ReturnToMainMenu());

        settlementOverlay.SetActive(false);
    }

    private void BuildWaterOverlay(RectTransform root)
    {
        waterOverlay = CreateOverlay(root, "WaterOverlay");
        RectTransform dialog = CreatePanel(waterOverlay.transform as RectTransform, "WaterDialog", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700f, 420f), Vector2.zero);
        CreateText(dialog, "WaterTitle", 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -28f), new Vector2(520f, 32f)).text = "\u6c34\u57df\u6548\u679c\u63d0\u793a";
        waterBodyText = CreateText(dialog, "WaterBody", 18, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(34f, -82f), new Vector2(632f, 252f));
        CreateText(dialog, "WaterHint", 16, FontStyle.Bold, TextAnchor.MiddleCenter, Accent, new Vector2(0f, -366f), new Vector2(520f, 24f)).text = "\u6309\u4efb\u610f\u952e\u7ee7\u7eed";
        waterOverlay.SetActive(false);
    }

    private void BuildRewindOverlay(RectTransform root)
    {
        rewindOverlay = CreateOverlay(root, "RewindOverlay");
        RectTransform overlayRect = rewindOverlay.transform as RectTransform;
        CreateText(overlayRect, "RewindTitle", 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.78f, 0.78f, 0.78f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(560f, 40f)).text = "\u56de\u6eaf\u51c6\u5907\u4e2d";
        rewindCountdownText = CreateText(overlayRect, "RewindCountdown", 240, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.68f, 0.68f, 0.68f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(420f, 260f));
        AddOutline(rewindCountdownText.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(2f, -2f));
        CreateText(overlayRect, "RewindHint", 18, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.72f, 0.72f, 0.72f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(620f, 24f)).text = "\u5012\u8ba1\u65f6\u7ed3\u675f\u540e\u5c06\u81ea\u52a8\u56de\u5230\u6700\u8fd1\u5feb\u7167";
        rewindOverlay.SetActive(false);
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = panelObject.GetComponent<Image>();
        image.color = CardBackground;

        Outline outline = panelObject.GetComponent<Outline>();
        outline.effectColor = CardBorder;
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(panelObject.transform, false);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(0f, 4f);
        accentRect.anchoredPosition = Vector2.zero;
        accent.GetComponent<Image>().color = Accent;

        return rect;
    }

    private RectTransform CreateStretchRect(Transform parent, string name)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);

        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rectObject.layer = gameObject.layer;
        return rect;
    }

    private RectTransform CreateColoredRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        rectObject.transform.SetParent(parent, false);

        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = rectObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        rectObject.layer = gameObject.layer;
        return rect;
    }

    private Image CreateDot(Transform parent, string name, float size, Color color)
    {
        RectTransform rect = CreateColoredRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size, size), Vector2.zero, color);
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 effectDistance)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = effectDistance;
        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = false;
        }
    }

    private void CreateBar(RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color fillColor, out RectTransform backgroundRect, out RectTransform fillRect)
    {
        GameObject backgroundObject = new GameObject("BarBackground", typeof(RectTransform), typeof(Image), typeof(Outline));
        backgroundObject.transform.SetParent(parent, false);
        backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 1f);
        backgroundRect.anchorMax = new Vector2(0f, 1f);
        backgroundRect.pivot = new Vector2(0f, 1f);
        backgroundRect.sizeDelta = size;
        backgroundRect.anchoredPosition = anchoredPosition;

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = BarBackground;

        Outline outline = backgroundObject.GetComponent<Outline>();
        outline.effectColor = CardBorder;
        outline.effectDistance = new Vector2(1f, -1f);

        fillRect = CreateSlidingBarFill(backgroundRect, "BarFill", fillColor, 0f);
    }

    private RectTransform CreateSlidingBarFill(RectTransform parent, string name, Color fillColor, float width)
    {
        GameObject fillObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(parent, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(Mathf.Max(0f, width), 0f);

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.raycastTarget = false;
        return fillRect;
    }

    private void UpdateSmoothFill(RectTransform backgroundRect, RectTransform fillRect, float targetValue, ref float displayedValue)
    {
        if (backgroundRect == null || fillRect == null)
        {
            return;
        }

        float clampedTarget = Mathf.Clamp01(targetValue);
        displayedValue = SmoothBarValue(displayedValue, clampedTarget);
        fillRect.sizeDelta = new Vector2(Mathf.Max(0f, backgroundRect.sizeDelta.x * displayedValue), 0f);
    }

    private void UpdateSmoothFill(RectTransform backgroundRect, RectTransform fillRect, float targetValue, ref float displayedValue, ref bool initialized)
    {
        if (backgroundRect == null || fillRect == null)
        {
            return;
        }

        float clampedTarget = Mathf.Clamp01(targetValue);
        if (!initialized)
        {
            displayedValue = clampedTarget;
            initialized = true;
        }
        else
        {
            displayedValue = SmoothBarValue(displayedValue, clampedTarget);
        }

        fillRect.sizeDelta = new Vector2(Mathf.Max(0f, backgroundRect.sizeDelta.x * displayedValue), 0f);
    }

    private float SmoothBarValue(float currentValue, float targetValue)
    {
        if (Mathf.Abs(currentValue - targetValue) <= BarSnapThreshold)
        {
            return targetValue;
        }

        float interpolation = 1f - Mathf.Exp(-BarSmoothSpeed * Time.unscaledDeltaTime);
        return Mathf.Lerp(currentValue, targetValue, interpolation);
    }

    private GameObject CreateOverlay(RectTransform root, string name)
    {
        GameObject overlay = new GameObject(name, typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(root, false);

        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay.GetComponent<Image>().color = OverlayTint;
        return overlay;
    }

    private Button CreateActionButton(RectTransform parent, string label, Vector2 size, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.27f, 0.4f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.27f, 0.4f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.38f, 0.55f, 1f);
        colors.pressedColor = new Color(0.12f, 0.2f, 0.3f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        button.colors = colors;
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        CreateText(rect, label + "_Text", 15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size).text = label;
        return button;
    }

    private Text CreateText(RectTransform parent, string name, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        return CreateText(parent, name, fontSize, fontStyle, alignment, color, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size);
    }

    private Text CreateText(RectTransform parent, string name, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.transform.SetParent(null, false);
    }

    private static string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private static void SetVisible(GameObject target, bool visible)
    {
        if (target != null && target.activeSelf != visible)
        {
            target.SetActive(visible);
        }
    }

    private static string BuildCardSummary(PowerCardData card)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        AppendBonus(builder, card.BonusHp, "\u751f\u547d\u503c");
        AppendBonus(builder, card.BonusAttack, "\u653b\u51fb\u529b");
        AppendBonus(builder, card.BonusMoveSpeed, "\u79fb\u52a8\u901f\u5ea6");
        AppendBonus(builder, card.BonusShootRate, "\u5c04\u901f");
        AppendBonus(builder, card.BonusBulletSpeed, "\u5b50\u5f39\u901f\u5ea6");
        AppendBonus(builder, card.BonusProjectileCount, "\u989d\u5916\u5b50\u5f39");
        AppendBonus(builder, card.BonusVolleyCount, "\u5b50\u5f39\u9f50\u5c04");
        AppendBonus(builder, card.BonusBurstCount, "\u5b50\u5f39\u8fde\u5c04");
        AppendBonus(builder, card.BonusPierce, "\u7a7f\u900f");
        AppendBonus(builder, card.BonusPickupRadius, "\u62fe\u53d6\u8303\u56f4");
        AppendBonus(builder, card.BonusHealOnPickup, "\u62fe\u53d6\u56de\u8840");
        return builder.Length == 0 ? "\u5f53\u524d\u6ca1\u6709\u989d\u5916\u6548\u679c\u3002" : builder.ToString();
    }

    private static void AddInventoryBonus(Dictionary<string, int> bucket, string label, int value)
    {
        if (value == 0)
        {
            return;
        }

        bucket[label] = bucket.TryGetValue(label, out int currentValue) ? currentValue + value : value;
    }

    private static void AddInventoryBonus(Dictionary<string, float> bucket, string label, float value)
    {
        if (Mathf.Abs(value) <= 0.001f)
        {
            return;
        }

        bucket[label] = bucket.TryGetValue(label, out float currentValue) ? currentValue + value : value;
    }

    private static void AppendInventoryLines(System.Text.StringBuilder builder, Dictionary<string, int> bonuses)
    {
        foreach (KeyValuePair<string, int> pair in bonuses)
        {
            builder.Append('\n')
                .Append(pair.Key)
                .Append(" +")
                .Append(pair.Value);
        }
    }

    private static void AppendInventoryLines(System.Text.StringBuilder builder, Dictionary<string, float> bonuses)
    {
        foreach (KeyValuePair<string, float> pair in bonuses)
        {
            builder.Append('\n')
                .Append(pair.Key)
                .Append(" +")
                .Append(pair.Value.ToString("0.0"));
        }
    }

    private static void AppendBonus(System.Text.StringBuilder builder, int value, string label)
    {
        if (value == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.Append(label).Append(" +").Append(value);
    }

    private static void AppendBonus(System.Text.StringBuilder builder, float value, string label)
    {
        if (Mathf.Abs(value) <= 0.001f)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.Append(label).Append(" +").Append(value.ToString("0.0"));
    }
}
