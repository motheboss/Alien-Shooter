using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Builds and drives the whole UI at runtime (no manual Canvas setup needed):
/// HUD (score, best, wave, hull pips, power-up timers), wave banner, toasts,
/// start menu and game-over screen with score and high score.
/// </summary>
public class UIManager : MonoBehaviour
{
    [SerializeField] int referenceWidth = 1920;
    [SerializeField] int referenceHeight = 1080;

    GameManager gm;
    Font font;
    RectTransform canvasRect;

    GameObject hudPanel;
    GameObject menuPanel;
    GameObject gameOverPanel;

    Text scoreText;
    Text bestText;
    Text waveText;
    Text powerUpText;
    Text bannerText;
    Text toastText;
    Text menuBestText;
    Text gameOverScoreText;
    Text gameOverWaveText;
    Text gameOverBestText;
    Text gameOverReasonText;
    Text newRecordText;

    RectTransform healthContainer;
    Image[] healthPips = new Image[0];

    Coroutine bannerRoutine;
    Coroutine toastRoutine;
    string lastPowerUpString = "";

    static readonly Color Cyan = new Color(0.4f, 0.9f, 1f);
    static readonly Color Yellow = new Color(1f, 0.9f, 0.3f);

    void Start()
    {
        gm = GameManager.Instance;
        font = LoadFont();

        EnsureEventSystem();
        BuildCanvas();
        BuildHud();
        BuildMenu();
        BuildGameOver();

        if (gm == null)
        {
            Debug.LogError("UIManager: no GameManager found in the scene.");
            return;
        }

        gm.StateChanged += OnStateChanged;
        gm.ScoreChanged += OnScoreChanged;
        gm.WaveChanged += OnWaveChanged;
        gm.HealthChanged += OnHealthChanged;
        gm.MessageRaised += OnMessage;

        OnStateChanged(gm.State);
        OnScoreChanged(gm.Score);
        waveText.text = "WAVE " + Mathf.Max(1, gm.Wave);
        if (gm.Player != null) OnHealthChanged(gm.Player.Health, gm.Player.MaxHealth);
    }

    void OnDestroy()
    {
        if (gm == null) return;
        gm.StateChanged -= OnStateChanged;
        gm.ScoreChanged -= OnScoreChanged;
        gm.WaveChanged -= OnWaveChanged;
        gm.HealthChanged -= OnHealthChanged;
        gm.MessageRaised -= OnMessage;
    }

    void Update()
    {
        if (gm == null || powerUpText == null) return;

        string s = "";
        PlayerController p = gm.Player;
        if (gm.State == GameState.Playing && p != null)
        {
            if (p.RapidFireTimeLeft > 0f) s += "RAPID FIRE " + Mathf.CeilToInt(p.RapidFireTimeLeft) + "s\n";
            if (p.TripleShotTimeLeft > 0f) s += "TRIPLE SHOT " + Mathf.CeilToInt(p.TripleShotTimeLeft) + "s";
        }

        if (s != lastPowerUpString)
        {
            lastPowerUpString = s;
            powerUpText.text = s;
        }
    }

    // ------------------------------------------------------------------ event handlers

    void OnStateChanged(GameState state)
    {
        bool inMenu = state == GameState.Menu;
        menuPanel.SetActive(inMenu);
        hudPanel.SetActive(!inMenu);
        gameOverPanel.SetActive(state == GameState.GameOver);

        HideTransientText();

        if (inMenu)
        {
            menuBestText.text = "HIGH SCORE: " + gm.HighScore;
        }
        else if (state == GameState.GameOver)
        {
            gameOverScoreText.text = "SCORE: " + gm.Score;
            gameOverWaveText.text = "WAVE REACHED: " + Mathf.Max(1, gm.Wave);
            gameOverBestText.text = "BEST: " + gm.HighScore;
            gameOverReasonText.text = gm.GameOverReason;
            newRecordText.gameObject.SetActive(gm.IsNewHighScore);
        }
    }

    void OnScoreChanged(int score)
    {
        scoreText.text = "SCORE " + score;
        bestText.text = "BEST " + gm.HighScore;
    }

