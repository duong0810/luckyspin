using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GUI.DTO.models;
using GUI.Service;
using GUI.DTO;

namespace GUI.UI.modules
{
    public partial class frmCreateProgram : Form
    {
        private List<LuckyDrawDTO> _events = new List<LuckyDrawDTO>();
        private readonly string _currentUserId;

        public string CreatedProgramId { get; private set; }
        public string CreatedProgramName { get; private set; }

        public string CreatedBannerHeader1 { get; private set; }
        public string CreatedBannerHeader2 { get; private set; }
        public string CreatedBannerHeader3 { get; private set; }

        // public property expose số ô được chọn (để caller lấy)
        public int CreatedNumSegments { get; private set; } = 8;

        public frmCreateProgram(string currentUserId)
        {
            _currentUserId = currentUserId ?? "admin";
            InitializeComponent();
            txtTorokuId.Text = _currentUserId;

            // Khóa hai trường ngày mặc định (sẽ gán lại khi chọn event)
            try
            {
                dtpStart.Properties.ReadOnly = true;
                dtpEnd.Properties.ReadOnly = true;
            }
            catch
            {
                // nếu controls không phải DevExpress hoặc null thì bỏ qua
            }

            // Không auto gợi tên chương trình
            txtProgramName.Text = string.Empty;
        }

        private async void FrmCreateProgram_Load(object sender, EventArgs e)
        {
            await LoadEventsAsync();
        }

