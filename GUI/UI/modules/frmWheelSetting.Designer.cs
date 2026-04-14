namespace GUI.UI.modules
{
    partial class frmWheelSetting
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.groupControl1 = new DevExpress.XtraEditors.GroupControl();
            this.radioGroupStyle = new DevExpress.XtraEditors.RadioGroup();
            this.groupControl2 = new DevExpress.XtraEditors.GroupControl();
            this.lblDurationValue = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.trackBarDuration = new DevExpress.XtraEditors.TrackBarControl();
            this.BtnShuffle = new DevExpress.XtraEditors.SimpleButton();
            this.BtnSave = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).BeginInit();
            this.groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radioGroupStyle.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl2)).BeginInit();
            this.groupControl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarDuration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarDuration.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupControl1
            // 
            this.groupControl1.Controls.Add(this.radioGroupStyle);
            this.groupControl1.Location = new System.Drawing.Point(12, 12);
            this.groupControl1.Name = "groupControl1";
            this.groupControl1.Size = new System.Drawing.Size(460, 200);
            this.groupControl1.TabIndex = 0;
            this.groupControl1.Text = "🎨 Chọn Style Vòng Quay";
            // 
            // radioGroupStyle
            // 
            this.radioGroupStyle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radioGroupStyle.Location = new System.Drawing.Point(2, 23);
            this.radioGroupStyle.Name = "radioGroupStyle";
            this.radioGroupStyle.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.radioGroupStyle.Properties.Appearance.Options.UseFont = true;
            this.radioGroupStyle.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem(null, "⭐ Mặc định"), // THÊM DÒNG NÀY
            new DevExpress.XtraEditors.Controls.RadioGroupItem(0, "🎊 Tết Nguyên Đán"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem(1, "🎄 Giáng Sinh"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem(2, "🎃 Halloween"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem(3, "🏮 Trung Thu"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem(4, "💎 Modern Neon")});
            this.radioGroupStyle.Size = new System.Drawing.Size(456, 175);
            this.radioGroupStyle.TabIndex = 0;
            this.radioGroupStyle.SelectedIndexChanged += new System.EventHandler(this.RadioGroupStyle_SelectedIndexChanged);
            // 
            // groupControl2
            // 
            this.groupControl2.Controls.Add(this.lblDurationValue);
            this.groupControl2.Controls.Add(this.labelControl1);
            this.groupControl2.Controls.Add(this.trackBarDuration);
            this.groupControl2.Location = new System.Drawing.Point(12, 218);
            this.groupControl2.Name = "groupControl2";
            this.groupControl2.Size = new System.Drawing.Size(460, 120);
            this.groupControl2.TabIndex = 1;
            this.groupControl2.Text = "⏱️ Thời Gian Quay";
            // 
            // lblDurationValue
            // 
            this.lblDurationValue.Appearance.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblDurationValue.Appearance.ForeColor = System.Drawing.Color.DarkRed;
            this.lblDurationValue.Appearance.Options.UseFont = true;
            this.lblDurationValue.Appearance.Options.UseForeColor = true;
            this.lblDurationValue.Location = new System.Drawing.Point(405, 40);
            this.lblDurationValue.Name = "lblDurationValue";
            this.lblDurationValue.Size = new System.Drawing.Size(22, 25);
            this.lblDurationValue.TabIndex = 2;
            this.lblDurationValue.Text = "5";
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(15, 45);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(168, 17);
            this.labelControl1.TabIndex = 1;
            this.labelControl1.Text = "Thời gian (giây): 5 - 30 giây";
            // 
            // trackBarDuration
            // 
            this.trackBarDuration.EditValue = 5;
            this.trackBarDuration.Location = new System.Drawing.Point(15, 68);
            this.trackBarDuration.Name = "trackBarDuration";
            this.trackBarDuration.Properties.LabelAppearance.Options.UseTextOptions = true;
            this.trackBarDuration.Properties.LabelAppearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.trackBarDuration.Properties.Maximum = 30;
            this.trackBarDuration.Properties.Minimum = 5;
            this.trackBarDuration.Properties.ShowLabels = true;
            this.trackBarDuration.Properties.TickFrequency = 5;
            this.trackBarDuration.Size = new System.Drawing.Size(430, 45);
            this.trackBarDuration.TabIndex = 0;
            this.trackBarDuration.Value = 5;
            this.trackBarDuration.EditValueChanged += new System.EventHandler(this.TrackBarDuration_ValueChanged);
            // 
            // BtnShuffle
            // 
            this.BtnShuffle.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.BtnShuffle.Appearance.Options.UseFont = true;
            this.BtnShuffle.ImageOptions.SvgImageSize = new System.Drawing.Size(24, 24);
            this.BtnShuffle.Location = new System.Drawing.Point(10, 10);
            this.BtnShuffle.Name = "BtnShuffle";
            this.BtnShuffle.Size = new System.Drawing.Size(140, 40);
            this.BtnShuffle.TabIndex = 2;
            this.BtnShuffle.Text = "🔀 Tráo Vị Trí";
            this.BtnShuffle.Click += new System.EventHandler(this.BtnShuffle_Click);
            // 
            // BtnSave
            // 
            this.BtnSave.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.BtnSave.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.BtnSave.Appearance.Options.UseBackColor = true;
            this.BtnSave.Appearance.Options.UseFont = true;
            this.BtnSave.Location = new System.Drawing.Point(230, 10);
            this.BtnSave.Name = "BtnSave";
            this.BtnSave.Size = new System.Drawing.Size(110, 40);
            this.BtnSave.TabIndex = 3;
            this.BtnSave.Text = "✅ Lưu";
            this.BtnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // BtnCancel
            // 
            this.BtnCancel.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.BtnCancel.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.BtnCancel.Appearance.Options.UseBackColor = true;
            this.BtnCancel.Appearance.Options.UseFont = true;
            this.BtnCancel.Location = new System.Drawing.Point(346, 10);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(110, 40);
            this.BtnCancel.TabIndex = 4;
            this.BtnCancel.Text = "❌ Hủy";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // panelControl1
            // 
            this.panelControl1.Controls.Add(this.BtnShuffle);
            this.panelControl1.Controls.Add(this.BtnCancel);
            this.panelControl1.Controls.Add(this.BtnSave);
            this.panelControl1.Location = new System.Drawing.Point(12, 344);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(460, 60);
            this.panelControl1.TabIndex = 5;
            // 
            // frmWheelSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 416);
            this.Controls.Add(this.panelControl1);
            this.Controls.Add(this.groupControl2);
            this.Controls.Add(this.groupControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            //this.IconOptions.SvgImage = global::GUI.Properties.Resources.bo_appearance;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmWheelSetting";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "⚙️ Cài Đặt Vòng Quay";
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).EndInit();
            this.groupControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radioGroupStyle.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl2)).EndInit();
            this.groupControl2.ResumeLayout(false);
            this.groupControl2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarDuration.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarDuration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.GroupControl groupControl1;
        private DevExpress.XtraEditors.RadioGroup radioGroupStyle;
        private DevExpress.XtraEditors.GroupControl groupControl2;
        private DevExpress.XtraEditors.LabelControl lblDurationValue;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.TrackBarControl trackBarDuration;
        private DevExpress.XtraEditors.SimpleButton BtnShuffle;
        private DevExpress.XtraEditors.SimpleButton BtnSave;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
        private DevExpress.XtraEditors.PanelControl panelControl1;
    }
}