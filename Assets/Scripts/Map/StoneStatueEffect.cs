using System.Collections.Generic;
using UnityEngine;

public sealed class StoneStatueEffect : MonoBehaviour
{
    private static readonly List<StoneStatueEffect> ActiveStatueBuffer = new List<StoneStatueEffect>();

    private const float MinHealRadius = 1.9f;
    private const float HealRadiusScale = 2.1f;
    private const float OcclusionThresholdPadding = 0.05f;
    private const float AuraAlpha = 0.32f;

    private static Sprite auraSprite;

    private readonly List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    private readonly List<int> baseSortingOrders = new List<int>();

    private BoxCollider2D obstacleCollider;
    private SpriteRenderer auraRenderer;
    private float healRadius;
    private float occlusionThresholdY;
    private Bounds occlusionBounds;

    public static IReadOnlyList<StoneStatueEffect> ActiveStatues => ActiveStatueBuffer;

    public float HealRadius => healRadius;
    public Vector2 HealCenter => obstacleCollider != null ? obstacleCollider.bounds.center : transform.position;

    public static StoneStatueEffect EnsureConfigured(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        StoneStatueEffect effect = target.GetComponent<StoneStatueEffect>();
        if (effect == null)
        {
            effect = target.AddComponent<StoneStatueEffect>();
        }

        effect.ConfigureRuntime();
        return effect;
    }

    private void OnEnable()
    {
        if (!ActiveStatueBuffer.Contains(this))
        {
            ActiveStatueBuffer.Add(this);
        }

        ConfigureRuntime();
    }

    private void OnDisable()
    {
        RestoreSortingOrder();
        ActiveStatueBuffer.Remove(this);
    }

    private void LateUpdate()
    {
        UpdateOcclusion();
    }

    public void ConfigureRuntime()
    {
        obstacleCollider = GetComponent<BoxCollider2D>();
        CacheSpriteRenderers();
        CacheBaseSortingOrders();
        RecalculateGeometry();
        EnsureAuraRenderer();
        RefreshAuraVisual();
    }

    public bool ContainsPlayer(Vector2 playerPosition, float playerRadius)
    {
        float combinedRadius = healRadius + Mathf.Max(0f, playerRadius * 0.35f);
        return (playerPosition - HealCenter).sqrMagnitude <= combinedRadius * combinedRadius;
    }

    public bool ShouldOccludePoint(Vector2 point, float radius = 0f)
    {
        Bounds bounds = occlusionBounds;
        if (bounds.size.sqrMagnitude <= 0.0001f)
        {
            Vector3 center = transform.position;
            bounds = new Bounds(center, new Vector3(1f, 1f, 0.5f));
        }

        float expand = Mathf.Max(0f, radius) * 2f;
        if (expand > 0f)
        {
            bounds.Expand(new Vector3(expand, expand, 0f));
        }

        return point.y >= occlusionThresholdY
            && point.x >= bounds.min.x
            && point.x <= bounds.max.x
            && point.y >= bounds.min.y
            && point.y <= bounds.max.y;
    }

    private void CacheSpriteRenderers()
    {
        spriteRenderers.Clear();
        GetComponentsInChildren(true, spriteRenderers);
        spriteRenderers.RemoveAll(renderer => renderer == null || renderer == auraRenderer || renderer.gameObject.name == "HealingAura");
    }

    private void CacheBaseSortingOrders()
    {
        if (baseSortingOrders.Count == spriteRenderers.Count && baseSortingOrders.Count > 0)
        {
            return;
        }

        baseSortingOrders.Clear();
        for (int index = 0; index < spriteRenderers.Count; index++)
        {
            baseSortingOrders.Add(spriteRenderers[index].sortingOrder);
        }
    }

    private void RecalculateGeometry()
    {
        Bounds combinedBounds = default;
        bool hasBounds = false;

        for (int index = 0; index < spriteRenderers.Count; index++)
        {
            SpriteRenderer renderer = spriteRenderers[index];
            if (renderer == null || renderer.sprite == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            combinedBounds.Encapsulate(renderer.bounds);
        }

        if (!hasBounds)
        {
            healRadius = MinHealRadius;
            occlusionThresholdY = transform.position.y;
            occlusionBounds = new Bounds(transform.position, new Vector3(1f, 1f, 0.5f));
            return;
        }

        float visualRadius = Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.y) * HealRadiusScale;
        healRadius = Mathf.Max(MinHealRadius, visualRadius);
        occlusionBounds = combinedBounds;
        occlusionThresholdY = obstacleCollider != null
            ? obstacleCollider.bounds.max.y - OcclusionThresholdPadding
            : combinedBounds.min.y + (combinedBounds.size.y * 0.28f);
    }

