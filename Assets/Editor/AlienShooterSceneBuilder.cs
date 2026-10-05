using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


public static class AlienShooterSceneBuilder
{
    const string SpriteDir = "Assets/Sprites";
    const string PrefabDir = "Assets/Prefabs";
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = "Assets/Scenes/AlienShooter.unity";

    [MenuItem("Tools/Alien Shooter/Build Game Scene")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder("Assets", "Sprites");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets", "Scenes");

        Sprite square = CreateSpriteAsset("Square", ShapeType.Square);
        Sprite circle = CreateSpriteAsset("Circle", ShapeType.Circle);
        Sprite triangle = CreateSpriteAsset("Triangle", ShapeType.Triangle);

        Bullet bulletPrefab = CreateBulletPrefab(square);
        Enemy enemyPrefab = CreateEnemyPrefab(circle);
        PowerUp powerUpPrefab = CreatePowerUpPrefab(circle);
        PlayerController playerPrefab = CreatePlayerPrefab(triangle, bulletPrefab);

        // FIX: Pass GameObject prefabs instead of component references
        BuildScene(
            playerPrefab.gameObject,
            enemyPrefab.gameObject,
            powerUpPrefab.gameObject
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Alien Shooter scene built at " + ScenePath + ". Press Play!");
    }

    // ------------------------------------------------------------------ helpers

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static Sprite CreateSpriteAsset(string name, ShapeType shape)
    {
        string path = SpriteDir + "/" + name + ".png";

        Texture2D tex = SpriteFactory.CreateTexture(shape);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = SpriteFactory.PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void SetRef(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("Field not found: " + field + " on " + target.name);
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static T SavePrefab<T>(GameObject go, string name) where T : Component
    {
        string path = PrefabDir + "/" + name + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved.GetComponent<T>();
    }

    // ------------------------------------------------------------------ prefabs

    static Bullet CreateBulletPrefab(Sprite square)
    {
        GameObject go = new GameObject("Bullet");
        go.transform.localScale = new Vector3(0.14f, 0.45f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(1f, 0.95f, 0.4f);
        sr.sortingOrder = 5;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        col.isTrigger = true;

        go.AddComponent<Bullet>();
        return SavePrefab<Bullet>(go, "Bullet");
    }

    static Enemy CreateEnemyPrefab(Sprite circle)
    {
        GameObject go = new GameObject("Enemy");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = new Color(0.45f, 1f, 0.45f);
        sr.sortingOrder = 10;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;

        go.AddComponent<Enemy>();
        return SavePrefab<Enemy>(go, "Enemy");
    }

    static PowerUp CreatePowerUpPrefab(Sprite circle)
    {
        GameObject go = new GameObject("PowerUp");
        go.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = new Color(1f, 0.9f, 0.2f);
        sr.sortingOrder = 8;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.6f;
        col.isTrigger = true;

        go.AddComponent<PowerUp>();
        return SavePrefab<PowerUp>(go, "PowerUp");
    }

    static PlayerController CreatePlayerPrefab(Sprite triangle, Bullet bulletPrefab)
    {
        GameObject go = new GameObject("Player");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = triangle;
        sr.color = new Color(0.3f, 0.8f, 1f);
        sr.sortingOrder = 10;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);
        col.isTrigger = true;

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(go.transform, false);
        firePoint.transform.localPosition = new Vector3(0f, 0.6f, 0f);

        PlayerController pc = go.AddComponent<PlayerController>();
        SetRef(pc, "bulletPrefab", bulletPrefab);
        SetRef(pc, "firePoint", firePoint.transform);

        return SavePrefab<PlayerController>(go, "Player");
    }

    // ------------------------------------------------------------------ scene

    // FIXED VERSION — accepts GameObject prefabs
    static void BuildScene(GameObject playerPrefab, GameObject enemyPrefab, GameObject powerUpPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.08f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CameraShake>();

        // Background
        GameObject bg = new GameObject("Background");
        bg.AddComponent<ScrollingBackground>();

        // Player
        GameObject playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        playerGo.transform.position = new Vector3(0f, -3.8f, 0f);
        PlayerController player = playerGo.GetComponent<PlayerController>();

        // Enemy spawner
        GameObject spawnerGo = new GameObject("EnemySpawner");
        spawnerGo.transform.position = new Vector3(0f, 6f, 0f);
        EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();
        SetRef(spawner, "enemyPrefab", enemyPrefab.GetComponent<Enemy>());
        SetRef(spawner, "powerUpPrefab", powerUpPrefab.GetComponent<PowerUp>());

        // Systems: game manager, audio, UI
        GameObject systems = new GameObject("GameSystems");
        GameManager gm = systems.AddComponent<GameManager>();
        systems.AddComponent<AudioManager>();
        systems.AddComponent<UIManager>();
        SetRef(gm, "player", player);
        SetRef(gm, "spawner", spawner);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
    }
}
