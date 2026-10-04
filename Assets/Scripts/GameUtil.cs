using UnityEngine;

/// <summary>
/// Small helpers shared by every script: screen bounds taken from the orthographic
/// camera, and a version-safe "find object in scene" call.
/// </summary>
public static class GameUtil
{
    static Camera cachedCamera;

    public static Camera Cam
    {
        get
        {
            if (cachedCamera == null) cachedCamera = Camera.main;
            return cachedCamera;
        }
    }

    public static float HalfHeight { get { return Cam != null ? Cam.orthographicSize : 5f; } }
    public static float HalfWidth { get { return HalfHeight * (Cam != null ? Cam.aspect : 16f / 9f); } }
    public static float CenterX { get { return Cam != null ? Cam.transform.position.x : 0f; } }
    public static float CenterY { get { return Cam != null ? Cam.transform.position.y : 0f; } }

    public static float Left { get { return CenterX - HalfWidth; } }
    public static float Right { get { return CenterX + HalfWidth; } }
    public static float Top { get { return CenterY + HalfHeight; } }
    public static float Bottom { get { return CenterY - HalfHeight; } }

    public static T Find<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
