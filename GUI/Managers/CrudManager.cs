using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using GUI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;

namespace GUI.Managers
{
    /// <summary>
    /// Generic CRUD Manager để quản lý các thao tác thêm, sửa, xóa, lưu, refresh
    /// </summary>
    /// <typeparam name="T">Entity type phải implement ICrudEntity</typeparam>
    public class CrudManager<T> where T : class, ICrudEntity, new()
    {
        private GridControl _gridControl;
        private GridView _gridView;
        private List<T> _dataSource;
        private BindingList<T> _bindingList;         
        private bool _isEditing = false;
        private bool _isAdding = false;
        private bool _isInitialized = false;

        // Delegates cho các operations
        public Func<Task<List<T>>> LoadDataFunc { get; set; }
        public Func<T, Task<(bool success, string message)>> SaveFunc { get; set; }
        public Func<T, Task<(bool success, string message)>> DeleteFunc { get; set; }
        public Func<T> CreateNewEntityFunc { get; set; }
        public void ClearData()
        {
            // Clear internal list and the binding list while preserving the binding itself.
            _dataSource = new List<T>();
            try
            {
                _bindingList.RaiseListChangedEvents = false;
                _bindingList.Clear();
                _bindingList.RaiseListChangedEvents = true;
                _bindingList.ResetBindings();
            }
            catch
            {
                // swallow any exception to avoid breaking UI flow
            }
        }
        public CrudManager(GridControl gridControl, GridView gridView)
        {
            _gridControl = gridControl;
            _gridView = gridView;
            _dataSource = new List<T>();

            // Initialize BindingList and assign once to grid control to avoid repeated rebinds
            _bindingList = new BindingList<T>();
            _gridControl.DataSource = _bindingList;
        }

