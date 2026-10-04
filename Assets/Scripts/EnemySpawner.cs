using System.Collections;
using UnityEngine;

/// <summary>
/// Runs the wave system: each wave spawns more, faster and more varied enemies.
/// Place this object near the top of the scene (spawn height is derived from the camera).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] Enemy enemyPrefab;
    [SerializeField] PowerUp powerUpPrefab;

    [Header("Wave Settings")]
    [SerializeField] int baseEnemiesPerWave = 6;
    [SerializeField] int enemiesAddedPerWave = 3;
    [SerializeField] float startSpawnInterval = 1.3f;
    [SerializeField] float minSpawnInterval = 0.35f;
    [SerializeField] float intervalDecreasePerWave = 0.08f;
    [SerializeField] float speedIncreasePerWave = 0.07f;
    [SerializeField] float maxSpeedMultiplier = 2.5f;
    [SerializeField] float delayBeforeWave = 2.2f;
    [SerializeField] float delayAfterWaveCleared = 1.5f;

    [Header("Spawn Area")]
    [SerializeField] float horizontalMargin = 0.7f;
    [SerializeField] float spawnHeightAboveScreen = 1f;

    Coroutine waveRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void StartSpawning()
    {
        StopSpawning();
        waveRoutine = StartCoroutine(WaveRoutine());
    }

    public void StopSpawning()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
    }

    IEnumerator WaveRoutine()
    {
        int wave = 0;
        while (true)
        {
            wave++;
            GameManager.Instance.SetWave(wave);
            yield return new WaitForSeconds(delayBeforeWave);

            int count = baseEnemiesPerWave + (wave - 1) * enemiesAddedPerWave;
            float interval = Mathf.Max(minSpawnInterval, startSpawnInterval - (wave - 1) * intervalDecreasePerWave);
            float speedMultiplier = Mathf.Min(maxSpeedMultiplier, 1f + (wave - 1) * speedIncreasePerWave);

            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(PickType(wave), speedMultiplier, wave);
                yield return new WaitForSeconds(interval);
            }

            while (Enemy.ActiveCount > 0) yield return null;

            GameManager.Instance.WaveCleared(wave);
            yield return new WaitForSeconds(delayAfterWaveCleared);
        }
    }

    EnemyType PickType(int wave)
    {
        float wNormal = 10f;
        float wFast = wave >= 2 ? Mathf.Min(8f, 3f + wave * 0.5f) : 0f;
        float wZig = wave >= 3 ? Mathf.Min(7f, 2f + wave * 0.5f) : 0f;
        float wTank = wave >= 4 ? Mathf.Min(5f, 1f + wave * 0.4f) : 0f;

        float roll = Random.value * (wNormal + wFast + wZig + wTank);
        if (roll < wNormal) return EnemyType.Normal;
        roll -= wNormal;
        if (roll < wFast) return EnemyType.Fast;
        roll -= wFast;
        if (roll < wZig) return EnemyType.ZigZag;
        return EnemyType.Tank;
    }

    void SpawnEnemy(EnemyType type, float speedMultiplier, int wave)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: assign the Enemy prefab in the inspector.");
            return;
        }

        float x = Random.Range(GameUtil.Left + horizontalMargin, GameUtil.Right - horizontalMargin);
        float y = GameUtil.Top + spawnHeightAboveScreen;

        Enemy enemy = Instantiate(enemyPrefab, new Vector3(x, y, 0f), Quaternion.identity);
        enemy.Setup(type, speedMultiplier, type == EnemyType.Tank ? wave / 5 : 0);
    }

    /// <summary>Called when an enemy dies; may spawn a power-up at its position.</summary>
    public void TryDropPowerUp(Vector3 position, float chance)
    {
        if (powerUpPrefab == null || Random.value > chance) return;

        PowerUpType kind;
        float roll = Random.value;
        if (roll < 0.4f) kind = PowerUpType.RapidFire;
        else if (roll < 0.8f) kind = PowerUpType.TripleShot;
        else kind = PowerUpType.Heal;

        PlayerController player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (kind == PowerUpType.Heal && player != null && player.Health >= player.MaxHealth)
        {
            kind = PowerUpType.RapidFire;
        }

        PowerUp powerUp = Instantiate(powerUpPrefab, position, Quaternion.identity);
        powerUp.Setup(kind);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(16f, 0.3f, 0f));
    }
}
