using System.Collections.Generic;
using UnityEngine;

public enum EnemyType { Normal, Fast, Tank, ZigZag }

/// <summary>
/// Falling alien. One prefab serves all four types; Setup() configures stats and looks.
/// Dynamic body (gravity 0) moved in FixedUpdate; trigger collider is used by bullets and the player.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> Active = new List<Enemy>();
    public static int ActiveCount { get { return Active.Count; } }

    [SerializeField] float flashDuration = 0.08f;

    Rigidbody2D rb;
    SpriteRenderer sr;

    EnemyType type = EnemyType.Normal;
    float speed = 2f;
    int health = 1;
    int scoreValue = 10;
    float baseX;
    float age;
    float zigAmplitude;
    float zigFrequency;
    float flashTimer;
    bool dead;
    bool configured;

    public Color BodyColor { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Active.Clear();
    }

    void Awake()
    {
        EnsureComponents();
    }

    void EnsureComponents()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnEnable()
    {
        Active.Add(this);
    }

    void OnDisable()
    {
        Active.Remove(this);
    }

    void Start()
    {
        if (!configured) Setup(EnemyType.Normal, 1f, 0);
    }

    /// <summary>Configures this enemy's stats and visuals. Call right after Instantiate.</summary>
    public void Setup(EnemyType enemyType, float speedMultiplier, int bonusHealth)
    {
        EnsureComponents();
        configured = true;
        type = enemyType;

        ShapeType shape;
        float scale;
        float rotation = 0f;

        switch (type)
        {
            case EnemyType.Fast:
                speed = 4.2f; health = 1; scoreValue = 20;
                BodyColor = new Color(1f, 0.5f, 0.15f);
                scale = 0.75f; shape = ShapeType.Triangle; rotation = 180f;
                break;
            case EnemyType.Tank:
                speed = 1.1f; health = 5 + bonusHealth; scoreValue = 50;
                BodyColor = new Color(0.7f, 0.4f, 1f);
                scale = 1.5f; shape = ShapeType.Square;
                break;
            case EnemyType.ZigZag:
                speed = 2.0f; health = 2; scoreValue = 30;
                BodyColor = new Color(0.3f, 0.9f, 1f);
                scale = 0.9f; shape = ShapeType.Square; rotation = 45f;
                zigAmplitude = 2f; zigFrequency = 2.2f;
                break;
            default:
                speed = 2f; health = 1; scoreValue = 10;
                BodyColor = new Color(0.45f, 1f, 0.45f);
                scale = 0.95f; shape = ShapeType.Circle;
                break;
        }

        speed *= speedMultiplier;
        transform.localScale = new Vector3(scale, scale, 1f);
        transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        baseX = transform.position.x;
        age = 0f;

        if (sr != null)
        {
            sr.sprite = SpriteFactory.Get(shape);
            sr.color = BodyColor;
            sr.sortingOrder = 10;
        }
    }

    void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && sr != null) sr.color = BodyColor;
        }
    }

    void FixedUpdate()
    {
        if (dead || !GameManager.IsPlaying) return;

        float dt = Time.fixedDeltaTime;
        age += dt;

        Vector2 pos = rb.position;
        pos.y -= speed * dt;

        if (type == EnemyType.ZigZag)
        {
            pos.x = baseX + Mathf.Sin(age * zigFrequency) * zigAmplitude;
            pos.x = Mathf.Clamp(pos.x, GameUtil.Left + 0.6f, GameUtil.Right - 0.6f);
        }

        rb.MovePosition(pos);

        if (pos.y <= GameUtil.Bottom + 0.1f)
        {
            dead = true;
            GameManager.Instance.EnemyReachedBottom(this);
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int amount)
    {
        if (dead) return;

        health -= amount;
        if (health <= 0)
        {
            Kill(true);
            return;
        }

        if (sr != null) sr.color = Color.white;
        flashTimer = flashDuration;
        AudioManager.Play(Sfx.EnemyHit, 0.7f, 0.1f);
    }

    /// <summary>Destroys the enemy with effects. awardScore = false when it crashed into the player.</summary>
    public void Kill(bool awardScore)
    {
        if (dead) return;
        dead = true;

        bool isTank = type == EnemyType.Tank;

        if (awardScore)
        {
            GameManager.Instance.AddScore(scoreValue);
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.TryDropPowerUp(transform.position, isTank ? 0.35f : 0.07f);
            }
        }

        ParticleEffects.Explosion(transform.position, BodyColor, isTank ? 1.8f : 1f);
        AudioManager.Play(Sfx.Explosion, isTank ? 1f : 0.7f, 0.12f);
        CameraShake.Shake(isTank ? 0.18f : 0.06f, 0.15f);
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
