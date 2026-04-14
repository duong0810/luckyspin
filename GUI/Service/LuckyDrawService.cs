using BLL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Data.Linq;
using GUI.DTO;
using DAL;

namespace GUI.Service
{
    /// <summary>
    /// Service nội bộ: gọi BLL thay vì HTTP WebAPI.
    /// Giữ signature async để không phải chỉnh UI.
    /// </summary>
    public static class LuckyDrawService
    {
        private static readonly LuckyDrawBLL _bll;
        private static readonly string _connectionString;

        static LuckyDrawService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["LuckyDrawConnectionString"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                Debug.WriteLine("Connection string 'LuckyDrawConnectionString' not found in App.config.");
            }
            _bll = new LuckyDrawBLL(_connectionString);
            Debug.WriteLine("LuckyDrawService initialized (local BLL).");
        }

        #region Helpers: mapping entity <-> DTO and image conversion

        private static LuckyDrawDTO ToDto(LUCKY_DRAW e)
        {
            if (e == null) return null;
            return new LuckyDrawDTO
            {
                LUCKY_DRAW_ID = e.LUCKY_DRAW_ID,
                LUCKY_DRAW_NAME = e.LUCKY_DRAW_NAME,
                LUCKY_DRAW_BUNRUI = e.LUCKY_DRAW_BUNRUI,
                LUCKY_DRAW_TITLE = e.LUCKY_DRAW_TITLE,
                LUCKY_DRAW_SLOGAN = e.LUCKY_DRAW_SLOGAN,
                LUCKY_DRAW_BG_IMG = e.LUCKY_DRAW_BG_IMG != null ? Convert.ToBase64String(e.LUCKY_DRAW_BG_IMG.ToArray()) : null,
                LUCKY_DRAW_SLOT_NUM = e.LUCKY_DRAW_SLOT_NUM,
                TOROKU_DATE = e.TOROKU_DATE,
                TOROKU_ID = e.TOROKU_ID,
                KOSIN_NAIYOU = e.KOSIN_NAIYOU,
                KOSIN_DATE = e.KOSIN_DATE,
                KOSIN_ID = e.KOSIN_DATE == null ? null : e.KOSIN_ID
            };
        }

        private static LuckyDrawMeisaiDTO ToDto(LUCKY_DRAW_MEISAI m)
        {
            if (m == null) return null;
            return new LuckyDrawMeisaiDTO
            {
                LUCKY_DRAW_ID = m.LUCKY_DRAW_ID,
                LUCKY_DRAW_MEISAI_NO = m.LUCKY_DRAW_MEISAI_NO,
                LUCKY_DRAW_MEISAI_NAME = m.LUCKY_DRAW_MEISAI_NAME,
                LUCKY_DRAW_MEISAI_NAIYOU = m.LUCKY_DRAW_MEISAI_NAIYOU,
                LUCKY_DRAW_MEISAI_IMG = m.LUCKY_DRAW_MEISAI_IMG != null ? Convert.ToBase64String(m.LUCKY_DRAW_MEISAI_IMG.ToArray()) : null,
                LUCKY_DRAW_MEISAI_RATE = m.LUCKY_DRAW_MEISAI_RATE,
                LUCKY_DRAW_MEISAI_SURYO = m.LUCKY_DRAW_MEISAI_SURYO,
                LUCKY_DRAW_MEISAI_ONSHOU_NUM = m.LUCKY_DRAW_MEISAI_ONSHOU_NUM,
                LUCKY_DRAW_MEISAI_BUNRUI = m.LUCKY_DRAW_MEISAI_BUNRUI,
                LUCKY_DRAW_MEISAI_MUKO_FLG = m.LUCKY_DRAW_MEISAI_MUKO_FLG,
                KAISHI_DATE = m.KAISHI_DATE,
                SYURYOU_DATE = m.SYURYOU_DATE,
                TOROKU_DATE = m.TOROKU_DATE,
                TOROKU_ID = m.TOROKU_ID,
                KOSIN_NAIYOU = m.KOSIN_NAIYOU,
                KOSIN_DATE = m.KOSIN_DATE,
                KOSIN_ID = m.KOSIN_DATE == null ? null : m.KOSIN_ID
            };
        }

        private static LuckyDrawProgrameDTO ToDto(LUCKY_DRAW_PROGRAME p)
        {
            if (p == null) return null;
            return new LuckyDrawProgrameDTO
            {
                LUCKY_DRAW_PROGRAME_ID = p.LUCKY_DRAW_PROGRAME_ID,
                LUCKY_DRAW_PROGRAME_NAME = p.LUCKY_DRAW_PROGRAME_NAME,
                LUCKY_DRAW_ID = p.LUCKY_DRAW_ID,
                KAISHI_DATE = p.KAISHI_DATE,
                SYURYOU_DATE = p.SYURYOU_DATE,
                PROGRAME_SLOGAN = p.PROGRAME_SLOGAN,
                TOROKU_DATE = p.TOROKU_DATE,
                TOROKU_ID = p.TOROKU_ID,
                KOSIN_NAIYOU = p.KOSIN_NAIYOU,
                KOSIN_DATE = p.KOSIN_DATE,
                KOSIN_ID = p.KOSIN_DATE == null ? null : p.KOSIN_ID
            };
        }

