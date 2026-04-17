using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RoguelikeHudCanvas : MonoBehaviour
{
    private const int MaxCardSlots = 3;

    private static readonly Color CardBackground = new Color(0.07f, 0.1f, 0.14f, 0.88f);
    private static readonly Color CardBorder = new Color(0.37f, 0.68f, 0.95f, 1f);
    private static readonly Color Accent = new Color(0.96f, 0.72f, 0.26f, 1f);
    private static readonly Color SoftText = new Color(0.82f, 0.88f, 0.94f, 1f);
    private static readonly Color OverlayTint = new Color(0.02f, 0.03f, 0.05f, 0.84f);
    private static readonly Color HpBarColor = new Color(0.91f, 0.28f, 0.34f, 1f);
    private static readonly Color ExpBarColor = new Color(0.3f, 0.83f, 0.48f, 1f);
    private static readonly Color BarBackground = new Color(0.15f, 0.2f, 0.25f, 1f);
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    private readonly List<GameObject> cardRoots = new List<GameObject>(MaxCardSlots);
    private readonly List<Text> cardTitleTexts = new List<Text>(MaxCardSlots);
    private readonly List<Text> cardDescriptionTexts = new List<Text>(MaxCardSlots);
    private readonly List<Text> cardStackTexts = new List<Text>(MaxCardSlots);
    private readonly List<Text> cardSummaryTexts = new List<Text>(MaxCardSlots);

    private Canvas canvas;
    private Font font;

    private GameObject pauseOverlay;
    private GameObject upgradeOverlay;
    private GameObject inventoryOverlay;
    private GameObject settlementOverlay;
    private GameObject waterOverlay;

    private Text playerNameText;
    private Text hpValueText;
    private Text attackText;
    private Text moveSpeedText;
    private Text fireRateText;
    private Text pierceText;
    private Text weaponText;
    private Image hpFillImage;

    private Text waveValueText;
    private Text waveHintText;
    private Text timerValueText;

    private Text levelText;
    private Text ammoText;
    private Text rewindText;
    private Text expHintText;
    private Text reloadHintText;
    private Image expFillImage;

    private Text settlementWaveText;
    private Text settlementGoldText;
    private Text settlementExpText;
    private Text settlementLevelText;
    private Text settlementCoinsText;
    private Text inventoryCardsText;
    private Text waterBodyText;

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

        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
        {
            root = gameObject.AddComponent<RectTransform>();
        }

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        BuildHudPanels(root);
        BuildPauseOverlay(root);
        BuildUpgradeOverlay(root);
        BuildInventoryOverlay(root);
        BuildSettlementOverlay(root);
        BuildWaterOverlay(root);
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
        hpFillImage.fillAmount = stats != null ? stats.HealthRatio : 0f;

        waveValueText.text = $"\u7b2c {session.CurrentWave} \u6ce2";
        waveHintText.text = $"\u4e0b\u4e00\u6ce2\u5012\u8ba1\u65f6 {session.NextWaveIn:0} \u79d2";
        timerValueText.text = FormatTime(session.ElapsedTime);

        levelText.text = $"\u7b49\u7ea7 {session.PlayerLevel}";
        ammoText.text = $"\u5f39\u836f {session.CurrentAmmo}/{session.MaxAmmo}";
        rewindText.text = $"\u56de\u6eaf {session.RewindUsesRemaining}";
        expFillImage.fillAmount = session.ExpRatio;
        expHintText.text = $"\u7ecf\u9a8c\u503c {session.CurrentExp:0}/{session.ExpToNextLevel:0}    \u6309 R \u952e\u56de\u6eaf\u5230\u6700\u8fd1\u5feb\u7167";
        reloadHintText.text = session.IsReloading ? $"\u6362\u5f39\u4e2d {session.ReloadRemaining:0.0} \u79d2" : string.Empty;

        SetVisible(pauseOverlay, session.ShowPauseMenu);
        SetVisible(upgradeOverlay, session.ShowUpgradeChoices);
        SetVisible(inventoryOverlay, !session.ShowPauseMenu && !session.ShowUpgradeChoices && !session.ShowSettlement && !session.ShowWaterEffectPrompt && Input.GetKey(KeyCode.Tab));
        SetVisible(settlementOverlay, session.ShowSettlement);
        SetVisible(waterOverlay, session.ShowWaterEffectPrompt);

        RefreshUpgradeCards(session);
        RefreshInventory(session);
        RefreshSettlement(session);
        RefreshWaterOverlay();
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

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int index = 0; index < ownedCards.Count; index++)
        {
            RoguelikeGameManager.OwnedPowerCardInfo info = ownedCards[index];
            if (info?.Card == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            builder.Append(info.Card.Title)
                .Append("  x")
                .Append(info.StackCount)
                .Append('\n')
                .Append(info.Card.Description)
                .Append('\n')
                .Append(BuildCardSummary(info.Card));
        }

        inventoryCardsText.text = builder.Length > 0 ? builder.ToString() : "\u5f53\u524d\u8fd8\u6ca1\u6709\u83b7\u5f97\u4efb\u4f55\u5f3a\u5316\u3002";
    }

    private void RefreshWaterOverlay()
    {
        if (waterBodyText == null)
        {
            return;
        }

        waterBodyText.text = "\u4f60\u5df2\u7b2c\u4e00\u6b21\u8e29\u5165\u6c34\u57df\u3002\n\n\u6c34\u9762\u6548\u679c\uff1a\n1. \u89d2\u8272\u4f1a\u6bcf\u79d2\u53d7\u5230 1 \u70b9\u4f24\u5bb3\u3002\n2. \u602a\u7269\u8e29\u5728\u6c34\u91cc\u4e5f\u4f1a\u6bcf\u79d2\u6389 1 \u70b9\u751f\u547d\u3002\n3. \u89d2\u8272\u548c\u602a\u7269\u7684\u79fb\u52a8\u901f\u5ea6\u90fd\u4f1a\u964d\u4f4e 2 \u70b9\u3002\n\n\u6309\u4efb\u610f\u952e\u5173\u95ed\u63d0\u793a\uff0c\u5e76\u7ee7\u7eed\u5f53\u524d\u5bf9\u5c40\u3002";
    }

    private void BuildHudPanels(RectTransform root)
    {
        RectTransform playerPanel = CreatePanel(root, "PlayerPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, 194f), new Vector2(18f, -18f));
        playerNameText = CreateText(playerPanel, "PlayerName", 22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, new Vector2(18f, -14f), new Vector2(324f, 28f));
        CreateBar(playerPanel, new Vector2(18f, -52f), new Vector2(324f, 18f), HpBarColor, out hpFillImage);
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
        CreateBar(progressPanel, new Vector2(18f, -44f), new Vector2(424f, 18f), ExpBarColor, out expFillImage);
        expHintText = CreateText(progressPanel, "ExpHint", 13, FontStyle.Normal, TextAnchor.UpperLeft, SoftText, new Vector2(18f, -66f), new Vector2(424f, 18f));
        reloadHintText = CreateText(progressPanel, "ReloadHint", 12, FontStyle.Bold, TextAnchor.UpperLeft, Accent, new Vector2(18f, -84f), new Vector2(424f, 16f));
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

    private void CreateBar(RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color fillColor, out Image fillImage)
    {
        GameObject backgroundObject = new GameObject("BarBackground", typeof(RectTransform), typeof(Image), typeof(Outline));
        backgroundObject.transform.SetParent(parent, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
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

        GameObject fillObject = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(backgroundObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fillImage = fillObject.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 0f;
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
