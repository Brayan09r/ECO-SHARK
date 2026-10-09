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
        // (Claude) Controles táctiles: TouchControls escribe aquí el joystick virtual y el botón de impulso.
        /// <summary>Dirección del joystick en pantalla (magnitud máxima 1).</summary>
        public static Vector2 VirtualAxis;
        /// <summary>True mientras se mantiene el botón de impulso en pantalla.</summary>
        public static bool VirtualBoost;
        /// <summary>True si el joystick en pantalla está en uso: entonces "mantener el dedo" ya no dirige al tiburón.</summary>
        public static bool VirtualControlsActive;

        /// <summary>True si hay al menos un dedo en la pantalla (para mostrar los controles táctiles). (Claude)</summary>
        public static bool TouchActive()
        {
#if ENABLE_INPUT_SYSTEM
            Touchscreen ts = Touchscreen.current;
            return ts != null && ts.primaryTouch.press.isPressed;
#else
            return Input.touchCount > 0;
#endif
        }

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
            if (VirtualAxis.sqrMagnitude > v.sqrMagnitude) v = VirtualAxis; // (Claude) joystick en pantalla
            return Vector2.ClampMagnitude(v, 1f);
        }

        /// <summary>Impulso (Shift / Espacio / botón A del mando).</summary>
        public static bool BoostHeld()
        {
            if (VirtualBoost) return true; // (Claude) botón de impulso en pantalla
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
            // (Claude) Con el joystick en pantalla, tocar un botón no debe llevar al tiburón hacia ese punto.
            if (VirtualControlsActive)
            {
                screenPosition = Vector2.zero;
                return false;
            }
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

        /// <summary>Confirmar en menús: Enter, Espacio o botón A del mando. (Claude)</summary>
        public static bool ConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        /// <summary>Silenciar / activar el sonido (tecla M). (Claude)</summary>
        public static bool MutePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.mKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.M);
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
