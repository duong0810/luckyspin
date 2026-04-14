namespace GUI
{
    partial class frmMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.contentPanel = new DevExpress.XtraEditors.PanelControl();
            this.accordionControl1 = new DevExpress.XtraBars.Navigation.AccordionControl();
            this.accordingControlElement2 = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.mnLuckyDraw = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.mnProgram = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.accordionControlElement1 = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.mnGiaoDien = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.mnVongquay = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.mnThoat = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            this.lblTieuDeLabel = new DevExpress.XtraEditors.LabelControl();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.contentPanel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.accordionControl1)).BeginInit();
            this.SuspendLayout();
            // 
            // contentPanel
            // 
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(352, 49);
            this.contentPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(674, 598);
            this.contentPanel.TabIndex = 0;
            // 
            // accordionControl1
            // 
            this.accordionControl1.Dock = System.Windows.Forms.DockStyle.Left;
            this.accordionControl1.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] {
            this.accordingControlElement2,
            this.accordionControlElement1});
            this.accordionControl1.Location = new System.Drawing.Point(0, 49);
            this.accordionControl1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.accordionControl1.Name = "accordionControl1";
            this.accordionControl1.ScrollBarMode = DevExpress.XtraBars.Navigation.ScrollBarMode.Touch;
            this.accordionControl1.Size = new System.Drawing.Size(352, 598);
            this.accordionControl1.TabIndex = 1;
            this.accordionControl1.ViewType = DevExpress.XtraBars.Navigation.AccordionControlViewType.HamburgerMenu;
            // 
            // accordingControlElement2
            // 
            this.accordingControlElement2.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] {
            this.mnLuckyDraw,
            this.mnProgram});
            this.accordingControlElement2.Expanded = true;
            this.accordingControlElement2.Name = "accordingControlElement2";
            this.accordingControlElement2.Text = "DANH MỤC";
            // 
            // mnLuckyDraw
            // 
            this.mnLuckyDraw.HeaderTemplate.AddRange(new DevExpress.XtraBars.Navigation.HeaderElementInfo[] {
            new DevExpress.XtraBars.Navigation.HeaderElementInfo(DevExpress.XtraBars.Navigation.HeaderElementType.Text),
            new DevExpress.XtraBars.Navigation.HeaderElementInfo(DevExpress.XtraBars.Navigation.HeaderElementType.Image),
            new DevExpress.XtraBars.Navigation.HeaderElementInfo(DevExpress.XtraBars.Navigation.HeaderElementType.HeaderControl),
            new DevExpress.XtraBars.Navigation.HeaderElementInfo(DevExpress.XtraBars.Navigation.HeaderElementType.ContextButtons)});
            this.mnLuckyDraw.Name = "mnLuckyDraw";
            this.mnLuckyDraw.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            this.mnLuckyDraw.Text = "LuckyDraw";
            this.mnLuckyDraw.Click += new System.EventHandler(this.mnLuckyDraw_Click);
            // 
            // mnProgram
            // 
            this.mnProgram.Name = "mnProgram";
            this.mnProgram.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            this.mnProgram.Text = "Program&History";
            this.mnProgram.Click += new System.EventHandler(this.mnProgram_Click);
            // 
            // accordionControlElement1
            // 
            this.accordionControlElement1.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] {
            this.mnGiaoDien,
            this.mnVongquay,
            this.mnThoat});
            this.accordionControlElement1.Expanded = true;
            this.accordionControlElement1.Name = "accordionControlElement1";
            this.accordionControlElement1.Text = "HỆ THỐNG";
            this.accordionControlElement1.Click += new System.EventHandler(this.accordionControlElement1_Click);
            // 
            // mnGiaoDien
            // 
            this.mnGiaoDien.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("mnGiaoDien.ImageOptions.Image")));
            this.mnGiaoDien.Name = "mnGiaoDien";
            this.mnGiaoDien.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            this.mnGiaoDien.Text = "Tuỳ chỉnh";
            this.mnGiaoDien.Click += new System.EventHandler(this.mnGiaoDien_Click);
            // 
            // mnVongquay
            // 
            this.mnVongquay.Expanded = true;
            this.mnVongquay.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("mnVongquay.ImageOptions.Image")));
            this.mnVongquay.Name = "mnVongquay";
            this.mnVongquay.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            this.mnVongquay.Text = "Vòng quay may mắn";
            this.mnVongquay.Click += new System.EventHandler(this.mnVongquay_Click);
            // 
            // mnThoat
            // 
            this.mnThoat.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("mnThoat.ImageOptions.Image")));
            this.mnThoat.Name = "mnThoat";
            this.mnThoat.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            this.mnThoat.Text = "Thoát";
            this.mnThoat.Click += new System.EventHandler(this.mnThoat_Click);
            // 
            // lblTieuDeLabel
            // 
            this.lblTieuDeLabel.Appearance.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblTieuDeLabel.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.lblTieuDeLabel.Appearance.Options.UseFont = true;
            this.lblTieuDeLabel.Appearance.Options.UseForeColor = true;
            this.lblTieuDeLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.lblTieuDeLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTieuDeLabel.Location = new System.Drawing.Point(0, 0);
            this.lblTieuDeLabel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lblTieuDeLabel.Name = "lblTieuDeLabel";
            this.lblTieuDeLabel.Padding = new System.Windows.Forms.Padding(10, 10, 0, 10);
            this.lblTieuDeLabel.Size = new System.Drawing.Size(1026, 49);
            this.lblTieuDeLabel.TabIndex = 2;
            this.lblTieuDeLabel.Text = "Quản lý phần thưởng";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1026, 647);
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.accordionControl1);
            this.Controls.Add(this.lblTieuDeLabel);
            this.IconOptions.Image = ((System.Drawing.Image)(resources.GetObject("frmMain.IconOptions.Image")));
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Kosmos Development";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.contentPanel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.accordionControl1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.PanelControl contentPanel;
        private DevExpress.XtraBars.Navigation.AccordionControl accordionControl1;
        private DevExpress.XtraBars.Navigation.AccordionControlElement accordingControlElement2;
        private DevExpress.XtraBars.Navigation.AccordionControlElement accordionControlElement1;
        private DevExpress.XtraBars.Navigation.AccordionControlElement mnGiaoDien;
        private DevExpress.XtraBars.Navigation.AccordionControlElement mnThoat;
        private DevExpress.XtraBars.Navigation.AccordionControlElement mnVongquay;
        private DevExpress.XtraEditors.LabelControl lblTieuDeLabel;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private DevExpress.XtraBars.Navigation.AccordionControlElement mnLuckyDraw;
        private DevExpress.XtraBars.Navigation.AccordionControlElement mnProgram;
    }
}