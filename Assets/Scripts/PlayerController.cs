using UnityEngine;

/// <summary>
/// Player ship: left/right movement (input in Update, physics in FixedUpdate), shooting,
/// health with brief invulnerability, and power-up timers.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 9f;
    [SerializeField] float edgePadding = 0.6f;
    [SerializeField] float startHeightFromBottom = 1.2f;

    [Header("Shooting")]
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] float fireCooldown = 0.22f;

    [Header("Health")]
    [SerializeField] int maxHealth = 3;
    [SerializeField] float invulnerabilitySeconds = 1.5f;

    [Header("Power-ups")]
    [SerializeField] float powerUpDuration = 8f;
    [SerializeField] float rapidFireCooldownMultiplier = 0.45f;

    [Header("Look")]
    [SerializeField] Color bodyColor = new Color(0.3f, 0.8f, 1f);

    Rigidbody2D rb;
    SpriteRenderer sr;

    float horizontalInput;
    float nextFireTime;
    float invulnUntil;
    float rapidTimer;
    float tripleTimer;

    public int Health { get; private set; }
    public int MaxHealth { get { return maxHealth; } }
    public float RapidFireTimeLeft { get { return rapidTimer; } }
    public float TripleShotTimeLeft { get { return tripleTimer; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (sr.sprite == null) sr.sprite = SpriteFactory.Get(ShapeType.Triangle);
            sr.color = bodyColor;
            sr.sortingOrder = 10;
        }

        Health = maxHealth;
    }

    /// <summary>Restores health, clears power-ups and moves the ship to the start position.</summary>
    public void ResetPlayer()
    {
        Health = maxHealth;
        invulnUntil = 0f;
        rapidTimer = 0f;
        tripleTimer = 0f;
        nextFireTime = 0f;
        horizontalInput = 0f;

        Vector2 start = new Vector2(GameUtil.CenterX, GameUtil.Bottom + startHeightFromBottom);
        transform.position = start;
        if (rb != null) rb.position = start;
        if (sr != null) sr.color = bodyColor;

        if (GameManager.Instance != null) GameManager.Instance.NotifyPlayerHealth(Health, maxHealth);
    }

    void Update()
    {
        if (!GameManager.IsPlaying) return;

        horizontalInput = GameInput.Horizontal;

        if (rapidTimer > 0f) rapidTimer -= Time.deltaTime;
        if (tripleTimer > 0f) tripleTimer -= Time.deltaTime;

        UpdateInvulnerabilityVisual();

        if (GameInput.Fire && Time.time >= nextFireTime)
        {
            Shoot();
            float cooldown = rapidTimer > 0f ? fireCooldown * rapidFireCooldownMultiplier : fireCooldown;
            nextFireTime = Time.time + cooldown;
        }
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying) return;

        Vector2 pos = rb.position;
        pos.x += horizontalInput * moveSpeed * Time.fixedDeltaTime;
        pos.x = Mathf.Clamp(pos.x, GameUtil.Left + edgePadding, GameUtil.Right - edgePadding);
        rb.MovePosition(pos);
    }

    void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("PlayerController: assign the Bullet prefab in the inspector.");
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * 0.6f;

        if (tripleTimer > 0f)
        {
            SpawnBullet(origin, -14f);
            SpawnBullet(origin, 0f);
            SpawnBullet(origin, 14f);
        }
        else
        {
            SpawnBullet(origin, 0f);
        }

        AudioManager.Play(Sfx.Shoot, 0.5f, 0.08f);
    }

    void SpawnBullet(Vector3 origin, float angle)
    {
        Bullet bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
        Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
        bullet.Init(new Vector2(dir.x, dir.y));
    }

    void UpdateInvulnerabilityVisual()
    {
        if (sr == null) return;
        Color c = bodyColor;
        if (Time.time < invulnUntil && Mathf.Sin(Time.time * 40f) > 0f) c.a = 0.3f;
        sr.color = c;
    }

    public void TakeDamage(int amount)
    {
        if (!GameManager.IsPlaying || Time.time < invulnUntil) return;

        Health = Mathf.Max(0, Health - amount);
        invulnUntil = Time.time + invulnerabilitySeconds;

        ParticleEffects.Explosion(transform.position, bodyColor, 1.2f);
        AudioManager.Play(Sfx.PlayerHit);
        CameraShake.Shake(0.3f, 0.25f);
        GameManager.Instance.NotifyPlayerHealth(Health, maxHealth);

        if (Health <= 0) GameManager.Instance.PlayerDied();
    }

    public void ApplyPowerUp(PowerUpType kind)
    {
        switch (kind)
        {
            case PowerUpType.RapidFire:
                rapidTimer = powerUpDuration;
                GameManager.Instance.RaiseMessage("RAPID FIRE!");
                break;
            case PowerUpType.TripleShot:
                tripleTimer = powerUpDuration;
                GameManager.Instance.RaiseMessage("TRIPLE SHOT!");
                break;
            case PowerUpType.Heal:
                Health = Mathf.Min(maxHealth, Health + 1);
                GameManager.Instance.NotifyPlayerHealth(Health, maxHealth);
                GameManager.Instance.RaiseMessage("+1 HULL");
                break;
        }
        AudioManager.Play(Sfx.PowerUp);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!GameManager.IsPlaying) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Kill(false);
            TakeDamage(1);
            return;
        }

        PowerUp powerUp = other.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            ApplyPowerUp(powerUp.Kind);
            powerUp.Collect();
        }
    }
}
