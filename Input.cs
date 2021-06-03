using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;
using OpenTK;

namespace DefinitelyNotMinecraft
{
    public static class Input
    {
        private static KeyboardState prevKeyboarState;
        private static KeyboardState keyboardState;
        private static MouseState prevMouseState;
        private static MouseState mouseState;
        internal static void Init()
        {
            keyboardState = Keyboard.GetState();
            prevKeyboarState = keyboardState;
            mouseState = Mouse.GetState();
            prevMouseState = mouseState;
        }
        internal static void OnUpdateFrame()
        {
            prevKeyboarState = keyboardState;
            keyboardState = Keyboard.GetState();
            prevMouseState = mouseState;
            mouseState = Mouse.GetState();
        }
        public static bool IsKeyDown(Key key)
        {
            return keyboardState.IsKeyDown(key);
        }
        public static bool IsKeyUp(Key key)
        {
            return keyboardState.IsKeyUp(key);
        }
        public static bool IsKeyPressed(Key key)
        {
            return prevKeyboarState.IsKeyUp(key) && keyboardState.IsKeyDown(key);
        }
        public static bool IsKeyReleased(Key key)
        {
            return prevKeyboarState.IsKeyDown(key) && keyboardState.IsKeyUp(key);
        }
        public static Vector2 GetMouseDelta()
        {
            return new Vector2(mouseState.X - prevMouseState.X, mouseState.Y - prevMouseState.Y);
        }
    }
}
