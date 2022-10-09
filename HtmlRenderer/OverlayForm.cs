using RainbowMage.HtmlRenderer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.Structs;
using CefSharp.Enums;
using Point = System.Drawing.Point;

namespace RainbowMage.HtmlRenderer
{
    public partial class OverlayForm : Form, IWinFormsTarget
    {
        private DIBitmap surfaceBuffer;
        private bool terminated = false;

        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int CP_NOCLOSE_BUTTON = 0x200;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        public WinFormsOffScreenRenderer Renderer { get; private set; }

        private string url;
        public string Url
        {
            get { return this.url; }
            set
            {
                this.url = value;
                this.Renderer.Load(value);
            }
        }

        private bool isClickThru;
        public bool IsClickThru
        {
            get
            {
                return this.isClickThru;
            }
            set
            {
                if (this.isClickThru != value)
                {
                    this.isClickThru = value;
                    UpdateMouseClickThru();
                }
            }
        }

        private int maxFrameRate;
        public int MaxFrameRate
        {
            get
            {
                return this.maxFrameRate;
            }
            set
            {
                this.maxFrameRate = value;
                this.Renderer.SetMaxFramerate(value);
            }
        }

        public bool Locked
        {
            get
            {
                return Renderer.Locked;
            }
            set
            {
                Renderer.Locked = value;
            }
        }

        public OverlayForm(string overlayName, string overlayUuid, string url, int maxFrameRate = 30, object api = null)
        {
            InitializeComponent();

            this.Renderer = new WinFormsOffScreenRenderer(overlayName, overlayUuid, url, this, api);
            this.maxFrameRate = maxFrameRate;
            this.url = url;

            ClearFrame();

            // Alt+Tab を押したときに表示されるプレビューから除外する
            HidePreview();
        }

        /// <summary>
        /// 指定されたフォームを Windows の Alt+Tab の切り替え候補から除外します。
        /// </summary>
        /// <param name="form"></param>
        public void HidePreview()
        {
            int ex = NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_EXSTYLE);
            ex |= NativeMethods.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLongA(Handle, NativeMethods.GWL_EXSTYLE, (IntPtr)ex);
        }

        public void SetAcceptFocus(bool accept)
        {
            int ex = NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_EXSTYLE);
            if (accept)
            {
                ex &= ~WS_EX_NOACTIVATE;
            }
            else
            {
                ex |= WS_EX_NOACTIVATE;
            }
            NativeMethods.SetWindowLongA(Handle, NativeMethods.GWL_EXSTYLE, (IntPtr)ex);
        }

        public void Reload()
        {
            this.Renderer.Reload();
        }

        #region Layered window related stuffs
        protected override System.Windows.Forms.CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle = cp.ExStyle | WS_EX_TOPMOST | WS_EX_LAYERED | WS_EX_NOACTIVATE;
                cp.ClassStyle = cp.ClassStyle | CP_NOCLOSE_BUTTON;

                return cp;
            }
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            const int WM_NCHITTEST = 0x84;
            const int HTBOTTOMRIGHT = 17;

            const int gripSize = 16;

            if (m.Msg == WM_NCHITTEST && !this.Locked)
            {
                var posisiton = new Point(m.LParam.ToInt32() & 0xFFFF, m.LParam.ToInt32() >> 16);
                posisiton = this.PointToClient(posisiton);
                if (posisiton.X >= this.ClientSize.Width - gripSize &&
                    posisiton.Y >= this.ClientSize.Height - gripSize)
                {
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                    return;
                }
            }

            if (m.Msg == NativeMethods.WM_KEYDOWN ||
                m.Msg == NativeMethods.WM_KEYUP ||
                m.Msg == NativeMethods.WM_CHAR ||
                m.Msg == NativeMethods.WM_SYSKEYDOWN ||
                m.Msg == NativeMethods.WM_SYSKEYUP ||
                m.Msg == NativeMethods.WM_SYSCHAR)
            {
                Renderer.OnKeyEvent(ref m);
            }
        }

        private void UpdateLayeredWindowBitmap()
        {
            if (surfaceBuffer.IsDisposed || this.terminated) { return; }

            var blend = new NativeMethods.BlendFunction
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };
            var windowPosition = new NativeMethods.Point
            {
                X = this.Left,
                Y = this.Top
            };
            var surfaceSize = new NativeMethods.Size
            {
                Width = surfaceBuffer.Width,
                Height = surfaceBuffer.Height
            };
            var surfacePosition = new NativeMethods.Point
            {
                X = 0,
                Y = 0
            };

            IntPtr handle = IntPtr.Zero;
            try
            {
                if (!this.terminated)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            handle = this.Handle;
                        }));
                    }
                    else
                    {
                        handle = this.Handle;
                    }

                    NativeMethods.UpdateLayeredWindow(
                        handle,
                        IntPtr.Zero,
                        ref windowPosition,
                        ref surfaceSize,
                        surfaceBuffer.DeviceContext,
                        ref surfacePosition,
                        0,
                        ref blend,
                        NativeMethods.ULW_ALPHA);
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }
        #endregion

        #region Mouse click-thru related

        private void UpdateMouseClickThru()
        {
            if (this.isClickThru)
            {
                EnableMouseClickThru();
            }
            else
            {
                DisableMouseClickThru();
            }
        }

        private void EnableMouseClickThru()
        {
            NativeMethods.SetWindowLong(
                this.Handle,
                NativeMethods.GWL_EXSTYLE,
                NativeMethods.GetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE) | NativeMethods.WS_EX_TRANSPARENT);
        }

        private void DisableMouseClickThru()
        {
            NativeMethods.SetWindowLong(
                this.Handle,
                NativeMethods.GWL_EXSTYLE,
                NativeMethods.GetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE) & ~NativeMethods.WS_EX_TRANSPARENT);
        }

        #endregion

        public void RenderFrame(PaintElementType type, Rect dirtyRect, IntPtr buffer, int width, int height)
        {
            if (!this.terminated)
            {
                try
                {
                    if (surfaceBuffer != null &&
                        (surfaceBuffer.Width != width || surfaceBuffer.Height != height))
                    {
                        surfaceBuffer.Dispose();
                        surfaceBuffer = null;
                    }

                    if (surfaceBuffer == null)
                    {
                        surfaceBuffer = new DIBitmap(width, height);
                    }

                    // TODO: DirtyRect に対応
                    surfaceBuffer.SetSurfaceData(buffer, (uint)(width * height * 4));

                    UpdateLayeredWindowBitmap();
                }
                catch
                {

                }
            }
        }

        public void ClearFrame()
        {
            if (!this.terminated)
            {
                if (surfaceBuffer != null)
                {
                    surfaceBuffer.Dispose();
                }

                surfaceBuffer = new DIBitmap(Width, Height);
                UpdateLayeredWindowBitmap();
            }
        }

        private void OverlayForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Renderer.EndRender();
            terminated = true;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (this.surfaceBuffer != null)
            {
                this.surfaceBuffer.Dispose();
            }

            if (this.Renderer != null)
            {
                this.Renderer.Dispose();
            }

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        public void MovePopup(Rect rect)
        {

        }

        public void SetPopupVisible(bool visible)
        {

        }
    }
}
