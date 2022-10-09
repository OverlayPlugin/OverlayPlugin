using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;

namespace RainbowMage.HtmlRenderer
{
    public class WinFormsRenderer : Renderer
    {
        public WinFormsRenderer(string overlayName, string overlayUuid, string url, IWinFormsTarget target, object api) :
            base(overlayName, overlayUuid, url, target, api)
        {
            target.Resize += OnResize;
        }

        public void OnResize(object sender, EventArgs e)
        {
            Resize(_target.Width, _target.Height);
        }

        protected override WindowInfo CreateWindowInfo()
        {
            var cefWindowInfo = new WindowInfo();
            cefWindowInfo.SetAsChild(((IWinFormsTarget)_target).Handle);
            cefWindowInfo.Width = _target.Width;
            cefWindowInfo.Height = _target.Height;

            return cefWindowInfo;
        }
    }
}
