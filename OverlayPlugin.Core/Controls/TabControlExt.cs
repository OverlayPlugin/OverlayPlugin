using System;
using System.Drawing;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

namespace RainbowMage.OverlayPlugin
{
    public class TabControlExt : TabControl
    {
        float dpiScale = 0;
        float DpiScale
        {
            get
            {
                if (dpiScale == 0)
                {
                    try { dpiScale = ActGlobals.oFormActMain.DpiScale; }
                    catch { dpiScale = 1; }
                }
                return dpiScale;
            }
        }
        int Dpi(float InputValue)
        {
            return (int)(InputValue * DpiScale);
        }
        byte PercFromTo(byte From, byte To, double Percent)
        {
            int diff = Math.Abs(To - From);
            if (From < To)
                return (byte)(From + diff * Percent);
            else
                return (byte)(From - diff * Percent);
        }
        Color GetColorMix(Color From, Color To, double Percent)
        {
            return Color.FromArgb(
                    PercFromTo(From.R, To.R, Math.Abs(Percent)),
                    PercFromTo(From.G, To.G, Math.Abs(Percent)),
                    PercFromTo(From.B, To.B, Math.Abs(Percent)));
        }


        bool itemSizeSet = false;
        protected override void OnPaint(PaintEventArgs e)
        {
            if (itemSizeSet == false)   // For some reason setting the size in the constructor is ineffective
            {
                itemSizeSet = true;
                ItemSize = new Size(Dpi(46), Dpi(140));
            }

            Color tabBackColor, tabSetBackColor;
            if (ActGlobals.oFormActMain == null)
            {
                tabBackColor = Color.White;
                tabSetBackColor = SystemColors.ControlLight;
            }
            else
            {
                tabBackColor = GetColorMix(ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.BackColorSetting, ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.ForeColorSetting, 0.0);
                tabSetBackColor = GetColorMix(ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.BackColorSetting, ActGlobals.oFormActMain.ActColorSettings.InternalWindowColors.ForeColorSetting, 0.2);
            }

            //base.OnPaint(e);    // Seems unnecessary since the next line wipes everything?
            e.Graphics.Clear(ActGlobals.oFormActMain?.ActColorSettings.MainWindowColors.BackColorSetting ?? SystemColors.ControlLightLight);
            Rectangle tabsetRect = new Rectangle(2, Dpi(4), (ItemSize.Height * RowCount) - 0, Height);   // The entire tabset area
            Rectangle tabmodelRect = new Rectangle(tabsetRect.X + Dpi(2), 0, ItemSize.Height - Dpi(4), 20);    // A size model for a single tab
            e.Graphics.FillRectangle(new SolidBrush(tabSetBackColor), tabsetRect);

            int inc = 0;
            foreach (TabPage tp in TabPages)
            {
                Color tabForeColor = ActGlobals.oFormActMain?.ActColorSettings.InternalWindowColors.ForeColorSetting ?? Color.Black;
                Font fontF = Font;
                Font fontFSmall = new Font(Font.FontFamily, (float)(Font.Size * 0.85));  // This is already DPI scaled by Windows because this.Font was
                Rectangle tabclipRect = GetTabRect(inc);    // A clipping rectangle that encompasses this tab
                Rectangle tabRect = new Rectangle(tabclipRect.X + Dpi(2), tabclipRect.Y + Dpi(5), tabmodelRect.Width, tabclipRect.Height - Dpi(2)); // A combination of our tab size model and the offset from the clipping rectangle
                Rectangle textRect1 = new Rectangle(tabclipRect.X + Dpi(2), tabclipRect.Y + Dpi(6), tabmodelRect.Width, tabclipRect.Height - Dpi(20));
                Rectangle textRect2 = new Rectangle(tabclipRect.X + Dpi(2), tabclipRect.Y + Dpi(22), tabmodelRect.Width, tabclipRect.Height - Dpi(20));

                StringFormat sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Center;

                if (inc == SelectedIndex)
                {
                    e.Graphics.FillRectangle(new SolidBrush(SystemColors.Highlight), tabRect);
                    tabForeColor = SystemColors.HighlightText;
                    fontF = new Font(Font, FontStyle.Bold);
                }
                else
                {
                    e.Graphics.FillRectangle(new SolidBrush(tabBackColor), tabRect);
                }

                e.Graphics.DrawString(tp.Name, fontF, new SolidBrush(tabForeColor), textRect1, sf);
                e.Graphics.DrawString(tp.Text, fontFSmall, new SolidBrush(tabForeColor), textRect2, sf);
                inc++;
            }
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        public TabControlExt() : base()
        {
            Alignment = TabAlignment.Left;

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);

            DoubleBuffered = true;

            //ItemSize = new Size(Dpi(46), Dpi(140));   // For some reason setting the size in the constructor is ineffective
            SizeMode = TabSizeMode.Fixed;
            BackColor = Color.Transparent;
        }
    }
}