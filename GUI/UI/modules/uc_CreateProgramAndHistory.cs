using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraEditors.Controls;
using GUI.DTO;
using GUI.Interfaces;
using GUI.Managers;
using GUI.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace GUI.UI.modules
{
    public partial class uc_CreateProgramAndHistory : UserControl
    {
        // Khóa hàng đang được phép chỉnh sửa (chỉ hàng này được mở editor)
        private int? _editingRowHandleProgram = null;
        // Khóa hàng cho bảng History (tương tự Program)
        private int? _editingRowHandleHistory = null;

        // Repository Item dùng cho lookup sự kiện
        private RepositoryItemLookUpEdit _repoEventLookup;
        private List<LuckyDrawDTO> _eventLookupSource = new List<LuckyDrawDTO>();

        // Crud managers cho Program và History
        private CrudManager<LuckyDrawProgrameDTO> _programCrudManager;
        private CrudManager<LuckyDrawHistoryDTO> _historyCrudManager;

        // Toolbar dùng chung
        private uc_CrudToolbar _sharedToolbar;

        // Theo dõi grid đang active
        private enum ActiveGridType { Program, History }
        private ActiveGridType _activeGrid = ActiveGridType.Program;

        private readonly HashSet<string> _historyImageFetchInProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public uc_CreateProgramAndHistory()
        {
            InitializeComponent();
            this.Load += Uc_ProgramAndHistory_Load;
        }

        private async void Uc_ProgramAndHistory_Load(object sender, EventArgs e)
        {
            SetupToolbar();
            SetupProgramGrid();
            SetupHistoryGrid();

            // Load dữ liệu lookup sự kiện trước
            await LoadEventLookupAsync();

            SetupCrudManagers();

            // Load danh sách chương trình ban đầu
            await _programCrudManager.LoadDataAsync();

            // Load toàn bộ lịch sử một lần (hiển thị luôn toàn bộ lịch sử ở gvHistory)
            // (Trước đây history chỉ load theo program được chọn)
            try
            {
                await _historyCrudManager.LoadDataAsync();
            }
            catch 
            {
            }
        }

        private async Task LoadEventLookupAsync()
        {
            try
            {
                // Load events (service has no includeImages parameter in your build)
                _eventLookupSource = await LuckyDrawService.GetAllEventsAsync();
                if (_repoEventLookup != null)
                {
                    // Gán trực tiếp List<DTO> để DisplayMember/ValueMember hoạt động
                    _repoEventLookup.DataSource = _eventLookupSource;
                    _repoEventLookup.DisplayMember = "LUCKY_DRAW_ID";
                    _repoEventLookup.ValueMember = "LUCKY_DRAW_ID";

                    _repoEventLookup.PopulateColumns();
                    foreach (LookUpColumnInfo c in _repoEventLookup.Columns)
                    {
                        c.Visible = c.FieldName == "LUCKY_DRAW_ID" || c.FieldName == "LUCKY_DRAW_NAME";
                    }

                    try { gvPrograms.RefreshData(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                }
            }
            catch 
            {
            }
        }

        // Cache ảnh preview cho history
        private readonly Dictionary<string, Image> _historyImageCache = new Dictionary<string, Image>();

        private void ClearHistoryImageCache()
        {
            try
            {
                foreach (var img in _historyImageCache.Values) img?.Dispose();
                _historyImageCache.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
            }
        }

        private Image Base64ToImage(string base64String)
        {
            if (string.IsNullOrEmpty(base64String)) return null;
            try
            {
                // Bỏ prefix nếu có data URI
                if (base64String.Contains(",")) base64String = base64String.Split(',')[1];

                // Loại bỏ whitespace/newline có thể phá Convert.FromBase64String
                base64String = base64String.Trim()
                                           .Replace("\r", string.Empty)
                                           .Replace("\n", string.Empty)
                                           .Replace(" ", string.Empty);


                byte[] bytes = Convert.FromBase64String(base64String);
                using (var ms = new MemoryStream(bytes))
                {
                    var img = Image.FromStream(ms);
                    return new Bitmap(img);
                }
            }
            catch 
            {
                return null;
            }
        }

        private Image ResizeImage(Image image, int width, int height)
        {
            if (image == null) return null;
            var dest = new Bitmap(width, height);
            dest.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var g = Graphics.FromImage(dest))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                using (var attr = new ImageAttributes())
                {
                    attr.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(image, new Rectangle(0, 0, width, height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attr);
                }
            }
            return dest;
        }

        private Image GetDefaultImage(int width = 25, int height = 25)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.LightGray);
                using (var f = new Font("Arial", 8))
                using (var b = new SolidBrush(Color.DarkGray))
                {
                    var text = "No Image";
                    var sz = g.MeasureString(text, f);
                    g.DrawString(text, f, b, (width - sz.Width) / 2f, (height - sz.Height) / 2f);
                }
            }
            return bmp;
        }



        private void SetupToolbar()
        {
            _sharedToolbar = new uc_CrudToolbar();
            _sharedToolbar.Dock = DockStyle.Fill;
            panelToolbar.Controls.Add(_sharedToolbar);

            // Đăng ký sự kiện toolbar
            _sharedToolbar.AddClicked += SharedToolbar_AddClicked;
            _sharedToolbar.EditClicked += SharedToolbar_EditClicked;
            _sharedToolbar.DeleteClicked += SharedToolbar_DeleteClicked;
            _sharedToolbar.SaveClicked += SharedToolbar_SaveClicked;
            _sharedToolbar.CancelClicked += SharedToolbar_CancleClicked;
            _sharedToolbar.RefreshClicked += SharedToolbar_RefreshClicked;

            // Track grid đang active
            gcPrograms.Click += (s, e) => _activeGrid = ActiveGridType.Program;
            gcHistory.Click += (s, e) => _activeGrid = ActiveGridType.History;
            gvPrograms.Click += (s, e) => _activeGrid = ActiveGridType.Program;
            gvHistory.Click += (s, e) => _activeGrid = ActiveGridType.History;
        }

        //// Handler nhỏ để tránh trùng tên event do Designer
        //private void _active_grid_from_history_click(object s, EventArgs e) => _active_grid_from_history_click_impl();
        //private void _active_grid_from_history_click_impl() { _active_grid_from_history_click_impl2(); }
        //private void _active_grid_from_history_click_impl2() { /* intentionally empty; kept for parity with other modules */ }

        private void GvPrograms_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (!e.IsGetData) return;

            var view = sender as GridView;
            if (view == null)
            {
                e.Value = null;
                return;
            }

            // STT (unbound) — luôn trả như cũ
            if (e.Column.FieldName == "STT")
            {
                e.Value = e.ListSourceRowIndex + 1;
                return;
            }

            // Optional: nếu bạn thêm cột unbound "LUCKY_DRAW_NAME", hiển thị tên sự kiện dựa trên LUCKY_DRAW_ID
            if (string.Equals(e.Column.FieldName, "LUCKY_DRAW_NAME", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Lấy LUCKY_DRAW_ID từ DTO hoặc từ cell (hỗ trợ cả BindingList và DataRow)
                    string eventId = null;
                    var rowObj = view.GetRow(e.ListSourceRowIndex);
                    if (rowObj is LuckyDrawProgrameDTO progDto)
                    {
                        eventId = progDto.LUCKY_DRAW_ID;
                    }
                    else
                    {
                        var cell = view.GetListSourceRowCellValue(e.ListSourceRowIndex, "LUCKY_DRAW_ID");
                        eventId = cell?.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(eventId))
                    {
                        e.Value = string.Empty;
                        return;
                    }

                    // Tra lookup đã load trước (_eventLookupSource)
                    var evt = _eventLookupSource?.FirstOrDefault(x => string.Equals(x.LUCKY_DRAW_ID, eventId, StringComparison.OrdinalIgnoreCase));
                    if (evt != null)
                    {
                        e.Value = !string.IsNullOrWhiteSpace(evt.LUCKY_DRAW_NAME) ? evt.LUCKY_DRAW_NAME : evt.LUCKY_DRAW_ID;
                    }
                    else
                    {
                        // fallback: hiển thị chính eventId nếu không tìm thấy object
                        e.Value = eventId;
                    }
                }
                catch 
                {
                    e.Value = string.Empty;
                }

                return;
            }
        }

        private void SetupProgramGrid()
        {
            gvPrograms.Columns.Clear();
            gvPrograms.OptionsView.ShowColumnHeaders = true;
            gvPrograms.OptionsView.ShowAutoFilterRow = true;
            gvPrograms.OptionsBehavior.Editable = true;
            gvPrograms.OptionsView.ShowGroupPanel = false;
            gvPrograms.OptionsView.ColumnAutoWidth = false;
            gvPrograms.RowHeight = 22;

            //// STT (unbound)
            //var colStt = new DevExpress.XtraGrid.Columns.GridColumn
            //{
            //    Caption = "STT",
            //    FieldName = "STT",
            //    Visible = true,
            //    UnboundType = DevExpress.Data.UnboundColumnType.Integer,
            //    Width = 50,
            //    OptionsColumn = { AllowEdit = false, AllowSort = DevExpress.Utils.DefaultBoolean.False } // không sắp xếp trên STT
            //};
            //gvPrograms.Columns.Add(colStt);

            // Mã chương trình
            var colProgId = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "LUCKY_DRAW_PROGRAME_ID",
                Caption = "Mã chương trình",
                Visible = true,
                Width = 160,
                OptionsColumn = { AllowEdit = true }
            };
            gvPrograms.Columns.Add(colProgId);

            // Tên lần quay
            var colProgName = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "LUCKY_DRAW_PROGRAME_NAME",
                Caption = "Tên lần quay",
                Visible = true,
                Width = 240,
                OptionsColumn = { AllowEdit = true }
            };
            gvPrograms.Columns.Add(colProgName);

            // Mã sự kiện (LookUp)
            var colEventId = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "LUCKY_DRAW_ID",
                Caption = "Mã sự kiện",
                Visible = true,
                Width = 180,
                OptionsColumn = { AllowEdit = true }
            };

            _repoEventLookup = new RepositoryItemLookUpEdit
            {
                NullText = "",
                ValueMember = "LUCKY_DRAW_ID",
                // Display the event ID in the cell (user requested)
                DisplayMember = "LUCKY_DRAW_ID",
                TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor,
                ShowHeader = true,
                PopupWidth = 350
            };
            _repoEventLookup.Columns.Clear();
            _repoEventLookup.Columns.Add(new LookUpColumnInfo("LUCKY_DRAW_ID", "Mã"));
            _repoEventLookup.Columns.Add(new LookUpColumnInfo("LUCKY_DRAW_NAME", "Tên sự kiện"));

            gcPrograms.RepositoryItems.Add(_repoEventLookup);
            colEventId.ColumnEdit = _repoEventLookup;
            gvPrograms.Columns.Add(colEventId);

            // Ngày bắt đầu / kết thúc
            var colKaishi = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KAISHI_DATE",
                Caption = "Ngày bắt đầu",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = true }
            };
            colKaishi.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colKaishi.DisplayFormat.FormatString = "dd/MM/yyyy";
            gvPrograms.Columns.Add(colKaishi);

            var colSyuryou = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "SYURYOU_DATE",
                Caption = "Ngày kết thúc",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = true }
            };
            colSyuryou.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colSyuryou.DisplayFormat.FormatString = "dd/MM/yyyy";
            gvPrograms.Columns.Add(colSyuryou);

            // Slogan
            var colSlogan = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "PROGRAME_SLOGAN",
                Caption = "Slogan",
                Visible = true,
                Width = 220,
                OptionsColumn = { AllowEdit = true }
            };
            gvPrograms.Columns.Add(colSlogan);

            // Ngày tạo
            var colTorokuDate = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "TOROKU_DATE",
                Caption = "Ngày tạo",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colTorokuDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colTorokuDate.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            // sort mặc định: mới -> cũ
            colTorokuDate.SortOrder = DevExpress.Data.ColumnSortOrder.Descending;
            gvPrograms.Columns.Add(colTorokuDate);

            var colTorokuId = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "TOROKU_ID",
                Caption = "Người tạo",
                Visible = true,
                Width = 100,
                OptionsColumn = { AllowEdit = false }
            };
            gvPrograms.Columns.Add(colTorokuId);

            // Thông tin audit
            var colKosinId = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOSIN_ID",
                Caption = "Người sửa",
                Visible = true,
                Width = 100,
                OptionsColumn = { AllowEdit = false }
            };
            gvPrograms.Columns.Add(colKosinId);

            var colKosinDate = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOSIN_DATE",
                Caption = "Ngày sửa",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colKosinDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colKosinDate.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            gvPrograms.Columns.Add(colKosinDate);

            var colKosinNaiyou = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOSIN_NAIYOU",
                Caption = "Nội dung sửa",
                Visible = false,
                Width = 250,
                OptionsColumn = { AllowEdit = false }
            };
            gvPrograms.Columns.Add(colKosinNaiyou);

            // Đăng ký sự kiện unbound/STT
            gvPrograms.CustomUnboundColumnData -= GvPrograms_CustomUnboundColumnData;
            gvPrograms.CustomUnboundColumnData += GvPrograms_CustomUnboundColumnData;

            // Chặn mở editor trên các hàng không được khoá
            gvPrograms.ShowingEditor -= GvPrograms_ShowingEditor;
            gvPrograms.ShowingEditor += GvPrograms_ShowingEditor;

            gvPrograms.BestFitColumns();

            gvPrograms.RowStyle -= GvPrograms_RowStyle;
            gvPrograms.RowStyle += GvPrograms_RowStyle;

            gvPrograms.FocusedRowChanged -= GvPrograms_FocusedRowChanged;
            gvPrograms.FocusedRowChanged += GvPrograms_FocusedRowChanged;
            gvPrograms.DoubleClick -= GvPrograms_DoubleClick;
            gvPrograms.DoubleClick += GvPrograms_DoubleClick;


            gvPrograms.RowCellStyle -= GvPrograms_RowCellStyle;
            gvPrograms.RowCellStyle += GvPrograms_RowCellStyle;
        }

        private void GvPrograms_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            try
            {
                if (e.RowHandle < 0) return;
                var row = gvPrograms.GetRow(e.RowHandle) as LuckyDrawProgrameDTO;
                if (row == null) return;

                // Nếu SYURYOU_DATE đã tới hoặc đã qua -> mờ/ẩn giống MUKO style
                if (row.SYURYOU_DATE.HasValue)
                {
                    try
                    {
                        if (row.SYURYOU_DATE.Value.Date < DateTime.Now.Date)
                        {
                            e.Appearance.ForeColor = Color.Gray;
                            e.Appearance.BackColor = Color.FromArgb(230, 230, 230);
                            e.Appearance.Font = new Font(e.Appearance.Font ?? this.Font, FontStyle.Italic);
                            e.HighPriority = true;
                            return;
                        }
                    }
                    catch
                    {
                        // ignore date/compare errors
                    }
                }
            }
            catch
            {
                // swallow styling errors to avoid UI crash
            }
        }

        private void GvPrograms_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            try
            {
                if (e.RowHandle < 0 || e.Column == null) return;
                var view = sender as GridView;
                if (view == null) return;

                var row = view.GetRow(e.RowHandle) as LuckyDrawProgrameDTO;
                if (row == null) return;

                // Nếu cột là SYURYOU_DATE và đã hết hạn -> tô đỏ nhạt ô đó
                if (string.Equals(e.Column.FieldName, "SYURYOU_DATE", StringComparison.OrdinalIgnoreCase))
                {
                    if (row.SYURYOU_DATE.HasValue && row.SYURYOU_DATE.Value.Date < DateTime.Now.Date)
                    {
                        e.Appearance.BackColor = Color.FromArgb(255, 230, 230); // đỏ nhạt
                        e.Appearance.ForeColor = Color.DarkRed;
                        // no e.HighPriority on RowCellStyleEventArgs
                    }
                }
            }
            catch
            {
                // swallow styling errors
            }
        }

        private async void GvHistory_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                var view = sender as GridView;
                if (view == null) return;

                var hitInfo = view.CalcHitInfo(view.GridControl.PointToClient(Control.MousePosition));
                if (!hitInfo.InRowCell || hitInfo.Column == null) return;

                if (hitInfo.Column.FieldName != "MAE_IMG_PREVIEW" && hitInfo.Column.FieldName != "ATO_IMG_PREVIEW")
                    return;

                // Lấy DTO nếu có
                var rowObj = view.GetRow(hitInfo.RowHandle);
                LuckyDrawHistoryDTO row = rowObj as LuckyDrawHistoryDTO;

                // Ưu tiên TempImagePath nếu có
                string tempPath = null;
                if (row != null)
                {
                    var prop = row.GetType().GetProperty("TempImagePath");
                    if (prop != null)
                        tempPath = prop.GetValue(row) as string;
                }

                Image img = null;
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try { img = Image.FromFile(tempPath); }
                    catch { img = null; }
                }

                // Nếu không có temp file, thử base64 từ DTO hoặc cell
                string base64 = null;
                if (img == null)
                {
                    if (row != null)
                    {
                        base64 = hitInfo.Column.FieldName == "MAE_IMG_PREVIEW" ? row.MAE_JISSHI_BG_IMG : row.ATO_JISSHI_BG_IMG;
                    }
                    else
                    {
                        var dataField = hitInfo.Column.FieldName == "MAE_IMG_PREVIEW" ? "MAE_JISSHI_BG_IMG" : "ATO_JISSHI_BG_IMG";
                        var cell = gvHistory.GetListSourceRowCellValue(hitInfo.RowHandle, dataField);
                        base64 = cell?.ToString();
                    }

                    // If base64 is empty but we have a row with IDs, try to fetch images on-demand for that single record
                    if (string.IsNullOrWhiteSpace(base64) && row != null && row.PROGRAME_JISSHI_ID != 0)
                    {
                        try
                        {
                            var fetched = await LuckyDrawService.GetHistoryByIdAsync(row.PROGRAME_JISSHI_ID);
                            if (fetched != null)
                            {
                                base64 = hitInfo.Column.FieldName == "MAE_IMG_PREVIEW" ? fetched.MAE_JISSHI_BG_IMG : fetched.ATO_JISSHI_BG_IMG;
                                // cache back to DTO
                                try
                                {
                                    if (hitInfo.Column.FieldName == "MAE_IMG_PREVIEW")
                                        row.MAE_JISSHI_BG_IMG = base64;
                                    else
                                        row.ATO_JISSHI_BG_IMG = base64;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(base64) && img == null)
                    {
                        try
                        {
                            img = await Task.Run(() => Base64ToImage(base64));
                        }
                        catch
                        {
                            img = null;
                        }
                    }
                }

                if (img == null)
                {
                    MessageBox.Show("Không có ảnh để hiển thị.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Hiển thị viewer (clone để không khóa file gốc)
                try
                {
                    using (var clone = new Bitmap(img))
                    {
                        var viewer = new ImageViewerForm(clone);
                        viewer.ShowDialog();
                    }
                }
                finally
                {
                    try { img.Dispose(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
            }
        }

        private async Task FetchAndCacheHistoryImageAsync(decimal jisshiId, LuckyDrawHistoryDTO dto, string which) // which = "MAE" or "ATO"
        {
            if (jisshiId == 0) return;
            var cacheKey = $"{jisshiId}_{which}";
            lock (_historyImageCache)
            {
                if (_historyImageCache.ContainsKey(cacheKey)) return; // already cached
            }

            try
            {
                // fetch single history record with images
                var fetched = await LuckyDrawService.GetHistoryByIdAsync(jisshiId);
                if (fetched == null) return;

                // pick target base64
                string base64 = which == "MAE" ? fetched.MAE_JISSHI_BG_IMG : fetched.ATO_JISSHI_BG_IMG;
                if (string.IsNullOrWhiteSpace(base64)) return;

                // decode + resize off UI thread
                var img = await Task.Run(() =>
                {
                    try
                    {
                        var full = Base64ToImage(base64);
                        if (full == null) return GetDefaultImage(35, 35);
                        try
                        {
                            var thumb = ResizeImage(full, 35, 35);
                            return thumb;
                        }
                        finally
                        {
                            try { full.Dispose(); }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                            }
                        }
                    }
                    catch
                    {
                        return GetDefaultImage(35, 35);
                    }
                });

                // store in cache and update DTO in grid datasource (if present)
                lock (_historyImageCache)
                {
                    if (!_historyImageCache.ContainsKey(cacheKey))
                        _historyImageCache[cacheKey] = img;
                    else
                        try { img.Dispose(); }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                        }
                }

                // update DTO (store base64 so subsequent requests don't re-fetch) and refresh row
                this.Invoke((Action)(() =>
                {
                    try
                    {
                        if (dto != null)
                        {
                            if (which == "MAE") dto.MAE_JISSHI_BG_IMG = fetched.MAE_JISSHI_BG_IMG;
                            else dto.ATO_JISSHI_BG_IMG = fetched.ATO_JISSHI_BG_IMG;
                        }

                        var handle = gvHistory.LocateByValue("PROGRAME_JISSHI_ID", jisshiId);
                        if (handle >= 0) gvHistory.RefreshRow(handle);
                        else gvHistory.RefreshData();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
            }
        }

        private void SetupHistoryGrid()
        {
            gvHistory.Columns.Clear();
            gvHistory.OptionsView.ShowColumnHeaders = true;
            gvHistory.OptionsView.ShowAutoFilterRow = true;
            // CHÚ Ý: bật Editable = true nhưng sẽ kiểm soát mở editor qua ShowingEditor
            gvHistory.OptionsBehavior.Editable = true;
            gvHistory.OptionsView.ShowGroupPanel = false;
            gvHistory.OptionsView.ColumnAutoWidth = false;
            gvHistory.RowHeight = 22;

            //// STT (unbound)
            //var colStt = new DevExpress.XtraGrid.Columns.GridColumn
            //{
            //    Caption = "STT",
            //    FieldName = "STT",
            //    Visible = true,
            //    UnboundType = DevExpress.Data.UnboundColumnType.Integer,
            //    Width = 50,
            //    OptionsColumn = { AllowEdit = false }
            //};
            //gvHistory.Columns.Add(colStt);

            // ID thực hiện
            var colExecId = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "PROGRAME_JISSHI_ID",
                Caption = "ID lần thực hiện",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colExecId);

            var colProgId = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "LUCKY_DRAW_PROGRAME_ID",
                Caption = "Mã chương trình",
                Visible = true,
                Width = 160,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colProgId);

            var colEventId = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "LUCKY_DRAW_ID",
                Caption = "Mã sự kiện",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colEventId);

            // Số tham chiếu hóa đơn - CHO PHÉP SỬA
            var colMitumori = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "MITUMORI_NO_SANSYO",
                Caption = "Số tham chiếu hóa đơn",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = true } // <- allow edit
            };
            gvHistory.Columns.Add(colMitumori);

            // Thông tin người trúng - CHO PHÉP SỬA các trường theo yêu cầu
            // (ở file gốc có 2 lần thêm cột tên, giữ 1 định nghĩa rõ ràng với AllowEdit = true)
            gvHistory.Columns.Add(new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOKAKU_HITO_NAME",
                Caption = "Tên người trúng",
                Visible = true,
                Width = 200,
                OptionsColumn = { AllowEdit = true } // <- allow edit
            });

            var colPhone = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOKAKU_HITO_PHONE",
                Caption = "SĐT",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = true } // <- allow edit
            };
            gvHistory.Columns.Add(colPhone);

            var colAddress = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOKAKU_HITO_ADDRESS",
                Caption = "Địa chỉ",
                Visible = true,
                Width = 240,
                OptionsColumn = { AllowEdit = true } // <- allow edit
            };
            gvHistory.Columns.Add(colAddress);

            // Tỉ lệ cấu hình (log)
            var colRateLog = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "RATE_LOG_SPLIT",
                Caption = "Tỉ lệ cấu hình trúng thưởng %",
                Visible = true,
                Width = 200,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colRateLog);

            // Thông tin giải thưởng / voucher
            var colMeisaiId = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KENSYO_DRAW_MEISAI_ID",
                Caption = "STT giải thưởng",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colMeisaiId);

            var colVoucher = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KENSYO_VOUCHER_CODE",
                Caption = "Mã voucher",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colVoucher);

            var colMeisaiSuryo = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KENSYO_DRAW_MEISAI_SURYO",
                Caption = "Số lượng",
                Visible = true,
                Width = 100,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colMeisaiSuryo);

            // Ngày bắt đầu / kết thúc chương trình (hiển thị, không edit)
            var colKaishi = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KAISHI_DATE",
                Caption = "Ngày bắt đầu",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = false }
            };
            colKaishi.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colKaishi.DisplayFormat.FormatString = "dd/MM/yyyy";
            gvHistory.Columns.Add(colKaishi);

            var colSyuryou = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "SYURYOU_DATE",
                Caption = "Ngày kết thúc",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = false }
            };
            colSyuryou.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colSyuryou.DisplayFormat.FormatString = "dd/MM/yyyy";
            gvHistory.Columns.Add(colSyuryou);

            // Ngày tạo
            var colTorokuDate = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "TOROKU_DATE",
                Caption = "Ngày tạo",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colTorokuDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colTorokuDate.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            gvHistory.Columns.Add(colTorokuDate);

            var colTorokuId = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "TOROKU_ID",
                Caption = "Người tạo",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colTorokuId);

            // Thời điểm quay có giây
            var colJisshiStart = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "JISSHI_KAISHI_DATE",
                Caption = "Thời điểm bắt đầu quay",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colJisshiStart.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colJisshiStart.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm:ss";
            gvHistory.Columns.Add(colJisshiStart);

            var colJisshiEnd = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "JISSHI_SYURYOU_DATE",
                Caption = "Thời điểm kết thúc quay",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colJisshiEnd.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colJisshiEnd.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm:ss";
            gvHistory.Columns.Add(colJisshiEnd);

            var picRepoHistory = new RepositoryItemPictureEdit
            {
                SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom,
                NullText = " ",
                ShowMenu = false,
                CustomHeight = 30
            };
            gcHistory.RepositoryItems.Add(picRepoHistory);

            // Cột preview ảnh trước / sau (unbound object)
            var colMaePreview = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "MAE_IMG_PREVIEW",
                Caption = "Ảnh trước quay (Preview)",
                Visible = true,
                Width = 80,
                UnboundType = DevExpress.Data.UnboundColumnType.Object,
                ColumnEdit = picRepoHistory,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colMaePreview);

            var colAtoPreview = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "ATO_IMG_PREVIEW",
                Caption = "Ảnh sau quay (Preview)",
                Visible = true,
                Width = 80,
                UnboundType = DevExpress.Data.UnboundColumnType.Object,
                ColumnEdit = picRepoHistory,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colAtoPreview);

            // Các cột editable liên quan tới thông tin nhận hàng (giữ nguyên)
            var colKokakuNameGet = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOKAKU_NAME_SYUTOKU",
                Caption = "Tên người nhận",
                Visible = true,
                Width = 180,
                OptionsColumn = { AllowEdit = true }
            };
            gvHistory.Columns.Add(colKokakuNameGet);

            var colKokakuPhoneGet = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOKAKU_PHONE_SYUTOKU",
                Caption = "SĐT người nhận",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = true }
            };
            gvHistory.Columns.Add(colKokakuPhoneGet);

            var colKokakuSho = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOKAKU_SHOUMEISYO_NO_SYUTOKU",
                Caption = "Số căn cước người nhận",
                Visible = true,
                Width = 160,
                OptionsColumn = { AllowEdit = true }
            };
            gvHistory.Columns.Add(colKokakuSho);

            var colKokakuAddrGet = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOKAKU_ADDRESS_SYUTOKU",
                Caption = "Địa chỉ người nhận",
                Visible = true,
                Width = 240,
                OptionsColumn = { AllowEdit = true }
            };
            gvHistory.Columns.Add(colKokakuAddrGet);

            var colKokakuDateGet = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "KOKAKU_SYUTOKU_DATE",
                Caption = "Ngày nhận",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = true }
            };
            colKokakuDateGet.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colKokakuDateGet.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            gvHistory.Columns.Add(colKokakuDateGet);

            // Nhân viên + tham chiếu xuất kho
            var colTanto = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "TANTO_SYA_NAME",
                Caption = "Nhân viên phát",
                Visible = true,
                Width = 160,
                OptionsColumn = { AllowEdit = true }
            };
            gvHistory.Columns.Add(colTanto);

            var colMitumoriSyutoku = new DevExpress.XtraGrid.Columns.GridColumn
            {
                FieldName = "MITUMORI_NO_SYUTOKU_SANSYO",
                Caption = "Số phiếu xuất kho",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            gvHistory.Columns.Add(colMitumoriSyutoku);

            // Hoàn tất layout
            gvHistory.BestFitColumns();

            gvHistory.CustomUnboundColumnData -= GvHistory_CustomUnboundColumnData;
            gvHistory.CustomUnboundColumnData += GvHistory_CustomUnboundColumnData;
            gvHistory.DoubleClick -= GvHistory_DoubleClick;
            gvHistory.DoubleClick += GvHistory_DoubleClick;

            // Đăng ký ShowingEditor để giới hạn edit chỉ trên hàng đã lock
            gvHistory.ShowingEditor -= GvHistory_ShowingEditor;
            gvHistory.ShowingEditor += GvHistory_ShowingEditor;
        }

        private void GvHistory_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)

        {
            if (e.Column.FieldName == "STT" && e.IsGetData)
            {
                var view = sender as GridView;
                if (view == null)
                {
                    e.Value = 0;
                    return;
                }

                // STT tăng dần từ trên xuống, giống uc_EventManagement
                e.Value = e.ListSourceRowIndex + 1;
                return;
            }

            if (!e.IsGetData) return;
            if (e.Column.FieldName != "MAE_IMG_PREVIEW" && e.Column.FieldName != "ATO_IMG_PREVIEW")
                return;

            try
            {
                // Convert list-source index -> row handle rồi lấy DTO (để xử lý sort/filter)
                int rowHandle = gvHistory.GetRowHandle(e.ListSourceRowIndex);
                var rowObj = rowHandle >= 0 ? gvHistory.GetRow(rowHandle) : null;
                LuckyDrawHistoryDTO row = rowObj as LuckyDrawHistoryDTO;

                // key base dùng PROGRAME_JISSHI_ID nếu có
                var keyBase = row != null && row.PROGRAME_JISSHI_ID != 0
                    ? row.PROGRAME_JISSHI_ID.ToString()
                    : e.ListSourceRowIndex.ToString();

                // Ưu tiên temp file nếu DTO có TempImagePath
                string tempPath = null;
                if (row != null)
                {
                    var prop = row.GetType().GetProperty("TempImagePath");
                    if (prop != null)
                        tempPath = prop.GetValue(row) as string;
                }

                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    var tmpKey = keyBase + "_tmp:" + tempPath;
                    if (!_historyImageCache.TryGetValue(tmpKey, out var tmpImg))
                    {
                        try
                        {
                            using (var img = Image.FromFile(tempPath))
                            {
                                tmpImg = ResizeImage(img, 35, 35);
                            }
                        }
                        catch
                        {
                            tmpImg = GetDefaultImage(35, 35);
                        }
                        _historyImageCache[tmpKey] = tmpImg;
                    }

                    e.Value = _historyImageCache[tmpKey];
                    return;
                }

                // Lấy base64 từ DTO hoặc list-source cell
                string dataField = e.Column.FieldName == "MAE_IMG_PREVIEW" ? "MAE_JISSHI_BG_IMG" : "ATO_JISSHI_BG_IMG";
                string base64 = null;

                if (row != null)
                {
                    base64 = e.Column.FieldName == "MAE_IMG_PREVIEW" ? row.MAE_JISSHI_BG_IMG : row.ATO_JISSHI_BG_IMG;
                }
                else
                {
                    var cell = gvHistory.GetListSourceRowCellValue(e.ListSourceRowIndex, dataField);
                    base64 = cell?.ToString();
                }

                // If there's no base64 available, trigger background fetch for that event (only once concurrently)
                if (string.IsNullOrWhiteSpace(base64))
                {
                    if (row != null && row.PROGRAME_JISSHI_ID != 0)
                    {
                        var fetchKey = $"hist:{row.PROGRAME_JISSHI_ID}_" + (e.Column.FieldName == "MAE_IMG_PREVIEW" ? "MAE" : "ATO");
                        lock (_historyImageFetchInProgress)
                        {
                            if (!_historyImageFetchInProgress.Contains(fetchKey))
                            {
                                _historyImageFetchInProgress.Add(fetchKey);
                                // fire-and-forget background fetch per record
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        await FetchAndCacheHistoryImageAsync(row.PROGRAME_JISSHI_ID, row, (e.Column.FieldName == "MAE_IMG_PREVIEW" ? "MAE" : "ATO"));
                                    }
                                    finally
                                    {
                                        lock (_historyImageFetchInProgress)
                                        {
                                            _historyImageFetchInProgress.Remove(fetchKey);
                                        }
                                    }
                                });
                            }
                        }
                    }

                    e.Value = GetDefaultImage(35, 35);
                    return;
                }

                var cacheKey = keyBase + "_" + (e.Column.FieldName == "MAE_IMG_PREVIEW" ? "MAE" : "ATO");
                if (!_historyImageCache.TryGetValue(cacheKey, out var cached))
                {
                    try
                    {
                        var img = Base64ToImage(base64);
                        if (img != null)
                        {
                            try { cached = ResizeImage(img, 35, 35); }
                            finally { img.Dispose(); }
                        }
                        else
                        {
                            cached = GetDefaultImage(35, 35);
                        }
                    }
                    catch
                    {
                        cached = GetDefaultImage(35, 35);
                    }
                    _historyImageCache[cacheKey] = cached;
                }

                e.Value = _historyImageCache[cacheKey];
            }
            catch 
            {
                e.Value = GetDefaultImage(35, 35);
            }
        }
        // Chặn mở editor trên các hàng không phải hàng được lock của Program
        private void GvPrograms_ShowingEditor(object sender, CancelEventArgs e)
        {
            var view = sender as GridView;
            if (view == null)
            {
                e.Cancel = true;
                return;
            }

            if (_editingRowHandleProgram.HasValue)
            {
                if (view.FocusedRowHandle != _editingRowHandleProgram.Value)
                {
                    e.Cancel = true;
                    return;
                }

                // If the row is an existing row (not new), block editing of key fields:
                var row = view.GetFocusedRow() as LuckyDrawProgrameDTO;
                var col = view.FocusedColumn;
                if (row != null && col != null)
                {
                    // Many DTOs in this project support IsNewRow() helper used elsewhere.
                    // Allow editing of keys only for new rows.
                    bool isNew = false;
                    try { isNew = row.IsNewRow(); } catch { isNew = false; }

                    if (!isNew)
                    {
                        if (col.FieldName == "LUCKY_DRAW_PROGRAME_ID" || col.FieldName == "LUCKY_DRAW_ID")
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }
            else
            {
                // Not in edit mode -> block all edits
                e.Cancel = true;
            }
        }

        // Chặn mở editor trên các hàng không phải hàng được lock của History
        private void GvHistory_ShowingEditor(object sender, CancelEventArgs e)
        {
            var view = sender as GridView;
            if (view == null)
            {
                e.Cancel = true;
                return;
            }

            if (_editingRowHandleHistory.HasValue)
            {
                if (view.FocusedRowHandle != _editingRowHandleHistory.Value)
                {
                    e.Cancel = true;
                    return;
                }
            }
            else
            {
                // Không ở chế độ thêm/sửa -> chặn mọi edit
                e.Cancel = true;
            }
        }

        private void GvPrograms_DoubleClick(object sender, EventArgs e)
        {
            SharedToolbar_EditClicked(this, EventArgs.Empty);
        }

        private async void GvPrograms_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            // Khi người dùng thay đổi chọn chương trình, không thay đổi nguồn dữ liệu của lịch sử.
            // Yêu cầu: hiển thị toàn bộ lịch sử bất kể chọn program nào -> không load lại hoặc filter theo program.
            // Chỉ clear cache ảnh và repaint grid (không thay dữ liệu).
            try
            {
                ClearHistoryImageCache();
                try { gvHistory.RefreshData(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            }
            catch 
            {
            }
        }

        public string SelectedLuckyDrawId { get; private set; }

        private string GenerateProgramId()
        {
            // Sinh id client-side đơn giản: timestamp + short guid
            return $"PG{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        }

        public void SetSelectedLuckyDrawId(string luckyDrawId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(luckyDrawId)) return;
                SelectedLuckyDrawId = luckyDrawId;

                var focused = gvPrograms.GetFocusedRow() as LuckyDrawProgrameDTO;
                if (focused != null)
                {
                    focused.LUCKY_DRAW_ID = luckyDrawId;
                    try { gvPrograms.RefreshRow(gvPrograms.FocusedRowHandle); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                }

                var fld = this.GetType().GetField("txtLuckyDrawId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (fld != null)
                {
                    var ctrl = fld.GetValue(this) as Control;
                    if (ctrl != null)
                    {
                        if (ctrl.InvokeRequired)
                        {
                            ctrl.Invoke((MethodInvoker)(() =>
                            {
                                if (ctrl is DevExpress.XtraEditors.TextEdit te) te.Text = luckyDrawId;
                                else if (ctrl is TextBox tb) tb.Text = luckyDrawId;
                            }));
                        }
                        else
                        {
                            if (ctrl is DevExpress.XtraEditors.TextEdit te) te.Text = luckyDrawId;
                            else if (ctrl is TextBox tb) tb.Text = luckyDrawId;
                        }
                    }
                }

                // Trigger reload of programs and event lookup so UI reflects selected event
                try
                {
                    _ = LoadEventLookupAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
                try
                {
                    if (_programCrudManager != null)
                        _ = _programCrudManager.LoadDataAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
            }
        }

        private void SetupCrudManagers()
        {
            _programCrudManager = new CrudManager<LuckyDrawProgrameDTO>(gcPrograms, gvPrograms);

            // CreateNewEntityFunc: trả về DTO đã prefill (sẽ được AddNewRow sử dụng)
            _programCrudManager.CreateNewEntityFunc = () =>
            {
                var dto = new LuckyDrawProgrameDTO();
                try { dto.MarkAsNew(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
                try { dto.TOROKU_ID = "admin"; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
                try { dto.TOROKU_DATE = DateTime.Now; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.LUCKY_DRAW_PROGRAME_ID))
                        dto.LUCKY_DRAW_PROGRAME_ID = GenerateProgramId();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
                try
                {
                    if (!string.IsNullOrWhiteSpace(SelectedLuckyDrawId))
                        dto.LUCKY_DRAW_ID = SelectedLuckyDrawId;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
                return dto;
            };

            // Load programs (filter by SelectedLuckyDrawId)
            _programCrudManager.LoadDataFunc = async () => await LuckyDrawService.GetAllProgramsAsync(SelectedLuckyDrawId);

            // Delete program
            _programCrudManager.DeleteFunc = async (entity) =>
                await LuckyDrawService.DeleteProgramAsync(entity.LUCKY_DRAW_PROGRAME_ID);

            // Save function với logging
            _programCrudManager.SaveFunc = async (entity) =>
            {
                try
                {
                    // Đảm bảo KOSIN_ID được set
                    entity.KOSIN_ID = entity.TOROKU_ID;

                    // debug payload
                    try
                    {
                        var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(entity, new Newtonsoft.Json.JsonSerializerSettings
                        {
                            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                            DateFormatString = "yyyy-MM-dd"
                        });
                       
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }

                    if (entity.IsNewRow())
                    {
                        var (success, message) = await LuckyDrawService.CreateProgramAsync(entity);
                        return (success, message);
                    }
                    else
                    {
                        var (success, message) = await LuckyDrawService.UpdateProgramAsync(entity.LUCKY_DRAW_PROGRAME_ID, entity);
                        return (success, message);
                    }
                }
                catch (Exception ex)
                {
                    return (false, "Lỗi khi lưu chương trình: " + ex.Message);
                }
            };

            // History manager
            _historyCrudManager = new CrudManager<LuckyDrawHistoryDTO>(gcHistory, gvHistory);

            // CreateNewEntityFunc cho history: set TOROKU_ID, liên kết Program hiện tại nếu có
            _historyCrudManager.CreateNewEntityFunc = () =>
            {
                var dto = new LuckyDrawHistoryDTO();
                try { dto.MarkAsNew(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
                try { dto.TOROKU_ID = "admin"; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                try
                {
                    // Prefer program currently focused in programs grid
                    var prog = gvPrograms.GetFocusedRow() as LuckyDrawProgrameDTO;
                    if (prog != null)
                    {
                        // set FK links
                        if (!string.IsNullOrWhiteSpace(prog.LUCKY_DRAW_PROGRAME_ID))
                            dto.LUCKY_DRAW_PROGRAME_ID = prog.LUCKY_DRAW_PROGRAME_ID;
                        if (!string.IsNullOrWhiteSpace(prog.LUCKY_DRAW_ID))
                            dto.LUCKY_DRAW_ID = prog.LUCKY_DRAW_ID;

                        // Set program start/end dates onto the new history DTO
                        dto.KAISHI_DATE = prog.KAISHI_DATE;
                        dto.SYURYOU_DATE = prog.SYURYOU_DATE;
                    }
                    else
                    {
                        var progId = GetSelectedProgramId();
                        if (!string.IsNullOrWhiteSpace(progId))
                            dto.LUCKY_DRAW_PROGRAME_ID = progId;
                        if (!string.IsNullOrWhiteSpace(SelectedLuckyDrawId))
                            dto.LUCKY_DRAW_ID = SelectedLuckyDrawId;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                return dto;
            };

            // LOAD DATA FOR HISTORY: show ALL history (not filtered by program)
            // Load without images to avoid large payload; images fetched on-demand in double-click handler
            _historyCrudManager.LoadDataFunc = async () =>
            {
                try
                {
                    var list = await LuckyDrawService.GetHistoryAsync(includeImages: false);
                    return list ?? new List<LuckyDrawHistoryDTO>();
                }
                catch 
                {
                    return new List<LuckyDrawHistoryDTO>();
                }
            };
            _historyCrudManager.DeleteFunc = async (entity) => await Task.FromResult((false, "Delete history not implemented"));

            _historyCrudManager.SaveFunc = async (entity) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(entity.LUCKY_DRAW_ID))
                    {
                        if (!string.IsNullOrWhiteSpace(SelectedLuckyDrawId))
                        {
                            entity.LUCKY_DRAW_ID = SelectedLuckyDrawId;
                        }
                        else
                        {
                            var progId = GetSelectedProgramId();
                            if (!string.IsNullOrWhiteSpace(progId))
                            {
                                var prog = await LuckyDrawService.GetProgramByIdAsync(progId);
                                if (prog != null && !string.IsNullOrWhiteSpace(prog.LUCKY_DRAW_ID))
                                    entity.LUCKY_DRAW_ID = prog.LUCKY_DRAW_ID;
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(entity.LUCKY_DRAW_ID))
                        return (false, "LUCKY_DRAW_ID không được để trống (vui lòng chọn chương trình hoặc sự kiện).");

                    if (string.IsNullOrWhiteSpace(entity.LUCKY_DRAW_PROGRAME_ID))
                    {
                        var focusedProgId = GetSelectedProgramId();
                        if (!string.IsNullOrWhiteSpace(focusedProgId))
                            entity.LUCKY_DRAW_PROGRAME_ID = focusedProgId;
                        else
                            return (false, "LUCKY_DRAW_PROGRAME_ID không được để trống.");
                    }

                    if (string.IsNullOrWhiteSpace(entity.TOROKU_ID))
                        entity.TOROKU_ID = "admin";

                    var (success, message, jisshiId) = await LuckyDrawService.SaveHistoryAsync(entity);
                    return (success, message);
                }
                catch (Exception ex)
                {
                    return (false, "Lỗi khi lưu lịch sử: " + ex.Message);
                }
            };
        }
        // Lấy ProgramId từ hàng hiện tại (hỗ trợ DTO hoặc DataRow)
        private string GetSelectedProgramId()
        {
            var row = gvPrograms.GetFocusedRow();
            if (row == null) return null;

            if (row is LuckyDrawProgrameDTO dto)
                return dto.LUCKY_DRAW_PROGRAME_ID;

            var val = gvPrograms.GetRowCellValue(gvPrograms.FocusedRowHandle, "LUCKY_DRAW_PROGRAME_ID")
                   ?? gvPrograms.GetRowCellValue(gvPrograms.FocusedRowHandle, "LUCKY_DRAW_ID")
                   ?? gvPrograms.GetRowCellValue(gvPrograms.FocusedRowHandle, "ProgramId");
            return val?.ToString();
        }

        #region SHARED TOOLBAR HANDLERS

        private void SharedToolbar_AddClicked(object sender, EventArgs e)
        {
            if (_active_grid_is_program())
            {
                try
                {
                    LuckyDrawProgrameDTO newDto = _programCrudManager.CreateNewEntityFunc != null ?
                        _programCrudManager.CreateNewEntityFunc() : new LuckyDrawProgrameDTO();

                    try { newDto.TOROKU_ID = "admin"; }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    try { newDto.TOROKU_DATE = DateTime.Now; }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }

                    // Sinh id chương trình client-side để hiển thị ngay trên UI
                    try
                    {
                        if (string.IsNullOrWhiteSpace(newDto.LUCKY_DRAW_PROGRAME_ID))
                        {
                            newDto.LUCKY_DRAW_PROGRAME_ID = GenerateProgramId();
                        }
                    }
                    catch { /* non-critical */ }

                    if (!string.IsNullOrWhiteSpace(SelectedLuckyDrawId))
                    {
                        try { newDto.LUCKY_DRAW_ID = SelectedLuckyDrawId; }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                        }
                    }

                    // Thêm hàng mới qua CrudManager
                    _programCrudManager.AddNewRow();

                    // Khóa chỉ cho phép edit trên hàng mới và mở editor tự động
                    _editingRowHandleProgram = gvPrograms.FocusedRowHandle;
                    try
                    {
                        // Đảm bảo hàng visible rồi open editor
                        if (_editingRowHandleProgram.HasValue)
                        {
                            gvPrograms.MakeRowVisible(_editingRowHandleProgram.Value);
                            gvPrograms.ShowEditor();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }

                    // Gán giá trị mặc định cho DTO đang focus
                    var focused = gvPrograms.GetFocusedRow() as LuckyDrawProgrameDTO;
                    if (focused != null)
                    {
                        focused.TOROKU_ID = newDto.TOROKU_ID;
                        focused.TOROKU_DATE = newDto.TOROKU_DATE;

                        try
                        {
                            if (string.IsNullOrWhiteSpace(focused.LUCKY_DRAW_PROGRAME_ID))
                                focused.LUCKY_DRAW_PROGRAME_ID = newDto.LUCKY_DRAW_PROGRAME_ID;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                        }

                        if (!string.IsNullOrWhiteSpace(newDto.LUCKY_DRAW_ID))
                            focused.LUCKY_DRAW_ID = newDto.LUCKY_DRAW_ID;
                    }

                    _sharedToolbar.SetEditingMode(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi thêm chương trình: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Thêm mới History và khóa chỉ cho phép edit trên dòng vừa thêm
                _historyCrudManager.AddNewRow();

                _editingRowHandleHistory = gvHistory.FocusedRowHandle;
                try
                {
                    if (_editingRowHandleHistory.HasValue)
                    {
                        gvHistory.MakeRowVisible(_editingRowHandleHistory.Value);
                        gvHistory.ShowEditor();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                // Set mặc định cho DTO mới nếu cần
                var newRow = gvHistory.GetFocusedRow() as LuckyDrawHistoryDTO;
                if (newRow != null)
                {
                    try { newRow.TOROKU_ID = "admin"; }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    var progId = GetSelectedProgramId();
                    if (!string.IsNullOrWhiteSpace(progId))
                        newRow.LUCKY_DRAW_PROGRAME_ID = progId;
                }

                _sharedToolbar.SetEditingMode(true);
            }
        }

        private bool _active_grid_is_program() => _activeGrid == ActiveGridType.Program;

        private void SharedToolbar_EditClicked(object sender, EventArgs e)
        {
            _sharedToolbar.SetEditingMode(true);

            if (_active_grid_is_program())
            {
                _programCrudManager.EnableEdit();

                // Khóa chỉ cho phép edit trên hàng đang chọn
                _editingRowHandleProgram = gvPrograms.FocusedRowHandle;
                try { gvPrograms.ShowEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            }
            else
            {
                _historyCrudManager.EnableEdit();

                // Khóa chỉ cho phép edit trên hàng history đang chọn
                _editingRowHandleHistory = gvHistory.FocusedRowHandle;
                try { gvHistory.ShowEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            }
        }

        private async void SharedToolbar_DeleteClicked(object sender, EventArgs e)
        {
            if (_active_grid_is_program())
            {
                await _programCrudManager.DeleteRowAsync();
            }
            else
            {
                await _historyCrudManager.DeleteRowAsync();
            }
        }

        private async void SharedToolbar_SaveClicked(object sender, EventArgs e)
        {
            bool success = false;

            if (_active_grid_is_program())
            {
                try
                {
                    // Commit in-cell editor trước khi validate/save
                    gvPrograms.PostEditor();
                    gvPrograms.CloseEditor();
                    gvPrograms.UpdateCurrentRow();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                var focusedEntity = gvPrograms.GetFocusedRow() as ICrudEntity;
                string focusedProgId = null;
                if (focusedEntity != null)
                {
                    var (isValid, err) = focusedEntity.Validate();
                    if (!isValid)
                    {
                        MessageBox.Show(err, "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (gvPrograms.GetFocusedRow() is LuckyDrawProgrameDTO dto && !string.IsNullOrWhiteSpace(dto.LUCKY_DRAW_PROGRAME_ID))
                        focusedProgId = dto.LUCKY_DRAW_PROGRAME_ID;
                }

                success = await _programCrudManager.SaveChangesAsync();

                if (success)
                {
                    // Reset khóa edit cho program
                    _editingRowHandleProgram = null;
                    try { gvPrograms.CloseEditor(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }

                    // Refresh lookup và dữ liệu, set focus về row đã lưu nếu có
                    try { await LoadEventLookupAsync(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    try { await _programCrudManager.LoadDataAsync(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }

                    if (!string.IsNullOrWhiteSpace(focusedProgId))
                    {
                        try
                        {
                            var handle = gvPrograms.LocateByValue("LUCKY_DRAW_PROGRAME_ID", focusedProgId);
                            if (handle >= 0)
                            {
                                gvPrograms.FocusedRowHandle = handle;
                                gvPrograms.MakeRowVisible(handle);
                                gvPrograms.RefreshRow(handle);
                            }
                            else
                            {
                                gvPrograms.RefreshData();
                            }
                        }
                        catch { gvPrograms.RefreshData(); }
                    }
                    else
                    {
                        gvPrograms.RefreshData();
                    }

                    _sharedToolbar.SetEditingMode(false);
                }
            }
            else
            {
                try
                {
                    gvHistory.PostEditor();
                    gvHistory.CloseEditor();
                    gvHistory.UpdateCurrentRow();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                var focused = gvHistory.GetFocusedRow() as ICrudEntity;
                if (focused != null)
                {
                    var (isValid, err) = focused.Validate();
                    if (!isValid)
                    {
                        MessageBox.Show(err, "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                success = await _historyCrudManager.SaveChangesAsync();
                if (success)
                {
                    // Reset khóa edit history khi lưu thành công
                    _editingRowHandleHistory = null;
                    try { gvHistory.CloseEditor(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    _sharedToolbar.SetEditingMode(false);
                }
            }
        }

        private async void SharedToolbar_CancleClicked(object sender, EventArgs e)
        {
            if (_active_grid_is_program())
            {
                await _programCrudManager.CancelChangesAsync();
                _editingRowHandleProgram = null;
                try { gvPrograms.CloseEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            }
            else
            {
                await _historyCrudManager.CancelChangesAsync();
                // Reset khóa edit history khi hủy
                _editingRowHandleHistory = null;
                try { gvHistory.CloseEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            }

            _sharedToolbar.SetEditingMode(false);
        }

        private async void SharedToolbar_RefreshClicked(object sender, EventArgs e)
        {
            try
            {
                // ensure managers exist
                if (_historyCrudManager == null) SetupCrudManagers();

                // if editing, cancel to allow clean reload
                if (_historyCrudManager != null && _historyCrudManager.IsEditing)
                {
                    await _historyCrudManager.CancelChangesAsync();
                }

                // clear local cache (images) and manager data, then reload
                ClearHistoryImageCache();
                _historyCrudManager.ClearData();
                await _historyCrudManager.LoadDataAsync();

                gvHistory.RefreshData();
            }
            catch (Exception ex)
            {
            }
        }

        #endregion


    }
}