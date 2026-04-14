namespace GUI.UI.modules
{
    partial class uc_CreateProgramAndHistory
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
            this.gcPrograms = new DevExpress.XtraGrid.GridControl();
            this.gvPrograms = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.gcHistory = new DevExpress.XtraGrid.GridControl();
            this.gvHistory = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).BeginInit();
            this.splitContainerControl1.Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).BeginInit();
            this.splitContainerControl1.Panel2.SuspendLayout();
            this.splitContainerControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gcPrograms)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvPrograms)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcHistory)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvHistory)).BeginInit();
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
            this.splitContainerControl1.Panel1.Controls.Add(this.gcPrograms);
            this.splitContainerControl1.Panel1.Text = "Panel1";
            // 
            // splitContainerControl1.Panel2
            // 
            this.splitContainerControl1.Panel2.Controls.Add(this.gcHistory);
            this.splitContainerControl1.Panel2.Text = "Panel2";
            this.splitContainerControl1.Size = new System.Drawing.Size(1371, 591);
            this.splitContainerControl1.SplitterPosition = 683;
            this.splitContainerControl1.TabIndex = 0;
            // 
            // gcPrograms
            // 
            this.gcPrograms.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcPrograms.Location = new System.Drawing.Point(0, 0);
            this.gcPrograms.MainView = this.gvPrograms;
            this.gcPrograms.Name = "gcPrograms";
            this.gcPrograms.Size = new System.Drawing.Size(683, 591);
            this.gcPrograms.TabIndex = 0;
            this.gcPrograms.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvPrograms});
            // 
            // gvPrograms
            // 
            this.gvPrograms.GridControl = this.gcPrograms;
            this.gvPrograms.Name = "gvPrograms";
            this.gvPrograms.OptionsView.ShowGroupPanel = false;
            // 
            // gcHistory
            // 
            this.gcHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcHistory.Location = new System.Drawing.Point(0, 0);
            this.gcHistory.MainView = this.gvHistory;
            this.gcHistory.Name = "gcHistory";
            this.gcHistory.Size = new System.Drawing.Size(676, 591);
            this.gcHistory.TabIndex = 0;
            this.gcHistory.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvHistory});
            // 
            // gvHistory
            // 
            this.gvHistory.GridControl = this.gcHistory;
            this.gvHistory.Name = "gvHistory";
            this.gvHistory.OptionsView.ShowGroupPanel = false;
            // 
            // uc_CreateProgramAndHistory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainerControl1);
            this.Controls.Add(this.panelToolbar);
            this.Name = "uc_CreateProgramAndHistory";
            this.Size = new System.Drawing.Size(1371, 631);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).EndInit();
            this.splitContainerControl1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).EndInit();
            this.splitContainerControl1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).EndInit();
            this.splitContainerControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gcPrograms)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvPrograms)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcHistory)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvHistory)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelToolbar;
        private DevExpress.XtraEditors.SplitContainerControl splitContainerControl1;
        private DevExpress.XtraGrid.GridControl gcPrograms;
        private DevExpress.XtraGrid.Views.Grid.GridView gvPrograms;
        private DevExpress.XtraGrid.GridControl gcHistory;
        private DevExpress.XtraGrid.Views.Grid.GridView gvHistory;
    }
}