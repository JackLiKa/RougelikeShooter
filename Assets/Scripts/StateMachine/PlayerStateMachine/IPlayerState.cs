using UnityEngine;

public abstract class IPlayerState:IState
{
    public new PlayerStateMachine m_Machine
    {
        get=>base.m_Machine as PlayerStateMachine;
        set=>base.m_Machine=value;
    }
    protected IPlayer player;
    protected GameObject gameObject;
    protected Transform transform=>gameObject.transform;
    protected Rigidbody2D m_rb;
    protected Animator m_Animator;
    protected CharacterAnimationBridge animationBridge;
    private Collider2D movementCollider;
    
    public IPlayerState(PlayerStateMachine machine):base(machine){ }
    protected override void OnInit()
    {
        base.OnInit();
        player=m_Machine.m_Player;
        gameObject=player.gameObject;
        m_rb=transform.GetComponent<Rigidbody2D>();
        animationBridge = CharacterAnimationBridge.GetOrCreate(gameObject);
        m_Animator = animationBridge != null ? animationBridge.Animator : UnityTool.Instance.GetComponentFromChildren<Animator>(gameObject,"Sprite");
    }
    protected override void OnEnter()
    {
        base.OnEnter();
        Debug.Log(this);
    }

    protected bool CanReadPlayerInput()
    {
        return RoguelikeGameManager.Instance == null || RoguelikeGameManager.Instance.CanAcceptPlayerInput;
    }

    protected bool TryMove(Vector2 normalizedDirection, float moveSpeed)
    {
        if (normalizedDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector2 currentPosition = m_rb != null ? m_rb.position : (Vector2)transform.position;
        Vector2 delta = normalizedDirection * moveSpeed * Time.deltaTime;
        Vector2 resolvedPosition = ResolveMovement(currentPosition, delta);
        bool hasMoved = (resolvedPosition - currentPosition).sqrMagnitude > 0.0001f;
        if (!hasMoved)
        {
            return false;
        }

        if (m_rb != null)
        {
            m_rb.MovePosition(resolvedPosition);
        }
        else
        {
            transform.position = resolvedPosition;
        }

        return true;
    }

    private Vector2 ResolveMovement(Vector2 currentPosition, Vector2 delta)
    {
        Vector2 resolved = currentPosition;
        Vector2 xTarget = ClampToPlayableMapBounds(resolved + new Vector2(delta.x, 0f));
        if (!IsBlockedAt(xTarget))
        {
            resolved = xTarget;
        }

        Vector2 yTarget = ClampToPlayableMapBounds(resolved + new Vector2(0f, delta.y));
        if (!IsBlockedAt(yTarget))
        {
            resolved = yTarget;
        }

        return resolved;
    }

    private bool IsBlockedAt(Vector2 rootPosition)
    {
        if (!IsInsidePlayableMapBounds(rootPosition))
        {
            return true;
        }

        Collider2D collider = GetMovementCollider();
        if (collider == null)
        {
            return false;
        }

        Vector2 centerOffset = (Vector2)collider.bounds.center - (Vector2)transform.position;
        Vector2 overlapCenter = rootPosition + centerOffset;
        Vector2 overlapSize = collider.bounds.size * 0.88f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(overlapCenter, overlapSize, 0f);
        for (int index = 0; index < hits.Length; index++)
        {
            Collider2D hit = hits[index];
            if (hit == null || hit.isTrigger)
            {
                continue;
            }

            if (hit.attachedRigidbody == m_rb || hit.transform.IsChildOf(transform) || transform.IsChildOf(hit.transform))
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

    private Vector2 ClampToPlayableMapBounds(Vector2 rootPosition)
    {
        RoguelikeGameManager manager = RoguelikeGameManager.Instance;
        if (manager == null)
        {
            return rootPosition;
        }

        Vector2 movementPadding = GetMovementPadding();
        return manager.ClampPositionToMapBounds(rootPosition, movementPadding);
    }

    private bool IsInsidePlayableMapBounds(Vector2 rootPosition)
    {
        RoguelikeGameManager manager = RoguelikeGameManager.Instance;
        if (manager == null)
        {
            return true;
        }

        Vector2 clampedPosition = manager.ClampPositionToMapBounds(rootPosition, GetMovementPadding());
        return (clampedPosition - rootPosition).sqrMagnitude <= 0.0001f;
    }

    private Vector2 GetMovementPadding()
    {
        Collider2D collider = GetMovementCollider();
        if (collider == null)
        {
            return Vector2.zero;
        }

        return collider.bounds.extents * 0.88f;
    }

    private Collider2D GetMovementCollider()
    {
        if (movementCollider != null)
        {
            return movementCollider;
        }

        Collider2D[] colliders = gameObject.GetComponentsInChildren<Collider2D>(true);
        for (int index = 0; index < colliders.Length; index++)
        {
            Collider2D candidate = colliders[index];
            if (candidate == null || candidate.isTrigger)
            {
                continue;
            }

            if (candidate.name == "Collider")
            {
                movementCollider = candidate;
                return movementCollider;
            }
        }

        for (int index = 0; index < colliders.Length; index++)
        {
            Collider2D candidate = colliders[index];
            if (candidate == null || candidate.isTrigger)
            {
                continue;
            }

            movementCollider = candidate;
            return movementCollider;
        }

        return null;
    }

}
