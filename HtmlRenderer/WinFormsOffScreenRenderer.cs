using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using CefSharp;
using Point = System.Drawing.Point;

namespace RainbowMage.HtmlRenderer
{
    public class WinFormsOffScreenRenderer : Renderer
    {
        private bool shiftKeyPressed = false;
        private bool altKeyPressed = false;
        private bool controlKeyPressed = false;

        bool isDragging;
        bool hasDragged;
        Point offset;

        public bool Locked = false;

        public WinFormsOffScreenRenderer(string overlayName, string overlayUuid, string url, IWinFormsTarget target, object api) :
            base(overlayName, overlayUuid, url, target, api)
        {
            target.MouseWheel += OnMouseWheel;
            target.MouseDown += OnMouseDown;
            target.MouseUp += OnMouseUp;
            target.MouseMove += OnMouseMove;
            target.MouseLeave += OnMouseLeave;
            target.KeyDown += OnKeyDown;
            target.KeyUp += OnKeyUp;
            target.Resize += OnResize;
        }

        public void OnResize(object sender, EventArgs e)
        {
            Resize(_target.Width, _target.Height);
        }

        public void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (!Locked && !isDragging)
            {
                // If we have a draggable region defined, start dragging only if we're inside the region.
                if (DraggableRegion == null || DraggableRegion.IsVisible(e.Location))
                {
                    isDragging = true;
                    hasDragged = false;
                    offset = e.Location;
                }
            }

            SendMouseUpDown(e.X, e.Y, GetMouseButtonType(e), false);
        }

        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                if (!hasDragged)
                {
                    // Only notify once
                    hasDragged = true;
                    NotifyMoveStarted();
                }

                _target.Location = new Point(
                    e.X - offset.X + _target.Location.X,
                    e.Y - offset.Y + _target.Location.Y);
            }
            else
            {
                SendMouseMove(e.X, e.Y, GetMouseButtonType(e));
            }
        }

        public void OnMouseLeave(object sender, EventArgs e)
        {
            SendMouseLeave();
        }

        public void OnMouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;

            SendMouseUpDown(e.X, e.Y, GetMouseButtonType(e), true);
        }

        public void OnMouseWheel(object sender, MouseEventArgs e)
        {
            var flags = GetMouseEventFlags(e);

            SendMouseWheel(e.X, e.Y, e.Delta, shiftKeyPressed);
        }


        private MouseButtonType GetMouseButtonType(MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    return MouseButtonType.Left;
                case MouseButtons.Middle:
                    return MouseButtonType.Middle;
                case MouseButtons.Right:
                    return MouseButtonType.Right;
                default:
                    return MouseButtonType.Left; // 非対応のボタンは左クリックとして扱う
            }
        }

        private CefEventFlags GetMouseEventFlags(MouseEventArgs e)
        {
            var flags = CefEventFlags.None;

            if (e.Button == MouseButtons.Left)
            {
                flags |= CefEventFlags.LeftMouseButton;
            }
            else if (e.Button == MouseButtons.Middle)
            {
                flags |= CefEventFlags.MiddleMouseButton;
            }
            else if (e.Button == MouseButtons.Right)
            {
                flags |= CefEventFlags.RightMouseButton;
            }

            if (shiftKeyPressed)
            {
                flags |= CefEventFlags.ShiftDown;
            }
            if (altKeyPressed)
            {
                flags |= CefEventFlags.AltDown;
            }
            if (controlKeyPressed)
            {
                flags |= CefEventFlags.ControlDown;
            }

            return flags;
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            this.altKeyPressed = e.Alt;
            this.shiftKeyPressed = e.Shift;
            this.controlKeyPressed = e.Control;
        }

        public void OnKeyUp(object sender, KeyEventArgs e)
        {
            this.altKeyPressed = e.Alt;
            this.shiftKeyPressed = e.Shift;
            this.controlKeyPressed = e.Control;
        }

        public void OnKeyEvent(ref Message m)
        {
            var keyEvent = new KeyEvent();
            keyEvent.WindowsKeyCode = m.WParam.ToInt32();
            keyEvent.NativeKeyCode = (int)m.LParam.ToInt64();
            keyEvent.IsSystemKey = m.Msg == NativeMethods.WM_SYSCHAR ||
                                   m.Msg == NativeMethods.WM_SYSKEYDOWN ||
                                   m.Msg == NativeMethods.WM_SYSKEYUP;

            if (m.Msg == NativeMethods.WM_KEYDOWN || m.Msg == NativeMethods.WM_SYSKEYDOWN)
            {
                keyEvent.Type = KeyEventType.RawKeyDown;
            }
            else if (m.Msg == NativeMethods.WM_KEYUP || m.Msg == NativeMethods.WM_SYSKEYUP)
            {
                keyEvent.Type = KeyEventType.KeyUp;
            }
            else
            {
                keyEvent.Type = KeyEventType.Char;
            }
            keyEvent.Modifiers = GetKeyboardModifiers(ref m);

            SendKeyEvent(keyEvent);
        }

        private CefEventFlags GetKeyboardModifiers(ref Message m)
        {
            var modifiers = CefEventFlags.None;

            if (shiftKeyPressed) modifiers |= CefEventFlags.ShiftDown;
            if (controlKeyPressed) modifiers |= CefEventFlags.ControlDown;
            if (altKeyPressed) modifiers |= CefEventFlags.AltDown;

            if (IsKeyToggled(Keys.NumLock)) modifiers |= CefEventFlags.NumLockOn;
            if (IsKeyToggled(Keys.Capital)) modifiers |= CefEventFlags.CapsLockOn;

            switch ((Keys)m.WParam)
            {
                case Keys.Return:
                    if (((m.LParam.ToInt64() >> 48) & 0x0100) != 0)
                        modifiers |= CefEventFlags.IsKeyPad;
                    break;
                case Keys.Insert:
                case Keys.Delete:
                case Keys.Home:
                case Keys.End:
                case Keys.Prior:
                case Keys.Next:
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    if (!(((m.LParam.ToInt64() >> 48) & 0x0100) != 0))
                        modifiers |= CefEventFlags.IsKeyPad;
                    break;
                case Keys.NumLock:
                case Keys.NumPad0:
                case Keys.NumPad1:
                case Keys.NumPad2:
                case Keys.NumPad3:
                case Keys.NumPad4:
                case Keys.NumPad5:
                case Keys.NumPad6:
                case Keys.NumPad7:
                case Keys.NumPad8:
                case Keys.NumPad9:
                case Keys.Divide:
                case Keys.Multiply:
                case Keys.Subtract:
                case Keys.Add:
                case Keys.Decimal:
                case Keys.Clear:
                    modifiers |= CefEventFlags.IsKeyPad;
                    break;
                case Keys.Shift:
                    if (shiftKeyPressed) modifiers |= CefEventFlags.IsLeft;
                    else modifiers |= CefEventFlags.IsRight;
                    break;
                case Keys.Control:
                    if (controlKeyPressed) modifiers |= CefEventFlags.IsLeft;
                    else modifiers |= CefEventFlags.IsRight;
                    break;
                case Keys.Alt:
                    if (altKeyPressed) modifiers |= CefEventFlags.IsLeft;
                    else modifiers |= CefEventFlags.IsRight;
                    break;
                case Keys.LWin:
                    modifiers |= CefEventFlags.IsLeft;
                    break;
                case Keys.RWin:
                    modifiers |= CefEventFlags.IsRight;
                    break;
            }

            return modifiers;
        }

        private bool IsKeyToggled(Keys key)
        {
            return (NativeMethods.GetKeyState((int)key) & 1) == 1;
        }
    }
}
