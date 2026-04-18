using UnityEngine;

public class ExperiencePickup : MonoBehaviour
{
    private const string NormalSpritePath = "Images/Materials/Gem";
    private const string EliteSpritePath = "Images/Materials/OrangeMagicFragments";
    private const float MagnetRadiusMultiplier = 7.6f;
    private const float MagnetVisualDistanceMultiplier = 20f;

    private static Sprite cachedNormalSprite;
    private static Sprite cachedEliteSprite;

    private RoguelikeGameManager owner;
    private float experienceValue;
    private bool grantsFreeLevel;
    private float moveSpeed;
    private float pulseSeed;
    private float baseScale;
    private float magnetAnimation;

    public float PickupRadius { get; private set; }
    public float ExperienceValue => experienceValue;
    public bool GrantsFreeLevel => grantsFreeLevel;
    public Vector2 Position => transform.position;

    public void Configure(RoguelikeGameManager owner, Vector3 position, float experienceValue, bool grantsFreeLevel, float scale)
    {
        this.owner = owner;
        this.experienceValue = Mathf.Max(0f, experienceValue);
        this.grantsFreeLevel = grantsFreeLevel;
        moveSpeed = grantsFreeLevel ? 14f : 10f;
        PickupRadius = grantsFreeLevel ? 1f : 0.7f;
        pulseSeed = Random.Range(0f, Mathf.PI * 2f);
        baseScale = scale;
        magnetAnimation = 0f;
        transform.position = position;
        transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = grantsFreeLevel ? GetEliteSprite() : GetNormalSprite();
            renderer.color = grantsFreeLevel ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(0.4f, 1f, 0.55f, 1f);
            renderer.sortingOrder = 24;
        }
    }

    public bool Tick(float deltaTime, Vector3 playerPosition, float magnetRadius)
    {
        pulseSeed += deltaTime * (grantsFreeLevel ? 5f : 3.5f);
        float pulseAmount = grantsFreeLevel ? 0.08f : 0.04f;
        float pulseScale = baseScale * (1f + Mathf.Sin(pulseSeed) * pulseAmount);
        transform.localScale = new Vector3(pulseScale, pulseScale, 1f);

        Vector2 toPlayer = playerPosition - transform.position;
        float distance = toPlayer.magnitude;
        float pickupTriggerDistance = PickupRadius + owner.PlayerHitRadius;
        float visualMagnetDistance = owner.PlayerHitRadius * MagnetVisualDistanceMultiplier;
        float attractionStartDistance = Mathf.Max(magnetRadius * MagnetRadiusMultiplier, pickupTriggerDistance + visualMagnetDistance);
        if (distance <= attractionStartDistance && distance > 0.001f)
        {
            float travelRange = Mathf.Max(0.01f, attractionStartDistance - pickupTriggerDistance);
            float attractionRatio = 1f - Mathf.Clamp01((distance - pickupTriggerDistance) / travelRange);
            float attractionSpeed = moveSpeed + (attractionRatio * attractionRatio * (grantsFreeLevel ? 30f : 22f));
            if (distance <= pickupTriggerDistance + owner.PlayerHitRadius)
            {
                attractionSpeed += grantsFreeLevel ? 12f : 9f;
            }

            magnetAnimation = Mathf.MoveTowards(magnetAnimation, 1f, deltaTime * 6f);
            transform.position = Vector3.MoveTowards(transform.position, playerPosition, attractionSpeed * deltaTime);
            float travelAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, travelAngle - 90f);
        }
        else
        {
            magnetAnimation = Mathf.MoveTowards(magnetAnimation, 0f, deltaTime * 3.5f);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(pulseSeed * 0.75f) * 6f);
        }

        float stretch = Mathf.Lerp(1f, grantsFreeLevel ? 1.42f : 1.28f, magnetAnimation);
        float squash = Mathf.Lerp(1f, grantsFreeLevel ? 0.82f : 0.88f, magnetAnimation);
        transform.localScale = new Vector3(pulseScale * squash, pulseScale * stretch, 1f);

        if (distance <= pickupTriggerDistance)
        {
            owner.CollectPickup(this);
            return false;
        }

        return true;
    }

    private static Sprite GetNormalSprite()
    {
        if (cachedNormalSprite == null)
        {
            cachedNormalSprite = Resources.Load<Sprite>(NormalSpritePath);
        }

        return cachedNormalSprite;
    }

    private static Sprite GetEliteSprite()
    {
        if (cachedEliteSprite == null)
        {
            cachedEliteSprite = Resources.Load<Sprite>(EliteSpritePath);
        }

        return cachedEliteSprite != null ? cachedEliteSprite : GetNormalSprite();
    }
}
