using GUI.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace GUI.DTO
{
    /// <summary>
    /// DTO cho chương trình quay thưởng (LUCKY_DRAW_PROGRAME)
    /// </summary>
    public class LuckyDrawProgrameDTO : ICrudEntity
    {
        // ===== NOT NULL FIELDS (Required) =====
        [Required(ErrorMessage = "LUCKY_DRAW_PROGRAME_ID không được để trống")]
        [StringLength(50, ErrorMessage = "LUCKY_DRAW_PROGRAME_ID không được vượt quá 50 ký tự")]
        public string LUCKY_DRAW_PROGRAME_ID { get; set; }

        [Required(ErrorMessage = "LUCKY_DRAW_ID không được để trống")]
        [StringLength(50, ErrorMessage = "LUCKY_DRAW_ID không được vượt quá 50 ký tự")]
        public string LUCKY_DRAW_ID { get; set; }

        [Required(ErrorMessage = "TOROKU_ID không được để trống")]
        [StringLength(12, ErrorMessage = "TOROKU_ID không được vượt quá 12 ký tự")]
        public string TOROKU_ID { get; set; }

        // ✅ SỬA: KOSIN_ID không bắt buộc trong DTO (chỉ dùng cho UPDATE)
        [StringLength(12, ErrorMessage = "KOSIN_ID không được vượt quá 12 ký tự")]
        public string KOSIN_ID { get; set; }

        // Auto-generated
        public DateTime TOROKU_DATE { get; set; }

        // ===== NULLABLE FIELDS (Optional) =====
        [StringLength(250, ErrorMessage = "LUCKY_DRAW_PROGRAME_NAME không được vượt quá 250 ký tự")]
        public string LUCKY_DRAW_PROGRAME_NAME { get; set; }

        public DateTime? KAISHI_DATE { get; set; }

        public DateTime? SYURYOU_DATE { get; set; }

        [StringLength(250, ErrorMessage = "PROGRAME_SLOGAN không được vượt quá 250 ký tự")]
        public string PROGRAME_SLOGAN { get; set; }

        [StringLength(500, ErrorMessage = "KOSIN_NAIYOU không được vượt quá 500 ký tự")]
        public string KOSIN_NAIYOU { get; set; }

        public DateTime? KOSIN_DATE { get; set; }

        // ICrudEntity members (skeleton — implement as in other DTOs)
        private bool _isNewRow = false;
        public void MarkAsNew() => _isNewRow = true;
        public bool IsNewRow() => _isNewRow;
        public string GetUniqueId() => LUCKY_DRAW_PROGRAME_ID;
        public (bool isValid, string errorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(LUCKY_DRAW_PROGRAME_ID))
                return (false, "LUCKY_DRAW_PROGRAME_ID không được để trống.");
            if (LUCKY_DRAW_PROGRAME_ID.Length > 50)
                return (false, "LUCKY_DRAW_PROGRAME_ID không được vượt quá 50 ký tự.");
            if (string.IsNullOrWhiteSpace(LUCKY_DRAW_ID))
                return (false, "LUCKY_DRAW_ID không được để trống.");
            if (LUCKY_DRAW_ID.Length > 50)
                return (false, "LUCKY_DRAW_ID không được vượt quá 50 ký tự.");
            if (!string.IsNullOrWhiteSpace(TOROKU_ID) && TOROKU_ID.Length > 12)
                return (false, "TOROKU_ID không được vượt quá 12 ký tự.");
            if (!string.IsNullOrWhiteSpace(KOSIN_ID) && KOSIN_ID.Length > 12)
                return (false, "KOSIN_ID không được vượt quá 12 ký tự.");
            return (true, null);
        }
    }
}