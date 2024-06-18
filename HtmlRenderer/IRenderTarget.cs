using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.Structs;

namespace RainbowMage.HtmlRenderer
{
    public interface IRenderTarget
    {
        int MaxFrameRate { get; }
        System.Drawing.Point Location { get; set; }
        int Width { get; }
        int Height { get; }

        void RenderFrame(PaintElementType type, Rect dirtyRect, IntPtr buffer, int width, int height);

        void MovePopup(Rect rect);

        void SetPopupVisible(bool visible);

        void SetCursor(Cursor cursor);
    }

    public interface IWinFormsTarget : IRenderTarget
    {
        IntPtr Handle { get; }

        event EventHandler HandleCreated;
        event EventHandler HandleDestroyed;
        event MouseEventHandler MouseWheel;
        event MouseEventHandler MouseDown;
        event MouseEventHandler MouseUp;
        event MouseEventHandler MouseMove;
        event EventHandler MouseLeave;
        event KeyEventHandler KeyDown;
        event KeyEventHandler KeyUp;
        event EventHandler Resize;
    }
}
