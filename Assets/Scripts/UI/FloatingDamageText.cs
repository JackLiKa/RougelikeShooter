using UnityEngine;

public sealed class FloatingDamageText : MonoBehaviour
{
    private Camera targetCamera;
    private TextMesh textMesh;
    private Vector3 velocity;
    private float lifetime;
    private float totalLifetime;
    private Color startColor;

    public void Configure(Camera camera, Vector3 position, string text, Color color, float scale, Vector3 initialVelocity, float duration)
    {
        targetCamera = camera;
        textMesh = GetOrCreateTextMesh();
        transform.position = position;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        velocity = initialVelocity;
        totalLifetime = Mathf.Max(0.2f, duration);
        lifetime = totalLifetime;
        startColor = color;
        textMesh.text = text;
        textMesh.color = color;
    }

    public bool Tick(float deltaTime)
    {
        lifetime -= deltaTime;
        transform.position += velocity * deltaTime;
        velocity += new Vector3(0f, 1.4f, 0f) * deltaTime;

        if (targetCamera != null)
        {
            Vector3 forward = targetCamera.transform.forward;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        if (textMesh != null)
        {
            float ratio = Mathf.Clamp01(lifetime / totalLifetime);
            Color color = startColor;
            color.a = ratio;
            textMesh.color = color;
        }

        return lifetime > 0f;
    }

    private TextMesh GetOrCreateTextMesh()
    {
        if (textMesh != null)
        {
            return textMesh;
        }

        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
        }

        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 1.15f;
        textMesh.fontSize = 64;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.richText = false;
        MeshRenderer meshRenderer = textMesh.GetComponent<MeshRenderer>();
        meshRenderer.sortingLayerName = "Default";
        meshRenderer.sortingOrder = 5000;
        return textMesh;
    }
}
