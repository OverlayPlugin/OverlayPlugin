using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;

namespace RainbowMage.HtmlRenderer
{
    internal class ContextMenuHandler : IContextMenuHandler
    {
        private Func<int, int, bool> ctxMenuCallback;

        public ContextMenuHandler(Func<int, int, bool> ctxMenuCallback)
        {
            this.ctxMenuCallback = ctxMenuCallback;
        }

        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
        }

        public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            return false;
        }

        public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
        {
        }

        public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            if (ctxMenuCallback == null)
            {
                // Suppress the context menu.
                return true;
            }
            else
            {
                return ctxMenuCallback(parameters.XCoord, parameters.YCoord);
            }
        }
    }
}
