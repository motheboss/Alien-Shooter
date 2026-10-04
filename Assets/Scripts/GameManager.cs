using System;
using UnityEngine;

public enum GameState { Menu, Playing, GameOver }

/// <summary>
/// Central game state: menu / playing / game over, score, wave, high score (PlayerPrefs).
/// Other systems (UI, audio) react to the events exposed here.
/// </summary>
public class GameManager : MonoBehaviour
{
    public const string HighScoreKey = "AlienShooter.HighScore";

    public static GameManager Instance { get; private set; }
    public static bool IsPlaying { get { return Instance != null && Instance.State == GameState.Playing; } }

    [Header("References (auto-found if empty)")]
    [SerializeField] PlayerController player;
    [SerializeField] EnemySpawner spawner;

    [Header("Rules")]
    [Tooltip("If true, one enemy reaching the bottom ends the game. If false it costs one hull point.")]
    [SerializeField] bool instantGameOverOnBreach = true;
    [SerializeField] int waveClearBonusPerWave = 50;
    [SerializeField] float gameOverInputDelay = 0.8f;

    public GameState State { get; private set; }
    public int Score { get; private set; }
    public int Wave { get; private set; }
    public int HighScore { get; private set; }
    public bool IsNewHighScore { get; private set; }
    public string GameOverReason { get; private set; }
    public PlayerController Player { get { return player; } }

    public event Action<GameState> StateChanged;
    public event Action<int> ScoreChanged;
    public event Action<int> WaveChanged;
    public event Action<int, int> HealthChanged;
    public event Action<string> MessageRaised;

    int highScoreAtStart;
    float stateEnteredTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        State = GameState.Menu;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        GameOverReason = "";

        if (player == null) player = GameUtil.Find<PlayerController>();
        if (spawner == null) spawner = GameUtil.Find<EnemySpawner>();
    }

    void Start()
    {
        stateEnteredTime = Time.unscaledTime;
        if (player != null) player.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        switch (State)
        {
            case GameState.Menu:
                if (GameInput.SubmitDown) StartGame();
                break;

            case GameState.GameOver:
                if (Time.unscaledTime - stateEnteredTime < gameOverInputDelay) break;
                if (GameInput.SubmitDown) StartGame();
                else if (GameInput.CancelDown) ReturnToMenu();
                break;
        }
    }

    void SetState(GameState newState)
    {
        State = newState;
        stateEnteredTime = Time.unscaledTime;
        if (StateChanged != null) StateChanged(newState);
    }

    void ClearField()
    {
        Enemy.DestroyAll();
        Bullet.DestroyAll();
        PowerUp.DestroyAll();
    }

    public void StartGame()
    {
        if (State == GameState.Playing) return;

        ClearField();
        Score = 0;
        Wave = 0;
        IsNewHighScore = false;
        GameOverReason = "";
        highScoreAtStart = HighScore;

        if (player != null)
        {
            player.gameObject.SetActive(true);
            player.ResetPlayer();
        }

        SetState(GameState.Playing);
        if (ScoreChanged != null) ScoreChanged(Score);
        if (WaveChanged != null) WaveChanged(Wave);

        AudioManager.Play(Sfx.UiClick);
        if (spawner != null) spawner.StartSpawning();
    }

    public void ReturnToMenu()
    {
        if (spawner != null) spawner.StopSpawning();
        ClearField();
        if (player != null) player.gameObject.SetActive(false);
        AudioManager.Play(Sfx.UiClick);
        SetState(GameState.Menu);
    }

    void EndGame(string reason)
    {
        if (State != GameState.Playing) return;

        if (spawner != null) spawner.StopSpawning();
        GameOverReason = reason;

        IsNewHighScore = Score > highScoreAtStart && Score > 0;
        if (Score > HighScore) HighScore = Score;
        PlayerPrefs.SetInt(HighScoreKey, HighScore);
        PlayerPrefs.Save();

        if (player != null)
        {
            ParticleEffects.Explosion(player.transform.position, new Color(0.3f, 0.8f, 1f), 2.5f);
            player.gameObject.SetActive(false);
        }

        CameraShake.Shake(0.5f, 0.5f);
        AudioManager.Play(Sfx.GameOver);
        SetState(GameState.GameOver);
    }

    public void PlayerDied()
    {
        EndGame("Your ship was destroyed!");
    }

    public void EnemyReachedBottom(Enemy enemy)
    {
        if (!IsPlaying) return;

        ParticleEffects.Explosion(enemy.transform.position, new Color(1f, 0.2f, 0.2f), 1.5f);
        CameraShake.Shake(0.35f, 0.3f);

        if (instantGameOverOnBreach)
        {
            EndGame("An alien broke through!");
        }
        else if (player != null)
        {
            player.TakeDamage(1);
        }
    }

    public void AddScore(int amount)
    {
        if (!IsPlaying) return;

        Score += amount;
        if (Score > HighScore) HighScore = Score;
        if (ScoreChanged != null) ScoreChanged(Score);
    }

    public void SetWave(int wave)
    {
        Wave = wave;
        AudioManager.Play(Sfx.WaveStart);
        if (WaveChanged != null) WaveChanged(Wave);
    }

    public void WaveCleared(int wave)
    {
        int bonus = wave * waveClearBonusPerWave;
        AddScore(bonus);
        RaiseMessage("WAVE " + wave + " CLEARED  +" + bonus);
    }

    public void NotifyPlayerHealth(int current, int max)
    {
        if (HealthChanged != null) HealthChanged(current, max);
    }

    public void RaiseMessage(string message)
    {
        if (MessageRaised != null) MessageRaised(message);
    }
}
