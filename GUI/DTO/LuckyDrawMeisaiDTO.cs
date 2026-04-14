using GUI.Interfaces;
using Newtonsoft.Json;
using System;
using System.Drawing;

namespace GUI.DTO
{
    /// <summary>
    /// DTO cho chi tiết giải thưởng (LUCKY_DRAW_MEISAI)
    /// </summary>
    public class LuckyDrawMeisaiDTO : ICrudEntity
    {
        public string LUCKY_DRAW_ID { get; set; }
        public string LUCKY_DRAW_MEISAI_NO { get; set; }
        public string LUCKY_DRAW_MEISAI_NAME { get; set; }
        public string LUCKY_DRAW_MEISAI_NAIYOU { get; set; }
        public string LUCKY_DRAW_MEISAI_IMG { get; set; }
        public double LUCKY_DRAW_MEISAI_RATE { get; set; }
        public double? LUCKY_DRAW_MEISAI_SURYO { get; set; }
        public int? LUCKY_DRAW_MEISAI_ONSHOU_NUM { get; set; }

        public string LUCKY_DRAW_MEISAI_BUNRUI { get; set; }
        public int? LUCKY_DRAW_MEISAI_MUKO_FLG { get; set; }

        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? KAISHI_DATE { get; set; }

        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? SYURYOU_DATE { get; set; }

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
            return $"{LUCKY_DRAW_ID}_{LUCKY_DRAW_MEISAI_NO}";
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

            if (string.IsNullOrWhiteSpace(LUCKY_DRAW_MEISAI_NO))
                return (false, "Mã giải thưởng không được để trống!");

            if (LUCKY_DRAW_MEISAI_NO.Length > 5)
                return (false, "Mã giải thưởng không được vượt quá 5 ký tự!");

            if (string.IsNullOrWhiteSpace(LUCKY_DRAW_MEISAI_BUNRUI))
                return (false, "Loại giải thưởng không được để trống!");

            if (LUCKY_DRAW_MEISAI_BUNRUI.Length > 50)
                return (false, "Loại giải thưởng không được vượt quá 50 ký tự!");

            if (LUCKY_DRAW_MEISAI_RATE < 0 || LUCKY_DRAW_MEISAI_RATE > 100)
                return (false, "Tỷ lệ phải từ 0 đến 100!");

            if (!string.IsNullOrWhiteSpace(LUCKY_DRAW_MEISAI_NAME) && LUCKY_DRAW_MEISAI_NAME.Length > 50)
                return (false, "Tên giải thưởng không được vượt quá 50 ký tự!");

            if (!string.IsNullOrWhiteSpace(LUCKY_DRAW_MEISAI_NAIYOU) && LUCKY_DRAW_MEISAI_NAIYOU.Length > 150)
                return (false, "Mô tả không được vượt quá 150 ký tự!");

            if (LUCKY_DRAW_MEISAI_SURYO.HasValue && LUCKY_DRAW_MEISAI_SURYO.Value < 0)
                return (false, "Số lượng phải lớn hơn hoặc bằng 0!");
            if (LUCKY_DRAW_MEISAI_ONSHOU_NUM.HasValue && LUCKY_DRAW_MEISAI_ONSHOU_NUM.Value < 0)
                return (false, "Thứ tự giải (Onshou Num) phải là số nguyên >= 0!");

            if (KAISHI_DATE.HasValue && SYURYOU_DATE.HasValue && KAISHI_DATE.Value > SYURYOU_DATE.Value)
                return (false, "Ngày bắt đầu phải trước ngày kết thúc!");

            return (true, string.Empty);
        }
    }

    /// <summary>
    /// Custom JSON Converter để xử lý nhiều định dạng DateTime
    /// </summary>
    public class FlexibleDateTimeConverter : JsonConverter
    {
        private static readonly string[] DateFormats = new[]
        {
            "dd/MM/yyyy",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fffZ"
        };

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType == JsonToken.Date)
                return reader.Value;

            if (reader.TokenType == JsonToken.String)
            {
                string dateString = reader.Value.ToString();

                if (string.IsNullOrWhiteSpace(dateString))
                    return null;

                foreach (var format in DateFormats)
                {
                    if (DateTime.TryParseExact(dateString, format,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime result))
                    {
                        return result;
                    }
                }

                if (DateTime.TryParse(dateString, out DateTime autoResult))
                {
                    return autoResult;
                }
            }

            throw new JsonSerializationException($"Unable to convert \"{reader.Value}\" to DateTime.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var dateTime = (DateTime)value;
            writer.WriteValue(dateTime.ToString("yyyy-MM-ddTHH:mm:ss"));
        }
    }
}