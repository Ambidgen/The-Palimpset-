using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Palimpsest
{
    /// <summary>
    /// A single, tiny input surface for the whole game.
    ///
    /// The project is authored as text only, so it cannot ship an InputActions
    /// asset. This router therefore reads the keyboard and mouse directly and
    /// compiles against whichever input backend the host project happens to have
    /// enabled: the legacy Input Manager, or the new Input System package.
    /// </summary>
    public static class InputRouter
    {
        // ------------------------------------------------------------------
        // Movement
        // ------------------------------------------------------------------

        /// <summary>WASD / arrows, in screen space (x = right, y = away from the camera).</summary>
        public static Vector2 Move
        {
            get
            {
                float x = 0f;
                float y = 0f;

#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
                }
#else
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
#endif

                Vector2 move = new Vector2(x, y);
                if (move.sqrMagnitude > 1f) move.Normalize();
                return move;
            }
        }

        // ------------------------------------------------------------------
        // Actions
        // ------------------------------------------------------------------

        /// <summary>True while the player is drawing ink: left mouse button or space.</summary>
        public static bool InscribeHeld
        {
            get
            {
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
                bool mouse = Mouse.current != null && Mouse.current.leftButton.isPressed;
                bool key = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
                return mouse || key;
#else
                return Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
#endif
            }
        }

        /// <summary>Scrape the page (turn every ward into undertext).</summary>
        public static bool ScrapePressed
        {
            get
            {
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
                bool mouse = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
                bool key = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
                return mouse || key;
#else
                return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.E);
#endif
            }
        }

        /// <summary>Illuminate: a radial pulse of clean light.</summary>
        public static bool IlluminatePressed
        {
            get
            {
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null) return false;
                return keyboard.leftShiftKey.wasPressedThisFrame
                    || keyboard.rightShiftKey.wasPressedThisFrame
                    || keyboard.qKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.LeftShift)
                    || Input.GetKeyDown(KeyCode.RightShift)
                    || Input.GetKeyDown(KeyCode.Q);
#endif
            }
        }

        public static bool PausePressed
        {
            get
            {
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }

        /// <summary>Space / Enter, used to leave the title and to confirm.</summary>
        public static bool ConfirmPressed
        {
            get
            {
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null) return false;
                return keyboard.spaceKey.wasPressedThisFrame
                    || keyboard.enterKey.wasPressedThisFrame
                    || keyboard.numpadEnterKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Space)
                    || Input.GetKeyDown(KeyCode.Return)
                    || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
            }
        }

        public static bool RestartPressed
        {
            get
            {
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.R);
#endif
            }
        }

        /// <summary>The cursor in screen pixels (falls back to the screen centre).</summary>
        public static Vector2 PointerScreen
        {
            get
            {
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
                if (Mouse.current == null) return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                return Mouse.current.position.ReadValue();
#else
                return Input.mousePosition;
#endif
            }
        }
    }
}
