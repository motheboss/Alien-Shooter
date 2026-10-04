using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Input wrapper that works with the old Input Manager, the new Input System package,
/// or both. No Input Manager axes or Input Actions need to be configured.
/// </summary>
public static class GameInput
{
    /// <summary>-1 (left), 0, or +1 (right). A/D or arrow keys.</summary>
    public static float Horizontal
    {
        get
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            float h = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) h -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) h += 1f;
            return h;
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return 0f;
            float h = 0f;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) h -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) h += 1f;
            return h;
#else
            return 0f;
#endif
        }
    }

    /// <summary>True while Space or the left mouse button is held.</summary>
    public static bool Fire
    {
        get
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            return (kb != null && kb.spaceKey.isPressed) || (mouse != null && mouse.leftButton.isPressed);
#else
            return false;
#endif
        }
    }

    /// <summary>True on the frame Enter or Space is pressed (start / restart).</summary>
    public static bool SubmitDown
    {
        get
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame);
#else
            return false;
#endif
        }
    }

    /// <summary>True on the frame Escape is pressed.</summary>
    public static bool CancelDown
    {
        get
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return false;
#endif
        }
    }
}