        private static GUI.DTO.LuckyDrawHistoryDTO ToDto(LUCKY_DRAW_PROGRAME_HISTORY h, bool includeImages = false)
        {
            if (h == null) return null;

            string maeBase64 = null;
            string atoBase64 = null;

            if (includeImages)
            {
                // MAE (before) image
                try
                {
                    if (h.MAE_JISSHI_BG_IMG != null)
                    {
                        var bytes = h.MAE_JISSHI_BG_IMG.ToArray();
                        // safety limit for UI payload — log & skip if excessively large
                        const int maxBytes = 5 * 1024 * 1024;
                        if (bytes.Length <= maxBytes)
                        {
                            maeBase64 = Convert.ToBase64String(bytes);
                            // GUI.Base64ToImage accepts both raw base64 and data URI; do not force prefix.
                        }
                    }
                }
                catch 
                {
                    try
                    {
                        var dbgPath = Path.Combine(Path.GetTempPath(), $"mae_img_error_{h.PROGRAME_JISSHI_ID}_{DateTime.Now:yyyyMMdd_HHmmss}.bin");
                        File.WriteAllBytes(dbgPath, h.MAE_JISSHI_BG_IMG?.ToArray() ?? new byte[0]);
                    }
                    catch { /* swallow secondary failure */ }
                    maeBase64 = null;
                }

                // ATO (after) image
                try
                {
                    if (h.ATO_JISSHI_BG_IMG != null)
                    {
                        var bytes = h.ATO_JISSHI_BG_IMG.ToArray();
                        const int maxBytes = 5 * 1024 * 1024;
                        if (bytes.Length <= maxBytes)
                        {
                            atoBase64 = Convert.ToBase64String(bytes);
                        }

                    }
                }
                catch 
                {
                    try
                    {
                        var dbgPath = Path.Combine(Path.GetTempPath(), $"ato_img_error_{h.PROGRAME_JISSHI_ID}_{DateTime.Now:yyyyMMdd_HHmmss}.bin");
                        File.WriteAllBytes(dbgPath, h.ATO_JISSHI_BG_IMG?.ToArray() ?? new byte[0]);
                    }
                    catch { /* swallow secondary failure */ }
                    atoBase64 = null;
                }
            }

            return new GUI.DTO.LuckyDrawHistoryDTO
            {
                PROGRAME_JISSHI_ID = h.PROGRAME_JISSHI_ID,
                LUCKY_DRAW_PROGRAME_ID = h.LUCKY_DRAW_PROGRAME_ID,
                LUCKY_DRAW_ID = h.LUCKY_DRAW_ID,
                MITUMORI_NO_SANSYO = h.MITUMORI_NO_SANSYO,
                KOKAKU_HITO_NAME = h.KOKAKU_HITO_NAME,
                KOKAKU_HITO_PHONE = h.KOKAKU_HITO_PHONE,
                KOKAKU_HITO_ADDRESS = h.KOKAKU_HITO_ADDRESS,
                RATE_LOG_SPLIT = h.RATE_LOG_SPLIT,
                KENSYO_DRAW_MEISAI_ID = h.KENSYO_DRAW_MEISAI_ID,
                KENSYO_VOUCHER_CODE = h.KENSYO_VOUCHER_CODE,
                KENSYO_DRAW_MEISAI_SURYO = h.KENSYO_DRAW_MEISAI_SURYO,
                KAISHI_DATE = h.KAISHI_DATE,
                SYURYOU_DATE = h.SYURYOU_DATE,
                TOROKU_DATE = h.TOROKU_DATE,
                TOROKU_ID = h.TOROKU_ID,
                KOKAKU_NAME_SYUTOKU = h.KOKAKU_NAME_SYUTOKU,
                KOKAKU_PHONE_SYUTOKU = h.KOKAKU_PHONE_SYUTOKU,
                KOKAKU_SHOUMEISYO_NO_SYUTOKU = h.KOKAKU_SHOUMEISYO_NO_SYUTOKU,
                KOKAKU_ADDRESS_SYUTOKU = h.KOKAKU_ADDRESS_SYUTOKU,
                KOKAKU_SYUTOKU_DATE = h.KOKAKU_SYUTOKU_DATE,
                TANTO_SYA_NAME = h.TANTO_SYA_NAME,
                MITUMORI_NO_SYUTOKU_SANSYO = h.MITUMORI_NO_SYUTOKU_SANSYO,
                JISSHI_KAISHI_DATE = h.JISSHI_KAISHI_DATE,
                JISSHI_SYURYOU_DATE = h.JISSHI_SYURYOU_DATE,
                MAE_JISSHI_BG_IMG = maeBase64,
                ATO_JISSHI_BG_IMG = atoBase64
            };
        }