    void OnWaveChanged(int wave)
    {
        waveText.text = "WAVE " + Mathf.Max(1, wave);
        if (wave > 0)
        {
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);
            bannerRoutine = StartCoroutine(FadeText(bannerText, "WAVE " + wave, 1.4f, 0.6f));
        }
    }

    void OnHealthChanged(int current, int max)
    {
        if (healthPips.Length != max) RebuildHealthPips(max);

        for (int i = 0; i < healthPips.Length; i++)
        {
            healthPips[i].color = i < current
                ? new Color(1f, 0.25f, 0.3f)
                : new Color(0.25f, 0.25f, 0.3f, 0.8f);
        }
    }

    void OnMessage(string message)
    {
        if (gm.State != GameState.Playing) return;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(FadeText(toastText, message, 1.0f, 0.5f));
    }

    void HideTransientText()
    {
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        bannerRoutine = null;
        toastRoutine = null;
        if (bannerText != null) bannerText.gameObject.SetActive(false);
        if (toastText != null) toastText.gameObject.SetActive(false);
    }

    IEnumerator FadeText(Text text, string content, float hold, float fade)
    {
        text.text = content;
        SetAlpha(text, 1f);
        text.gameObject.SetActive(true);

        yield return new WaitForSeconds(hold);

        float elapsed = 0f;
        while (elapsed < fade)
        {
            elapsed += Time.deltaTime;
            SetAlpha(text, 1f - Mathf.Clamp01(elapsed / fade));
            yield return null;
        }
        text.gameObject.SetActive(false);
    }

    static void SetAlpha(Text text, float alpha)
    {
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }

    // ------------------------------------------------------------------ building the UI

    static Font LoadFont()
    {
        Font f = null;
#if UNITY_2022_2_OR_NEWER
        f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        f = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        if (f == null)
        {
            f = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Liberation Sans", "DejaVu Sans" }, 32);
        }
        return f;
    }

    void EnsureEventSystem()
    {
        if (GameUtil.Find<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    void BuildCanvas()
    {
        GameObject go = new GameObject("Canvas");
        go.transform.SetParent(transform, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        canvasRect = go.GetComponent<RectTransform>();
    }

    RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    Text MakeText(Transform parent, string name, string content, int size, TextAnchor align, Color color,
                  Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 rectSize)
    {
        RectTransform rt = CreateRect(name, parent);
        Place(rt, anchor, pivot, position, rectSize);

        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow shadow = rt.gameObject.AddComponent<Shadow>();
        shadow.effectDistance = new Vector2(2f, -2f);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        return t;
    }

    Text MakeCenterText(Transform parent, string name, string content, int size, Color color, float y)
    {
        Vector2 c = new Vector2(0.5f, 0.5f);
        return MakeText(parent, name, content, size, TextAnchor.MiddleCenter, color, c, c, new Vector2(0f, y), new Vector2(1600f, size * 1.4f));
    }

    Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color,
                      UnityEngine.Events.UnityAction onClick)
    {
        Vector2 c = new Vector2(0.5f, 0.5f);
        RectTransform rt = CreateRect(name, parent);
        Place(rt, c, c, position, size);

        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        Text t = MakeText(rt, "Label", label, 40, TextAnchor.MiddleCenter, Color.white,
                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Stretch(t.rectTransform);
        return button;
    }

    GameObject MakePanel(string name, Color overlay)
    {
        RectTransform rt = CreateRect(name, canvasRect);
        Stretch(rt);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = overlay;
        return rt.gameObject;
    }

    void BuildHud()
    {
        RectTransform root = CreateRect("HUD", canvasRect);
        Stretch(root);
        hudPanel = root.gameObject;

        Vector2 topLeft = new Vector2(0f, 1f);
        Vector2 topRight = new Vector2(1f, 1f);
        Vector2 bottomLeft = new Vector2(0f, 0f);
        Vector2 bottomRight = new Vector2(1f, 0f);
        Vector2 topCenter = new Vector2(0.5f, 1f);
        Vector2 center = new Vector2(0.5f, 0.5f);

        scoreText = MakeText(root, "Score", "SCORE 0", 44, TextAnchor.UpperLeft, Color.white,
                             topLeft, topLeft, new Vector2(30f, -20f), new Vector2(700f, 60f));
        bestText = MakeText(root, "Best", "BEST 0", 28, TextAnchor.UpperLeft, Yellow,
                            topLeft, topLeft, new Vector2(30f, -78f), new Vector2(700f, 40f));
        waveText = MakeText(root, "Wave", "WAVE 1", 44, TextAnchor.UpperRight, Cyan,
                            topRight, topRight, new Vector2(-30f, -20f), new Vector2(700f, 60f));

        MakeText(root, "HullLabel", "HULL", 26, TextAnchor.LowerLeft, Color.white,
                 bottomLeft, bottomLeft, new Vector2(30f, 70f), new Vector2(300f, 36f));

        RectTransform hc = CreateRect("HealthPips", root);
        Place(hc, bottomLeft, bottomLeft, new Vector2(30f, 30f), new Vector2(400f, 34f));
        healthContainer = hc;

        powerUpText = MakeText(root, "PowerUps", "", 28, TextAnchor.LowerRight, Yellow,
                               bottomRight, bottomRight, new Vector2(-30f, 30f), new Vector2(600f, 90f));

        bannerText = MakeText(root, "WaveBanner", "WAVE 1", 110, TextAnchor.MiddleCenter, Yellow,
                              center, center, new Vector2(0f, 150f), new Vector2(1400f, 150f));
        bannerText.gameObject.SetActive(false);

        toastText = MakeText(root, "Toast", "", 40, TextAnchor.MiddleCenter, Color.white,
                             topCenter, topCenter, new Vector2(0f, -130f), new Vector2(1200f, 60f));
        toastText.gameObject.SetActive(false);
    }

    void RebuildHealthPips(int max)
    {
        for (int i = 0; i < healthPips.Length; i++)
        {
            if (healthPips[i] != null) Destroy(healthPips[i].gameObject);
        }

        healthPips = new Image[max];
        Vector2 bl = new Vector2(0f, 0f);
        for (int i = 0; i < max; i++)
        {
            RectTransform rt = CreateRect("Pip" + i, healthContainer);
            Place(rt, bl, bl, new Vector2(i * 52f, 0f), new Vector2(44f, 30f));
            healthPips[i] = rt.gameObject.AddComponent<Image>();
        }
    }

    void BuildMenu()
    {
        menuPanel = MakePanel("MenuPanel", new Color(0f, 0f, 0f, 0.55f));
        Transform p = menuPanel.transform;

        MakeCenterText(p, "Title", "ALIEN SHOOTER", 120, new Color(0.45f, 1f, 0.45f), 250f);
        MakeCenterText(p, "Subtitle", "Defend Earth from the invasion", 40, Color.white, 150f);
        menuBestText = MakeCenterText(p, "MenuBest", "HIGH SCORE: 0", 50, Yellow, 60f);

        MakeButton(p, "PlayButton", "PLAY", new Vector2(0f, -60f), new Vector2(420f, 100f),
                   new Color(0.2f, 0.6f, 0.25f), delegate { gm.StartGame(); });

#if !UNITY_WEBGL
        MakeButton(p, "QuitButton", "QUIT", new Vector2(0f, -190f), new Vector2(420f, 90f),
                   new Color(0.5f, 0.2f, 0.2f), OnQuitClicked);
#endif

        MakeCenterText(p, "Controls", "A / D or LEFT / RIGHT to move   -   SPACE to shoot   -   ENTER to start",
                       30, new Color(0.8f, 0.8f, 0.9f), -330f);
    }

    void BuildGameOver()
    {
        gameOverPanel = MakePanel("GameOverPanel", new Color(0.12f, 0f, 0f, 0.65f));
        Transform p = gameOverPanel.transform;

        MakeCenterText(p, "GameOverTitle", "GAME OVER", 130, new Color(1f, 0.3f, 0.3f), 250f);
        gameOverReasonText = MakeCenterText(p, "Reason", "", 40, Color.white, 150f);
        gameOverScoreText = MakeCenterText(p, "FinalScore", "SCORE: 0", 70, Color.white, 70f);
        gameOverWaveText = MakeCenterText(p, "FinalWave", "WAVE REACHED: 1", 36, Cyan, 0f);
        gameOverBestText = MakeCenterText(p, "FinalBest", "BEST: 0", 50, Yellow, -60f);
        newRecordText = MakeCenterText(p, "NewRecord", "NEW HIGH SCORE!", 60, Yellow, -130f);

        MakeButton(p, "RestartButton", "RESTART", new Vector2(-210f, -260f), new Vector2(360f, 90f),
                   new Color(0.2f, 0.6f, 0.25f), delegate { gm.StartGame(); });
        MakeButton(p, "MenuButton", "MENU", new Vector2(210f, -260f), new Vector2(360f, 90f),
                   new Color(0.25f, 0.3f, 0.55f), delegate { gm.ReturnToMenu(); });

        MakeCenterText(p, "Hint", "Press ENTER to restart   -   ESC for menu", 30, new Color(0.85f, 0.85f, 0.9f), -380f);
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
