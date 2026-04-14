using DevExpress.XtraEditors;
using GUI.UI.modules;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace GUI
{
    public partial class frmMain : XtraForm
    {
        uc_Wheel ucWheel;
        uc_EventManagement ucEventManagement;
        uc_CreateProgramAndHistory ucCreateProgramAndHistory;

        private const string FixedMarqueeText = "Vòng quay may mắn - Tham gia ngay để nhận quà!";
        public frmMain()
        {
            InitializeComponent();
            this.BackColor = Color.White;
        }



        private void mnThoat_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private async void mnVongquay_Click(object sender, EventArgs e)
        {
            try
            {
                if (ucWheel == null)
                {
                    ucWheel = new uc_Wheel();
                    ucWheel.Dock = DockStyle.Fill;
                    contentPanel.Controls.Add(ucWheel);
                }

                try
                {
                    ucWheel.SetMarqueeText(FixedMarqueeText);
                }
                catch { }

                try
                {
                    if (string.IsNullOrWhiteSpace(ucWheel.LoadedEventId))
                    {
                        ucWheel.UpdateNumSegments(8);
                    }
                }
                catch { }

                ucWheel.BringToFront();
                lblTieuDeLabel.Text = "Vòng quay may mắn";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi mở vòng quay: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void mnLuckyDraw_Click(object sender, EventArgs e)
        {
            if (ucEventManagement == null)
            {
                ucEventManagement = new uc_EventManagement();
                ucEventManagement.Dock = DockStyle.Fill;
                contentPanel.Controls.Add(ucEventManagement);
            }

            ucEventManagement.BringToFront();
            lblTieuDeLabel.Text = "Quản lý sự kiện & giải thưởng";
        }

        private void mnProgram_Click(object sender, EventArgs e)
        {
            if (ucCreateProgramAndHistory == null)
            {
                ucCreateProgramAndHistory = new uc_CreateProgramAndHistory();
                ucCreateProgramAndHistory.Dock = DockStyle.Fill;
                contentPanel.Controls.Add(ucCreateProgramAndHistory);
            }

            ucCreateProgramAndHistory.BringToFront();
            lblTieuDeLabel.Text = "Quản lý chương trình & lịch sử";
        }

        private void accordionControlElement1_Click(object sender, EventArgs e)
        {

        }

        private void mnGiaoDien_Click(object sender, EventArgs e)
        {
            try
            {
                // Nếu ucWheel chưa tồn tại thì khởi tạo và thêm vào contentPanel (giống mnVongquay_Click)
                if (ucWheel == null)
                {
                    ucWheel = new uc_Wheel();
                    ucWheel.Dock = DockStyle.Fill;
                    contentPanel.Controls.Add(ucWheel);
                }

                // Đặt marquee mặc định (an toàn nếu method không tồn tại)
                try { ucWheel.SetMarqueeText(FixedMarqueeText); } catch { }

                // Nếu chưa load event, set số ô mặc định
                try
                {
                    if (string.IsNullOrWhiteSpace(ucWheel.LoadedEventId))
                    {
                        ucWheel.UpdateNumSegments(8);
                    }
                }
                catch { }

                // Đưa ucWheel lên trước để người dùng thấy bánh xe khi chỉnh
                try { ucWheel.BringToFront(); } catch { }

                // Mở form cấu hình vòng quay (frmWheelSetting) và truyền tham chiếu uc_Wheel
                using (var frm = new frmWheelSetting(ucWheel))
                {
                    frm.StartPosition = FormStartPosition.CenterParent;
                    frm.ShowDialog(this);
                }

                // Cập nhật tiêu đề
                lblTieuDeLabel.Text = "Chỉnh giao diện vòng quay";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi mở giao diện: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}