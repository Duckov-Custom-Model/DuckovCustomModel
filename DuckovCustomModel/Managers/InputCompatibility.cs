using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DuckovCustomModel.Managers
{
    internal static class InputCompatibility
    {
        private static bool _legacyUnavailable;

        internal static bool AllowLegacyFallback { get; set; } = true;

        internal static Vector2 MousePosition => Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Legacy(() => (Vector2)Input.mousePosition, Vector2.zero);

        internal static float MouseScrollY => Mouse.current != null
            ? Mouse.current.scroll.ReadValue().y
            : Legacy(() => Input.mouseScrollDelta.y, 0f);

        internal static bool GetKey(KeyCode key)
        {
            return ReadKey(key, Edge.Held);
        }

        internal static bool GetKeyDown(KeyCode key)
        {
            return ReadKey(key, Edge.Down);
        }

        internal static bool GetKeyUp(KeyCode key)
        {
            return ReadKey(key, Edge.Up);
        }

        private static bool ReadKey(KeyCode key, Edge edge)
        {
            if (key == KeyCode.None || (InputBlocker.IsInputBlocked && !InputBlocker.IsGettingRealInput)) return false;
            if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6)
                return ReadMouseButton((int)key - (int)KeyCode.Mouse0, edge);
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var mapped = MapKey(key);
                if (mapped == Key.None || (int)mapped > keyboard.allKeys.Count) return false;
                return ReadButton(keyboard[mapped], edge);
            }

            if (!AllowLegacyFallback || _legacyUnavailable) return false;
            try
            {
                return edge == Edge.Down ? Input.GetKeyDown(key) :
                    edge == Edge.Up ? Input.GetKeyUp(key) : Input.GetKey(key);
            }
            catch (InvalidOperationException)
            {
                _legacyUnavailable = true;
                return false;
            }
        }

        internal static bool GetMouseButtonDown(int button)
        {
            return ReadMouseButton(button, Edge.Down);
        }

        private static bool ReadMouseButton(int button, Edge edge)
        {
            if (button < 0 || button > 6) return false;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var control = button switch
                {
                    0 => mouse.leftButton,
                    1 => mouse.rightButton,
                    2 => mouse.middleButton,
                    3 => mouse.backButton,
                    4 => mouse.forwardButton,
                    _ => null,
                };
                return control != null && ReadButton(control, edge);
            }

            if (!AllowLegacyFallback || _legacyUnavailable) return false;
            try
            {
                return edge == Edge.Down ? Input.GetMouseButtonDown(button) :
                    edge == Edge.Up ? Input.GetMouseButtonUp(button) : Input.GetMouseButton(button);
            }
            catch (InvalidOperationException)
            {
                _legacyUnavailable = true;
                return false;
            }
        }

        private static bool ReadButton(ButtonControl button, Edge edge)
        {
            return edge == Edge.Down ? button.wasPressedThisFrame :
                edge == Edge.Up ? button.wasReleasedThisFrame : button.isPressed;
        }

        private static T Legacy<T>(Func<T> read, T unavailable)
        {
            if (!AllowLegacyFallback || _legacyUnavailable) return unavailable;
            try
            {
                return read();
            }
            catch (InvalidOperationException)
            {
                _legacyUnavailable = true;
                return unavailable;
            }
        }

        private static Key MapKey(KeyCode key)
        {
            if (key >= KeyCode.A && key <= KeyCode.Z) return Key.A + ((int)key - (int)KeyCode.A);
            if (key >= KeyCode.Alpha1 && key <= KeyCode.Alpha9) return Key.Digit1 + ((int)key - (int)KeyCode.Alpha1);
            if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
                return Key.Numpad0 + ((int)key - (int)KeyCode.Keypad0);
            if (key >= KeyCode.F1 && key <= KeyCode.F12) return Key.F1 + ((int)key - (int)KeyCode.F1);
            if (key >= KeyCode.F13 && key <= KeyCode.F15) return (Key)(112 + (int)key - (int)KeyCode.F13);
            return key switch
            {
                KeyCode.Alpha0 => Key.Digit0,
                KeyCode.Space => Key.Space, KeyCode.Return => Key.Enter, KeyCode.Tab => Key.Tab,
                KeyCode.BackQuote => Key.Backquote, KeyCode.Quote => Key.Quote, KeyCode.Semicolon => Key.Semicolon,
                KeyCode.Comma => Key.Comma, KeyCode.Period => Key.Period, KeyCode.Slash => Key.Slash,
                KeyCode.Backslash => Key.Backslash, KeyCode.LeftBracket => Key.LeftBracket,
                KeyCode.RightBracket => Key.RightBracket,
                KeyCode.Minus => Key.Minus, KeyCode.Equals => Key.Equals,
                KeyCode.LeftShift => Key.LeftShift, KeyCode.RightShift => Key.RightShift,
                KeyCode.LeftAlt => Key.LeftAlt, KeyCode.RightAlt => Key.RightAlt, KeyCode.AltGr => Key.RightAlt,
                KeyCode.LeftControl => Key.LeftCtrl, KeyCode.RightControl => Key.RightCtrl,
                KeyCode.LeftCommand => Key.LeftMeta, KeyCode.RightCommand => Key.RightMeta,
                KeyCode.LeftWindows => Key.LeftMeta, KeyCode.RightWindows => Key.RightMeta,
                KeyCode.Menu => Key.ContextMenu, KeyCode.Escape => Key.Escape,
                KeyCode.LeftArrow => Key.LeftArrow, KeyCode.RightArrow => Key.RightArrow,
                KeyCode.UpArrow => Key.UpArrow, KeyCode.DownArrow => Key.DownArrow,
                KeyCode.Backspace => Key.Backspace, KeyCode.PageDown => Key.PageDown, KeyCode.PageUp => Key.PageUp,
                KeyCode.Home => Key.Home, KeyCode.End => Key.End, KeyCode.Insert => Key.Insert,
                KeyCode.Delete => Key.Delete,
                KeyCode.CapsLock => Key.CapsLock, KeyCode.Numlock => Key.NumLock, KeyCode.ScrollLock => Key.ScrollLock,
                KeyCode.Print => Key.PrintScreen, KeyCode.Pause => Key.Pause,
                KeyCode.KeypadEnter => Key.NumpadEnter, KeyCode.KeypadDivide => Key.NumpadDivide,
                KeyCode.KeypadMultiply => Key.NumpadMultiply, KeyCode.KeypadPlus => Key.NumpadPlus,
                KeyCode.KeypadMinus => Key.NumpadMinus, KeyCode.KeypadPeriod => Key.NumpadPeriod,
                KeyCode.KeypadEquals => Key.NumpadEquals,
                _ => Key.None,
            };
        }

        private enum Edge
        {
            Held,
            Down,
            Up,
        }
    }
}