        /// <summary>
        /// Load dữ liệu
        /// </summary>
        public async Task LoadDataAsync()
        {
            if (LoadDataFunc == null)
            {
                XtraMessageBox.Show("LoadDataFunc chưa được cấu hình!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _gridView.ShowLoadingPanel();
            try
            {
                _dataSource = await LoadDataFunc();

                // Update binding list efficiently
                _bindingList.RaiseListChangedEvents = false;
                _bindingList.Clear();
                foreach (var item in _dataSource)
                    _bindingList.Add(item);
                _bindingList.RaiseListChangedEvents = true;
                _bindingList.ResetBindings();

                // BestFitColumns only first time (tốn kém nếu gọi liên tục)
                if (!_isInitialized)
                {
                    try { _gridView.BestFitColumns(); } catch { }
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _gridView.HideLoadingPanel();
            }
        }

        /// <summary>
        /// Thêm row mới
        /// </summary>
        public void AddNewRow()
        {
            if (_isEditing)
            {
                XtraMessageBox.Show("Vui lòng lưu hoặc hủy thay đổi trước khi thêm mới!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Tạo entity mới
            T newEntity = CreateNewEntityFunc != null ? CreateNewEntityFunc() : new T();
            newEntity.MarkAsNew();

            // Thêm vào đầu danh sách
            _dataSource.Insert(0, newEntity);
            RefreshGrid();

            // Enable edit mode
            _gridView.OptionsBehavior.Editable = true;
            _isAdding = true;
            _isEditing = true;

            // Focus vào row mới
            _gridView.FocusedRowHandle = 0;
        }

        /// <summary>
        /// Bật chế độ sửa
        /// </summary>
        public void EnableEdit()
        {
            var selectedEntity = _gridView.GetFocusedRow() as T;
            if (selectedEntity == null)
            {
                XtraMessageBox.Show("Vui lòng chọn một dòng để sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selectedEntity.IsNewRow())
            {
                XtraMessageBox.Show("Dòng này đang ở chế độ thêm mới!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _gridView.OptionsBehavior.Editable = true;
            _isEditing = true;
        }

        /// <summary>
        /// Lưu thay đổi
        /// </summary>
        public async Task<bool> SaveChangesAsync()
        {
            if (!_isEditing)
            {
                XtraMessageBox.Show("Không có thay đổi nào để lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (SaveFunc == null)
            {
                XtraMessageBox.Show("SaveFunc chưa được cấu hình!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Lấy entity hiện tại
            var currentEntity = _gridView.GetFocusedRow() as T;
            if (currentEntity == null)
            {
                XtraMessageBox.Show("Không tìm thấy dữ liệu để lưu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Validate
            var (isValid, errorMessage) = currentEntity.Validate();
            if (!isValid)
            {
                XtraMessageBox.Show(errorMessage, "Lỗi Validate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                var (success, message) = await SaveFunc(currentEntity);
                if (success)
                {
                    XtraMessageBox.Show("Lưu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _gridView.OptionsBehavior.Editable = false;
                    _isEditing = false;
                    _isAdding = false;
                    await LoadDataAsync(); // Reload data
                    return true;
                }
                else
                {
                    XtraMessageBox.Show($"Lưu thất bại: {message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Xóa row
        /// </summary>
        public async Task<bool> DeleteRowAsync()
        {
            var selectedEntity = _gridView.GetFocusedRow() as T;
            if (selectedEntity == null)
            {
                XtraMessageBox.Show("Vui lòng chọn một dòng để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (selectedEntity.IsNewRow())
            {
                // Nếu là row mới chưa lưu, xóa trực tiếp
                _dataSource.Remove(selectedEntity);
                RefreshGrid();
                _isAdding = false;
                _isEditing = false;
                _gridView.OptionsBehavior.Editable = false;
                return true;
            }

            if (XtraMessageBox.Show("Bạn có chắc chắn muốn xóa?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return false;
            }

            if (DeleteFunc == null)
            {
                XtraMessageBox.Show("DeleteFunc chưa được cấu hình!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                var (success, message) = await DeleteFunc(selectedEntity);
                if (success)
                {
                    XtraMessageBox.Show("Xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadDataAsync();
                    return true;
                }
                else
                {
                    XtraMessageBox.Show($"Xóa thất bại: {message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Hủy thay đổi
        /// </summary>
        public async Task CancelChangesAsync()
        {
            try
            {
                // If we are in the middle of adding a new row: remove it locally and keep the binding.
                if (_isAdding)
                {
                    var newRow = _dataSource.FirstOrDefault(x => x.IsNewRow());
                    if (newRow != null)
                    {
                        _dataSource.Remove(newRow);
                        RefreshGrid(); // preserve binding and UI
                    }

                    _isAdding = false;
                    _isEditing = false;
                    _gridView.OptionsBehavior.Editable = false;
                    return;
                }

                // If we are editing an existing row (not adding), reload from server to discard changes.
                if (_isEditing)
                {
                    _gridView.OptionsBehavior.Editable = false;
                    _isEditing = false;
                    _isAdding = false;

                    // Only reload if a LoadDataFunc is configured.
                    if (LoadDataFunc != null)
                    {
                        await LoadDataAsync();
                    }
                    return;
                }

                // Nothing to cancel: ensure edit state is cleared.
                _gridView.OptionsBehavior.Editable = false;
                _isEditing = false;
                _isAdding = false;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Lỗi khi hủy thay đổi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Refresh grid
        /// </summary>
        private void RefreshGrid()
        {
            // Previously re-assigned DataSource and BestFitColumns each time.
            // Now we just refresh the binding list which is already assigned to grid.
            try
            {
                _gridControl.BeginUpdate();
                _bindingList.RaiseListChangedEvents = false;
                _bindingList.Clear();
                foreach (var item in _dataSource)
                    _bindingList.Add(item);
                _bindingList.RaiseListChangedEvents = true;
                _bindingList.ResetBindings();
            }
            finally
            {
                _gridControl.EndUpdate();
            }
        }

        /// <summary>
        /// Kiểm tra có đang edit không
        /// </summary>
        public bool IsEditing => _isEditing;
    }
}