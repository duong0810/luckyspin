using DevExpress.XtraEditors;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GUI.UI.modules
{
    public partial class frmWheelSetting : XtraForm
    {
        private bool _fadeEffectEnabled = true; // Mặc định bật fade effect

        // ⭐ THÊM METHOD NÀY
        /// <summary>
        /// Tắt hiệu ứng fade-in khi hiển thị form
        /// </summary>
        public void DisableFadeEffect()
        {
            _fadeEffectEnabled = false;
            this.Opacity = 1.0; // Đặt opacity = 100% ngay lập tức
        }

        private uc_Wheel _wheel;
        private const string WHEEL_STYLE_SETTINGS_FILE = "wheel_style_settings.json";

        public frmWheelSetting(uc_Wheel wheel)
        {
            if (wheel == null) throw new ArgumentNullException(nameof(wheel));
            _wheel = wheel;

            InitializeComponent();
            TryInitializeFromWheel();
            LoadStyleSettings();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TryInitializeFromWheel()
        {
            try
            {
                // Khởi tạo thời gian quay
                try
                {
                    int currentSeconds = _wheel.GetSpinDuration();
                    if (trackBarDuration != null)
                    {
                        if (currentSeconds < trackBarDuration.Properties.Minimum)
                            currentSeconds = (int)trackBarDuration.Properties.Minimum;
                        if (currentSeconds > trackBarDuration.Properties.Maximum)
                            currentSeconds = (int)trackBarDuration.Properties.Maximum;

                        trackBarDuration.Value = currentSeconds;
                        lblDurationValue.Text = currentSeconds.ToString() + " giây";
                    }
                }
                catch { /* ignore nếu API chưa có */ }
            }
            catch { /* non-critical */ }
        }

        private void LoadStyleSettings()
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, WHEEL_STYLE_SETTINGS_FILE);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    var settings = JsonConvert.DeserializeAnonymousType(json, new { SelectedStyleIndex = 0 });

                    if (settings != null && radioGroupStyle != null)
                    {
                        radioGroupStyle.SelectedIndex = settings.SelectedStyleIndex;
                    }
                }
                else
                {
                    // Mặc định chọn Tết Nguyên Đán
                    radioGroupStyle.SelectedIndex = 0;
                }
            }
            catch
            {
                radioGroupStyle.SelectedIndex = 0; // Fallback
            }
        }

        private void SaveStyleSettings(int styleIndex)
        {
            try
            {
                var settings = new { SelectedStyleIndex = styleIndex };
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                string path = Path.Combine(Application.StartupPath, WHEEL_STYLE_SETTINGS_FILE);
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Lỗi khi lưu style: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RadioGroupStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (radioGroupStyle == null || _wheel == null) return;

                int selectedIndex = radioGroupStyle.SelectedIndex;

                // Áp dụng style ngay lập tức (preview real-time)
                _wheel.SetWheelStyle(selectedIndex);

                // Hiển thị thông báo ngắn
                string styleName = GetStyleName(selectedIndex);
                // Không hiện MessageBox để UX mượt hơn, chỉ update trực tiếp
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Lỗi khi thay đổi style: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetStyleName(int index)
        {
            switch (index)
            {
                case 0: return "Mặc định"; 
                case 1: return "Tết Nguyên Đán";
                case 2: return "Giáng Sinh";
                case 3: return "Halloween";
                case 4: return "Trung Thu";
                case 5: return "Modern Neon";
                default: return "Unknown";
            }
        }

        private void BtnShuffle_Click(object sender, EventArgs e)
        {
            try
            {
                _wheel?.ShuffleSegments();
                XtraMessageBox.Show(this, "Đã tráo vị trí các ô thành công! 🎲", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Lỗi khi tráo vị trí: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
             
                int seconds = 5;
                try
                {
                    if (trackBarDuration != null)
                        seconds = trackBarDuration.Value;
                }
                catch { }

                if (seconds < 5) seconds = 5;
                if (seconds > 30) seconds = 30;

                _wheel?.SetSpinDuration(seconds);

                // Lưu style đã chọn
                SaveStyleSettings(radioGroupStyle.SelectedIndex);

                XtraMessageBox.Show(this,
                    $"✅ Đã lưu cấu hình vòng quay:\n\n" +
                    $"• Style: {GetStyleName(radioGroupStyle.SelectedIndex)}\n" +
                    $"• Thời gian quay: {seconds} giây",
                    "Lưu Thành Công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Lỗi khi lưu cấu hình: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TrackBarDuration_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (lblDurationValue != null && trackBarDuration != null)
                    lblDurationValue.Text = trackBarDuration.Value.ToString() + " giây";
            }
            catch { }
        }
    }
}