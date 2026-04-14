using GUI.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace GUI.DTO
{
    /// <summary>
    /// DTO cho sự kiện quay thưởng chính (LUCKY_DRAW)
    /// </summary>
    public class LuckyDrawDTO : ICrudEntity
    {
        public string LUCKY_DRAW_ID { get; set; }
        public string LUCKY_DRAW_NAME { get; set; }
        public string LUCKY_DRAW_BUNRUI { get; set; }
        public string LUCKY_DRAW_TITLE { get; set; }
        public string LUCKY_DRAW_SLOGAN { get; set; }

        [Range(1, 100, ErrorMessage = "LUCKY_DRAW_SLOT_NUM phải từ 1 đến 100")]
        public int LUCKY_DRAW_SLOT_NUM { get; set; } = 1;

        // Convert Binary sang Base64 string để truyền qua API
        public string LUCKY_DRAW_BG_IMG { get; set; }

        // Thông tin audit
        public DateTime TOROKU_DATE { get; set; }
        public string TOROKU_ID { get; set; }
        public string KOSIN_NAIYOU { get; set; }
        public DateTime? KOSIN_DATE { get; set; }
        public string KOSIN_ID { get; set; }

        [System.ComponentModel.Browsable(false)]
        [Newtonsoft.Json.JsonIgnore]
        public string TempImagePath { get; set; }

        // ===== ICrudEntity Implementation =====

        private bool _isNewRow = false;

        public string GetUniqueId()
        {
            return LUCKY_DRAW_ID;
        }

        public bool IsNewRow()
        {
            return _isNewRow;
        }

        public void MarkAsNew()
        {
            _isNewRow = true;
        }

        public (bool isValid, string errorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(LUCKY_DRAW_ID))
                return (false, "Mã sự kiện không được để trống!");

            if (LUCKY_DRAW_ID.Length > 50)
                return (false, "Mã sự kiện không được vượt quá 50 ký tự!");

            if (!string.IsNullOrWhiteSpace(LUCKY_DRAW_NAME) && LUCKY_DRAW_NAME.Length > 250)
                return (false, "Tên sự kiện không được vượt quá 250 ký tự!");

            if (!string.IsNullOrWhiteSpace(LUCKY_DRAW_BUNRUI) && LUCKY_DRAW_BUNRUI.Length > 50)
                return (false, "Phân loại không được vượt quá 50 ký tự!");

            if (!string.IsNullOrWhiteSpace(LUCKY_DRAW_TITLE) && LUCKY_DRAW_TITLE.Length > 250)
                return (false, "Tiêu đề không được vượt quá 250 ký tự!");

            if (!string.IsNullOrWhiteSpace(LUCKY_DRAW_SLOGAN) && LUCKY_DRAW_SLOGAN.Length > 250)
                return (false, "Slogan không được vượt quá 250 ký tự!");

            if (LUCKY_DRAW_SLOT_NUM < 1 || LUCKY_DRAW_SLOT_NUM > 100)
                return (false, "LUCKY_DRAW_SLOT_NUM phải là số nguyên trong khoảng 1-100!");

            return (true, string.Empty);
        }

    }
}