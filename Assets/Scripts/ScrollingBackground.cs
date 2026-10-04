using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parallax starfield + drifting nebula blobs, all generated at runtime.
/// Scroll speed increases slightly with the wave number.
/// </summary>
public class ScrollingBackground : MonoBehaviour
{
    class Star
    {
        public Transform transform;
        public float speed;
        public float margin;
    }

    struct Layer
    {
        public ShapeType shape;
        public int count;
        public float minSize, maxSize;
        public float speed;
        public float alpha;
        public int sortingOrder;
        public float stretch;
    }

    [SerializeField] Color backgroundColor = new Color(0.02f, 0.02f, 0.08f);
    [SerializeField] float waveSpeedBoost = 0.04f;

    readonly List<Star> stars = new List<Star>();

    void Start()
    {
        Camera cam = GameUtil.Cam;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
        }

        Layer[] layers = new Layer[]
        {
            new Layer { shape = ShapeType.Circle, count = 4,  minSize = 6f,    maxSize = 10f,   speed = 0.25f, alpha = 0.08f, sortingOrder = -20, stretch = 1f },
            new Layer { shape = ShapeType.Square, count = 40, minSize = 0.04f, maxSize = 0.07f, speed = 0.4f,  alpha = 0.5f,  sortingOrder = -15, stretch = 1f },
            new Layer { shape = ShapeType.Square, count = 30, minSize = 0.07f, maxSize = 0.1f,  speed = 1.2f,  alpha = 0.7f,  sortingOrder = -14, stretch = 2f },
            new Layer { shape = ShapeType.Square, count = 18, minSize = 0.1f,  maxSize = 0.14f, speed = 3f,    alpha = 0.9f,  sortingOrder = -13, stretch = 4f },
        };

        for (int i = 0; i < layers.Length; i++) BuildLayer(layers[i]);
    }

    void BuildLayer(Layer layer)
    {
        Sprite sprite = SpriteFactory.Get(layer.shape);
        bool nebula = layer.maxSize > 1f;

        for (int i = 0; i < layer.count; i++)
        {
            GameObject go = new GameObject(nebula ? "Nebula" : "Star");
            go.transform.SetParent(transform, false);

            float size = Random.Range(layer.minSize, layer.maxSize);
            go.transform.localScale = new Vector3(size, size * layer.stretch, 1f);
            go.transform.position = new Vector3(
                Random.Range(GameUtil.Left, GameUtil.Right),
                Random.Range(GameUtil.Bottom, GameUtil.Top),
                0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = layer.sortingOrder;

            Color c = nebula
                ? Color.HSVToRGB(Random.Range(0.55f, 0.85f), 0.7f, 0.9f)
                : Color.white;
            c.a = layer.alpha;
            sr.color = c;

            Star star = new Star();
            star.transform = go.transform;
            star.speed = layer.speed * Random.Range(0.8f, 1.2f);
            star.margin = size * layer.stretch * 0.5f + 0.5f;
            stars.Add(star);
        }
    }

    void Update()
    {
        int wave = GameManager.Instance != null ? GameManager.Instance.Wave : 0;
        float boost = 1f + waveSpeedBoost * wave;
        float dt = Time.deltaTime;

        for (int i = 0; i < stars.Count; i++)
        {
            Star s = stars[i];
            Vector3 p = s.transform.position;
            p.y -= s.speed * boost * dt;

            if (p.y < GameUtil.Bottom - s.margin)
            {
                p.y = GameUtil.Top + s.margin;
                p.x = Random.Range(GameUtil.Left, GameUtil.Right);
            }
            s.transform.position = p;
        }
    }
}
