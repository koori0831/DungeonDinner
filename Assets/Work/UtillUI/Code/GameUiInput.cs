using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Work.UtillUI.Code
{
    public enum GameUiContext { Preparation, Adventure, Cooking, Conversation, Dispatch }

    /// <summary>Screen transitions own leases; input is rearmed only after all held controls are released.</summary>
    public static class GameUiInput
    {
        private static readonly HashSet<Lease> Locks = new HashSet<Lease>();
        private static bool _awaitRelease;
        private static int _releaseFrame = -1;
        public static GameUiContext Context { get; private set; }
        public static bool IsBlocked => Locks.Count > 0 || _awaitRelease || Time.frameCount <= _releaseFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Locks.Clear();
            _awaitRelease = false;
            _releaseFrame = -1;
            Context = GameUiContext.Preparation;
        }

        public static IDisposable Acquire()
        {
            var lease = new Lease();
            Locks.Add(lease);
            ClearSelection();
            return lease;
        }

        public static void SetContext(GameUiContext context)
        {
            Context = context;
            RequireFreshPress();
        }

        public static void RequireFreshPress()
        {
            _awaitRelease = true;
            _releaseFrame = Time.frameCount;
            ClearSelection();
        }

        public static void ClearSelection()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        internal static void PollRelease()
        {
            if (!_awaitRelease || Locks.Count > 0) return;
            var keyboard = Keyboard.current;
            bool held = keyboard != null && (keyboard.spaceKey.isPressed || keyboard.enterKey.isPressed
                || keyboard.numpadEnterKey.isPressed || keyboard.escapeKey.isPressed);
            held |= Mouse.current != null && Mouse.current.leftButton.isPressed;
            held |= Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
            held |= Gamepad.current != null && Gamepad.current.buttonSouth.isPressed;
            if (held) return;
            _awaitRelease = false;
            _releaseFrame = Time.frameCount;
        }

        private sealed class Lease : IDisposable
        {
            public void Dispose()
            {
                if (Locks.Remove(this) && Locks.Count == 0) RequireFreshPress();
            }
        }
    }

}
