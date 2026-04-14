using System;
using GUI.Interfaces;

namespace GUI.DTO
{
    /// <summary>
    /// DTO phía client cho lịch sử quay thưởng (LUCKY_DRAW_PROGRAME_HISTORY)
    /// Phù hợp với LuckyDrawAPI.DTOs.LuckyDrawHistoryDTO trên server.
    /// </summary>
    public class LuckyDrawHistoryDTO : ICrudEntity
    {
        public decimal PROGRAME_JISSHI_ID { get; set; }
        public string LUCKY_DRAW_PROGRAME_ID { get; set; }
        public string LUCKY_DRAW_ID { get; set; }
        public string TOROKU_ID { get; set; }
        public DateTime TOROKU_DATE { get; set; }

        public string MITUMORI_NO_SANSYO { get; set; }
        public string KOKAKU_HITO_NAME { get; set; }
        public string KOKAKU_HITO_PHONE { get; set; }
        public string KOKAKU_HITO_ADDRESS { get; set; }
        public string RATE_LOG_SPLIT { get; set; }
        public string KENSYO_DRAW_MEISAI_ID { get; set; }
        public string KENSYO_VOUCHER_CODE { get; set; }
        public double? KENSYO_DRAW_MEISAI_SURYO { get; set; }

        public DateTime? KAISHI_DATE { get; set; }
        public DateTime? SYURYOU_DATE { get; set; }

        public string KOKAKU_NAME_SYUTOKU { get; set; }
        public string KOKAKU_PHONE_SYUTOKU { get; set; }
        public string KOKAKU_SHOUMEISYO_NO_SYUTOKU { get; set; }
        public string KOKAKU_ADDRESS_SYUTOKU { get; set; }
        public DateTime? KOKAKU_SYUTOKU_DATE { get; set; }

        public string TANTO_SYA_NAME { get; set; }
        public string MITUMORI_NO_SYUTOKU_SANSYO { get; set; }

        public string MAE_JISSHI_BG_IMG { get; set; }
        public string ATO_JISSHI_BG_IMG { get; set; }

        public DateTime? JISSHI_KAISHI_DATE { get; set; }
        public DateTime? JISSHI_SYURYOU_DATE { get; set; }

        // ===== ICrudEntity Implementation =====
        private bool _isNewRow = false;

        public string GetUniqueId()
        {
            return PROGRAME_JISSHI_ID.ToString();
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
            if (string.IsNullOrWhiteSpace(LUCKY_DRAW_PROGRAME_ID))
                return (false, "Mã chương trình không được để trống!");

            if (string.IsNullOrWhiteSpace(LUCKY_DRAW_ID))
                return (false, "Mã sự kiện không được để trống!");

            return (true, string.Empty);
        }
    }
}