using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin.Controls
{
    public partial class LogPanel : UserControl
    {
        public LogPanel(TinyIoCContainer container)
        {
            InitializeComponent();

            container.Resolve<ILogger>().OnLog += HandleOnLog;
        }

        private void HandleOnLog(object sender, IReadOnlyCollection<LogEntry> e)
        {
            PluginMain.InvokeOnUIThread(() =>
            {
                var newText = @"{\rtf1\ansi";

                foreach (var entry in e)
                {
                    newText += entry.ToRtfString();
                }

                newText += "}";

                logBox.Rtf = newText;
            });
        }
    }
}
