using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class BreakableChest : MonoBehaviour
{
    private static readonly List<BreakableChest> ActiveChestBuffer = new List<BreakableChest>();

    private const int DefaultMaxHits = 100;
    private const float ColliderWidthFactor = 0.78f;
    private const float ColliderHeightFactor = 0.5f;
    private const float ColliderYOffsetFactor = -0.14f;

    private readonly List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();

    private BoxCollider2D obstacleCollider;
    private ObstacleMarker obstacleMarker;
    private int maxHits = DefaultMaxHits;
    private int currentHits;
    private bool isBroken;
    private bool isRuntimeSpawned;
    private string sceneKey;
    private float uiHeadOffset = 1.1f;

    public static IReadOnlyList<BreakableChest> ActiveChests => ActiveChestBuffer;

    public int MaxHits => maxHits;
    public int CurrentHits => currentHits;
    public int RemainingHits => Mathf.Max(0, maxHits - currentHits);
    public bool IsBroken => isBroken;
    public float RemainingRatio => maxHits <= 0 ? 0f : Mathf.Clamp01(RemainingHits / (float)maxHits);
    public Vector2 Position => transform.position;
    public float UiHeadOffset => uiHeadOffset;
    public string SceneKey => sceneKey;
    public bool IsRuntimeSpawned => isRuntimeSpawned;
    public Vector2 ColliderSize => obstacleCollider != null ? Vector2.Scale(obstacleCollider.size, new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))) : Vector2.one;
    public Vector2 ColliderOffset => obstacleCollider != null ? Vector2.Scale(obstacleCollider.offset, new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))) : Vector2.zero;

    public static BreakableChest EnsureConfigured(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        BreakableChest chest = target.GetComponent<BreakableChest>();
        if (chest == null)
        {
            chest = target.AddComponent<BreakableChest>();
        }

        chest.ConfigureRuntime();
        return chest;
    }

    private void OnEnable()
    {
        if (!ActiveChestBuffer.Contains(this))
        {
            ActiveChestBuffer.Add(this);
        }

        ConfigureRuntime();
    }

    private void OnDisable()
    {
        ActiveChestBuffer.Remove(this);
    }

    public void ConfigureRuntime()
    {
        obstacleCollider = GetComponent<BoxCollider2D>();
        if (obstacleCollider == null)
        {
            obstacleCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        obstacleCollider.isTrigger = false;
        obstacleMarker = GetComponent<ObstacleMarker>();
        ConfigureCollider();
        CacheSpriteRenderers();
        sceneKey = BuildSceneKey();
        RefreshVisualState();
    }

    public void ResetRuntimeState()
    {
        maxHits = DefaultMaxHits;
        currentHits = 0;
        isBroken = false;
        RefreshVisualState();
    }

    public void RestoreRuntimeState(int hits, bool broken)
    {
        currentHits = Mathf.Clamp(hits, 0, maxHits);
        isBroken = broken || currentHits >= maxHits;
        if (isBroken)
        {
            currentHits = maxHits;
        }

        RefreshVisualState();
    }

    public void SetMaxHits(int hitRequirement)
    {
        maxHits = Mathf.Max(1, hitRequirement);
        currentHits = Mathf.Clamp(currentHits, 0, maxHits);
        if (isBroken)
        {
            currentHits = maxHits;
        }
    }

    public void SetSceneKey(string key)
    {
        sceneKey = string.IsNullOrWhiteSpace(key) ? BuildSceneKey() : key;
    }

    public void SetRuntimeSpawned(bool runtimeSpawned)
    {
        isRuntimeSpawned = runtimeSpawned;
    }

    public bool TryGetHit(Vector2 segmentStart, Vector2 segmentEnd, float hitRadius, out float hitT)
    {
        hitT = 0f;
        if (isBroken || obstacleCollider == null || !gameObject.activeInHierarchy)
        {
            return false;
        }

        Bounds bounds = obstacleCollider.bounds;
        bounds.Expand(new Vector3(hitRadius * 2f, hitRadius * 2f, 0f));
        return SegmentAabbIntersects(segmentStart, segmentEnd, bounds.min, bounds.max, out hitT);
    }

    public bool ApplyBulletHit(out bool brokeThisFrame)
    {
        brokeThisFrame = false;
        if (isBroken)
        {
            return false;
        }

        currentHits = Mathf.Clamp(currentHits + 1, 0, maxHits);
        if (currentHits < maxHits)
        {
            return true;
        }

        isBroken = true;
        brokeThisFrame = true;
        RefreshVisualState();
        return true;
    }

    private void CacheSpriteRenderers()
    {
        spriteRenderers.Clear();
        GetComponentsInChildren(true, spriteRenderers);

        float top = transform.position.y + 1f;
        bool hasBounds = false;
        for (int index = 0; index < spriteRenderers.Count; index++)
        {
            SpriteRenderer renderer = spriteRenderers[index];
            if (renderer == null || renderer.sprite == null)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            top = hasBounds ? Mathf.Max(top, bounds.max.y) : bounds.max.y;
            hasBounds = true;
        }

        uiHeadOffset = Mathf.Max(1f, top - transform.position.y + 0.3f);
    }

    private void ConfigureCollider()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            obstacleCollider.size = Vector2.one;
            obstacleCollider.offset = Vector2.zero;
            return;
        }

        Vector2 spriteSize = renderer.sprite.bounds.size;
        obstacleCollider.size = new Vector2(
            Mathf.Max(0.4f, spriteSize.x * ColliderWidthFactor),
            Mathf.Max(0.4f, spriteSize.y * ColliderHeightFactor));
        obstacleCollider.offset = new Vector2(0f, spriteSize.y * ColliderYOffsetFactor);
    }

    private void RefreshVisualState()
    {
        bool visible = !isBroken;
        for (int index = 0; index < spriteRenderers.Count; index++)
        {
            if (spriteRenderers[index] != null)
            {
                spriteRenderers[index].enabled = visible;
            }
        }

        if (obstacleCollider != null)
        {
            obstacleCollider.enabled = visible;
        }

        if (obstacleMarker != null)
        {
            obstacleMarker.enabled = visible;
        }
    }

    private string BuildSceneKey()
    {
        Transform mapRoot = GameObject.Find("map")?.transform;
        if (mapRoot == null)
        {
            return transform.GetInstanceID().ToString();
        }

        List<int> siblingIndices = new List<int>();
        Transform cursor = transform;
        while (cursor != null && cursor != mapRoot)
        {
            siblingIndices.Add(cursor.GetSiblingIndex());
            cursor = cursor.parent;
        }

        siblingIndices.Reverse();
        StringBuilder builder = new StringBuilder("map");
        for (int index = 0; index < siblingIndices.Count; index++)
        {
            builder.Append('/').Append(siblingIndices[index]);
        }

        return builder.ToString();
    }

    private static bool SegmentAabbIntersects(Vector2 start, Vector2 end, Vector2 min, Vector2 max, out float hitT)
    {
        hitT = 0f;
        Vector2 delta = end - start;
        float enterT = 0f;
        float exitT = 1f;

        for (int axis = 0; axis < 2; axis++)
        {
            float startValue = axis == 0 ? start.x : start.y;
            float deltaValue = axis == 0 ? delta.x : delta.y;
            float minValue = axis == 0 ? min.x : min.y;
            float maxValue = axis == 0 ? max.x : max.y;

            if (Mathf.Abs(deltaValue) <= 0.0001f)
            {
                if (startValue < minValue || startValue > maxValue)
                {
                    return false;
                }

                continue;
            }

            float inverse = 1f / deltaValue;
            float axisEnter = (minValue - startValue) * inverse;
            float axisExit = (maxValue - startValue) * inverse;
            if (axisEnter > axisExit)
            {
                float swap = axisEnter;
                axisEnter = axisExit;
                axisExit = swap;
            }

            enterT = Mathf.Max(enterT, axisEnter);
            exitT = Mathf.Min(exitT, axisExit);
            if (enterT > exitT)
            {
                return false;
            }
        }

        hitT = Mathf.Clamp01(enterT);
        return exitT >= 0f && enterT <= 1f;
    }
}
