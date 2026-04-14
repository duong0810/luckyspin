using System;
using System.Windows.Forms;

namespace GUI.UI.modules
{
    /// <summary>
    /// UserControl chứa toolbar CRUD tái sử dụng
    /// </summary>
    public partial class uc_CrudToolbar : UserControl
    {
        // Events để parent control có thể subscribe
        public event EventHandler AddClicked;
        public event EventHandler EditClicked;
        public event EventHandler DeleteClicked;
        public event EventHandler SaveClicked;
        public event EventHandler CancelClicked;
        public event EventHandler RefreshClicked;
        
        public uc_CrudToolbar()
        {
            InitializeComponent();
        }

        #region Button Click Events

        private void btnAdd_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            AddClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnEdit_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            EditClicked?.Invoke(this, EventArgs.Empty);
        }                           

        private void btnDelete_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            DeleteClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnSave_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            SaveClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            RefreshClicked?.Invoke(this, EventArgs.Empty);
        }
        private void btnCancle_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Public Methods để điều khiển trạng thái buttons

        /// <summary>
        /// Bật/tắt chế độ editing (khi bấm Thêm/Sửa)
        /// </summary>
        public void SetEditingMode(bool isEditing)
        {
            btnAdd.Enabled = !isEditing;
            btnEdit.Enabled = !isEditing;
            btnDelete.Enabled = !isEditing;
            btnRefresh.Enabled = !isEditing;

            btnSave.Enabled = isEditing;
            btnCancle.Enabled = isEditing;
        }

        /// <summary>
        /// Enable/Disable toàn bộ toolbar
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            btnAdd.Enabled = enabled;
            btnEdit.Enabled = enabled;
            btnDelete.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnCancle.Enabled = enabled;
            btnRefresh.Enabled = enabled;
        }

        /// <summary>
        /// Ẩn/hiện các nút cụ thể
        /// </summary>
        public void ShowButton(string buttonName, bool visible)
        {
            switch (buttonName.ToLower())
            {
                case "add":
                    btnAdd.Visibility = visible ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never;
                    break;
                case "edit":
                    btnEdit.Visibility = visible ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never;
                    break;
                case "delete":
                    btnDelete.Visibility = visible ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never;
                    break;
                case "save":
                    btnSave.Visibility = visible ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never;
                    break;
                case "cancel":
                    btnCancle.Visibility = visible ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never;
                    break;
                case "refresh":
                    btnRefresh.Visibility = visible ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never;
                    break;
            }
        }


        #endregion

       
    }
}