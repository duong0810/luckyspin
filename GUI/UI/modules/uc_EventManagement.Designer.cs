namespace GUI.UI.modules
{
    partial class uc_EventManagement
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

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.panelToolbar = new System.Windows.Forms.Panel();
            this.splitContainerControl1 = new DevExpress.XtraEditors.SplitContainerControl();
            this.gcEvents = new DevExpress.XtraGrid.GridControl();
            this.gvEvents = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.gcMeisai = new DevExpress.XtraGrid.GridControl();
            this.gvMeisai = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).BeginInit();
            this.splitContainerControl1.Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).BeginInit();
            this.splitContainerControl1.Panel2.SuspendLayout();
            this.splitContainerControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gcEvents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvEvents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcMeisai)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvMeisai)).BeginInit();
            this.SuspendLayout();
            // 
            // panelToolbar
            // 
            this.panelToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelToolbar.Location = new System.Drawing.Point(0, 0);
            this.panelToolbar.Name = "panelToolbar";
            this.panelToolbar.Size = new System.Drawing.Size(1371, 40);
            this.panelToolbar.TabIndex = 1;
            // 
            // splitContainerControl1
            // 
            this.splitContainerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerControl1.Location = new System.Drawing.Point(0, 40);
            this.splitContainerControl1.Name = "splitContainerControl1";
            // 
            // splitContainerControl1.Panel1
            // 
            this.splitContainerControl1.Panel1.Controls.Add(this.gcEvents);
            this.splitContainerControl1.Panel1.Text = "Panel1";
            // 
            // splitContainerControl1.Panel2
            // 
            this.splitContainerControl1.Panel2.Controls.Add(this.gcMeisai);
            this.splitContainerControl1.Panel2.Text = "Panel2";
            this.splitContainerControl1.Size = new System.Drawing.Size(1371, 591);
            this.splitContainerControl1.SplitterPosition = 691;
            this.splitContainerControl1.TabIndex = 0;
            // 
            // gcEvents
            // 
            this.gcEvents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcEvents.Location = new System.Drawing.Point(0, 0);
            this.gcEvents.MainView = this.gvEvents;
            this.gcEvents.Name = "gcEvents";
            this.gcEvents.Size = new System.Drawing.Size(691, 591);
            this.gcEvents.TabIndex = 0;
            this.gcEvents.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvEvents});
            // 
            // gvEvents
            // 
            this.gvEvents.GridControl = this.gcEvents;
            this.gvEvents.Name = "gvEvents";
            this.gvEvents.OptionsBehavior.Editable = false;
            this.gvEvents.OptionsEditForm.PopupEditFormWidth = 914;
            this.gvEvents.OptionsView.ShowGroupPanel = false;
            // 
            // gcMeisai
            // 
            this.gcMeisai.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcMeisai.Location = new System.Drawing.Point(0, 0);
            this.gcMeisai.MainView = this.gvMeisai;
            this.gcMeisai.Name = "gcMeisai";
            this.gcMeisai.Size = new System.Drawing.Size(668, 591);
            this.gcMeisai.TabIndex = 0;
            this.gcMeisai.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvMeisai});
            // 
            // gvMeisai
            // 
            this.gvMeisai.GridControl = this.gcMeisai;
            this.gvMeisai.Name = "gvMeisai";
            this.gvMeisai.OptionsBehavior.Editable = false;
            this.gvMeisai.OptionsEditForm.PopupEditFormWidth = 914;
            this.gvMeisai.OptionsView.ShowGroupPanel = false;
            // 
            // uc_EventManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainerControl1);
            this.Controls.Add(this.panelToolbar);
            this.Name = "uc_EventManagement";
            this.Size = new System.Drawing.Size(1371, 631);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).EndInit();
            this.splitContainerControl1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).EndInit();
            this.splitContainerControl1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).EndInit();
            this.splitContainerControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gcEvents)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvEvents)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcMeisai)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvMeisai)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelToolbar; // ✅ THÊM
        private DevExpress.XtraEditors.SplitContainerControl splitContainerControl1;
        private DevExpress.XtraGrid.GridControl gcEvents;
        private DevExpress.XtraGrid.Views.Grid.GridView gvEvents;
        private DevExpress.XtraGrid.GridControl gcMeisai;
        private DevExpress.XtraGrid.Views.Grid.GridView gvMeisai;
    }
}