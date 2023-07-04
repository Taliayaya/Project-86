using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public struct Window
    {
        public Action Open;
        public Action Close;
        public bool Hidden;

        public Window(Action open, Action close, bool hidden = false)
        {
            Open = open;
            Close = close;
            Hidden = hidden;
        }
        
        public static implicit operator Window((Action open, Action close) tuple) => new (tuple.open, tuple.close);
        public static implicit operator Window((Action open, Action close, bool hidden) tuple) => new (tuple.open, tuple.close, tuple.hidden);
    }
    public static class WindowManager
    {
        public static Stack<Window> WindowStack = new Stack<Window>();
        
        public static void Open(Action open, Action close, bool hidden = true)
        {
            Open(new Window(open, close, hidden));
        }

        public static void Open(Window window)
        {
            if (WindowStack.TryPeek(out Window lastWindowOpen))
                if (lastWindowOpen.Hidden)
                    HideWindowWindow(lastWindowOpen);
            
            WindowStack.Push(window);
            window.Open();
        }
        
        public static void Close()
        {
            if (WindowStack.Count == 0) return;
            var lastWindowOpen = WindowStack.Pop();
            lastWindowOpen.Close();
            
            if (WindowStack.TryPeek(out Window window))
                if (window.Hidden)
                    ShowWindowWindow(window);
        }
        
        private static void HideWindowWindow(Window window)
        {
            window.Close();
        }
        
        private static void ShowWindowWindow(Window window)
        {
            window.Open();
        }
        
        public static int WindowOpenedCount => WindowStack.Count;
        
    }
}