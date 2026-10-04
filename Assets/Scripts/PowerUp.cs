using System.Collections.Generic;
using UnityEngine;

public enum PowerUpType { RapidFire, TripleShot, Heal }

/// <summary>
/// Falling pickup dropped by enemies. The player's trigger handles collection.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PowerUp : MonoBehaviour
{
    public static readonly List<PowerUp> Active = new List<PowerUp>();

    [SerializeField] float fallSpeed = 2f;

    Rigidbody2D rb;
    SpriteRenderer sr;

    public PowerUpType Kind { get; private set; }
    public Color BodyColor { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Active.Clear();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        Active.Add(this);
    }

    void OnDisable()
    {
        Active.Remove(this);
    }

    public void Setup(PowerUpType kind)
    {
        Kind = kind;
        switch (kind)
        {
            case PowerUpType.RapidFire: BodyColor = new Color(1f, 0.9f, 0.2f); break;
            case PowerUpType.TripleShot: BodyColor = new Color(1f, 0.5f, 0.9f); break;
            default: BodyColor = new Color(1f, 0.25f, 0.3f); break;
        }

        if (sr != null)
        {
            sr.sprite = SpriteFactory.Get(ShapeType.Circle);
            sr.color = BodyColor;
            sr.sortingOrder = 8;
        }
    }

    void Update()
    {
        float pulse = 0.55f + 0.08f * Mathf.Sin(Time.time * 8f);
        transform.localScale = new Vector3(pulse, pulse, 1f);
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying) return;

        rb.MovePosition(rb.position + Vector2.down * fallSpeed * Time.fixedDeltaTime);
        if (transform.position.y < GameUtil.Bottom - 1f)
        {
            Destroy(gameObject);
        }
    }

    public void Collect()
    {
        ParticleEffects.PickUp(transform.position, BodyColor);
        Destroy(gameObject);
    }

    public static void DestroyAll()
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            if (Active[i] != null) Destroy(Active[i].gameObject);
        }
        Active.Clear();
    }
}
