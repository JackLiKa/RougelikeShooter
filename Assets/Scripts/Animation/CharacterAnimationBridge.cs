using UnityEngine;

public sealed class CharacterAnimationBridge : MonoBehaviour
{
    private static readonly int IsRunHash = Animator.StringToHash("isRun");
    private static readonly int IsDieHash = Animator.StringToHash("isDie");

    private Animator cachedAnimator;
    private bool supportsRun;
    private bool supportsDie;
    private bool resolvedAnimator;
    private bool isDying;

    public Animator Animator => ResolveAnimator();

    private void Awake()
    {
        ResolveAnimator();
    }

    private void OnEnable()
    {
        ResolveAnimator();
    }

    public static CharacterAnimationBridge GetOrCreate(GameObject owner)
    {
        if (owner == null)
        {
            return null;
        }

        CharacterAnimationBridge bridge = owner.GetComponent<CharacterAnimationBridge>();
        return bridge != null ? bridge : owner.AddComponent<CharacterAnimationBridge>();
    }

    public void ResetState()
    {
        isDying = false;
        SetDying(false);
        SetRunning(false);
    }

    public void SetRunning(bool isRunning)
    {
        Animator animator = ResolveAnimator();
        if (animator == null || !supportsRun)
        {
            return;
        }

        animator.SetBool(IsRunHash, !isDying && isRunning);
    }

    public void SetDying(bool dying)
    {
        Animator animator = ResolveAnimator();
        isDying = dying;
        if (animator == null)
        {
            return;
        }

        if (supportsRun)
        {
            animator.SetBool(IsRunHash, false);
        }

        if (supportsDie)
        {
            animator.SetBool(IsDieHash, dying);
        }
    }

    private Animator ResolveAnimator()
    {
        if (cachedAnimator != null)
        {
            return cachedAnimator;
        }

        if (resolvedAnimator)
        {
            return null;
        }

        resolvedAnimator = true;

        Transform spriteRoot = transform.Find("Sprite");
        if (spriteRoot != null)
        {
            cachedAnimator = spriteRoot.GetComponent<Animator>();
        }

        if (cachedAnimator == null)
        {
            cachedAnimator = GetComponent<Animator>();
        }

        if (cachedAnimator == null)
        {
            cachedAnimator = GetComponentInChildren<Animator>(true);
        }

        RefreshParameterSupport();
        return cachedAnimator;
    }

    private void RefreshParameterSupport()
    {
        supportsRun = false;
        supportsDie = false;
        if (cachedAnimator == null)
        {
            return;
        }

        AnimatorControllerParameter[] parameters = cachedAnimator.parameters;
        for (int index = 0; index < parameters.Length; index++)
        {
            AnimatorControllerParameter parameter = parameters[index];
            if (parameter.nameHash == IsRunHash)
            {
                supportsRun = true;
            }
            else if (parameter.nameHash == IsDieHash)
            {
                supportsDie = true;
            }
        }
    }
}