        public static Task<GUI.DTO.LuckyDrawHistoryDTO> GetHistoryByIdAsync(decimal jisshiId)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_connectionString))
                    {
                        Debug.WriteLine("GetHistoryByIdAsync: missing connection string.");
                        return null;
                    }

                    using (var db = new DataContext(_connectionString))
                    {
                        var table = db.GetTable<LUCKY_DRAW_PROGRAME_HISTORY>();
                        var h = table.FirstOrDefault(x => x.PROGRAME_JISSHI_ID == jisshiId);
                        return ToDto(h, includeImages: true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetHistoryByIdAsync error: " + ex.Message);
                    return null;
                }
            });
        }

        private static byte[] Base64ToBytes(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return null;
            var s = base64.Trim();
            if (s.Contains(",")) s = s.Substring(s.IndexOf(",") + 1);
            s = s.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            return Convert.FromBase64String(s);
        }

        #endregion

        #region Events

        public static Task<List<LuckyDrawDTO>> GetAllEventsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_connectionString))
                    {
                        Debug.WriteLine("GetAllEventsAsync: missing connection string.");
                        return new List<LuckyDrawDTO>();
                    }

                    using (var db = new DataContext(_connectionString))
                    {
                        // Project only needed columns to avoid loading large binary blobs
                        var list = db.GetTable<LUCKY_DRAW>()
                            .OrderByDescending(e => e.TOROKU_DATE)
                            .Select(e => new
                            {
                                e.LUCKY_DRAW_ID,
                                e.LUCKY_DRAW_NAME,
                                e.LUCKY_DRAW_BUNRUI,
                                e.LUCKY_DRAW_TITLE,
                                e.LUCKY_DRAW_SLOGAN,
                                e.LUCKY_DRAW_SLOT_NUM,
                                e.TOROKU_DATE,
                                e.TOROKU_ID,
                                e.KOSIN_NAIYOU,
                                e.KOSIN_DATE,
                                e.KOSIN_ID
                            })
                            .ToList();

                        // Map to DTO but intentionally DO NOT include LUCKY_DRAW_BG_IMG (lazy load only)
                        var result = list.Select(e => new LuckyDrawDTO
                        {
                            LUCKY_DRAW_ID = e.LUCKY_DRAW_ID,
                            LUCKY_DRAW_NAME = e.LUCKY_DRAW_NAME,
                            LUCKY_DRAW_BUNRUI = e.LUCKY_DRAW_BUNRUI,
                            LUCKY_DRAW_TITLE = e.LUCKY_DRAW_TITLE,
                            LUCKY_DRAW_SLOGAN = e.LUCKY_DRAW_SLOGAN,
                            LUCKY_DRAW_BG_IMG = null, // DO NOT populate here
                            LUCKY_DRAW_SLOT_NUM = e.LUCKY_DRAW_SLOT_NUM,
                            TOROKU_DATE = e.TOROKU_DATE,
                            TOROKU_ID = e.TOROKU_ID,
                            KOSIN_NAIYOU = e.KOSIN_NAIYOU,
                            KOSIN_DATE = e.KOSIN_DATE,
                            KOSIN_ID = e.KOSIN_DATE == null ? null : e.KOSIN_ID
                        }).ToList();

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetAllEventsAsync error: " + ex.Message);
                    return new List<LuckyDrawDTO>();
                }
            });
        }

        public static Task<LuckyDrawDTO> GetEventByIdAsync(string eventId)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_connectionString))
                    {
                        Debug.WriteLine("GetEventByIdAsync: missing connection string.");
                        return null;
                    }

                    using (var db = new DataContext(_connectionString))
                    {
                        var table = db.GetTable<LUCKY_DRAW>();
                        var e = table.FirstOrDefault(x => x.LUCKY_DRAW_ID == eventId);
                        return ToDto(e);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetEventByIdAsync error: " + ex.Message);
                    return null;
                }
            });
        }

        public static Task<(bool success, string message, string eventId)> CreateEventWithImageAsync(LuckyDrawDTO eventData, string imageFilePath = null)
        {
            return Task.Run<(bool, string, string)>(() =>
            {
                try
                {
                    byte[] bytes = null;
                    if (!string.IsNullOrWhiteSpace(imageFilePath) && File.Exists(imageFilePath))
                        bytes = File.ReadAllBytes(imageFilePath);

                    _bll.CreateEvent(
                        eventId: eventData.LUCKY_DRAW_ID,
                        eventName: eventData.LUCKY_DRAW_NAME,
                        bunrui: eventData.LUCKY_DRAW_BUNRUI,
                        title: eventData.LUCKY_DRAW_TITLE,
                        slogan: eventData.LUCKY_DRAW_SLOGAN,
                        torokuId: eventData.TOROKU_ID,
                        slotNum: eventData.LUCKY_DRAW_SLOT_NUM,
                        bgImage: bytes
                    );

                    // return created event id (caller expects eventId)
                    return (true, "Tạo sự kiện thành công", eventData.LUCKY_DRAW_ID);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("CreateEventWithImageAsync error: " + ex.Message);
                    return (false, ex.Message, null);
                }
            });
        }

        public static Task<(bool success, string message)> UpdateEventWithImageAsync(string eventId, LuckyDrawDTO eventData, string imageFilePath)
        {
            return Task.Run<(bool, string)>(() =>
            {
                try
                {
                    byte[] bytes = null;
                    bool provided = false;
                    if (!string.IsNullOrWhiteSpace(imageFilePath))
                    {
                        provided = true;
                        if (File.Exists(imageFilePath))
                        {
                            bytes = File.ReadAllBytes(imageFilePath);
                        }
                        else
                        {
                            // empty path means clear image
                            bytes = null;
                        }
                    }

                    _bll.UpdateEvent(
                        id: eventId,
                        kosinId: eventData.KOSIN_ID,
                        kosinNaiyou: eventData.KOSIN_NAIYOU,
                        newName: eventData.LUCKY_DRAW_NAME,
                        newBunrui: eventData.LUCKY_DRAW_BUNRUI,
                        newTitle: eventData.LUCKY_DRAW_TITLE,
                        newSlogan: eventData.LUCKY_DRAW_SLOGAN,
                        newSlotNum: eventData.LUCKY_DRAW_SLOT_NUM > 0 ? (int?)eventData.LUCKY_DRAW_SLOT_NUM : null,
                        bgImage: bytes,
                        bgImageProvided: provided
                    );

                    return (true, "Cập nhật sự kiện thành công");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UpdateEventWithImageAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message)> UpdateEventAsync(string eventId, LuckyDrawDTO eventData)
        {
            return Task.Run<(bool, string)>(() =>
            {
                try
                {
                    // call BLL update without providing an image (bgImageProvided = false)
                    _bll.UpdateEvent(
                        id: eventId,
                        kosinId: eventData.KOSIN_ID ?? eventData.TOROKU_ID,
                        kosinNaiyou: eventData.KOSIN_NAIYOU,
                        newName: eventData.LUCKY_DRAW_NAME,
                        newBunrui: eventData.LUCKY_DRAW_BUNRUI,
                        newTitle: eventData.LUCKY_DRAW_TITLE,
                        newSlogan: eventData.LUCKY_DRAW_SLOGAN,
                        newSlotNum: eventData.LUCKY_DRAW_SLOT_NUM > 0 ? (int?)eventData.LUCKY_DRAW_SLOT_NUM : null,
                        bgImage: null,
                        bgImageProvided: false
                    );

                    return (true, "Cập nhật sự kiện thành công");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UpdateEventAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message)> DeleteEventAsync(string eventId)
        {
            return Task.Run(() =>
            {
                try
                {
                    _bll.DeleteEvent(eventId);
                    return (true, "Xóa sự kiện thành công");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("DeleteEventAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        #endregion

        #region Meisai

        public static Task<List<LuckyDrawMeisaiDTO>> GetAllMeisaiAsync(string luckyDrawId = null, string category = null, bool includeMuko = false)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_connectionString))
                    {
                        Debug.WriteLine("GetAllMeisaiAsync: missing connection string.");
                        return new List<LuckyDrawMeisaiDTO>();
                    }

                    using (var db = new DataContext(_connectionString))
                    {
                        var table = db.GetTable<LUCKY_DRAW_MEISAI>().AsQueryable();

                        if (!string.IsNullOrWhiteSpace(luckyDrawId))
                            table = table.Where(m => m.LUCKY_DRAW_ID == luckyDrawId);

                        if (!string.IsNullOrWhiteSpace(category))
                            table = table.Where(m => m.LUCKY_DRAW_MEISAI_BUNRUI == category);

                        if (!includeMuko)
                            table = table.Where(m => (m.LUCKY_DRAW_MEISAI_MUKO_FLG ?? 0) != 1);

                        var list = table.ToList();
                        return list.Select(ToDto).ToList();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetAllMeisaiAsync error: " + ex.Message);
                    return new List<LuckyDrawMeisaiDTO>();
                }
            });
        }

        public static Task<(bool success, string message)> CreateMeisaiWithImageAsync(LuckyDrawMeisaiDTO meisaiData, string imageFilePath = null)
        {
            return Task.Run<(bool, string)>(() =>
            {
                try
                {
                    byte[] bytes = null;
                    if (!string.IsNullOrWhiteSpace(imageFilePath) && File.Exists(imageFilePath))
                        bytes = File.ReadAllBytes(imageFilePath);

                    _bll.CreateMeisai(
                        luckyDrawId: meisaiData.LUCKY_DRAW_ID,
                        meisaiNo: meisaiData.LUCKY_DRAW_MEISAI_NO,
                        meisaiName: meisaiData.LUCKY_DRAW_MEISAI_NAME,
                        meisaiNaiyou: meisaiData.LUCKY_DRAW_MEISAI_NAIYOU,
                        rate: meisaiData.LUCKY_DRAW_MEISAI_RATE,
                        suryo: meisaiData.LUCKY_DRAW_MEISAI_SURYO,
                        meisaiBunrui: meisaiData.LUCKY_DRAW_MEISAI_BUNRUI,
                        kaishiDate: meisaiData.KAISHI_DATE,
                        syuryouDate: meisaiData.SYURYOU_DATE,
                        torokuId: meisaiData.TOROKU_ID,
                        onshouNum: meisaiData.LUCKY_DRAW_MEISAI_ONSHOU_NUM,
                        imageBytes: bytes
                    );

                    return (true, "Tạo giải thưởng thành công");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("CreateMeisaiWithImageAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message)> UpdateMeisaiAsync(string luckyDrawId, string meisaiNo, LuckyDrawMeisaiDTO meisaiData)
        {
            return Task.Run<(bool, string)>(() =>
            {
                try
                {
                    _bll.UpdateMeisai(
                        luckyDrawId: luckyDrawId,
                        meisaiNo: meisaiNo,
                        kosinId: meisaiData.KOSIN_ID ?? meisaiData.TOROKU_ID,
                        meisaiName: meisaiData.LUCKY_DRAW_MEISAI_NAME,
                        meisaiNaiyou: meisaiData.LUCKY_DRAW_MEISAI_NAIYOU,
                        meisaiBunrui: meisaiData.LUCKY_DRAW_MEISAI_BUNRUI,
                        newRate: meisaiData.LUCKY_DRAW_MEISAI_RATE,
                        newSuryo: meisaiData.LUCKY_DRAW_MEISAI_SURYO,
                        newOnshouNum: meisaiData.LUCKY_DRAW_MEISAI_ONSHOU_NUM,
                        kaishi: meisaiData.KAISHI_DATE,
                        syuryou: meisaiData.SYURYOU_DATE,
                        mukoFlg: meisaiData.LUCKY_DRAW_MEISAI_MUKO_FLG,
                        kosinNaiyou: meisaiData.KOSIN_NAIYOU,
                        meisaiImgBase64: null,
                        meisaiImgProvided: false
                    );

                    return (true, "Cập nhật giải thưởng thành công");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UpdateMeisaiAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message)> UpdateMeisaiWithImageAsync(string luckyDrawId, string meisaiNo, LuckyDrawMeisaiDTO meisaiData, string imageFilePath)
        {
            return Task.Run<(bool, string)>(() =>
            {
                try
                {
                    bool provided = false;
                    string base64 = null;
                    if (!string.IsNullOrWhiteSpace(imageFilePath))
                    {
                        provided = true;
                        if (File.Exists(imageFilePath))
                        {
                            var bytes = File.ReadAllBytes(imageFilePath);
                            base64 = Convert.ToBase64String(bytes);
                        }
                        else
                        {
                            base64 = string.Empty; // signal delete
                        }
                    }

                    _bll.UpdateMeisai(
                        luckyDrawId: luckyDrawId,
                        meisaiNo: meisaiNo,
                        kosinId: meisaiData.KOSIN_ID,
                        meisaiName: meisaiData.LUCKY_DRAW_MEISAI_NAME,
                        meisaiNaiyou: meisaiData.LUCKY_DRAW_MEISAI_NAIYOU,
                        meisaiBunrui: meisaiData.LUCKY_DRAW_MEISAI_BUNRUI,
                        newRate: meisaiData.LUCKY_DRAW_MEISAI_RATE,
                        newSuryo: meisaiData.LUCKY_DRAW_MEISAI_SURYO,
                        newOnshouNum: meisaiData.LUCKY_DRAW_MEISAI_ONSHOU_NUM,
                        kaishi: meisaiData.KAISHI_DATE,
                        syuryou: meisaiData.SYURYOU_DATE,
                        mukoFlg: meisaiData.LUCKY_DRAW_MEISAI_MUKO_FLG,
                        kosinNaiyou: meisaiData.KOSIN_NAIYOU,
                        meisaiImgBase64: base64,
                        meisaiImgProvided: provided
                    );

                    return (true, "Cập nhật giải thưởng thành công");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UpdateMeisaiWithImageAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message)> DeleteMeisaiAsync(string luckyDrawId, string meisaiNo)
        {
            return Task.Run(() =>
            {
                try
                {
                    _bll.DeleteMeisai(luckyDrawId, meisaiNo);
                    return (true, "Xóa giải thưởng thành công");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("DeleteMeisaiAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        #endregion

        #region Programs & History & Spin (selected)

        public static Task<List<LuckyDrawProgrameDTO>> GetAllProgramsAsync(string luckyDrawId = null)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_connectionString))
                    {
                        Debug.WriteLine("GetAllProgramsAsync: missing connection string.");
                        return new List<LuckyDrawProgrameDTO>();
                    }

                    using (var db = new DataContext(_connectionString))
                    {
                        var table = db.GetTable<LUCKY_DRAW_PROGRAME>().AsQueryable();
                        if (!string.IsNullOrWhiteSpace(luckyDrawId))
                            table = table.Where(p => p.LUCKY_DRAW_ID == luckyDrawId);

                        var list = table.ToList();
                        return list.Select(ToDto).ToList();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetAllProgramsAsync error: " + ex.Message);
                    return new List<LuckyDrawProgrameDTO>();
                }
            });
        }

        public static Task<(bool success, string message)> CreateProgramAsync(LuckyDrawProgrameDTO dto)
        {
            return Task.Run<(bool, string)>(() =>
            {
                try
                {
                    _bll.CreateProgram(
                        programeId: dto.LUCKY_DRAW_PROGRAME_ID,
                        programeName: dto.LUCKY_DRAW_PROGRAME_NAME,
                        luckyDrawId: dto.LUCKY_DRAW_ID,
                        kaishi: dto.KAISHI_DATE,
                        syuryou: dto.SYURYOU_DATE,
                        slogan: dto.PROGRAME_SLOGAN,
                        torokuId: dto.TOROKU_ID
                    );
                    return (true, "Tạo chương trình thành công");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("CreateProgramAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message)> UpdateProgramAsync(string programId, LuckyDrawProgrameDTO dto)
        {
            return Task.Run<(bool, string)>(() =>
            {
                try
                {
                    _bll.UpdateProgram(
                        id: programId,
                        kosinId: dto.KOSIN_ID ?? dto.TOROKU_ID,
                        kosinNaiyou: dto.KOSIN_NAIYOU,
                        newName: dto.LUCKY_DRAW_PROGRAME_NAME,
                        kaishi: dto.KAISHI_DATE,
                        syuryou: dto.SYURYOU_DATE,
                        slogan: dto.PROGRAME_SLOGAN
                    );
                    return (true, "Cập nhật chương trình thành công");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UpdateProgramAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message)> DeleteProgramAsync(string programId)
        {
            return Task.Run<(bool, string)>(() =>
            {
                try
                {
                    _bll.DeleteProgram(programId);
                    return (true, "Xóa chương trình thành công");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("DeleteProgramAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message, decimal? jisshiId)> SaveHistoryAsync(GUI.DTO.LuckyDrawHistoryDTO dto)
        {
            return Task.Run<(bool, string, decimal?)>(() =>
            {
                try
                {
                    // Map GUI DTO -> entity for BLL SaveHistory
                    var entity = new LUCKY_DRAW_PROGRAME_HISTORY
                    {
                        PROGRAME_JISSHI_ID = dto.PROGRAME_JISSHI_ID,
                        LUCKY_DRAW_PROGRAME_ID = dto.LUCKY_DRAW_PROGRAME_ID,
                        LUCKY_DRAW_ID = dto.LUCKY_DRAW_ID,
                        MITUMORI_NO_SANSYO = dto.MITUMORI_NO_SANSYO,
                        KOKAKU_HITO_NAME = dto.KOKAKU_HITO_NAME,
                        KOKAKU_HITO_PHONE = dto.KOKAKU_HITO_PHONE,
                        KOKAKU_HITO_ADDRESS = dto.KOKAKU_HITO_ADDRESS,
                        RATE_LOG_SPLIT = dto.RATE_LOG_SPLIT,
                        KENSYO_DRAW_MEISAI_ID = dto.KENSYO_DRAW_MEISAI_ID,
                        KENSYO_VOUCHER_CODE = dto.KENSYO_VOUCHER_CODE,
                        KENSYO_DRAW_MEISAI_SURYO = dto.KENSYO_DRAW_MEISAI_SURYO,
                        KAISHI_DATE = dto.KAISHI_DATE,
                        SYURYOU_DATE = dto.SYURYOU_DATE,
                        JISSHI_KAISHI_DATE = dto.JISSHI_KAISHI_DATE,
                        JISSHI_SYURYOU_DATE = dto.JISSHI_SYURYOU_DATE
                    };

                    decimal id = _bll.SaveHistory(entity, dto.TOROKU_ID, dto.MAE_JISSHI_BG_IMG, dto.ATO_JISSHI_BG_IMG, isCreate: dto.PROGRAME_JISSHI_ID == 0, deductQty: dto.KENSYO_DRAW_MEISAI_SURYO);
                    return (true, "Lưu lịch sử thành công", id);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SaveHistoryAsync error: " + ex.Message);
                    return (false, ex.Message, null);
                }
            });
        }

        public static Task<List<GUI.DTO.LuckyDrawHistoryDTO>> GetHistoryAsync(string phone = null, string luckyDrawId = null, bool includeImages = true)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_connectionString))
                    {
                        Debug.WriteLine("GetHistoryAsync: missing connection string.");
                        return new List<GUI.DTO.LuckyDrawHistoryDTO>();
                    }

                    using (var db = new DataContext(_connectionString))
                    {
                        var table = db.GetTable<LUCKY_DRAW_PROGRAME_HISTORY>().AsQueryable();

                        if (!string.IsNullOrWhiteSpace(phone))
                            table = table.Where(h => h.KOKAKU_HITO_PHONE == phone);

                        if (!string.IsNullOrWhiteSpace(luckyDrawId))
                            table = table.Where(h => h.LUCKY_DRAW_ID == luckyDrawId);

                        var list = table.ToList();

                        if (!includeImages)
                        {
                            // avoid sending binary payload: null out image fields so ToDto won't convert them
                            foreach (var h in list)
                            {
                                h.MAE_JISSHI_BG_IMG = null;
                                h.ATO_JISSHI_BG_IMG = null;
                            }
                        }

                        return list.Select(h => ToDto(h, includeImages)).ToList();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetHistoryAsync error: " + ex.Message);
                    return new List<GUI.DTO.LuckyDrawHistoryDTO>();
                }
            });
        }

        public static Task<(LuckyDrawMeisaiDTO Meisai, double RandomValue, string RateLog)> SpinMeisaiAsync(string luckyDrawId = null, string category = null, bool includeMuko = false)
        {
            return Task.Run<(LuckyDrawMeisaiDTO, double, string)>(() =>
            {
                try
                {
                    // call BLL spin (returns (LUCKY_DRAW_MEISAI winner, double rv))
                    var (winner, rv) = _bll.SpinVoucher(luckyDrawId, category, includeMuko);
                    var dto = ToDto(winner);
                    // rateLog not produced by current BLL; return null for now (UI should handle null)
                    string rateLog = null;
                    return (dto, rv, rateLog);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SpinMeisaiAsync error: " + ex.Message);
                    return (null, 0.0, null);
                }
            });
        }

        public static Task<(LuckyDrawMeisaiDTO Meisai, double RandomValue, string RateLog)> ConfirmSpinMeisaiAsync(double randomValue, string luckyDrawId = null, string category = null, bool includeMuko = false)
        {
            return Task.Run<(LuckyDrawMeisaiDTO, double, string)>(() =>
            {
                try
                {
                    var winner = _bll.ConfirmSpin(randomValue, luckyDrawId, category, includeMuko);
                    var dto = ToDto(winner);
                    string rateLog = null;
                    return (dto, randomValue, rateLog);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ConfirmSpinMeisaiAsync error: " + ex.Message);
                    return (null, randomValue, null);
                }
            });
        }

        public static Task<LuckyDrawProgrameDTO> GetProgramByIdAsync(string programId)
        {
            return Task.Run<LuckyDrawProgrameDTO>(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_connectionString))
                    {
                        Debug.WriteLine("GetProgramByIdAsync: missing connection string.");
                        return null;
                    }

                    using (var db = new DataContext(_connectionString))
                    {
                        var p = db.GetTable<LUCKY_DRAW_PROGRAME>().FirstOrDefault(x => x.LUCKY_DRAW_PROGRAME_ID == programId);
                        return ToDto(p);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetProgramByIdAsync error: " + ex.Message);
                    return null;
                }
            });
        }

        public static Task<(string h1, string h2, string h3)> GetBannerHeadersAsync()
        {
            return Task.Run<(string, string, string)>(() =>
            {
                try
                {
                    var fh1 = ConfigurationManager.AppSettings["BannerHeader1"];
                    var fh2 = ConfigurationManager.AppSettings["BannerHeader2"];
                    var fh3 = ConfigurationManager.AppSettings["BannerHeader3"];

                    return (fh1 ?? "VÒNG QUAY LÌ XÌ", fh2 ?? "XUÂN SANG LỘC VỀ", fh3 ?? "RINH NGAY QUÀ LỚN");
                }
                catch
                {
                    return ("VÒNG QUAY LÌ XÌ", "XUÂN SANG LỘC VỀ", "RINH NGAY QUÀ LỚN");
                }
            });
        }

        #endregion

        #region Kokaku

        public static Task<List<LuckyDrawKokakuDTO>> GetKokakuByEventAsync(string eventId, bool includeMuko = false, bool includeImages = false)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_connectionString))
                    {
                        Debug.WriteLine("GetKokakuByEventAsync: missing connection string.");
                        return new List<LuckyDrawKokakuDTO>();
                    }

                    using (var db = new DataContext(_connectionString))
                    {
                        var table = db.GetTable<LUCKY_DRAW_KOKAKU>().AsQueryable();
                        if (!string.IsNullOrWhiteSpace(eventId))
                            table = table.Where(k => k.LUCKY_DRAW_ID == eventId);

                        if (!includeMuko)
                            table = table.Where(k => (k.KOKAKU_MUKO_FLG ?? 0) != 1);

                        var list = table.ToList();
                        return list.Select(k => new LuckyDrawKokakuDTO
                        {
                            LUCKY_DRAW_ID = k.LUCKY_DRAW_ID,
                            KOKAKU_HITO_PHONE = k.KOKAKU_HITO_PHONE,
                            KOKAKU_HITO_NAME = k.KOKAKU_HITO_NAME,
                            KOKAKU_ADDRESS = k.KOKAKU_ADDRESS,
                            KOKAKU_SHOUMEISYO_NO = k.KOKAKU_SHOUMEISYO_NO,
                            KOKAKU_MUKO_FLG = k.KOKAKU_MUKO_FLG,
                            TOROKU_DATE = k.TOROKU_DATE,
                            TOROKU_ID = k.TOROKU_ID,
                            KOSIN_NAIYOU = k.KOSIN_NAIYOU,
                            KOSIN_DATE = k.KOSIN_DATE,
                            KOSIN_ID = k.KOSIN_DATE == null ? null : k.KOSIN_ID,
                            KAISHI_DATE = k.KAISHI_DATE,
                            SYURYOU_DATE = k.SYURYOU_DATE,
                            // include image only when requested to avoid heavy payloads
                            KOKAKU_HITO_IMG = (includeImages && k.KOKAKU_HITO_IMG != null)
                                ? Convert.ToBase64String(k.KOKAKU_HITO_IMG.ToArray())
                                : null
                        }).ToList();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetKokakuByEventAsync error: " + ex.Message);
                    return new List<LuckyDrawKokakuDTO>();
                }
            });
        }

        public static Task<(bool success, string message)> SaveKokakuAsync(LuckyDrawKokakuDTO dto)
        {
            return Task.Run(() =>
            {
                try
                {
                    var entity = new LUCKY_DRAW_KOKAKU
                    {
                        LUCKY_DRAW_ID = dto.LUCKY_DRAW_ID,
                        KOKAKU_HITO_PHONE = dto.KOKAKU_HITO_PHONE,
                        KOKAKU_HITO_NAME = dto.KOKAKU_HITO_NAME,
                        KOKAKU_ADDRESS = dto.KOKAKU_ADDRESS,
                        KOKAKU_SHOUMEISYO_NO = dto.KOKAKU_SHOUMEISYO_NO,
                        KOKAKU_MUKO_FLG = dto.KOKAKU_MUKO_FLG
                    };

                    // If GUI contains Base64 image property name different, pass null/no image; SaveKokaku in BLL supports kokakuImgBase64 param
                    _bll.SaveKokaku(entity, dto.TOROKU_ID, kokakuImgBase64: null);
                    return (true, "Saved");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SaveKokakuAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message)> UpdateKokakuAsync(string luckyDrawId, string phone, LuckyDrawKokakuDTO dto, string originalPhone = null)
        {
            return Task.Run(() =>
            {
                try
                {
                    var patch = new LUCKY_DRAW_KOKAKU
                    {
                        KOKAKU_HITO_PHONE = dto.KOKAKU_HITO_PHONE,
                        KOKAKU_HITO_NAME = dto.KOKAKU_HITO_NAME,
                        KOKAKU_ADDRESS = dto.KOKAKU_ADDRESS,
                        KOKAKU_SHOUMEISYO_NO = dto.KOKAKU_SHOUMEISYO_NO,
                        KOKAKU_MUKO_FLG = dto.KOKAKU_MUKO_FLG,
                        KOSIN_NAIYOU = dto.KOSIN_NAIYOU,
                        KAISHI_DATE = dto.KAISHI_DATE,
                        SYURYOU_DATE = dto.SYURYOU_DATE
                    };

                    var keyPhone = !string.IsNullOrWhiteSpace(originalPhone) ? originalPhone : phone;
                    _bll.UpdateKokaku(luckyDrawId, keyPhone, dto.KOSIN_ID, patch);
                    return (true, "Updated");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UpdateKokakuAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message)> SetKokakuMukoFlagAsync(string eventId, string phone, int mukoFlag = 1, string kosinId = null, string kosinNaiyou = null)
        {
            return Task.Run(() =>
            {
                try
                {
                    _bll.SetKokakuMukoFlag(eventId, phone, mukoFlag, kosinId, kosinNaiyou);
                    return (true, "Updated");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SetKokakuMukoFlagAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        public static Task<(bool success, string message)> DeleteKokakuAsync(string luckyDrawId, string phone)
        {
            return Task.Run(() =>
            {
                try
                {
                    _bll.DeleteKokaku(luckyDrawId, phone);
                    return (true, "Deleted");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("DeleteKokakuAsync error: " + ex.Message);
                    return (false, ex.Message);
                }
            });
        }

        #endregion

    }
}