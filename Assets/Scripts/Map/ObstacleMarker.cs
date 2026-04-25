using System;
using UnityEngine;

public sealed class ObstacleMarker : MonoBehaviour
{
    private enum ObstacleKind
    {
        None,
        Generic,
        StoneStatue,
        BreakableChest
    }

    private const float ColliderWidthFactor = 0.72f;
    private const float ColliderHeightFactor = 0.34f;
    private const float ColliderYOffsetFactor = -0.28f;

    public static void ConfigureSceneObstacles()
    {
        Transform mapRoot = GameObject.Find("map")?.transform;
        if (mapRoot == null)
        {
            return;
        }

        foreach (Transform child in mapRoot)
        {
            ObstacleKind obstacleKind = ResolveObstacleKind(child.name);
            if (obstacleKind == ObstacleKind.None)
            {
                continue;
            }

            ObstacleMarker marker = child.GetComponent<ObstacleMarker>();
            if (marker == null)
            {
                marker = child.gameObject.AddComponent<ObstacleMarker>();
            }

            marker.EnsureCollider();

            if (obstacleKind == ObstacleKind.StoneStatue)
            {
                StoneStatueEffect.EnsureConfigured(child.gameObject);
                continue;
            }

            if (obstacleKind == ObstacleKind.BreakableChest)
            {
                BreakableChest.EnsureConfigured(child.gameObject);
            }
        }
    }

    public static bool IsObstacle(Collider2D collider)
    {
        return collider != null && collider.GetComponentInParent<ObstacleMarker>() != null;
    }

    private static ObstacleKind ResolveObstacleKind(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return ObstacleKind.None;
        }

        if (string.Equals(objectName, "stoneStatue", StringComparison.OrdinalIgnoreCase))
        {
            return ObstacleKind.StoneStatue;
        }

        if (string.Equals(objectName, "box", StringComparison.OrdinalIgnoreCase))
        {
            return ObstacleKind.BreakableChest;
        }

        return objectName.IndexOf("cap", StringComparison.OrdinalIgnoreCase) >= 0
            ? ObstacleKind.Generic
            : ObstacleKind.None;
    }

    private void EnsureCollider()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = false;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            collider.size = Vector2.one;
            collider.offset = Vector2.zero;
            return;
        }

        Vector2 spriteSize = renderer.sprite.bounds.size;
        collider.size = new Vector2(
            Mathf.Max(0.35f, spriteSize.x * ColliderWidthFactor),
            Mathf.Max(0.35f, spriteSize.y * ColliderHeightFactor));
        collider.offset = new Vector2(0f, spriteSize.y * ColliderYOffsetFactor);
    }
}