    private void EnsureAuraRenderer()
    {
        if (auraRenderer == null)
        {
            Transform child = transform.Find("HealingAura");
            if (child == null)
            {
                GameObject auraObject = new GameObject("HealingAura");
                auraObject.transform.SetParent(transform, false);
                child = auraObject.transform;
            }

            auraRenderer = child.GetComponent<SpriteRenderer>();
            if (auraRenderer == null)
            {
                auraRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }
        }

        auraRenderer.sprite = GetOrCreateAuraSprite();
        auraRenderer.color = new Color(0.24f, 0.92f, 0.5f, AuraAlpha);
        auraRenderer.sortingOrder = GetLowestSortingOrder() - 2;
        auraRenderer.transform.localPosition = (Vector3)(HealCenter - (Vector2)transform.position) + new Vector3(0f, 0f, 0.01f);
        auraRenderer.transform.localRotation = Quaternion.identity;
        auraRenderer.transform.localScale = new Vector3(healRadius * 2f, healRadius * 2f, 1f);
    }

    private void RefreshAuraVisual()
    {
        if (auraRenderer == null)
        {
            return;
        }

        auraRenderer.enabled = true;
    }

    private void UpdateOcclusion()
    {
        RoguelikeGameManager owner = RoguelikeGameManager.Instance;
        if (owner == null || spriteRenderers.Count == 0)
        {
            RestoreSortingOrder();
            return;
        }

        bool shouldCoverPlayer = owner.PlayerPosition.y >= occlusionThresholdY;
        int targetBaseSorting = shouldCoverPlayer ? owner.GetHighestPlayerSortingOrder() + 1 : int.MinValue;
        for (int index = 0; index < spriteRenderers.Count; index++)
        {
            SpriteRenderer renderer = spriteRenderers[index];
            if (renderer == null)
            {
                continue;
            }

            int baseSorting = index < baseSortingOrders.Count ? baseSortingOrders[index] : renderer.sortingOrder;
            renderer.sortingOrder = shouldCoverPlayer ? Mathf.Max(baseSorting, targetBaseSorting) : baseSorting;
        }

        if (auraRenderer != null)
        {
            auraRenderer.sortingOrder = GetLowestSortingOrder() - 2;
        }
    }

    private void RestoreSortingOrder()
    {
        for (int index = 0; index < spriteRenderers.Count; index++)
        {
            SpriteRenderer renderer = spriteRenderers[index];
            if (renderer == null)
            {
                continue;
            }

            if (index < baseSortingOrders.Count)
            {
                renderer.sortingOrder = baseSortingOrders[index];
            }
        }
    }

    private int GetLowestSortingOrder()
    {
        int order = 0;
        bool hasValue = false;
        for (int index = 0; index < spriteRenderers.Count; index++)
        {
            if (spriteRenderers[index] == null)
            {
                continue;
            }

            int current = index < baseSortingOrders.Count ? baseSortingOrders[index] : spriteRenderers[index].sortingOrder;
            if (!hasValue || current < order)
            {
                order = current;
                hasValue = true;
            }
        }

        return hasValue ? order : 0;
    }

    private static Sprite GetOrCreateAuraSprite()
    {
        if (auraSprite != null)
        {
            return auraSprite;
        }

        const int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "StoneStatueHealingAura",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float radius = textureSize * 0.5f;
        float innerRing = radius * 0.78f;
        float outerRing = radius * 0.98f;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color[] pixels = new Color[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 0f;
                if (distance <= outerRing)
                {
                    alpha = distance >= innerRing
                        ? Mathf.InverseLerp(outerRing, innerRing, distance)
                        : Mathf.Lerp(0.22f, 0f, distance / Mathf.Max(0.0001f, innerRing));
                }

                pixels[(y * textureSize) + x] = alpha <= 0.001f
                    ? clear
                    : new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        auraSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
        return auraSprite;
    }
}
