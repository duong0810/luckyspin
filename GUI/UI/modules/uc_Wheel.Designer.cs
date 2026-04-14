using System.Drawing;

namespace GUI.UI.modules
{
    partial class uc_Wheel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblPhone;
        private DevExpress.XtraEditors.TextEdit txtCustomerName;
        private System.Windows.Forms.Label lblCustomerName;
        private DevExpress.XtraEditors.TextEdit txtInvoiceNumber;
        private System.Windows.Forms.Label lblInvoiceNumber;
        private System.Windows.Forms.Label marqueeLabel;
        private System.Windows.Forms.Label lblSpinMode; // Label hiển thị chế độ vòng quay
        private System.Windows.Forms.TableLayoutPanel tableLists;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // ⭐ 1. DISPOSE COMPONENTS (Component Container)
                if (components != null)
                {
                    components.Dispose();
                }

                // ⭐ 2. DISPOSE TIMERS
                try
                {
                    if (spinTimer != null)
                    {
                        spinTimer.Stop();
                        spinTimer.Dispose();
                    }
                }
                catch { }

                try
                {
                    if (marqueeTimer != null)
                    {
                        marqueeTimer.Stop();
                        marqueeTimer.Dispose();
                    }
                }
                catch { }

                try
                {
                    if (fireworkDisplayTimer != null)
                    {
                        fireworkDisplayTimer.Stop();
                        fireworkDisplayTimer.Dispose();
                    }
                }
                catch { }

                try
                {
                    if (particleTimer != null)
                    {
                        particleTimer.Stop();
                        particleTimer.Dispose();
                    }
                }
                catch { }

                // ⭐ 3. DISPOSE FIREWORK PICTUREBOXES
                if (fireworkGifs != null)
                {
                    foreach (var fireworkGif in fireworkGifs)
                    {
                        try
                        {
                            if (fireworkGif?.Image != null)
                            {
                                ImageAnimator.StopAnimate(fireworkGif.Image, null);
                                fireworkGif.Image.Dispose();
                            }
                            fireworkGif?.Dispose();
                        }
                        catch { }
                    }
                    fireworkGifs.Clear();
                }

                // ⭐ 4. DISPOSE IMAGES & BITMAPS
                try
                {
                    if (wheelBackgroundImage != null)
                    {
                        wheelBackgroundImage.Dispose();
                        wheelBackgroundImage = null;
                    }
                }
                catch { }

                try
                {
                    if (cachedBackground != null)
                    {
                        cachedBackground.Dispose();
                        cachedBackground = null;
                    }
                }
                catch { }

                // ⭐ 5. DISPOSE SEGMENT IMAGES
                if (segmentImageCache != null)
                {
                    foreach (var img in segmentImageCache)
                    {
                        try
                        {
                            img?.Dispose();
                        }
                        catch { }
                    }
                    segmentImageCache.Clear();
                }

                // ⭐ 6. DISPOSE DECORATION ICONS CACHE
                if (decorationIconsCache != null)
                {
                    foreach (var kvp in decorationIconsCache)
                    {
                        if (kvp.Value != null)
                        {
                            foreach (var icon in kvp.Value)
                            {
                                try
                                {
                                    icon?.Dispose();
                                }
                                catch { }
                            }
                        }
                    }
                    decorationIconsCache.Clear();
                }

                // ⭐ 7. DISPOSE SPIN BUTTON IMAGES (THEO THEME)
                if (spinButtonImages != null)
                {
                    foreach (var img in spinButtonImages.Values)
                    {
                        try
                        {
                            img?.Dispose();
                        }
                        catch { }
                    }
                    spinButtonImages.Clear();
                }

                // ⭐ 8. DISPOSE PRIZE IMAGE CACHE
                ClearPrizeImageCache();

                // ⭐ 9. DISPOSE DECORATIVE PARTICLES
                if (decorativeParticles != null)
                {
                    foreach (var particle in decorativeParticles)
                    {
                        try
                        {
                            particle.Icon?.Dispose();
                        }
                        catch { }
                    }
                    decorativeParticles.Clear();
                }

