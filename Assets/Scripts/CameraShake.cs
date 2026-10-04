using UnityEngine;

/// <summary>
/// Screen shake. Add to the Main Camera (it is added automatically on first use if missing).
/// </summary>
public class CameraShake : MonoBehaviour
{
    static CameraShake instance;

    Vector3 basePosition;
    float timer;
    float duration = 0.01f;
    float intensity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
    }

    void Awake()
    {
        instance = this;
        basePosition = transform.position;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Shake(float intensity, float duration)
    {
        if (instance == null)
        {
            Camera cam = GameUtil.Cam;
            if (cam == null) return;
            cam.gameObject.AddComponent<CameraShake>();
        }

        instance.intensity = Mathf.Max(instance.intensity * (instance.timer > 0f ? 1f : 0f), intensity);
        instance.duration = Mathf.Max(0.01f, duration);
        instance.timer = duration;
    }

    void LateUpdate()
    {
        if (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            float falloff = Mathf.Clamp01(timer / duration);
            Vector2 offset = Random.insideUnitCircle * intensity * falloff;
            transform.position = basePosition + new Vector3(offset.x, offset.y, 0f);
        }
        else
        {
            intensity = 0f;
            transform.position = basePosition;
        }
    }
}
