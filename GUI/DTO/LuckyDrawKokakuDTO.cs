using System;
using System.ComponentModel.DataAnnotations;
using GUI.Interfaces;
using Newtonsoft.Json;

namespace GUI.DTO
{
    public class LuckyDrawKokakuDTO : ICrudEntity
    {
        [Required]
        [StringLength(50)]
        public string LUCKY_DRAW_ID { get; set; }

        [Required]
        [StringLength(20)]
        public string KOKAKU_HITO_PHONE { get; set; }

        [StringLength(50)]
        public string KOKAKU_HITO_NAME { get; set; }

        // Base64 image string for client-side
        public string KOKAKU_HITO_IMG { get; set; }

        [StringLength(250)]
        public string KOKAKU_ADDRESS { get; set; }

        [StringLength(50)]
        public string KOKAKU_SHOUMEISYO_NO { get; set; }

        public int? KOKAKU_MUKO_FLG { get; set; }

        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? TOROKU_DATE { get; set; }
        [StringLength(12)]
        public string TOROKU_ID { get; set; }

        [StringLength(500)]
        public string KOSIN_NAIYOU { get; set; }

        public DateTime? KOSIN_DATE { get; set; }

        [StringLength(12)]
        public string KOSIN_ID { get; set; }

        public DateTime? KAISHI_DATE { get; set; }
        public DateTime? SYURYOU_DATE { get; set; }

        // Client-only helper for preview/upload path (not serialized)
        [JsonIgnore]
        public string TempImagePath { get; set; }

        // ICrudEntity support for CrudManager
        [JsonIgnore]
        private bool _isNewFlag;

        public void MarkAsNew() => _isNewFlag = true;
        public bool IsNewRow() => _isNewFlag;

        /// <summary>
        /// Return unique id for grid identification (implements ICrudEntity.GetUniqueId)
        /// </summary>
        public string GetUniqueId()
        {
            return $"{LUCKY_DRAW_ID ?? ""}|{KOKAKU_HITO_PHONE ?? ""}";
        }

        /// <summary>
        /// Simple validation used by CrudManager before save.
        /// Must match interface tuple element names: (bool isValid, string errorMessage)
        /// </summary>
        public (bool isValid, string errorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(LUCKY_DRAW_ID))
                return (false, "LUCKY_DRAW_ID không được để trống.");
            if (string.IsNullOrWhiteSpace(KOKAKU_HITO_PHONE))
                return (false, "Số điện thoại khách (KOKAKU_HITO_PHONE) không được để trống.");
            return (true, null);
        }
    }
}