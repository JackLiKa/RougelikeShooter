using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PanelRoot : IPanel
{
    private const float PreviewAnimationFps = 6f;
    private const float VirtualWidth = 1920f;
    private const float VirtualHeight = 1080f;

    private static readonly Color ScreenTint = new Color(0.05f, 0.08f, 0.12f, 0.9f);
    private static readonly Color CardBackground = new Color(0.09f, 0.13f, 0.18f, 0.94f);
    private static readonly Color CardBorder = new Color(0.37f, 0.68f, 0.95f, 1f);
    private static readonly Color Accent = new Color(0.96f, 0.72f, 0.26f, 1f);
    private static readonly Color SoftText = new Color(0.82f, 0.88f, 0.94f, 1f);
    private static readonly Color DimOverlay = new Color(0f, 0f, 0f, 0.48f);

    private readonly Dictionary<PlayerType, Sprite[]> idlePreviewCache = new Dictionary<PlayerType, Sprite[]>();
    private readonly Dictionary<WeaponType, Texture2D> weaponPreviewCache = new Dictionary<WeaponType, Texture2D>();
    private readonly List<SavedSessionInfo> savedSessions = new List<SavedSessionInfo>();

    private bool showExitConfirm;
    private bool showLoadDialog;
    private Vector2 saveScrollPosition;
    private Matrix4x4 previousGuiMatrix;

    public PanelRoot() : base(null)
    {
    }

    protected override void OnInit()
    {
        gameObject = GameObject.Find("MainMenu");
        rectTransform = gameObject != null ? gameObject.GetComponent<RectTransform>() : null;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (showLoadDialog)
        {
            showLoadDialog = false;
            return;
        }

        showExitConfirm = !showExitConfirm;
    }

    public void DrawGUI()
    {
        BeginVirtualCanvas();

        DrawFilledRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight), ScreenTint);
        DrawTitleCard();
        DrawMenuCard(new Rect(96f, 164f, 432f, 426f));
        DrawPreviewCard(new Rect(96f, 618f, 548f, 372f));
        DrawProgressCard(new Rect(1376f, 164f, 448f, 254f));
        DrawTipsCard(new Rect(1240f, 428f, 584f, 562f));

        if (showLoadDialog || showExitConfirm)
        {
            DrawFilledRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight), DimOverlay);
        }

        if (showLoadDialog)
        {
            DrawLoadDialog();
        }

        if (showExitConfirm)
        {
            DrawExitDialog();
        }

        EndVirtualCanvas();
    }

    private void DrawTitleCard()
    {
        DrawCompatibleLabel(new Rect(96f, 58f, 620f, 48f), "\u8089\u9e3d\u5c04\u51fb\u8bd5\u70bc", CreateLabelStyle(36, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
        DrawCompatibleLabel(new Rect(100f, 110f, 760f, 26f), "\u9009\u62e9\u89d2\u8272\u548c\u6b66\u5668\u540e\u8fdb\u5165\u6218\u573a\uff0c\u5728\u5bf9\u5c40\u91cc\u5373\u65f6\u6210\u957f\uff0c\u5728\u5c40\u5916\u6301\u7eed\u5f3a\u5316\u3002", CreateLabelStyle(17, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));
    }

    private void DrawMenuCard(Rect rect)
    {
        DrawCard(rect, "\u4e3b\u83dc\u5355");

        float buttonX = rect.x + 34f;
        float buttonWidth = rect.width - 68f;
        float buttonHeight = 46f;
        float currentY = rect.y + 60f;

        if (SessionSaveRepository.HasSavedSession() && DrawButton(new Rect(buttonX, currentY, buttonWidth, buttonHeight), "\u7ee7\u7eed\u6e38\u620f"))
        {
            SessionSaveRepository.SelectSaveForLoad(null);
            SceneManager.LoadScene("GameScene");
        }

        currentY += 58f;
        if (DrawButton(new Rect(buttonX, currentY, buttonWidth, buttonHeight), "\u52a0\u8f7d\u5b58\u6863\u6e38\u620f"))
        {
            RefreshSavedSessions();
            showLoadDialog = true;
        }

        currentY += 58f;
        if (DrawButton(new Rect(buttonX, currentY, buttonWidth, buttonHeight), "\u5f00\u59cb\u65b0\u5bf9\u5c40"))
        {
            SessionSaveRepository.SelectSaveForLoad(null);
            SessionSaveRepository.ClearSavedSession();
            SessionSaveRepository.ClearSnapshots();
            SceneManager.LoadScene("GameScene");
        }

        currentY += 58f;
        if (DrawButton(new Rect(buttonX, currentY, buttonWidth, buttonHeight), "\u89d2\u8272\u9762\u677f"))
        {
            SceneManager.LoadScene("CharacterPanelScene");
        }

        currentY += 58f;
        if (DrawButton(new Rect(buttonX, currentY, buttonWidth, buttonHeight), "\u9000\u51fa\u6e38\u620f"))
        {
            showExitConfirm = true;
        }

        Rect footerRect = new Rect(rect.x + 18f, rect.y + rect.height - 64f, rect.width - 36f, 44f);
        DrawFilledRect(footerRect, new Color(0.05f, 0.08f, 0.11f, 0.96f));
        DrawBorder(footerRect, CardBorder, 1.5f);
        DrawCompatibleLabel(new Rect(footerRect.x + 12f, footerRect.y + 6f, footerRect.width - 24f, 16f), $"\u5f53\u524d\u89d2\u8272\uff1a{GameSelectionConfig.GetPlayerDisplayName(GameSelectionConfig.CurrentPlayerType)}", CreateLabelStyle(14, FontStyle.Bold, TextAnchor.UpperLeft, Accent));
        DrawCompatibleLabel(new Rect(footerRect.x + 12f, footerRect.y + 24f, footerRect.width - 24f, 14f), $"\u5f53\u524d\u6b66\u5668\uff1a{GameSelectionConfig.GetWeaponDisplayName(GameSelectionConfig.CurrentWeaponType)}", CreateLabelStyle(13, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));
    }

    private void DrawPreviewCard(Rect rect)
    {
        DrawCard(rect, "\u5f53\u524d\u89d2\u8272\u5f85\u673a\u9884\u89c8");
        Rect previewRect = new Rect(rect.x + 24f, rect.y + 48f, rect.width - 48f, rect.height - 72f);
        DrawFilledRect(previewRect, new Color(0.04f, 0.06f, 0.09f, 1f));
        DrawBorder(previewRect, CardBorder, 2f);

        Sprite[] previewFrames = GetIdlePreviewFrames(GameSelectionConfig.CurrentPlayerType);
        if (previewFrames.Length > 0)
        {
            int frameIndex = Mathf.FloorToInt(Time.realtimeSinceStartup * PreviewAnimationFps) % previewFrames.Length;
            Rect characterRect = new Rect(previewRect.x + 12f, previewRect.y + 12f, previewRect.width - 24f, previewRect.height - 24f);
            DrawSprite(characterRect, previewFrames[frameIndex]);
        }

        DrawWeaponPreview(previewRect);
        Rect captionRect = new Rect(previewRect.x + 14f, previewRect.yMax - 44f, previewRect.width - 28f, 26f);
        DrawFilledRect(captionRect, new Color(0f, 0f, 0f, 0.5f));
        DrawCompatibleLabel(new Rect(captionRect.x + 8f, captionRect.y + 5f, captionRect.width - 16f, 16f), "\u5de6\u4e0b\u9884\u89c8\u5f53\u524d Player \u4e0e\u624b\u6301\u6b66\u5668\u7684 Idle \u52a8\u753b", CreateLabelStyle(12, FontStyle.Normal, TextAnchor.MiddleLeft, SoftText));
    }

    private void DrawProgressCard(Rect rect)
    {
        DrawCard(rect, "\u5c40\u5916\u6210\u957f");

        UserProgressData progress = UserProgressRepository.GetProgress();
        PlayerType currentPlayer = GameSelectionConfig.CurrentPlayerType;

        Rect infoBoxA = new Rect(rect.x + 18f, rect.y + 50f, rect.width - 36f, 72f);
        Rect infoBoxB = new Rect(rect.x + 18f, rect.y + 130f, rect.width - 36f, 72f);
        DrawFilledRect(infoBoxA, new Color(0.05f, 0.08f, 0.11f, 0.96f));
        DrawFilledRect(infoBoxB, new Color(0.05f, 0.08f, 0.11f, 0.96f));
        DrawBorder(infoBoxA, CardBorder, 1.5f);
        DrawBorder(infoBoxB, CardBorder, 1.5f);

        float columnWidth = (infoBoxA.width - 42f) * 0.5f;
        DrawCompatibleLabel(new Rect(infoBoxA.x + 14f, infoBoxA.y + 10f, columnWidth, 16f), "\u7528\u6237\u7b49\u7ea7", CreateLabelStyle(12, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));
        DrawCompatibleLabel(new Rect(infoBoxA.x + 14f, infoBoxA.y + 30f, columnWidth, 24f), progress.Level.ToString(), CreateLabelStyle(22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
        DrawCompatibleLabel(new Rect(infoBoxA.x + 20f + columnWidth, infoBoxA.y + 10f, columnWidth, 16f), "\u5f53\u524d\u91d1\u5e01", CreateLabelStyle(12, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));
        DrawCompatibleLabel(new Rect(infoBoxA.x + 20f + columnWidth, infoBoxA.y + 30f, columnWidth, 24f), progress.Coins.ToString(), CreateLabelStyle(22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));

        DrawCompatibleLabel(new Rect(infoBoxB.x + 14f, infoBoxB.y + 10f, columnWidth, 16f), "\u89d2\u8272\u5f3a\u5316", CreateLabelStyle(12, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));
        DrawCompatibleLabel(new Rect(infoBoxB.x + 14f, infoBoxB.y + 30f, columnWidth, 22f), $"{UserProgressRepository.GetUpgradeLevel(currentPlayer)}/{UserProgressRepository.GetPlayerUpgradeCap()}", CreateLabelStyle(20, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
        DrawCompatibleLabel(new Rect(infoBoxB.x + 20f + columnWidth, infoBoxB.y + 10f, columnWidth, 16f), "\u4e0b\u6b21\u82b1\u8d39", CreateLabelStyle(12, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));
        DrawCompatibleLabel(new Rect(infoBoxB.x + 20f + columnWidth, infoBoxB.y + 30f, columnWidth, 22f), UserProgressRepository.IsPlayerUpgradeAtCap(currentPlayer) ? "\u5df2\u6ee1\u7ea7" : UserProgressRepository.GetNextUpgradeCost(currentPlayer).ToString(), CreateLabelStyle(20, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));

        int requiredExp = UserProgressRepository.GetRequiredExpForLevel(progress.Level);
        Rect expBox = new Rect(rect.x + 18f, rect.y + 210f, rect.width - 36f, 24f);
        DrawFilledRect(expBox, new Color(0.05f, 0.08f, 0.11f, 0.96f));
        DrawBorder(expBox, Accent, 1.5f);
        DrawCompatibleLabel(new Rect(expBox.x + 12f, expBox.y + 4f, expBox.width - 24f, 16f), $"\u7528\u6237\u7ecf\u9a8c  {progress.CurrentExp}/{requiredExp}", CreateLabelStyle(12, FontStyle.Bold, TextAnchor.MiddleLeft, Accent));
    }

    private void DrawTipsCard(Rect rect)
    {
        DrawCard(rect, "\u6a21\u5f0f\u8bf4\u660e");
        GUI.Label(
            new Rect(rect.x + 24f, rect.y + 54f, rect.width - 48f, rect.height - 76f),
            "\u6bcf\u9694\u4e00\u6bb5\u65f6\u95f4\u4f1a\u5237\u65b0\u65b0\u6ce2\u6b21\u602a\u7269\uff0c\u6bcf 5 \u6ce2\u4f1a\u989d\u5916\u51fa\u73b0\u7cbe\u82f1\u602a\u3002\n\n\u51fb\u6740\u602a\u7269\u4f1a\u6389\u843d\u7ecf\u9a8c\uff0c\u63a5\u8fd1\u73a9\u5bb6\u65f6\u4f1a\u81ea\u52a8\u5438\u9644\uff0c\u7cbe\u82f1\u7ecf\u9a8c\u53ef\u4ee5\u76f4\u63a5\u5347 1 \u7ea7\u3002\n\n\u6bcf\u6b21\u5347\u7ea7\u90fd\u4f1a\u6682\u505c\u6e38\u620f\uff0c\u4ece 2 \u5f20 Roguelike \u5361\u724c\u91cc\u9009 1 \u5f20\u5f3a\u5316\u5f53\u524d\u5bf9\u5c40\u3002\n\n\u6309 Esc \u53ef\u6253\u5f00\u5bf9\u5c40\u83dc\u5355\uff0c\u53ef\u4fdd\u5b58\u5bf9\u5c40\u3001\u7ed3\u7b97\u672c\u5c40\u3001\u8fd4\u56de\u4e3b\u83dc\u5355\u6216\u4fdd\u5b58\u5e76\u9000\u51fa\u3002\n\n\u6309 R \u53ef\u4f7f\u7528\u6700\u8fd1\u5feb\u7167\u56de\u6eaf\uff0c\u5355\u5c40\u6700\u591a 3 \u6b21\u3002",
            CreateLabelStyle(18, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));
    }

    private void DrawLoadDialog()
    {
        Rect dialogRect = new Rect(VirtualWidth * 0.5f - 420f, VirtualHeight * 0.5f - 250f, 840f, 500f);
        Rect listRect = new Rect(dialogRect.x + 26f, dialogRect.y + 78f, dialogRect.width - 52f, 314f);
        Rect viewRect = new Rect(0f, 0f, listRect.width - 20f, Mathf.Max(listRect.height - 4f, savedSessions.Count * 78f));

        DrawCard(dialogRect, "\u52a0\u8f7d\u5b58\u6863\u6e38\u620f");
        DrawCompatibleLabel(new Rect(dialogRect.x + 26f, dialogRect.y + 44f, dialogRect.width - 52f, 20f), "\u9009\u62e9\u4e00\u4e2a\u5b58\u6863\u540e\u91cd\u65b0\u8fdb\u5165\u5f53\u524d\u5bf9\u5c40\u3002", CreateLabelStyle(14, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));

        DrawFilledRect(listRect, new Color(0.05f, 0.07f, 0.1f, 0.96f));
        DrawBorder(listRect, CardBorder, 2f);

        if (savedSessions.Count == 0)
        {
            DrawCompatibleLabel(new Rect(listRect.x, listRect.y + 134f, listRect.width, 24f), "\u5f53\u524d\u6682\u65e0\u53ef\u7528\u5b58\u6863\u3002", CreateLabelStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        }
        else
        {
            saveScrollPosition = GUI.BeginScrollView(listRect, saveScrollPosition, viewRect, false, true);
            for (int index = 0; index < savedSessions.Count; index++)
            {
                SavedSessionInfo info = savedSessions[index];
                Rect itemRect = new Rect(8f, 8f + index * 78f, viewRect.width - 16f, 66f);
                DrawFilledRect(itemRect, new Color(0.1f, 0.15f, 0.2f, 1f));
                DrawBorder(itemRect, info.IsContinueSave ? Accent : CardBorder, 2f);
                DrawCompatibleLabel(new Rect(itemRect.x + 16f, itemRect.y + 10f, itemRect.width - 178f, 20f), info.DisplayName, CreateLabelStyle(15, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
                DrawCompatibleLabel(new Rect(itemRect.x + 16f, itemRect.y + 34f, itemRect.width - 178f, 18f), $"\u7b49\u7ea7 {info.Snapshot.PlayerLevel}  |  \u751f\u5b58 {FormatTime(info.Snapshot.ElapsedTime)}  |  \u7ecf\u9a8c {info.Snapshot.CurrentExp:0}/{Mathf.Max(1f, info.Snapshot.ExpToNextLevel):0}", CreateLabelStyle(13, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));

                if (DrawButton(new Rect(itemRect.x + itemRect.width - 142f, itemRect.y + 13f, 126f, 40f), "\u8f7d\u5165\u5b58\u6863"))
                {
                    LoadSavedSession(info);
                }
            }

            GUI.EndScrollView();
        }

        if (DrawButton(new Rect(dialogRect.x + dialogRect.width - 168f, dialogRect.y + dialogRect.height - 56f, 140f, 38f), "\u5173\u95ed"))
        {
            showLoadDialog = false;
        }
    }

    private void LoadSavedSession(SavedSessionInfo info)
    {
        if (info == null)
        {
            return;
        }

        GameSelectionConfig.CurrentPlayerType = info.Snapshot.PlayerType;
        GameSelectionConfig.CurrentWeaponType = info.Snapshot.WeaponType;
        SessionSaveRepository.SelectSaveForLoad(info.FilePath);
        SceneManager.LoadScene("GameScene");
    }

    private void RefreshSavedSessions()
    {
        savedSessions.Clear();
        savedSessions.AddRange(SessionSaveRepository.GetSavedSessions(8));
        saveScrollPosition = Vector2.zero;
    }

    private Sprite[] GetIdlePreviewFrames(PlayerType playerType)
    {
        if (idlePreviewCache.TryGetValue(playerType, out Sprite[] cachedSprites))
        {
            return cachedSprites;
        }

        string previewFolder = GameSelectionConfig.GetPlayerPreviewResourceFolder(playerType);
        string[] candidatePaths =
        {
            previewFolder + "/Idle-Sheet",
            previewFolder + "/Idle_Sheet"
        };

        for (int index = 0; index < candidatePaths.Length; index++)
        {
            Sprite[] loadedSprites = Resources.LoadAll<Sprite>(candidatePaths[index]);
            if (loadedSprites != null && loadedSprites.Length > 0)
            {
                idlePreviewCache[playerType] = loadedSprites;
                return loadedSprites;
            }
        }

        idlePreviewCache[playerType] = Array.Empty<Sprite>();
        return idlePreviewCache[playerType];
    }

    private void DrawWeaponPreview(Rect previewRect)
    {
        Texture2D weaponTexture = GetWeaponPreviewTexture(GameSelectionConfig.CurrentWeaponType);
        if (weaponTexture == null)
        {
            return;
        }

        float angle = Mathf.Sin(Time.realtimeSinceStartup * 2f) * 12f - 18f;
        Vector2 pivot = new Vector2(previewRect.x + previewRect.width * 0.68f, previewRect.y + previewRect.height * 0.58f);
        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, pivot);

        Rect weaponRect = new Rect(
            previewRect.x + previewRect.width * 0.45f,
            previewRect.y + previewRect.height * 0.45f,
            previewRect.width * 0.34f,
            previewRect.height * 0.15f);
        GUI.DrawTexture(weaponRect, weaponTexture, ScaleMode.ScaleToFit, true);
        GUI.matrix = previousMatrix;
    }

    private Texture2D GetWeaponPreviewTexture(WeaponType weaponType)
    {
        if (weaponPreviewCache.TryGetValue(weaponType, out Texture2D cachedTexture))
        {
            return cachedTexture;
        }

        Texture2D texture = Resources.Load<Texture2D>("Images/Weapon/" + GameSelectionConfig.GetWeaponObjectName(weaponType));
        weaponPreviewCache[weaponType] = texture;
        return texture;
    }

    private void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        Texture texture = sprite.texture;
        Rect spriteRect = sprite.rect;
        Rect uvRect = new Rect(
            spriteRect.x / texture.width,
            spriteRect.y / texture.height,
            spriteRect.width / texture.width,
            spriteRect.height / texture.height);
        float spriteAspect = spriteRect.width / spriteRect.height;
        float rectAspect = rect.width / rect.height;
        Rect drawRect = rect;

        if (spriteAspect > rectAspect)
        {
            float height = rect.width / spriteAspect;
            drawRect.y += (rect.height - height) * 0.5f;
            drawRect.height = height;
        }
        else
        {
            float width = rect.height * spriteAspect;
            drawRect.x += (rect.width - width) * 0.5f;
            drawRect.width = width;
        }

        GUI.DrawTextureWithTexCoords(drawRect, texture, uvRect, true);
    }

    private void DrawExitDialog()
    {
        Rect dialogRect = new Rect(VirtualWidth * 0.5f - 210f, VirtualHeight * 0.5f - 110f, 420f, 220f);
        DrawCard(dialogRect, "\u786e\u8ba4\u9000\u51fa");
        DrawCompatibleLabel(new Rect(dialogRect.x + 24f, dialogRect.y + 62f, dialogRect.width - 48f, 24f), "\u73b0\u5728\u9000\u51fa\u6e38\u620f\u5417\uff1f", CreateLabelStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        DrawCompatibleLabel(new Rect(dialogRect.x + 24f, dialogRect.y + 92f, dialogRect.width - 48f, 20f), "\u5f53\u524d\u5bf9\u5c40\u7684\u7ee7\u7eed\u5b58\u6863\u4f1a\u88ab\u4fdd\u7559\u3002", CreateLabelStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText));

        if (DrawButton(new Rect(dialogRect.x + 36f, dialogRect.y + 146f, 146f, 40f), "\u53d6\u6d88"))
        {
            showExitConfirm = false;
        }

        if (DrawButton(new Rect(dialogRect.x + dialogRect.width - 182f, dialogRect.y + 146f, 146f, 40f), "\u9000\u51fa\u6e38\u620f"))
        {
            showExitConfirm = false;
            ExitGame();
        }
    }

    private void DrawCard(Rect rect, string title)
    {
        DrawFilledRect(rect, CardBackground);
        DrawBorder(rect, CardBorder, 2f);
        DrawFilledRect(new Rect(rect.x, rect.y, rect.width, 5f), Accent);
        DrawCompatibleLabel(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, 22f), title, CreateLabelStyle(17, FontStyle.Bold, TextAnchor.UpperLeft, Accent));
    }

    private bool DrawButton(Rect rect, string label)
    {
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            font = CjkFontHelper.GetFont()
        };
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;

        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.18f, 0.27f, 0.4f, 1f);
        bool clicked = GUI.Button(rect, label, buttonStyle);
        GUI.backgroundColor = previous;
        return clicked;
    }

    private GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, TextAnchor anchor, Color color)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = fontStyle,
            alignment = anchor,
            wordWrap = true,
            clipping = TextClipping.Overflow,
            font = CjkFontHelper.GetFont()
        };
        style.normal.textColor = color;
        return style;
    }

    private void DrawCompatibleLabel(Rect rect, string text, GUIStyle style)
    {
        if (style == null)
        {
            GUI.Label(rect, text);
            return;
        }

        rect = GetExpandedLabelRect(rect, text, style);

        if (style.fontStyle != FontStyle.Bold && style.fontStyle != FontStyle.BoldAndItalic)
        {
            GUI.Label(rect, text, style);
            return;
        }

        GUIStyle compatibleStyle = new GUIStyle(style)
        {
            fontStyle = style.fontStyle == FontStyle.BoldAndItalic ? FontStyle.Italic : FontStyle.Normal
        };
        GUIStyle outlineStyle = new GUIStyle(compatibleStyle);
        Color mainColor = compatibleStyle.normal.textColor;
        outlineStyle.normal.textColor = new Color(0f, 0f, 0f, Mathf.Clamp01(mainColor.a * 0.68f));

        GUI.Label(new Rect(rect.x - 0.65f, rect.y, rect.width, rect.height), text, outlineStyle);
        GUI.Label(new Rect(rect.x + 0.65f, rect.y, rect.width, rect.height), text, outlineStyle);
        GUI.Label(new Rect(rect.x, rect.y - 0.65f, rect.width, rect.height), text, outlineStyle);
        GUI.Label(new Rect(rect.x, rect.y + 0.65f, rect.width, rect.height), text, outlineStyle);
        GUI.Label(rect, text, compatibleStyle);
    }

    private Rect GetExpandedLabelRect(Rect rect, string text, GUIStyle style)
    {
        GUIStyle measureStyle = new GUIStyle(style)
        {
            clipping = TextClipping.Overflow
        };

        float paddingX = Mathf.Max(6f, style.fontSize * 0.18f);
        float paddingY = Mathf.Max(8f, style.fontSize * 0.28f);
        float measuredWidth = Mathf.Max(1f, rect.width + paddingX * 2f);
        float measuredHeight = Mathf.Max(rect.height, measureStyle.CalcHeight(new GUIContent(text), measuredWidth) + paddingY);
        float offsetY = 0f;

        if (style.alignment == TextAnchor.MiddleLeft ||
            style.alignment == TextAnchor.MiddleCenter ||
            style.alignment == TextAnchor.MiddleRight)
        {
            offsetY = (measuredHeight - rect.height) * 0.5f;
        }
        else if (style.alignment == TextAnchor.LowerLeft ||
                 style.alignment == TextAnchor.LowerCenter ||
                 style.alignment == TextAnchor.LowerRight)
        {
            offsetY = measuredHeight - rect.height;
        }

        return new Rect(
            rect.x - paddingX * 0.5f,
            rect.y - paddingY * 0.35f - offsetY,
            rect.width + paddingX,
            measuredHeight);
    }

    private void BeginVirtualCanvas()
    {
        previousGuiMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / VirtualWidth, Screen.height / VirtualHeight);
        Vector2 offset = new Vector2(
            (Screen.width - (VirtualWidth * scale)) * 0.5f,
            (Screen.height - (VirtualHeight * scale)) * 0.5f);
        GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1f));
    }

    private void EndVirtualCanvas()
    {
        GUI.matrix = previousGuiMatrix;
    }

    private void DrawFilledRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void DrawBorder(Rect rect, Color color, float thickness)
    {
        DrawFilledRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawFilledRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawFilledRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawFilledRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private static string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
