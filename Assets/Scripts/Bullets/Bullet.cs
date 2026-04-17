using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private readonly HashSet<int> hitEnemyIds = new HashSet<int>();

    private RoguelikeGameManager owner;
    private Vector2 direction;
    private Vector2 previousPosition;
    private float speed;
    private float remainingLifetime;
    private float hitRadius;
    private int damage;
    private int remainingPierce;

    public int Damage => damage;
    public float HitRadius => hitRadius;
    public int RemainingPierce => remainingPierce;
    public Vector2 PreviousPosition => previousPosition;
    public Vector2 Position => transform.position;

    public void Fire(
        RoguelikeGameManager owner,
        Vector3 startPosition,
        Vector2 direction,
        int damage,
        float speed,
        float lifetime,
        int pierce,
        float scale)
    {
        this.owner = owner;
        this.direction = direction.normalized;
        if (this.direction.sqrMagnitude <= 0.001f)
        {
            this.direction = Vector2.right;
        }

        this.damage = damage;
        this.speed = speed;
        remainingLifetime = Mathf.Max(90f, lifetime);
        remainingPierce = pierce;
        hitRadius = Mathf.Max(0.25f, scale * 0.45f);
        hitEnemyIds.Clear();

        transform.position = startPosition;
        previousPosition = startPosition;
        transform.localScale = new Vector3(scale * 5f, scale * 5f, 1f);
        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public bool Tick(float deltaTime)
    {
        previousPosition = transform.position;
        transform.position += (Vector3)(direction * speed * deltaTime);

        remainingLifetime -= deltaTime;
        if (remainingLifetime <= 0f)
        {
            return false;
        }

        if (owner == null)
        {
            return true;
        }

        if (!owner.ProcessBulletHit(this))
        {
            return false;
        }

        return owner.IsInsideMapBounds(Position, hitRadius * 0.35f);
    }

    public void ConsumePierce()
    {
        remainingPierce--;
    }

    public bool HasHitEnemy(EnemyActor enemy)
    {
        return enemy != null && hitEnemyIds.Contains(enemy.GetInstanceID());
    }

    public void RegisterEnemyHit(EnemyActor enemy)
    {
        if (enemy == null)
        {
            return;
        }

        hitEnemyIds.Add(enemy.GetInstanceID());
    }
}