        private async Task LoadEventsAsync()
        {
            try
            {
                cboEvents.Properties.Items.Clear();
                cboEvents.Properties.Items.Add("Đang tải...");

                var events = await LuckyDrawService.GetAllEventsAsync();
                cboEvents.Properties.Items.Clear();

                if (events == null || events.Count == 0)
                {
                    cboEvents.Properties.Items.Add("Không có sự kiện");
                    cboEvents.SelectedIndex = 0;
                    _events = new List<LuckyDrawDTO>();
                    return;
                }

                _events = events;

                foreach (var ev in _events)
                {
                    string text = $"{ev.LUCKY_DRAW_ID} - {ev.LUCKY_DRAW_NAME ?? ev.LUCKY_DRAW_TITLE ?? ""}";
                    cboEvents.Properties.Items.Add(text);
                }

                if (cboEvents.Properties.Items.Count > 0)
                    cboEvents.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách sự kiện: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CboEvents_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                int idx = cboEvents.SelectedIndex;
                if (idx < 0 || idx >= _events.Count)
                    return;

                var selected = _events[idx];
                if (selected == null)
                    return;

                // Không tự điền tên chương trình theo yêu cầu
                // txtProgramName.Text = string.Empty;

                // --- LẤY SỐ Ô (LUCKY_DRAW_SLOT_NUM) TỪ EVENT ---
                int slotNum = 8; // fallback
                try
                {
                    // Thử lấy trực tiếp từ DTO (nếu DTO có property)
                    var slotProp = selected.GetType().GetProperty("LUCKY_DRAW_SLOT_NUM");
                    if (slotProp != null)
                    {
                        var val = slotProp.GetValue(selected);
                        if (val != null)
                        {
                            slotNum = Math.Max(1, Convert.ToInt32(val));
                        }
                    }
                    else
                    {
                        // nếu DTO không chứa, gọi API lấy event chi tiết
                        var eventDetail = await LuckyDrawService.GetEventByIdAsync(selected.LUCKY_DRAW_ID);
                        if (eventDetail != null)
                        {
                            var prop = eventDetail.GetType().GetProperty("LUCKY_DRAW_SLOT_NUM");
                            if (prop != null)
                            {
                                var val = prop.GetValue(eventDetail);
                                if (val != null) slotNum = Math.Max(1, Convert.ToInt32(val));
                            }
                        }
                    }
                }
                catch
                {
                    slotNum = 8;
                }

                // Set public property để caller (frmMain) lấy
                this.CreatedNumSegments = slotNum;

                // Lấy danh sách meisai để xác định ngày start/end (như trước)
                try
                {
                    var meisaiList = await LuckyDrawService.GetAllMeisaiAsync(selected.LUCKY_DRAW_ID);
                    if (meisaiList != null && meisaiList.Count > 0)
                    {
                        var kaDates = meisaiList.Select(m => m.KAISHI_DATE).Where(d => d.HasValue).Select(d => d.Value.Date).ToList();
                        var syDates = meisaiList.Select(m => m.SYURYOU_DATE).Where(d => d.HasValue).Select(d => d.Value.Date).ToList();

                        DateTime startDate = kaDates.Count > 0 ? kaDates.Min() : DateTime.Now.Date;
                        DateTime endDate = syDates.Count > 0 ? syDates.Max() : DateTime.Now.Date;

                        dtpStart.DateTime = startDate;
                        dtpEnd.DateTime = endDate;

                        dtpStart.Properties.ReadOnly = true;
                        dtpEnd.Properties.ReadOnly = true;
                        return;
                    }
                }
                catch { /* ignore */ }

                // fallback lấy từ event DTO nếu có ngày
                try
                {
                    var kaProp = selected.GetType().GetProperty("KAISHI_DATE");
                    var syProp = selected.GetType().GetProperty("SYURYOU_DATE");

                    DateTime start = DateTime.Now.Date;
                    DateTime end = DateTime.Now.Date;

                    if (kaProp != null)
                    {
                        var kaVal = kaProp.GetValue(selected);
                        if (kaVal != null) start = Convert.ToDateTime(kaVal).Date;
                    }

                    if (syProp != null)
                    {
                        var syVal = syProp.GetValue(selected);
                        if (syVal != null) end = Convert.ToDateTime(syVal).Date;
                    }

                    dtpStart.DateTime = start;
                    dtpEnd.DateTime = end;
                    dtpStart.Properties.ReadOnly = true;
                    dtpEnd.Properties.ReadOnly = true;
                }
                catch
                {
                    dtpStart.DateTime = DateTime.Now.Date;
                    dtpEnd.DateTime = DateTime.Now.Date;
                    dtpStart.Properties.ReadOnly = true;
                    dtpEnd.Properties.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi chọn sự kiện: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            int idx = cboEvents.SelectedIndex;
            if (idx < 0 || idx >= _events.Count)
            {
                MessageBox.Show("Vui lòng chọn một sự kiện.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selEvent = _events[idx];

            string programId = txtProgramId.Text?.Trim();

            // Nếu người dùng không nhập Program ID, tự sinh để tránh trùng
            if (string.IsNullOrWhiteSpace(programId))
            {
                programId = GenerateProgramId(selEvent.LUCKY_DRAW_ID);
                txtProgramId.Text = programId; // hiển thị cho user biết
            }

            if (programId.Length > 50)
            {
                MessageBox.Show("Program ID không được vượt quá 50 ký tự.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtProgramId.Focus();
                return;
            }

            var dto = new LuckyDrawProgrameDTO
            {
                LUCKY_DRAW_PROGRAME_ID = programId,
                LUCKY_DRAW_PROGRAME_NAME = string.IsNullOrWhiteSpace(txtProgramName.Text) ? null : txtProgramName.Text.Trim(),
                LUCKY_DRAW_ID = selEvent.LUCKY_DRAW_ID,
                KAISHI_DATE = dtpStart.DateTime.Date,
                SYURYOU_DATE = dtpEnd.DateTime.Date,
                PROGRAME_SLOGAN = string.IsNullOrWhiteSpace(txtSlogan.Text) ? null : txtSlogan.Text.Trim(),
                TOROKU_ID = _currentUserId
            };

            try
            {
                btnSave.Enabled = false;
                btnCancel.Enabled = false;
                Cursor = Cursors.WaitCursor;

                // Gọi API tạo chương trình
                var (success, message) = await LuckyDrawService.CreateProgramAsync(dto);

                if (!success)
                {
                    MessageBox.Show("Không thể tạo chương trình: " + message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Xác nhận lại từ server: lấy chương trình vừa tạo
                var created = await LuckyDrawService.GetProgramByIdAsync(programId);
                if (created == null)
                {
                    MessageBox.Show($"Tạo chương trình thành công nhưng không thể xác nhận chương trình: {programId}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Gán ID + Name + slotnum cho caller; nếu server không trả tên, dùng programId
                this.CreatedProgramId = created?.LUCKY_DRAW_PROGRAME_ID ?? dto.LUCKY_DRAW_PROGRAME_ID;
                this.CreatedProgramName = !string.IsNullOrWhiteSpace(created?.LUCKY_DRAW_PROGRAME_NAME) ? created.LUCKY_DRAW_PROGRAME_NAME : this.CreatedProgramId;

                var sloganText = (!string.IsNullOrWhiteSpace(created?.PROGRAME_SLOGAN) ? created.PROGRAME_SLOGAN : dto.PROGRAME_SLOGAN) ?? string.Empty;
                var lines = sloganText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(l => l.Trim())
                                      .Where(l => !string.IsNullOrWhiteSpace(l))
                                      .ToArray();

                if (lines.Length >= 3)
                {
                    // Nếu người dùng nhập 3 dòng trong slogan => dùng 3 dòng đó làm banner
                    this.CreatedBannerHeader1 = lines[0];
                    this.CreatedBannerHeader2 = lines[1];
                    this.CreatedBannerHeader3 = lines[2];
                }
                else
                {
                    // LƯU Ý: Không lấy tự động LUCKY_DRAW_TITLE nữa để tránh hiển thị "sự kiện mùa xuân 2026"
                    // Thay vào đó chỉ dùng các dòng người dùng nhập (nếu có), còn không để trống
                    this.CreatedBannerHeader1 = lines.Length > 0 ? lines[0] : string.Empty;
                    this.CreatedBannerHeader2 = lines.Length > 1 ? lines[1] : string.Empty;
                    this.CreatedBannerHeader3 = string.Empty;
                }

                // Nếu server trả slot num trong program dto (hoặc chúng ta có Earlier SelectedNum), ưu tiên giá trị lấy từ event
                // CreatedNumSegments đã được set khi chọn event; giữ nguyên
                MessageBox.Show("Tạo chương trình thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tạo chương trình: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private string GenerateProgramId(string eventId)
        {
            string id = $"{eventId}_{DateTime.Now:yyyyMMddHHmmss}";
            if (id.Length > 50) id = id.Substring(0, 50);
            return id;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}