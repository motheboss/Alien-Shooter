using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player bullet. Kinematic body moved in FixedUpdate; trigger collider detects enemies.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public static readonly List<Bullet> Active = new List<Bullet>();

    [SerializeField] float speed = 14f;
    [SerializeField] int damage = 1;
    [SerializeField] float maxLifetime = 3f;
    [SerializeField] Color color = new Color(1f, 0.95f, 0.4f);

    Rigidbody2D rb;
    Vector2 direction = Vector2.up;
    bool spent;
    float age;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Active.Clear();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (sr.sprite == null) sr.sprite = SpriteFactory.Get(ShapeType.Square);
            sr.color = color;
            sr.sortingOrder = 5;
        }
    }

    void OnEnable()
    {
        Active.Add(this);
    }

    void OnDisable()
    {
        Active.Remove(this);
    }

    /// <summary>Sets the travel direction and rotates the sprite to match.</summary>
    public void Init(Vector2 dir)
    {
        direction = dir.normalized;
        transform.up = direction;
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying) return;

        float dt = Time.fixedDeltaTime;
        rb.MovePosition(rb.position + direction * speed * dt);

        age += dt;
        Vector3 p = transform.position;
        bool offscreen = p.y > GameUtil.Top + 1f || p.x < GameUtil.Left - 1f || p.x > GameUtil.Right + 1f;
        if (offscreen || age > maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (spent) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        spent = true;
        ParticleEffects.Impact(transform.position, enemy.BodyColor);
        enemy.TakeDamage(damage);
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