                // ⭐ 10. DISPOSE CACHED FONTS
                DisposeCachedFonts();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uc_Wheel));
            this.lblPhone = new System.Windows.Forms.Label();
            this.marqueeLabel = new System.Windows.Forms.Label();
            this.lblCustomerName = new System.Windows.Forms.Label();
            this.txtCustomerName = new DevExpress.XtraEditors.TextEdit();
            this.lblInvoiceNumber = new System.Windows.Forms.Label();
            this.txtInvoiceNumber = new DevExpress.XtraEditors.TextEdit();
            this.lblSpinMode = new System.Windows.Forms.Label();
            this.lstPrizes = new System.Windows.Forms.ListBox();
            this.lstWinners = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.gridLookUpEditEvent = new DevExpress.XtraEditors.GridLookUpEdit();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.txtPhone = new DevExpress.XtraEditors.TextEdit();
            this.dockManager1 = new DevExpress.XtraBars.Docking.DockManager(this.components);
            this.dockPanel1 = new DevExpress.XtraBars.Docking.DockPanel();
            this.dockPanel1_Container = new DevExpress.XtraBars.Docking.ControlContainer();
            this.btnXem = new DevExpress.XtraEditors.SimpleButton();
            this.deDateTo = new DevExpress.XtraEditors.DateEdit();
            this.sePrinceMax = new DevExpress.XtraEditors.SpinEdit();
            this.sePriceMin = new DevExpress.XtraEditors.SpinEdit();
            this.slueWarehouse = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.searchLookUpEdit1View = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ccbeBranch = new DevExpress.XtraEditors.CheckedComboBoxEdit();
            this.lblWarehouse = new System.Windows.Forms.Label();
            this.deDateFrom = new DevExpress.XtraEditors.DateEdit();
            this.lblPriceMax = new System.Windows.Forms.Label();
            this.lblPriceMin = new System.Windows.Forms.Label();
            this.lblBranch = new System.Windows.Forms.Label();
            this.lblDateTo = new System.Windows.Forms.Label();
            this.lblDateFrom = new System.Windows.Forms.Label();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.gcPhoneNumbers = new DevExpress.XtraGrid.GridControl();
            this.gridView2 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.txtCustomerName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtInvoiceNumber.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEditEvent.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPhone.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dockManager1)).BeginInit();
            this.dockPanel1.SuspendLayout();
            this.dockPanel1_Container.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.deDateTo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deDateTo.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sePrinceMax.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sePriceMin.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.slueWarehouse.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1View)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ccbeBranch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deDateFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deDateFrom.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcPhoneNumbers)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView2)).BeginInit();
            this.SuspendLayout();
            // 
            // lblPhone
            // 
            this.lblPhone.BackColor = System.Drawing.Color.Transparent;
            this.lblPhone.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPhone.ForeColor = System.Drawing.Color.DarkRed;
            this.lblPhone.Location = new System.Drawing.Point(5, 54);
            this.lblPhone.Name = "lblPhone";
            this.lblPhone.Size = new System.Drawing.Size(129, 26);
            this.lblPhone.TabIndex = 0;
            this.lblPhone.Text = "Nhập số điện thoại:";
            // 
            // marqueeLabel
            // 
            this.marqueeLabel.AutoSize = true;
            this.marqueeLabel.BackColor = System.Drawing.Color.Transparent;
            this.marqueeLabel.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.marqueeLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.marqueeLabel.Location = new System.Drawing.Point(377, 207);
            this.marqueeLabel.Name = "marqueeLabel";
            this.marqueeLabel.Size = new System.Drawing.Size(866, 41);
            this.marqueeLabel.TabIndex = 0;
            this.marqueeLabel.Text = "CHÀO MỪNG QUÝ KHÁCH ĐẾN VỚI VÒNG QUAY MAY MẮN";
            this.marqueeLabel.Paint += new System.Windows.Forms.PaintEventHandler(this.marqueeLabel_Paint);
            // 
            // lblCustomerName
            // 
            this.lblCustomerName.BackColor = System.Drawing.Color.Transparent;
            this.lblCustomerName.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCustomerName.ForeColor = System.Drawing.Color.DarkRed;
            this.lblCustomerName.Location = new System.Drawing.Point(3, 84);
            this.lblCustomerName.Name = "lblCustomerName";
            this.lblCustomerName.Size = new System.Drawing.Size(131, 26);
            this.lblCustomerName.TabIndex = 2;
            this.lblCustomerName.Text = "Tên khách hàng:";
            // 
            // txtCustomerName
            // 
            this.txtCustomerName.Location = new System.Drawing.Point(140, 82);
            this.txtCustomerName.Name = "txtCustomerName";
            this.txtCustomerName.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.txtCustomerName.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCustomerName.Properties.Appearance.ForeColor = System.Drawing.Color.Black;
            this.txtCustomerName.Properties.Appearance.Options.UseBackColor = true;
            this.txtCustomerName.Properties.Appearance.Options.UseFont = true;
            this.txtCustomerName.Properties.Appearance.Options.UseForeColor = true;
            this.txtCustomerName.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.txtCustomerName.Properties.MaxLength = 50;
            this.txtCustomerName.Properties.NullText = "Nhập tên...";
            this.txtCustomerName.Size = new System.Drawing.Size(137, 22);
            this.txtCustomerName.TabIndex = 3;
            // 
            // lblInvoiceNumber
            // 
            this.lblInvoiceNumber.BackColor = System.Drawing.Color.Transparent;
            this.lblInvoiceNumber.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInvoiceNumber.ForeColor = System.Drawing.Color.DarkRed;
            this.lblInvoiceNumber.Location = new System.Drawing.Point(5, 118);
            this.lblInvoiceNumber.Name = "lblInvoiceNumber";
            this.lblInvoiceNumber.Size = new System.Drawing.Size(129, 26);
            this.lblInvoiceNumber.TabIndex = 4;
            this.lblInvoiceNumber.Text = "Số hoá đơn:";
            // 
            // txtInvoiceNumber
            // 
            this.txtInvoiceNumber.Location = new System.Drawing.Point(140, 110);
            this.txtInvoiceNumber.Name = "txtInvoiceNumber";
            this.txtInvoiceNumber.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.txtInvoiceNumber.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtInvoiceNumber.Properties.Appearance.ForeColor = System.Drawing.Color.Black;
            this.txtInvoiceNumber.Properties.Appearance.Options.UseBackColor = true;
            this.txtInvoiceNumber.Properties.Appearance.Options.UseFont = true;
            this.txtInvoiceNumber.Properties.Appearance.Options.UseForeColor = true;
            this.txtInvoiceNumber.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.txtInvoiceNumber.Properties.MaxLength = 20;
            this.txtInvoiceNumber.Properties.NullText = "Nhập số hoá đơn...";
            this.txtInvoiceNumber.Size = new System.Drawing.Size(137, 22);
            this.txtInvoiceNumber.TabIndex = 4;
            // 
            // lblSpinMode
            // 
            this.lblSpinMode.BackColor = System.Drawing.Color.Transparent;
            this.lblSpinMode.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSpinMode.ForeColor = System.Drawing.Color.DarkBlue;
            this.lblSpinMode.Location = new System.Drawing.Point(3, 0);
            this.lblSpinMode.Name = "lblSpinMode";
            this.lblSpinMode.Size = new System.Drawing.Size(131, 25);
            this.lblSpinMode.TabIndex = 0;
            this.lblSpinMode.Text = "Chế độ: 1";
            // 
            // lstPrizes
            // 
            this.lstPrizes.FormattingEnabled = true;
            this.lstPrizes.ItemHeight = 16;
            this.lstPrizes.Location = new System.Drawing.Point(4, 597);
            this.lstPrizes.Name = "lstPrizes";
            this.lstPrizes.Size = new System.Drawing.Size(263, 340);
            this.lstPrizes.TabIndex = 13;
            this.lstPrizes.Visible = false;
            // 
            // lstWinners
            // 
            this.lstWinners.FormattingEnabled = true;
            this.lstWinners.ItemHeight = 16;
            this.lstWinners.Location = new System.Drawing.Point(4, 967);
            this.lstWinners.Name = "lstWinners";
            this.lstWinners.Size = new System.Drawing.Size(263, 292);
            this.lstWinners.TabIndex = 14;
            this.lstWinners.Visible = false;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.DarkRed;
            this.label1.Location = new System.Drawing.Point(3, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(133, 26);
            this.label1.TabIndex = 10;
            this.label1.Text = "Chương trình:";
            // 
            // gridLookUpEditEvent
            // 
            this.gridLookUpEditEvent.Location = new System.Drawing.Point(140, 20);
            this.gridLookUpEditEvent.Name = "gridLookUpEditEvent";
            this.gridLookUpEditEvent.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.gridLookUpEditEvent.Properties.PopupView = this.gridView1;
            this.gridLookUpEditEvent.Size = new System.Drawing.Size(137, 22);
            this.gridLookUpEditEvent.TabIndex = 1;
            // 
            // gridView1
            // 
            this.gridView1.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.gridView1.Name = "gridView1";
            this.gridView1.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.gridView1.OptionsView.ShowGroupPanel = false;
            // 
            // txtPhone
            // 
            this.txtPhone.Location = new System.Drawing.Point(140, 52);
            this.txtPhone.Name = "txtPhone";
            this.txtPhone.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.txtPhone.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPhone.Properties.Appearance.ForeColor = System.Drawing.Color.Black;
            this.txtPhone.Properties.Appearance.Options.UseBackColor = true;
            this.txtPhone.Properties.Appearance.Options.UseFont = true;
            this.txtPhone.Properties.Appearance.Options.UseForeColor = true;
            this.txtPhone.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.txtPhone.Properties.MaxLength = 50;
            this.txtPhone.Properties.NullText = "Nhập sđt...";
            this.txtPhone.Size = new System.Drawing.Size(137, 22);
            this.txtPhone.TabIndex = 2;
            // 
            // dockManager1
            // 
            this.dockManager1.Form = this;
            this.dockManager1.RootPanels.AddRange(new DevExpress.XtraBars.Docking.DockPanel[] {
            this.dockPanel1});
            this.dockManager1.TopZIndexControls.AddRange(new string[] {
            "DevExpress.XtraBars.BarDockControl",
            "DevExpress.XtraBars.StandaloneBarDockControl",
            "System.Windows.Forms.MenuStrip",
            "System.Windows.Forms.StatusStrip",
            "System.Windows.Forms.StatusBar",
            "DevExpress.XtraBars.Ribbon.RibbonStatusBar",
            "DevExpress.XtraBars.Ribbon.RibbonControl",
            "DevExpress.XtraBars.Navigation.OfficeNavigationBar",
            "DevExpress.XtraBars.Navigation.TileNavPane",
            "DevExpress.XtraBars.TabFormControl",
            "DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl",
            "DevExpress.XtraBars.ToolbarForm.ToolbarFormControl"});
            // 
            // dockPanel1
            // 
            this.dockPanel1.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.dockPanel1.Appearance.Options.UseBackColor = true;
            this.dockPanel1.Controls.Add(this.dockPanel1_Container);
            this.dockPanel1.Dock = DevExpress.XtraBars.Docking.DockingStyle.Left;
            this.dockPanel1.ID = new System.Guid("6c2bc095-8e5a-47f9-9b4e-246869b76be7");
            this.dockPanel1.Location = new System.Drawing.Point(0, 0);
            this.dockPanel1.Name = "dockPanel1";
            this.dockPanel1.OriginalSize = new System.Drawing.Size(288, 200);
            this.dockPanel1.Size = new System.Drawing.Size(288, 1293);
            this.dockPanel1.Text = "vòng quay ";
            // 
            // dockPanel1_Container
            // 
            this.dockPanel1_Container.BackColor = System.Drawing.Color.Transparent;
            this.dockPanel1_Container.Controls.Add(this.btnXem);
            this.dockPanel1_Container.Controls.Add(this.deDateTo);
            this.dockPanel1_Container.Controls.Add(this.lblWarehouse);
            this.dockPanel1_Container.Controls.Add(this.slueWarehouse);
            this.dockPanel1_Container.Controls.Add(this.sePrinceMax);
            this.dockPanel1_Container.Controls.Add(this.sePriceMin);
            this.dockPanel1_Container.Controls.Add(this.ccbeBranch);
            this.dockPanel1_Container.Controls.Add(this.deDateFrom);
            this.dockPanel1_Container.Controls.Add(this.lblPriceMax);
            this.dockPanel1_Container.Controls.Add(this.lblPriceMin);
            this.dockPanel1_Container.Controls.Add(this.lblBranch);
            this.dockPanel1_Container.Controls.Add(this.lblDateTo);
            this.dockPanel1_Container.Controls.Add(this.lblDateFrom);
            this.dockPanel1_Container.Controls.Add(this.labelControl2);
            this.dockPanel1_Container.Controls.Add(this.gcPhoneNumbers);
            this.dockPanel1_Container.Controls.Add(this.labelControl1);
            this.dockPanel1_Container.Controls.Add(this.lstWinners);
            this.dockPanel1_Container.Controls.Add(this.txtInvoiceNumber);
            this.dockPanel1_Container.Controls.Add(this.txtCustomerName);
            this.dockPanel1_Container.Controls.Add(this.txtPhone);
            this.dockPanel1_Container.Controls.Add(this.lblCustomerName);
            this.dockPanel1_Container.Controls.Add(this.lblPhone);
            this.dockPanel1_Container.Controls.Add(this.lblInvoiceNumber);
            this.dockPanel1_Container.Controls.Add(this.label1);
            this.dockPanel1_Container.Controls.Add(this.lstPrizes);
            this.dockPanel1_Container.Controls.Add(this.lblSpinMode);
            this.dockPanel1_Container.Controls.Add(this.gridLookUpEditEvent);
            this.dockPanel1_Container.Location = new System.Drawing.Point(4, 32);
            this.dockPanel1_Container.Name = "dockPanel1_Container";
            this.dockPanel1_Container.Size = new System.Drawing.Size(278, 1257);
            this.dockPanel1_Container.TabIndex = 0;
            // 
            // btnXem
            // 
            this.btnXem.Appearance.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnXem.Appearance.ForeColor = System.Drawing.Color.Green;
            this.btnXem.Appearance.Options.UseFont = true;
            this.btnXem.Appearance.Options.UseForeColor = true;
            this.btnXem.Appearance.Options.UseTextOptions = true;
            this.btnXem.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.btnXem.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnXem.ImageOptions.Image")));
            this.btnXem.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.TopCenter;
            this.btnXem.ImageOptions.ImageToTextIndent = 0;
            this.btnXem.Location = new System.Drawing.Point(232, 144);
            this.btnXem.Name = "btnXem";
            this.btnXem.Size = new System.Drawing.Size(39, 50);
            this.btnXem.TabIndex = 7;
            this.btnXem.Text = "Xem";
            this.btnXem.ToolTip = "Xem";
            this.btnXem.Visible = false;
            // 
            // deDateTo
            // 
            this.deDateTo.EditValue = null;
            this.deDateTo.Location = new System.Drawing.Point(101, 172);
            this.deDateTo.Name = "deDateTo";
            this.deDateTo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deDateTo.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deDateTo.Size = new System.Drawing.Size(125, 22);
            this.deDateTo.TabIndex = 6;
            this.deDateTo.Visible = false;
            // 
            // sePrinceMax
            // 
            this.sePrinceMax.EditValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.sePrinceMax.Location = new System.Drawing.Point(101, 286);
            this.sePrinceMax.Name = "sePrinceMax";
            this.sePrinceMax.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.sePrinceMax.Size = new System.Drawing.Size(125, 24);
            this.sePrinceMax.TabIndex = 11;
            this.sePrinceMax.Visible = false;
            // 
            // sePriceMin
            // 
            this.sePriceMin.EditValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.sePriceMin.Location = new System.Drawing.Point(101, 256);
            this.sePriceMin.Name = "sePriceMin";
            this.sePriceMin.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.sePriceMin.Size = new System.Drawing.Size(125, 24);
            this.sePriceMin.TabIndex = 10;
            this.sePriceMin.Visible = false;
            // 
            // slueWarehouse
            // 
            this.slueWarehouse.Location = new System.Drawing.Point(101, 200);
            this.slueWarehouse.Name = "slueWarehouse";
            this.slueWarehouse.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.slueWarehouse.Properties.PopupView = this.searchLookUpEdit1View;
            this.slueWarehouse.Size = new System.Drawing.Size(125, 22);
            this.slueWarehouse.TabIndex = 8;
            this.slueWarehouse.Visible = false;
            // 
            // searchLookUpEdit1View
            // 
            this.searchLookUpEdit1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.searchLookUpEdit1View.Name = "searchLookUpEdit1View";
            this.searchLookUpEdit1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.searchLookUpEdit1View.OptionsView.ShowGroupPanel = false;
            // 
            // ccbeBranch
            // 
            this.ccbeBranch.Location = new System.Drawing.Point(101, 228);
            this.ccbeBranch.Name = "ccbeBranch";
            this.ccbeBranch.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.ccbeBranch.Size = new System.Drawing.Size(125, 22);
            this.ccbeBranch.TabIndex = 9;
            this.ccbeBranch.Visible = false;
            // 
            // lblWarehouse
            // 
            this.lblWarehouse.AutoSize = true;
            this.lblWarehouse.Location = new System.Drawing.Point(5, 206);
            this.lblWarehouse.Name = "lblWarehouse";
            this.lblWarehouse.Size = new System.Drawing.Size(35, 16);
            this.lblWarehouse.TabIndex = 19;
            this.lblWarehouse.Text = "KHO";
            this.lblWarehouse.Visible = false;
            // 
            // deDateFrom
            // 
            this.deDateFrom.EditValue = null;
            this.deDateFrom.Location = new System.Drawing.Point(101, 144);
            this.deDateFrom.Name = "deDateFrom";
            this.deDateFrom.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deDateFrom.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deDateFrom.Size = new System.Drawing.Size(125, 22);
            this.deDateFrom.TabIndex = 5;
            this.deDateFrom.Visible = false;
            // 
            // lblPriceMax
            // 
            this.lblPriceMax.AutoSize = true;
            this.lblPriceMax.Location = new System.Drawing.Point(4, 294);
            this.lblPriceMax.Name = "lblPriceMax";
            this.lblPriceMax.Size = new System.Drawing.Size(78, 16);
            this.lblPriceMax.TabIndex = 20;
            this.lblPriceMax.Text = "Giá HĐ max";
            this.lblPriceMax.Visible = false;
            // 
            // lblPriceMin
            // 
            this.lblPriceMin.AutoSize = true;
            this.lblPriceMin.Location = new System.Drawing.Point(4, 264);
            this.lblPriceMin.Name = "lblPriceMin";
            this.lblPriceMin.Size = new System.Drawing.Size(78, 16);
            this.lblPriceMin.TabIndex = 21;
            this.lblPriceMin.Text = "GIÁ HĐ min ";
            this.lblPriceMin.Visible = false;
            // 
            // lblBranch
            // 
            this.lblBranch.AutoSize = true;
            this.lblBranch.Location = new System.Drawing.Point(5, 234);
            this.lblBranch.Name = "lblBranch";
            this.lblBranch.Size = new System.Drawing.Size(81, 16);
            this.lblBranch.TabIndex = 17;
            this.lblBranch.Text = "CHI NHÁNH";
            this.lblBranch.Visible = false;
            // 
            // lblDateTo
            // 
            this.lblDateTo.AutoSize = true;
            this.lblDateTo.Location = new System.Drawing.Point(5, 178);
            this.lblDateTo.Name = "lblDateTo";
            this.lblDateTo.Size = new System.Drawing.Size(64, 16);
            this.lblDateTo.TabIndex = 16;
            this.lblDateTo.Text = "Đến ngày";
            this.lblDateTo.Visible = false;
            // 
            // lblDateFrom
            // 
            this.lblDateFrom.AutoSize = true;
            this.lblDateFrom.Location = new System.Drawing.Point(5, 150);
            this.lblDateFrom.Name = "lblDateFrom";
            this.lblDateFrom.Size = new System.Drawing.Size(56, 16);
            this.lblDateFrom.TabIndex = 15;
            this.lblDateFrom.Text = "Từ ngày";
            this.lblDateFrom.Visible = false;
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(4, 573);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(172, 18);
            this.labelControl2.TabIndex = 12;
            this.labelControl2.Text = "Danh sách phần thưởng";
            this.labelControl2.Visible = false;
            // 
            // gcPhoneNumbers
            // 
            this.gcPhoneNumbers.Location = new System.Drawing.Point(3, 316);
            this.gcPhoneNumbers.MainView = this.gridView2;
            this.gcPhoneNumbers.Name = "gcPhoneNumbers";
            this.gcPhoneNumbers.Size = new System.Drawing.Size(264, 251);
            this.gcPhoneNumbers.TabIndex = 12;
            this.gcPhoneNumbers.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView2});
            this.gcPhoneNumbers.Visible = false;
            // 
            // gridView2
            // 
            this.gridView2.GridControl = this.gcPhoneNumbers;
            this.gridView2.Name = "gridView2";
            this.gridView2.OptionsView.ShowGroupPanel = false;
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(4, 943);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(176, 18);
            this.labelControl1.TabIndex = 11;
            this.labelControl1.Text = "Danh sách trúng thưởng";
            this.labelControl1.Visible = false;
            // 
            // uc_Wheel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.marqueeLabel);
            this.Controls.Add(this.dockPanel1);
            this.Name = "uc_Wheel";
            this.Size = new System.Drawing.Size(1295, 1293);
            ((System.ComponentModel.ISupportInitialize)(this.txtCustomerName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtInvoiceNumber.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridLookUpEditEvent.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPhone.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dockManager1)).EndInit();
            this.dockPanel1.ResumeLayout(false);
            this.dockPanel1_Container.ResumeLayout(false);
            this.dockPanel1_Container.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.deDateTo.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deDateTo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sePrinceMax.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sePriceMin.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.slueWarehouse.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.searchLookUpEdit1View)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ccbeBranch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deDateFrom.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deDateFrom.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcPhoneNumbers)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox lstPrizes;
        private System.Windows.Forms.ListBox lstWinners;
        private System.Windows.Forms.Label label1;
        private DevExpress.XtraEditors.GridLookUpEdit gridLookUpEditEvent;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.TextEdit txtPhone;
        private DevExpress.XtraBars.Docking.DockManager dockManager1;
        private DevExpress.XtraBars.Docking.DockPanel dockPanel1;
        private DevExpress.XtraBars.Docking.ControlContainer dockPanel1_Container;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraGrid.GridControl gcPhoneNumbers;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView2;
        private System.Windows.Forms.Label lblDateTo;
        private System.Windows.Forms.Label lblDateFrom;
        private System.Windows.Forms.Label lblBranch;
        private System.Windows.Forms.Label lblWarehouse;
        private System.Windows.Forms.Label lblPriceMax;
        private System.Windows.Forms.Label lblPriceMin;
        private DevExpress.XtraEditors.DateEdit deDateFrom;
        private DevExpress.XtraEditors.CheckedComboBoxEdit ccbeBranch;
        private DevExpress.XtraEditors.SearchLookUpEdit slueWarehouse;
        private DevExpress.XtraGrid.Views.Grid.GridView searchLookUpEdit1View;
        private DevExpress.XtraEditors.SpinEdit sePrinceMax;
        private DevExpress.XtraEditors.SpinEdit sePriceMin;
        private DevExpress.XtraEditors.DateEdit deDateTo;
        private DevExpress.XtraEditors.SimpleButton btnXem;
    }
}