using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EcoShark
{
    /// <summary>
    /// Lectura de controles en UN solo lugar.
    /// Funciona con el Input System nuevo (Unity 6 lo trae por defecto) y con el antiguo.
    /// Si mañana cambias los controles, solo tocas este archivo.
    /// </summary>
    public static class EcoInput
    {
        /// <summary>Dirección de teclado / mando. Magnitud máxima 1.</summary>
        public static Vector2 MoveAxis()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
            }
            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > v.sqrMagnitude) v = stick;
            }
#else
            v.x = Input.GetAxisRaw("Horizontal");
            v.y = Input.GetAxisRaw("Vertical");
#endif
            return Vector2.ClampMagnitude(v, 1f);
        }

        /// <summary>Impulso (Shift / Espacio / botón A del mando).</summary>
        public static bool BoostHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            bool key = kb != null && (kb.leftShiftKey.isPressed || kb.spaceKey.isPressed);
            bool button = pad != null && (pad.buttonSouth.isPressed || pad.rightTrigger.ReadValue() > 0.5f);
            return key || button;
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Space);
#endif
        }

        /// <summary>True mientras se mantiene presionado el clic o el dedo (control tipo Hungry Shark).</summary>
        public static bool PointerHeld(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed)
            {
                screenPosition = pointer.position.ReadValue();
                return true;
            }
#else
            if (Input.GetMouseButton(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }
#endif
            screenPosition = Vector2.zero;
            return false;
        }

        public static bool PausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            return (kb != null && (kb.pKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                || (pad != null && pad.startButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public static bool RestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            return (kb != null && (kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return);
#endif
        }
    }
}
