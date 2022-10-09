using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using CefSharp;
using CefSharp.Structs;
using System.Windows.Forms;

namespace RainbowMage.HtmlRenderer
{
    public interface IRenderTarget
    {
        int MaxFrameRate { get; }
        Cursor Cursor { get; set; }
        System.Drawing.Point Location { get; set; }
        int Width { get; }
        int Height { get; }

        void RenderFrame(PaintElementType type, Rect dirtyRect, IntPtr buffer, int width, int height);

        void MovePopup(Rect rect);

        void SetPopupVisible(bool visible);
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
