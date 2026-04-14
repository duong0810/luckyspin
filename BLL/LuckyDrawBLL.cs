using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using System.Globalization;
using System.Data.Linq;
using DAL;

namespace BLL
{
    public class LuckyDrawBLL
    {
        private readonly string _connectionString;
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        private const int MaxEventImageBytes = 50 * 1024 * 1024;
        private const int MaxMeisaiImageBytes = 3 * 1024 * 1024;

        public LuckyDrawBLL(string connectionString)
        {
            _connectionString = string.IsNullOrWhiteSpace(connectionString) ? throw new ArgumentNullException(nameof(connectionString)) : connectionString;
        }

        private DateTime VietnamNow() => TimeZoneInfo.ConvertTime(DateTime.Now, VietnamTimeZone);

        #region Events (LUCKY_DRAW)
        public List<LUCKY_DRAW> GetAllEvents()
        {
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                return db.LUCKY_DRAWs.ToList();
            }
        }

        public LUCKY_DRAW GetEventById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("LUCKY_DRAW_ID không được để trống", nameof(id));
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var e = db.LUCKY_DRAWs.FirstOrDefault(x => x.LUCKY_DRAW_ID == id);
                if (e == null) throw new InvalidOperationException("Không tìm thấy sự kiện");
                return e;
            }
        }

        public void CreateEvent(string eventId, string eventName, string bunrui, string title, string slogan, string torokuId, int slotNum, byte[] bgImage)
        {
            if (string.IsNullOrWhiteSpace(eventId)) throw new ArgumentException("LUCKY_DRAW_ID không được để trống");
            if (string.IsNullOrWhiteSpace(torokuId)) throw new ArgumentException("TOROKU_ID không được để trống");
            if (slotNum < 1 || slotNum > 100) throw new ArgumentException("LUCKY_DRAW_SLOT_NUM phải là số nguyên trong khoảng 1-100");

            if (eventId.Length > 50) throw new ArgumentException("LUCKY_DRAW_ID không được vượt quá 50 ký tự");
            if (torokuId.Length > 12) throw new ArgumentException("TOROKU_ID không được vượt quá 12 ký tự");
            if (!string.IsNullOrWhiteSpace(eventName) && eventName.Length > 250) throw new ArgumentException("LUCKY_DRAW_NAME không được vượt quá 250 ký tự");
            if (!string.IsNullOrWhiteSpace(bunrui) && bunrui.Length > 50) throw new ArgumentException("LUCKY_DRAW_BUNRUI không được vượt quá 50 ký tự");
            if (!string.IsNullOrWhiteSpace(title) && title.Length > 250) throw new ArgumentException("LUCKY_DRAW_TITLE không được vượt quá 250 ký tự");
            if (!string.IsNullOrWhiteSpace(slogan) && slogan.Length > 250) throw new ArgumentException("LUCKY_DRAW_SLOGAN không được vượt quá 250 ký tự");

            if (bgImage != null && bgImage.Length > MaxEventImageBytes) throw new ArgumentException($"Kích thước ảnh vượt quá {MaxEventImageBytes / (1024 * 1024)}MB");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                if (db.LUCKY_DRAWs.Any(e => e.LUCKY_DRAW_ID == eventId))
                    throw new InvalidOperationException("Sự kiện đã tồn tại");

                var vietnamNow = VietnamNow();

                var newEvent = new LUCKY_DRAW
                {
                    LUCKY_DRAW_ID = eventId.Trim(),
                    LUCKY_DRAW_NAME = string.IsNullOrWhiteSpace(eventName) ? null : eventName.Trim(),
                    LUCKY_DRAW_BUNRUI = string.IsNullOrWhiteSpace(bunrui) ? null : bunrui.Trim(),
                    LUCKY_DRAW_TITLE = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
                    LUCKY_DRAW_SLOGAN = string.IsNullOrWhiteSpace(slogan) ? null : slogan.Trim(),
                    LUCKY_DRAW_BG_IMG = bgImage != null ? new System.Data.Linq.Binary(bgImage) : null,
                    LUCKY_DRAW_SLOT_NUM = slotNum,
                    TOROKU_DATE = vietnamNow,
                    TOROKU_ID = torokuId.Trim(),
                    KOSIN_ID = torokuId.Trim()
                };

                db.LUCKY_DRAWs.InsertOnSubmit(newEvent);
                db.SubmitChanges();
            }
        }

        public void UpdateEvent(string id, string kosinId, string kosinNaiyou = null,
            string newName = null, string newBunrui = null, string newTitle = null, string newSlogan = null,
            int? newSlotNum = null, byte[] bgImage = null, bool bgImageProvided = false)
        {
            if (string.IsNullOrWhiteSpace(kosinId)) throw new ArgumentException("KOSIN_ID không được để trống khi cập nhật");
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("LUCKY_DRAW_ID không được để trống");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var luckyDraw = db.LUCKY_DRAWs.FirstOrDefault(e => e.LUCKY_DRAW_ID == id);
                if (luckyDraw == null) throw new InvalidOperationException("Không tìm thấy sự kiện");

                var vietnamNow = VietnamNow();

                if (!string.IsNullOrWhiteSpace(newName))
                {
                    if (newName.Length > 250) throw new ArgumentException("LUCKY_DRAW_NAME không được vượt quá 250 ký tự");
                    luckyDraw.LUCKY_DRAW_NAME = newName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(newBunrui))
                {
                    if (newBunrui.Length > 50) throw new ArgumentException("LUCKY_DRAW_BUNRUI không được vượt quá 50 ký tự");
                    luckyDraw.LUCKY_DRAW_BUNRUI = newBunrui.Trim();
                }

                if (!string.IsNullOrWhiteSpace(newTitle))
                {
                    if (newTitle.Length > 250) throw new ArgumentException("LUCKY_DRAW_TITLE không được vượt quá 250 ký tự");
                    luckyDraw.LUCKY_DRAW_TITLE = newTitle.Trim();
                }

                if (!string.IsNullOrWhiteSpace(newSlogan))
                {
                    if (newSlogan.Length > 250) throw new ArgumentException("LUCKY_DRAW_SLOGAN không được vượt quá 250 ký tự");
                    luckyDraw.LUCKY_DRAW_SLOGAN = newSlogan.Trim();
                }

                // slot num: nếu được truyền (nullable) -> validate vs hiện có wheel count
                if (newSlotNum.HasValue && newSlotNum.Value > 0)
                {
                    if (newSlotNum.Value < 1 || newSlotNum.Value > 100) throw new ArgumentException("LUCKY_DRAW_SLOT_NUM phải trong khoảng 1-100");

                    var currentWheelCount = db.LUCKY_DRAW_MEISAIs
                        .Count(m => m.LUCKY_DRAW_ID == id && m.LUCKY_DRAW_MEISAI_BUNRUI == "wheel" &&
                                    (m.LUCKY_DRAW_MEISAI_MUKO_FLG == 0 || m.LUCKY_DRAW_MEISAI_MUKO_FLG == null));
                    if (newSlotNum.Value < currentWheelCount)
                        throw new InvalidOperationException($"Không thể giảm số ô vòng quay xuống {newSlotNum.Value} vì hiện có {currentWheelCount} giải 'wheel'.");

                    luckyDraw.LUCKY_DRAW_SLOT_NUM = newSlotNum.Value;
                }

                // Image update logic
                if (bgImageProvided)
                {
                    if (bgImage != null)
                    {
                        if (bgImage.Length > MaxEventImageBytes) throw new ArgumentException($"Kích thước ảnh vượt quá {MaxEventImageBytes / (1024 * 1024)}MB");
                        luckyDraw.LUCKY_DRAW_BG_IMG = new System.Data.Linq.Binary(bgImage);
                    }
                    else
                    {
                        // empty -> delete image
                        luckyDraw.LUCKY_DRAW_BG_IMG = null;
                    }
                }

                luckyDraw.KOSIN_ID = kosinId.Trim();
                luckyDraw.KOSIN_DATE = vietnamNow;
                luckyDraw.KOSIN_NAIYOU = string.IsNullOrWhiteSpace(kosinNaiyou) ? luckyDraw.KOSIN_NAIYOU : kosinNaiyou.Trim();

                db.SubmitChanges();
            }
        }

        public void DeleteEvent(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("LUCKY_DRAW_ID không được để trống");
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var luckyDraw = db.LUCKY_DRAWs.FirstOrDefault(e => e.LUCKY_DRAW_ID == id);
                if (luckyDraw == null) throw new InvalidOperationException("Không tìm thấy sự kiện");

                var hasPrograms = db.LUCKY_DRAW_PROGRAMEs.Any(p => p.LUCKY_DRAW_ID == id);
                if (hasPrograms) throw new InvalidOperationException("Không thể xóa sự kiện đã có chương trình liên quan");

                var hasMeisai = db.LUCKY_DRAW_MEISAIs.Any(m => m.LUCKY_DRAW_ID == id);
                if (hasMeisai) throw new InvalidOperationException("Không thể xóa sự kiện đã có giải thưởng liên quan");

                db.LUCKY_DRAWs.DeleteOnSubmit(luckyDraw);
                db.SubmitChanges();
            }
        }
        #endregion

        #region Meisai (LUCKY_DRAW_MEISAI)
        public List<LUCKY_DRAW_MEISAI> GetAllMeisai(string luckyDrawId = null, string category = null, bool includeMuko = false)
        {
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var q = db.LUCKY_DRAW_MEISAIs.AsQueryable();
                if (!string.IsNullOrEmpty(luckyDrawId)) q = q.Where(m => m.LUCKY_DRAW_ID == luckyDrawId);
                if (!string.IsNullOrEmpty(category)) q = q.Where(m => m.LUCKY_DRAW_MEISAI_BUNRUI == category);
                if (!includeMuko) q = q.Where(m => m.LUCKY_DRAW_MEISAI_MUKO_FLG == 0 || m.LUCKY_DRAW_MEISAI_MUKO_FLG == null);
                return q.ToList();
            }
        }

        public LUCKY_DRAW_MEISAI GetMeisaiById(string luckyDrawId, string meisaiNo)
        {
            if (string.IsNullOrWhiteSpace(luckyDrawId) || string.IsNullOrWhiteSpace(meisaiNo))
                throw new ArgumentException("LUCKY_DRAW_ID và MEISAI_NO không được để trống");
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var m = db.LUCKY_DRAW_MEISAIs.FirstOrDefault(x => x.LUCKY_DRAW_ID == luckyDrawId && x.LUCKY_DRAW_MEISAI_NO == meisaiNo);
                if (m == null) throw new InvalidOperationException("Không tìm thấy giải thưởng");
                return m;
            }
        }

        public void CreateMeisai(string luckyDrawId, string meisaiNo, string meisaiName, string meisaiNaiyou,
            double rate, double? suryo, string meisaiBunrui, DateTime? kaishiDate, DateTime? syuryouDate,
            string torokuId, int? onshouNum, byte[] imageBytes)
        {
            if (string.IsNullOrWhiteSpace(luckyDrawId)) throw new ArgumentException("LUCKY_DRAW_ID không được để trống");
            if (string.IsNullOrWhiteSpace(meisaiNo)) throw new ArgumentException("LUCKY_DRAW_MEISAI_NO không được để trống");
            if (string.IsNullOrWhiteSpace(meisaiBunrui)) throw new ArgumentException("LUCKY_DRAW_MEISAI_BUNRUI không được để trống");
            if (string.IsNullOrWhiteSpace(torokuId)) throw new ArgumentException("TOROKU_ID không được để trống");
            if (rate < 0) throw new ArgumentException("LUCKY_DRAW_MEISAI_RATE phải là số >= 0");

            if (imageBytes != null && imageBytes.Length > MaxMeisaiImageBytes) throw new ArgumentException("Kích thước ảnh vượt quá 3MB");

            if (luckyDrawId.Length > 50) throw new ArgumentException("LUCKY_DRAW_ID không được vượt quá 50 ký tự");
            if (meisaiNo.Length > 5) throw new ArgumentException("LUCKY_DRAW_MEISAI_NO không được vượt quá 5 ký tự");
            if (meisaiBunrui.Length > 50) throw new ArgumentException("LUCKY_DRAW_MEISAI_BUNRUI không được vượt quá 50 ký tự");
            if (torokuId.Length > 12) throw new ArgumentException("TOROKU_ID không được vượt quá 12 ký tự");
            if (!string.IsNullOrWhiteSpace(meisaiName) && meisaiName.Length > 50) throw new ArgumentException("LUCKY_DRAW_MEISAI_NAME không được vượt quá 50 ký tự");
            if (!string.IsNullOrWhiteSpace(meisaiNaiyou) && meisaiNaiyou.Length > 150) throw new ArgumentException("LUCKY_DRAW_MEISAI_NAIYOU không được vượt quá 150 ký tự");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                if (db.LUCKY_DRAW_MEISAIs.Any(m => m.LUCKY_DRAW_ID == luckyDrawId && m.LUCKY_DRAW_MEISAI_NO == meisaiNo))
                    throw new InvalidOperationException("Giải thưởng đã tồn tại");

                var eventExists = db.LUCKY_DRAWs.FirstOrDefault(e => e.LUCKY_DRAW_ID == luckyDrawId);
                if (eventExists == null) throw new InvalidOperationException("Sự kiện không tồn tại");

                var currentMeisaiCount = db.LUCKY_DRAW_MEISAIs
                    .Count(m => m.LUCKY_DRAW_ID == luckyDrawId &&
                                (m.LUCKY_DRAW_MEISAI_MUKO_FLG == 0 || m.LUCKY_DRAW_MEISAI_MUKO_FLG == null));
                if (currentMeisaiCount >= eventExists.LUCKY_DRAW_SLOT_NUM)
                    throw new InvalidOperationException($"Đã đạt giới hạn số ô vòng quay! Tối đa {eventExists.LUCKY_DRAW_SLOT_NUM}");

                var vietnamNow = VietnamNow();

                var newMeisai = new LUCKY_DRAW_MEISAI
                {
                    LUCKY_DRAW_ID = luckyDrawId.Trim(),
                    LUCKY_DRAW_MEISAI_NO = meisaiNo.Trim(),
                    LUCKY_DRAW_MEISAI_BUNRUI = meisaiBunrui.Trim(),
                    LUCKY_DRAW_MEISAI_RATE = rate,
                    TOROKU_DATE = vietnamNow,
                    TOROKU_ID = torokuId.Trim(),
                    KOSIN_ID = torokuId.Trim(),
                    LUCKY_DRAW_MEISAI_NAME = string.IsNullOrWhiteSpace(meisaiName) ? null : meisaiName.Trim(),
                    LUCKY_DRAW_MEISAI_NAIYOU = string.IsNullOrWhiteSpace(meisaiNaiyou) ? null : meisaiNaiyou.Trim(),
                    LUCKY_DRAW_MEISAI_IMG = imageBytes != null ? new System.Data.Linq.Binary(imageBytes) : null,
                    LUCKY_DRAW_MEISAI_SURYO = suryo,
                    LUCKY_DRAW_MEISAI_ONSHOU_NUM = onshouNum,
                    LUCKY_DRAW_MEISAI_MUKO_FLG = 0,
                    KAISHI_DATE = kaishiDate,
                    SYURYOU_DATE = syuryouDate
                };

                var txOptions = new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.Serializable };
                using (var scope = new TransactionScope(TransactionScopeOption.Required, txOptions))
                {
                    // nếu event.bunrui == "A" kiểm tra tổng rate
                    if (!string.IsNullOrWhiteSpace(eventExists.LUCKY_DRAW_BUNRUI) &&
                        eventExists.LUCKY_DRAW_BUNRUI.Equals("A", StringComparison.OrdinalIgnoreCase))
                    {
                        var totalRate = db.LUCKY_DRAW_MEISAIs
                            .Where(m => m.LUCKY_DRAW_ID == luckyDrawId &&
                                        (m.LUCKY_DRAW_MEISAI_MUKO_FLG == 0 || m.LUCKY_DRAW_MEISAI_MUKO_FLG == null))
                            .Sum(m => (double?)m.LUCKY_DRAW_MEISAI_RATE) ?? 0.0;

                        if (totalRate + rate > 100.0) throw new InvalidOperationException($"Tổng phần trăm cho event vượt quá 100% (hiện tại {totalRate}%)");
                    }

                    db.LUCKY_DRAW_MEISAIs.InsertOnSubmit(newMeisai);
                    db.SubmitChanges();
                    scope.Complete();
                }
            }
        }

        public void UpdateMeisai(string luckyDrawId, string meisaiNo, string kosinId,
            string meisaiName = null, string meisaiNaiyou = null, string meisaiBunrui = null,
            double newRate = 0, double? newSuryo = null, int? newOnshouNum = null,
            DateTime? kaishi = null, DateTime? syuryou = null, int? mukoFlg = null,
            string kosinNaiyou = null, string meisaiImgBase64 = null, bool meisaiImgProvided = false)
        {
            if (string.IsNullOrWhiteSpace(kosinId)) throw new ArgumentException("KOSIN_ID không được để trống khi cập nhật");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var meisai = db.LUCKY_DRAW_MEISAIs.FirstOrDefault(m => m.LUCKY_DRAW_ID == luckyDrawId && m.LUCKY_DRAW_MEISAI_NO == meisaiNo);
                if (meisai == null) throw new InvalidOperationException("Không tìm thấy giải thưởng");

                var eventExists = db.LUCKY_DRAWs.FirstOrDefault(e => e.LUCKY_DRAW_ID == luckyDrawId);

                var txOptions = new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.Serializable };
                using (var scope = new TransactionScope(TransactionScopeOption.Required, txOptions))
                {
                    if (eventExists != null &&
                        !string.IsNullOrWhiteSpace(eventExists.LUCKY_DRAW_BUNRUI) &&
                        eventExists.LUCKY_DRAW_BUNRUI.Equals("A", StringComparison.OrdinalIgnoreCase))
                    {
                        var totalRate = db.LUCKY_DRAW_MEISAIs
                            .Where(m => m.LUCKY_DRAW_ID == luckyDrawId &&
                                        m.LUCKY_DRAW_MEISAI_NO != meisaiNo &&
                                        (m.LUCKY_DRAW_MEISAI_MUKO_FLG == 0 || m.LUCKY_DRAW_MEISAI_MUKO_FLG == null))
                            .Sum(m => (double?)m.LUCKY_DRAW_MEISAI_RATE) ?? 0.0;

                        if (totalRate + newRate > 100.0) throw new InvalidOperationException($"Tổng phần trăm cho event vượt quá 100% (hiện tại {totalRate}%)");
                    }

                    if (!string.IsNullOrWhiteSpace(meisaiName)) meisai.LUCKY_DRAW_MEISAI_NAME = meisaiName.Trim();
                    if (!string.IsNullOrWhiteSpace(meisaiNaiyou)) meisai.LUCKY_DRAW_MEISAI_NAIYOU = meisaiNaiyou.Trim();
                    if (!string.IsNullOrWhiteSpace(meisaiBunrui)) meisai.LUCKY_DRAW_MEISAI_BUNRUI = meisaiBunrui.Trim();

                    if (meisaiImgProvided)
                    {
                        if (!string.IsNullOrEmpty(meisaiImgBase64))
                        {
                            try
                            {
                                var bytes = Convert.FromBase64String(meisaiImgBase64);
                                if (bytes.Length > MaxMeisaiImageBytes) throw new ArgumentException("Kích thước ảnh vượt quá 3MB");
                                meisai.LUCKY_DRAW_MEISAI_IMG = new System.Data.Linq.Binary(bytes);
                            }
                            catch (FormatException) { throw new ArgumentException("Định dạng ảnh Base64 không hợp lệ"); }
                        }
                        else
                        {
                            meisai.LUCKY_DRAW_MEISAI_IMG = null;
                        }
                    }

                    meisai.LUCKY_DRAW_MEISAI_RATE = newRate;
                    meisai.LUCKY_DRAW_MEISAI_SURYO = newSuryo;
                    if (newOnshouNum.HasValue)
                    {
                        if (newOnshouNum.Value < 0) throw new ArgumentException("LUCKY_DRAW_MEISAI_ONSHOU_NUM phải là số >= 0");
                        meisai.LUCKY_DRAW_MEISAI_ONSHOU_NUM = newOnshouNum;
                    }

                    meisai.LUCKY_DRAW_MEISAI_MUKO_FLG = mukoFlg;
                    meisai.KAISHI_DATE = kaishi;
                    meisai.SYURYOU_DATE = syuryou;

                    var vietnamNow = VietnamNow();
                    meisai.KOSIN_ID = kosinId.Trim();
                    meisai.KOSIN_DATE = vietnamNow;
                    meisai.KOSIN_NAIYOU = string.IsNullOrWhiteSpace(kosinNaiyou) ? meisai.KOSIN_NAIYOU : kosinNaiyou.Trim();

                    db.SubmitChanges();
                    scope.Complete();
                }
            }
        }

        public void DeleteMeisai(string luckyDrawId, string meisaiNo)
        {
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var meisai = db.LUCKY_DRAW_MEISAIs.FirstOrDefault(m => m.LUCKY_DRAW_ID == luckyDrawId && m.LUCKY_DRAW_MEISAI_NO == meisaiNo);
                if (meisai == null) throw new InvalidOperationException("Không tìm thấy giải thưởng");

                var vietnamNow = VietnamNow();
                meisai.LUCKY_DRAW_MEISAI_MUKO_FLG = 1;
                meisai.KOSIN_DATE = vietnamNow;
                meisai.KOSIN_NAIYOU = "Đã xóa";
                if (string.IsNullOrWhiteSpace(meisai.KOSIN_ID)) meisai.KOSIN_ID = "system";

                db.SubmitChanges();
            }
        }
        #endregion

        #region Programs (LUCKY_DRAW_PROGRAME)
        public List<LUCKY_DRAW_PROGRAME> GetAllPrograms(string luckyDrawId = null)
        {
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var q = db.LUCKY_DRAW_PROGRAMEs.AsQueryable();
                if (!string.IsNullOrWhiteSpace(luckyDrawId)) q = q.Where(p => p.LUCKY_DRAW_ID == luckyDrawId);
                return q.OrderByDescending(p => p.TOROKU_DATE).ToList();
            }
        }

        public LUCKY_DRAW_PROGRAME GetProgramById(string id)
        {
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var p = db.LUCKY_DRAW_PROGRAMEs.FirstOrDefault(x => x.LUCKY_DRAW_PROGRAME_ID == id);
                if (p == null) throw new InvalidOperationException("Không tìm thấy chương trình");
                return p;
            }
        }

        public void CreateProgram(string programeId, string programeName, string luckyDrawId,
            DateTime? kaishi, DateTime? syuryou, string slogan, string torokuId)
        {
            if (string.IsNullOrWhiteSpace(programeId)) throw new ArgumentException("LUCKY_DRAW_PROGRAME_ID required");
            if (string.IsNullOrWhiteSpace(luckyDrawId)) throw new ArgumentException("LUCKY_DRAW_ID required");
            if (string.IsNullOrWhiteSpace(torokuId)) throw new ArgumentException("TOROKU_ID required");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                if (db.LUCKY_DRAW_PROGRAMEs.Any(p => p.LUCKY_DRAW_PROGRAME_ID == programeId)) throw new InvalidOperationException("Chương trình đã tồn tại");
                if (!db.LUCKY_DRAWs.Any(e => e.LUCKY_DRAW_ID == luckyDrawId)) throw new InvalidOperationException("Sự kiện không tồn tại");

                var vietnamNow = VietnamNow();
                var prog = new LUCKY_DRAW_PROGRAME
                {
                    LUCKY_DRAW_PROGRAME_ID = programeId.Trim(),
                    LUCKY_DRAW_ID = luckyDrawId.Trim(),
                    LUCKY_DRAW_PROGRAME_NAME = string.IsNullOrWhiteSpace(programeName) ? null : programeName.Trim(),
                    KAISHI_DATE = kaishi,
                    SYURYOU_DATE = syuryou,
                    PROGRAME_SLOGAN = string.IsNullOrWhiteSpace(slogan) ? null : slogan.Trim(),
                    TOROKU_DATE = vietnamNow,
                    TOROKU_ID = torokuId.Trim(),
                    KOSIN_ID = torokuId.Trim()
                };

                db.LUCKY_DRAW_PROGRAMEs.InsertOnSubmit(prog);
                db.SubmitChanges();
            }
        }

        public void UpdateProgram(string id, string kosinId, string kosinNaiyou = null,
            string newName = null, DateTime? kaishi = null, DateTime? syuryou = null, string slogan = null)
        {
            if (string.IsNullOrWhiteSpace(kosinId)) throw new ArgumentException("KOSIN_ID required");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var prog = db.LUCKY_DRAW_PROGRAMEs.FirstOrDefault(p => p.LUCKY_DRAW_PROGRAME_ID == id);
                if (prog == null) throw new InvalidOperationException("Không tìm thấy chương trình");

                if (!string.IsNullOrWhiteSpace(newName)) prog.LUCKY_DRAW_PROGRAME_NAME = newName.Trim();
                if (!string.IsNullOrWhiteSpace(slogan)) prog.PROGRAME_SLOGAN = slogan.Trim();
                prog.KAISHI_DATE = kaishi;
                prog.SYURYOU_DATE = syuryou;

                prog.KOSIN_ID = kosinId.Trim();
                prog.KOSIN_DATE = VietnamNow();
                prog.KOSIN_NAIYOU = string.IsNullOrWhiteSpace(kosinNaiyou) ? prog.KOSIN_NAIYOU : kosinNaiyou.Trim();

                db.SubmitChanges();
            }
        }

        public void DeleteProgram(string id)
        {
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var prog = db.LUCKY_DRAW_PROGRAMEs.FirstOrDefault(p => p.LUCKY_DRAW_PROGRAME_ID == id);
                if (prog == null) throw new InvalidOperationException("Không tìm thấy chương trình");

                var hasHistory = db.LUCKY_DRAW_PROGRAME_HISTORies.Any(h => h.LUCKY_DRAW_PROGRAME_ID == id);
                if (hasHistory) throw new InvalidOperationException("Không thể xóa chương trình đã có lịch sử trúng thưởng");

                db.LUCKY_DRAW_PROGRAMEs.DeleteOnSubmit(prog);
                db.SubmitChanges();
            }
        }
        #endregion

        #region Spin
        public (LUCKY_DRAW_MEISAI winner, double randomValue) SpinVoucher(string luckyDrawId = null, string category = null, bool includeMuko = false)
        {
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var query = db.LUCKY_DRAW_MEISAIs.Where(m => m.LUCKY_DRAW_MEISAI_RATE > 0);
                if (!string.IsNullOrWhiteSpace(category)) query = query.Where(m => m.LUCKY_DRAW_MEISAI_BUNRUI == category);
                if (!string.IsNullOrWhiteSpace(luckyDrawId)) query = query.Where(m => m.LUCKY_DRAW_ID == luckyDrawId);
                if (!includeMuko) query = query.Where(m => m.LUCKY_DRAW_MEISAI_MUKO_FLG == 0 || m.LUCKY_DRAW_MEISAI_MUKO_FLG == null);

                var meisaiList = query.OrderBy(m => m.TOROKU_DATE).ToList();
                if (meisaiList.Count == 0) throw new InvalidOperationException("Không có giải thưởng khả dụng");

                double totalRate = meisaiList.Sum(m => m.LUCKY_DRAW_MEISAI_RATE);
                if (totalRate == 0) throw new InvalidOperationException("Tổng xác suất bằng 0");

                var random = new Random();
                double randomValue = random.NextDouble() * 100;
                double accumulator = 0;
                LUCKY_DRAW_MEISAI winner = null;
                foreach (var meisai in meisaiList)
                {
                    double normalizedProb = (meisai.LUCKY_DRAW_MEISAI_RATE / totalRate) * 100;
                    accumulator += normalizedProb;
                    if (randomValue <= accumulator)
                    {
                        winner = meisai;
                        break;
                    }
                }
                if (winner == null) winner = meisaiList.Last();
                return (winner, randomValue);
            }
        }

        public LUCKY_DRAW_MEISAI ConfirmSpin(double randomValue, string luckyDrawId = null, string category = null, bool includeMuko = false)
        {
            // Logic same as Spin but using provided randomValue — returns winner
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var query = db.LUCKY_DRAW_MEISAIs.Where(m => m.LUCKY_DRAW_MEISAI_RATE > 0);
                if (!string.IsNullOrWhiteSpace(category)) query = query.Where(m => m.LUCKY_DRAW_MEISAI_BUNRUI == category);
                if (!string.IsNullOrWhiteSpace(luckyDrawId)) query = query.Where(m => m.LUCKY_DRAW_ID == luckyDrawId);
                if (!includeMuko) query = query.Where(m => m.LUCKY_DRAW_MEISAI_MUKO_FLG == 0 || m.LUCKY_DRAW_MEISAI_MUKO_FLG == null);

                var meisaiList = query.OrderBy(m => m.TOROKU_DATE).ToList();
                if (meisaiList.Count == 0) throw new InvalidOperationException("Không có giải thưởng khả dụng");

                double totalRate = meisaiList.Sum(m => m.LUCKY_DRAW_MEISAI_RATE);
                if (totalRate == 0) throw new InvalidOperationException("Tổng xác suất bằng 0");

                double accumulator = 0;
                LUCKY_DRAW_MEISAI winner = null;
                foreach (var meisai in meisaiList)
                {
                    double normalizedProb = (meisai.LUCKY_DRAW_MEISAI_RATE / totalRate) * 100;
                    accumulator += normalizedProb;
                    if (randomValue <= accumulator)
                    {
                        winner = meisai;
                        break;
                    }
                }
                if (winner == null) winner = meisaiList.Last();
                return winner;
            }
        }
        #endregion

        #region History (LUCKY_DRAW_PROGRAME_HISTORY)
        // dto-like approach: pass a history entity plus optional base64 strings for images
        public decimal SaveHistory(LUCKY_DRAW_PROGRAME_HISTORY historyEntity, string torokuId = null,
    string maeJisshiBgBase64 = null, string atoJisshiBgBase64 = null, bool isCreate = true, double? deductQty = null)
        {
            if (historyEntity == null) throw new ArgumentNullException(nameof(historyEntity));

            // Log đầu vào để debug (sẽ xuất ra Output window)
            //System.Diagnostics.Trace.WriteLine($"[SaveHistory] Called: Program={historyEntity.LUCKY_DRAW_PROGRAME_ID}, Event={historyEntity.LUCKY_DRAW_ID}, KENSYO_DRAW_MEISAI_ID={historyEntity.KENSYO_DRAW_MEISAI_ID}, history.KENSYO_DRAW_MEISAI_SURYO={historyEntity.KENSYO_DRAW_MEISAI_SURYO}, deductQty={deductQty}, isCreate={isCreate}");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var vietnamNow = VietnamNow();
                var effectiveTorokuId = string.IsNullOrWhiteSpace(torokuId) ? "admin" : torokuId.Trim();

                // helper decode
                Func<string, System.Data.Linq.Binary> decode = (b64) =>
                {
                    if (string.IsNullOrWhiteSpace(b64)) return null;
                    var s = b64.Trim();
                    if (s.Contains(",")) s = s.Substring(s.IndexOf(",") + 1);
                    s = s.Replace("\r", "").Replace("\n", "").Replace(" ", "");
                    var bytes = Convert.FromBase64String(s);
                    if (bytes.Length > MaxMeisaiImageBytes) throw new ArgumentException("Kích thước ảnh vượt quá 3MB");
                    return new System.Data.Linq.Binary(bytes);
                };

                if (isCreate && historyEntity.PROGRAME_JISSHI_ID == 0)
                {
                    // validate FK
                    if (!db.LUCKY_DRAW_PROGRAMEs.Any(p => p.LUCKY_DRAW_PROGRAME_ID == historyEntity.LUCKY_DRAW_PROGRAME_ID))
                        throw new InvalidOperationException("Chương trình không tồn tại");
                    if (!db.LUCKY_DRAWs.Any(e => e.LUCKY_DRAW_ID == historyEntity.LUCKY_DRAW_ID))
                        throw new InvalidOperationException("Sự kiện không tồn tại");

                    // mặc định trừ 1, nhưng cho phép caller truyền deductQty có chủ ý
                    double qtyToDeduct = 1.0;
                    if (deductQty.HasValue)
                    {
                        if (deductQty.Value <= 0) throw new ArgumentException("deductQty phải là số > 0 khi truyền vào");
                        qtyToDeduct = deductQty.Value;
                    }

                    // Log quyết định qtyToDeduct
                    //System.Diagnostics.Trace.WriteLine($"[SaveHistory] Effective qtyToDeduct={qtyToDeduct}");

                    var maxId = db.LUCKY_DRAW_PROGRAME_HISTORies.Any() ? db.LUCKY_DRAW_PROGRAME_HISTORies.Max(h => h.PROGRAME_JISSHI_ID) : 0m;
                    var newJisshiId = maxId + 1m;

                    var txOptions = new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.Serializable };
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, txOptions))
                    {
                        LUCKY_DRAW_MEISAI meisai = null;
                        if (!string.IsNullOrWhiteSpace(historyEntity.KENSYO_DRAW_MEISAI_ID))
                        {
                            meisai = db.LUCKY_DRAW_MEISAIs
                                .FirstOrDefault(m => m.LUCKY_DRAW_ID == historyEntity.LUCKY_DRAW_ID && m.LUCKY_DRAW_MEISAI_NO == historyEntity.KENSYO_DRAW_MEISAI_ID);

                            if (meisai == null) throw new InvalidOperationException("Không tìm thấy giải thưởng để trừ số lượng");
                            if (!meisai.LUCKY_DRAW_MEISAI_SURYO.HasValue) throw new InvalidOperationException("Số lượng giải thưởng chưa được thiết lập trên Meisai");

                            // Log trước khi trừ
                            //System.Diagnostics.Trace.WriteLine($"[SaveHistory] Before deduct: Meisai={meisai.LUCKY_DRAW_ID}/{meisai.LUCKY_DRAW_MEISAI_NO} SURYO={meisai.LUCKY_DRAW_MEISAI_SURYO.Value}");

                            if (meisai.LUCKY_DRAW_MEISAI_SURYO.Value < qtyToDeduct) throw new InvalidOperationException($"Không đủ số lượng giải ({meisai.LUCKY_DRAW_MEISAI_SURYO.Value}) để trừ {qtyToDeduct}.");

                            meisai.LUCKY_DRAW_MEISAI_SURYO = meisai.LUCKY_DRAW_MEISAI_SURYO - qtyToDeduct;
                            meisai.KOSIN_ID = effectiveTorokuId;
                            meisai.KOSIN_DATE = vietnamNow;
                            meisai.KOSIN_NAIYOU = $"Trừ {qtyToDeduct} do trúng thưởng (ProgramJisshiId={newJisshiId})";

                            // Log sau khi trừ
                            //System.Diagnostics.Trace.WriteLine($"[SaveHistory] After deduct: Meisai={meisai.LUCKY_DRAW_ID}/{meisai.LUCKY_DRAW_MEISAI_NO} NewSURYO={meisai.LUCKY_DRAW_MEISAI_SURYO}");

                            // Extra warning nếu caller vô tình truyền toàn bộ
                            if (Math.Abs((double)meisai.LUCKY_DRAW_MEISAI_SURYO.Value) < 0.0000001 || Math.Abs(qtyToDeduct - (double)(meisai.LUCKY_DRAW_MEISAI_SURYO + qtyToDeduct).Value) < 0.0000001)
                            {
                                //System.Diagnostics.Trace.WriteLine($"[SaveHistory][WARN] qtyToDeduct equals previous full SURYO for {meisai.LUCKY_DRAW_ID}/{meisai.LUCKY_DRAW_MEISAI_NO}. Caller may have passed full amount.");
                            }

                            db.SubmitChanges();
                        }

                        var newHistory = new LUCKY_DRAW_PROGRAME_HISTORY
                        {
                            PROGRAME_JISSHI_ID = newJisshiId,
                            LUCKY_DRAW_PROGRAME_ID = historyEntity.LUCKY_DRAW_PROGRAME_ID?.Trim(),
                            LUCKY_DRAW_ID = historyEntity.LUCKY_DRAW_ID?.Trim(),
                            TOROKU_DATE = vietnamNow,
                            TOROKU_ID = effectiveTorokuId,
                            MITUMORI_NO_SANSYO = historyEntity.MITUMORI_NO_SANSYO,
                            KOKAKU_HITO_NAME = historyEntity.KOKAKU_HITO_NAME,
                            KOKAKU_HITO_PHONE = historyEntity.KOKAKU_HITO_PHONE,
                            KOKAKU_HITO_ADDRESS = historyEntity.KOKAKU_HITO_ADDRESS,
                            RATE_LOG_SPLIT = historyEntity.RATE_LOG_SPLIT,
                            KENSYO_DRAW_MEISAI_ID = historyEntity.KENSYO_DRAW_MEISAI_ID,
                            KENSYO_VOUCHER_CODE = historyEntity.KENSYO_VOUCHER_CODE,
                            KENSYO_DRAW_MEISAI_SURYO = !string.IsNullOrWhiteSpace(historyEntity.KENSYO_DRAW_MEISAI_ID) ? (double?)qtyToDeduct : historyEntity.KENSYO_DRAW_MEISAI_SURYO,
                            KAISHI_DATE = historyEntity.KAISHI_DATE,
                            SYURYOU_DATE = historyEntity.SYURYOU_DATE,
                            KOKAKU_NAME_SYUTOKU = historyEntity.KOKAKU_NAME_SYUTOKU,
                            KOKAKU_PHONE_SYUTOKU = historyEntity.KOKAKU_PHONE_SYUTOKU,
                            KOKAKU_SHOUMEISYO_NO_SYUTOKU = historyEntity.KOKAKU_SHOUMEISYO_NO_SYUTOKU,
                            KOKAKU_ADDRESS_SYUTOKU = historyEntity.KOKAKU_ADDRESS_SYUTOKU,
                            KOKAKU_SYUTOKU_DATE = historyEntity.KOKAKU_SYUTOKU_DATE,
                            TANTO_SYA_NAME = historyEntity.TANTO_SYA_NAME,
                            MITUMORI_NO_SYUTOKU_SANSYO = historyEntity.MITUMORI_NO_SYUTOKU_SANSYO
                        };

                        if (historyEntity.JISSHI_KAISHI_DATE.HasValue) newHistory.JISSHI_KAISHI_DATE = historyEntity.JISSHI_KAISHI_DATE;
                        if (!string.IsNullOrWhiteSpace(maeJisshiBgBase64)) newHistory.MAE_JISSHI_BG_IMG = decode(maeJisshiBgBase64);
                        else if (!string.IsNullOrWhiteSpace(historyEntity.MAE_JISSHI_BG_IMG?.ToString())) newHistory.MAE_JISSHI_BG_IMG = historyEntity.MAE_JISSHI_BG_IMG;

                        if (historyEntity.JISSHI_SYURYOU_DATE.HasValue) newHistory.JISSHI_SYURYOU_DATE = historyEntity.JISSHI_SYURYOU_DATE;
                        if (!string.IsNullOrWhiteSpace(atoJisshiBgBase64)) newHistory.ATO_JISSHI_BG_IMG = decode(atoJisshiBgBase64);
                        else if (!string.IsNullOrWhiteSpace(historyEntity.ATO_JISSHI_BG_IMG?.ToString())) newHistory.ATO_JISSHI_BG_IMG = historyEntity.ATO_JISSHI_BG_IMG;

                        db.LUCKY_DRAW_PROGRAME_HISTORies.InsertOnSubmit(newHistory);
                        db.SubmitChanges();

                        // Log record inserted
                        //System.Diagnostics.Trace.WriteLine($"[SaveHistory] Inserted history JisshiId={newJisshiId}, HistorySuryo={newHistory.KENSYO_DRAW_MEISAI_SURYO}");

                        scope.Complete();

                        return newJisshiId;
                    }
                }
                else
                {
                    // Update existing history (keeps original behavior)
                    var history = db.LUCKY_DRAW_PROGRAME_HISTORies
                        .FirstOrDefault(h => h.PROGRAME_JISSHI_ID == historyEntity.PROGRAME_JISSHI_ID &&
                                             (string.IsNullOrEmpty(historyEntity.LUCKY_DRAW_PROGRAME_ID) || h.LUCKY_DRAW_PROGRAME_ID == historyEntity.LUCKY_DRAW_PROGRAME_ID));

                    if (history == null) throw new InvalidOperationException("Không tìm thấy bản ghi lịch sử");

                    // Update fields if provided (simple semantics)
                    if (!string.IsNullOrWhiteSpace(historyEntity.MITUMORI_NO_SANSYO)) history.MITUMORI_NO_SANSYO = historyEntity.MITUMORI_NO_SANSYO.Trim();
                    if (!string.IsNullOrWhiteSpace(historyEntity.KOKAKU_HITO_NAME)) history.KOKAKU_HITO_NAME = historyEntity.KOKAKU_HITO_NAME.Trim();
                    if (!string.IsNullOrWhiteSpace(historyEntity.KOKAKU_HITO_PHONE)) history.KOKAKU_HITO_PHONE = historyEntity.KOKAKU_HITO_PHONE.Trim();
                    if (!string.IsNullOrWhiteSpace(historyEntity.KOKAKU_HITO_ADDRESS)) history.KOKAKU_HITO_ADDRESS = historyEntity.KOKAKU_HITO_ADDRESS.Trim();
                    if (!string.IsNullOrWhiteSpace(historyEntity.RATE_LOG_SPLIT)) history.RATE_LOG_SPLIT = historyEntity.RATE_LOG_SPLIT;
                    if (!string.IsNullOrWhiteSpace(historyEntity.KENSYO_DRAW_MEISAI_ID)) history.KENSYO_DRAW_MEISAI_ID = historyEntity.KENSYO_DRAW_MEISAI_ID.Trim();
                    if (!string.IsNullOrWhiteSpace(historyEntity.KENSYO_VOUCHER_CODE)) history.KENSYO_VOUCHER_CODE = historyEntity.KENSYO_VOUCHER_CODE.Trim();
                    if (historyEntity.KENSYO_DRAW_MEISAI_SURYO.HasValue) history.KENSYO_DRAW_MEISAI_SURYO = historyEntity.KENSYO_DRAW_MEISAI_SURYO;
                    if (historyEntity.KAISHI_DATE.HasValue) history.KAISHI_DATE = historyEntity.KAISHI_DATE;
                    if (historyEntity.SYURYOU_DATE.HasValue) history.SYURYOU_DATE = historyEntity.SYURYOU_DATE;
                    if (!string.IsNullOrWhiteSpace(historyEntity.KOKAKU_NAME_SYUTOKU)) history.KOKAKU_NAME_SYUTOKU = historyEntity.KOKAKU_NAME_SYUTOKU.Trim();
                    if (!string.IsNullOrWhiteSpace(historyEntity.KOKAKU_PHONE_SYUTOKU)) history.KOKAKU_PHONE_SYUTOKU = historyEntity.KOKAKU_PHONE_SYUTOKU.Trim();
                    if (!string.IsNullOrWhiteSpace(historyEntity.KOKAKU_SHOUMEISYO_NO_SYUTOKU)) history.KOKAKU_SHOUMEISYO_NO_SYUTOKU = historyEntity.KOKAKU_SHOUMEISYO_NO_SYUTOKU.Trim();
                    if (!string.IsNullOrWhiteSpace(historyEntity.KOKAKU_ADDRESS_SYUTOKU)) history.KOKAKU_ADDRESS_SYUTOKU = historyEntity.KOKAKU_ADDRESS_SYUTOKU.Trim();
                    if (historyEntity.KOKAKU_SYUTOKU_DATE.HasValue) history.KOKAKU_SYUTOKU_DATE = historyEntity.KOKAKU_SYUTOKU_DATE;
                    if (!string.IsNullOrWhiteSpace(historyEntity.TANTO_SYA_NAME)) history.TANTO_SYA_NAME = historyEntity.TANTO_SYA_NAME.Trim();
                    if (!string.IsNullOrWhiteSpace(historyEntity.MITUMORI_NO_SYUTOKU_SANSYO)) history.MITUMORI_NO_SYUTOKU_SANSYO = historyEntity.MITUMORI_NO_SYUTOKU_SANSYO.Trim();

                    if (historyEntity.JISSHI_KAISHI_DATE.HasValue) history.JISSHI_KAISHI_DATE = historyEntity.JISSHI_KAISHI_DATE;
                    if (historyEntity.JISSHI_SYURYOU_DATE.HasValue) history.JISSHI_SYURYOU_DATE = historyEntity.JISSHI_SYURYOU_DATE;

                    if (!string.IsNullOrWhiteSpace(maeJisshiBgBase64)) history.MAE_JISSHI_BG_IMG = decode(maeJisshiBgBase64);
                    if (!string.IsNullOrWhiteSpace(atoJisshiBgBase64)) history.ATO_JISSHI_BG_IMG = decode(atoJisshiBgBase64);

                    db.SubmitChanges();
                    return history.PROGRAME_JISSHI_ID;
                }
            }
        }

        public List<LUCKY_DRAW_PROGRAME_HISTORY> GetHistory(string phone = null, string luckyDrawId = null, bool includeImages = false)
        {
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var q = db.LUCKY_DRAW_PROGRAME_HISTORies.AsQueryable();
                if (!string.IsNullOrWhiteSpace(phone)) q = q.Where(h => h.KOKAKU_HITO_PHONE.Contains(phone));
                if (!string.IsNullOrWhiteSpace(luckyDrawId)) q = q.Where(h => h.LUCKY_DRAW_ID == luckyDrawId);
                var list = q.OrderByDescending(h => h.TOROKU_DATE).ToList();

                if (!includeImages)
                {
                    // strip images to reduce payload (set null)
                    foreach (var h in list) { h.MAE_JISSHI_BG_IMG = null; h.ATO_JISSHI_BG_IMG = null; }
                }

                return list;
            }
        }

        public List<LUCKY_DRAW_PROGRAME_HISTORY> GetHistoryByProgram(string programId, bool includeImages = false)
        {
            if (string.IsNullOrWhiteSpace(programId)) throw new ArgumentException("LUCKY_DRAW_PROGRAME_ID không được để trống");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var list = db.LUCKY_DRAW_PROGRAME_HISTORies.Where(h => h.LUCKY_DRAW_PROGRAME_ID == programId)
                    .OrderByDescending(h => h.TOROKU_DATE).ToList();

                if (!includeImages)
                {
                    foreach (var h in list) { h.MAE_JISSHI_BG_IMG = null; h.ATO_JISSHI_BG_IMG = null; }
                }

                return list;
            }
        }
        #endregion

        #region Upload images (event / meisai)
        public void UploadEventImage(string id, string kosinId, byte[] fileBytes, string kosinNaiyou = null)
        {
            if (string.IsNullOrWhiteSpace(kosinId)) throw new ArgumentException("KOSIN_ID không được để trống khi upload ảnh");
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("LUCKY_DRAW_ID không được để trống");
            if (fileBytes == null) throw new ArgumentException("File bytes required");

            if (fileBytes.Length > MaxEventImageBytes) throw new ArgumentException($"Kích thước file vượt quá {MaxEventImageBytes / (1024 * 1024)}MB");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var luckyDraw = db.LUCKY_DRAWs.FirstOrDefault(e => e.LUCKY_DRAW_ID == id);
                if (luckyDraw == null) throw new InvalidOperationException("Không tìm thấy sự kiện");

                luckyDraw.LUCKY_DRAW_BG_IMG = new System.Data.Linq.Binary(fileBytes);
                var vietnamNow = VietnamNow();
                luckyDraw.KOSIN_ID = kosinId.Trim();
                luckyDraw.KOSIN_DATE = vietnamNow;
                luckyDraw.KOSIN_NAIYOU = string.IsNullOrWhiteSpace(kosinNaiyou) ? $"Upload ảnh nền" : kosinNaiyou.Trim();
                db.SubmitChanges();
            }
        }

        public void UploadMeisaiImage(string luckyDrawId, string meisaiNo, string kosinId, byte[] fileBytes, string kosinNaiyou = null)
        {
            if (string.IsNullOrWhiteSpace(kosinId)) throw new ArgumentException("KOSIN_ID không được để trống");
            if (fileBytes == null) throw new ArgumentException("File bytes required");
            if (fileBytes.Length > MaxMeisaiImageBytes) throw new ArgumentException("Kích thước file vượt quá 3MB");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var meisai = db.LUCKY_DRAW_MEISAIs.FirstOrDefault(m => m.LUCKY_DRAW_ID == luckyDrawId && m.LUCKY_DRAW_MEISAI_NO == meisaiNo);
                if (meisai == null) throw new InvalidOperationException("Không tìm thấy giải thưởng");

                meisai.LUCKY_DRAW_MEISAI_IMG = new System.Data.Linq.Binary(fileBytes);
                var vietnamNow = VietnamNow();
                meisai.KOSIN_ID = kosinId.Trim();
                meisai.KOSIN_DATE = vietnamNow;
                meisai.KOSIN_NAIYOU = string.IsNullOrWhiteSpace(kosinNaiyou) ? $"Upload ảnh giải thưởng" : kosinNaiyou.Trim();

                db.SubmitChanges();
            }
        }
        #endregion

        #region Kokaku (LUCKY_DRAW_KOKAKU)
        public void SaveKokaku(LUCKY_DRAW_KOKAKU entity, string torokuId = null, string kokakuImgBase64 = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.LUCKY_DRAW_ID)) throw new ArgumentException("LUCKY_DRAW_ID không được để trống");
            if (string.IsNullOrWhiteSpace(entity.KOKAKU_HITO_PHONE)) throw new ArgumentException("KOKAKU_HITO_PHONE không được để trống");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                if (!db.LUCKY_DRAWs.Any(e => e.LUCKY_DRAW_ID == entity.LUCKY_DRAW_ID)) throw new InvalidOperationException("Sự kiện không tồn tại");

                if (db.LUCKY_DRAW_KOKAKUs.Any(k => k.LUCKY_DRAW_ID == entity.LUCKY_DRAW_ID && k.KOKAKU_HITO_PHONE == entity.KOKAKU_HITO_PHONE))
                    throw new InvalidOperationException("Bản ghi kokaku đã tồn tại cho event + số điện thoại này");

                System.Data.Linq.Binary decoded = null;
                if (!string.IsNullOrWhiteSpace(kokakuImgBase64))
                {
                    var s = kokakuImgBase64.Trim();
                    if (s.Contains(",")) s = s.Substring(s.IndexOf(",") + 1);
                    s = s.Replace("\r", "").Replace("\n", "").Replace(" ", "");
                    var bytes = Convert.FromBase64String(s);
                    if (bytes.Length > MaxMeisaiImageBytes) throw new ArgumentException("Kích thước ảnh vượt quá 3MB");
                    decoded = new System.Data.Linq.Binary(bytes);
                }

                var vietnamNow = VietnamNow();
                var effToroku = string.IsNullOrWhiteSpace(torokuId) ? "admin" : torokuId.Trim();

                var ent = new LUCKY_DRAW_KOKAKU
                {
                    LUCKY_DRAW_ID = entity.LUCKY_DRAW_ID.Trim(),
                    KOKAKU_HITO_PHONE = entity.KOKAKU_HITO_PHONE.Trim(),
                    KOKAKU_HITO_NAME = string.IsNullOrWhiteSpace(entity.KOKAKU_HITO_NAME) ? null : entity.KOKAKU_HITO_NAME.Trim(),
                    KOKAKU_HITO_IMG = decoded,
                    KOKAKU_ADDRESS = string.IsNullOrWhiteSpace(entity.KOKAKU_ADDRESS) ? null : entity.KOKAKU_ADDRESS.Trim(),
                    KOKAKU_SHOUMEISYO_NO = string.IsNullOrWhiteSpace(entity.KOKAKU_SHOUMEISYO_NO) ? null : entity.KOKAKU_SHOUMEISYO_NO.Trim(),
                    KOKAKU_MUKO_FLG = entity.KOKAKU_MUKO_FLG ?? 0,
                    TOROKU_DATE = vietnamNow,
                    TOROKU_ID = effToroku,
                    KOSIN_NAIYOU = string.IsNullOrWhiteSpace(entity.KOSIN_NAIYOU) ? null : entity.KOSIN_NAIYOU,
                    KOSIN_DATE = string.IsNullOrWhiteSpace(entity.KOSIN_ID) ? (DateTime?)null : (entity.KOSIN_DATE ?? vietnamNow),
                    KOSIN_ID = effToroku,
                    KAISHI_DATE = entity.KAISHI_DATE,
                    SYURYOU_DATE = entity.SYURYOU_DATE
                };

                db.LUCKY_DRAW_KOKAKUs.InsertOnSubmit(ent);
                db.SubmitChanges();
            }
        }

        public List<LUCKY_DRAW_KOKAKU> GetKokakuByEvent(string eventId, bool includeMuko = false)
        {
            if (string.IsNullOrWhiteSpace(eventId)) throw new ArgumentException("eventId không được để trống");
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var q = db.LUCKY_DRAW_KOKAKUs.Where(k => k.LUCKY_DRAW_ID == eventId);
                if (!includeMuko) q = q.Where(k => k.KOKAKU_MUKO_FLG == null || k.KOKAKU_MUKO_FLG == 0);
                return q.OrderByDescending(k => k.TOROKU_DATE).ToList();
            }
        }

        public void UpdateKokaku(string eventId, string phone, string kosinId, LUCKY_DRAW_KOKAKU patch)
        {
            if (string.IsNullOrWhiteSpace(kosinId)) throw new ArgumentException("KOSIN_ID không được để trống khi cập nhật");
            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("eventId và phone không được để trống");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var item = db.LUCKY_DRAW_KOKAKUs.FirstOrDefault(k => k.LUCKY_DRAW_ID == eventId && k.KOKAKU_HITO_PHONE == phone)
                           ?? db.LUCKY_DRAW_KOKAKUs.FirstOrDefault(k => k.LUCKY_DRAW_ID == eventId && k.KOKAKU_HITO_NAME == phone)
                           ?? db.LUCKY_DRAW_KOKAKUs.FirstOrDefault(k => k.LUCKY_DRAW_ID == eventId && (k.KOKAKU_HITO_PHONE.Contains(phone) || (k.KOKAKU_HITO_NAME != null && k.KOKAKU_HITO_NAME.Contains(phone))));
                if (item == null) throw new InvalidOperationException("Không tìm thấy bản ghi kokaku");

                if (!string.IsNullOrWhiteSpace(patch.KOKAKU_HITO_PHONE) && patch.KOKAKU_HITO_PHONE.Trim() != item.KOKAKU_HITO_PHONE)
                {
                    var newPhone = patch.KOKAKU_HITO_PHONE.Trim();
                    if (newPhone.Length > 20) throw new ArgumentException("KOKAKU_HITO_PHONE không được vượt quá 20 ký tự");
                    if (db.LUCKY_DRAW_KOKAKUs.Any(k => k.LUCKY_DRAW_ID == eventId && k.KOKAKU_HITO_PHONE == newPhone))
                        throw new InvalidOperationException("Số điện thoại mới đã tồn tại cho event này");
                    item.KOKAKU_HITO_PHONE = newPhone;
                }

                if (patch.KOKAKU_HITO_NAME != null) item.KOKAKU_HITO_NAME = patch.KOKAKU_HITO_NAME == string.Empty ? null : patch.KOKAKU_HITO_NAME.Trim();
                if (patch.KOKAKU_ADDRESS != null) item.KOKAKU_ADDRESS = patch.KOKAKU_ADDRESS == string.Empty ? null : patch.KOKAKU_ADDRESS.Trim();
                if (patch.KOKAKU_SHOUMEISYO_NO != null) item.KOKAKU_SHOUMEISYO_NO = patch.KOKAKU_SHOUMEISYO_NO == string.Empty ? null : patch.KOKAKU_SHOUMEISYO_NO.Trim();

                if (patch.KOKAKU_HITO_IMG != null) item.KOKAKU_HITO_IMG = patch.KOKAKU_HITO_IMG;
                if (patch.KOKAKU_MUKO_FLG.HasValue) item.KOKAKU_MUKO_FLG = patch.KOKAKU_MUKO_FLG;

                if (patch.KAISHI_DATE.HasValue) item.KAISHI_DATE = patch.KAISHI_DATE;
                if (patch.SYURYOU_DATE.HasValue) item.SYURYOU_DATE = patch.SYURYOU_DATE;

                var vietnamNow = VietnamNow();
                item.KOSIN_ID = kosinId.Trim();
                item.KOSIN_DATE = vietnamNow;
                item.KOSIN_NAIYOU = !string.IsNullOrWhiteSpace(patch.KOSIN_NAIYOU) ? patch.KOSIN_NAIYOU.Trim() : item.KOSIN_NAIYOU;

                db.SubmitChanges();
            }
        }

        public void SetKokakuMukoFlag(string eventId, string phone, int mukoFlag, string kosinId, string kosinNaiyou = null, DateTime? kosinDate = null)
        {
            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("eventId và phone không được để trống");
            if (string.IsNullOrWhiteSpace(kosinId)) throw new ArgumentException("KOSIN_ID không được để trống khi cập nhật");

            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var item = db.LUCKY_DRAW_KOKAKUs.FirstOrDefault(k => k.LUCKY_DRAW_ID == eventId && k.KOKAKU_HITO_PHONE == phone)
                           ?? db.LUCKY_DRAW_KOKAKUs.FirstOrDefault(k => k.LUCKY_DRAW_ID == eventId && k.KOKAKU_HITO_NAME == phone)
                           ?? db.LUCKY_DRAW_KOKAKUs.FirstOrDefault(k => k.LUCKY_DRAW_ID == eventId && (k.KOKAKU_HITO_PHONE.Contains(phone) || (k.KOKAKU_HITO_NAME != null && k.KOKAKU_HITO_NAME.Contains(phone))));
                if (item == null) throw new InvalidOperationException("Không tìm thấy bản ghi kokaku");

                item.KOKAKU_MUKO_FLG = mukoFlag;
                item.KOSIN_NAIYOU = string.IsNullOrWhiteSpace(kosinNaiyou) ? item.KOSIN_NAIYOU : kosinNaiyou.Trim();
                item.KOSIN_DATE = kosinDate ?? VietnamNow();
                item.KOSIN_ID = kosinId.Trim();

                db.SubmitChanges();
            }
        }

        public void DeleteKokaku(string eventId, string phone)
        {
            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("eventId và phone không được để trống");
            using (var db = new LuckyDrawDBDataContext(_connectionString))
            {
                var item = db.LUCKY_DRAW_KOKAKUs.FirstOrDefault(k => k.LUCKY_DRAW_ID == eventId && k.KOKAKU_HITO_PHONE == phone)
                           ?? db.LUCKY_DRAW_KOKAKUs.FirstOrDefault(k => k.LUCKY_DRAW_ID == eventId && k.KOKAKU_HITO_NAME == phone)
                           ?? db.LUCKY_DRAW_KOKAKUs.FirstOrDefault(k => k.LUCKY_DRAW_ID == eventId && (k.KOKAKU_HITO_PHONE.Contains(phone) || (k.KOKAKU_HITO_NAME != null && k.KOKAKU_HITO_NAME.Contains(phone))));
                if (item == null) throw new InvalidOperationException("Không tìm thấy bản ghi kokaku");

                var vietnamNow = VietnamNow();
                item.KOKAKU_MUKO_FLG = 1;
                item.KOSIN_DATE = vietnamNow;
                item.KOSIN_NAIYOU = "Đã xóa";
                if (string.IsNullOrWhiteSpace(item.KOSIN_ID)) item.KOSIN_ID = "system";
                db.SubmitChanges();
            }
        }
        #endregion
    }
}