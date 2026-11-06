using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Input wrapper that works with the old Input Manager, the new Input System, or both
/// (whichever "Active Input Handling" is set to in Player Settings).
/// </summary>
public static class GameInput
{
    public static bool FlapDown()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        var m = Mouse.current;
        var t = Touchscreen.current;
        return (k != null && (k.spaceKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame))
            || (m != null && m.leftButton.wasPressedThisFrame)
            || (t != null && t.primaryTouch.press.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)
            || Input.GetMouseButtonDown(0)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
#else
        return false;
#endif
    }

    public static bool CrouchHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.downArrowKey.isPressed || k.sKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
#else
        return false;
#endif
    }

    public static bool RestartDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }

    public static bool PauseDown()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.pKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    public static bool MuteDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.M);
#else
        return false;
#endif
    }
}
