using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TempoApp
{
    internal static class Palette
    {
        public static readonly Color Night = Color.FromArgb(5, 7, 12);
        public static readonly Color Canvas = Color.FromArgb(7, 9, 16);
        public static readonly Color Card = Color.FromArgb(11, 15, 26);
        public static readonly Color Surface = Color.FromArgb(16, 22, 38);
        public static readonly Color Ink = Color.FromArgb(228, 240, 255);
        public static readonly Color Muted = Color.FromArgb(108, 128, 158);
        public static readonly Color Faint = Color.FromArgb(38, 54, 86);
        public static readonly Color Accent = Color.FromArgb(56, 236, 220);
        public static readonly Color AccentDark = Color.FromArgb(28, 168, 164);
        public static readonly Color Soft = Color.FromArgb(12, 16, 28);
        public static readonly Color Hairline = Color.FromArgb(48, Palette.Accent);
        public static readonly Color DangerFill = Color.FromArgb(42, 12, 28);
        public static readonly Color DangerText = Color.FromArgb(255, 92, 148);
        public static readonly Color DangerBorder = Color.FromArgb(120, 36, 72);
        public static readonly Color MoonRed = Color.FromArgb(255, 64, 128);
        public static readonly Color MoonRedHot = Color.FromArgb(255, 110, 168);
    }

    internal static class PaintKit
    {
        public static readonly SolidBrush Night = new SolidBrush(Palette.Night);
        public static readonly SolidBrush Canvas = new SolidBrush(Palette.Canvas);
        public static readonly SolidBrush Card = new SolidBrush(Palette.Card);
        public static readonly SolidBrush Surface = new SolidBrush(Palette.Surface);
        public static readonly SolidBrush Soft = new SolidBrush(Palette.Soft);
        public static readonly SolidBrush Accent = new SolidBrush(Palette.Accent);
        public static readonly SolidBrush Muted = new SolidBrush(Palette.Muted);
        public static readonly SolidBrush Faint = new SolidBrush(Palette.Faint);
        public static readonly SolidBrush WidgetShell = new SolidBrush(Color.FromArgb(8, 12, 22));
        public static readonly SolidBrush GlowTop = new SolidBrush(Color.FromArgb(36, Palette.Accent));
        public static readonly SolidBrush GlowBottom = new SolidBrush(Color.FromArgb(28, Palette.MoonRed));
        public static readonly SolidBrush DialogGlow = new SolidBrush(Color.FromArgb(28, Palette.Accent));
        public static readonly Pen Hairline = new Pen(Palette.Hairline, 1f);
        public static readonly Pen FaintPen = new Pen(Palette.Faint, 1f);
        public static readonly Pen WidgetBorder = new Pen(Color.FromArgb(70, Palette.Accent), 1f);
        public static readonly Pen RingTrack = CreateRoundPen(Color.FromArgb(70, Palette.Faint), 3.2f);
        public static readonly Pen RingAccent = CreateRoundPen(Palette.Accent, 3.2f);
        public static readonly Pen RingMuted = CreateRoundPen(Palette.Muted, 3.2f);
        public static readonly Pen RingGlow = CreateRoundPen(Color.FromArgb(90, Palette.Accent), 8f);
        public static readonly Pen Logo = CreateRoundPen(Palette.Night, 2.4f);
        public static readonly Pen Grid = new Pen(Color.FromArgb(18, Palette.Accent), 1f);

        private static Pen CreateRoundPen(Color color, float width)
        {
            Pen pen = new Pen(color, width);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            return pen;
        }

        public static void HighQuality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }
    }

    internal static class Shapes
    {
        public static GraphicsPath Rounded(Rectangle bounds, int radius)
        {
            int max = Math.Max(0, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
            GraphicsPath path = new GraphicsPath();
            if (max <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }
            int diameter = max * 2;
            Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void ApplyRegion(Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            using (GraphicsPath path = Rounded(new Rectangle(0, 0, control.Width, control.Height), radius))
            {
                Region old = control.Region;
                control.Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }
    }

    internal static class Ui
    {
        public static bool SetText(Control control, string value)
        {
            if (control.Text == value) return false;
            control.Text = value;
            return true;
        }

        public static bool SetColor(Control control, Color value)
        {
            if (control.ForeColor == value) return false;
            control.ForeColor = value;
            return true;
        }
    }

    internal sealed class CachedRoundPath : IDisposable
    {
        private GraphicsPath path;
        private Rectangle bounds;
        private int radius = int.MinValue;

        public GraphicsPath Get(Rectangle next, int nextRadius)
        {
            if (path != null && next == bounds && nextRadius == radius) return path;
            if (path != null) path.Dispose();
            bounds = next;
            radius = nextRadius;
            path = Shapes.Rounded(next, nextRadius);
            return path;
        }

        public void Dispose()
        {
            if (path == null) return;
            path.Dispose();
            path = null;
        }
    }

    internal class CardPanel : Panel
    {
        private readonly CachedRoundPath fillPath = new CachedRoundPath();
        private readonly CachedRoundPath borderPath = new CachedRoundPath();
        private SolidBrush fillBrush;

        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            PaintKit.HighQuality(e.Graphics);
            Rectangle rect = new Rectangle(1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3));
            if (fillBrush == null) fillBrush = new SolidBrush(Palette.Card);
            e.Graphics.FillPath(fillBrush, fillPath.Get(rect, 12));
            e.Graphics.DrawPath(PaintKit.Hairline, borderPath.Get(new Rectangle(1, 1, Math.Max(0, Width - 4), Math.Max(0, Height - 4)), 12));
            using (Pen corner = new Pen(Color.FromArgb(90, Palette.Accent), 1.2f))
            {
                e.Graphics.DrawLine(corner, rect.X + 2, rect.Y + 14, rect.X + 2, rect.Y + 2);
                e.Graphics.DrawLine(corner, rect.X + 2, rect.Y + 2, rect.X + 14, rect.Y + 2);
                e.Graphics.DrawLine(corner, rect.Right - 2, rect.Y + 14, rect.Right - 2, rect.Y + 2);
                e.Graphics.DrawLine(corner, rect.Right - 2, rect.Y + 2, rect.Right - 14, rect.Y + 2);
            }
            base.OnPaint(e);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            Shapes.ApplyRegion(this, 12);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fillPath.Dispose();
                borderPath.Dispose();
                if (fillBrush != null) fillBrush.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal class SoftPanel : Panel
    {
        public int Radius = 8;
        public Color FillColor = Palette.Soft;
        public bool DrawBorder;
        public Color BorderColor = Palette.Faint;
        private readonly CachedRoundPath fillPath = new CachedRoundPath();
        private readonly CachedRoundPath borderPath = new CachedRoundPath();
        private SolidBrush fillBrush;
        private Color brushColor;

        public SoftPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            PaintKit.HighQuality(e.Graphics);
            if (fillBrush == null || brushColor != FillColor)
            {
                if (fillBrush != null) fillBrush.Dispose();
                fillBrush = new SolidBrush(FillColor);
                brushColor = FillColor;
            }
            Rectangle fill = new Rectangle(0, 0, Width, Height);
            e.Graphics.FillPath(fillBrush, fillPath.Get(fill, Radius));
            if (DrawBorder)
            {
                using (Pen border = new Pen(BorderColor, 1f))
                    e.Graphics.DrawPath(border, borderPath.Get(new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1)), Radius));
            }
            base.OnPaint(e);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            Shapes.ApplyRegion(this, Radius);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fillPath.Dispose();
                borderPath.Dispose();
                if (fillBrush != null) fillBrush.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal class TempoButton : Control
    {
        private bool hovered;
        private bool pressed;
        public int Radius = 8;
        public bool Selected;
        public bool Primary;
        public bool Danger;

        public TempoButton()
        {
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (CanFocus) Focus();
            pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Shapes.ApplyRegion(this, Radius);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            PaintKit.HighQuality(e.Graphics);
            Rectangle fillRect = new Rectangle(0, 0, Width, Height);
            Rectangle borderRect = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
            Color fill;
            Color text;
            Color border;

            if (!Enabled)
            {
                fill = Palette.Soft;
                text = Color.FromArgb(70, 86, 110);
                border = Palette.Faint;
            }
            else if (Danger)
            {
                fill = pressed ? Color.FromArgb(58, 16, 34) : hovered ? Palette.DangerFill : Palette.Surface;
                text = Palette.DangerText;
                border = Palette.DangerBorder;
            }
            else if (Primary)
            {
                fill = pressed ? Palette.AccentDark : hovered ? Color.FromArgb(110, 255, 240) : Palette.Accent;
                text = Palette.Night;
                border = fill;
            }
            else if (Selected)
            {
                fill = pressed ? Palette.AccentDark : Palette.Accent;
                text = Palette.Night;
                border = fill;
            }
            else
            {
                fill = pressed ? Palette.Card : hovered ? Color.FromArgb(22, 32, 54) : Palette.Surface;
                text = Palette.Ink;
                border = hovered ? Color.FromArgb(90, Palette.Accent) : Palette.Faint;
            }

            using (GraphicsPath fillPath = Shapes.Rounded(fillRect, Radius))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                e.Graphics.FillPath(brush, fillPath);
                using (GraphicsPath borderPath = Shapes.Rounded(borderRect, Radius))
                using (Pen pen = new Pen(border, 1f))
                    e.Graphics.DrawPath(pen, borderPath);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                borderRect,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

            if (Focused && ShowFocusCues)
            {
                Rectangle focus = Rectangle.Inflate(borderRect, -4, -4);
                ControlPaint.DrawFocusRectangle(e.Graphics, focus, text, fill);
            }
        }
    }

    internal class TempoSwitch : Control
    {
        private bool hovered;
        private bool pressed;
        private bool on = true;

        public event EventHandler Toggled;

        public bool On
        {
            get { return on; }
            set
            {
                if (on == value) return;
                on = value;
                Invalidate();
            }
        }

        public TempoSwitch()
        {
            Cursor = Cursors.Hand;
            Size = new Size(48, 26);
            TabStop = true;
            AccessibleRole = AccessibleRole.CheckButton;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            BackColor = Color.Transparent;
        }

        protected override void OnClick(EventArgs e)
        {
            if (!Enabled) return;
            On = !On;
            if (Toggled != null) Toggled(this, EventArgs.Empty);
            base.OnClick(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (CanFocus) Focus();
            pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Enabled && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space))
            {
                OnClick(EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            PaintKit.HighQuality(e.Graphics);
            Rectangle track = new Rectangle(0, 3, Width - 1, Height - 7);
            using (GraphicsPath path = Shapes.Rounded(track, track.Height / 2))
            using (SolidBrush fill = new SolidBrush(TrackColor()))
            using (Pen border = new Pen(Enabled ? Palette.Faint : Color.FromArgb(28, 40, 62), 1f))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }

            int knob = Height - 8;
            int x = on ? Width - knob - 3 : 3;
            if (pressed) x += on ? -1 : 1;
            Rectangle knobRect = new Rectangle(x, 4, knob, knob);
            using (SolidBrush knobFill = new SolidBrush(Enabled ? Palette.Ink : Palette.Muted))
                e.Graphics.FillEllipse(knobFill, knobRect);
        }

        private Color TrackColor()
        {
            if (!Enabled) return Palette.Soft;
            if (on) return hovered ? Color.FromArgb(110, 255, 240) : Palette.Accent;
            return hovered ? Color.FromArgb(22, 32, 54) : Palette.Surface;
        }
    }

    internal class DialControl : Control
    {
        private readonly Label stateLabel;
        private readonly Label unitLabel;
        private readonly Label unitLeftLabel;
        private readonly Label unitMiddleLabel;
        private readonly Label unitRightLabel;
        private readonly Label stopwatchLabel;
        private readonly Font stopwatchDisplayFont;
        private readonly Font workDisplayFont;
        private readonly TextBox minuteBox;
        private readonly TextBox secondBox;
        private readonly Label colonLabel;
        private bool timerMode = true;
        private double timerProgress = 1.0;
        private double stopwatchSeconds;
        private bool running;
        private int layoutKind = -1;
        private string lastStateText = "";
        private string lastMinuteText = "";
        private string lastSecondText = "";
        private string lastStopwatchText = "";
        private double lastDrawnProgress = -1;

        public event EventHandler TimerValueCommitted;

        public DialControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            SuspendLayout();

            stateLabel = CreateLabel(10f, FontStyle.Bold, Palette.Muted);
            stateLabel.TextAlign = ContentAlignment.MiddleCenter;
            stateLabel.Text = "SET TIMER";
            Controls.Add(stateLabel);

            unitLabel = CreateLabel(8.2f, FontStyle.Bold, Palette.Muted);
            unitLabel.TextAlign = ContentAlignment.MiddleCenter;
            unitLabel.Text = "MIN      SEC";
            unitLabel.Visible = false;
            Controls.Add(unitLabel);

            unitLeftLabel = CreateLabel(8.2f, FontStyle.Bold, Palette.Muted);
            unitLeftLabel.TextAlign = ContentAlignment.MiddleCenter;
            unitLeftLabel.Text = "MIN";
            Controls.Add(unitLeftLabel);

            unitMiddleLabel = CreateLabel(8.2f, FontStyle.Bold, Palette.Muted);
            unitMiddleLabel.TextAlign = ContentAlignment.MiddleCenter;
            unitMiddleLabel.Text = "SEC";
            Controls.Add(unitMiddleLabel);

            unitRightLabel = CreateLabel(8.2f, FontStyle.Bold, Palette.Muted);
            unitRightLabel.TextAlign = ContentAlignment.MiddleCenter;
            unitRightLabel.Visible = false;
            Controls.Add(unitRightLabel);

            minuteBox = CreateTimeBox("25", "Minutes");
            secondBox = CreateTimeBox("00", "Seconds");
            colonLabel = CreateLabel(34f, FontStyle.Regular, Palette.Ink);
            colonLabel.Font = new Font("Consolas", 34f, FontStyle.Regular, GraphicsUnit.Point);
            colonLabel.Text = ":";
            colonLabel.TextAlign = ContentAlignment.MiddleCenter;

            Controls.Add(minuteBox);
            Controls.Add(colonLabel);
            Controls.Add(secondBox);

            stopwatchDisplayFont = new Font("Consolas", 35f, FontStyle.Regular, GraphicsUnit.Point);
            workDisplayFont = new Font("Consolas", 33f, FontStyle.Regular, GraphicsUnit.Point);
            stopwatchLabel = CreateLabel(35f, FontStyle.Regular, Palette.Ink);
            stopwatchLabel.Font = stopwatchDisplayFont;
            stopwatchLabel.Text = "00:00.0";
            stopwatchLabel.TextAlign = ContentAlignment.MiddleCenter;
            stopwatchLabel.Visible = false;
            Controls.Add(stopwatchLabel);

            minuteBox.Leave += CommitTimerValue;
            secondBox.Leave += CommitTimerValue;
            minuteBox.KeyDown += TimeBoxKeyDown;
            secondBox.KeyDown += TimeBoxKeyDown;
            minuteBox.KeyPress += NumericOnly;
            secondBox.KeyPress += NumericOnly;
            minuteBox.Enter += SelectTimeText;
            secondBox.Enter += SelectTimeText;
            Size = new Size(280, 280);
            ResumeLayout(false);
        }

        private Label CreateLabel(float size, FontStyle style, Color color)
        {
            Label label = new Label();
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            label.ForeColor = color;
            label.Font = new Font("Segoe UI", size, style, GraphicsUnit.Point);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private TextBox CreateTimeBox(string text, string accessibleName)
        {
            TextBox box = new TextBox();
            box.Text = text;
            box.AccessibleName = accessibleName;
            box.BorderStyle = BorderStyle.None;
            box.BackColor = Palette.Night;
            box.ForeColor = Palette.Ink;
            box.Font = new Font("Consolas", 42f, FontStyle.Regular, GraphicsUnit.Point);
            box.TextAlign = HorizontalAlignment.Center;
            box.MaxLength = 2;
            box.ShortcutsEnabled = false;
            return box;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            stateLabel.SetBounds(50, 70, Width - 100, 24);
            minuteBox.SetBounds(46, 111, 86, 56);
            colonLabel.SetBounds(128, 105, 25, 65);
            secondBox.SetBounds(150, 111, 86, 56);
            stopwatchLabel.SetBounds(25, 105, Width - 50, 70);
            unitLabel.SetBounds(50, 175, Width - 100, 22);
            base.OnLayout(levent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            PaintKit.HighQuality(e.Graphics);
            RectangleF ring = new RectangleF(10, 10, Width - 21, Height - 21);
            RectangleF inner = new RectangleF(22, 22, Width - 45, Height - 45);

            e.Graphics.DrawEllipse(PaintKit.RingTrack, ring);
            e.Graphics.FillEllipse(PaintKit.Night, inner);
            using (Pen innerRing = new Pen(Color.FromArgb(40, Palette.Accent), 1f))
                e.Graphics.DrawEllipse(innerRing, inner);

            using (Pen tick = new Pen(Color.FromArgb(80, Palette.Accent), 1f))
            {
                float cx = Width / 2f;
                float cy = Height / 2f;
                float r = Math.Min(Width, Height) / 2f;
                for (int i = 0; i < 12; i++)
                {
                    double a = (i * 30 - 90) * Math.PI / 180.0;
                    float innerR = r - (i % 3 == 0 ? 22 : 16);
                    float outerR = r - 11;
                    e.Graphics.DrawLine(tick,
                        cx + (float)(Math.Cos(a) * innerR),
                        cy + (float)(Math.Sin(a) * innerR),
                        cx + (float)(Math.Cos(a) * outerR),
                        cy + (float)(Math.Sin(a) * outerR));
                }
            }

            if (timerMode)
            {
                float sweep = (float)(Math.Max(0.0, Math.Min(1.0, timerProgress)) * 359.5);
                if (sweep > 0.4f)
                {
                    if (running) e.Graphics.DrawArc(PaintKit.RingGlow, RectangleF.Inflate(ring, 1, 1), -90f, sweep);
                    e.Graphics.DrawArc(PaintKit.RingAccent, ring, -90f, sweep);
                }
            }
            else
            {
                float angle = (float)((stopwatchSeconds % 60.0) / 60.0 * 360.0) - 90f;
                Pen active = running ? PaintKit.RingAccent : PaintKit.RingMuted;
                e.Graphics.DrawArc(active, ring, angle, 12f);
            }

            base.OnPaint(e);
        }

        private void NumericOnly(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
        }

        private void SelectTimeText(object sender, EventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void TimeBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitTimerValue(sender, EventArgs.Empty);
                Parent.Focus();
                e.SuppressKeyPress = true;
            }
        }

        private void CommitTimerValue(object sender, EventArgs e)
        {
            EventHandler handler = TimerValueCommitted;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public int TimerMinutes
        {
            get
            {
                int value;
                return int.TryParse(minuteBox.Text, out value) ? Math.Min(99, Math.Max(0, value)) : 0;
            }
        }

        public int TimerSeconds
        {
            get
            {
                int value;
                return int.TryParse(secondBox.Text, out value) ? Math.Min(59, Math.Max(0, value)) : 0;
            }
        }

        public void ShowTimer(TimeSpan remaining, TimeSpan duration, bool isRunning, bool paused, bool complete)
        {
            timerMode = true;
            running = isRunning;
            EnsureLayout(0);
            if (stopwatchLabel.Visible) stopwatchLabel.Visible = false;
            if (!minuteBox.Visible) minuteBox.Visible = true;
            if (!secondBox.Visible) secondBox.Visible = true;
            if (!colonLabel.Visible) colonLabel.Visible = true;
            bool locked = isRunning || paused;
            if (minuteBox.ReadOnly != locked) minuteBox.ReadOnly = locked;
            if (secondBox.ReadOnly != locked) secondBox.ReadOnly = locked;
            string state = complete ? "TIME'S UP" : isRunning ? "FOCUSING" : paused ? "PAUSED" : "SET TIMER";
            Color stateColor = complete ? Palette.Accent : Palette.Muted;
            Ui.SetText(stateLabel, state);
            Ui.SetColor(stateLabel, stateColor);
            int totalSeconds = (int)Math.Ceiling(Math.Max(0, remaining.TotalSeconds));
            int minutes = Math.Min(99, totalSeconds / 60);
            int seconds = totalSeconds % 60;
            string mm = minutes.ToString("00");
            string ss = seconds.ToString("00");
            if ((!minuteBox.Focused || isRunning) && lastMinuteText != mm)
            {
                minuteBox.Text = mm;
                lastMinuteText = mm;
            }
            if ((!secondBox.Focused || isRunning) && lastSecondText != ss)
            {
                secondBox.Text = ss;
                lastSecondText = ss;
            }
            timerProgress = duration.TotalMilliseconds > 0 ? remaining.TotalMilliseconds / duration.TotalMilliseconds : 0;
            bool ringChanged = Math.Abs(timerProgress - lastDrawnProgress) > 0.0015 || lastStateText != state;
            lastStateText = state;
            if (ringChanged)
            {
                lastDrawnProgress = timerProgress;
                Invalidate();
            }
        }

        public void ShowStopwatch(TimeSpan elapsed, bool isRunning)
        {
            timerMode = false;
            running = isRunning;
            EnsureLayout(1);
            if (minuteBox.Visible) minuteBox.Visible = false;
            if (secondBox.Visible) secondBox.Visible = false;
            if (colonLabel.Visible) colonLabel.Visible = false;
            if (!stopwatchLabel.Visible) stopwatchLabel.Visible = true;
            if (!ReferenceEquals(stopwatchLabel.Font, stopwatchDisplayFont)) stopwatchLabel.Font = stopwatchDisplayFont;
            string state = isRunning ? "MEASURING" : elapsed.TotalMilliseconds > 0 ? "PAUSED" : "READY";
            Ui.SetText(stateLabel, state);
            Ui.SetColor(stateLabel, Palette.Muted);
            string display = FormatStopwatch(elapsed);
            if (lastStopwatchText != display)
            {
                stopwatchLabel.Text = display;
                lastStopwatchText = display;
            }
            stopwatchSeconds = elapsed.TotalSeconds;
            double progress = (stopwatchSeconds % 60.0) / 60.0;
            bool ringChanged = Math.Abs(progress - lastDrawnProgress) > 0.002 || lastStateText != state;
            lastStateText = state;
            if (ringChanged)
            {
                lastDrawnProgress = progress;
                Invalidate();
            }
        }

        public void ShowWork(TimeSpan elapsed, bool isRunning, bool idleStopped)
        {
            timerMode = false;
            running = isRunning;
            EnsureLayout(2);
            if (minuteBox.Visible) minuteBox.Visible = false;
            if (secondBox.Visible) secondBox.Visible = false;
            if (colonLabel.Visible) colonLabel.Visible = false;
            if (!stopwatchLabel.Visible) stopwatchLabel.Visible = true;
            if (!ReferenceEquals(stopwatchLabel.Font, workDisplayFont)) stopwatchLabel.Font = workDisplayFont;
            string state = isRunning ? "WORKING" : idleStopped ? "STOPPED" : elapsed.TotalMilliseconds > 0 ? "SAVED" : "READY";
            Ui.SetText(stateLabel, state);
            Ui.SetColor(stateLabel, isRunning ? Palette.Accent : idleStopped ? Palette.DangerText : Palette.Muted);
            string display = FormatWork(elapsed);
            if (lastStopwatchText != display)
            {
                stopwatchLabel.Text = display;
                lastStopwatchText = display;
            }
            stopwatchSeconds = elapsed.TotalSeconds;
            double progress = (elapsed.TotalMinutes % 60.0) / 60.0;
            bool ringChanged = Math.Abs(progress - lastDrawnProgress) > 0.002 || lastStateText != state;
            lastStateText = state;
            if (ringChanged)
            {
                lastDrawnProgress = progress;
                Invalidate();
            }
        }

        private void EnsureLayout(int kind)
        {
            if (layoutKind == kind) return;
            layoutKind = kind;
            if (kind == 0)
            {
                unitLeftLabel.Text = "MIN";
                unitLeftLabel.SetBounds(46, 175, 86, 22);
                unitLeftLabel.Visible = true;
                unitMiddleLabel.Text = "SEC";
                unitMiddleLabel.SetBounds(150, 175, 86, 22);
                unitMiddleLabel.Visible = true;
                unitRightLabel.Visible = false;
            }
            else if (kind == 1)
            {
                unitLeftLabel.Text = "MIN";
                unitLeftLabel.SetBounds(32, 175, 72, 22);
                unitLeftLabel.Visible = true;
                unitMiddleLabel.Text = "SEC";
                unitMiddleLabel.SetBounds(116, 175, 72, 22);
                unitMiddleLabel.Visible = true;
                unitRightLabel.Text = "1/10";
                unitRightLabel.SetBounds(190, 175, 72, 22);
                unitRightLabel.Visible = true;
            }
            else
            {
                unitLeftLabel.Text = "HRS";
                unitLeftLabel.SetBounds(24, 175, 72, 22);
                unitLeftLabel.Visible = true;
                unitMiddleLabel.Text = "MIN";
                unitMiddleLabel.SetBounds(104, 175, 72, 22);
                unitMiddleLabel.Visible = true;
                unitRightLabel.Text = "SEC";
                unitRightLabel.SetBounds(184, 175, 72, 22);
                unitRightLabel.Visible = true;
            }
        }

        public static string FormatStopwatch(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1)
                return string.Format("{0:00}:{1:00}:{2:00}", (int)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds);
            return string.Format("{0:00}:{1:00}.{2}", (int)elapsed.TotalMinutes, elapsed.Seconds, elapsed.Milliseconds / 100);
        }

        public static string FormatLap(TimeSpan elapsed)
        {
            return string.Format("{0:00}:{1:00}.{2:00}", (int)elapsed.TotalMinutes, elapsed.Seconds, elapsed.Milliseconds / 10);
        }

        public static string FormatWork(TimeSpan elapsed)
        {
            return string.Format("{0:00}:{1:00}:{2:00}", (int)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds);
        }

        public static string FormatOverlayTimer(TimeSpan remaining)
        {
            int total = (int)Math.Ceiling(Math.Max(0, remaining.TotalSeconds));
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            int seconds = total % 60;
            if (hours > 0) return hours + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
            return minutes + ":" + seconds.ToString("00");
        }

        public static string FormatOverlayStopwatch(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1)
                return ((int)elapsed.TotalHours) + ":" + elapsed.Minutes.ToString("00") + ":" + elapsed.Seconds.ToString("00");
            if (elapsed.TotalMinutes >= 1)
                return ((int)elapsed.TotalMinutes) + ":" + elapsed.Seconds.ToString("00");
            return elapsed.Seconds + "." + (elapsed.Milliseconds / 100);
        }

        public static string FormatOverlayWork(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1)
                return ((int)elapsed.TotalHours) + ":" + elapsed.Minutes.ToString("00") + ":" + elapsed.Seconds.ToString("00");
            if (elapsed.TotalMinutes >= 1)
                return ((int)elapsed.TotalMinutes) + ":" + elapsed.Seconds.ToString("00");
            return elapsed.Seconds.ToString();
        }
    }

    internal enum WidgetDock
    {
        Free,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    internal class WidgetMoonForm : Form
    {
        public const int SizePx = 36;
        private static readonly Color Shell = Color.FromArgb(8, 12, 22);
        private static readonly Color Edge = Color.FromArgb(3, 5, 9);
        private readonly bool pauseMoon;
        private bool running = true;
        private bool endArmed;
        private DateTime endArmedUntil;
        private bool hovered;
        public event EventHandler PauseClicked;
        public event EventHandler EndConfirmed;

        public WidgetMoonForm(bool pause)
        {
            pauseMoon = pause;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(SizePx, SizePx);
            BackColor = Shell;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            MouseEnter += delegate { hovered = true; Invalidate(); };
            MouseLeave += delegate { hovered = false; Invalidate(); };
            Click += OnMoonClick;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080;
                cp.ExStyle |= 0x08000000;
                return cp;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(0, 0, Width, Height);
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Edge);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color tint = pauseMoon ? Palette.Accent : Palette.MoonRed;
            bool lit = hovered || endArmed;
            if (endArmed) tint = Palette.MoonRedHot;

            // Dark body with a colored rim; hovering (or an armed end) floods the disc.
            Color body = lit ? tint : Shell;
            Color icon = lit ? Color.FromArgb(10, 14, 20) : tint;

            RectangleF disc = new RectangleF(1.5f, 1.5f, Width - 4f, Height - 4f);
            using (SolidBrush fill = new SolidBrush(body))
                g.FillEllipse(fill, disc);
            using (Pen rim = new Pen(lit ? Color.FromArgb(90, 255, 255, 255) : tint, 1.5f))
                g.DrawEllipse(rim, disc);

            float cx = Width / 2f;
            float cy = Height / 2f;
            using (Pen ink = new Pen(icon, 2.4f))
            {
                ink.StartCap = LineCap.Round;
                ink.EndCap = LineCap.Round;
                ink.LineJoin = LineJoin.Round;
                if (pauseMoon && running)
                {
                    g.DrawLine(ink, cx - 4f, cy - 6f, cx - 4f, cy + 6f);
                    g.DrawLine(ink, cx + 4f, cy - 6f, cx + 4f, cy + 6f);
                }
                else if (pauseMoon)
                {
                    PointF[] play = new PointF[]
                    {
                        new PointF(cx - 4.5f, cy - 6.5f),
                        new PointF(cx - 4.5f, cy + 6.5f),
                        new PointF(cx + 7f, cy)
                    };
                    using (SolidBrush b = new SolidBrush(icon))
                        g.FillPolygon(b, play);
                    using (Pen outline = new Pen(icon, 1.6f))
                    {
                        outline.LineJoin = LineJoin.Round;
                        g.DrawPolygon(outline, play);
                    }
                }
                else
                {
                    g.DrawLine(ink, cx - 4.5f, cy - 4.5f, cx + 4.5f, cy + 4.5f);
                    g.DrawLine(ink, cx + 4.5f, cy - 4.5f, cx - 4.5f, cy + 4.5f);
                }
            }
        }

        public void SetRunning(bool isRunning)
        {
            if (running == isRunning) return;
            running = isRunning;
            Invalidate();
        }

        public void ClearEndArm()
        {
            if (!endArmed) return;
            endArmed = false;
            Invalidate();
        }

        private void OnMoonClick(object sender, EventArgs e)
        {
            if (pauseMoon)
            {
                EventHandler handler = PauseClicked;
                if (handler != null) handler(this, EventArgs.Empty);
                return;
            }
            if (!endArmed || DateTime.UtcNow > endArmedUntil)
            {
                endArmed = true;
                endArmedUntil = DateTime.UtcNow.AddSeconds(3);
                Invalidate();
                return;
            }
            endArmed = false;
            Invalidate();
            EventHandler confirmed = EndConfirmed;
            if (confirmed != null) confirmed(this, EventArgs.Empty);
        }
    }

    internal class WidgetForm : Form
    {
        private const int CornerSize = 96;
        private const int FreeSize = 120;
        private const int SnapPx = 72;
        private readonly string layoutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tempo",
            "widget.dat");

        private readonly Form mainWindow;
        private readonly Label hourLabel;
        private readonly Label timeLabel;
        private readonly Font freeFont;
        private readonly Font dockFont;
        private readonly Font hourFont;
        private double progress;
        private double lastProgress = -1;
        private int overlayHours;
        private string overlayMain = "25:00";
        private WidgetDock dock = WidgetDock.TopRight;
        private bool placed;
        private bool dragging;
        private Point dragGrab;
        private Point dragOrigin;
        private bool movedDuringDrag;
        private readonly WidgetMoonForm pauseMoon;
        private readonly WidgetMoonForm endMoon;
        private readonly Timer hoverTimer;
        private bool actionsShown;

        public event EventHandler PauseRequested;
        public event EventHandler EndRequested;

        public WidgetForm(Form main)
        {
            mainWindow = main;
            Text = "Tempo Mini";
            AccessibleName = "Tempo circular overlay. Drag to move. Click to open Tempo.";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(CornerSize, CornerSize);
            BackColor = Palette.Night;
            ForeColor = Palette.Ink;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            Cursor = Cursors.SizeAll;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            freeFont = new Font("Consolas", 18f, FontStyle.Bold, GraphicsUnit.Point);
            dockFont = new Font("Consolas", 16f, FontStyle.Bold, GraphicsUnit.Point);
            hourFont = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);

            hourLabel = MakeTimeLabel(hourFont, Palette.Ink);
            hourLabel.Visible = false;
            Controls.Add(hourLabel);

            timeLabel = MakeTimeLabel(dockFont, Palette.Ink);
            timeLabel.Text = "25:00";
            timeLabel.Visible = false;
            Controls.Add(timeLabel);

            BindDrag(this);
            BindDrag(hourLabel);
            BindDrag(timeLabel);

            pauseMoon = new WidgetMoonForm(true);
            pauseMoon.PauseClicked += delegate
            {
                endMoon.ClearEndArm();
                EventHandler handler = PauseRequested;
                if (handler != null) handler(this, EventArgs.Empty);
            };
            endMoon = new WidgetMoonForm(false);
            endMoon.EndConfirmed += delegate
            {
                EventHandler handler = EndRequested;
                if (handler != null) handler(this, EventArgs.Empty);
            };

            hoverTimer = new Timer();
            hoverTimer.Interval = 40;
            hoverTimer.Tick += PollHover;
            VisibleChanged += delegate
            {
                if (Visible) hoverTimer.Start();
                else
                {
                    hoverTimer.Stop();
                    HideActions();
                }
            };

            LoadLayout();
            ApplyTimeLayout();
        }

        private Label MakeTimeLabel(Font font, Color color)
        {
            Label label = new Label();
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            label.ForeColor = color;
            label.Font = font;
            label.TextAlign = ContentAlignment.MiddleCenter;
            return label;
        }

        private void BindDrag(Control control)
        {
            control.MouseDown += BeginDrag;
            control.MouseMove += DragMove;
            control.MouseUp += EndDrag;
            control.DoubleClick += OpenMainWindow;
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080;
                cp.ExStyle |= 0x08000000;
                return cp;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyShape();
        }

        private void ApplyShape()
        {
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = BuildShape())
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        private GraphicsPath BuildShape()
        {
            GraphicsPath path = new GraphicsPath();
            if (dock == WidgetDock.Free)
            {
                path.AddEllipse(0, 0, Width - 1, Height - 1);
                return path;
            }
            Rectangle pie = CircleBounds();
            path.AddPie(pie, PieStart(), 90f);
            return path;
        }

        private Rectangle CircleBounds()
        {
            if (dock == WidgetDock.Free) return new Rectangle(1, 1, Width - 3, Height - 3);
            int r = Width;
            switch (dock)
            {
                case WidgetDock.TopRight: return new Rectangle(0, -r, r * 2, r * 2);
                case WidgetDock.TopLeft: return new Rectangle(-r, -r, r * 2, r * 2);
                case WidgetDock.BottomRight: return new Rectangle(0, 0, r * 2, r * 2);
                default: return new Rectangle(-r, 0, r * 2, r * 2);
            }
        }

        private float PieStart()
        {
            switch (dock)
            {
                case WidgetDock.TopRight: return 90f;
                case WidgetDock.TopLeft: return 0f;
                case WidgetDock.BottomRight: return 180f;
                case WidgetDock.BottomLeft: return 270f;
                default: return 0f;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            PaintKit.HighQuality(e.Graphics);
            e.Graphics.Clear(Palette.Night);
            using (GraphicsPath path = BuildShape())
                e.Graphics.FillPath(PaintKit.WidgetShell, path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PaintKit.HighQuality(e.Graphics);
            Rectangle ring = CircleBounds();
            ring.Inflate(-5, -5);
            double clamped = Math.Max(0.0, Math.Min(1.0, progress));
            if (dock == WidgetDock.Free)
            {
                e.Graphics.DrawEllipse(PaintKit.RingTrack, ring);
                float sweep = (float)(clamped * 359.5);
                if (sweep > 0.6f) e.Graphics.DrawArc(PaintKit.RingAccent, ring, -90f, sweep);
            }
            else
            {
                // Only a quarter of the circle is on-screen when docked, so the whole
                // progress range is mapped onto that visible 90° edge-to-edge.
                float start = PieStart();
                e.Graphics.DrawArc(PaintKit.RingTrack, ring, start, 90f);
                float sweep = (float)(clamped * 90.0);
                if (sweep > 0.3f) e.Graphics.DrawArc(PaintKit.RingAccent, ring, start, sweep);
            }
            DrawOverlayTime(e.Graphics);
        }

        private Point CircleOrigin()
        {
            int r = Width;
            switch (dock)
            {
                case WidgetDock.TopRight: return new Point(r, 0);
                case WidgetDock.TopLeft: return new Point(0, 0);
                case WidgetDock.BottomRight: return new Point(r, r);
                case WidgetDock.BottomLeft: return new Point(0, r);
                default: return new Point(Width / 2, Height / 2);
            }
        }

        private Point VisibleTextCenter()
        {
            int yNudge = overlayHours > 0 ? 8 : 0;
            if (dock == WidgetDock.Free)
                return new Point(Width / 2, Height / 2 + yNudge);

            // Quarter-disk visual center: along the pie bisector, at the area centroid.
            double angle = (PieStart() + 45.0) * Math.PI / 180.0;
            float dist = Width * 0.60f;
            Point origin = CircleOrigin();
            return new Point(
                (int)Math.Round(origin.X + dist * Math.Cos(angle)),
                (int)Math.Round(origin.Y + dist * Math.Sin(angle) + yNudge));
        }

        private void DrawOverlayTime(Graphics g)
        {
            Point center = VisibleTextCenter();
            Font timeFont = dock == WidgetDock.Free ? freeFont : dockFont;
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;

            if (overlayHours > 0)
            {
                Rectangle hourBox = new Rectangle(center.X - 28, center.Y - 28, 56, 16);
                TextRenderer.DrawText(g, overlayHours + "H", hourFont, hourBox, Palette.Muted, flags);
            }

            Rectangle timeBox = new Rectangle(center.X - 48, center.Y - 16, 96, 32);
            TextRenderer.DrawText(g, overlayMain, timeFont, timeBox, Palette.Ink, flags);
        }

        public void ShowTimer(TimeSpan remaining, TimeSpan duration)
        {
            int total = (int)Math.Ceiling(Math.Max(0, remaining.TotalSeconds));
            Present(total / 3600, Pad((total % 3600) / 60) + ":" + Pad(total % 60),
                duration.TotalMilliseconds > 0 ? remaining.TotalMilliseconds / duration.TotalMilliseconds : 0);
        }

        public void ShowStopwatch(TimeSpan elapsed)
        {
            Present((int)elapsed.TotalHours, Pad(elapsed.Minutes) + ":" + Pad(elapsed.Seconds),
                (elapsed.TotalSeconds % 60.0) / 60.0);
        }

        public void ShowWork(TimeSpan elapsed)
        {
            Present((int)elapsed.TotalHours, Pad(elapsed.Minutes) + ":" + Pad(elapsed.Seconds),
                (elapsed.TotalMinutes % 60.0) / 60.0);
        }

        private static string Pad(int value)
        {
            return value.ToString("00");
        }

        public void HideWidget()
        {
            HideActions();
            if (Visible) Hide();
        }

        public void SetRunning(bool running)
        {
            pauseMoon.SetRunning(running);
        }

        private void Present(int hours, string main, double nextProgress)
        {
            overlayHours = hours;
            overlayMain = main;
            bool textChanged = RefreshTimeText();
            bool progressChanged = Math.Abs(nextProgress - lastProgress) > 0.004;
            progress = nextProgress;
            lastProgress = nextProgress;
            if (!placed)
            {
                ApplyDock(dock, HomeScreen());
                placed = true;
            }
            if (progressChanged || textChanged) Invalidate();
            if (!Visible) Show();
        }

        private bool RefreshTimeText()
        {
            hourLabel.Visible = false;
            timeLabel.Visible = false;
            return true;
        }

        private Screen HomeScreen()
        {
            return Screen.FromControl(mainWindow) ?? Screen.PrimaryScreen;
        }

        private Screen ScreenFromCursor()
        {
            return Screen.FromPoint(Cursor.Position) ?? Screen.PrimaryScreen;
        }

        private void ApplyTimeLayout()
        {
            Invalidate();
        }

        private void SetWidgetSize(int size)
        {
            if (ClientSize.Width == size && ClientSize.Height == size) return;
            ClientSize = new Size(size, size);
        }

        private void ApplyDock(WidgetDock next, Screen screen)
        {
            dock = next;
            Rectangle b = screen.Bounds;
            if (dock == WidgetDock.Free)
            {
                SetWidgetSize(FreeSize);
                ClampToScreen(screen);
            }
            else
            {
                SetWidgetSize(CornerSize);
                switch (dock)
                {
                    case WidgetDock.TopRight:
                        Location = new Point(b.Right - CornerSize, b.Top);
                        break;
                    case WidgetDock.TopLeft:
                        Location = new Point(b.Left, b.Top);
                        break;
                    case WidgetDock.BottomRight:
                        Location = new Point(b.Right - CornerSize, b.Bottom - CornerSize);
                        break;
                    case WidgetDock.BottomLeft:
                        Location = new Point(b.Left, b.Bottom - CornerSize);
                        break;
                }
            }
            ApplyShape();
            ApplyTimeLayout();
            SaveLayout();
        }

        private WidgetDock DetectDock(Point location, Screen screen)
        {
            Rectangle b = screen.Bounds;
            int size = dock == WidgetDock.Free ? FreeSize : CornerSize;
            bool nearLeft = location.X <= b.Left + SnapPx;
            bool nearRight = location.X + size >= b.Right - SnapPx;
            bool nearTop = location.Y <= b.Top + SnapPx;
            bool nearBottom = location.Y + size >= b.Bottom - SnapPx;
            if (nearTop && nearRight) return WidgetDock.TopRight;
            if (nearTop && nearLeft) return WidgetDock.TopLeft;
            if (nearBottom && nearRight) return WidgetDock.BottomRight;
            if (nearBottom && nearLeft) return WidgetDock.BottomLeft;
            return WidgetDock.Free;
        }

        private void BeginDrag(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            HideActions();
            dragging = true;
            movedDuringDrag = false;
            dragGrab = e.Location;
            if (sender != this && sender is Control)
            {
                Control child = (Control)sender;
                dragGrab = new Point(e.X + child.Left, e.Y + child.Top);
            }
            dragOrigin = Location;
            Capture = true;
        }

        private void DragMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            Point cursor = Cursor.Position;
            Point next = new Point(cursor.X - dragGrab.X, cursor.Y - dragGrab.Y);
            if (Math.Abs(next.X - dragOrigin.X) + Math.Abs(next.Y - dragOrigin.Y) > 4)
                movedDuringDrag = true;

            Screen screen = ScreenFromCursor();
            if (dock != WidgetDock.Free)
            {
                dock = WidgetDock.Free;
                SetWidgetSize(FreeSize);
                dragGrab = new Point(dragGrab.X + (FreeSize / 2 - CornerSize / 2), dragGrab.Y + (FreeSize / 2 - CornerSize / 2));
                next = new Point(cursor.X - dragGrab.X, cursor.Y - dragGrab.Y);
                ApplyShape();
                ApplyTimeLayout();
            }

            Location = ClampPoint(next, screen);
        }

        private void EndDrag(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            dragging = false;
            Capture = false;
            if (!movedDuringDrag)
            {
                OpenMain();
                return;
            }
            Screen screen = ScreenFromCursor();
            WidgetDock next = DetectDock(Location, screen);
            ApplyDock(next, screen);
        }

        private Point ClampPoint(Point location, Screen screen)
        {
            Rectangle b = screen.Bounds;
            int x = Math.Max(b.Left, Math.Min(location.X, b.Right - Width));
            int y = Math.Max(b.Top, Math.Min(location.Y, b.Bottom - Height));
            return new Point(x, y);
        }

        private void ClampToScreen(Screen screen)
        {
            Location = ClampPoint(Location, screen);
        }

        private void LoadLayout()
        {
            try
            {
                if (!File.Exists(layoutPath)) return;
                string[] parts = File.ReadAllText(layoutPath).Trim().Split('|');
                if (parts.Length < 1) return;
                int stored;
                if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out stored)) return;
                if (stored < 0 || stored > 4) return;
                dock = (WidgetDock)stored;
                if (dock == WidgetDock.Free && parts.Length >= 3)
                {
                    int x, y;
                    if (int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out x)
                        && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out y))
                    {
                        Location = new Point(x, y);
                        ClampToScreen(Screen.FromPoint(Location) ?? Screen.PrimaryScreen);
                    }
                }
            }
            catch
            {
                dock = WidgetDock.TopRight;
            }
        }

        private void SaveLayout()
        {
            try
            {
                string folder = Path.GetDirectoryName(layoutPath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                File.WriteAllText(layoutPath, ((int)dock).ToString(CultureInfo.InvariantCulture)
                    + "|" + Location.X.ToString(CultureInfo.InvariantCulture)
                    + "|" + Location.Y.ToString(CultureInfo.InvariantCulture));
            }
            catch
            {
            }
        }

        private void PollHover(object sender, EventArgs e)
        {
            if (!Visible || dragging)
            {
                HideActions();
                return;
            }
            Point cursor = Cursor.Position;
            bool onCircle = NearShape(cursor, 8);
            bool onButtons = actionsShown && (NearMoon(pauseMoon, cursor, 10) || NearMoon(endMoon, cursor, 10));
            bool stillNear = NearShape(cursor, 56) || NearMoon(pauseMoon, cursor, 40) || NearMoon(endMoon, cursor, 40);
            if (!actionsShown && onCircle) ShowActions();
            else if (actionsShown && !onCircle && !onButtons && !stillNear) HideActions();
            else if (actionsShown) PlaceActions();
        }

        private bool NearShape(Point screen, int extra)
        {
            Point client = PointToClient(screen);
            using (GraphicsPath path = BuildShape())
            {
                if (path.IsVisible(client)) return true;
                if (extra <= 0) return false;
                using (Pen pen = new Pen(Color.Black, Math.Max(2, extra * 2)))
                {
                    pen.LineJoin = LineJoin.Round;
                    try { path.Widen(pen); }
                    catch { return false; }
                    return path.IsVisible(client);
                }
            }
        }

        private static bool NearMoon(Form moon, Point screen, int extra)
        {
            Rectangle bounds = new Rectangle(moon.Location, moon.Size);
            bounds.Inflate(extra, extra);
            return bounds.Contains(screen);
        }

        private void ShowActions()
        {
            actionsShown = true;
            endMoon.ClearEndArm();
            PlaceActions();
            if (!pauseMoon.Visible) pauseMoon.Show(this);
            if (!endMoon.Visible) endMoon.Show(this);
        }

        private void HideActions()
        {
            if (!actionsShown && !pauseMoon.Visible && !endMoon.Visible) return;
            actionsShown = false;
            endMoon.ClearEndArm();
            if (pauseMoon.Visible) pauseMoon.Hide();
            if (endMoon.Visible) endMoon.Hide();
        }

        private void PlaceActions()
        {
            Point planet = PointToScreen(CircleOrigin());
            float planetR = dock == WidgetDock.Free ? Width / 2f : Width;
            float orbit = planetR + WidgetMoonForm.SizePx / 2f + 3f;
            float spread = dock == WidgetDock.Free ? 22f : 20f;
            float mid;
            switch (dock)
            {
                case WidgetDock.TopLeft:
                    mid = 45f;
                    break;
                case WidgetDock.TopRight:
                    mid = 135f;
                    break;
                case WidgetDock.BottomRight:
                    mid = 225f;
                    break;
                case WidgetDock.BottomLeft:
                    mid = 315f;
                    break;
                default:
                    mid = 63f;
                    break;
            }
            PlaceMoon(pauseMoon, planet, orbit, mid - spread);
            PlaceMoon(endMoon, planet, orbit, mid + spread);
        }

        private void PlaceMoon(Form moon, Point planet, float orbit, float degrees)
        {
            double rad = degrees * Math.PI / 180.0;
            int x = (int)Math.Round(planet.X + orbit * Math.Cos(rad) - WidgetMoonForm.SizePx / 2.0);
            int y = (int)Math.Round(planet.Y + orbit * Math.Sin(rad) - WidgetMoonForm.SizePx / 2.0);
            Screen screen = Screen.FromPoint(planet) ?? Screen.PrimaryScreen;
            Rectangle b = screen.Bounds;
            x = Math.Max(b.Left, Math.Min(x, b.Right - WidgetMoonForm.SizePx));
            y = Math.Max(b.Top, Math.Min(y, b.Bottom - WidgetMoonForm.SizePx));
            moon.Location = new Point(x, y);
        }

        private void OpenMainWindow(object sender, EventArgs e)
        {
            OpenMain();
        }

        private void OpenMain()
        {
            if (mainWindow.WindowState == FormWindowState.Minimized)
                mainWindow.WindowState = FormWindowState.Normal;
            mainWindow.Show();
            mainWindow.Activate();
            mainWindow.BringToFront();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            hoverTimer.Stop();
            hoverTimer.Dispose();
            HideActions();
            pauseMoon.Close();
            pauseMoon.Dispose();
            endMoon.Close();
            endMoon.Dispose();
            base.OnFormClosed(e);
        }
    }

    internal static class WorkIdle
    {
        private const int eRender = 0;
        private const int eConsole = 0;
        private const int ClsctxAll = 23;
        private static readonly Guid MeterId = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064");

        [StructLayout(LayoutKind.Sequential)]
        private struct PointApi
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out PointApi point);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator { }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig]
            int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr devices);
            [PreserveSig]
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
            [PreserveSig]
            int GetDevice(string id, out IMMDevice device);
            [PreserveSig]
            int RegisterEndpointNotificationCallback(IntPtr client);
            [PreserveSig]
            int UnregisterEndpointNotificationCallback(IntPtr client);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int clsCtx, IntPtr activationParams,
                [MarshalAs(UnmanagedType.IUnknown)] out object created);
            [PreserveSig]
            int OpenPropertyStore(int access, out IntPtr properties);
            [PreserveSig]
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
            [PreserveSig]
            int GetState(out int state);
        }

        [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioMeterInformation
        {
            [PreserveSig]
            int GetPeakValue(out float peak);
        }

        private static PointApi lastCursor;
        private static bool haveCursor;
        private static object enumerator;
        private static object meter;

        public static void RememberCursor()
        {
            PointApi point;
            if (GetCursorPos(out point))
            {
                lastCursor = point;
                haveCursor = true;
            }
        }

        public static bool MouseMoved()
        {
            PointApi point;
            if (!GetCursorPos(out point)) return false;
            if (!haveCursor)
            {
                lastCursor = point;
                haveCursor = true;
                return false;
            }
            bool moved = point.X != lastCursor.X || point.Y != lastCursor.Y;
            lastCursor = point;
            return moved;
        }

        public static void RememberKeyboard()
        {
            for (int vk = 8; vk <= 254; vk++)
                GetAsyncKeyState(vk);
        }

        public static bool KeyboardUsed()
        {
            for (int vk = 8; vk <= 254; vk++)
            {
                if ((GetAsyncKeyState(vk) & 0x8001) != 0)
                    return true;
            }
            return false;
        }

        public static bool SoundPlaying()
        {
            try
            {
                IAudioMeterInformation info = Meter();
                if (info == null) return false;
                float peak;
                if (info.GetPeakValue(out peak) != 0)
                {
                    DropMeter();
                    return false;
                }
                return peak >= 0.02f;
            }
            catch
            {
                DropMeter();
                return false;
            }
        }

        private static IAudioMeterInformation Meter()
        {
            if (meter != null) return (IAudioMeterInformation)meter;
            if (enumerator == null)
                enumerator = new MMDeviceEnumerator();
            IMMDeviceEnumerator devices = (IMMDeviceEnumerator)enumerator;
            IMMDevice device;
            if (devices.GetDefaultAudioEndpoint(eRender, eConsole, out device) != 0 || device == null)
                return null;
            Guid iid = MeterId;
            object created;
            if (device.Activate(ref iid, ClsctxAll, IntPtr.Zero, out created) != 0 || created == null)
            {
                Marshal.ReleaseComObject(device);
                return null;
            }
            Marshal.ReleaseComObject(device);
            meter = created;
            return (IAudioMeterInformation)meter;
        }

        private static void DropMeter()
        {
            if (meter != null)
            {
                try { Marshal.ReleaseComObject(meter); } catch { }
                meter = null;
            }
        }
    }

    internal class SavedWorkTimer
    {
        public string Id;
        public string Name;
        public TimeSpan Elapsed;
        public DateTime SavedUtc;
        public readonly List<WorkSessionRecord> Sessions = new List<WorkSessionRecord>();
    }

    internal class WorkSessionRecord
    {
        public string Id;
        public string Name;
        public TimeSpan Elapsed;
        public DateTime CreatedUtc;
        public DateTime UpdatedUtc;
    }

    internal class SaveTimerDialog : Form
    {
        private readonly TextBox nameBox;
        private readonly SoftPanel inputShell;

        public string TimerName { get; private set; }

        public SaveTimerDialog(string suggestedName, TimeSpan elapsed, bool renameSession = false)
        {
            Text = renameSession ? "Rename session" : "Save work timer";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(430, 260);
            BackColor = Palette.Card;
            ForeColor = Palette.Ink;
            AutoScaleMode = AutoScaleMode.None;
            KeyPreview = true;
            DoubleBuffered = true;

            SoftPanel logo = new SoftPanel();
            logo.Radius = 6;
            logo.FillColor = Palette.Accent;
            logo.SetBounds(24, 22, 30, 30);
            logo.Paint += PaintLogo;
            Controls.Add(logo);

            Label brand = MakeLabel(renameSession ? "RENAME SESSION" : "SAVE WORK TIMER", 8f, FontStyle.Bold, Palette.Accent);
            brand.SetBounds(65, 24, 170, 25);
            Controls.Add(brand);

            SoftPanel timePill = new SoftPanel();
            timePill.Radius = 8;
            timePill.FillColor = Palette.Surface;
            timePill.DrawBorder = true;
            timePill.SetBounds(276, 21, 94, 34);
            Controls.Add(timePill);

            Label time = MakeLabel(DialControl.FormatWork(elapsed), 9f, FontStyle.Bold, Palette.Ink);
            time.Font = new Font("Consolas", 9f, FontStyle.Bold);
            time.TextAlign = ContentAlignment.MiddleCenter;
            time.Dock = DockStyle.Fill;
            timePill.Controls.Add(time);

            TempoButton close = new TempoButton();
            close.Text = "×";
            close.Radius = 8;
            close.Font = new Font("Segoe UI", 11f, FontStyle.Regular);
            close.SetBounds(377, 22, 30, 30);
            close.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(close);

            Label heading = MakeLabel(renameSession ? "Name this session" : "Name this work save", 18f, FontStyle.Bold, Palette.Ink);
            heading.SetBounds(24, 72, 360, 34);
            Controls.Add(heading);

            Label helper = MakeLabel(renameSession ? "This name appears inside its work save." : "Sessions will be collected inside this save.", 8.8f, FontStyle.Regular, Palette.Muted);
            helper.SetBounds(25, 106, 360, 22);
            Controls.Add(helper);

            Label inputLabel = MakeLabel("TIMER NAME", 7.2f, FontStyle.Bold, Color.FromArgb(112, 125, 113));
            inputLabel.Text = renameSession ? "SESSION NAME" : "SAVE NAME";
            inputLabel.SetBounds(25, 133, 120, 18);
            Controls.Add(inputLabel);

            inputShell = new SoftPanel();
            inputShell.Radius = 8;
            inputShell.FillColor = Palette.Surface;
            inputShell.DrawBorder = true;
            inputShell.BorderColor = Palette.Faint;
            inputShell.SetBounds(24, 152, 382, 44);
            Controls.Add(inputShell);

            nameBox = new TextBox();
            nameBox.Text = suggestedName;
            nameBox.AccessibleName = "Saved timer name";
            nameBox.BackColor = Palette.Surface;
            nameBox.ForeColor = Palette.Ink;
            nameBox.BorderStyle = BorderStyle.None;
            nameBox.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            nameBox.SetBounds(14, 11, 354, 25);
            nameBox.Enter += delegate { inputShell.BorderColor = Palette.Accent; inputShell.Invalidate(); };
            nameBox.Leave += delegate { inputShell.BorderColor = Palette.Faint; inputShell.Invalidate(); };
            inputShell.Controls.Add(nameBox);

            TempoButton save = new TempoButton();
            save.Text = renameSession ? "Save name" : "Create save";
            save.Primary = true;
            save.Radius = 8;
            save.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            save.SetBounds(306, 211, 100, 38);
            save.Click += delegate { AcceptName(); };
            Controls.Add(save);

            TempoButton cancel = new TempoButton();
            cancel.Text = "Cancel";
            cancel.Radius = 8;
            cancel.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            cancel.SetBounds(214, 211, 84, 38);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            Label keyboardHint = MakeLabel("ENTER  save     ESC  cancel", 7.5f, FontStyle.Regular, Color.FromArgb(87, 99, 89));
            keyboardHint.Font = new Font("Consolas", 7.5f, FontStyle.Regular);
            keyboardHint.SetBounds(25, 216, 175, 24);
            Controls.Add(keyboardHint);

            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    AcceptName();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            using (GraphicsPath path = Shapes.Rounded(new Rectangle(0, 0, Width, Height), 12))
                Region = new Region(path);
            nameBox.Focus();
            nameBox.SelectAll();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            PaintKit.HighQuality(e.Graphics);
            e.Graphics.Clear(Palette.Card);
            e.Graphics.FillEllipse(PaintKit.DialogGlow, 302, -70, 200, 180);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PaintKit.HighQuality(e.Graphics);
            using (GraphicsPath path = Shapes.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 12))
                e.Graphics.DrawPath(PaintKit.Hairline, path);
        }

        private void PaintLogo(object sender, PaintEventArgs e)
        {
            PaintKit.HighQuality(e.Graphics);
            e.Graphics.DrawLine(PaintKit.Logo, 12, 9, 12, 21);
            e.Graphics.DrawLine(PaintKit.Logo, 18, 9, 18, 21);
        }

        private Label MakeLabel(string text, float size, FontStyle style, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            label.ForeColor = color;
            label.Font = new Font("Segoe UI", size, style, GraphicsUnit.Point);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private void AcceptName()
        {
            string value = nameBox.Text.Trim();
            if (value.Length == 0)
            {
                nameBox.Focus();
                return;
            }
            if (value.Length > 48) value = value.Substring(0, 48).Trim();
            TimerName = value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal class TempoForm : Form
    {
        private enum Mode { Timer, Stopwatch, Work, Settings }

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hWnd, bool invert);

        private readonly CardPanel card;
        private readonly SoftPanel modeSwitch;
        private readonly TempoButton timerTab;
        private readonly TempoButton stopwatchTab;
        private readonly TempoButton workTab;
        private readonly TempoButton settingsTab;
        private readonly Panel settingsPanel;
        private readonly TempoSwitch idleAutoStopSwitch;
        private readonly TempoSwitch idleMouseSwitch;
        private readonly TempoSwitch idleKeyboardSwitch;
        private readonly TempoSwitch idleSoundSwitch;
        private readonly TempoSwitch overlaySwitch;
        private readonly TempoButton[] idleMinuteButtons;
        private readonly DialControl dial;
        private readonly TempoButton primaryButton;
        private readonly TempoButton resetButton;
        private readonly TempoButton addMinuteButton;
        private readonly TempoButton saveWorkButton;
        private readonly TempoButton trimWorkButton;
        private readonly SoftPanel trimBar;
        private readonly TempoButton[] trimButtons;
        private readonly TempoButton lapButton;
        private readonly TempoButton[] presetButtons;
        private readonly Panel presetsPanel;
        private readonly Panel lapChipsPanel;
        private readonly SoftPanel[] lapChips;
        private readonly Label[] lapChipTitles;
        private readonly Label[] lapChipValues;
        private readonly Label lapsInfoLabel;
        private readonly Label workInfoLabel;
        private readonly CardPanel savedTimersPanel;
        private readonly FlowLayoutPanel savedTimerRows;
        private readonly Label savedTimersEmpty;
        private readonly Label savedHeading;
        private readonly Label savedHelper;
        private readonly Label savedTimersCount;
        private readonly TempoButton sessionsBackButton;
        private readonly TempoButton savedTimersButton;
        private readonly Label liveStatus;
        private readonly Label shortcutLabel;
        private readonly Timer updateTimer;
        private readonly WidgetForm widget;

        private Mode mode = Mode.Timer;
        private TimeSpan timerDuration = TimeSpan.FromMinutes(25);
        private TimeSpan timerRemaining = TimeSpan.FromMinutes(25);
        private DateTime timerEndsAt;
        private bool timerRunning;
        private bool timerHasStarted;
        private bool timerComplete;

        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly List<TimeSpan> laps = new List<TimeSpan>();
        private readonly Stopwatch workSession = new Stopwatch();
        private TimeSpan workAccumulated = TimeSpan.Zero;
        private readonly List<SavedWorkTimer> savedWorkTimers = new List<SavedWorkTimer>();
        private string activeSavedTimerId;
        private string activeWorkSessionId;
        private SavedWorkTimer viewedSavedTimer;
        private bool savedTimersOpen;
        private DateTime nextWorkAutoSave = DateTime.MinValue;
        private DateTime workIdleSince = DateTime.MinValue;
        private bool workIdleStopped;
        private Bitmap backgroundCache;
        private readonly string workStatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tempo",
            "worktime.dat");
        private readonly string savedTimersPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tempo",
            "savedtimers.dat");
        private readonly string sessionsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tempo",
            "sessions.dat");
        private readonly string settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tempo",
            "settings.dat");

        private bool idleAutoStop = true;
        private bool idleWatchMouse = true;
        private bool idleWatchKeyboard = true;
        private bool idleWatchSound = true;
        private int idleMinutes = 3;
        private bool showOverlay = true;

        public TempoForm()
        {
            Text = "Tempo — Timer, Stopwatch & Work Tracker";
            AccessibleName = "Tempo timer, stopwatch, and work tracker";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            FormBorderStyle = FormBorderStyle.None;
            ClientSize = new Size(520, 760);
            AutoScaleMode = AutoScaleMode.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Palette.Canvas;
            ForeColor = Palette.Ink;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
            KeyPreview = true;
            DoubleBuffered = true;

            Label brand = MakeLabel("TEMPO", 15f, FontStyle.Bold, Palette.Ink);
            brand.SetBounds(54, 24, 100, 30);
            Controls.Add(brand);

            Panel logo = new SoftPanel();
            ((SoftPanel)logo).Radius = 6;
            ((SoftPanel)logo).FillColor = Palette.Accent;
            logo.SetBounds(20, 24, 28, 28);
            logo.Paint += PaintLogo;
            Controls.Add(logo);

            liveStatus = MakeLabel("●  Take your time", 8.5f, FontStyle.Regular, Palette.Muted);
            liveStatus.TextAlign = ContentAlignment.MiddleRight;
            liveStatus.AutoEllipsis = true;
            liveStatus.SetBounds(274, 23, 145, 30);
            Controls.Add(liveStatus);

            TempoButton minimize = WindowButton("—");
            minimize.SetBounds(432, 20, 34, 34);
            minimize.Click += delegate { WindowState = FormWindowState.Minimized; };
            Controls.Add(minimize);

            TempoButton close = WindowButton("×");
            close.Font = new Font("Segoe UI", 13f, FontStyle.Regular);
            close.SetBounds(470, 20, 34, 34);
            close.Click += delegate { Close(); };
            Controls.Add(close);

            Label heading = MakeLabel("Neo city clock.", 22f, FontStyle.Bold, Palette.Ink);
            heading.TextAlign = ContentAlignment.MiddleCenter;
            heading.SetBounds(20, 70, 480, 42);
            Controls.Add(heading);

            Label subtitle = MakeLabel("Timer, stopwatch, and work — dark, simple, precise.", 9.2f, FontStyle.Regular, Palette.Muted);
            subtitle.TextAlign = ContentAlignment.MiddleCenter;
            subtitle.SetBounds(20, 111, 480, 25);
            Controls.Add(subtitle);

            card = new CardPanel();
            card.SetBounds(32, 148, 456, 536);
            Controls.Add(card);

            modeSwitch = new SoftPanel();
            modeSwitch.Radius = 10;
            modeSwitch.DrawBorder = true;
            modeSwitch.BorderColor = Palette.Faint;
            modeSwitch.FillColor = Palette.Night;
            modeSwitch.SetBounds(28, 18, 400, 42);
            card.Controls.Add(modeSwitch);

            timerTab = new TempoButton();
            timerTab.Text = "Timer";
            timerTab.Selected = true;
            timerTab.Radius = 8;
            timerTab.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            timerTab.SetBounds(4, 4, 95, 34);
            timerTab.Click += delegate { SwitchMode(Mode.Timer); };
            modeSwitch.Controls.Add(timerTab);

            stopwatchTab = new TempoButton();
            stopwatchTab.Text = "Stopwatch";
            stopwatchTab.Radius = 8;
            stopwatchTab.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            stopwatchTab.SetBounds(103, 4, 95, 34);
            stopwatchTab.Click += delegate { SwitchMode(Mode.Stopwatch); };
            modeSwitch.Controls.Add(stopwatchTab);

            workTab = new TempoButton();
            workTab.Text = "Work";
            workTab.Radius = 8;
            workTab.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            workTab.SetBounds(202, 4, 95, 34);
            workTab.Click += delegate { SwitchMode(Mode.Work); };
            modeSwitch.Controls.Add(workTab);

            settingsTab = new TempoButton();
            settingsTab.Text = "Settings";
            settingsTab.Radius = 8;
            settingsTab.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            settingsTab.SetBounds(301, 4, 95, 34);
            settingsTab.Click += delegate { SwitchMode(Mode.Settings); };
            modeSwitch.Controls.Add(settingsTab);

            dial = new DialControl();
            dial.SetBounds(88, 72, 280, 280);
            dial.TimerValueCommitted += ApplyTimerEditor;
            card.Controls.Add(dial);

            presetsPanel = new Panel();
            presetsPanel.BackColor = Color.Transparent;
            presetsPanel.SetBounds(43, 363, 370, 42);
            card.Controls.Add(presetsPanel);

            int[] presetMinutes = new int[] { 5, 10, 25, 45 };
            presetButtons = new TempoButton[presetMinutes.Length];
            for (int i = 0; i < presetMinutes.Length; i++)
            {
                int minutes = presetMinutes[i];
                TempoButton button = new TempoButton();
                button.Text = minutes + " min";
                button.Tag = minutes;
                button.Radius = 8;
                button.Selected = minutes == 25;
                button.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                button.SetBounds(i * 94, 4, 88, 34);
                button.Click += PresetClicked;
                presetsPanel.Controls.Add(button);
                presetButtons[i] = button;
            }

            primaryButton = new TempoButton();
            primaryButton.Text = "▶   Start";
            primaryButton.Primary = true;
            primaryButton.Radius = 8;
            primaryButton.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            primaryButton.SetBounds(43, 420, 166, 48);
            primaryButton.Click += PrimaryClicked;
            card.Controls.Add(primaryButton);

            addMinuteButton = new TempoButton();
            addMinuteButton.Text = "+1 min";
            addMinuteButton.Radius = 8;
            addMinuteButton.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            addMinuteButton.SetBounds(219, 420, 88, 48);
            addMinuteButton.Click += delegate { AddMinute(); };
            card.Controls.Add(addMinuteButton);

            saveWorkButton = new TempoButton();
            saveWorkButton.Text = "Save";
            saveWorkButton.Radius = 8;
            saveWorkButton.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            saveWorkButton.SetBounds(223, 420, 84, 48);
            saveWorkButton.Visible = false;
            saveWorkButton.Enabled = false;
            saveWorkButton.Click += delegate { SaveCurrentWorkTimer(); };
            card.Controls.Add(saveWorkButton);

            trimWorkButton = new TempoButton();
            trimWorkButton.Text = "−";
            trimWorkButton.Radius = 8;
            trimWorkButton.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            trimWorkButton.AccessibleName = "Lower work time";
            trimWorkButton.SetBounds(43, 420, 44, 48);
            trimWorkButton.Visible = false;
            trimWorkButton.Click += delegate { ToggleTrimBar(); };
            card.Controls.Add(trimWorkButton);

            trimBar = new SoftPanel();
            trimBar.Radius = 8;
            trimBar.DrawBorder = true;
            trimBar.BorderColor = Palette.Faint;
            trimBar.FillColor = Palette.Night;
            trimBar.SetBounds(43, 363, 370, 42);
            trimBar.Visible = false;
            card.Controls.Add(trimBar);

            int[] trimMinutes = new int[] { 1, 5, 10, 15 };
            trimButtons = new TempoButton[trimMinutes.Length];
            for (int i = 0; i < trimMinutes.Length; i++)
            {
                int minutes = trimMinutes[i];
                TempoButton button = new TempoButton();
                button.Text = "−" + minutes + "m";
                button.Tag = minutes;
                button.Radius = 8;
                button.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                button.SetBounds(4 + i * 72, 4, 68, 34);
                button.Click += TrimWorkClicked;
                trimBar.Controls.Add(button);
                trimButtons[i] = button;
            }
            TempoButton trimDone = new TempoButton();
            trimDone.Text = "Done";
            trimDone.Radius = 8;
            trimDone.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            trimDone.SetBounds(296, 4, 70, 34);
            trimDone.Click += delegate { ShowTrimBar(false); };
            trimBar.Controls.Add(trimDone);

            lapButton = new TempoButton();
            lapButton.Text = "Lap";
            lapButton.Radius = 8;
            lapButton.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lapButton.SetBounds(219, 420, 88, 48);
            lapButton.Visible = false;
            lapButton.Enabled = false;
            lapButton.Click += delegate { AddLap(); };
            card.Controls.Add(lapButton);

            resetButton = new TempoButton();
            resetButton.Text = "Reset";
            resetButton.Radius = 8;
            resetButton.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            resetButton.SetBounds(317, 420, 96, 48);
            resetButton.Click += delegate { ResetCurrent(); };
            card.Controls.Add(resetButton);

            // Stopwatch laps live in the same slot as timer presets: the four most
            // recent laps as chips, oldest on the left, newest on the right.
            lapChipsPanel = new Panel();
            lapChipsPanel.BackColor = Color.Transparent;
            lapChipsPanel.SetBounds(43, 363, 370, 42);
            lapChipsPanel.Visible = false;
            card.Controls.Add(lapChipsPanel);

            lapChips = new SoftPanel[4];
            lapChipTitles = new Label[4];
            lapChipValues = new Label[4];
            for (int i = 0; i < lapChips.Length; i++)
            {
                SoftPanel chip = new SoftPanel();
                chip.Radius = 8;
                chip.DrawBorder = true;
                chip.BorderColor = Palette.Faint;
                chip.FillColor = Palette.Surface;
                chip.SetBounds(i * 94, 4, 88, 34);
                chip.Visible = false;
                lapChipsPanel.Controls.Add(chip);
                lapChips[i] = chip;

                Label title = MakeLabel("LAP 1", 6.5f, FontStyle.Bold, Palette.Muted);
                title.TextAlign = ContentAlignment.MiddleCenter;
                title.SetBounds(0, 3, 88, 11);
                chip.Controls.Add(title);
                lapChipTitles[i] = title;

                Label value = MakeLabel("00:00.00", 8.5f, FontStyle.Bold, Palette.Ink);
                value.Font = new Font("Consolas", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
                value.TextAlign = ContentAlignment.MiddleCenter;
                value.SetBounds(0, 14, 88, 18);
                chip.Controls.Add(value);
                lapChipValues[i] = value;
            }

            lapsInfoLabel = MakeLabel("Press L or Lap while running to split", 8.5f, FontStyle.Regular, Palette.Muted);
            lapsInfoLabel.TextAlign = ContentAlignment.MiddleCenter;
            lapsInfoLabel.AutoEllipsis = true;
            lapsInfoLabel.SetBounds(45, 484, 366, 28);
            lapsInfoLabel.Visible = false;
            card.Controls.Add(lapsInfoLabel);

            workInfoLabel = MakeLabel("Saved automatically on this PC", 8.5f, FontStyle.Regular, Palette.Muted);
            workInfoLabel.TextAlign = ContentAlignment.MiddleCenter;
            workInfoLabel.AutoEllipsis = true;
            workInfoLabel.SetBounds(45, 484, 366, 28);
            workInfoLabel.Visible = false;
            card.Controls.Add(workInfoLabel);

            settingsPanel = new Panel();
            settingsPanel.BackColor = Color.Transparent;
            settingsPanel.SetBounds(28, 72, 400, 448);
            settingsPanel.Visible = false;
            card.Controls.Add(settingsPanel);

            Label settingsHeading = MakeLabel("Features", 16f, FontStyle.Bold, Palette.Ink);
            settingsHeading.SetBounds(4, 0, 392, 28);
            settingsPanel.Controls.Add(settingsHeading);

            Label settingsHelper = MakeLabel("Turn automatic helpers on or off. Changes save on this PC.", 8.5f, FontStyle.Regular, Palette.Muted);
            settingsHelper.SetBounds(4, 28, 392, 22);
            settingsPanel.Controls.Add(settingsHelper);

            idleAutoStopSwitch = new TempoSwitch();
            idleMouseSwitch = new TempoSwitch();
            idleKeyboardSwitch = new TempoSwitch();
            idleSoundSwitch = new TempoSwitch();
            overlaySwitch = new TempoSwitch();
            idleAutoStopSwitch.Toggled += delegate { SetIdleAutoStop(idleAutoStopSwitch.On); };
            idleMouseSwitch.Toggled += delegate { SetIdleWatchMouse(idleMouseSwitch.On); };
            idleKeyboardSwitch.Toggled += delegate { SetIdleWatchKeyboard(idleKeyboardSwitch.On); };
            idleSoundSwitch.Toggled += delegate { SetIdleWatchSound(idleSoundSwitch.On); };
            overlaySwitch.Toggled += delegate { SetShowOverlay(overlaySwitch.On); };

            settingsPanel.Controls.Add(MakeSettingRow("Auto-stop idle work", "Pause Work after a quiet stretch so you can trim it.", idleAutoStopSwitch, 52));
            settingsPanel.Controls.Add(MakeSettingRow("Watch mouse", "Mouse movement counts as still working.", idleMouseSwitch, 118));
            settingsPanel.Controls.Add(MakeSettingRow("Watch keyboard", "Typing or shortcut keys count as still working.", idleKeyboardSwitch, 184));
            settingsPanel.Controls.Add(MakeSettingRow("Watch playback audio", "Sound from speakers counts as still working.", idleSoundSwitch, 250));

            SoftPanel timeoutRow = new SoftPanel();
            timeoutRow.Radius = 10;
            timeoutRow.DrawBorder = true;
            timeoutRow.FillColor = Palette.Surface;
            timeoutRow.SetBounds(0, 316, 400, 64);
            settingsPanel.Controls.Add(timeoutRow);

            Label timeoutTitle = MakeLabel("Idle timeout", 10f, FontStyle.Bold, Palette.Ink);
            timeoutTitle.SetBounds(16, 10, 200, 22);
            timeoutRow.Controls.Add(timeoutTitle);

            Label timeoutHint = MakeLabel("Quiet time before pausing.", 8f, FontStyle.Regular, Palette.Muted);
            timeoutHint.AutoEllipsis = true;
            timeoutHint.SetBounds(16, 32, 148, 22);
            timeoutRow.Controls.Add(timeoutHint);

            int[] idleChoices = new int[] { 1, 3, 5, 10 };
            idleMinuteButtons = new TempoButton[idleChoices.Length];
            for (int i = 0; i < idleChoices.Length; i++)
            {
                int minutes = idleChoices[i];
                TempoButton button = new TempoButton();
                button.Text = minutes + "m";
                button.Tag = minutes;
                button.Radius = 8;
                button.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                button.SetBounds(168 + i * 54, 14, 48, 36);
                button.Click += IdleTimeoutClicked;
                timeoutRow.Controls.Add(button);
                idleMinuteButtons[i] = button;
            }

            settingsPanel.Controls.Add(MakeSettingRow("Desktop overlay", "Show the circular widget while a clock is running.", overlaySwitch, 384));

            savedTimersPanel = new CardPanel();
            savedTimersPanel.SetBounds(32, 148, 456, 536);
            savedTimersPanel.Visible = false;
            Controls.Add(savedTimersPanel);

            sessionsBackButton = new TempoButton();
            sessionsBackButton.Text = "\u2190";
            sessionsBackButton.Radius = 8;
            sessionsBackButton.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            sessionsBackButton.SetBounds(28, 27, 36, 36);
            sessionsBackButton.Visible = false;
            sessionsBackButton.Click += delegate { BackToSavedTimers(); };
            savedTimersPanel.Controls.Add(sessionsBackButton);

            savedHeading = MakeLabel("Saved timers", 18f, FontStyle.Bold, Palette.Ink);
            savedHeading.AutoEllipsis = true;
            savedHeading.SetBounds(30, 27, 220, 36);
            savedTimersPanel.Controls.Add(savedHeading);

            savedHelper = MakeLabel("Load a save or manage its individual sessions.", 8.5f, FontStyle.Regular, Palette.Muted);
            savedHelper.AutoEllipsis = true;
            savedHelper.SetBounds(31, 61, 270, 22);
            savedTimersPanel.Controls.Add(savedHelper);

            savedTimersCount = MakeLabel("0 SAVED", 8f, FontStyle.Bold, Palette.Accent);
            savedTimersCount.TextAlign = ContentAlignment.MiddleRight;
            savedTimersCount.AutoEllipsis = true;
            savedTimersCount.SetBounds(278, 31, 88, 26);
            savedTimersPanel.Controls.Add(savedTimersCount);

            TempoButton closeSaved = new TempoButton();
            closeSaved.Text = "×";
            closeSaved.Radius = 8;
            closeSaved.Font = new Font("Segoe UI", 12f, FontStyle.Regular);
            closeSaved.SetBounds(378, 27, 36, 36);
            closeSaved.Click += delegate { CloseSavedTimers(); };
            savedTimersPanel.Controls.Add(closeSaved);

            savedTimerRows = new FlowLayoutPanel();
            savedTimerRows.FlowDirection = FlowDirection.TopDown;
            savedTimerRows.WrapContents = false;
            savedTimerRows.AutoScroll = true;
            savedTimerRows.BackColor = Color.Transparent;
            savedTimerRows.SetBounds(28, 98, 400, 405);
            savedTimersPanel.Controls.Add(savedTimerRows);

            savedTimersEmpty = MakeLabel("No saved timers yet\n\nOpen Work, track some time, then press Save.", 9f, FontStyle.Regular, Palette.Muted);
            savedTimersEmpty.TextAlign = ContentAlignment.MiddleCenter;
            savedTimersEmpty.SetBounds(48, 205, 360, 90);
            savedTimersPanel.Controls.Add(savedTimersEmpty);

            shortcutLabel = MakeLabel("SPACE  start / pause     R  reset", 8f, FontStyle.Regular, Palette.Muted);
            shortcutLabel.Font = new Font("Consolas", 8f, FontStyle.Regular);
            shortcutLabel.TextAlign = ContentAlignment.MiddleCenter;
            shortcutLabel.SetBounds(24, 705, 326, 28);
            Controls.Add(shortcutLabel);

            savedTimersButton = new TempoButton();
            savedTimersButton.Text = "Saved timers  0";
            savedTimersButton.Radius = 8;
            savedTimersButton.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            savedTimersButton.SetBounds(357, 702, 139, 34);
            savedTimersButton.Click += delegate { OpenSavedTimers(); };
            Controls.Add(savedTimersButton);

            updateTimer = new Timer();
            updateTimer.Interval = 100;
            updateTimer.Tick += Tick;

            widget = new WidgetForm(this);
            widget.PauseRequested += delegate { OverlayPause(); };
            widget.EndRequested += delegate { OverlayEnd(); };
            LoadSettings();
            LoadSavedTimers();
            LoadSessions();
            LoadWorkState();
            if (SaveActiveTimerProgress()) SaveWorkState();
            RebuildSavedTimers();
            FormClosed += delegate
            {
                StopAndSaveWork();
                updateTimer.Stop();
                if (backgroundCache != null) backgroundCache.Dispose();
                widget.Close();
                widget.Dispose();
            };

            KeyDown += HandleShortcut;
            MouseDown += DragWindow;
            brand.MouseDown += DragWindow;
            heading.MouseDown += DragWindow;
            subtitle.MouseDown += DragWindow;

            UpdateInterface();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Shapes.ApplyRegion(this, 12);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (backgroundCache != null)
            {
                backgroundCache.Dispose();
                backgroundCache = null;
            }
            Shapes.ApplyRegion(this, 12);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0) return;
            if (backgroundCache == null || backgroundCache.Width != Width || backgroundCache.Height != Height)
            {
                if (backgroundCache != null) backgroundCache.Dispose();
                backgroundCache = new Bitmap(Width, Height);
                using (Graphics g = Graphics.FromImage(backgroundCache))
                {
                    PaintKit.HighQuality(g);
                    g.Clear(Palette.Canvas);
                    for (int x = 24; x < Width; x += 24)
                        g.DrawLine(PaintKit.Grid, x, 0, x, Height);
                    for (int y = 24; y < Height; y += 24)
                        g.DrawLine(PaintKit.Grid, 0, y, Width, y);
                    g.FillEllipse(PaintKit.GlowTop, 300, -140, 320, 280);
                    g.FillEllipse(PaintKit.GlowBottom, -140, 560, 300, 280);
                }
            }
            e.Graphics.DrawImageUnscaled(backgroundCache, 0, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PaintKit.HighQuality(e.Graphics);
            using (GraphicsPath path = Shapes.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 12))
                e.Graphics.DrawPath(PaintKit.Hairline, path);
        }

        private Label MakeLabel(string text, float size, FontStyle style, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            label.ForeColor = color;
            label.Font = new Font("Segoe UI", size, style, GraphicsUnit.Point);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private TempoButton WindowButton(string text)
        {
            TempoButton button = new TempoButton();
            button.Text = text;
            button.Radius = 8;
            button.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            button.TabStop = false;
            return button;
        }

        private void PaintLogo(object sender, PaintEventArgs e)
        {
            PaintKit.HighQuality(e.Graphics);
            e.Graphics.DrawLine(PaintKit.Logo, 11, 9, 11, 19);
            e.Graphics.DrawLine(PaintKit.Logo, 17, 9, 17, 19);
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        private TimeSpan CurrentWorkTime
        {
            get { return workAccumulated + workSession.Elapsed; }
        }

        private SavedWorkTimer ActiveSavedTimer
        {
            get
            {
                if (string.IsNullOrEmpty(activeSavedTimerId)) return null;
                for (int i = 0; i < savedWorkTimers.Count; i++)
                {
                    if (string.Equals(savedWorkTimers[i].Id, activeSavedTimerId, StringComparison.Ordinal))
                        return savedWorkTimers[i];
                }
                return null;
            }
        }

        private WorkSessionRecord ActiveWorkSession
        {
            get
            {
                SavedWorkTimer timer = ActiveSavedTimer;
                if (timer == null || string.IsNullOrEmpty(activeWorkSessionId)) return null;
                for (int i = 0; i < timer.Sessions.Count; i++)
                {
                    if (string.Equals(timer.Sessions[i].Id, activeWorkSessionId, StringComparison.Ordinal))
                        return timer.Sessions[i];
                }
                return null;
            }
        }

        private TimeSpan CalculateSavedTimerTotal(SavedWorkTimer timer, bool includeLiveSession)
        {
            if (timer == null) return TimeSpan.Zero;
            long totalTicks = 0;
            for (int i = 0; i < timer.Sessions.Count; i++)
            {
                WorkSessionRecord session = timer.Sessions[i];
                TimeSpan elapsed = includeLiveSession
                    && string.Equals(timer.Id, activeSavedTimerId, StringComparison.Ordinal)
                    && string.Equals(session.Id, activeWorkSessionId, StringComparison.Ordinal)
                    ? CurrentWorkTime
                    : session.Elapsed;
                if (elapsed.Ticks > TimeSpan.MaxValue.Ticks - totalTicks)
                    return TimeSpan.MaxValue;
                totalTicks += Math.Max(0, elapsed.Ticks);
            }
            return TimeSpan.FromTicks(totalTicks);
        }

        private void RefreshSavedTimerTotal(SavedWorkTimer timer)
        {
            if (timer != null) timer.Elapsed = CalculateSavedTimerTotal(timer, false);
        }

        private Panel MakeSettingRow(string title, string hint, TempoSwitch toggle, int y)
        {
            SoftPanel row = new SoftPanel();
            row.Radius = 10;
            row.DrawBorder = true;
            row.FillColor = Palette.Surface;
            row.SetBounds(0, y, 400, 64);

            Label heading = MakeLabel(title, 10f, FontStyle.Bold, Palette.Ink);
            heading.SetBounds(16, 8, 310, 22);
            row.Controls.Add(heading);

            Label helper = MakeLabel(hint, 8f, FontStyle.Regular, Palette.Muted);
            helper.AutoEllipsis = true;
            helper.SetBounds(16, 32, 310, 22);
            row.Controls.Add(helper);

            toggle.SetBounds(336, 19, 48, 26);
            row.Controls.Add(toggle);
            return row;
        }

        private void LoadWorkState()
        {
            try
            {
                if (!File.Exists(workStatePath)) return;
                string saved = File.ReadAllText(workStatePath).Trim();
                string[] parts = saved.Split('|');
                string elapsedPart = parts.Length > 0 ? parts[0] : string.Empty;
                string activePart = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                string sessionPart = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                long ticks;
                if (long.TryParse(elapsedPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks) && ticks >= 0)
                    workAccumulated = TimeSpan.FromTicks(ticks);
                activeSavedTimerId = activePart.Length > 0 ? activePart : null;
                activeWorkSessionId = sessionPart.Length > 0 ? sessionPart : null;
                SavedWorkTimer activeTimer = ActiveSavedTimer;
                if (activeTimer == null)
                {
                    activeSavedTimerId = null;
                    activeWorkSessionId = null;
                }
                else if (ActiveWorkSession == null && workAccumulated > TimeSpan.Zero)
                {
                    if (sessionPart.Length == 0 && activeTimer.Sessions.Count == 1)
                    {
                        activeWorkSessionId = activeTimer.Sessions[0].Id;
                    }
                    else
                    {
                        WorkSessionRecord recovered = new WorkSessionRecord
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Name = "Recovered session",
                            Elapsed = workAccumulated,
                            CreatedUtc = DateTime.UtcNow,
                            UpdatedUtc = DateTime.UtcNow
                        };
                        activeTimer.Sessions.Add(recovered);
                        activeWorkSessionId = recovered.Id;
                        RefreshSavedTimerTotal(activeTimer);
                        SaveSessions();
                        SaveSavedTimers();
                    }
                }
            }
            catch
            {
                workAccumulated = TimeSpan.Zero;
                activeSavedTimerId = null;
                activeWorkSessionId = null;
            }
        }

        private void SaveWorkState()
        {
            try
            {
                string folder = Path.GetDirectoryName(workStatePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string saved = CurrentWorkTime.Ticks.ToString(CultureInfo.InvariantCulture)
                    + "|" + (activeSavedTimerId ?? string.Empty)
                    + "|" + (activeWorkSessionId ?? string.Empty);
                File.WriteAllText(workStatePath, saved);
            }
            catch
            {
                // Timing remains usable even if Windows temporarily blocks storage.
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string[] lines = File.ReadAllLines(settingsPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        int split = line.IndexOf('=');
                        if (split <= 0) continue;
                        string key = line.Substring(0, split).Trim();
                        string value = line.Substring(split + 1).Trim();
                        if (key == "idleAutoStop") idleAutoStop = value != "0";
                        else if (key == "idleWatchMouse") idleWatchMouse = value != "0";
                        else if (key == "idleWatchKeyboard") idleWatchKeyboard = value != "0";
                        else if (key == "idleWatchSound") idleWatchSound = value != "0";
                        else if (key == "showOverlay") showOverlay = value != "0";
                        else if (key == "idleMinutes")
                        {
                            int minutes;
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes)
                                && (minutes == 1 || minutes == 3 || minutes == 5 || minutes == 10))
                                idleMinutes = minutes;
                        }
                    }
                }
            }
            catch
            {
                idleAutoStop = true;
                idleWatchMouse = true;
                idleWatchKeyboard = true;
                idleWatchSound = true;
                idleMinutes = 3;
                showOverlay = true;
            }
            ApplySettingsUi();
        }

        private void SaveSettings()
        {
            try
            {
                string folder = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                File.WriteAllText(settingsPath,
                    "idleAutoStop=" + (idleAutoStop ? "1" : "0") + "\r\n"
                    + "idleWatchMouse=" + (idleWatchMouse ? "1" : "0") + "\r\n"
                    + "idleWatchKeyboard=" + (idleWatchKeyboard ? "1" : "0") + "\r\n"
                    + "idleWatchSound=" + (idleWatchSound ? "1" : "0") + "\r\n"
                    + "idleMinutes=" + idleMinutes.ToString(CultureInfo.InvariantCulture) + "\r\n"
                    + "showOverlay=" + (showOverlay ? "1" : "0") + "\r\n");
            }
            catch
            {
            }
        }

        private void ApplySettingsUi()
        {
            idleAutoStopSwitch.On = idleAutoStop;
            idleMouseSwitch.On = idleWatchMouse;
            idleKeyboardSwitch.On = idleWatchKeyboard;
            idleSoundSwitch.On = idleWatchSound;
            overlaySwitch.On = showOverlay;
            idleMouseSwitch.Enabled = idleAutoStop;
            idleKeyboardSwitch.Enabled = idleAutoStop;
            idleSoundSwitch.Enabled = idleAutoStop;
            for (int i = 0; i < idleMinuteButtons.Length; i++)
            {
                idleMinuteButtons[i].Enabled = idleAutoStop;
                idleMinuteButtons[i].Selected = (int)idleMinuteButtons[i].Tag == idleMinutes;
                idleMinuteButtons[i].Invalidate();
            }
        }

        private void SetIdleAutoStop(bool value)
        {
            idleAutoStop = value;
            if (!idleAutoStop) workIdleStopped = false;
            ApplySettingsUi();
            SaveSettings();
            UpdateInterface();
        }

        private void SetIdleWatchMouse(bool value)
        {
            idleWatchMouse = value;
            ApplySettingsUi();
            SaveSettings();
        }

        private void SetIdleWatchKeyboard(bool value)
        {
            idleWatchKeyboard = value;
            ApplySettingsUi();
            SaveSettings();
        }

        private void SetIdleWatchSound(bool value)
        {
            idleWatchSound = value;
            ApplySettingsUi();
            SaveSettings();
        }

        private void SetShowOverlay(bool value)
        {
            showOverlay = value;
            ApplySettingsUi();
            SaveSettings();
            UpdateInterface();
        }

        private void IdleTimeoutClicked(object sender, EventArgs e)
        {
            if (!idleAutoStop) return;
            TempoButton clicked = (TempoButton)sender;
            idleMinutes = (int)clicked.Tag;
            ApplySettingsUi();
            SaveSettings();
        }

        private string IdlePauseReason()
        {
            List<string> parts = new List<string>();
            if (idleWatchMouse) parts.Add("mouse");
            if (idleWatchKeyboard) parts.Add("keyboard");
            if (idleWatchSound) parts.Add("sound");
            string sensors;
            if (parts.Count == 0) sensors = "activity";
            else if (parts.Count == 1) sensors = parts[0];
            else if (parts.Count == 2) sensors = parts[0] + " or " + parts[1];
            else sensors = parts[0] + ", " + parts[1] + ", or " + parts[2];
            return "No " + sensors + " for " + idleMinutes + " min — timer paused";
        }

        private void StopAndSaveWork()
        {
            if (workSession.Elapsed > TimeSpan.Zero)
            {
                workAccumulated += workSession.Elapsed;
                workSession.Reset();
            }
            PersistWorkProgress(false);
        }

        private bool SaveActiveTimerProgress()
        {
            SavedWorkTimer active = ActiveSavedTimer;
            if (active == null) return false;
            WorkSessionRecord session = ActiveWorkSession;
            if (session == null && CurrentWorkTime > TimeSpan.Zero)
                session = EnsureActiveWorkSession();
            if (session == null) return false;
            DateTime now = DateTime.UtcNow;
            session.Elapsed = CurrentWorkTime;
            session.UpdatedUtc = now;
            RefreshSavedTimerTotal(active);
            active.SavedUtc = now;
            SaveSessions();
            SaveSavedTimers();
            return true;
        }

        private WorkSessionRecord EnsureActiveWorkSession()
        {
            SavedWorkTimer timer = ActiveSavedTimer;
            if (timer == null) return null;
            WorkSessionRecord existing = ActiveWorkSession;
            if (existing != null) return existing;
            DateTime now = DateTime.UtcNow;
            WorkSessionRecord session = new WorkSessionRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Session " + (timer.Sessions.Count + 1),
                Elapsed = CurrentWorkTime,
                CreatedUtc = now,
                UpdatedUtc = now
            };
            timer.Sessions.Add(session);
            activeWorkSessionId = session.Id;
            RefreshSavedTimerTotal(timer);
            SaveSessions();
            SaveSavedTimers();
            SaveWorkState();
            return session;
        }

        private void PersistWorkProgress(bool refreshSavedList)
        {
            SaveWorkState();
            if (SaveActiveTimerProgress() && refreshSavedList)
                RebuildSavedTimers();
        }

        private void LoadSavedTimers()
        {
            savedWorkTimers.Clear();
            try
            {
                if (!File.Exists(savedTimersPath)) return;
                string[] lines = File.ReadAllLines(savedTimersPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split('|');
                    if (parts.Length != 4) continue;
                    long elapsedTicks;
                    long savedTicks;
                    if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out elapsedTicks) || elapsedTicks < 0) continue;
                    if (!long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out savedTicks) || savedTicks <= 0) continue;
                    string name = Encoding.UTF8.GetString(Convert.FromBase64String(parts[3]));
                    if (name.Trim().Length == 0) continue;
                    savedWorkTimers.Add(new SavedWorkTimer
                    {
                        Id = parts[0].Length > 0 ? parts[0] : Guid.NewGuid().ToString("N"),
                        Name = name,
                        Elapsed = TimeSpan.FromTicks(elapsedTicks),
                        SavedUtc = new DateTime(savedTicks, DateTimeKind.Utc)
                    });
                }
            }
            catch
            {
                savedWorkTimers.Clear();
            }
        }

        private SavedWorkTimer FindSavedTimerById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < savedWorkTimers.Count; i++)
            {
                if (string.Equals(savedWorkTimers[i].Id, id, StringComparison.Ordinal))
                    return savedWorkTimers[i];
            }
            return null;
        }

        private void LoadSessions()
        {
            bool migrated = false;
            try
            {
                if (File.Exists(sessionsPath))
                {
                    string[] lines = File.ReadAllLines(sessionsPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        try
                        {
                            string[] parts = lines[i].Split('|');
                            if (parts.Length != 6) continue;
                            SavedWorkTimer timer = FindSavedTimerById(parts[0]);
                            if (timer == null) continue;
                            long elapsedTicks;
                            long createdTicks;
                            long updatedTicks;
                            if (!long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out elapsedTicks) || elapsedTicks < 0) continue;
                            if (!long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out createdTicks) || createdTicks <= 0) continue;
                            if (!long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out updatedTicks) || updatedTicks <= 0) continue;
                            string name = Encoding.UTF8.GetString(Convert.FromBase64String(parts[5])).Trim();
                            if (name.Length == 0) name = "Session " + (timer.Sessions.Count + 1);
                            timer.Sessions.Add(new WorkSessionRecord
                            {
                                Id = parts[1].Length > 0 ? parts[1] : Guid.NewGuid().ToString("N"),
                                Name = name,
                                Elapsed = TimeSpan.FromTicks(elapsedTicks),
                                CreatedUtc = new DateTime(createdTicks, DateTimeKind.Utc),
                                UpdatedUtc = new DateTime(updatedTicks, DateTimeKind.Utc)
                            });
                        }
                        catch
                        {
                            // Ignore a damaged session while keeping the rest available.
                        }
                    }
                }

                for (int i = 0; i < savedWorkTimers.Count; i++)
                {
                    SavedWorkTimer timer = savedWorkTimers[i];
                    if (timer.Sessions.Count == 0 && timer.Elapsed > TimeSpan.Zero)
                    {
                        timer.Sessions.Add(new WorkSessionRecord
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Name = "Session 1",
                            Elapsed = timer.Elapsed,
                            CreatedUtc = timer.SavedUtc,
                            UpdatedUtc = timer.SavedUtc
                        });
                        migrated = true;
                    }
                    RefreshSavedTimerTotal(timer);
                }
                if (migrated) SaveSessions();
            }
            catch
            {
                // Existing timer totals remain available if session storage is unavailable.
            }
        }

        private void SaveSessions()
        {
            try
            {
                string folder = Path.GetDirectoryName(sessionsPath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                List<string> lines = new List<string>();
                for (int i = 0; i < savedWorkTimers.Count; i++)
                {
                    SavedWorkTimer timer = savedWorkTimers[i];
                    for (int j = 0; j < timer.Sessions.Count; j++)
                    {
                        WorkSessionRecord session = timer.Sessions[j];
                        string encodedName = Convert.ToBase64String(Encoding.UTF8.GetBytes(session.Name));
                        lines.Add(string.Join("|", new string[]
                        {
                            timer.Id,
                            session.Id,
                            session.Elapsed.Ticks.ToString(CultureInfo.InvariantCulture),
                            session.CreatedUtc.Ticks.ToString(CultureInfo.InvariantCulture),
                            session.UpdatedUtc.Ticks.ToString(CultureInfo.InvariantCulture),
                            encodedName
                        }));
                    }
                }
                File.WriteAllLines(sessionsPath, lines.ToArray());
            }
            catch
            {
                // Sessions remain available in the current app session.
            }
        }

        private void SaveSavedTimers()
        {
            try
            {
                string folder = Path.GetDirectoryName(savedTimersPath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                List<string> lines = new List<string>();
                for (int i = 0; i < savedWorkTimers.Count; i++)
                {
                    SavedWorkTimer timer = savedWorkTimers[i];
                    string encodedName = Convert.ToBase64String(Encoding.UTF8.GetBytes(timer.Name));
                    lines.Add(string.Join("|", new string[]
                    {
                        timer.Id,
                        timer.Elapsed.Ticks.ToString(CultureInfo.InvariantCulture),
                        timer.SavedUtc.Ticks.ToString(CultureInfo.InvariantCulture),
                        encodedName
                    }));
                }
                File.WriteAllLines(savedTimersPath, lines.ToArray());
            }
            catch
            {
                // Saved timers remain available in the current session.
            }
        }

        private void SaveCurrentWorkTimer()
        {
            if (CurrentWorkTime <= TimeSpan.Zero) return;
            SavedWorkTimer active = ActiveSavedTimer;
            if (active != null)
            {
                PersistWorkProgress(true);
                UpdateInterface();
                return;
            }
            using (SaveTimerDialog dialog = new SaveTimerDialog("Work timer " + (savedWorkTimers.Count + 1), CurrentWorkTime))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    SaveNamedWorkTimer(dialog.TimerName);
            }
        }

        private void SaveNamedWorkTimer(string name)
        {
            TimeSpan elapsed = CurrentWorkTime;
            if (elapsed <= TimeSpan.Zero || string.IsNullOrWhiteSpace(name)) return;
            SavedWorkTimer timer = new SavedWorkTimer
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name.Trim(),
                SavedUtc = DateTime.UtcNow
            };
            WorkSessionRecord session = new WorkSessionRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Session 1",
                Elapsed = elapsed,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };
            timer.Sessions.Add(session);
            RefreshSavedTimerTotal(timer);
            savedWorkTimers.Add(timer);
            activeSavedTimerId = timer.Id;
            activeWorkSessionId = session.Id;
            SaveSavedTimers();
            SaveSessions();
            SaveWorkState();
            RebuildSavedTimers();
            UpdateInterface();
        }

        private void OpenSavedTimers()
        {
            if (savedTimersOpen)
            {
                CloseSavedTimers();
                return;
            }
            savedTimersOpen = true;
            viewedSavedTimer = null;
            RebuildSavedTimers();
            card.Visible = false;
            savedTimersPanel.Visible = true;
            savedTimersPanel.BringToFront();
            savedTimersButton.Text = "Back to tracking";
        }

        private void CloseSavedTimers()
        {
            savedTimersOpen = false;
            viewedSavedTimer = null;
            savedTimersPanel.Visible = false;
            card.Visible = true;
            card.BringToFront();
            savedTimersButton.BringToFront();
            savedTimersButton.Text = "Saved timers  " + savedWorkTimers.Count;
        }

        private void LoadSavedTimer(SavedWorkTimer timer)
        {
            if (timer == null) return;
            FinishCurrentSessionBeforeLoad();
            workSession.Reset();
            workAccumulated = TimeSpan.Zero;
            activeSavedTimerId = timer.Id;
            activeWorkSessionId = null;
            SaveWorkState();
            CloseSavedTimers();
            SwitchMode(Mode.Work);
        }

        private void DeleteSavedTimer(SavedWorkTimer timer)
        {
            if (timer == null) return;
            bool wasActive = string.Equals(activeSavedTimerId, timer.Id, StringComparison.Ordinal);
            savedWorkTimers.Remove(timer);
            if (wasActive)
            {
                workSession.Reset();
                workAccumulated = TimeSpan.Zero;
                activeSavedTimerId = null;
                activeWorkSessionId = null;
                SaveWorkState();
            }
            if (viewedSavedTimer == timer) viewedSavedTimer = null;
            SaveSessions();
            SaveSavedTimers();
            RebuildSavedTimers();
            UpdateInterface();
        }

        private void FinishCurrentSessionBeforeLoad()
        {
            if (workSession.IsRunning)
            {
                workAccumulated += workSession.Elapsed;
                workSession.Reset();
            }
            if (ActiveSavedTimer != null) PersistWorkProgress(false);
        }

        private void LoadWorkSession(SavedWorkTimer timer, WorkSessionRecord session)
        {
            if (timer == null || session == null || !timer.Sessions.Contains(session)) return;
            FinishCurrentSessionBeforeLoad();
            workSession.Reset();
            workAccumulated = session.Elapsed;
            activeSavedTimerId = timer.Id;
            activeWorkSessionId = session.Id;
            SaveWorkState();
            CloseSavedTimers();
            SwitchMode(Mode.Work);
        }

        private void OpenSavedTimerSessions(SavedWorkTimer timer)
        {
            if (timer == null) return;
            viewedSavedTimer = timer;
            RebuildSavedTimers();
        }

        private void BackToSavedTimers()
        {
            viewedSavedTimer = null;
            RebuildSavedTimers();
        }

        private void RenameWorkSession(SavedWorkTimer timer, WorkSessionRecord session)
        {
            if (timer == null || session == null || !timer.Sessions.Contains(session)) return;
            using (SaveTimerDialog dialog = new SaveTimerDialog(session.Name, session.Elapsed, true))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                session.Name = dialog.TimerName;
                session.UpdatedUtc = DateTime.UtcNow;
                timer.SavedUtc = session.UpdatedUtc;
                SaveSessions();
                SaveSavedTimers();
                RebuildSavedTimers();
                UpdateInterface();
            }
        }

        private void DeleteWorkSession(SavedWorkTimer timer, WorkSessionRecord session)
        {
            if (timer == null || session == null || !timer.Sessions.Contains(session)) return;
            bool wasActive = string.Equals(activeSavedTimerId, timer.Id, StringComparison.Ordinal)
                && string.Equals(activeWorkSessionId, session.Id, StringComparison.Ordinal);
            if (wasActive)
            {
                workSession.Reset();
                workAccumulated = TimeSpan.Zero;
                activeWorkSessionId = null;
                SaveWorkState();
            }
            timer.Sessions.Remove(session);
            RefreshSavedTimerTotal(timer);
            timer.SavedUtc = DateTime.UtcNow;
            SaveSessions();
            SaveSavedTimers();
            RebuildSavedTimers();
            UpdateInterface();
        }

        private void RebuildSavedTimers()
        {
            while (savedTimerRows.Controls.Count > 0)
            {
                Control oldRow = savedTimerRows.Controls[0];
                savedTimerRows.Controls.RemoveAt(0);
                oldRow.Dispose();
            }
            if (viewedSavedTimer != null && savedWorkTimers.Contains(viewedSavedTimer))
            {
                RebuildSessionRows(viewedSavedTimer);
                return;
            }
            viewedSavedTimer = null;
            sessionsBackButton.Visible = false;
            savedHeading.Text = "Saved timers";
            savedHeading.SetBounds(30, 27, 220, 36);
            savedHelper.Text = "Load a save or manage its individual sessions.";
            savedHelper.SetBounds(31, 61, 310, 22);
            savedTimersEmpty.Visible = savedWorkTimers.Count == 0;
            savedTimersEmpty.Text = "No saved work yet\n\nOpen Work, track a session, then press Save.";
            savedTimersCount.Text = savedWorkTimers.Count + " SAVED";
            savedTimersButton.Text = savedTimersOpen ? "Back to tracking" : "Saved timers  " + savedWorkTimers.Count;

            for (int i = savedWorkTimers.Count - 1; i >= 0; i--)
            {
                SavedWorkTimer timer = savedWorkTimers[i];
                SavedWorkTimer captured = timer;
                SoftPanel row = new SoftPanel();
                row.Radius = 8;
                row.FillColor = Palette.Surface;
                row.Margin = new Padding(0, 0, 0, 10);
                row.Size = new Size(382, 76);

                Label name = MakeLabel(timer.Name, 10f, FontStyle.Bold, Palette.Ink);
                name.AutoEllipsis = true;
                name.SetBounds(15, 10, 164, 24);
                row.Controls.Add(name);

                string savedWhen = timer.SavedUtc.ToLocalTime().ToString("MMM d, HH:mm", CultureInfo.CurrentCulture);
                Label detail = MakeLabel(DialControl.FormatWork(timer.Elapsed) + "   •   " + savedWhen, 8f, FontStyle.Regular, Palette.Muted);
                detail.Font = new Font("Consolas", 8f, FontStyle.Regular);
                detail.AutoEllipsis = true;
                detail.Text = DialControl.FormatWork(timer.Elapsed) + "  -  " + timer.Sessions.Count + (timer.Sessions.Count == 1 ? " session" : " sessions");
                detail.SetBounds(15, 40, 168, 21);
                row.Controls.Add(detail);

                TempoButton load = new TempoButton();
                bool isActive = string.Equals(timer.Id, activeSavedTimerId, StringComparison.Ordinal);
                load.Text = isActive ? "Active" : "Load";
                load.Primary = true;
                load.Radius = 8;
                load.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
                load.SetBounds(184, 20, 58, 36);
                load.Enabled = !isActive;
                load.Click += delegate { LoadSavedTimer(captured); };
                row.Controls.Add(load);

                TempoButton sessions = new TempoButton();
                sessions.Text = "Sessions";
                sessions.Radius = 8;
                sessions.Font = new Font("Segoe UI", 7.8f, FontStyle.Bold);
                sessions.SetBounds(248, 20, 78, 36);
                sessions.Click += delegate { OpenSavedTimerSessions(captured); };
                row.Controls.Add(sessions);

                TempoButton delete = new TempoButton();
                delete.Text = "Delete";
                delete.Danger = true;
                delete.Radius = 8;
                delete.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
                delete.SetBounds(332, 20, 48, 36);
                delete.Click += delegate { DeleteSavedTimer(captured); };
                row.Controls.Add(delete);

                savedTimerRows.Controls.Add(row);
            }
            if (savedTimersEmpty.Visible) savedTimersEmpty.BringToFront();
        }

        private void RebuildSessionRows(SavedWorkTimer timer)
        {
            sessionsBackButton.Visible = true;
            sessionsBackButton.BringToFront();
            savedHeading.Text = timer.Name;
            savedHeading.SetBounds(74, 27, 194, 36);
            savedHelper.Text = "Each session contributes to the overall saved time.";
            savedHelper.SetBounds(74, 61, 292, 22);
            savedTimersCount.Text = timer.Sessions.Count + (timer.Sessions.Count == 1 ? " SESSION" : " SESSIONS");
            savedTimersEmpty.Visible = timer.Sessions.Count == 0;
            savedTimersEmpty.Text = "No sessions yet\n\nLoad this save and start working to create one.";

            for (int i = timer.Sessions.Count - 1; i >= 0; i--)
            {
                WorkSessionRecord session = timer.Sessions[i];
                WorkSessionRecord capturedSession = session;
                SavedWorkTimer capturedTimer = timer;
                SoftPanel row = new SoftPanel();
                row.Radius = 8;
                row.FillColor = Palette.Surface;
                row.Margin = new Padding(0, 0, 0, 10);
                row.Size = new Size(382, 76);

                Label name = MakeLabel(session.Name, 9.5f, FontStyle.Bold, Palette.Ink);
                name.AutoEllipsis = true;
                name.SetBounds(15, 10, 164, 24);
                row.Controls.Add(name);

                string sessionWhen = session.CreatedUtc.ToLocalTime().ToString("MMM d, HH:mm", CultureInfo.CurrentCulture);
                Label detail = MakeLabel(DialControl.FormatWork(session.Elapsed) + "  -  " + sessionWhen, 8f, FontStyle.Regular, Palette.Muted);
                detail.Font = new Font("Consolas", 8f, FontStyle.Regular);
                detail.AutoEllipsis = true;
                detail.SetBounds(15, 40, 168, 21);
                row.Controls.Add(detail);

                bool isActive = string.Equals(timer.Id, activeSavedTimerId, StringComparison.Ordinal)
                    && string.Equals(session.Id, activeWorkSessionId, StringComparison.Ordinal);
                TempoButton load = new TempoButton();
                load.Text = isActive ? "Active" : "Load";
                load.Primary = true;
                load.Radius = 8;
                load.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
                load.SetBounds(184, 20, 58, 36);
                load.Enabled = !isActive;
                load.Click += delegate { LoadWorkSession(capturedTimer, capturedSession); };
                row.Controls.Add(load);

                TempoButton rename = new TempoButton();
                rename.Text = "Rename";
                rename.Radius = 8;
                rename.Font = new Font("Segoe UI", 7.8f, FontStyle.Bold);
                rename.SetBounds(248, 20, 70, 36);
                rename.Click += delegate { RenameWorkSession(capturedTimer, capturedSession); };
                row.Controls.Add(rename);

                TempoButton delete = new TempoButton();
                delete.Text = "Delete";
                delete.Danger = true;
                delete.Radius = 8;
                delete.Font = new Font("Segoe UI", 7.8f, FontStyle.Bold);
                delete.SetBounds(324, 20, 56, 36);
                delete.Click += delegate { DeleteWorkSession(capturedTimer, capturedSession); };
                row.Controls.Add(delete);

                savedTimerRows.Controls.Add(row);
            }
            if (savedTimersEmpty.Visible) savedTimersEmpty.BringToFront();
        }

        private void SwitchMode(Mode next)
        {
            mode = next;
            timerTab.Selected = mode == Mode.Timer;
            stopwatchTab.Selected = mode == Mode.Stopwatch;
            workTab.Selected = mode == Mode.Work;
            settingsTab.Selected = mode == Mode.Settings;
            timerTab.Invalidate();
            stopwatchTab.Invalidate();
            workTab.Invalidate();
            settingsTab.Invalidate();
            bool clockMode = mode != Mode.Settings;
            settingsPanel.Visible = mode == Mode.Settings;
            dial.Visible = clockMode;
            primaryButton.Visible = clockMode;
            resetButton.Visible = clockMode;
            presetsPanel.Visible = mode == Mode.Timer;
            addMinuteButton.Visible = mode == Mode.Timer;
            saveWorkButton.Visible = mode == Mode.Work;
            trimWorkButton.Visible = mode == Mode.Work;
            if (mode != Mode.Work) ShowTrimBar(false);
            lapChipsPanel.Visible = mode == Mode.Stopwatch;
            lapsInfoLabel.Visible = mode == Mode.Stopwatch;
            lapButton.Visible = mode == Mode.Stopwatch;
            workInfoLabel.Visible = mode == Mode.Work;

            resetButton.SetBounds(317, 420, 96, 48);
            if (mode == Mode.Timer)
            {
                primaryButton.SetBounds(43, 420, 166, 48);
                shortcutLabel.Text = "SPACE  start / pause     R  reset";
            }
            else if (mode == Mode.Stopwatch)
            {
                primaryButton.SetBounds(43, 420, 166, 48);
                shortcutLabel.Text = "SPACE  start / pause     L  lap     R  reset";
            }
            else if (mode == Mode.Work)
            {
                primaryButton.SetBounds(97, 420, 116, 48);
                shortcutLabel.Text = "SPACE  start / pause     R  reset";
            }
            else
            {
                shortcutLabel.Text = "Idle pause, sensors, overlay";
            }
            UpdateInterface();
        }

        private void AddMinute()
        {
            TimeSpan extra = TimeSpan.FromMinutes(1);
            TimeSpan maximum = TimeSpan.FromMinutes(99) + TimeSpan.FromSeconds(59);
            timerDuration = timerDuration + extra > maximum ? maximum : timerDuration + extra;
            timerRemaining = timerRemaining + extra > maximum ? maximum : timerRemaining + extra;
            if (timerRunning) timerEndsAt = DateTime.UtcNow + timerRemaining;
            timerComplete = false;
            for (int i = 0; i < presetButtons.Length; i++)
            {
                int preset = (int)presetButtons[i].Tag;
                presetButtons[i].Selected = timerDuration == TimeSpan.FromMinutes(preset);
                presetButtons[i].Invalidate();
            }
            UpdateInterface();
        }

        private void PresetClicked(object sender, EventArgs e)
        {
            TempoButton clicked = (TempoButton)sender;
            int minutes = (int)clicked.Tag;
            timerRunning = false;
            timerHasStarted = false;
            timerComplete = false;
            timerDuration = TimeSpan.FromMinutes(minutes);
            timerRemaining = timerDuration;
            for (int i = 0; i < presetButtons.Length; i++)
            {
                presetButtons[i].Selected = presetButtons[i] == clicked;
                presetButtons[i].Invalidate();
            }
            UpdateInterface();
        }

        private void ApplyTimerEditor(object sender, EventArgs e)
        {
            if (timerRunning) return;
            int minutes = dial.TimerMinutes;
            int seconds = dial.TimerSeconds;
            timerDuration = TimeSpan.FromSeconds(minutes * 60 + seconds);
            timerRemaining = timerDuration;
            timerHasStarted = false;
            timerComplete = false;
            for (int i = 0; i < presetButtons.Length; i++)
            {
                int preset = (int)presetButtons[i].Tag;
                presetButtons[i].Selected = timerDuration == TimeSpan.FromMinutes(preset);
                presetButtons[i].Invalidate();
            }
            UpdateInterface();
        }

        private void PrimaryClicked(object sender, EventArgs e)
        {
            if (mode == Mode.Timer) ToggleTimer();
            else if (mode == Mode.Stopwatch) ToggleStopwatch();
            else if (mode == Mode.Work) ToggleWork();
        }

        private void ToggleTimer()
        {
            if (timerRunning)
            {
                timerRemaining = timerEndsAt - DateTime.UtcNow;
                if (timerRemaining < TimeSpan.Zero) timerRemaining = TimeSpan.Zero;
                timerRunning = false;
            }
            else
            {
                if (!timerHasStarted) ApplyTimerEditor(this, EventArgs.Empty);
                if (timerRemaining <= TimeSpan.Zero) timerRemaining = timerDuration;
                if (timerRemaining <= TimeSpan.Zero) return;
                timerComplete = false;
                timerEndsAt = DateTime.UtcNow + timerRemaining;
                timerRunning = true;
                timerHasStarted = true;
            }
            UpdateInterface();
        }

        private void ToggleStopwatch()
        {
            if (stopwatch.IsRunning) stopwatch.Stop();
            else stopwatch.Start();
            UpdateInterface();
        }

        private void ToggleWork()
        {
            if (workSession.IsRunning)
            {
                workAccumulated += workSession.Elapsed;
                workSession.Reset();
                PersistWorkProgress(false);
            }
            else
            {
                if (ActiveSavedTimer != null && ActiveWorkSession == null)
                    EnsureActiveWorkSession();
                workIdleStopped = false;
                workIdleSince = DateTime.UtcNow;
                WorkIdle.RememberCursor();
                WorkIdle.RememberKeyboard();
                workSession.Restart();
                nextWorkAutoSave = DateTime.UtcNow.AddSeconds(15);
            }
            UpdateInterface();
        }

        private void PauseWorkFromIdle()
        {
            if (!workSession.IsRunning) return;
            workAccumulated += workSession.Elapsed;
            workSession.Reset();
            PersistWorkProgress(false);
            workIdleStopped = true;
            ShowTrimBar(false);
            if (mode != Mode.Work) SwitchMode(Mode.Work);
            else UpdateInterface();
            try { FlashWindow(Handle, true); } catch { }
        }

        private void ToggleTrimBar()
        {
            ShowTrimBar(!trimBar.Visible);
        }

        private void ShowTrimBar(bool show)
        {
            if (trimBar.Visible == show) return;
            trimBar.Visible = show;
            trimWorkButton.Selected = show;
            trimWorkButton.Invalidate();
            if (show) RefreshTrimButtons();
        }

        private void RefreshTrimButtons()
        {
            TimeSpan total = CurrentWorkTime;
            for (int i = 0; i < trimButtons.Length; i++)
            {
                int minutes = (int)trimButtons[i].Tag;
                trimButtons[i].Enabled = total >= TimeSpan.FromMinutes(minutes);
            }
        }

        private void TrimWorkClicked(object sender, EventArgs e)
        {
            TempoButton clicked = (TempoButton)sender;
            TrimWork(TimeSpan.FromMinutes((int)clicked.Tag));
        }

        private void TrimWork(TimeSpan amount)
        {
            if (amount <= TimeSpan.Zero) return;
            if (workSession.IsRunning)
            {
                workAccumulated += workSession.Elapsed;
                workSession.Restart();
                workIdleSince = DateTime.UtcNow;
                WorkIdle.RememberCursor();
                WorkIdle.RememberKeyboard();
            }
            if (amount > workAccumulated) amount = workAccumulated;
            workAccumulated -= amount;
            PersistWorkProgress(savedTimersOpen);
            RefreshTrimButtons();
            UpdateInterface();
        }

        private void ResetCurrent()
        {
            if (mode == Mode.Settings) return;
            if (mode == Mode.Timer)
            {
                timerRunning = false;
                timerHasStarted = false;
                timerComplete = false;
                timerRemaining = timerDuration;
            }
            else if (mode == Mode.Stopwatch)
            {
                stopwatch.Reset();
                laps.Clear();
                RebuildLaps();
            }
            else
            {
                SaveActiveTimerProgress();
                workSession.Reset();
                workAccumulated = TimeSpan.Zero;
                activeSavedTimerId = null;
                activeWorkSessionId = null;
                workIdleStopped = false;
                SaveWorkState();
            }
            UpdateInterface();
        }

        internal void OverlayPause()
        {
            if (timerRunning) ToggleTimer();
            else if (stopwatch.IsRunning) ToggleStopwatch();
            else if (workSession.IsRunning) ToggleWork();
            else if (timerHasStarted && !timerComplete) ToggleTimer();
            else if (stopwatch.Elapsed > TimeSpan.Zero) ToggleStopwatch();
            else if (CurrentWorkTime > TimeSpan.Zero) ToggleWork();
        }

        internal void OverlayEnd()
        {
            if (timerRunning || timerHasStarted || timerComplete)
            {
                timerRunning = false;
                timerHasStarted = false;
                timerComplete = false;
                timerRemaining = timerDuration;
            }
            else if (stopwatch.IsRunning || stopwatch.Elapsed > TimeSpan.Zero)
            {
                stopwatch.Reset();
                laps.Clear();
                RebuildLaps();
            }
            else
            {
                SaveActiveTimerProgress();
                workSession.Reset();
                workAccumulated = TimeSpan.Zero;
                activeSavedTimerId = null;
                activeWorkSessionId = null;
                workIdleStopped = false;
                SaveWorkState();
            }
            UpdateInterface();
        }

        private void AddLap()
        {
            if (!stopwatch.IsRunning) return;
            laps.Add(stopwatch.Elapsed);
            RebuildLaps();
        }

        private void RebuildLaps()
        {
            int shown = Math.Min(lapChips.Length, laps.Count);
            int first = laps.Count - shown;
            TimeSpan best = TimeSpan.MaxValue;
            TimeSpan last = TimeSpan.Zero;
            for (int i = 0; i < laps.Count; i++)
            {
                TimeSpan split = laps[i] - (i > 0 ? laps[i - 1] : TimeSpan.Zero);
                if (split < best) best = split;
                if (i == laps.Count - 1) last = split;
            }

            for (int slot = 0; slot < lapChips.Length; slot++)
            {
                int index = first + slot;
                bool visible = slot < shown;
                if (lapChips[slot].Visible != visible) lapChips[slot].Visible = visible;
                if (!visible) continue;
                TimeSpan split = laps[index] - (index > 0 ? laps[index - 1] : TimeSpan.Zero);
                bool isBest = laps.Count > 1 && split == best;
                Ui.SetText(lapChipTitles[slot], "LAP " + (index + 1));
                Ui.SetText(lapChipValues[slot], DialControl.FormatLap(split));
                Ui.SetColor(lapChipValues[slot], isBest ? Palette.Accent : Palette.Ink);
            }

            if (laps.Count == 0)
                Ui.SetText(lapsInfoLabel, "Press L or Lap while running to split");
            else if (laps.Count == 1)
                Ui.SetText(lapsInfoLabel, "1 lap  ·  " + DialControl.FormatLap(last));
            else
                Ui.SetText(lapsInfoLabel, laps.Count + " laps  ·  best " + DialControl.FormatLap(best) + "  ·  last " + DialControl.FormatLap(last));
        }

        private bool AnythingRunning
        {
            get { return timerRunning || stopwatch.IsRunning || workSession.IsRunning; }
        }

        private void SyncTicker()
        {
            if (!AnythingRunning)
            {
                if (updateTimer.Enabled) updateTimer.Stop();
                return;
            }
            int interval = stopwatch.IsRunning ? 50 : 120;
            if (updateTimer.Interval != interval) updateTimer.Interval = interval;
            if (!updateTimer.Enabled) updateTimer.Start();
        }

        private void Tick(object sender, EventArgs e)
        {
            if (timerRunning)
            {
                timerRemaining = timerEndsAt - DateTime.UtcNow;
                if (timerRemaining <= TimeSpan.Zero)
                {
                    timerRemaining = TimeSpan.Zero;
                    timerRunning = false;
                    timerHasStarted = false;
                    timerComplete = true;
                    SystemSounds.Exclamation.Play();
                    FlashWindow(Handle, true);
                }
            }
            if (workSession.IsRunning && DateTime.UtcNow >= nextWorkAutoSave)
            {
                PersistWorkProgress(savedTimersOpen);
                nextWorkAutoSave = DateTime.UtcNow.AddSeconds(15);
            }
            if (workSession.IsRunning && idleAutoStop && (idleWatchMouse || idleWatchKeyboard || idleWatchSound))
            {
                bool active = (idleWatchMouse && WorkIdle.MouseMoved())
                    || (idleWatchKeyboard && WorkIdle.KeyboardUsed())
                    || (idleWatchSound && WorkIdle.SoundPlaying());
                if (active)
                    workIdleSince = DateTime.UtcNow;
                else if (workIdleSince != DateTime.MinValue
                    && DateTime.UtcNow - workIdleSince >= TimeSpan.FromMinutes(idleMinutes))
                    PauseWorkFromIdle();
            }
            UpdateInterface();
            SyncTicker();
        }

        private void UpdateInterface()
        {
            if (mode == Mode.Settings)
            {
                Ui.SetText(liveStatus, "●  Settings");
                Ui.SetColor(liveStatus, Palette.Muted);
                UpdateWidget();
                SyncTicker();
                return;
            }
            if (mode == Mode.Timer)
            {
                dial.ShowTimer(timerRemaining, timerDuration, timerRunning, timerHasStarted && !timerRunning, timerComplete);
                Ui.SetText(resetButton, "Reset");
                Ui.SetText(primaryButton, timerRunning ? "Pause" : timerComplete ? "Again" : timerHasStarted ? "Resume" : "Start");
                primaryButton.Enabled = timerDuration > TimeSpan.Zero;
                resetButton.Enabled = true;
                Ui.SetText(liveStatus, timerRunning ? "●  In motion" : timerComplete ? "●  Time's up" : "●  Take your time");
                Ui.SetColor(liveStatus, timerRunning || timerComplete ? Palette.AccentDark : Palette.Muted);
            }
            else if (mode == Mode.Stopwatch)
            {
                dial.ShowStopwatch(stopwatch.Elapsed, stopwatch.IsRunning);
                Ui.SetText(resetButton, "Reset");
                Ui.SetText(primaryButton, stopwatch.IsRunning ? "Pause" : stopwatch.Elapsed > TimeSpan.Zero ? "Resume" : "Start");
                primaryButton.Enabled = true;
                lapButton.Enabled = stopwatch.IsRunning;
                Ui.SetText(liveStatus, stopwatch.IsRunning ? "●  In motion" : "●  Take your time");
                Ui.SetColor(liveStatus, stopwatch.IsRunning ? Palette.AccentDark : Palette.Muted);
            }
            else
            {
                TimeSpan totalWork = CurrentWorkTime;
                SavedWorkTimer active = ActiveSavedTimer;
                WorkSessionRecord activeSession = ActiveWorkSession;
                dial.ShowWork(totalWork, workSession.IsRunning, workIdleStopped);
                primaryButton.Enabled = true;
                Ui.SetText(primaryButton, workSession.IsRunning ? "Pause" : totalWork > TimeSpan.Zero ? "Continue" : active != null ? "Start session" : "Start work");
                saveWorkButton.Enabled = totalWork > TimeSpan.Zero;
                Ui.SetText(saveWorkButton, "Save");
                if (saveWorkButton.Visible != (mode == Mode.Work && active == null))
                    saveWorkButton.Visible = mode == Mode.Work && active == null;
                trimWorkButton.Visible = mode == Mode.Work;
                trimWorkButton.Enabled = totalWork > TimeSpan.Zero;
                if (trimBar.Visible) RefreshTrimButtons();
                // Row: [−][primary][Save?][New work / Reset]. Save only exists before
                // the work has been named, so the primary button stretches when it's gone.
                int primaryWidth = active != null ? 210 : 116;
                if (primaryButton.Width != primaryWidth) primaryButton.SetBounds(97, 420, primaryWidth, 48);
                Ui.SetText(resetButton, active != null ? "New work" : "Reset");
                resetButton.Enabled = true;
                if (workIdleStopped)
                {
                    Ui.SetText(liveStatus, "●  Stopped — idle");
                    Ui.SetColor(liveStatus, Palette.DangerText);
                }
                else
                {
                    Ui.SetText(liveStatus, workSession.IsRunning ? "●  Working" : totalWork > TimeSpan.Zero ? "●  Session saved" : active != null ? "●  New session" : "●  Ready to work");
                    Ui.SetColor(liveStatus, workSession.IsRunning ? Palette.AccentDark : Palette.Muted);
                }
                if (workIdleStopped)
                {
                    Ui.SetText(workInfoLabel, IdlePauseReason());
                    Ui.SetColor(workInfoLabel, Palette.DangerText);
                }
                else if (active != null)
                {
                    TimeSpan overall = CalculateSavedTimerTotal(active, true);
                    string sessionName = activeSession == null ? "New session" : activeSession.Name;
                    if (sessionName.Length > 18) sessionName = sessionName.Substring(0, 17) + "...";
                    Ui.SetText(workInfoLabel, sessionName + "  ·  " + DialControl.FormatWork(overall) + " overall");
                    Ui.SetColor(workInfoLabel, Palette.Accent);
                    Ui.SetText(shortcutLabel, "SPACE  pause / resume     R  new work");
                }
                else
                {
                    Ui.SetText(workInfoLabel, "Saved automatically on this PC");
                    Ui.SetColor(workInfoLabel, Palette.Muted);
                    Ui.SetText(shortcutLabel, "SPACE  start / pause     R  reset");
                }
            }
            UpdateWidget();
            SyncTicker();
        }

        private void UpdateWidget()
        {
            if (!showOverlay)
            {
                widget.HideWidget();
                return;
            }
            bool timerLive = timerRunning || timerHasStarted || timerComplete;
            bool swLive = stopwatch.IsRunning || stopwatch.Elapsed > TimeSpan.Zero;
            bool workLive = workSession.IsRunning || CurrentWorkTime > TimeSpan.Zero;

            if (timerRunning || (mode == Mode.Timer && timerLive))
                widget.ShowTimer(timerRemaining, timerDuration);
            else if (stopwatch.IsRunning || (mode == Mode.Stopwatch && swLive))
                widget.ShowStopwatch(stopwatch.Elapsed);
            else if (workSession.IsRunning || (mode == Mode.Work && workLive))
                widget.ShowWork(CurrentWorkTime);
            else if (timerLive)
                widget.ShowTimer(timerRemaining, timerDuration);
            else if (swLive)
                widget.ShowStopwatch(stopwatch.Elapsed);
            else if (workLive)
                widget.ShowWork(CurrentWorkTime);
            else
                widget.HideWidget();

            widget.SetRunning(timerRunning || stopwatch.IsRunning || workSession.IsRunning);
        }

        private void HandleShortcut(object sender, KeyEventArgs e)
        {
            if (savedTimersOpen)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    CloseSavedTimers();
                    e.SuppressKeyPress = true;
                }
                return;
            }
            if (ActiveControl is TextBox) return;
            if (mode == Mode.Settings) return;
            if (e.KeyCode == Keys.Space)
            {
                PrimaryClicked(this, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.R)
            {
                ResetCurrent();
            }
            else if (e.KeyCode == Keys.L && mode == Mode.Stopwatch)
            {
                AddLap();
            }
        }
    }

    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            bool createdNew;
            using (System.Threading.Mutex singleInstance = new System.Threading.Mutex(true, "TempoApp_7D4A8109_2F6A_4E71_9B42", out createdNew))
            {
                if (!createdNew) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TempoForm());
            }
        }
    }
}
