using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace GUI.UI.modules
{
    public partial class frmPrizeNotification : Form
    {
        private Timer fadeTimer;
        private float opacityStep = 0.12f;

        private GraphicsPath cachedPath;
        private LinearGradientBrush cachedBrush;

        // ⭐ THÊM FLAG ĐỂ TẮT FADE
        private bool _disableFade = false;

        public frmPrizeNotification(string prizeText, Control parentControl)
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.White;
            this.Width = 450;
            this.Height = 250;

            // ⭐ THAY ĐỔI: Không đặt Opacity = 0 nữa, để hiện ngay
            this.Opacity = 1.0; // Hiện ngay lập tức

            CreateRoundedRegion();

            int padding = 20;
            int currentTop = padding;

            // Tiêu đề
            Label lblTitle = new Label
            {
                Text = "🎉Xin chúc mừng! 🎉",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.DarkMagenta,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Left = padding,
                Width = this.Width - padding * 2
            };
            Size titleSize = TextRenderer.MeasureText(lblTitle.Text, lblTitle.Font,
                new Size(lblTitle.Width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            lblTitle.Height = Math.Max(40, titleSize.Height + 8);
            lblTitle.Top = currentTop;
            this.Controls.Add(lblTitle);

            currentTop += lblTitle.Height + 6;

            // Nội dung phần thưởng
            Label lblPrize = new Label
            {
                Text = prizeText ?? string.Empty,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.MediumVioletRed,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Left = padding,
                Width = this.Width - padding * 2
            };
            Size prizeSize = TextRenderer.MeasureText(lblPrize.Text, lblPrize.Font,
                new Size(lblPrize.Width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            lblPrize.Height = Math.Max(48, prizeSize.Height + 8);
            lblPrize.Top = currentTop;
            this.Controls.Add(lblPrize);

            currentTop += lblPrize.Height + 12;

            // Nút đóng
            Button btnOK = new Button
            {
                Text = "Đóng",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Width = 100,
                Height = 36,
                FlatStyle = FlatStyle.Flat
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += (s, e) => this.Close();
            btnOK.Left = (this.Width - btnOK.Width) / 2;
            btnOK.Top = currentTop;
            this.Controls.Add(btnOK);

            currentTop += btnOK.Height + padding;

            this.Height = Math.Max(this.Height, currentTop);
            CreateRoundedRegion();

            cachedBrush = new LinearGradientBrush(
                this.ClientRectangle,
                Color.White,
                Color.BlueViolet,
                LinearGradientMode.ForwardDiagonal);

            if (parentControl != null)
            {
                this.Left = parentControl.Left + (parentControl.Width - this.Width) / 2;
                this.Top = parentControl.Top + (parentControl.Height - this.Height) / 2;
            }

            // ⭐ XÓA BỎ TIMER FADE (không cần nữa)
            // fadeTimer = new Timer();
            // fadeTimer.Interval = 30;
            // fadeTimer.Tick += FadeTimer_Tick;
            // fadeTimer.Start();
        }

        private void CreateRoundedRegion()
        {
            cachedPath?.Dispose();

            cachedPath = new GraphicsPath();
            int cornerRadius = 20;

            cachedPath.AddArc(0, 0, cornerRadius, cornerRadius, 180, 90);
            cachedPath.AddArc(this.Width - cornerRadius, 0, cornerRadius, cornerRadius, 270, 90);
            cachedPath.AddArc(this.Width - cornerRadius, this.Height - cornerRadius,
                cornerRadius, cornerRadius, 0, 90);
            cachedPath.AddArc(0, this.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            cachedPath.CloseAllFigures();

            this.Region = new Region(cachedPath);
        }

        // ⭐ XÓA METHOD NÀY (không cần fade nữa)
        // private void FadeTimer_Tick(object sender, EventArgs e) { ... }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (cachedBrush != null)
            {
                e.Graphics.FillRectangle(cachedBrush, this.ClientRectangle);
            }
        }

        // ⭐ UNCOMMENT DISPOSE (quan trọng để tránh memory leak)
        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        // Dispose timer nếu còn (trong trường hợp sau này cần lại)
        //        if (fadeTimer != null)
        //        {
        //            fadeTimer.Stop();
        //            //fadeTimer.Tick -= FadeTimer_Tick;
        //            fadeTimer.Dispose();
        //            fadeTimer = null;
        //        }

        //        // Dispose cached objects
        //        cachedPath?.Dispose();
        //        cachedBrush?.Dispose();
        //        cachedPath = null;
        //        cachedBrush = null;
        //    }

        //    base.Dispose(disposing);
        //}

    }
}