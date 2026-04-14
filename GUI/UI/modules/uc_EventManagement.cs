using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using GUI.DTO;
using GUI.Managers;
using GUI.Service;
using Newtonsoft.Json;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace GUI.UI.modules
{
    public partial class uc_EventManagement : UserControl
    {
        private class BunruiOption
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public BunruiOption(string code, string name) { Code = code; Name = name; }
        }

        // Sample options — nếu dữ liệu phân loại lấy từ server, thay bằng call service và gán vào _bunruiOptions
        private readonly List<BunruiOption> _bunruiOptions = new List<BunruiOption>
        {
            new BunruiOption("A", "Cấu hình tỉ lệ"),
            new BunruiOption("B", "Random"),
        };

        private readonly Dictionary<string, Image> _eventImageCache = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _meisaiImageCache = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _kokakuImageCache = new Dictionary<string, Image>();

        private const long MAX_IMAGE_FILE_SIZE = 50L * 1024 * 1024; // 50 MB
        private List<LuckyDrawDTO> _events;
        private List<LuckyDrawMeisaiDTO> _meisais;
        private string _selectedEventId;

        // CRUD Managers
        private CrudManager<LuckyDrawDTO> _eventCrudManager;
        private CrudManager<LuckyDrawMeisaiDTO> _meisaiCrudManager;
        private CrudManager<LuckyDrawKokakuDTO> _kokakuCrudManager;

        // ✅ THÊM MỚI: Chỉ giữ 1 toolbar dùng chung
        private uc_CrudToolbar _sharedToolbar;

        // ✅ THÊM MỚI: Tracking grid nào đang active
        private enum ActiveGridType { Event, Meisai, Kokaku }
        private ActiveGridType _activeGrid = ActiveGridType.Event;

        // ===== New: only allow editing the single row being added/edited =====
        private int? _editingRowHandleEvent = null;
        private int? _editingRowHandleMeisai = null;
        private int? _editingRowHandleKokaku = null;

        // Dynamic kokaku controls (created at runtime)
        private DevExpress.XtraGrid.GridControl gcKokaku;
        private DevExpress.XtraGrid.Views.Grid.GridView gvKokaku;

        private string _lastLoadedEventIdMeisai = null;
        private string _lastLoadedEventIdKokaku = null;

        private readonly HashSet<string> _eventImageFetchInProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _meisaiImageFetchInProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _kokakuImageFetchInProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public uc_EventManagement()
        {
            InitializeComponent();
            this.Load += Uc_EventManagement_Load;
        }

        private async void Uc_EventManagement_Load(object sender, EventArgs e)
        {
            // Đăng ký 1 lần để Json.NET biết parse theo en-GB / định dạng dd/MM/yyyy HH:mm:ss
            if (JsonConvert.DefaultSettings == null)
            {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    Culture = new CultureInfo("en-GB"), // en-GB dùng dd/MM/yyyy
                    DateParseHandling = DateParseHandling.DateTime,
                    DateFormatString = "dd/MM/yyyy HH:mm:ss",
                    NullValueHandling = NullValueHandling.Ignore
                };
            }

            SetupToolbars();

            // Tạo động gcKokaku / gvKokaku và đặt phía dưới gcMeisai bằng SplitContainer
            EnsureKokakuContainer();

            SetupEventGrid();
            SetupMeisaiGrid();
            SetupKokakuGrid();
            SetupCrudManagers();
            await _eventCrudManager.LoadDataAsync();
        }

        #region DYNAMIC LAYOUT: create kokaku grid under meisai

        private void EnsureKokakuContainer()
        {
            // Nếu đã tạo thì skip
            if (gcKokaku != null) return;

            try
            {
                // Parent của gcMeisai trong Designer
                var parent = gcMeisai?.Parent;
                if (parent == null)
                    return;

                // Remove gcMeisai from parent and insert SplitContainer
                var previousIndex = parent.Controls.GetChildIndex(gcMeisai);
                parent.Controls.Remove(gcMeisai);

                var split = new SplitContainer
                {
                    Orientation = Orientation.Horizontal, // top: meisai, bottom: kokaku
                    Dock = DockStyle.Fill,
                    SplitterWidth = 6,
                    BackColor = SystemColors.Control,
                };

                // Default split height (will adjust on resize)
                split.SplitterDistance = Math.Max(180, split.Height / 2);

                // Add split into parent at same index
                parent.Controls.Add(split);
                parent.Controls.SetChildIndex(split, previousIndex);

                // Reparent gcMeisai into Panel1
                gcMeisai.Dock = DockStyle.Fill;
                split.Panel1.Controls.Add(gcMeisai);

                // Create gcKokaku and gvKokaku
                gcKokaku = new DevExpress.XtraGrid.GridControl();
                gvKokaku = new DevExpress.XtraGrid.Views.Grid.GridView();

                ((System.ComponentModel.ISupportInitialize)(gvKokaku)).BeginInit();
                ((System.ComponentModel.ISupportInitialize)(gcKokaku)).BeginInit();

                gcKokaku.MainView = gvKokaku;
                gcKokaku.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gvKokaku });
                gcKokaku.Dock = DockStyle.Fill;

                // Add to Panel2
                split.Panel2.Controls.Add(gcKokaku);

                ((System.ComponentModel.ISupportInitialize)(gvKokaku)).EndInit();
                ((System.ComponentModel.ISupportInitialize)(gcKokaku)).EndInit();

                // Register click handlers so toolbar can detect active grid
                gcKokaku.Click += (s, e) => _activeGrid = ActiveGridType.Kokaku;
                gvKokaku.Click += (s, e) => _activeGrid = ActiveGridType.Kokaku;
            }
            catch 
            {
            }
        }

        #endregion

        #region SETUP TOOLBARS

        private void SetupToolbars()
        {
            // ===== TẠO TOOLBAR ĐỘNG VÀ ĐẶT VÀO PANEL =====
            _sharedToolbar = new uc_CrudToolbar();
            _sharedToolbar.Dock = DockStyle.Fill; // ✅ Fill trong panelToolbar

            // ✅ Add vào panelToolbar (đã có trong Designer)
            panelToolbar.Controls.Add(_sharedToolbar);

            // ===== ĐẢM BẢO GRID DOCK = FILL =====
            gcEvents.Dock = DockStyle.Fill;
            gcMeisai.Dock = DockStyle.Fill;
            // gcKokaku được tạo động trong EnsureKokakuContainer

            // ===== SUBSCRIBE EVENTS =====
            _sharedToolbar.AddClicked += SharedToolbar_AddClicked;
            _sharedToolbar.EditClicked += SharedToolbar_EditClicked;
            _sharedToolbar.DeleteClicked += SharedToolbar_DeleteClicked;
            _sharedToolbar.SaveClicked += SharedToolbar_SaveClicked;
            _sharedToolbar.CancelClicked += SharedToolbar_CancleClicked;
            _sharedToolbar.RefreshClicked += SharedToolbar_RefreshClicked;

            // ===== TRACKING ACTIVE GRID =====
            gcEvents.Click += (s, e) => _activeGrid = ActiveGridType.Event;
            gcMeisai.Click += (s, e) => _activeGrid = ActiveGridType.Meisai;
            gvEvents.Click += (s, e) => _activeGrid = ActiveGridType.Event;
            gvMeisai.Click += (s, e) => _activeGrid = ActiveGridType.Meisai;

            // If gcKokaku created already, register; otherwise EnsureKokakuContainer will register
            if (gcKokaku != null)
            {
                gcKokaku.Click += (s, e) => _activeGrid = ActiveGridType.Kokaku;
                if (gvKokaku != null) gvKokaku.Click += (s, e) => _activeGrid = ActiveGridType.Kokaku;
            }
        }

        #endregion

        #region SETUP CRUD MANAGERS

        private void SetupCrudManagers()
        {
            // ===== EVENT CRUD MANAGER =====
            _eventCrudManager = new CrudManager<LuckyDrawDTO>(gcEvents, gvEvents);

            // ✅ SỬA: Gán _events khi load để các phần khác có thể tham chiếu reliably
            _eventCrudManager.LoadDataFunc = async () =>
            {
                var list = await LuckyDrawService.GetAllEventsAsync();
                _events = list ?? new List<LuckyDrawDTO>();
                return _events;
            };

            _eventCrudManager.DeleteFunc = async (entity) =>
                await LuckyDrawService.DeleteEventAsync(entity.LUCKY_DRAW_ID);

            // ✅ SaveFunc cho Event (hỗ trợ upload ảnh)
            _eventCrudManager.SaveFunc = async (entity) =>
            {
                if (entity.IsNewRow())
                {
                    // === THÊM MỚI ===

                    // Set KOSIN_ID = TOROKU_ID
                    entity.KOSIN_ID = entity.TOROKU_ID;

                    // Gọi API với ảnh (CreateEventWithImageAsync trả về 3 giá trị)
                    var (success, message, eventId) = await LuckyDrawService.CreateEventWithImageAsync(
                        entity,
                        entity.TempImagePath  // Path ảnh (có thể null)
                    );

                    // Chuyển đổi về (bool, string) để phù hợp với SaveFunc
                    return (success, message);
                }
                else
                {
                    // === CẬP NHẬT ===

                    // Set KOSIN_ID
                    entity.KOSIN_ID = entity.TOROKU_ID;

                    // Kiểm tra có ảnh mới không
                    if (!string.IsNullOrEmpty(entity.TempImagePath))
                    {
                        // Có ảnh mới → Upload
                        return await LuckyDrawService.UpdateEventWithImageAsync(
                            entity.LUCKY_DRAW_ID,
                            entity,
                            entity.TempImagePath
                        );
                    }
                    else
                    {
                        // Không có ảnh mới → Giữ nguyên ảnh cũ
                        return await LuckyDrawService.UpdateEventAsync(
                            entity.LUCKY_DRAW_ID,
                            entity
                        );
                    }
                }
            };

            // ===== MEISAI CRUD MANAGER =====
            _meisaiCrudManager = new CrudManager<LuckyDrawMeisaiDTO>(gcMeisai, gvMeisai);

            _meisaiCrudManager.LoadDataFunc = async () =>
            {
                if (string.IsNullOrEmpty(_selectedEventId))
                    return new List<LuckyDrawMeisaiDTO>();

                // Yêu cầu server trả cả các bản ghi MUKO để UI có thể hiển thị mờ
                return await LuckyDrawService.GetAllMeisaiAsync(_selectedEventId, category: null, includeMuko: true);
            };

            _meisaiCrudManager.DeleteFunc = async (entity) =>
                await LuckyDrawService.DeleteMeisaiAsync(entity.LUCKY_DRAW_ID, entity.LUCKY_DRAW_MEISAI_NO);

            // ✅ SaveFunc cho Meisai (hỗ trợ upload ảnh)
            _meisaiCrudManager.SaveFunc = async (entity) =>
            {
                if (entity.IsNewRow())
                {
                    // === THÊM MỚI ===

                    // Set KOSIN_ID = TOROKU_ID
                    entity.KOSIN_ID = entity.TOROKU_ID;

                    // Gọi API với ảnh (nếu có)
                    return await LuckyDrawService.CreateMeisaiWithImageAsync(
                        entity,
                        entity.TempImagePath  // Path ảnh (có thể null)
                    );
                }
                else
                {
                    // === CẬP NHẬT ===

                    // Set KOSIN_ID
                    entity.KOSIN_ID = entity.TOROKU_ID;

                    // Kiểm tra có ảnh mới không
                    if (!string.IsNullOrEmpty(entity.TempImagePath))
                    {
                        // Có ảnh mới → Upload
                        return await LuckyDrawService.UpdateMeisaiWithImageAsync(
                            entity.LUCKY_DRAW_ID,
                            entity.LUCKY_DRAW_MEISAI_NO,
                            entity,
                            entity.TempImagePath
                        );
                    }
                    else
                    {
                        // Không có ảnh mới → Giữ nguyên ảnh cũ
                        return await LuckyDrawService.UpdateMeisaiAsync(
                            entity.LUCKY_DRAW_ID,
                            entity.LUCKY_DRAW_MEISAI_NO,
                            entity
                        );
                    }
                }
            };

            // ===== KOKAKU CRUD MANAGER =====
            if (gcKokaku != null && gvKokaku != null)
            {
                _kokakuCrudManager = new CrudManager<LuckyDrawKokakuDTO>(gcKokaku, gvKokaku);

                _kokakuCrudManager.LoadDataFunc = async () =>
                {
                    if (string.IsNullOrEmpty(_selectedEventId))
                        return new List<LuckyDrawKokakuDTO>();

                    // IncludeMuko = true để hiển thị kokaku đã bị đánh MUKO (UI sẽ làm mờ)
                    return await LuckyDrawService.GetKokakuByEventAsync(_selectedEventId, includeMuko: true);
                };

                // DELETE: giữ soft-delete bằng gọi API hiện có (SetKokakuMukoFlagAsync) hoặc gọi DeleteKokakuAsync nếu API DELETE tồn tại.
                _kokakuCrudManager.DeleteFunc = async (entity) =>
                {
                    if (entity == null) return (false, "Không có dữ liệu để xóa");
                    try
                    {
                        // Gọi PUT api/vouchers/kokaku/{eventId}/{phone}/muko để đánh dấu muko (soft-delete)
                        var (success, message) = await LuckyDrawService.SetKokakuMukoFlagAsync(
                            entity.LUCKY_DRAW_ID,
                            entity.KOKAKU_HITO_PHONE,
                            mukoFlag: 1,
                            kosinId: entity.KOSIN_ID ?? entity.TOROKU_ID,
                            kosinNaiyou: "Xóa bằng giao diện"
                        );
                        return (success, message);
                    }
                    catch (Exception ex)
                    {
                        return (false, "Lỗi khi xóa kokaku: " + ex.Message);
                    }
                };

                // SAVE: POST khi tạo mới, PUT khi cập nhật
                _kokakuCrudManager.SaveFunc = async (entity) =>
                {
                    if (entity == null) return (false, "Dữ liệu không hợp lệ");

                    try
                    {
                        // Nếu user chọn file mới -> đọc file và gán base64
                        if (!string.IsNullOrEmpty(entity.TempImagePath) && File.Exists(entity.TempImagePath))
                        {
                            try
                            {
                                var bytes = File.ReadAllBytes(entity.TempImagePath);
                                entity.KOKAKU_HITO_IMG = Convert.ToBase64String(bytes);
                            }
                            catch (Exception ex)
                            {
                                return (false, "Lỗi khi đọc file ảnh: " + ex.Message);
                            }
                        }
                        else
                        {
                            // Nếu không có ảnh mới:
                            // - Tạo mới: để KOKAKU_HITO_IMG = null (không có ảnh)
                            // - Cập nhật: gán null để backend hiểu "không thay đổi ảnh" theo hợp đồng UI/BE
                            if (!entity.IsNewRow())
                                entity.KOKAKU_HITO_IMG = null;
                        }

                        if (entity.IsNewRow())
                        {
                            // POST create
                            // ensure TOROKU_ID đã được set trên DTO trước khi gọi
                            return await LuckyDrawService.SaveKokakuAsync(entity);
                        }
                        else
                        {
                            // PUT update
                            if (string.IsNullOrWhiteSpace(entity.KOSIN_ID))
                                entity.KOSIN_ID = entity.TOROKU_ID; // audit id

                            return await LuckyDrawService.UpdateKokakuAsync(entity.LUCKY_DRAW_ID, entity.KOKAKU_HITO_PHONE, entity);
                        }
                    }
                    catch (Exception ex)
                    {
                        return (false, "Lỗi khi lưu kokaku: " + ex.Message);
                    }
                };
            }
        }

        #endregion

        #region EVENT GRID SETUP & OPERATIONS

        private void SetupEventGrid()
        {
            // ✅ BẮT BUỘC: Clear và set ShowColumnHeaders NGAY ĐẦU TIÊN
            gvEvents.Columns.Clear();
            gvEvents.OptionsView.ShowColumnHeaders = true; // ← QUAN TRỌNG NHẤT!

            // NOTE: Set Editable = true but restrict per-row in ShowingEditor
            gvEvents.OptionsBehavior.Editable = true;
            gvEvents.OptionsView.ShowGroupPanel = false;
            gvEvents.OptionsView.ShowIndicator = true;
            gvEvents.OptionsView.ColumnAutoWidth = false;

            gvEvents.OptionsFind.AlwaysVisible = false;
            gvEvents.OptionsView.ShowAutoFilterRow = true;

            gvEvents.RowHeight = 22;

            // Subscribe ShowingEditor to allow only one row edit at a time
            gvEvents.ShowingEditor -= GvEvents_ShowingEditor;
            gvEvents.ShowingEditor += GvEvents_ShowingEditor;

            //// Cột STT
            //var colStt = new GridColumn
            //{
            //    Caption = "STT",
            //    FieldName = "STT",
            //    Visible = true,
            //    UnboundType = DevExpress.Data.UnboundColumnType.Integer,
            //    Width = 50,
            //    OptionsColumn = { AllowEdit = false }
            //};
            //gvEvents.Columns.Add(colStt);

            // Mã sự kiện
            var colEventId = new GridColumn
            {
                FieldName = "LUCKY_DRAW_ID",
                Caption = "Mã LuckyDraw",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = true } // allow for Add; block editing existing row in ShowingEditor
            };
            gvEvents.Columns.Add(colEventId);

            // Repository cho hình ảnh preview
            var pictureEditEvent = new RepositoryItemPictureEdit
            {
                SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom,
                NullText = " ",
                ShowMenu = false,
                CustomHeight = 30
            };
            gcEvents.RepositoryItems.Add(pictureEditEvent);

            var colEventImg = new GridColumn
            {
                FieldName = "EventImage",
                Caption = "Hình ảnh",
                Visible = true,
                Width = 60,
                UnboundType = DevExpress.Data.UnboundColumnType.Object,
                ColumnEdit = pictureEditEvent,
                OptionsColumn = { AllowEdit = false }
            };
            gvEvents.Columns.Add(colEventImg);

            // Tên sự kiện
            var colEventName = new GridColumn
            {
                FieldName = "LUCKY_DRAW_NAME",
                Caption = "Tên LuckyDraw",
                Visible = true,
                Width = 180,
                OptionsColumn = { AllowEdit = true }
            };
            gvEvents.Columns.Add(colEventName);

            // Phân loại
            var colBunrui = new GridColumn
            {
                FieldName = "LUCKY_DRAW_BUNRUI",
                Caption = "Phân loại",
                Visible = true,
                Width = 160,
                OptionsColumn = { AllowEdit = true } // allow for Add; block editing existing row in ShowingEditor
            };

            // Tạo repository item dạng GridLookUp (hiển thị grid nhỏ khi mở)
            var repoBunrui = new RepositoryItemGridLookUpEdit
            {
                NullText = "",
                TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard, // cho phép gõ tìm kiếm
                PopupFormSize = new System.Drawing.Size(260, 200)
            };

            // Configure inner view của GridLookUp để chỉ hiện tên (Name) và có hàng tìm kiếm
            var repoView = repoBunrui.View;
            repoView.OptionsView.ShowIndicator = false;
            repoView.OptionsView.ShowColumnHeaders = true;
            repoView.OptionsView.ShowAutoFilterRow = true;
            repoView.Columns.Clear();
            repoView.Columns.AddVisible(nameof(BunruiOption.Name), "Tên phân loại");

            // Hiển thị Name cho người dùng, lưu Code vào field LUCKY_DRAW_BUNRUI
            repoBunrui.DisplayMember = nameof(BunruiOption.Name);
            repoBunrui.ValueMember = nameof(BunruiOption.Code);

            // Gán datasource (ở đây dùng danh sách mẫu _bunruiOptions)
            repoBunrui.DataSource = _bunruiOptions;

            // Thêm vào repository của grid và set làm ColumnEdit
            gcEvents.RepositoryItems.Add(repoBunrui);
            colBunrui.ColumnEdit = repoBunrui;

            // Thêm cột vào GridView
            gvEvents.Columns.Add(colBunrui);

            // Tiêu đề
            var colTitle = new GridColumn
            {
                FieldName = "LUCKY_DRAW_TITLE",
                Caption = "Tiêu đề",
                Visible = true,
                Width = 150,
                OptionsColumn = { AllowEdit = true }
            };
            gvEvents.Columns.Add(colTitle);

            // Slogan
            var colSlogan = new GridColumn
            {
                FieldName = "LUCKY_DRAW_SLOGAN",
                Caption = "Slogan",
                Visible = true,
                Width = 180,
                OptionsColumn = { AllowEdit = true }
            };
            gvEvents.Columns.Add(colSlogan);

            // Số ô vòng quay
            var colSlotNum = new GridColumn
            {
                FieldName = "LUCKY_DRAW_SLOT_NUM",
                Caption = "Số ô vòng quay",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = true }
            };
            // Thêm repository để chỉ cho phép nhập số từ 1-20
            var spinEditSlotNum = new RepositoryItemSpinEdit
            {
                MinValue = 1,
                MaxValue = 50,
                IsFloatValue = false,
                AllowNullInput = DevExpress.Utils.DefaultBoolean.False
            };
            gcEvents.RepositoryItems.Add(spinEditSlotNum);
            colSlotNum.ColumnEdit = spinEditSlotNum;
            gvEvents.Columns.Add(colSlotNum);

            // Ngày tạo
            var colTorokuDate = new GridColumn
            {
                FieldName = "TOROKU_DATE",
                Caption = "Ngày tạo",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colTorokuDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colTorokuDate.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            gvEvents.Columns.Add(colTorokuDate);

            // Người tạo
            var colTorokuId = new GridColumn
            {
                FieldName = "TOROKU_ID",
                Caption = "Người tạo",
                Visible = true,
                Width = 100,
                OptionsColumn = { AllowEdit = false }
            };
            gvEvents.Columns.Add(colTorokuId);

            // ✅ THÊM MỚI: Người sửa cuối
            var colKosinId = new GridColumn
            {
                FieldName = "KOSIN_ID",
                Caption = "Người sửa",
                Visible = true,
                Width = 100,
                OptionsColumn = { AllowEdit = false }
            };
            gvEvents.Columns.Add(colKosinId);

            // ✅ THÊM MỚI: Ngày sửa cuối
            var colKosinDate = new GridColumn
            {
                FieldName = "KOSIN_DATE",
                Caption = "Ngày sửa",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colKosinDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colKosinDate.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            gvEvents.Columns.Add(colKosinDate);

            // ✅ THÊM MỚI: Nội dung sửa (ẩn mặc định)
            var colKosinNaiyou = new GridColumn
            {
                FieldName = "KOSIN_NAIYOU",
                Caption = "Nội dung sửa",
                Visible = false,  // Ẩn mặc định, có thể show qua Column Chooser
                Width = 200,
                OptionsColumn = { AllowEdit = false }
            };
            gvEvents.Columns.Add(colKosinNaiyou);

            // ✅ CUỐI CÙNG: Đảm bảo GridView được apply đúng
            gcEvents.MainView = gvEvents;
            gcEvents.ForceInitialize();
            gcEvents.DataSourceChanged -= (s, e) => { ClearEventImageCache(); };
            gcEvents.DataSourceChanged += (s, e) => { ClearEventImageCache(); };
            gvEvents.OptionsView.ShowColumnHeaders = true; // Set lại lần nữa sau ForceInitialize
            gvEvents.BestFitColumns();
            gvMeisai.RowStyle -= GvMeisai_RowStyle;
            gvMeisai.RowStyle += GvMeisai_RowStyle;

            // Events
            gvEvents.CustomUnboundColumnData += GvEvents_CustomUnboundColumnData;
            gvEvents.FocusedRowChanged += GvEvents_FocusedRowChanged;
            gvEvents.DoubleClick += GvEvents_DoubleClick;
        }

        private async Task EnsureEventImageCachedAsync(LuckyDrawDTO row, string cacheKey, int width = 35, int height = 35)
        {
            if (row == null || string.IsNullOrEmpty(row.LUCKY_DRAW_BG_IMG)) return;

            // Avoid duplicate work
            lock (_eventImageCache)
            {
                if (_eventImageCache.ContainsKey(cacheKey)) return;
            }

            try
            {
                // Decode + resize off UI thread
                var bmp = await Task.Run(() =>
                {
                    try
                    {
                        var image = Base64ToImage(row.LUCKY_DRAW_BG_IMG);
                        if (image == null) return GetDefaultImage(width, height);
                        try
                        {
                            var thumb = ResizeImage(image, width, height);
                            image.Dispose();
                            return thumb;
                        }
                        catch
                        {
                            image.Dispose();
                            return GetDefaultImage(width, height);
                        }
                    }
                    catch
                    {
                        return GetDefaultImage(width, height);
                    }
                });

                lock (_eventImageCache)
                {
                    // If another thread already set it, dispose the new bmp
                    if (_eventImageCache.ContainsKey(cacheKey))
                    {
                        try { bmp.Dispose(); }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        _eventImageCache[cacheKey] = bmp;
                    }
                }

                // Refresh the specific row on UI thread
                if (!this.IsDisposed && this.Created)
                {
                    this.Invoke((Action)(() =>
                    {
                        try
                        {
                            var handle = gvEvents.LocateByValue("LUCKY_DRAW_ID", row.LUCKY_DRAW_ID);
                            if (handle >= 0) gvEvents.RefreshRow(handle);
                            else gvEvents.RefreshData();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                        }
                    }));
                }
            }
            catch
            {
                // swallow
            }
        }

        // Modified CustomUnboundColumnData for EventImage: non-blocking, triggers background load
        private void GvEvents_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "STT" && e.IsGetData)
            {
                e.Value = e.ListSourceRowIndex + 1;
                return;
            }

            if (e.Column.FieldName != "EventImage" || !e.IsGetData)
                return;

            var row = gvEvents.GetRow(e.ListSourceRowIndex) as LuckyDrawDTO;
            if (row == null)
            {
                e.Value = GetDefaultImage(35, 35);
                return;
            }

            var keyBase = !string.IsNullOrEmpty(row.LUCKY_DRAW_ID) ? row.LUCKY_DRAW_ID : e.ListSourceRowIndex.ToString();

            // Temp preview first
            if (!string.IsNullOrEmpty(row.TempImagePath) && File.Exists(row.TempImagePath))
            {
                var tmpKey = keyBase + "_tmp:" + row.TempImagePath;
                if (!_eventImageCache.TryGetValue(tmpKey, out var tmpImg))
                {
                    try
                    {
                        using (var img = Image.FromFile(row.TempImagePath))
                        {
                            tmpImg = ResizeImage(img, 35, 35);
                        }
                    }
                    catch
                    {
                        tmpImg = GetDefaultImage(35, 35);
                    }
                    _eventImageCache[tmpKey] = tmpImg;
                }
                e.Value = _eventImageCache[tmpKey];
                return;
            }

            // If DTO contains already-cached image (from previous load)
            if (_eventImageCache.TryGetValue(keyBase, out var cachedImage))
            {
                e.Value = cachedImage;
                return;
            }

            // If DTO has base64 string present, schedule async decode + cache
            if (!string.IsNullOrEmpty(row.LUCKY_DRAW_BG_IMG))
            {
                // Return placeholder immediately and trigger background decode if not already cached
                e.Value = GetDefaultImage(35, 35);

                // Fire-and-forget: ensure single background worker per key by checking _eventImageFetchInProgress
                var fetchKey = $"evt_decode:{keyBase}";
                lock (_eventImageFetchInProgress)
                {
                    if (!_eventImageFetchInProgress.Contains(fetchKey))
                    {
                        _eventImageFetchInProgress.Add(fetchKey);
                        // Start background decode task
                        Task.Run(async () =>
                        {
                            try
                            {
                                await EnsureEventImageCachedAsync(row, keyBase, 35, 35);
                            }
                            finally
                            {
                                lock (_eventImageFetchInProgress) { _eventImageFetchInProgress.Remove(fetchKey); }
                            }
                        });
                    }
                }
                return;
            }

            // No base64 -> trigger one-time background fetch for this event (unchanged behavior)
            if (!string.IsNullOrEmpty(row.LUCKY_DRAW_ID))
            {
                var fetchKey = $"evt:{row.LUCKY_DRAW_ID}";
                lock (_eventImageFetchInProgress)
                {
                    if (!_eventImageFetchInProgress.Contains(fetchKey))
                    {
                        _eventImageFetchInProgress.Add(fetchKey);
                        Task.Run(async () =>
                        {
                            try
                            {
                                var fetched = await LuckyDrawService.GetEventByIdAsync(row.LUCKY_DRAW_ID);
                                if (fetched != null && !string.IsNullOrWhiteSpace(fetched.LUCKY_DRAW_BG_IMG))
                                {
                                    // Update DTO with base64 (so next render will schedule decode)
                                    this.Invoke((Action)(() =>
                                    {
                                        try
                                        {
                                            var listObj = gcEvents.DataSource as IEnumerable<LuckyDrawDTO>;
                                            if (listObj != null)
                                            {
                                                foreach (var item in listObj.Where(x => x != null && x.LUCKY_DRAW_ID == fetched.LUCKY_DRAW_ID))
                                                {
                                                    item.LUCKY_DRAW_BG_IMG = fetched.LUCKY_DRAW_BG_IMG;
                                                }
                                            }
                                            var handle = gvEvents.LocateByValue("LUCKY_DRAW_ID", fetched.LUCKY_DRAW_ID);
                                            if (handle >= 0) gvEvents.RefreshRow(handle);
                                            else gvEvents.RefreshData();
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                        }
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                            }
                            finally
                            {
                                lock (_eventImageFetchInProgress) { _eventImageFetchInProgress.Remove(fetchKey); }
                            }
                        });
                    }
                }
            }

            e.Value = GetDefaultImage(35, 35);
        }

        private async void GvEvents_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            try
            {
                var selectedEvent = gvEvents.GetFocusedRow() as LuckyDrawDTO;
                if (selectedEvent == null) return;

                // Nếu đang edit event thì không tự động load
                if (_eventCrudManager != null && _eventCrudManager.IsEditing)
                    return;

                var selectedId = selectedEvent.LUCKY_DRAW_ID;
                if (string.IsNullOrEmpty(selectedId)) return;

                var bunrui = (selectedEvent.LUCKY_DRAW_BUNRUI ?? string.Empty).Trim();


                // Set ngay selected id để LoadDataFunc dùng đúng giá trị
                _selectedEventId = selectedId;

                // Luôn tải Meisai cho event được chọn (để đảm bảo dữ liệu Meisai luôn đồng bộ)
                if (_meisaiCrudManager != null && _lastLoadedEventIdMeisai != selectedId)
                {
                    await _meisaiCrudManager.LoadDataAsync();
                    _lastLoadedEventIdMeisai = selectedId;
                }

                // Nếu bunrui == "B" thì vẫn load Kokaku (không thay đổi behavior hiện tại)
                if (bunrui == "B" && _kokakuCrudManager != null && _lastLoadedEventIdKokaku != selectedId)
                {
                    await _kokakuCrudManager.LoadDataAsync();
                    _lastLoadedEventIdKokaku = selectedId;
                }
            }
            catch 
            {
            }
        }

        private void GvEvents_DoubleClick(object sender, EventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            if (view == null) return;

            var hitInfo = view.CalcHitInfo(view.GridControl.PointToClient(Control.MousePosition));

            // Kiểm tra có click vào cột "Hình ảnh" không
            if (hitInfo.InRowCell && hitInfo.Column != null && hitInfo.Column.FieldName == "EventImage")
            {
                // Require edit mode (either CrudManager reports editing or we hold an editing row handle)
                if ((_eventCrudManager == null) || (!_eventCrudManager.IsEditing && !_editingRowHandleEvent.HasValue))
                    return; // not in edit mode -> ignore double click

                var currentRow = view.GetRow(hitInfo.RowHandle) as LuckyDrawDTO;
                if (currentRow == null) return;

                // Mở hộp thoại chọn file
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Chọn ảnh nền sự kiện";
                    ofd.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(ofd.FileName);

                            // Validate size (max 50MB)
                            if (fileInfo.Length > 50 * 1024 * 1024)
                            {
                                XtraMessageBox.Show(
                                    "Kích thước file vượt quá 50MB. Vui lòng chọn file nhỏ hơn.",
                                    "Cảnh báo",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return;
                            }

                            // Lưu path vào DTO
                            currentRow.TempImagePath = ofd.FileName;

                            // Refresh grid để hiển thị preview
                            view.RefreshData();
                        }
                        catch (Exception ex)
                        {
                            XtraMessageBox.Show(
                                $"Lỗi khi chọn ảnh: {ex.Message}",
                                "Lỗi",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
        #endregion

        #region MEISAI GRID SETUP & OPERATIONS

        private void SetupMeisaiGrid()
        {
            // ✅ BẮT BUỘC: Clear và set ShowColumnHeaders NGAY ĐẦU TIÊN
            gvMeisai.Columns.Clear();
            gvMeisai.OptionsView.ShowColumnHeaders = true; // ← QUAN TRỌNG NHẤT!

            // NOTE: Editable true but restrict per-row via ShowingEditor
            gvMeisai.OptionsBehavior.Editable = true;
            gvMeisai.OptionsView.ShowGroupPanel = false;
            gvMeisai.OptionsView.ShowIndicator = true;
            gvMeisai.OptionsView.ColumnAutoWidth = false;

            gvMeisai.OptionsFind.AlwaysVisible = false;
            gvMeisai.OptionsView.ShowAutoFilterRow = true;

            gvMeisai.RowHeight = 22;

            // Subscribe ShowingEditor to allow only one row edit at a time
            gvMeisai.ShowingEditor -= GvMeisai_ShowingEditor;
            gvMeisai.ShowingEditor += GvMeisai_ShowingEditor;

            //// Cột STT
            //var colStt = new GridColumn
            //{
            //    Caption = "STT",
            //    FieldName = "STT",
            //    Visible = true,
            //    UnboundType = DevExpress.Data.UnboundColumnType.Integer,
            //    Width = 50,
            //    OptionsColumn = { AllowEdit = false }
            //};
            //gvMeisai.Columns.Add(colStt);

            // Thêm cột LUCKY_DRAW_ID (hiển thị mã event cho mỗi meisai) - không cho sửa trên grid
            var colLuckyDrawId = new GridColumn
            {
                FieldName = "LUCKY_DRAW_ID",
                Caption = "Mã LuckyDraw",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = false }
            };
            gvMeisai.Columns.Add(colLuckyDrawId);

            // Mã giải
            var colMeisaiNo = new GridColumn
            {
                FieldName = "LUCKY_DRAW_MEISAI_NO",
                Caption = "STT giải",
                Visible = true,
                Width = 80,
                OptionsColumn = { AllowEdit = true }
            };
            gvMeisai.Columns.Add(colMeisaiNo);

            // Repository cho hình ảnh preview
            var pictureEditMeisai = new RepositoryItemPictureEdit
            {
                SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom,
                NullText = " ",
                ShowMenu = false,
                CustomHeight = 30
            };
            gcMeisai.RepositoryItems.Add(pictureEditMeisai);

            var colMeisaiImg = new GridColumn
            {
                FieldName = "MeisaiImage",
                Caption = "Hình ảnh",
                Visible = true,
                Width = 60,
                UnboundType = DevExpress.Data.UnboundColumnType.Object,
                ColumnEdit = pictureEditMeisai,
                OptionsColumn = { AllowEdit = false }
            };
            gvMeisai.Columns.Add(colMeisaiImg);

            // Tên giải thưởng
            var colMeisaiName = new GridColumn
            {
                FieldName = "LUCKY_DRAW_MEISAI_NAME",
                Caption = "Tên giải thưởng",
                Visible = true,
                Width = 180,
                OptionsColumn = { AllowEdit = true }
            };
            gvMeisai.Columns.Add(colMeisaiName);

            // Loại
            var colBunrui = new GridColumn
            {
                FieldName = "LUCKY_DRAW_MEISAI_BUNRUI",
                Caption = "Loại",
                Visible = true,
                Width = 100,
                OptionsColumn = { AllowEdit = true }
            };
            gvMeisai.Columns.Add(colBunrui);

            // Repository spin edit cho ONSHOU_NUM (nullable int)
            var repoOnshouSpin = new RepositoryItemSpinEdit
            {
                IsFloatValue = false,
                MinValue = 0,
                MaxValue = 1000000,
                AllowNullInput = DevExpress.Utils.DefaultBoolean.True
            };
            // show empty when value is null
            repoOnshouSpin.NullText = "";
            gcMeisai.RepositoryItems.Add(repoOnshouSpin);

            // Cột thứ tự / onshou num
            var colOnshouNum = new GridColumn
            {
                FieldName = "LUCKY_DRAW_MEISAI_ONSHOU_NUM",
                Caption = "Thứ tự giải thưởng",
                Visible = true,
                Width = 80,
                OptionsColumn = { AllowEdit = true }
            };
            colOnshouNum.ColumnEdit = repoOnshouSpin;
            colOnshouNum.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colOnshouNum.DisplayFormat.FormatString = "N0"; // hiển thị không dấu thập phân
            colOnshouNum.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far; // căn phải
                                                                                                     // (removed invalid colOnshouNum.NullText assignment)
            gvMeisai.Columns.Add(colOnshouNum);

            // Tỷ lệ %
            var colRate = new GridColumn
            {
                FieldName = "LUCKY_DRAW_MEISAI_RATE",
                Caption = "Tỷ lệ (%)",
                Visible = true,
                Width = 80,
                OptionsColumn = { AllowEdit = true }
            };
            gvMeisai.Columns.Add(colRate);

            // Số lượng
            var colSuryo = new GridColumn
            {
                FieldName = "LUCKY_DRAW_MEISAI_SURYO",
                Caption = "Số lượng",
                Visible = true,
                Width = 80,
                OptionsColumn = { AllowEdit = true }
            };
            gvMeisai.Columns.Add(colSuryo);

            // Thêm cột MUKO_FLG (int) để hiển thị trạng thái vô hiệu hóa (soft-delete)
            var colMuko = new GridColumn
            {
                FieldName = "LUCKY_DRAW_MEISAI_MUKO_FLG",
                Caption = "MukoFlag",
                Visible = true,
                Width = 80,
                OptionsColumn = { AllowEdit = true } // cho phép sửa bằng checkbox
            };

            // RepositoryItemCheckEdit: map giá trị 1 <-> Checked, 0/null <-> Unchecked
            var repoMukoCheck = new RepositoryItemCheckEdit
            {
                ValueChecked = 1,
                ValueUnchecked = 0,
                NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked
            };
            gcMeisai.RepositoryItems.Add(repoMukoCheck);
            colMuko.ColumnEdit = repoMukoCheck;

            gvMeisai.Columns.Add(colMuko);

            // Mô tả
            var colNaiyou = new GridColumn
            {
                FieldName = "LUCKY_DRAW_MEISAI_NAIYOU",
                Caption = "Mô tả",
                Visible = true,
                Width = 180,
                OptionsColumn = { AllowEdit = true }
            };
            gvMeisai.Columns.Add(colNaiyou);

            // Ngày bắt đầu
            var colKaishiDate = new GridColumn
            {
                FieldName = "KAISHI_DATE",
                Caption = "Ngày bắt đầu",
                Visible = true,
                Width = 110,
                OptionsColumn = { AllowEdit = true }
            };
            colKaishiDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colKaishiDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            gvMeisai.Columns.Add(colKaishiDate);

            // Ngày kết thúc
            var colSyuryouDate = new GridColumn
            {
                FieldName = "SYURYOU_DATE",
                Caption = "Ngày kết thúc",
                Visible = true,
                Width = 110,
                OptionsColumn = { AllowEdit = true }
            };
            colSyuryouDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colSyuryouDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            gvMeisai.Columns.Add(colSyuryouDate);
             
            var colTorokuId = new GridColumn
            {
                FieldName = "TOROKU_ID",
                Caption = "Người tạo",
                Visible = true,
                Width = 100,
                OptionsColumn = { AllowEdit = false }
            };
            gvMeisai.Columns.Add(colTorokuId);

            // ✅ THÊM: Ngày tạo
            var colTorokuDate = new GridColumn
            {
                FieldName = "TOROKU_DATE",
                Caption = "Ngày tạo",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colTorokuDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colTorokuDate.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            gvMeisai.Columns.Add(colTorokuDate);

            // ✅ THÊM MỚI: Người sửa cuối
            var colKosinId = new GridColumn
            {
                FieldName = "KOSIN_ID",
                Caption = "Người sửa",
                Visible = true,
                Width = 100,
                OptionsColumn = { AllowEdit = false }
            };
            gvMeisai.Columns.Add(colKosinId);

            // ✅ THÊM MỚI: Ngày sửa cuối
            var colKosinDate = new GridColumn
            {
                FieldName = "KOSIN_DATE",
                Caption = "Ngày sửa",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colKosinDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colKosinDate.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            gvMeisai.Columns.Add(colKosinDate);

            // ✅ THÊM MỚI: Nội dung sửa (ẩn mặc định)
            var colKosinNaiyou = new GridColumn
            {
                FieldName = "KOSIN_NAIYOU",
                Caption = "Nội dung sửa",
                Visible = false,
                Width = 200,
                OptionsColumn = { AllowEdit = false }
            };
            gvMeisai.Columns.Add(colKosinNaiyou);


            // ✅ CUỐI CÙNG: Đảm bảo GridView được apply đúng
            gcMeisai.MainView = gvMeisai;
            gcMeisai.ForceInitialize();
            gcMeisai.DataSourceChanged -= (s, e) => { ClearMeisaiImageCache(); };
            gcMeisai.DataSourceChanged += (s, e) => { ClearMeisaiImageCache(); };
            gvMeisai.OptionsView.ShowColumnHeaders = true; // Set lại lần nữa sau ForceInitialize
            gvMeisai.BestFitColumns();

            // Events
            gvMeisai.CustomUnboundColumnData += GvMeisai_CustomUnboundColumnData;
            gvMeisai.DoubleClick += GvMeisai_DoubleClick;

            gvMeisai.RowCellStyle -= GvMeisai_RowCellStyle;
            gvMeisai.RowCellStyle += GvMeisai_RowCellStyle;

        }

        private void GvMeisai_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "STT" && e.IsGetData)
            {
                e.Value = e.ListSourceRowIndex + 1;
                return;
            }

            if (e.Column.FieldName != "MeisaiImage" || !e.IsGetData)
                return;

            var row = gvMeisai.GetRow(e.ListSourceRowIndex) as LuckyDrawMeisaiDTO;
            if (row == null)
            {
                e.Value = GetDefaultImage(35, 35);
                return;
            }

            var keyBase = (!string.IsNullOrEmpty(row.LUCKY_DRAW_ID) ? row.LUCKY_DRAW_ID : "evt") +
                          "_" + (!string.IsNullOrEmpty(row.LUCKY_DRAW_MEISAI_NO) ? row.LUCKY_DRAW_MEISAI_NO : e.ListSourceRowIndex.ToString());

            // Temp preview
            if (!string.IsNullOrEmpty(row.TempImagePath) && File.Exists(row.TempImagePath))
            {
                var tmpKey = keyBase + "_tmp:" + row.TempImagePath;
                if (!_meisaiImageCache.TryGetValue(tmpKey, out var tmpImg))
                {
                    try
                    {
                        using (var img = Image.FromFile(row.TempImagePath))
                        {
                            tmpImg = ResizeImage(img, 35, 35);
                        }
                    }
                    catch
                    {
                        tmpImg = GetDefaultImage(35, 35);
                    }
                    _meisaiImageCache[tmpKey] = tmpImg;
                }
                e.Value = _meisaiImageCache[tmpKey];
                return;
            }

            // If DTO has base64 -> use it
            if (!string.IsNullOrEmpty(row.LUCKY_DRAW_MEISAI_IMG))
            {
                if (!_meisaiImageCache.TryGetValue(keyBase, out var cached))
                {
                    try
                    {
                        var image = Base64ToImage(row.LUCKY_DRAW_MEISAI_IMG);
                        cached = image != null ? ResizeImage(image, 35, 35) : GetDefaultImage(35, 35);
                    }
                    catch
                    {
                        cached = GetDefaultImage(35, 35);
                    }
                    _meisaiImageCache[keyBase] = cached;
                }
                e.Value = _meisaiImageCache[keyBase];
                return;
            }

            // No image -> trigger one-time fetch for this event's meisai images
            if (!string.IsNullOrEmpty(row.LUCKY_DRAW_ID))
            {
                var fetchKey = $"ms:{row.LUCKY_DRAW_ID}";
                lock (_meisaiImageFetchInProgress)
                {
                    if (!_meisaiImageFetchInProgress.Contains(fetchKey))
                    {
                        _meisaiImageFetchInProgress.Add(fetchKey);
                        Task.Run(async () =>
                        {
                            try
                            {
                                var fetched = await LuckyDrawService.GetAllMeisaiAsync(row.LUCKY_DRAW_ID, category: null, includeMuko: true);
                                if (fetched != null && fetched.Count > 0)
                                {
                                    this.Invoke((Action)(() =>
                                    {
                                        try
                                        {
                                            var ds = gcMeisai.DataSource as IEnumerable<LuckyDrawMeisaiDTO>;
                                            if (ds != null)
                                            {
                                                foreach (var f in fetched)
                                                {
                                                    var match = ds.FirstOrDefault(x => x != null && x.LUCKY_DRAW_MEISAI_NO == f.LUCKY_DRAW_MEISAI_NO && x.LUCKY_DRAW_ID == f.LUCKY_DRAW_ID);
                                                    if (match != null)
                                                    {
                                                        match.LUCKY_DRAW_MEISAI_IMG = f.LUCKY_DRAW_MEISAI_IMG;
                                                    }
                                                }
                                            }
                                            gvMeisai.RefreshData();
                                        }
                                        catch  {  }
                                    }));
                                }
                            }
                            catch 
                            {
                                
                            }
                            finally
                            {
                                lock (_meisaiImageFetchInProgress) { _meisaiImageFetchInProgress.Remove(fetchKey); }
                            }
                        });
                    }
                }
            }

            e.Value = GetDefaultImage(35, 35);
        }
        // Add clear-cache helpers (put near IMAGE HELPER region)
        private void ClearEventImageCache()

        {
            foreach (var img in _eventImageCache.Values) img.Dispose();
            _eventImageCache.Clear();
        }

        private void ClearMeisaiImageCache()

        {
            foreach (var img in _meisaiImageCache.Values) img.Dispose();
            _meisaiImageCache.Clear();
        }

        private void GvMeisai_DoubleClick(object sender, EventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            if (view == null) return;

            var hitInfo = view.CalcHitInfo(view.GridControl.PointToClient(Control.MousePosition));

            if (hitInfo.InRowCell && hitInfo.Column != null && hitInfo.Column.FieldName == "MeisaiImage")
            {
                // Require edit mode for meisai
                if ((_meisaiCrudManager == null) || (!_meisaiCrudManager.IsEditing && !_editingRowHandleMeisai.HasValue))
                    return; // not in edit mode -> ignore double click

                var currentRow = view.GetRow(hitInfo.RowHandle) as LuckyDrawMeisaiDTO;
                if (currentRow == null) return;

                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Chọn ảnh giải thưởng";
                    ofd.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(ofd.FileName);

                            if (fileInfo.Length > 5 * 1024 * 1024)
                            {
                                XtraMessageBox.Show(
                                    "Kích thước file vượt quá 5MB.",
                                    "Cảnh báo",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return;
                            }

                            currentRow.TempImagePath = ofd.FileName;

                            gvMeisai.RefreshData();
                        }
                        catch (Exception ex)
                        {
                            XtraMessageBox.Show(
                                $"Lỗi: {ex.Message}",
                                "Lỗi",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void GvMeisai_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            try
            {
                if (e.RowHandle < 0) return;
                var row = gvMeisai.GetRow(e.RowHandle) as LuckyDrawMeisaiDTO;
                if (row == null) return;

                // nếu MUKO flag bật -> mờ như hiện tại
                if (row.LUCKY_DRAW_MEISAI_MUKO_FLG.HasValue && row.LUCKY_DRAW_MEISAI_MUKO_FLG.Value == 1)
                {
                    e.Appearance.ForeColor = Color.Gray;
                    e.Appearance.BackColor = Color.FromArgb(230, 230, 230);
                    e.Appearance.Font = new Font(e.Appearance.Font ?? this.Font, FontStyle.Italic);
                    e.HighPriority = true;
                    return;
                }

                // Nếu SYURYOU_DATE < hôm nay (đã hết hạn) -> mờ/ẩn giống MUKO
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
                    catch { /* ignore date parse errors */ }
                }

                // Nếu Số lượng <= 0 -> hiển thị mờ giống MUKO/expired
                if (row.LUCKY_DRAW_MEISAI_SURYO.HasValue)
                {
                    try
                    {
                        // treat zero or negative as "no stock"
                        if (row.LUCKY_DRAW_MEISAI_SURYO.Value <= 0.0)
                        {
                            e.Appearance.ForeColor = Color.Gray;
                            e.Appearance.BackColor = Color.FromArgb(230, 230, 230);
                            e.Appearance.Font = new Font(e.Appearance.Font ?? this.Font, FontStyle.Italic);
                            e.HighPriority = true;
                            return;
                        }
                    }
                    catch { /* ignore numeric errors */ }
                }
            }
            catch { /* ignore styling errors */ }
        }

        private void GvMeisai_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            try
            {
                if (e.RowHandle < 0) return;
                var row = gvMeisai.GetRow(e.RowHandle) as LuckyDrawMeisaiDTO;
                if (row == null) return;

                // light red background for expired date cell
                if (e.Column != null && e.Column.FieldName == "SYURYOU_DATE")
                {
                    if (row.SYURYOU_DATE.HasValue && row.SYURYOU_DATE.Value.Date < DateTime.Now.Date)
                    {
                        e.Appearance.BackColor = Color.FromArgb(255, 230, 230); // light red
                        e.Appearance.ForeColor = Color.DarkRed;
                        return;
                    }
                }

                // light red for quantity cell when <= 0
                if (e.Column != null && e.Column.FieldName == "LUCKY_DRAW_MEISAI_SURYO")
                {
                    if (row.LUCKY_DRAW_MEISAI_SURYO.HasValue && row.LUCKY_DRAW_MEISAI_SURYO.Value <= 0.0)
                    {
                        e.Appearance.BackColor = Color.FromArgb(255, 230, 230);
                        e.Appearance.ForeColor = Color.DarkRed;
                     
                        return;
                    }
                }

                // (Optional) highlight MUKO flag cell when flagged
                if (e.Column != null && e.Column.FieldName == "LUCKY_DRAW_MEISAI_MUKO_FLG")
                {
                    if (row.LUCKY_DRAW_MEISAI_MUKO_FLG.HasValue && row.LUCKY_DRAW_MEISAI_MUKO_FLG.Value == 1)
                    {
                        e.Appearance.BackColor = Color.FromArgb(255, 230, 230);
                        e.Appearance.ForeColor = Color.DarkRed;
     
                        return;
                    }
                }
            }
            catch
            {
                // swallow styling errors
            }
        }

        #endregion

        #region KOKAKU GRID SETUP & OPERATIONS

        private void SetupKokakuGrid()
        {
            if (gcKokaku == null || gvKokaku == null) return;

            gvKokaku.Columns.Clear();
            gvKokaku.OptionsView.ShowColumnHeaders = true;

            gvKokaku.OptionsBehavior.Editable = true;
            gvKokaku.OptionsView.ShowGroupPanel = false;
            gvKokaku.OptionsView.ShowIndicator = true;
            gvKokaku.OptionsView.ColumnAutoWidth = false;
            gvKokaku.OptionsView.ShowAutoFilterRow = true;
            gvKokaku.RowHeight = 22;

            gvKokaku.ShowingEditor -= GvKokaku_ShowingEditor;
            gvKokaku.ShowingEditor += GvKokaku_ShowingEditor;

            // LUCKY_DRAW_ID
            var colLuckyDrawId = new GridColumn
            {
                FieldName = "LUCKY_DRAW_ID",
                Caption = "Mã LuckyDraw",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = false }
            };
            gvKokaku.Columns.Add(colLuckyDrawId);

            // KOKAKU_HITO_PHONE (PK)
            var colPhone = new GridColumn
            {
                FieldName = "KOKAKU_HITO_PHONE",
                Caption = "SĐT khách",
                Visible = true,
                Width = 120,
                OptionsColumn = { AllowEdit = true }
            };
            gvKokaku.Columns.Add(colPhone);

            // Tên người nhận
            var colName = new GridColumn
            {
                FieldName = "KOKAKU_HITO_NAME",
                Caption = "Tên khách",
                Visible = true,
                Width = 150,
                OptionsColumn = { AllowEdit = true }
            };
            gvKokaku.Columns.Add(colName);

            // Repository cho ảnh kokaku
            var pictureEditKokaku = new RepositoryItemPictureEdit
            {
                SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom,
                NullText = " ",
                ShowMenu = false,
                CustomHeight = 30
            };
            gcKokaku.RepositoryItems.Add(pictureEditKokaku);

            var colKokakuImg = new GridColumn
            {
                FieldName = "KokakuImage",
                Caption = "Hình ảnh",
                Visible = true,
                Width = 60,
                UnboundType = DevExpress.Data.UnboundColumnType.Object,
                ColumnEdit = pictureEditKokaku,
                OptionsColumn = { AllowEdit = false }
            };
            gvKokaku.Columns.Add(colKokakuImg);

            // Muko flag -> cho phép sửa bằng checkbox
            var colMuko = new GridColumn
            {
                FieldName = "KOKAKU_MUKO_FLG",
                Caption = "MukoFlag",
                Visible = true,
                Width = 80,
                OptionsColumn = { AllowEdit = true } // enable editing
            };
            // RepositoryItemCheckEdit cho kokaku
            var repoKokakuMukoCheck = new RepositoryItemCheckEdit
            {
                ValueChecked = 1,
                ValueUnchecked = 0,
                NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked
            };
            gcKokaku.RepositoryItems.Add(repoKokakuMukoCheck);
            colMuko.ColumnEdit = repoKokakuMukoCheck;

            gvKokaku.Columns.Add(colMuko);

            // TOROKU_ID
            var colTorokuId = new GridColumn
            {
                FieldName = "TOROKU_ID",
                Caption = "Người tạo",
                Visible = true,
                Width = 100,
                OptionsColumn = { AllowEdit = false }
            };
            gvKokaku.Columns.Add(colTorokuId);

            // TOROKU_DATE
            var colTorokuDate = new GridColumn
            {
                FieldName = "TOROKU_DATE",
                Caption = "Ngày tạo",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colTorokuDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colTorokuDate.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            gvKokaku.Columns.Add(colTorokuDate);

            // KOSIN_ID
            var colKosinId = new GridColumn
            {
                FieldName = "KOSIN_ID",
                Caption = "Người sửa",
                Visible = true,
                Width = 100,
                OptionsColumn = { AllowEdit = false }
            };
            gvKokaku.Columns.Add(colKosinId);

            // KOSIN_DATE
            var colKosinDate = new GridColumn
            {
                FieldName = "KOSIN_DATE",
                Caption = "Ngày sửa",
                Visible = true,
                Width = 140,
                OptionsColumn = { AllowEdit = false }
            };
            colKosinDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colKosinDate.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            gvKokaku.Columns.Add(colKosinDate);

            // KOSIN_NAIYOU hidden
            var colKosinNaiyou = new GridColumn
            {
                FieldName = "KOSIN_NAIYOU",
                Caption = "Nội dung sửa",
                Visible = false,
                Width = 200,
                OptionsColumn = { AllowEdit = false }
            };
            gvKokaku.Columns.Add(colKosinNaiyou);

            // Dates
            var colKaishiDate = new GridColumn
            {
                FieldName = "KAISHI_DATE",
                Caption = "Ngày bắt đầu",
                Visible = true,
                Width = 110,
                OptionsColumn = { AllowEdit = true }
            };
            colKaishiDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colKaishiDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            gvKokaku.Columns.Add(colKaishiDate);

            var colSyuryouDate = new GridColumn
            {
                FieldName = "SYURYOU_DATE",
                Caption = "Ngày kết thúc",
                Visible = true,
                Width = 110,
                OptionsColumn = { AllowEdit = true }
            };
            colSyuryouDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colSyuryouDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            gvKokaku.Columns.Add(colSyuryouDate);

            // Apply main view
            gcKokaku.MainView = gvKokaku;
            gcKokaku.ForceInitialize();
            gcKokaku.DataSourceChanged -= (s, e) => { ClearKokakuImageCache(); };
            gcKokaku.DataSourceChanged += (s, e) => { ClearKokakuImageCache(); };
            gvKokaku.OptionsView.ShowColumnHeaders = true;
            gvKokaku.BestFitColumns();
            gvKokaku.RowStyle -= GvKokaku_RowStyle;
            gvKokaku.RowStyle += GvKokaku_RowStyle;
            // Events
            gvKokaku.CustomUnboundColumnData += GvKokaku_CustomUnboundColumnData;
            gvKokaku.DoubleClick += GvKokaku_DoubleClick;

            gvKokaku.RowCellStyle -= GvKokaku_RowCellStyle;
            gvKokaku.RowCellStyle += GvKokaku_RowCellStyle;
        }

        private void GvKokaku_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "STT" && e.IsGetData)
            {
                e.Value = e.ListSourceRowIndex + 1;
                return;
            }

            if (e.Column.FieldName != "KokakuImage" || !e.IsGetData)
                return;

            var row = gvKokaku.GetRow(e.ListSourceRowIndex) as LuckyDrawKokakuDTO;
            if (row == null)
            {
                e.Value = GetDefaultImage(35, 35);
                return;
            }

            var keyBase = (!string.IsNullOrEmpty(row.LUCKY_DRAW_ID) ? row.LUCKY_DRAW_ID : "evt") +
                          "_" + (!string.IsNullOrEmpty(row.KOKAKU_HITO_PHONE) ? row.KOKAKU_HITO_PHONE : e.ListSourceRowIndex.ToString());

            // Temp preview
            if (!string.IsNullOrEmpty(row.TempImagePath) && File.Exists(row.TempImagePath))
            {
                var tmpKey = keyBase + "_tmp:" + row.TempImagePath;
                if (!_kokakuImageCache.TryGetValue(tmpKey, out var tmpImg))
                {
                    try
                    {
                        using (var img = Image.FromFile(row.TempImagePath))
                        {
                            tmpImg = ResizeImage(img, 35, 35);
                        }
                    }
                    catch
                    {
                        tmpImg = GetDefaultImage(35, 35);
                    }
                    _kokakuImageCache[tmpKey] = tmpImg;
                }
                e.Value = _kokakuImageCache[tmpKey];
                return;
            }

            // If DTO has base64 -> use it
            if (!string.IsNullOrEmpty(row.KOKAKU_HITO_IMG))
            {
                if (!_kokakuImageCache.TryGetValue(keyBase, out var cached))
                {
                    try
                    {
                        var image = Base64ToImage(row.KOKAKU_HITO_IMG);
                        cached = image != null ? ResizeImage(image, 35, 35) : GetDefaultImage(35, 35);
                    }
                    catch
                    {
                        cached = GetDefaultImage(35, 35);
                    }
                    _kokakuImageCache[keyBase] = cached;
                }
                e.Value = _kokakuImageCache[keyBase];
                return;
            }

            // No image -> trigger one-time fetch for kokaku images of this event
            if (!string.IsNullOrEmpty(row.LUCKY_DRAW_ID))
            {
                var fetchKey = $"kk:{row.LUCKY_DRAW_ID}";
                lock (_kokakuImageFetchInProgress)
                {
                    if (!_kokakuImageFetchInProgress.Contains(fetchKey))
                    {
                        _kokakuImageFetchInProgress.Add(fetchKey);
                        Task.Run(async () =>
                        {
                            try
                            {
                                var fetched = await LuckyDrawService.GetKokakuByEventAsync(row.LUCKY_DRAW_ID, includeMuko: true, includeImages: true);
                                if (fetched != null && fetched.Count > 0)
                                {
                                    this.Invoke((Action)(() =>
                                    {
                                        try
                                        {
                                            var ds = gcKokaku.DataSource as IEnumerable<LuckyDrawKokakuDTO>;
                                            if (ds != null)
                                            {
                                                foreach (var f in fetched)
                                                {
                                                    var match = ds.FirstOrDefault(x => x != null && x.KOKAKU_HITO_PHONE == f.KOKAKU_HITO_PHONE && x.LUCKY_DRAW_ID == f.LUCKY_DRAW_ID);
                                                    if (match != null)
                                                    {
                                                        match.KOKAKU_HITO_IMG = f.KOKAKU_HITO_IMG;
                                                    }
                                                }
                                            }
                                            gvKokaku.RefreshData();
                                        }
                                        catch {  }
                                    }));
                                }
                            }
                            catch 
                            {
                            }
                            finally
                            {
                                lock (_kokakuImageFetchInProgress) { _kokakuImageFetchInProgress.Remove(fetchKey); }
                            }
                        });
                    }
                }
            }

            e.Value = GetDefaultImage(35, 35);
        }
        private void GvKokaku_DoubleClick(object sender, EventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            if (view == null) return;

            var hitInfo = view.CalcHitInfo(view.GridControl.PointToClient(Control.MousePosition));

            if (hitInfo.InRowCell && hitInfo.Column != null && hitInfo.Column.FieldName == "KokakuImage")
            {
                // Require edit mode for kokaku
                if ((_kokakuCrudManager == null) || (!_kokakuCrudManager.IsEditing && !_editingRowHandleKokaku.HasValue))
                    return; // not in edit mode -> ignore double click

                var currentRow = view.GetRow(hitInfo.RowHandle) as LuckyDrawKokakuDTO;
                if (currentRow == null) return;

                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Chọn ảnh khách nhận";
                    ofd.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(ofd.FileName);

                            if (fileInfo.Length > 5 * 1024 * 1024)
                            {
                                XtraMessageBox.Show(
                                    "Kích thước file vượt quá 5MB.",
                                    "Cảnh báo",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return;
                            }

                            currentRow.TempImagePath = ofd.FileName;

                            gvKokaku.RefreshData();
                        }
                        catch (Exception ex)
                        {
                            XtraMessageBox.Show(
                                $"Lỗi: {ex.Message}",
                                "Lỗi",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void ClearKokakuImageCache()
        {
            foreach (var img in _kokakuImageCache.Values) img.Dispose();
            _kokakuImageCache.Clear();
        }
        private void GvKokaku_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            try
            {
                if (e.RowHandle < 0) return;
                var row = gvKokaku.GetRow(e.RowHandle) as LuckyDrawKokakuDTO;
                if (row == null) return;

                if (row.KOKAKU_MUKO_FLG.HasValue && row.KOKAKU_MUKO_FLG.Value == 1)
                {
                    e.Appearance.ForeColor = Color.Gray;
                    e.Appearance.BackColor = Color.FromArgb(230, 230, 230);
                    e.Appearance.Font = new Font(e.Appearance.Font ?? this.Font, FontStyle.Italic);
                    e.HighPriority = true;
                    return;
                }

                // Nếu SYURYOU_DATE <= hôm nay -> apply same "disabled" style
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
                    catch { /* ignore date parse errors */ }
                }
            }
            catch { /* ignore styling errors */ }
        }

        private void GvKokaku_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            try
            {
                if (e.RowHandle < 0) return;
                var row = gvKokaku.GetRow(e.RowHandle) as LuckyDrawKokakuDTO;
                if (row == null) return;

                // light red for expired date cell
                if (e.Column != null && e.Column.FieldName == "SYURYOU_DATE")
                {
                    if (row.SYURYOU_DATE.HasValue && row.SYURYOU_DATE.Value.Date < DateTime.Now.Date)
                    {
                        e.Appearance.BackColor = Color.FromArgb(255, 230, 230);
                        e.Appearance.ForeColor = Color.DarkRed;
            
                        return;
                    }
                }

                // Optional: highlight MUKO flag cell when flagged
                if (e.Column != null && e.Column.FieldName == "KOKAKU_MUKO_FLG")
                {
                    if (row.KOKAKU_MUKO_FLG.HasValue && row.KOKAKU_MUKO_FLG.Value == 1)
                    {
                        e.Appearance.BackColor = Color.FromArgb(255, 230, 230);
                        e.Appearance.ForeColor = Color.DarkRed;
                    
                        return;
                    }
                }
            }
            catch
            {
                // ignore styling errors
            }
        }

        #endregion

        #region IMAGE HELPER

        private Image Base64ToImage(string base64String)
        {
            if (string.IsNullOrEmpty(base64String)) return null;

            try
            {
                if (base64String.Contains(","))
                    base64String = base64String.Split(',')[1];

                byte[] imageBytes = Convert.FromBase64String(base64String);
                using (var ms = new MemoryStream(imageBytes))
                {
                    var originalImage = Image.FromStream(ms);
                    var clonedImage = new Bitmap(originalImage);
                    originalImage.Dispose();
                    return clonedImage;
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

            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new System.Drawing.Imaging.ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private Image GetDefaultImage(int width = 25, int height = 25)
        {
            var defaultImg = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(defaultImg))
            {
                g.Clear(Color.LightGray);
                using (var font = new Font("Arial", 8))
                using (var brush = new SolidBrush(Color.DarkGray))
                {
                    var text = "No Image";
                    var size = g.MeasureString(text, font);
                    g.DrawString(text, font, brush,
                        (width - size.Width) / 2,
                        (height - size.Height) / 2);
                }
            }
            return defaultImg;
        }

        #endregion

        #region SHARED TOOLBAR HANDLERS

        private void SharedToolbar_AddClicked(object sender, EventArgs e)
        {
            if (_activeGrid == ActiveGridType.Event)
            {
                // Add new row via CrudManager
                _eventCrudManager.AddNewRow();
                _sharedToolbar.SetEditingMode(true);

                // After adding, focus should be on the new row - store its handle
                _editingRowHandleEvent = gvEvents.FocusedRowHandle;

                // Ensure only that row is editable and open editor
                try { gvEvents.ShowEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                // Set giá trị mặc định cho row mới
                var newRow = gvEvents.GetFocusedRow() as LuckyDrawDTO;
                if (newRow != null)
                {
                    newRow.TOROKU_ID = "admin1"; // TODO: Lấy từ session

                    // ✅ THÊM MỚI: Set giá trị mặc định cho số ô vòng quay
                    newRow.LUCKY_DRAW_SLOT_NUM = 6; // Mặc định 6 ô
                }
            }
            else if (_activeGrid == ActiveGridType.Meisai)
            {
                // ✅ SỬA: Lấy ID trực tiếp từ gvEvents nếu có, fallback sang _selectedEventId
                var focusedEvt = gvEvents.GetFocusedRow() as LuckyDrawDTO;
                var currentEventId = focusedEvt?.LUCKY_DRAW_ID ?? _selectedEventId;

                if (string.IsNullOrEmpty(currentEventId))
                {
                    XtraMessageBox.Show(
                        "Vui lòng chọn một sự kiện trước!",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // ✅ SỬA: Kiểm tra giới hạn dựa trên event hiện tại
                var selectedEvent = _events?.FirstOrDefault(evt => evt.LUCKY_DRAW_ID == currentEventId);
                if (selectedEvent != null)
                {
                    var meisaiList = (gcMeisai.DataSource as List<LuckyDrawMeisaiDTO>) ?? new List<LuckyDrawMeisaiDTO>();

                    var currentMeisaiCount = meisaiList.Count(m =>
                        (m.LUCKY_DRAW_MEISAI_MUKO_FLG == null || m.LUCKY_DRAW_MEISAI_MUKO_FLG == 0));

                    if (currentMeisaiCount >= selectedEvent.LUCKY_DRAW_SLOT_NUM)
                    {
                        XtraMessageBox.Show(
                            $"Đã đạt giới hạn số ô vòng quay!\n\n" +
                            $"Sự kiện '{selectedEvent.LUCKY_DRAW_NAME}' chỉ cho phép tạo tối đa {selectedEvent.LUCKY_DRAW_SLOT_NUM} giải thưởng.\n" +
                            $"Hiện tại đã có: {currentMeisaiCount} giải thưởng.",
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                // Add new row
                _meisaiCrudManager.AddNewRow();
                _sharedToolbar.SetEditingMode(true);

                // After adding, store handle and show editor for that row only
                _editingRowHandleMeisai = gvMeisai.FocusedRowHandle;
                try { gvMeisai.ShowEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                // Hiển thị cột "Chọn ảnh"
                var colChooseImage = gvMeisai.Columns["TempImagePath"];
                if (colChooseImage != null)
                    colChooseImage.Visible = true;

                // Set giá trị mặc định cho row mới — dùng currentEventId thay vì _selectedEventId trực tiếp
                var newRow = gvMeisai.GetFocusedRow() as LuckyDrawMeisaiDTO;
                if (newRow != null)
                {
                    newRow.LUCKY_DRAW_ID = currentEventId;
                    newRow.TOROKU_ID = "admin1"; // TODO: Lấy từ session
                    newRow.LUCKY_DRAW_MEISAI_RATE = 0;
                    newRow.LUCKY_DRAW_MEISAI_SURYO = 0;
                }
            }
            else // Kokaku
            {
                // ✅ SỬA: Lấy ID trực tiếp từ gvEvents nếu có, fallback sang _selectedEventId
                var focusedEvt = gvEvents.GetFocusedRow() as LuckyDrawDTO;
                var currentEventId = focusedEvt?.LUCKY_DRAW_ID ?? _selectedEventId;


                if (string.IsNullOrEmpty(currentEventId))
                {
                    XtraMessageBox.Show(
                        "Vui lòng chọn một sự kiện trước!",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Add new kokaku row
                _kokakuCrudManager.AddNewRow();
                _sharedToolbar.SetEditingMode(true);

                _editingRowHandleKokaku = gvKokaku.FocusedRowHandle;
                try { gvKokaku.ShowEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
                var colChooseImage = gvKokaku.Columns["TempImagePath"];
                if (colChooseImage != null)
                    colChooseImage.Visible = true;

                var newRow = gvKokaku.GetFocusedRow() as LuckyDrawKokakuDTO;
                if (newRow != null)
                {
                    newRow.LUCKY_DRAW_ID = currentEventId;
                    newRow.TOROKU_ID = "admin1"; // TODO: session
                }
            }
        }

        public static event Action<string> KokakuListChanged;

        private void SharedToolbar_EditClicked(object sender, EventArgs e)
        {
            _sharedToolbar.SetEditingMode(true);

            if (_activeGrid == ActiveGridType.Event)
            {
                _eventCrudManager.EnableEdit();

                // Store the handle of the row being edited and open editor
                _editingRowHandleEvent = gvEvents.FocusedRowHandle;
                try { gvEvents.ShowEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                // ✅ THÊM: Hiển thị cột "Chọn ảnh"
                var colChooseImage = gvEvents.Columns["TempImagePath"];
                if (colChooseImage != null)
                    colChooseImage.Visible = true;
            }
            else if (_activeGrid == ActiveGridType.Meisai)
            {
                _meisaiCrudManager.EnableEdit();

                // Store the handle of the row being edited and open editor
                _editingRowHandleMeisai = gvMeisai.FocusedRowHandle;
                try { gvMeisai.ShowEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                // ✅ THÊM: Hiển thị cột "Chọn ảnh"
                var colChooseImage = gvMeisai.Columns["TempImagePath"];
                if (colChooseImage != null)
                    colChooseImage.Visible = true;
            }
            else // Kokaku
            {
                _kokakuCrudManager.EnableEdit();
                _editingRowHandleKokaku = gvKokaku.FocusedRowHandle;
                try { gvKokaku.ShowEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                var colChooseImage = gvKokaku.Columns["TempImagePath"];
                if (colChooseImage != null)
                    colChooseImage.Visible = true;
            }
        }

        private async void SharedToolbar_DeleteClicked(object sender, EventArgs e)
        {
            if (_activeGrid == ActiveGridType.Event)
            {
                await _eventCrudManager.DeleteRowAsync();
            }
            else if (_activeGrid == ActiveGridType.Meisai)
            {
                await _meisaiCrudManager.DeleteRowAsync();
            }
            else
            {
                // Kokaku delete -> notify listeners if success
                bool deleted = await _kokakuCrudManager.DeleteRowAsync();
                if (deleted)
                {
                    try
                    {
                        var focusedEvt = gvEvents.GetFocusedRow() as LuckyDrawDTO;
                        var eventId = focusedEvt?.LUCKY_DRAW_ID ?? _selectedEventId;
                        KokakuListChanged?.Invoke(eventId);
                    }
                    catch { /* swallow */ }
                }
            }
        }

        private async void SharedToolbar_SaveClicked(object sender, EventArgs e)
        {
            bool success = false;

            // Commit edits in-grid first
            try
            {
                if (_activeGrid == ActiveGridType.Event)
                {
                    gvEvents.PostEditor();
                    gvEvents.CloseEditor();
                    gvEvents.UpdateCurrentRow();
                }
                else if (_activeGrid == ActiveGridType.Meisai)
                {
                    gvMeisai.PostEditor();
                    gvMeisai.CloseEditor();
                    gvMeisai.UpdateCurrentRow();
                }
                else
                {
                    if (gvKokaku != null)
                    {
                        gvKokaku.PostEditor();
                        gvKokaku.CloseEditor();
                        gvKokaku.UpdateCurrentRow();
                    }
                }
            }
            catch { /* ignore commit errors */ }

            // Save per active grid
            if (_activeGrid == ActiveGridType.Event)
            {
                var focused = gvEvents.GetFocusedRow() as LuckyDrawDTO;
                string focusedId = focused?.LUCKY_DRAW_ID;

                if (focused != null)
                {
                    var (isValid, err) = focused.Validate();
                    if (!isValid)
                    {
                        XtraMessageBox.Show(err, "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                success = await _eventCrudManager.SaveChangesAsync();

                if (success)
                {
                    // Hide temp-image column if present
                    var colChooseImage = gvEvents.Columns["TempImagePath"];
                    if (colChooseImage != null) colChooseImage.Visible = false;

                    // Clear cache and reload data from server so grid shows updated image
                    try
                    {
                        ClearEventImageCache();
                        await _eventCrudManager.LoadDataAsync();

                        // Try to restore focus to saved row
                        if (!string.IsNullOrWhiteSpace(focusedId))
                        {
                            try
                            {
                                var handle = gvEvents.LocateByValue("LUCKY_DRAW_ID", focusedId);
                                if (handle >= 0)
                                {
                                    gvEvents.FocusedRowHandle = handle;
                                    gvEvents.MakeRowVisible(handle);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                            }
                        }

                        gvEvents.RefreshData();
                    }
                    catch 
                    {
                    }

                    _editingRowHandleEvent = null;
                }
            }
            else if (_activeGrid == ActiveGridType.Meisai)
            {
                var focused = gvMeisai.GetFocusedRow() as LuckyDrawMeisaiDTO;
                string focusedKey = focused?.LUCKY_DRAW_MEISAI_NO; // use id composite if needed

                if (focused != null)
                {
                    var (isValid, err) = focused.Validate();
                    if (!isValid)
                    {
                        XtraMessageBox.Show(err, "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                success = await _meisaiCrudManager.SaveChangesAsync();

                if (success)
                {
                    var colChooseImage = gvMeisai.Columns["TempImagePath"];
                    if (colChooseImage != null) colChooseImage.Visible = false;

                    try
                    {
                        ClearMeisaiImageCache();
                        await _meisaiCrudManager.LoadDataAsync();

                        // restore focus by meisai no if possible
                        if (!string.IsNullOrWhiteSpace(focusedKey))
                        {
                            try
                            {
                                var handle = gvMeisai.LocateByValue("LUCKY_DRAW_MEISAI_NO", focusedKey);
                                if (handle >= 0)
                                {
                                    gvMeisai.FocusedRowHandle = handle;
                                    gvMeisai.MakeRowVisible(handle);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                            }
                        }

                        gvMeisai.RefreshData();
                    }
                    catch 
                    {
                    }

                    _editingRowHandleMeisai = null;
                }
            }
            else // Kokaku
            {
                var focused = gvKokaku?.GetFocusedRow() as LuckyDrawKokakuDTO;
                string focusedPhone = focused?.KOKAKU_HITO_PHONE;

                if (focused != null)
                {
                    var (isValid, err) = focused.Validate();
                    if (!isValid)
                    {
                        XtraMessageBox.Show(err, "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                success = await _kokakuCrudManager.SaveChangesAsync();

                if (success)
                {
                    try
                    {
                        ClearKokakuImageCache();
                        await _kokakuCrudManager.LoadDataAsync();

                        if (!string.IsNullOrWhiteSpace(focusedPhone))
                        {
                            try
                            {
                                var handle = gvKokaku.LocateByValue("KOKAKU_HITO_PHONE", focusedPhone);
                                if (handle >= 0)
                                {
                                    gvKokaku.FocusedRowHandle = handle;
                                    gvKokaku.MakeRowVisible(handle);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                            }
                        }

                        gvKokaku.RefreshData();

                        // notify other modules as before
                        try
                        {
                            var focusedEvt = gvEvents.GetFocusedRow() as LuckyDrawDTO;
                            var eventId = focusedEvt?.LUCKY_DRAW_ID ?? _selectedEventId;
                            KokakuListChanged?.Invoke(eventId);
                        }
                        catch { /* ignore */ }
                    }
                    catch 
                    {
                    }

                    _editingRowHandleKokaku = null;
                }
            }

            if (success)
            {
                _sharedToolbar.SetEditingMode(false);
            }
        }

        private async void SharedToolbar_CancleClicked(object sender, EventArgs e)
        {
            if (_activeGrid == ActiveGridType.Event)
            {
                await _eventCrudManager.CancelChangesAsync();

                // ✅ THÊM: Ẩn cột "Chọn ảnh"
                var colChooseImage = gvEvents.Columns["TempImagePath"];
                if (colChooseImage != null)
                    colChooseImage.Visible = false;

                // Reset editing handle
                _editingRowHandleEvent = null;
                try { gvEvents.CloseEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            }
            else if (_activeGrid == ActiveGridType.Meisai)
            {
                await _meisaiCrudManager.CancelChangesAsync();

                // ✅ THÊM: Ẩn cột "Chọn ảnh"
                var colChooseImage = gvMeisai.Columns["TempImagePath"];
                if (colChooseImage != null)
                    colChooseImage.Visible = false;

                // Reset editing handle
                _editingRowHandleMeisai = null;
                try { gvMeisai.CloseEditor(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            }
            else
            {
                await _kokakuCrudManager.CancelChangesAsync();

                var colChooseImage = gvKokaku.Columns["TempImagePath"];
                if (colChooseImage != null)
                    colChooseImage.Visible = false;

                _editingRowHandleKokaku = null;
                try { gvKokaku.CloseEditor(); }
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
                if (_activeGrid == ActiveGridType.Event)
                {
                    // Clear cache ảnh trước khi reload
                    ClearEventImageCache();

                    // Load events without images (nhẹ)
                    await _eventCrudManager.LoadDataAsync();
                }
                else if (_activeGrid == ActiveGridType.Meisai)
                {
                    // Clear cache và reload meisai cho event hiện tại
                    ClearMeisaiImageCache();
                    await _meisaiCrudManager.LoadDataAsync();
                }
                else // Kokaku
                {
                    ClearKokakuImageCache();
                    await _kokakuCrudManager.LoadDataAsync();
                }
            }
            catch 
            {
            }
        }

        #endregion

        #region SHOWING-EDITOR HANDLERS (restrict editing to a single row)

        private void GvEvents_ShowingEditor(object sender, CancelEventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            // If there is an editing handle set, allow editing only on that row.
            if (_editingRowHandleEvent.HasValue)
            {
                if (view.FocusedRowHandle != _editingRowHandleEvent.Value)
                {
                    e.Cancel = true;
                    return;
                }

                // Only allow editing of key fields when this is a NEW row.
                var row = view.GetFocusedRow() as LuckyDrawDTO;
                var col = view.FocusedColumn;
                if (row != null && col != null && !row.IsNewRow())
                {
                    if (col.FieldName == "LUCKY_DRAW_ID" || col.FieldName == "LUCKY_DRAW_BUNRUI")
                    {
                        // block editing of ID & Bunrui for existing rows
                        e.Cancel = true;
                        return;
                    }
                }
            }
            else
            {
                // If not in edit mode, cancel editing entirely
                e.Cancel = true;
                return;
            }
        }

        private void GvMeisai_ShowingEditor(object sender, CancelEventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            if (_editingRowHandleMeisai.HasValue)
            {
                if (view.FocusedRowHandle != _editingRowHandleMeisai.Value)
                {
                    e.Cancel = true;
                    return;
                }

                var row = view.GetFocusedRow() as LuckyDrawMeisaiDTO;
                var col = view.FocusedColumn;
                if (col != null)
                {
                    // Always block editing LUCKY_DRAW_ID in Meisai grid (we set it programmatically on Add)
                    if (col.FieldName == "LUCKY_DRAW_ID")
                    {
                        e.Cancel = true;
                        return;
                    }

                    // Block editing meisai no for existing rows (allow only when new)
                    if (row != null && !row.IsNewRow() && col.FieldName == "LUCKY_DRAW_MEISAI_NO")
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
            else
            {
                // Không đang edit hàng -> không cho chỉnh bất kỳ ô nào (bao gồm MUKO)
                e.Cancel = true;
                return;
            }
        }

        private void GvKokaku_ShowingEditor(object sender, CancelEventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            if (_editingRowHandleKokaku.HasValue)
            {
                if (view.FocusedRowHandle != _editingRowHandleKokaku.Value)
                {
                    e.Cancel = true;
                    return;
                }

                var row = view.GetFocusedRow() as LuckyDrawKokakuDTO;
                var col = view.FocusedColumn;
                if (col != null)
                {
                    // Always block editing LUCKY_DRAW_ID in Kokaku grid (we set it programmatically on Add)
                    if (col.FieldName == "LUCKY_DRAW_ID")
                    {
                        e.Cancel = true;
                        return;
                    }

                    // Block editing primary key phone for existing rows
                    if (row != null && !row.IsNewRow() && col.FieldName == "KOKAKU_HITO_PHONE")
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
            else
            {
                // Không đang edit hàng -> không cho chỉnh bất kỳ ô nào (bao gồm MUKO)
                e.Cancel = true;
                return;
            }
        }

        #endregion

        #region IMAGE UPLOAD HANDLERS

        private void BtnEditEventImage_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            // Only allow file pick when in edit mode for events
            if ((_eventCrudManager == null) || (!_eventCrudManager.IsEditing && !_editingRowHandleEvent.HasValue))
                return;

            var currentRow = gvEvents.GetFocusedRow() as LuckyDrawDTO;
            if (currentRow == null) return;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn ảnh nền sự kiện";
                ofd.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var fileInfo = new FileInfo(ofd.FileName);

                        // Validate size (max 50MB)
                        if (fileInfo.Length > MAX_IMAGE_FILE_SIZE)
                        {
                            XtraMessageBox.Show(
                                $"Kích thước file vượt quá {MAX_IMAGE_FILE_SIZE / (1024 * 1024)}MB. Vui lòng chọn file nhỏ hơn.",
                                "Cảnh báo",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        // Lưu path vào DTO
                        currentRow.TempImagePath = ofd.FileName;

                        // Refresh grid để hiển thị preview
                        gvEvents.RefreshData();
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show(
                            $"Lỗi khi chọn ảnh: {ex.Message}",
                            "Lỗi",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void BtnEditMeisaiImage_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            // Only allow file pick when in edit mode for meisai
            if ((_meisaiCrudManager == null) || (!_meisaiCrudManager.IsEditing && !_editingRowHandleMeisai.HasValue))
                return;

            var currentRow = gvMeisai.GetFocusedRow() as LuckyDrawMeisaiDTO;
            if (currentRow == null) return;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn ảnh giải thưởng";
                ofd.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var fileInfo = new FileInfo(ofd.FileName);

                        if (fileInfo.Length > 5 * 1024 * 1024)
                        {
                            XtraMessageBox.Show(
                                "Kích thước file vượt quá 5MB.",
                                "Cảnh báo",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        currentRow.TempImagePath = ofd.FileName;

                        gvMeisai.RefreshData();
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show(
                            $"Lỗi: {ex.Message}",
                            "Lỗi",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        #endregion
    }
}