namespace GUI.Interfaces
{
    /// <summary>
    /// Interface cho các entity có thể thực hiện CRUD operations
    /// </summary>
    public interface ICrudEntity
    {
        /// <summary>
        /// Trả về ID duy nhất của entity (dùng cho việc identify row)
        /// </summary>
        string GetUniqueId();

        /// <summary>
        /// Kiểm tra entity có phải là new row (chưa lưu) hay không
        /// </summary>
        bool IsNewRow();

        /// <summary>
        /// Đánh dấu entity là new row (chưa lưu)
        /// </summary>
        void MarkAsNew();

        /// <summary>
        /// Validate dữ liệu entity
        /// </summary>
        /// <returns>Tuple (isValid, errorMessage)</returns>
        (bool isValid, string errorMessage) Validate();
    }
}