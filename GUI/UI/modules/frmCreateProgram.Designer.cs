using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace GUI.UI.modules
{
    partial class frmCreateProgram
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        private LabelControl lblEvent;
        private ComboBoxEdit cboEvents;
        private LabelControl lblProgramId;
        private TextEdit txtProgramId;
        private LabelControl lblProgramName;
        private TextEdit txtProgramName;
        private LabelControl lblSlogan;
        private MemoEdit txtSlogan;
        private LabelControl lblStart;
        private DateEdit dtpStart;
        private LabelControl lblEnd;
        private DateEdit dtpEnd;
        private LabelControl lblTorokuId;
        private TextEdit txtTorokuId;
        private SimpleButton btnSave;
        private SimpleButton btnCancel;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Initialize UI controls (DevExpress)
        /// </summary>
        private void InitializeComponent()
        {
            // đăng ký Load form là OK (Load handler không phụ thuộc vào controls)
            this.Load += new System.EventHandler(this.FrmCreateProgram_Load);

            this.components = new Container();

            this.lblEvent = new LabelControl();
            this.cboEvents = new ComboBoxEdit();

            this.lblProgramId = new LabelControl();
            this.txtProgramId = new TextEdit();

            this.lblProgramName = new LabelControl();
            this.txtProgramName = new TextEdit();

            this.lblSlogan = new LabelControl();
            this.txtSlogan = new MemoEdit();

            this.lblStart = new LabelControl();
            this.dtpStart = new DateEdit();

            this.lblEnd = new LabelControl();
            this.dtpEnd = new DateEdit();

            this.lblTorokuId = new LabelControl();
            this.txtTorokuId = new TextEdit();

            this.btnSave = new SimpleButton();
            this.btnCancel = new SimpleButton();

            ((ISupportInitialize)(this.cboEvents.Properties)).BeginInit();
            ((ISupportInitialize)(this.txtProgramId.Properties)).BeginInit();
            ((ISupportInitialize)(this.txtProgramName.Properties)).BeginInit();
            ((ISupportInitialize)(this.txtSlogan.Properties)).BeginInit();
            ((ISupportInitialize)(this.dtpStart.Properties.CalendarTimeProperties)).BeginInit();
            ((ISupportInitialize)(this.dtpStart.Properties)).BeginInit();
            ((ISupportInitialize)(this.dtpEnd.Properties.CalendarTimeProperties)).BeginInit();
            ((ISupportInitialize)(this.dtpEnd.Properties)).BeginInit();
            ((ISupportInitialize)(this.txtTorokuId.Properties)).BeginInit();

            // 
            // frmCreateProgram
            // 
            this.ClientSize = new Size(560, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Tạo / Chọn Chương Trình (Program)";

            // 
            // lblEvent
            // 
            this.lblEvent.Location = new Point(16, 16);
            this.lblEvent.Size = new Size(120, 20);
            this.lblEvent.Text = "Chọn Event (Sự kiện):";

            // 
            // cboEvents
            // 
            this.cboEvents.Location = new Point(140, 12);
            this.cboEvents.Size = new Size(380, 24);
            this.cboEvents.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            // Đăng ký sự kiện sau khi control đã được khởi tạo
            this.cboEvents.SelectedIndexChanged += new EventHandler(this.CboEvents_SelectedIndexChanged);

            // 
            // lblProgramId
            // 
            this.lblProgramId.Location = new Point(16, 56);
            this.lblProgramId.Size = new Size(120, 20);
            this.lblProgramId.Text = "Program ID:";

            // 
            // txtProgramId
            // 
            this.txtProgramId.Location = new Point(140, 52);
            this.txtProgramId.Size = new Size(380, 24);

            // 
            // lblProgramName
            // 
            this.lblProgramName.Location = new Point(16, 96);
            this.lblProgramName.Size = new Size(120, 20);
            this.lblProgramName.Text = "Tên lần quay:";

            // 
            // txtProgramName
            // 
            this.txtProgramName.Location = new Point(140, 92);
            this.txtProgramName.Size = new Size(380, 24);

            // 
            // lblSlogan
            // 
            this.lblSlogan.Location = new Point(16, 136);
            this.lblSlogan.Size = new Size(120, 20);
            this.lblSlogan.Text = "Slogan (banner):";

            // 
            // txtSlogan
            // 
            this.txtSlogan.Location = new Point(140, 132);
            this.txtSlogan.Size = new Size(380, 36);
            this.txtSlogan.Properties.ScrollBars = ScrollBars.Vertical;

            // 
            // lblStart
            // 
            this.lblStart.Location = new Point(16, 176);
            this.lblStart.Size = new Size(120, 20);
            this.lblStart.Text = "Ngày bắt đầu:";

            // 
            // dtpStart
            // 
            this.dtpStart.Location = new Point(140, 172);
            this.dtpStart.Size = new Size(200, 24);
            this.dtpStart.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.dtpStart.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.dtpStart.Properties.EditFormat.FormatString = "dd/MM/yyyy";
            this.dtpStart.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.dtpStart.Properties.Mask.EditMask = "dd/MM/yyyy";

            // 
            // lblEnd
            // 
            this.lblEnd.Location = new Point(16, 212);
            this.lblEnd.Size = new Size(120, 20);
            this.lblEnd.Text = "Ngày kết thúc:";

            // 
            // dtpEnd
            // 
            this.dtpEnd.Location = new Point(140, 208);
            this.dtpEnd.Size = new Size(200, 24);
            this.dtpEnd.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.dtpEnd.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.dtpEnd.Properties.EditFormat.FormatString = "dd/MM/yyyy";
            this.dtpEnd.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.dtpEnd.Properties.Mask.EditMask = "dd/MM/yyyy";

            // 
            // lblTorokuId
            // 
            this.lblTorokuId.Location = new Point(16, 248);
            this.lblTorokuId.Size = new Size(120, 20);
            this.lblTorokuId.Text = "Người tạo (TOROKU_ID):";

            // 
            // txtTorokuId
            // 
            this.txtTorokuId.Location = new Point(140, 244);
            this.txtTorokuId.Size = new Size(200, 24);
            this.txtTorokuId.Properties.ReadOnly = true;
            this.txtTorokuId.Properties.Appearance.BackColor = SystemColors.ControlLight;
            this.txtTorokuId.Properties.Appearance.Options.UseBackColor = true;

            // 
            // btnSave
            // 
            this.btnSave.Location = new Point(340, 240);
            this.btnSave.Size = new Size(90, 28);
            this.btnSave.Text = "Lưu";
            this.btnSave.Click += new EventHandler(this.BtnSave_Click);

            // 
            // btnCancel
            // 
            this.btnCancel.Location = new Point(440, 240);
            this.btnCancel.Size = new Size(80, 28);
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Click += new EventHandler(this.BtnCancel_Click);

            // Add controls
            this.Controls.AddRange(new Control[] {
                this.lblEvent, this.cboEvents,
                this.lblProgramId, this.txtProgramId,
                this.lblProgramName, this.txtProgramName,
                this.lblSlogan, this.txtSlogan,
                this.lblStart, this.dtpStart,
                this.lblEnd, this.dtpEnd,
                this.lblTorokuId, this.txtTorokuId,
                this.btnSave, this.btnCancel
            });

            ((ISupportInitialize)(this.cboEvents.Properties)).EndInit();
            ((ISupportInitialize)(this.txtProgramId.Properties)).EndInit();
            ((ISupportInitialize)(this.txtProgramName.Properties)).EndInit();
            ((ISupportInitialize)(this.txtSlogan.Properties)).EndInit();
            ((ISupportInitialize)(this.dtpStart.Properties.CalendarTimeProperties)).EndInit();
            ((ISupportInitialize)(this.dtpStart.Properties)).EndInit();
            ((ISupportInitialize)(this.dtpEnd.Properties.CalendarTimeProperties)).EndInit();
            ((ISupportInitialize)(this.dtpEnd.Properties)).EndInit();
            ((ISupportInitialize)(this.txtTorokuId.Properties)).EndInit();
        }

        #endregion
    }
}