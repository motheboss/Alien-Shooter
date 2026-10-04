using UnityEngine;

/// <summary>
/// Code-built particle bursts (no prefabs or assets needed): enemy explosions,
/// bullet impacts, and pick-up sparkles.
/// </summary>
public static class ParticleEffects
{
    static Material particleMaterial;

    static Material Mat
    {
        get
        {
            if (particleMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                particleMaterial = new Material(shader);
                particleMaterial.mainTexture = SpriteFactory.Get(ShapeType.Circle).texture;
                particleMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            return particleMaterial;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        particleMaterial = null;
    }

    public static void Explosion(Vector3 position, Color color, float scale)
    {
        Burst(position, color, Mathf.RoundToInt(24f * scale), 5f * Mathf.Sqrt(scale), 0.2f * scale, 0.6f);
        Burst(position, Color.white, Mathf.RoundToInt(8f * scale), 3f, 0.14f * scale, 0.3f);
    }

    public static void Impact(Vector3 position, Color color)
    {
        Burst(position, color, 6, 3f, 0.1f, 0.25f);
    }

    public static void PickUp(Vector3 position, Color color)
    {
        Burst(position, color, 16, 4f, 0.14f, 0.5f);
    }

    static void Burst(Vector3 position, Color color, int count, float speed, float size, float lifetime)
    {
        GameObject go = new GameObject("FX");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count + 10;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystemRenderer psRenderer = go.GetComponent<ParticleSystemRenderer>();
        psRenderer.sharedMaterial = Mat;
        psRenderer.sortingOrder = 30;

        ps.Play();
        Object.Destroy(go, lifetime + 1f);
    }
}
