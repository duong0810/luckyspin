using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GUI.UI.modules
{
    public class ImageViewerForm : Form
    {
        private PictureBox _pictureBox;
        private Button _btnSave;
        private Button _btnClose;
        private TableLayoutPanel _layout;

        public ImageViewerForm(Image image)
        {
            InitializeComponent();
            if (image != null)
            {
                // assign a clone to avoid disposing caller's instance
                _pictureBox.Image = new Bitmap(image);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Image Preview";
            this.Width = 800;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterParent;

            _layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            _pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(6)
            };

            _btnSave = new Button { Text = "Lưu", AutoSize = true };
            _btnClose = new Button { Text = "Đóng", AutoSize = true };

            _btnSave.Click += BtnSave_Click;
            _btnClose.Click += (s, e) => this.Close();

            panelButtons.Controls.Add(_btnClose);
            panelButtons.Controls.Add(_btnSave);

            _layout.Controls.Add(_pictureBox, 0, 0);
            _layout.Controls.Add(panelButtons, 0, 1);

            this.Controls.Add(_layout);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_pictureBox.Image == null)
            {
                MessageBox.Show("Không có ảnh để lưu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "JPEG Image|*.jpg|PNG Image|*.png|Bitmap|*.bmp";
                sfd.FileName = "image";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var ext = Path.GetExtension(sfd.FileName).ToLower();
                        var format = System.Drawing.Imaging.ImageFormat.Jpeg;
                        if (ext == ".png") format = System.Drawing.Imaging.ImageFormat.Png;
                        else if (ext == ".bmp") format = System.Drawing.Imaging.ImageFormat.Bmp;

                        _pictureBox.Image.Save(sfd.FileName, format);
                        MessageBox.Show("Lưu ảnh thành công.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi lưu ảnh: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                _pictureBox.Image?.Dispose();
            }
            catch { }
            base.OnFormClosed(e);
        }
    }
}