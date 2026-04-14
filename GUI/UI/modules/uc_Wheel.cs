using GUI.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using GUI.DTO;
using System.Reflection;

namespace GUI.UI.modules
{
    public partial class uc_Wheel : UserControl
    {
        private string spinBeforeImageBase64 = null;
        private List<double> segmentRates = new List<double>();
        private Timer marqueeTimer;
        private int marqueeSpeed = 4;
        private int marqueeStartX;
        private int numSegments = 8;
        private double lastRandomValue = 0;
        private Bitmap cachedBackground = null;
        private List<Color> cachedSegmentColors = null;
        private Image wheelBackgroundImage = null;
        private List<Image> segmentImageCache = new List<Image>();
        private List<string> segmentImages = new List<string>();
        private List<string> segmentCodes = new List<string>();
        private List<string> segmentTexts = new List<string>
        {
            "Thưởng 1", "Thưởng 2", "Thưởng 3", "Thưởng 4",
            "Thưởng 5", "Thưởng 6", "Thưởng 7", "Thưởng 8"
        };



        /// <summary>
        /// Enum định nghĩa các style vòng quay
        /// </summary>
        public enum WheelStyle
        {
            Default = 0,
            TetNguyenDan = 1,
            GiangSinh = 2,
            Halloween = 3,
            TrungThu = 4,
            ModernNeon = 5
        }

        // Field lưu style hiện tại
        private WheelStyle currentStyle = WheelStyle.Default;

        // Dictionary lưu cache các icon decoration theo style
        private Dictionary<WheelStyle, List<Image>> decorationIconsCache = new Dictionary<WheelStyle, List<Image>>();
        private Dictionary<WheelStyle, Image> spinButtonImages = new Dictionary<WheelStyle, Image>();


        // Danh sách các particle effects (hoa mai, tuyết, ...)
        private class DecorativeParticle
        {
            public PointF Position;
            public float VelocityX, VelocityY;
            public Image Icon;
            public float Rotation;
            public float Alpha;
            public int Life;
        }

        private List<DecorativeParticle> decorativeParticles = new List<DecorativeParticle>();
        private Timer particleTimer;
        private bool showDecorativeParticles = false;

        // --- Thêm các trường mới (chèn gần các field khác, ví dụ sau spinStartTime) ---
        private float spinStartRotation = 0f;
        private float spinTotalDelta = 0f;
        private int spinDurationMs = 5000; // đồng bộ với spinDurationSeconds khi SetSpinDuration được gọi

        private int spinDurationSeconds = 5; // Mặc định 5 giây
        private const string SETTINGS_FILE = "wheel_settings.json";

        /// <summary>
        /// Lấy thời gian quay hiện tại (giây)
        /// </summary>
        public int GetSpinDuration()
        {
            return spinDurationSeconds;
        }

        /// <summary>
        /// Thiết lập thời gian quay (giây)
        /// </summary>
        public void SetSpinDuration(int seconds)
        {
            if (seconds < 5) seconds = 5;
            if (seconds > 30) seconds = 30;

            spinDurationSeconds = seconds;
            spinDurationMs = seconds * 1000;

            // Lưu cấu hình
            SaveWheelSettings();

        }

        /// <summary>
        /// Thiết lập style vòng quay (Tết, Giáng Sinh, Halloween, Trung Thu, Modern Neon)
        /// </summary>
        public void SetWheelStyle(int styleIndex)
        {
            try
            {
                // Validate index (bây giờ có 6 styles: 0-5)
                if (styleIndex < 0 || styleIndex > 5)
                {
                    styleIndex = 0; // Default: Mặc định
                }

                currentStyle = (WheelStyle)styleIndex;

                // Load decoration icons cho style này
                LoadStyleImages(currentStyle);

                // Cập nhật màu sắc segment theo style
                UpdateSegmentColorsForStyle(currentStyle);

                // Lưu style vào file config
                SaveStyleToFile(currentStyle);

                // Vẽ lại vòng quay
                if (this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)(() => this.Invalidate()));
                }
                else
                {
                    this.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thiết lập style: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Load hình ảnh theo style (gộp chung decoration icons và spin button)
        /// </summary>
        private void LoadStyleImages(WheelStyle style)
        {
            // Kiểm tra cache
            if (decorationIconsCache.ContainsKey(style) && spinButtonImages.ContainsKey(style))
                return; // Đã load rồi

            if (style == WheelStyle.Default)
            {
                decorationIconsCache[style] = new List<Image>();
                spinButtonImages[style] = LoadSingleIcon("star.png");
                return;
            }

            string assetFolder = Path.Combine(Application.StartupPath, "Assets", "img");
            if (!Directory.Exists(assetFolder))
            {
                Directory.CreateDirectory(assetFolder);
                return;
            }

            // ⭐ THAY THẾ: Dùng Dictionary để map style -> patterns
            var stylePatterns = new Dictionary<WheelStyle, (string[] patterns, string buttonIcon)>
            {
                [WheelStyle.TetNguyenDan] = (new[] { "*flower*.png", "*star*.png", "*money*.png", "*gift-box*.png", "*firework*.png" }, "flower.png"),
                [WheelStyle.GiangSinh] = (new[] { "*santa-claus*.png", "*snowman*.png", "*chrismas-tree*.png" }, "santa-claus.png"),
                [WheelStyle.Halloween] = (new[] { "*pumpkin*.png", "*ghost*.png", "*bat*.png", "*sweets*.png" }, "pumpkin.png"),
                [WheelStyle.TrungThu] = (new[] { "*lantern*.png", "*moon*.png", "*food*.png" }, "star.png"),
                [WheelStyle.ModernNeon] = (new[] { "*neon-controller*.png", "*diamond*.png", "*star*.png" }, "star.png")
            };

            if (!stylePatterns.ContainsKey(style)) return;

            var (patterns, buttonIcon) = stylePatterns[style];

            // Load decoration icons
            var iconList = new List<Image>();
            var allFiles = patterns.SelectMany(p => Directory.GetFiles(assetFolder, p)).Distinct().Take(16);

            foreach (var file in allFiles)
            {
                try
                {
                    var bytes = File.ReadAllBytes(file);
                    using (var ms = new MemoryStream(bytes))
                    {
                        iconList.Add(new Bitmap(Image.FromStream(ms)));
                    }
                }
                catch { /* ignore */ }
            }

            decorationIconsCache[style] = iconList;

            // Load button icon
            spinButtonImages[style] = LoadSingleIcon(buttonIcon);
        }

        private Image LoadSingleIcon(string filename)
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, "Assets", "img", filename);
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    using (var ms = new MemoryStream(bytes))
                    {
                        return new Bitmap(Image.FromStream(ms));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Cập nhật bảng màu segment theo style
        /// </summary>
        private void UpdateSegmentColorsForStyle(WheelStyle style)
        {
            var colorPalettes = new Dictionary<WheelStyle, List<Color>>
            {
                [WheelStyle.Default] = new List<Color>
        {
            Color.LightPink, Color.LightYellow, Color.LightBlue, Color.LightGreen,
            Color.Orange, Color.Violet, Color.LightCoral, Color.LightSkyBlue
        },
                [WheelStyle.TetNguyenDan] = new List<Color>
        {
            Color.FromArgb(255, 220, 50), Color.FromArgb(255, 80, 80),
            Color.FromArgb(255, 200, 100), Color.FromArgb(255, 150, 150),
            Color.Gold, Color.Crimson, Color.Orange, Color.LightCoral
        },
                [WheelStyle.GiangSinh] = new List<Color>
        {
            Color.FromArgb(200, 50, 50), Color.FromArgb(50, 150, 50),
            Color.FromArgb(240, 240, 255), Color.FromArgb(180, 120, 60),
            Color.Red, Color.Green, Color.White, Color.OrangeRed
        },
                [WheelStyle.Halloween] = new List<Color>
        {
            Color.FromArgb(255, 140, 0), Color.FromArgb(50, 50, 50),
            Color.FromArgb(150, 50, 150), Color.FromArgb(100, 200, 100),
            Color.Orange, Color.DarkSlateGray, Color.Purple, Color.DarkOrange
        },
                [WheelStyle.TrungThu] = new List<Color>
        {
            Color.FromArgb(255, 200, 50), Color.FromArgb(255, 100, 100),
            Color.FromArgb(255, 150, 0), Color.FromArgb(200, 180, 100),
            Color.Gold, Color.OrangeRed, Color.Yellow, Color.Coral
        },
                [WheelStyle.ModernNeon] = new List<Color>
        {
            Color.FromArgb(0, 255, 255), Color.FromArgb(255, 0, 255),
            Color.FromArgb(255, 255, 0), Color.FromArgb(0, 255, 127),
            Color.DeepSkyBlue, Color.HotPink, Color.LimeGreen, Color.Violet
        }
            };

            segmentColors = colorPalettes.ContainsKey(style)
                ? colorPalettes[style]
                : colorPalettes[WheelStyle.Default];

            SaveSegmentColorsToFile(segmentColors);
        }

        /// <summary>
        /// Lưu style hiện tại vào file config
        /// </summary>
        private void SaveStyleToFile(WheelStyle style)
        {
            try
            {
                var settings = new { CurrentStyle = (int)style };
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                string path = Path.Combine(Application.StartupPath, "wheel_current_style.json");
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch { /* silent fail */ }
        }
        /// <summary>
        /// Tráo đổi ngẫu nhiên vị trí các ô (cả 2 cơ chế)
        /// </summary>
        public void ShuffleSegments()
        {

            if (spinMode == "1")
            {
                // Cơ chế 1: tráo segments (prizes/vouchers)
                ShuffleList(segmentTexts);
                ShuffleList(segmentImages);
                ShuffleList(segmentImageCache);
                ShuffleList(segmentCodes);
                ShuffleList(segmentRates);
            }
            else
            {
                // Cơ chế 2: tráo phone numbers
                ShuffleList(phoneNumberList);

                // Cập nhật kokakuBindingList / gcPhoneNumbers
                try
                {
                    kokakuBindingList.RaiseListChangedEvents = false;
                    kokakuBindingList.Clear();
                    foreach (var p in phoneNumberList)
                    {
                        kokakuBindingList.Add(new LuckyDrawKokakuDTO
                        {
                            KOKAKU_HITO_PHONE = p.PhoneNumber,
                            KOKAKU_HITO_NAME = p.CustomerName
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ShuffleSegments update grid error: {ex.Message}");
                }
                finally
                {
                    kokakuBindingList.RaiseListChangedEvents = true;
                    (gcPhoneNumbers?.MainView as GridView)?.RefreshData();
                    gcPhoneNumbers?.Refresh();
                }

                // Cập nhật segments để khớp với danh sách mới
                UpdateNumSegments(phoneNumberList.Count);
            }

            // Vẽ lại
            this.Invalidate();

        }

        /// <summary>
        /// Helper: tráo đổi ngẫu nhiên một List
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            if (list == null || list.Count <= 1) return;

            var random = new Random();
            int n = list.Count;

            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                // Swap
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Lưu cấu hình vòng quay ra file
        /// </summary>
        private void SaveWheelSettings()
        {

            {
                var settings = new
                {
                    SpinDurationSeconds = spinDurationSeconds
                };

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                string path = Path.Combine(Application.StartupPath, SETTINGS_FILE);
                File.WriteAllText(path, json, Encoding.UTF8);
            }

        }

        /// <summary>
        /// Đọc cấu hình vòng quay từ file
        /// </summary>
        private void LoadWheelSettings()
        {

            {
                string path = Path.Combine(Application.StartupPath, SETTINGS_FILE);

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    var settings = JsonConvert.DeserializeAnonymousType(json, new { SpinDurationSeconds = 5 });

                    if (settings != null)
                    {
                        // Clamp khi đọc từ file để tránh giá trị ngoài mong muốn
                        int loaded = settings.SpinDurationSeconds;
                        if (loaded < 5) loaded = 5;
                        if (loaded > 30) loaded = 30;

                        spinDurationSeconds = loaded;
                        spinDurationMs = spinDurationSeconds * 1000;

                    }
                }
            }

            {
                spinDurationSeconds = 5; // Fallback
                spinDurationMs = spinDurationSeconds * 1000;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private const int CAPTUREBLT = unchecked((int)0x40000000);

        private string CaptureAppScreenshotAsBase64()
        {
            try
            {
                Rectangle vs = SystemInformation.VirtualScreen;
                if (vs.Width <= 0 || vs.Height <= 0) return null;

                IntPtr hDesktopDC = IntPtr.Zero;
                IntPtr hMemDC = IntPtr.Zero;
                IntPtr hBitmap = IntPtr.Zero;
                IntPtr hOld = IntPtr.Zero;
                Bitmap managedBmp = null;
                Bitmap finalBmp = null;

                try
                {
                    // Lấy DC toàn màn hình
                    hDesktopDC = GetDC(IntPtr.Zero);
                    if (hDesktopDC == IntPtr.Zero)
                        throw new InvalidOperationException("GetDC(IntPtr.Zero) returned zero");

                    hMemDC = CreateCompatibleDC(hDesktopDC);
                    if (hMemDC == IntPtr.Zero)
                        throw new InvalidOperationException("CreateCompatibleDC returned zero");

                    hBitmap = CreateCompatibleBitmap(hDesktopDC, vs.Width, vs.Height);
                    if (hBitmap == IntPtr.Zero)
                        throw new InvalidOperationException("CreateCompatibleBitmap returned zero");

                    hOld = SelectObject(hMemDC, hBitmap);

                    int rop = SRCCOPY | CAPTUREBLT;
                    bool bltOk = BitBlt(hMemDC, 0, 0, vs.Width, vs.Height, hDesktopDC, vs.Left, vs.Top, rop);

                    if (!bltOk)
                    {
                        // fallback an toàn
                        using (var tmp = new Bitmap(vs.Width, vs.Height, PixelFormat.Format32bppArgb))
                        using (var g = Graphics.FromImage(tmp))
                        {
                            g.CopyFromScreen(vs.Location, Point.Empty, vs.Size);
                            return EncodeBitmapToBase64WithLimits(tmp);
                        }
                    }

                    managedBmp = Image.FromHbitmap(hBitmap);

                    finalBmp = new Bitmap(managedBmp.Width, managedBmp.Height, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(finalBmp))
                    {
                        g.DrawImage(managedBmp, 0, 0);
                    }

                    return EncodeBitmapToBase64WithLimits(finalBmp);
                }
                finally
                {
                    try { if (hOld != IntPtr.Zero && hMemDC != IntPtr.Zero) SelectObject(hMemDC, hOld); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    try { if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    try { if (hMemDC != IntPtr.Zero) DeleteDC(hMemDC); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    try { if (hDesktopDC != IntPtr.Zero) ReleaseDC(IntPtr.Zero, hDesktopDC); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    try { managedBmp?.Dispose(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    try { finalBmp?.Dispose(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
            }
            {

                // fallback CopyFromScreen
                try
                {
                    Rectangle vs2 = SystemInformation.VirtualScreen;
                    if (vs2.Width > 0 && vs2.Height > 0)
                    {
                        using (var bmp = new Bitmap(vs2.Width, vs2.Height, PixelFormat.Format32bppArgb))
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.CopyFromScreen(vs2.Location, Point.Empty, vs2.Size);
                            return EncodeBitmapToBase64WithLimits(bmp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
                return null;
            }
        }

        // new GDI interop helpers for full-desktop capture (includes taskbar, multiple monitors)
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, System.Int32 dwRop);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        private const int SRCCOPY = unchecked((int)0x00CC0020);


        // Helper để encode bitmap giống logic cũ (JPEG ưu tiên, giới hạn 3MB, resize nếu cần)
        private string EncodeBitmapToBase64WithLimits(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                ImageCodecInfo jpgEncoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                if (jpgEncoder != null)
                {
                    var encParams = new EncoderParameters(1);
                    encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
                    bmp.Save(ms, jpgEncoder, encParams);
                }
                else
                {
                    bmp.Save(ms, ImageFormat.Png);
                }

                byte[] bytes = ms.ToArray();

                const int maxBytes = 3 * 1024 * 1024;
                if (bytes.Length > maxBytes)
                {
                    int newW = Math.Max(200, (int)(bmp.Width * 0.7));
                    int newH = Math.Max(200, (int)(bmp.Height * 0.7));
                    using (var small = ResizeBitmap(bmp, newW, newH))
                    {
                        ms.SetLength(0);
                        if (jpgEncoder != null)
                        {
                            var encParams2 = new EncoderParameters(1);
                            encParams2.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 70L);
                            small.Save(ms, jpgEncoder, encParams2);
                        }
                        else
                        {
                            small.Save(ms, ImageFormat.Png);
                        }
                        bytes = ms.ToArray();
                    }
                }

                if (bytes.Length > maxBytes)
                {
                    return null;
                }

                return Convert.ToBase64String(bytes);
            }
        }

        // Win32 interop (sử dụng fully-qualified attribute để không cần thêm using)
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
        private Bitmap ResizeBitmap(Bitmap src, int width, int height)
        {
            var dest = new Bitmap(width, height);
            dest.SetResolution(src.HorizontalResolution, src.VerticalResolution);
            using (var g = Graphics.FromImage(dest))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(src, 0, 0, width, height);
            }
            return dest;
        }


        private readonly Dictionary<string, Image> prizeImageCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private readonly object prizeImageCacheLock = new object();
        private void ClearPrizeImageCache()
        {
            lock (prizeImageCacheLock)
            {
                foreach (var kv in prizeImageCache)
                {
                    try { kv.Value?.Dispose(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                }
                prizeImageCache.Clear();
            }
        }

        private Image GetPrizeImage(string imgField)
        {
            if (string.IsNullOrEmpty(imgField)) return null;

            lock (prizeImageCacheLock)
            {
                if (prizeImageCache.TryGetValue(imgField, out var cached) && cached != null)
                    return cached;
            }

            Image imgToCache = null;

            // Try load from Images folder first (filename)
            try
            {
                string imageFolder = Path.Combine(Application.StartupPath, "Images");
                string imagePath = Path.Combine(imageFolder, imgField);
                if (File.Exists(imagePath))
                {
                    var bytes = File.ReadAllBytes(imagePath);
                    using (var ms = new MemoryStream(bytes))
                    using (var tmp = Image.FromStream(ms))
                    {
                        imgToCache = new Bitmap(tmp);
                    }
                }
            }
            catch { imgToCache = null; }

            // If not a file, try decode base64 (if long enough)
            if (imgToCache == null)
            {
                try
                {
                    string b = imgField;
                    int idx = b.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0) b = b.Substring(idx + 7);
                    b = b.Replace("\r", "").Replace("\n", "").Replace(" ", "");
                    int mod = b.Length % 4;
                    if (mod == 2) b += "=="; else if (mod == 3) b += "=";
                    if (b.Length > 80)
                    {
                        var bytes = Convert.FromBase64String(b);
                        using (var ms = new MemoryStream(bytes))
                        using (var tmp = Image.FromStream(ms))
                        {
                            imgToCache = new Bitmap(tmp);
                        }
                    }
                }
                catch { imgToCache = null; }
            }

            lock (prizeImageCacheLock)
            {
                try { prizeImageCache[imgField] = imgToCache; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            }

            return imgToCache;
        }


        private List<Color> segmentColors = new List<Color>
        {
            Color.LightPink, Color.LightYellow, Color.LightBlue, Color.LightGreen,
            Color.Orange, Color.Violet, Color.LightCoral, Color.LightSkyBlue
        };
        private List<(string PhoneNumber, string CustomerName)> phoneNumberList = new List<(string, string)>();
        private System.ComponentModel.BindingList<LuckyDrawKokakuDTO> kokakuBindingList = new System.ComponentModel.BindingList<LuckyDrawKokakuDTO>();
        private float wheelRotation = 0f; // Góc quay hiện tại
        private Timer spinTimer;
        private float spinSpeed = 0f;
        private float targetRotation = 0f;
        private bool isSpinning = false;
        private Rectangle btnRect; // Vùng nút QUAY 

        private int finalWinnerIndex = -1;
        private readonly Random rnd = new Random();

        //private bool isBtnAnimating = false;
        //private float btnAnimScale = 1.0f;
        //private Timer btnAnimTimer;
        //private int btnAnimStep = 0;

        private string bannerHeader1 = "";
        private string bannerHeader2 = "";
        private string bannerHeader3 = "";

        private int bannerHeight = 75; // chiều cao banner
        private int marqueeMargin = 52; // khoảng cách giữa banner và dòng chữ chạy

        private HashSet<string> highlightedPhoneNumbers = new HashSet<string>();
        public string LoadedEventId { get; private set; }

        private Timer fireworkDisplayTimer;
        private int fireworkShowCount = 0;
        private const int FIREWORK_DURATION_MS = 1500;
        private List<PictureBox> fireworkGifs = new List<PictureBox>();
        private const int FIREWORK_COUNT = 50;
        // Thêm vào khu vực khai báo biến (gần dòng fireworkShowCount)
        private const int FIREWORK_BLINK_COUNT = 6; // Số lần nhấp nháy (hiện/ẩn)
        private bool fireworkVisible = true; // Trạng thái hiện/ẩn hiện tại
        // Hiệu ứng pháo hoa
        //private List<FireworkParticle> fireworks = new List<FireworkParticle>();
        //private Timer fireworkTimer;
        //private bool showFirework = false;
        //private int fireworkDuration = 1500; // ms
        //private DateTime fireworkStart;
        //private int fireworkRepeatCount = 0;
        //private Timer fireworkRepeatTimer;

        //public class FireworkParticle
        //{
        //    public PointF Position;
        //    public float Angle;
        //    public float Speed;
        //    public Color Color;
        //    public int Life;
        //}

        //private void StartFirework(int repeat = 2)
        //{
        //    fireworks.Clear();
        //    var rnd = new Random();
        //    int centerX = this.Width / 2;
        //    int centerY = bannerHeight + 60 + 40 + (Math.Min(this.Width, this.Height - bannerHeight) - 200) / 2;
        //    for (int i = 0; i < 250; i++)
        //    {
        //        float angle = (float)(rnd.NextDouble() * 2 * Math.PI);
        //        float speed = (float)(rnd.NextDouble() * 30 + 19);
        //        Color color = Color.FromArgb(255, rnd.Next(256), rnd.Next(256), rnd.Next(256));
        //        fireworks.Add(new FireworkParticle
        //        {
        //            Position = new PointF(centerX, centerY),
        //            Angle = angle,
        //            Speed = speed,
        //            Color = color,
        //            Life = rnd.Next(58, 78)
        //        });
        //    }
        //    showFirework = true;
        //    fireworkStart = DateTime.Now;
        //    fireworkTimer.Start();

        //    fireworkRepeatCount = repeat - 1;
        //    if (fireworkRepeatTimer == null)
        //    {
        //        fireworkRepeatTimer = new Timer();
        //        fireworkRepeatTimer.Interval = fireworkDuration + 100; // thời gian giữa 2 lần pháo hoa
        //        fireworkRepeatTimer.Tick += FireworkRepeatTimer_Tick;
        //    }
        //    fireworkRepeatTimer.Stop();
        //    if (fireworkRepeatCount > 0)
        //        fireworkRepeatTimer.Start();
        //}
        //private void FireworkRepeatTimer_Tick(object sender, EventArgs e)
        //{
        //    if (fireworkRepeatCount >= 0) // Đảm bảo gọi đủ số lần
        //    {
        //        StartFirework(fireworkRepeatCount + 1);
        //        fireworkRepeatCount--;
        //    }
        //    else
        //    {
        //        fireworkRepeatTimer.Stop();
        //    }
        //}


        private DateTime? spinStartTime = null;
        public event Action<string> EventSelectedByUser;
        // ⭐⭐⭐ THÊM CÁC FIELDS SAU ĐÂY ⭐⭐⭐
        // Cache Brush/Pen để tránh allocation trong OnPaint
        private SolidBrush _cachedPhoneBrush;
        private Pen _cachedGoldPen;
        private Dictionary<Color, System.Drawing.Drawing2D.LinearGradientBrush> _segmentBrushCache
            = new Dictionary<Color, System.Drawing.Drawing2D.LinearGradientBrush>();
        // Cache font sizes đã tính toán
        private float _cachedPhoneFontSize = 12f;
        private float _cachedNameFontSize = 9f;
        private int _lastCalculatedRadius = -1;

        // Shared firework image (để dispose đúng cách)
        private Image _sharedFireworkImage;

        // Giới hạn font cache
        private const int MAX_FONT_CACHE_SIZE = 50;
        public uc_Wheel()
        {
            InitializeComponent();

            // Ensure date editors show today's date by default when control is created
            try
            {
                if (deDateFrom != null)
                {
                    // Only set default when no value assigned yet
                    if (deDateFrom.EditValue == null || deDateFrom.EditValue == DBNull.Value)
                        deDateFrom.DateTime = DateTime.Today;
                }

                if (deDateTo != null)
                {
                    if (deDateTo.EditValue == null || deDateTo.EditValue == DBNull.Value)
                        deDateTo.DateTime = DateTime.Today;
                }
            }
            catch
            {
                // silent: designer/runtime contexts may differ
            }

            ConfigureGcPhoneNumbers();
            this.Disposed += (s, e) => DisposeCachedFonts();
            this.BackColor = Color.White;
            this.DoubleBuffered = true;

            this.MinimumSize = new Size(400, 300);
            spinTimer = new Timer();
            spinTimer.Interval = 60;
            spinTimer.Tick += SpinTimer_Tick;
            this.MouseDown += UcWheel_MouseDown;
            // Timer cho animation nút QUAY
            //btnAnimTimer = new Timer();
            //btnAnimTimer.Interval = 25; // ~60fps
            //btnAnimTimer.Tick += BtnAnimTimer_Tick;

            LoadWheelBackgroundImage();

            //Task.Run(async () => await LoadWheelVouchersAsync());
            Task.Run(async () => await LoadBannerHeadersAsync());

            SetupProgramLookup();

            Task.Run(async () => await LoadProgramsIntoLookupAsync());

            // Đặt vị trí bắt đầu của label sát mép trái
            marqueeLabel.Left = 0;
            marqueeLabel.Top = bannerHeight + marqueeMargin;

            // Khởi tạo và chạy Timer
            marqueeTimer = new Timer();
            marqueeTimer.Interval = 25; // càng nhỏ càng mượt
            marqueeTimer.Tick += MarqueeTimer_Tick;
            marqueeTimer.Start();

            EnsureMarqueeBehindDockPanel();
            // Đảm bảo label luôn nằm đúng vị trí khi resize
            int extraOffset = 40; // Số pixel muốn dời xuống, có thể chỉnh lại cho phù hợp
            marqueeLabel.Top = bannerHeight + marqueeMargin + extraOffset;

            this.Resize += (s, e) =>
            {
                marqueeLabel.Top = bannerHeight + marqueeMargin + extraOffset;
                EnsureMarqueeBehindDockPanel();
            };

            // Khởi tạo timer pháo hoa
            //fireworkTimer = new Timer();
            //fireworkTimer.Interval = 24;
            //fireworkTimer.Tick += FireworkTimer_Tick;

            LoadSegmentColorsFromFile();
            LoadWheelSettings();
            // Thiết lập trạng thái ban đầu
            //txtPhoneNumber.Visible = false; // Đảm bảo ô nhập bị ẩn
            SetSpinMode("1");
            ConfigureLstPrizes();
            LoadSavedStyle();
            InitializeFireworkGifs();

            //try
            //{
            //    lstPhoneNumbers.DrawItem -= LstPhoneNumbers_DrawItem;
            //    lstPhoneNumbers.SelectedIndexChanged -= LstPhoneNumbers_SelectedIndexChanged;
            //}
            //catch { /* ignore if not registered */ }

            //lstPhoneNumbers.Visible = false;

            //try
            //{
            //    // clear non-public DoubleBuffered set for ListBox (no-op if fails)
            //    typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
            //        ?.SetValue(lstPhoneNumbers, false, null);
            //}
            //catch { /* ignore */ }
        }
        /// <summary>
        /// Khởi tạo các PictureBox để hiển thị GIF pháo hoa (TỐI ƯU)
        /// </summary>
        private void InitializeFireworkGifs()
        {
            try
            {
                // ⭐ GIẢM KÍCH THƯỚC XUỐNG 
                int fireworkSize = 80;

                // ⭐ TẠM DỪNG LAYOUT KHI THÊM 8 CONTROLS
                this.SuspendLayout();

                try
                {
                    for (int i = 0; i < FIREWORK_COUNT; i++)
                    {
                        var fireworkGif = new PictureBox
                        {
                            SizeMode = PictureBoxSizeMode.Zoom,
                            BackColor = Color.Transparent,
                            Visible = false,
                            Width = fireworkSize,
                            Height = fireworkSize,
                            Name = $"fireworkGif{i}",
                            // ⭐ TẮT DOUBLE BUFFERING (PictureBox tự quản lý)
                            // ⭐ THÊM: Anchor = None để tránh resize tự động
                            Anchor = AnchorStyles.None
                        };

                        this.Controls.Add(fireworkGif);
                        fireworkGifs.Add(fireworkGif);
                    }
                }
                finally
                {
                    // ⭐ RESUME LAYOUT
                    this.ResumeLayout(false); // false = không invalidate ngay
                }

                // Timer
                fireworkDisplayTimer = new Timer
                {
                    Interval = FIREWORK_DURATION_MS
                };
                fireworkDisplayTimer.Tick += FireworkDisplayTimer_Tick;

                // Load GIF
                LoadFireworkGif();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeFireworkGifs error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load file GIF pháo hoa từ thư mục Assets
        /// </summary>
        private void LoadFireworkGif()
        {
            // ⭐ ĐỔI TỪ GIF SANG PNG ĐỂ TRÁNH ANIMATION LAG
            string gifPath = Path.Combine(Application.StartupPath, "Assets", "img", "firework.png");

            if (File.Exists(gifPath))
            {
                // Load ảnh 1 lần duy nhất (shared image)
                Image sharedImage = Image.FromFile(gifPath);

                foreach (var pictureBox in fireworkGifs)
                {
                    pictureBox.Image = sharedImage; // Dùng chung 1 image
                }
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(gifPath));
            }
        }

        /// <summary>
        /// Timer tick để tạo hiệu ứng nhấp nháy pháo hoa
        /// </summary>
        private void FireworkDisplayTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Đảo trạng thái hiện/ẩn
                fireworkVisible = !fireworkVisible;

                this.SuspendLayout();
                try
                {
                    foreach (var fireworkGif in fireworkGifs)
                    {
                        fireworkGif.Visible = fireworkVisible;
                    }
                }
                finally
                {
                    this.ResumeLayout(true);
                }

                // Tăng biến đếm mỗi lần ẨN (để đếm số lần nhấp nháy hoàn chỉnh)
                if (!fireworkVisible)
                {
                    fireworkShowCount++;
                }

                // Dừng khi đã nhấp nháy đủ số lần
                if (fireworkShowCount >= FIREWORK_BLINK_COUNT)
                {
                    HideFireworks();
                    fireworkDisplayTimer.Stop();
                    fireworkShowCount = 0;
                    Debug.WriteLine("Fireworks blinking completed");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FireworkDisplayTimer_Tick error: {ex.Message}");
            }
        }

        /// <summary>
        /// Hiển thị hiệu ứng pháo hoa (nhấp nháy nhiều lần)
        /// </summary>
        private void ShowFireworks()
        {
            try
            {
                if (fireworkGifs == null || fireworkGifs.Count == 0)
                {
                    Debug.WriteLine("No firework PictureBoxes initialized");
                    return;
                }

                // Kiểm tra GIF đã load chưa
                bool hasGif = fireworkGifs.Any(pb => pb.Image != null);
                if (!hasGif)
                {
                    Debug.WriteLine("Firework GIF not loaded");
                    return;
                }

                // TẠM DỪNG RENDERING
                this.SuspendLayout();

                try
                {
                    // ⭐ TÍNH TOÁN CÁC THAM SỐ CẦN THIẾT
                    int bannerHeight = (int)(this.Height * 0.1);
                    int extraWheelOffset = (int)(this.Height * 0.05);
                    int wheelSize = Math.Min(this.Width, this.Height - bannerHeight) - 200;
                    if (wheelSize < 100) wheelSize = 100;

                    int centerX = this.Width / 2;
                    int centerY = bannerHeight + 60 + extraWheelOffset + wheelSize / 2;
                    int radius = wheelSize / 2;

                    // ⭐ THAY ĐỔI: Dùng PositionFireworksRandomly thay vì PositionFireworksInGrid
                    PositionFireworksRandomly(centerX, centerY, radius);

                    // Hiển thị tất cả pháo hoa lần đầu
                    foreach (var fireworkGif in fireworkGifs)
                    {
                        if (fireworkGif.Image != null)
                        {
                            fireworkGif.Visible = true;
                            ImageAnimator.Animate(fireworkGif.Image, null);
                        }
                    }
                }
                finally
                {
                    this.ResumeLayout(true);
                }

                // Reset biến đếm và bắt đầu timer nhấp nháy
                fireworkShowCount = 0;
                fireworkVisible = true;
                fireworkDisplayTimer.Stop();
                fireworkDisplayTimer.Interval = 300; // 300ms mỗi lần nhấp nháy
                fireworkDisplayTimer.Start();

                Debug.WriteLine($"Fireworks blinking started at {fireworkGifs.Count} random positions");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowFireworks error: {ex.Message}");
            }
        }
        /// <summary>
        /// CÁCH 1: Đặt pháo hoa theo vòng tròn xung quanh vòng quay
        /// </summary>
        private void PositionFireworksInCircle(int centerX, int centerY, int wheelRadius)
        {
            // Khoảng cách từ tâm vòng quay đến pháo hoa (xa hơn bán kính một chút)
            int fireworkRadius = wheelRadius + 120;

            // Tính góc giữa mỗi pháo hoa
            float angleStep = 360f / fireworkGifs.Count;

            for (int i = 0; i < fireworkGifs.Count; i++)
            {
                float angle = i * angleStep;
                double rad = angle * Math.PI / 180.0;

                // Tính vị trí X, Y
                int x = centerX + (int)(fireworkRadius * Math.Cos(rad)) - fireworkGifs[i].Width / 2;
                int y = centerY + (int)(fireworkRadius * Math.Sin(rad)) - fireworkGifs[i].Height / 2;

                fireworkGifs[i].Left = x;
                fireworkGifs[i].Top = y;
            }
        }

        /// <summary>
        /// CÁCH 2 (CẢI TIẾN): Đặt pháo hoa ngẫu nhiên nhưng PHÂN BỐ ĐỀU và KHÔNG CHỒNG LẤN
        /// </summary>
        private void PositionFireworksRandomly(int centerX, int centerY, int wheelRadius)
        {
            if (fireworkGifs == null || fireworkGifs.Count == 0) return;

            Random rand = new Random();
            List<Point> positions = new List<Point>();

            // ⭐ THAM SỐ QUAN TRỌNG
            int minDistance = 100; // Khoảng cách tối thiểu giữa các pháo hoa (tránh chồng lấn)
            int maxAttempts = 30;  // Số lần thử tìm vị trí cho mỗi pháo hoa

            // Vùng hiển thị (toàn màn hình, tránh viền)
            int margin = 50;
            Rectangle bounds = new Rectangle(
                margin,
                margin,
                this.Width - 2 * margin,
                this.Height - 2 * margin
            );

            // Vùng cấm (vòng tròn bánh xe + buffer)
            int forbiddenRadius = wheelRadius + 180;

            for (int i = 0; i < fireworkGifs.Count; i++)
            {
                Point newPos = Point.Empty;
                bool foundValidPos = false;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // Random vị trí trong bounds
                    int x = rand.Next(bounds.Left, bounds.Right - fireworkGifs[i].Width);
                    int y = rand.Next(bounds.Top, bounds.Bottom - fireworkGifs[i].Height);

                    Point candidatePos = new Point(x, y);
                    Point candidateCenter = new Point(
                        x + fireworkGifs[i].Width / 2,
                        y + fireworkGifs[i].Height / 2
                    );

                    // ⭐ KIỂM TRA 1: Không nằm trong vùng cấm (bánh xe)
                    double distToWheel = Math.Sqrt(
                        Math.Pow(candidateCenter.X - centerX, 2) +
                        Math.Pow(candidateCenter.Y - centerY, 2)
                    );
                    if (distToWheel < forbiddenRadius)
                    {
                        continue; // Thử lại
                    }

                    // ⭐ KIỂM TRA 2: Không chồng lấn với pháo hoa khác
                    bool tooClose = false;
                    foreach (var existingPos in positions)
                    {
                        Point existingCenter = new Point(
                            existingPos.X + fireworkGifs[i].Width / 2,
                            existingPos.Y + fireworkGifs[i].Height / 2
                        );

                        double dist = Math.Sqrt(
                            Math.Pow(candidateCenter.X - existingCenter.X, 2) +
                            Math.Pow(candidateCenter.Y - existingCenter.Y, 2)
                        );

                        if (dist < minDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        newPos = candidatePos;
                        foundValidPos = true;
                        break;
                    }
                }

                // Nếu không tìm được vị trí hợp lệ -> fallback random đơn giản
                if (!foundValidPos)
                {
                    float angle = (float)(rand.NextDouble() * 360);
                    double rad = angle * Math.PI / 180.0;
                    int distance = wheelRadius + rand.Next(150, 300);

                    int x = centerX + (int)(distance * Math.Cos(rad)) - fireworkGifs[i].Width / 2;
                    int y = centerY + (int)(distance * Math.Sin(rad)) - fireworkGifs[i].Height / 2;

                    x = Math.Max(margin, Math.Min(x, this.Width - fireworkGifs[i].Width - margin));
                    y = Math.Max(margin, Math.Min(y, this.Height - fireworkGifs[i].Height - margin));

                    newPos = new Point(x, y);
                }

                // Lưu vị trí và áp dụng
                positions.Add(newPos);
                fireworkGifs[i].Left = newPos.X;
                fireworkGifs[i].Top = newPos.Y;
            }
        }

        /// <summary>
        /// CÁCH 3: Đặt pháo hoa theo lưới đều
        /// </summary>
        private void PositionFireworksInGrid()
        {
            int cols = (int)Math.Ceiling(Math.Sqrt(fireworkGifs.Count));
            int rows = (int)Math.Ceiling((double)fireworkGifs.Count / cols);

            int spacingX = this.Width / (cols + 1);
            int spacingY = this.Height / (rows + 1);

            for (int i = 0; i < fireworkGifs.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                int x = (col + 1) * spacingX - fireworkGifs[i].Width / 2;
                int y = (row + 1) * spacingY - fireworkGifs[i].Height / 2;

                fireworkGifs[i].Left = x;
                fireworkGifs[i].Top = y;
            }
        }

        /// <summary>
        /// Ẩn hiệu ứng pháo hoa
        /// </summary>
        private void HideFireworks()
        {
            try
            {
                // ⭐ TẠM DỪNG RENDERING
                this.SuspendLayout();

                try
                {
                    foreach (var fireworkGif in fireworkGifs)
                    {
                        // Stop animation TRƯỚC KHI ẩn
                        if (fireworkGif.Image != null)
                        {
                            ImageAnimator.StopAnimate(fireworkGif.Image, null);
                        }

                        fireworkGif.Visible = false;
                    }
                }
                finally
                {
                    // ⭐ RESUME LAYOUT
                    this.ResumeLayout(true);
                }

                Debug.WriteLine("All fireworks hidden");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HideFireworks error: {ex.Message}");
            }
        }
        /// <summary>
        /// Load style đã lưu từ file khi khởi động
        /// </summary>
        private void LoadSavedStyle()
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, "wheel_current_style.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    var settings = JsonConvert.DeserializeAnonymousType(json, new { CurrentStyle = 0 });

                    if (settings != null)
                    {
                        SetWheelStyle(settings.CurrentStyle);
                    }
                }
            }
            catch
            {
                // Fallback: dùng style mặc định (Default - index 0)
                SetWheelStyle(0);
            }
        }
        private readonly Dictionary<(float Size, FontStyle Style), Font> _fontCache = new Dictionary<(float, FontStyle), Font>();
        private Font GetCachedFont(float size, FontStyle style)
        {
            var key = (Size: size, Style: style);
            if (_fontCache.TryGetValue(key, out var f) && f != null) return f;
            if (size < 6f) size = 6f;
            var font = new Font("Segoe UI", size, style);
            _fontCache[key] = font;
            return font;
        }
        private void DisposeCachedFonts()
        {
            foreach (var kv in _fontCache)
            {
                try { kv.Value?.Dispose(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            }
            _fontCache.Clear();
        }

        private void UcWheel_MouseDown(object sender, MouseEventArgs e)
        {
            var screenPt = this.PointToScreen(e.Location);

            // Prizes ListBox: giữ nguyên hành vi hiện có
            var ptPrizes = lstPrizes.PointToClient(screenPt);
            if (!lstPrizes.ClientRectangle.Contains(ptPrizes) && !btnRect.Contains(e.Location))
            {
                lstPrizes.ClearSelected();
                lstPrizes.Invalidate();
            }

            // --- NEW: GridControl (gcPhoneNumbers) handling ---
            try
            {
                if (gcPhoneNumbers != null)
                {
                    var ptGrid = gcPhoneNumbers.PointToClient(screenPt);
                    var gv = gcPhoneNumbers.MainView as GridView;

                    // Click hoàn toàn ngoài vùng GridControl -> clear selection
                    if (!gcPhoneNumbers.ClientRectangle.Contains(ptGrid))
                    {
                        gv?.ClearSelection();
                        gcPhoneNumbers.Refresh();
                    }
                    else if (gv != null)
                    {
                        // Tính hit info để biết click vào row hay vùng trống
                        var hit = gv.CalcHitInfo(ptGrid);

                        if (hit.InRow || hit.InRowCell)
                        {
                            // Focus và chọn hàng người dùng click
                            gv.FocusedRowHandle = hit.RowHandle;
                            gv.ClearSelection();
                            gv.SelectRow(hit.RowHandle);
                        }
                        else
                        {
                            // Click trong grid nhưng không trên row (ví dụ header / vùng trống) -> clear
                            gv.ClearSelection();
                            gcPhoneNumbers.Refresh();
                        }
                    }
                }
            }
            catch
            {
                // Swallow exceptions to keep UI responsive
            }

            // Winners ListBox: giữ nguyên hành vi hiện có
            var ptWinners = lstWinners.PointToClient(screenPt);
            if (!lstWinners.ClientRectangle.Contains(ptWinners))
            {
                lstWinners.ClearSelected();
                lstWinners.Invalidate();
            }
        }
        private void LstPhoneNumbers_DrawItem(object sender, DrawItemEventArgs e)
        {
            //if (e.Index < 0) return;

            //// Vẽ nền + selection
            //e.DrawBackground();

            //object item = lstPhoneNumbers.Items[e.Index];
            //string phoneNumber = string.Empty;
            //string customerName = string.Empty;

            //if (item is LuckyDrawKokakuDTO kokaku)
            //{
            //    phoneNumber = kokaku.KOKAKU_HITO_PHONE ?? string.Empty;
            //    customerName = kokaku.KOKAKU_HITO_NAME ?? string.Empty;
            //}
            //else
            //{
            //    phoneNumber = item?.ToString() ?? string.Empty;
            //    customerName = string.Empty;
            //}

            //bool isHighlighted = highlightedPhoneNumbers.Contains(phoneNumber);
            //Color textColor = isHighlighted ? Color.Red : e.ForeColor;

            //Font phoneFont = null;
            //Font nameFont = null;
            //try
            //{
            //    // Cả hai dòng dùng cùng kích thước font (nhỏ hơn), phone in đậm
            //    float fontSize = Math.Max(8f, e.Font.Size - 2f);
            //    phoneFont = new Font(e.Font.FontFamily, fontSize, FontStyle.Bold);
            //    nameFont = new Font(e.Font.FontFamily, fontSize, FontStyle.Regular);

            //    var g = e.Graphics;
            //    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            //    int paddingLeft = 6;
            //    int paddingRight = 6;
            //    int availableWidth = e.Bounds.Width - paddingLeft - paddingRight;

            //    // Chia item thành 2 vùng bằng nhau để đảm bảo hai dòng có cùng chiều cao
            //    int halfHeight = Math.Max(1, e.Bounds.Height / 2);

            //    // Căn giữa từng dòng trong nửa ô tương ứng
            //    var phoneSize = g.MeasureString(phoneNumber, phoneFont);
            //    var nameSize = g.MeasureString(customerName, nameFont);

            //    int phoneY = e.Bounds.Top + (halfHeight - (int)phoneSize.Height) / 2;
            //    int nameY = e.Bounds.Top + halfHeight + (halfHeight - (int)nameSize.Height) / 2;

            //    // Nếu không có tên, căn phone ở giữa toàn bộ ô
            //    if (string.IsNullOrEmpty(customerName))
            //    {
            //        phoneY = e.Bounds.Top + (e.Bounds.Height - (int)phoneSize.Height) / 2;
            //    }

            //    using (var brush = new SolidBrush(textColor))
            //    {
            //        // Dòng số điện thoại (truncate nếu cần)
            //        var phoneRect = new Rectangle(e.Bounds.Left + paddingLeft, phoneY, availableWidth, (int)phoneSize.Height);
            //        g.DrawString(phoneNumber, phoneFont, brush, phoneRect);

            //        // Dòng tên (nằm dưới)
            //        if (!string.IsNullOrEmpty(customerName))
            //        {
            //            var nameRect = new Rectangle(e.Bounds.Left + paddingLeft, nameY, availableWidth, (int)nameSize.Height);
            //            g.DrawString(customerName, nameFont, brush, nameRect);
            //        }
            //    }
            //}
            //finally
            //{
            //    phoneFont?.Dispose();
            //    nameFont?.Dispose();
            //}

            //e.DrawFocusRectangle();
        }

        private void MarkPhoneNumberAsWinner(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return;

            if (!highlightedPhoneNumbers.Contains(phoneNumber))
            {
                highlightedPhoneNumbers.Add(phoneNumber);

                // Refresh grid so RowCellStyle updates
                try
                {
                    (gcPhoneNumbers?.MainView as GridView)?.RefreshData();
                    gcPhoneNumbers?.Refresh();
                }
                catch { /* ignore refresh errors */ }
            }
        }
        private void uc_Wheel_Resize(object sender, EventArgs e)
        {
            int bannerHeight = (int)(this.Height * 0.1);
            int extraWheelOffset = (int)(this.Height * 0.05);

            // Kích thước bánh xe
            int wheelSize = Math.Min(this.Width, this.Height - bannerHeight) - 200;
            if (wheelSize < 100) wheelSize = 100; // Kích thước tối thiểu cho bánh xe
            int centerX = this.Width / 2;
            int centerY = bannerHeight + 60 + extraWheelOffset + wheelSize / 2;
            int radius = wheelSize / 2;

            // Kích thước và khoảng cách cho text box
            int textBoxWidth = (int)(this.Width * 0.25);
            int textBoxHeight = (int)(this.Height * 0.05);
            int textBoxSpacing = (int)(this.Height * 0.02);
            if (this.Width < 500 || this.Height < 400) // Điều chỉnh khi cửa sổ nhỏ
            {
                textBoxWidth = (int)(this.Width * 0.2);
                textBoxHeight = (int)(this.Height * 0.04);
                textBoxSpacing = (int)(this.Height * 0.01);
            }
            if (textBoxWidth < 120) textBoxWidth = 120; // Độ rộng tối thiểu

            // Vị trí text box
            txtPhone.Width = textBoxWidth;
            txtPhone.Height = textBoxHeight;
            txtPhone.Left = centerX - textBoxWidth / 2;
            txtPhone.Top = bannerHeight + 20;

            txtCustomerName.Width = textBoxWidth;
            txtCustomerName.Height = textBoxHeight;
            txtCustomerName.Left = centerX - textBoxWidth / 2;
            txtCustomerName.Top = txtPhone.Bottom + textBoxSpacing;

            txtInvoiceNumber.Width = textBoxWidth;
            txtInvoiceNumber.Height = textBoxHeight;
            txtInvoiceNumber.Left = centerX - textBoxWidth / 2;
            txtInvoiceNumber.Top = txtCustomerName.Bottom + textBoxSpacing;

            // Vị trí marquee
            int extraOffset = 30; // Giảm offset để tiết kiệm không gian
            int marqueeTop = txtInvoiceNumber.Bottom + textBoxSpacing + extraOffset;
            if (marqueeTop + marqueeLabel.Height > centerY - radius - 50) // Kiểm tra chồng lấn với bánh xe
            {
                marqueeLabel.Visible = false; // Ẩn marquee nếu không đủ không gian
            }
            else
            {
                marqueeLabel.Visible = true;
                marqueeLabel.Top = marqueeTop;
                marqueeLabel.Width = (int)(this.Width * 0.7); // Giảm chiều rộng marquee
                marqueeLabel.Left = (this.Width - marqueeLabel.Width) / 2;
            }
            EnsureMarqueeBehindDockPanel();

            // Điều chỉnh font size
            AdjustFontSize();

            // Cập nhật vị trí bánh xe
            btnRect = new Rectangle(
                centerX - wheelSize / 2,
                centerY - wheelSize / 2,
                wheelSize,
                wheelSize
            );

            this.Invalidate();
        }

        private void AdjustFontSize()
        {
            // Compute base size like before
            float baseFontSize = Math.Min(this.Width, this.Height) * 0.015f;
            float minFontSize = 6f;
            float maxFontSize = 20f;

            float fontSize = baseFontSize;
            if (fontSize < minFontSize) fontSize = minFontSize;
            if (fontSize > maxFontSize) fontSize = maxFontSize;

            // Textboxes keep same family but scaled
            txtPhone.Font = new Font(txtPhone.Font.FontFamily, fontSize, FontStyle.Regular);
            txtCustomerName.Font = new Font(txtCustomerName.Font.FontFamily, fontSize, FontStyle.Regular);
            txtInvoiceNumber.Font = new Font(txtInvoiceNumber.Font.FontFamily, fontSize, FontStyle.Regular);

            // Marquee: dùng cùng font family + style như banner (Segoe UI, Bold),
            // nhưng kích thước nhỏ hơn header để không quá to.
            // Tùy chỉnh hệ số scale nếu muốn marquee lớn/nhỏ hơn.
            try
            {
                if (marqueeLabel != null)
                {
                    float marqueeScale = 0.85f; // tỷ lệ so với fontSize tính ở trên
                    float marqueeSize = fontSize * marqueeScale;

                    // đảm bảo trong khoảng hợp lý
                    float marqueeMin = 10f;
                    float marqueeMax = 28f;
                    if (marqueeSize < marqueeMin) marqueeSize = marqueeMin;
                    if (marqueeSize > marqueeMax) marqueeSize = marqueeMax;

                    // Set font to match banner header style (Segoe UI, Bold)
                    marqueeLabel.Font = new Font("Segoe UI", marqueeSize, FontStyle.Bold);

                    // Optionally match marquee color to banner header color
                    marqueeLabel.ForeColor = Color.DarkGoldenrod;

                    // Reset position to avoid clipping when font size changes
                    if (marqueeLabel.Left > this.Width) marqueeLabel.Left = -marqueeLabel.Width;
                }
            }
            catch
            {
                // swallow - do not let font adjustments break UI
            }
        }
        //private void FireworkTimer_Tick(object sender, EventArgs e)
        //{
        //    foreach (var p in fireworks)
        //    {
        //        p.Position = new PointF(
        //            p.Position.X + (float)(Math.Cos(p.Angle) * p.Speed),
        //            p.Position.Y + (float)(Math.Sin(p.Angle) * p.Speed)
        //        );
        //        p.Speed *= 0.93f;
        //        p.Life--;
        //    }
        //    fireworks.RemoveAll(p => p.Life <= 0);
        //    if ((DateTime.Now - fireworkStart).TotalMilliseconds > fireworkDuration || fireworks.Count == 0)
        //    {
        //        showFirework = false;
        //        fireworkTimer.Stop();
        //    }
        //    this.Invalidate();
        //}

        private void SetupProgramLookup()
        {
            if (gridLookUpEditEvent == null) return;

            var view = gridLookUpEditEvent.Properties.View as GridView;
            if (view == null) return;

            view.Columns.Clear();
            view.OptionsView.ColumnAutoWidth = false;
            view.OptionsView.ShowIndicator = false;

            // Hiển thị trong popup: Mã chương trình, Tên chương trình, Mã sự kiện
            view.Columns.AddVisible("LUCKY_DRAW_PROGRAME_ID", "Mã chương trình");
            view.Columns.AddVisible("PROGRAME_NAME", "Tên chương trình");
            view.Columns.AddVisible("LUCKY_DRAW_ID", "Mã sự kiện");

            view.BestFitColumns();

            // ValueMember = mã chương trình (giữ làm khóa)
            gridLookUpEditEvent.Properties.ValueMember = "LUCKY_DRAW_PROGRAME_ID";

            // DisplayMember trong popup vẫn là tên, nhưng chúng ta sẽ custom text hiển thị phía trên (editor)
            gridLookUpEditEvent.Properties.DisplayMember = "PROGRAME_NAME";
            gridLookUpEditEvent.Properties.NullText = "Chọn chương trình...";
            gridLookUpEditEvent.Properties.ShowFooter = false;
            gridLookUpEditEvent.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;

            // Đăng ký event CustomDisplayText để editor hiển thị mã chương trình thay vì object.ToString()
            gridLookUpEditEvent.CustomDisplayText -= GridLookUpEditEvent_CustomDisplayText;
            gridLookUpEditEvent.CustomDisplayText += GridLookUpEditEvent_CustomDisplayText;

            // Gỡ đăng ký an toàn trước khi đăng ký lại EditValueChanged
            gridLookUpEditEvent.EditValueChanged -= GridLookUpEditProgram_EditValueChanged;
            gridLookUpEditEvent.EditValueChanged += GridLookUpEditProgram_EditValueChanged;

            // Đăng ký QueryPopUp để debug / force reload khi dropdown mở
            gridLookUpEditEvent.Properties.QueryPopUp -= GridLookUpEditEvent_QueryPopUp;
            gridLookUpEditEvent.Properties.QueryPopUp += GridLookUpEditEvent_QueryPopUp;

        }

        // New handler: ép editor text hiển thị mã chương trình (LUCKY_DRAW_PROGRAME_ID)
        private void GridLookUpEditEvent_CustomDisplayText(object sender, DevExpress.XtraEditors.Controls.CustomDisplayTextEventArgs e)
        {
            try
            {
                // Nếu giá trị rỗng thì để mặc định
                if (e.Value == null)
                {
                    e.DisplayText = string.Empty;
                    return;
                }

                // Thông thường e.Value là ValueMember (LUCKY_DRAW_PROGRAME_ID) kiểu string
                var valStr = e.Value as string;
                if (!string.IsNullOrWhiteSpace(valStr))
                {
                    e.DisplayText = valStr;
                    return;
                }

                // Fallback: nếu Value không phải string, lấy đối tượng đang chọn và đọc trường mã
                var edit = sender as GridLookUpEdit;
                if (edit != null)
                {
                    var row = edit.GetSelectedDataRow() as LuckyDrawProgrameDTO;
                    if (row != null)
                    {
                        e.DisplayText = row.LUCKY_DRAW_PROGRAME_ID ?? string.Empty;
                        return;
                    }
                }

                // cuối cùng dùng ToString() an toàn
                e.DisplayText = e.Value.ToString();
            }
            catch
            {
            }
        }

        private async void GridLookUpEditEvent_QueryPopUp(object sender, EventArgs e)
        {
            try
            {
                var currentDs = gridLookUpEditEvent?.Properties?.DataSource as System.Collections.IList;
                if (currentDs != null && currentDs.Count > 0) return; // ✅ Đã có data

                // ⭐ CHỈ GỌI METHOD LOAD (không gọi API trực tiếp)
                await LoadProgramsIntoLookupAsync(forceReload: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[QueryPopUp] Error: {ex.Message}");
            }
        }

        private async Task LoadProgramsIntoLookupAsync(bool forceReload = false)
        {
            try
            {
                if (gridLookUpEditEvent == null) return;

                // If not forced and DataSource already contains items, skip heavy reload.
                var currentDs = gridLookUpEditEvent.Properties.DataSource as System.Collections.IList;
                if (!forceReload && currentDs != null && currentDs.Count > 0)
                {
                    return;
                }

                var programs = await LuckyDrawService.GetAllProgramsAsync(null) ?? new List<LuckyDrawProgrameDTO>();

                if (this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        gridLookUpEditEvent.Properties.DataSource = programs;
                        (gridLookUpEditEvent.Properties.View as GridView)?.BestFitColumns();
                    });
                }
                else
                {
                    gridLookUpEditEvent.Properties.DataSource = programs;
                    (gridLookUpEditEvent.Properties.View as GridView)?.BestFitColumns();
                }
            }
            catch
            {
            }
        }
        private async void GridLookUpEditProgram_EditValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (gridLookUpEditEvent == null) return;

                var selectedProgramId = gridLookUpEditEvent.EditValue as string;

                if (string.IsNullOrWhiteSpace(selectedProgramId)) return;

                // Lấy thông tin Program (nếu null thì retry load programs)
                var program = await LuckyDrawService.GetProgramByIdAsync(selectedProgramId);
                if (program == null)
                {
                    try { await LoadProgramsIntoLookupAsync(); } catch { }
                    program = await LuckyDrawService.GetProgramByIdAsync(selectedProgramId);
                    if (program == null)
                    {
                        return;
                    }
                }

                var eventId = program.LUCKY_DRAW_ID;
                if (string.IsNullOrWhiteSpace(eventId))
                {
                    // nếu không có event, dùng slogan chương trình làm banner (nếu có)
                    if (!string.IsNullOrWhiteSpace(program.PROGRAME_SLOGAN))
                    {
                        var lines = program.PROGRAME_SLOGAN
                                    .Split(new[] { '\r', '\n', '-' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim())
                                    .Where(l => !string.IsNullOrWhiteSpace(l))
                                    .ToArray();

                        var h1 = lines.Length > 0 ? lines[0] : string.Empty;
                        var h2 = lines.Length > 1 ? lines[1] : string.Empty;
                        var h3 = lines.Length > 2 ? lines[2] : string.Empty;
                        UpdateBannerHeaders(h1, h2, h3);
                    }
                    else
                    {
                        // Nếu không có slogan chương trình, clear banner (hoặc giữ default từ LoadBannerHeadersAsync)
                        UpdateBannerHeaders(string.Empty, string.Empty, string.Empty);
                    }

                    UpdateNumSegments(numSegments);
                    return;
                }

                // Load event general data (banner, background, segment config...)
                await LoadEventToWheelAsync(eventId);

                // Read bunrui from event to decide mode
                var evt = await LuckyDrawService.GetEventByIdAsync(eventId);
                bool eventHasBanner = evt != null &&
                                      (!string.IsNullOrWhiteSpace(evt.LUCKY_DRAW_SLOGAN) ||
                                       !string.IsNullOrWhiteSpace(evt.LUCKY_DRAW_TITLE));

                string bunrui = evt?.LUCKY_DRAW_BUNRUI?.Trim();
                if (!string.IsNullOrEmpty(bunrui)) bunrui = bunrui.ToUpperInvariant();


                if (bunrui == "B")
                {
                    List<LuckyDrawKokakuDTO> kokakus = null;
                    List<LuckyDrawMeisaiDTO> meisais = null;
                    try
                    {
                        kokakus = await LuckyDrawService.GetKokakuByEventAsync(eventId, includeMuko: false);
                    }
                    catch
                    {
                        kokakus = new List<LuckyDrawKokakuDTO>();
                    }

                    try
                    {
                        // IMPORTANT: includeMuko:true để hiển thị cả phần thưởng đã MUKO/expired trên vòng quay
                        meisais = await LuckyDrawService.GetAllMeisaiAsync(eventId, category: null, includeMuko: true);
                    }
                    catch
                    {
                        meisais = new List<LuckyDrawMeisaiDTO>();
                    }

                    // Ensure UI mode set first
                    SetSpinMode("2");

                    // Always use helper (it will marshal to UI thread if needed)
                    LoadKokakusToLstPhoneNumbers(kokakus);

                    // populate prizes
                    LoadPrizesToLstPrizes(meisais ?? new List<LuckyDrawMeisaiDTO>());
                }
                else
                {
                    // Cơ chế 1: load Meisai (vouchers) and fill segments
                    List<LuckyDrawMeisaiDTO> meisais = null;
                    try
                    {
                        // IMPORTANT: includeMuko: true so UI shows expired/locked prizes on wheel
                        meisais = await LuckyDrawService.GetAllMeisaiAsync(eventId, category: null, includeMuko: true);
                    }
                    catch
                    {
                        meisais = new List<LuckyDrawMeisaiDTO>();
                    }

                    if (this.InvokeRequired)
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            SetSpinMode("1");
                            LoadPrizesToLstPrizes(meisais ?? new List<LuckyDrawMeisaiDTO>());
                            UpdateNumSegments(numSegments);
                        }));
                    }
                    else
                    {
                        SetSpinMode("1");
                        LoadPrizesToLstPrizes(meisais ?? new List<LuckyDrawMeisaiDTO>());
                        UpdateNumSegments(numSegments);
                    }
                }

                // If Event lacks banner/title, fallback to program slogan
                if (!eventHasBanner && !string.IsNullOrWhiteSpace(program.PROGRAME_SLOGAN))
                {
                    var lines = program.PROGRAME_SLOGAN
                                .Split(new[] { '\r', '\n', '-' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(l => l.Trim())
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToArray();

                    var h1 = lines.Length > 0 ? lines[0] : string.Empty;
                    var h2 = lines.Length > 1 ? lines[1] : string.Empty;
                    var h3 = lines.Length > 2 ? lines[2] : string.Empty;

                    UpdateBannerHeaders(h1, h2, h3);
                }
            }
            catch
            {
            }
        }

        public async Task LoadEventToWheelAsync(string eventId)
        {
            // Reset nếu eventId rỗng
            if (string.IsNullOrWhiteSpace(eventId))
            {
                LoadedEventId = null;
                return;
            }

            try
            {
                // Lấy event
                var evt = await LuckyDrawService.GetEventByIdAsync(eventId);

                bool isMechanism2 = false;
                if (evt != null)
                {
                    // Detect bunrui == "B" (Cơ chế 2)
                    var bunrui = evt.LUCKY_DRAW_BUNRUI?.Trim();
                    if (!string.IsNullOrEmpty(bunrui) && string.Equals(bunrui, "B", StringComparison.OrdinalIgnoreCase))
                    {
                        isMechanism2 = true;
                    }

                    // Ghi nhận event đang load (dùng bởi frmMain để quyết định có ép numSegments hay không)
                    LoadedEventId = eventId;

                    // Nếu không phải cơ chế 2 thì lấy số ô từ event; nếu là cơ chế 2 thì không ép numSegments tại đây
                    if (!isMechanism2)
                    {
                        this.numSegments = Math.Max(1, evt.LUCKY_DRAW_SLOT_NUM);
                    }

                    // Banner
                    string bannerSource = string.Empty;
                    try
                    {
                        // ⭐ GỌI API 1 LẦN + LẤY FIRSTORDEFAULT LUÔN
                        var prog = (await LuckyDrawService.GetAllProgramsAsync(evt.LUCKY_DRAW_ID))?.FirstOrDefault();

                        if (prog != null && !string.IsNullOrWhiteSpace(prog.PROGRAME_SLOGAN))
                        {
                            bannerSource = prog.PROGRAME_SLOGAN;
                        }
                    }
                    catch
                    {
                        // nếu muốn fallback cũ, có thể uncomment dòng dưới:
                        // bannerSource = evt.LUCKY_DRAW_SLOGAN ?? evt.LUCKY_DRAW_TITLE ?? string.Empty;
                    }

                    var lines = (bannerSource ?? string.Empty)
                                .Split(new[] { '\r', '\n', '-' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(l => l.Trim())
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToArray();

                    var h1 = lines.Length > 0 ? lines[0] : string.Empty;
                    var h2 = lines.Length > 1 ? lines[1] : string.Empty;
                    var h3 = lines.Length > 2 ? lines[2] : string.Empty;
                    UpdateBannerHeaders(h1, h2, h3);


                    // Background (Base64) xử lý nếu có
                    if (!string.IsNullOrWhiteSpace(evt.LUCKY_DRAW_BG_IMG))
                    {
                        try
                        {
                            string err;
                            var bmp = TryDecodeBase64Image(evt.LUCKY_DRAW_BG_IMG, out err);
                            if (bmp != null)
                            {
                                if (this.InvokeRequired)
                                {
                                    this.Invoke((MethodInvoker)(() =>
                                    {
                                        SetWheelBackgroundImage(bmp);
                                        try { bmp.Dispose(); }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                        }
                                    }));
                                }
                                else
                                {
                                    SetWheelBackgroundImage(bmp);
                                    try { bmp.Dispose(); }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                    }
                                }
                            }
                            else
                            {
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    // Nếu không tìm thấy event, clear LoadedEventId để cho biết không có event đang chọn
                    LoadedEventId = null;
                }

                // <<< CHANGE: includeMuko: true so UI sees expired/locked prizes >>>
                var meisais = await LuckyDrawService.GetAllMeisaiAsync(eventId, category: null, includeMuko: true);

                // --- NEW: detect problematic prizes and notify user ---
                try
                {
                    if (meisais != null && meisais.Count > 0)
                    {
                        var problems = new List<string>();
                        foreach (var m in meisais)
                        {
                            var reasons = new List<string>();
                            if (m.LUCKY_DRAW_MEISAI_MUKO_FLG.HasValue && m.LUCKY_DRAW_MEISAI_MUKO_FLG.Value == 1)
                                reasons.Add("MUKO");
                            try
                            {
                                if (m.SYURYOU_DATE.HasValue && m.SYURYOU_DATE.Value.Date < DateTime.Now.Date)
                                    reasons.Add("Expired");
                            }
                            catch { /* ignore date parse errors */ }
                            try
                            {
                                if (m.LUCKY_DRAW_MEISAI_SURYO.HasValue && m.LUCKY_DRAW_MEISAI_SURYO.Value <= 0.0)
                                    reasons.Add("Quantity=0");
                            }
                            catch { /* ignore numeric errors */ }

                            if (reasons.Count > 0)
                            {
                                string id = m.LUCKY_DRAW_MEISAI_NO ?? "(no-id)";
                                string name = !string.IsNullOrWhiteSpace(m.LUCKY_DRAW_MEISAI_NAME) ? m.LUCKY_DRAW_MEISAI_NAME : (m.LUCKY_DRAW_MEISAI_NAIYOU ?? "");
                                problems.Add($"{id} - {name} => {string.Join(", ", reasons)}");
                            }
                        }

                        if (problems.Count > 0)
                        {
                            // Build message (limit to first 50 lines to avoid huge dialogs)
                            int maxShow = 50;
                            var shown = problems.Take(maxShow).ToList();
                            string msg = $"Phát hiện {problems.Count} phần thưởng có trạng thái không hợp lệ (MUKO / Hết hạn / Số lượng = 0). Vui lòng sửa trước khi quay:\n\n" +
                                         string.Join("\n", shown);
                            if (problems.Count > maxShow)
                                msg += $"\n\n... và {problems.Count - maxShow} mục khác.";

                            // Show on UI thread
                            if (this.IsHandleCreated && this.InvokeRequired)
                            {
                                this.BeginInvoke((MethodInvoker)(() => MessageBox.Show(this, msg, "Cảnh báo phần thưởng", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                            }
                            else
                            {
                                MessageBox.Show(this, msg, "Cảnh báo phần thưởng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
                catch
                {
                }

                // Dispose ảnh cũ
                if (segmentImageCache != null)
                {
                    foreach (var img in segmentImageCache) img?.Dispose();
                }

                // Reset lists
                segmentImageCache = new List<Image>();
                segmentTexts = new List<string>();
                segmentImages = new List<string>();
                segmentCodes = new List<string>();
                segmentRates = new List<double>();

                if (meisais != null && meisais.Count > 0)
                {
                    foreach (var m in meisais)
                    {
                        segmentTexts.Add(!string.IsNullOrWhiteSpace(m.LUCKY_DRAW_MEISAI_NAME) ? m.LUCKY_DRAW_MEISAI_NAME : ("Giải " + m.LUCKY_DRAW_MEISAI_NO));
                        segmentCodes.Add(m.LUCKY_DRAW_MEISAI_NO ?? string.Empty);

                        // Map rate field from BE (LUCKY_DRAW_MEISAI_RATE)
                        try
                        {
                            double rate = 0d;
                            // support nullable or non-nullable DTO shapes
                            if (m.LUCKY_DRAW_MEISAI_RATE != null)
                                rate = Convert.ToDouble(m.LUCKY_DRAW_MEISAI_RATE);
                            segmentRates.Add(rate);
                        }
                        catch
                        {
                            segmentRates.Add(0d);
                        }

                        segmentImages.Add(m.LUCKY_DRAW_MEISAI_IMG);

                        if (!string.IsNullOrWhiteSpace(m.LUCKY_DRAW_MEISAI_IMG))
                        {
                            try
                            {
                                string base64 = m.LUCKY_DRAW_MEISAI_IMG;
                                if (base64.Contains(",")) base64 = base64.Split(',')[1];
                                var bytes = Convert.FromBase64String(base64);
                                using (var ms = new MemoryStream(bytes))
                                {
                                    var img = Image.FromStream(ms);
                                    segmentImageCache.Add(new Bitmap(img));
                                }
                            }
                            catch
                            {
                                segmentImageCache.Add(null);
                            }
                        }
                        else
                        {
                            segmentImageCache.Add(null);
                        }
                    }
                }

                // Pad / trim lists to match numSegments
                // NOTE: For Cơ chế 2 (bunrui == "B") we should NOT enforce numSegments here,
                // because segments are driven by phoneNumberList and can be dynamic.
                if (!isMechanism2)
                {
                    if (segmentTexts.Count < numSegments)
                    {
                        int missing = numSegments - segmentTexts.Count;
                        for (int i = 0; i < missing; i++)
                        {
                            segmentTexts.Add("Trống");
                            segmentImages.Add(null);
                            segmentImageCache.Add(null);
                            segmentCodes.Add(string.Empty);
                            segmentRates.Add(0d);
                        }
                    }
                    else if (segmentTexts.Count > numSegments)
                    {
                        int extra = segmentTexts.Count - numSegments;
                        segmentTexts.RemoveRange(numSegments, extra);

                        if (segmentImages != null && segmentImages.Count > numSegments)
                            segmentImages.RemoveRange(numSegments, Math.Min(extra, segmentImages.Count - numSegments));

                        if (segmentImageCache != null && segmentImageCache.Count > numSegments)
                        {
                            var toDispose = segmentImageCache.Skip(numSegments).ToList();
                            foreach (var img in toDispose) img?.Dispose();
                            segmentImageCache.RemoveRange(numSegments, Math.Min(extra, segmentImageCache.Count - numSegments));
                        }

                        if (segmentCodes != null && segmentCodes.Count > numSegments)
                            segmentCodes.RemoveRange(numSegments, Math.Min(extra, segmentCodes.Count - numSegments));

                        if (segmentRates != null && segmentRates.Count > numSegments)
                            segmentRates.RemoveRange(numSegments, Math.Min(extra, segmentRates.Count - numSegments));
                    }
                }
                else
                {
                    // mechanism 2: we intentionally avoid trimming/padding here.
                    // The UI will call LoadKokakusToLstPhoneNumbers(...) which triggers UpdateNumSegments(phoneCount).
                }

                // Refresh UI
                if (this.InvokeRequired)
                    this.Invoke((MethodInvoker)(() => this.Invalidate()));
                else
                    this.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu sự kiện: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper: cố gắng decode Base64 thành Image, log chi tiết nếu thất bại
        private Image TryDecodeBase64Image(string base64Raw, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(base64Raw)) return null;

            string s = base64Raw.Trim();

            try
            {
                // Nếu là data URI: "data:image/png;base64,...."
                int idx = s.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) s = s.Substring(idx + 7);

                // Chuẩn hoá: loại bỏ whitespace và convert base64url -> standard
                s = s.Replace("\r", "").Replace("\n", "").Replace(" ", "");
                s = s.Replace('-', '+').Replace('_', '/');

                // Heuristics: nếu chuỗi quá ngắn hoặc chứa kí tự rõ ràng không phải base64 ảnh -> log và bỏ qua
                if (s.Length < 80 || s.IndexOf("IMAGE/", StringComparison.OrdinalIgnoreCase) >= 0
                    || s.StartsWith("{") || s.StartsWith("[") || s.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase) >= 0
                    || s.IndexOf("data:", StringComparison.OrdinalIgnoreCase) == -1 && s.IndexOf('/') >= 0 && s.IndexOf('.') >= 0)
                {
                    // Ghi raw string để debug
                    try
                    {
                        string debugPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bgimg_raw_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                        System.IO.File.WriteAllText(debugPath, base64Raw ?? string.Empty, Encoding.UTF8);
                    }
                    catch { /* swallow */ }

                    error = "Input is not a valid Base64 image (heuristic).";
                    return null;
                }

                // Bổ sung padding nếu thiếu
                int mod = s.Length % 4;
                if (mod == 2) s += "==";
                else if (mod == 3) s += "=";


                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(s);
                }
                catch (FormatException fe)
                {
                    error = "Base64 FormatException: " + fe.Message;
                    // Ghi raw cho debug
                    try
                    {
                        string debugPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bgimg_invalid_base64_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                        System.IO.File.WriteAllText(debugPath, base64Raw ?? string.Empty, Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    return null;
                }

                try
                {
                    using (var ms = new MemoryStream(bytes))
                    {
                        using (var img = Image.FromStream(ms))
                        {
                            // Trả về một Bitmap clone để quản lý lifecycle rõ ràng
                            return new Bitmap(img);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Nếu Image.FromStream thất bại, ghi bytes ra file để phân tích
                    try
                    {
                        string binPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bgimg_bytes_{DateTime.Now:yyyyMMdd_HHmmss}.bin");
                        System.IO.File.WriteAllBytes(binPath, bytes);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }

                    error = "Image.FromStream failed: " + ex.Message;
                    return null;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private async Task LoadBannerHeadersAsync()
        {

            // Use LuckyDrawService to get banner headers (fallback to defaults inside service)
            var (h1, h2, h3) = await LuckyDrawService.GetBannerHeadersAsync();
            bannerHeader1 = h1 ?? "";
            bannerHeader2 = h2 ?? "";
            bannerHeader3 = h3 ?? "";
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)(() => this.Invalidate()));
            else
                this.Invalidate();
        }

        // Thay thế hiện có của UpdateBannerHeaders bằng:
        public void UpdateBannerHeaders(string header1, string header2, string header3)
        {
            this.bannerHeader1 = header1 ?? string.Empty;
            this.bannerHeader2 = header2 ?? string.Empty;
            this.bannerHeader3 = header3 ?? string.Empty;

            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    EnsureMarqueeBehindDockPanel();
                    this.Invalidate();
                }));
            }
            else
            {
                EnsureMarqueeBehindDockPanel();
                this.Invalidate();
            }
        }

        public void SetMarqueeText(string text)
        {
            if (this.IsDisposed || this.Disposing) return;

            Action updateUi = () =>
            {
                try
                {
                    // Ensure visual style matches banner: transparent background, same family + bold
                    if (marqueeLabel != null)
                    {
                        marqueeLabel.Text = text ?? string.Empty;
                        marqueeLabel.Left = 0;
                        marqueeLabel.BackColor = Color.Transparent;
                        marqueeLabel.ForeColor = Color.DarkGoldenrod;
                        // Keep size but force Bold + Segoe UI to match banner style
                        try
                        {
                            float size = marqueeLabel.Font?.Size ?? 14f;
                            marqueeLabel.Font = new Font("Segoe UI", size, FontStyle.Bold);
                        }
                        catch { /* ignore font creation errors */ }
                        EnsureMarqueeBehindDockPanel();
                        this.Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }
            };

            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate { updateUi(); });
            else
                updateUi();
        }

        //private void BtnAnimTimer_Tick(object sender, EventArgs e)
        //{
        //    if (btnAnimStep < 3)
        //    {
        //        btnAnimScale += 0.001f;
        //    }
        //    else if (btnAnimStep < 7)
        //    {
        //        btnAnimScale -= 0.02f;
        //    }
        //    else
        //    {
        //        btnAnimScale = 1.0f;
        //        isBtnAnimating = false;
        //        btnAnimTimer.Stop();
        //    }
        //    btnAnimStep++;
        //    this.Invalidate();
        //}

        private async Task LoadWheelVouchersAsync()
        {
            try
            {
                // Thử resolve EditValue thành eventId:
                string selectedValue = gridLookUpEditEvent?.EditValue as string;
                string resolvedEventId = null;

                if (!string.IsNullOrWhiteSpace(selectedValue))
                {
                    // 1) Thử coi selectedValue là ProgramId và lấy program.LUCKY_DRAW_ID
                    try
                    {
                        var program = await LuckyDrawService.GetProgramByIdAsync(selectedValue);
                        if (program != null && !string.IsNullOrWhiteSpace(program.LUCKY_DRAW_ID))
                        {
                            resolvedEventId = program.LUCKY_DRAW_ID;
                        }
                    }
                    catch
                    {
                    }

                    // 2) Nếu chưa có eventId, thử coi selectedValue là eventId trực tiếp
                    if (string.IsNullOrWhiteSpace(resolvedEventId))
                    {
                        try
                        {
                            var evtCheck = await LuckyDrawService.GetEventByIdAsync(selectedValue);
                            if (evtCheck != null)
                                resolvedEventId = selectedValue;
                        }
                        catch { /* ignore */ }
                    }
                }

                // Nếu đã resolve được eventId thì load như bình thường
                if (!string.IsNullOrWhiteSpace(resolvedEventId))
                {
                    await LoadEventToWheelAsync(resolvedEventId);
                    return;
                }

                // Fallback thông minh: nếu có chương trình nào liên kết event thì chọn chương trình đầu có event
                try
                {
                    var programs = await LuckyDrawService.GetAllProgramsAsync(null);
                    var firstWithEvent = programs?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.LUCKY_DRAW_ID));
                    if (firstWithEvent != null)
                    {
                        await LoadEventToWheelAsync(firstWithEvent.LUCKY_DRAW_ID);
                        return;
                    }
                }
                catch { /* ignore */ }

                // Tiếp tục fallback: load events và lấy event đầu tiên
                try
                {
                    var events = await LuckyDrawService.GetAllEventsAsync();
                    var first = events?.FirstOrDefault();
                    if (first != null)
                    {
                        await LoadEventToWheelAsync(first.LUCKY_DRAW_ID);
                        return;
                    }
                }
                catch { /* ignore */ }

                // Nếu không tìm được gì, giữ nguyên numSegments hiện tại (mặc định hoặc giữ từ trước)
                UpdateNumSegments(numSegments);
            }
            catch
            {
            }
        }

        private bool isBannerEnabled = true;

        public void SetBannerEnabled(bool enabled)
        {
            isBannerEnabled = enabled;
            this.Invalidate();
        }
        private void DrawBanner(Graphics g)
        {
            // ⭐ THAM SỐ ĐIỀU CHỈNH DỄ DÀNG
            int bannerStartY = 5;          // Vị trí Y bắt đầu banner (18 → 10 = dịch lên 8px)
            int spacing1to2 = 5;            // Khoảng cách dòng 1-2 (2 → 5 = rộng thêm 3px)
            int spacing2to3 = 8;           // Khoảng cách dòng 2-3 (8 → 12 = rộng thêm 4px)
            int shadowOffset = 2;           // Độ lệch bóng đổ

            int bannerCenterX = this.Width / 2;

            var fontHeader = new Font("Segoe UI", 22, FontStyle.Bold);
            var colorHeader = Color.DarkGoldenrod;
            var fontBannerBtn = new Font("Segoe UI", 20, FontStyle.Bold);
            var colorBannerBtn = Color.DarkRed;

            // Đo kích thước các dòng
            var sizeHeader1 = g.MeasureString(bannerHeader1, fontHeader);
            var sizeHeader2 = g.MeasureString(bannerHeader2, fontHeader);
            var sizeHeader3 = g.MeasureString(bannerHeader3, fontBannerBtn);

            // Tính vị trí Y cho từng dòng
            int y1 = bannerStartY;
            int y2 = y1 + (int)sizeHeader1.Height + spacing1to2;
            int y3 = y2 + (int)sizeHeader2.Height + spacing2to3;

            // Vẽ bóng đổ
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(120, Color.Black)))
            {
                g.DrawString(bannerHeader1, fontHeader, shadowBrush,
                    bannerCenterX - sizeHeader1.Width / 2 + shadowOffset, y1 + shadowOffset);
                g.DrawString(bannerHeader2, fontHeader, shadowBrush,
                    bannerCenterX - sizeHeader2.Width / 2 + shadowOffset, y2 + shadowOffset);
                g.DrawString(bannerHeader3, fontBannerBtn, shadowBrush,
                    bannerCenterX - sizeHeader3.Width / 2 + shadowOffset, y3 + shadowOffset);
            }

            // Vẽ chữ chính
            using (SolidBrush headerBrush = new SolidBrush(colorHeader))
            {
                g.DrawString(bannerHeader1, fontHeader, headerBrush,
                    bannerCenterX - sizeHeader1.Width / 2, y1);
                g.DrawString(bannerHeader2, fontHeader, headerBrush,
                    bannerCenterX - sizeHeader2.Width / 2, y2);
            }

            using (SolidBrush btnBrush = new SolidBrush(colorBannerBtn))
            {
                g.DrawString(bannerHeader3, fontBannerBtn, btnBrush,
                    bannerCenterX - sizeHeader3.Width / 2, y3);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int bannerHeight = (int)(this.Height * 0.1);
            int extraWheelOffset = (int)(this.Height * 0.05);

            // Background (cached)
            if (wheelBackgroundImage != null)
            {
                if (cachedBackground == null || cachedBackground.Width != this.Width || cachedBackground.Height != this.Height)
                {
                    cachedBackground?.Dispose();
                    cachedBackground = new Bitmap(this.Width, this.Height);
                    using (Graphics g = Graphics.FromImage(cachedBackground))
                    {
                        g.DrawImage(wheelBackgroundImage, this.ClientRectangle);
                    }
                }
                e.Graphics.DrawImage(cachedBackground, 0, 0);
            }
            else
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.Black))
                {
                    e.Graphics.FillRectangle(bgBrush, this.ClientRectangle);
                }
            }

            // Banner
            DrawBanner(e.Graphics);

            // Smoothing + rendering hints (switch quality for spinning)
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (isSpinning)
            {
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Default;
            }
            else
            {
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            }

            // Wheel geometry
            int size = Math.Min(this.Width, this.Height - bannerHeight) - 200;
            if (size < 100) size = 100;
            int centerX = this.Width / 2;
            int centerY = bannerHeight + 60 + extraWheelOffset + size / 2;
            int radius = size / 2;

            // Wheel shadow + border
            DrawIconBorder(e.Graphics, centerX, centerY, radius);


            float anglePerSegment = 360f / numSegments;
            float startAngle = -90f + wheelRotation;

            // Precompute font base sizes (reuse cached fonts)
            float basePhoneSize = Math.Min(18f, Math.Max(8f, radius * 0.12f));
            // match product-name font logic used in mechanism 1:
            float textBaseSize = Math.Min(16f, Math.Max(8f, radius * 0.08f));
            float nameFontSize = Math.Max(7f, textBaseSize * 0.7f);

            // Reuse phoneBrush only (remove shadow brushes to reduce allocations)
            using (var phoneBrush = new SolidBrush(Color.DarkRed))
            {
                for (int i = 0; i < numSegments; i++)
                {
                    float segStart = startAngle + i * anglePerSegment;

                    // 1. Segment fill
                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        path.AddPie(centerX - radius, centerY - radius, size, size, segStart, anglePerSegment);

                        // ✅ THAY ĐỔI: Dùng Color làm key
                        Color segmentColor = segmentColors[i % segmentColors.Count];

                        if (!_segmentBrushCache.ContainsKey(segmentColor))
                        {
                            _segmentBrushCache[segmentColor] = new System.Drawing.Drawing2D.LinearGradientBrush(
                                new Rectangle(centerX - radius, centerY - radius, size, size),
                                ControlPaint.Light(segmentColor, 0.2f),
                                ControlPaint.Dark(segmentColor, 0.2f),
                                System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);
                        }

                        e.Graphics.FillPath(_segmentBrushCache[segmentColor], path);
                    }

                    //2. Segment border line
                    using (Pen pen = new Pen(Color.Gold, 5))
                    {
                        e.Graphics.DrawPie(pen, centerX - radius, centerY - radius, size, size, segStart, anglePerSegment);
                    }

                    //3. Text anchor
                    float textAngle = segStart + anglePerSegment / 2;
                    double rad = textAngle * Math.PI / 180;
                    int textRadius = (int)(radius * 0.55);
                    int textX = centerX + (int)(textRadius * Math.Cos(rad));
                    int textY = centerY + (int)(textRadius * Math.Sin(rad));


                    //4. ==== VẼ DECORATION ICONS THEO STYLE (THÊM SAU ĐOẠN VẼ SEGMENT IMAGE) ====
                    // Kiểm tra xem style hiện tại có decoration icons không
                    if (spinMode != "2" &&
                        decorationIconsCache.ContainsKey(currentStyle) &&
                        decorationIconsCache[currentStyle].Count > 0)
                    {
                        var decoIcons = decorationIconsCache[currentStyle];

                        // Chọn icon theo index của segment (xoay vòng nếu thiếu)
                        int decoIndex = i % decoIcons.Count;
                        var decoImg = decoIcons[decoIndex];

                        // Nếu icon tồn tại thì vẽ
                        if (decoImg != null)
                        {
                            // ⭐ 1. TĂNG KÍCH THƯỚC ICON: 
                            int decoSize = (int)(radius * 0.22);

                            // ⭐ 2. DỜI VỊ TRÍ ICON: giảm xuống  (gần tâm hơn) ra xa tâm thì tăng lên
                            int decoRadius = (int)(radius * 0.50); // Gần tâm

                            // Tính toán vị trí X, Y dựa trên góc textAngle
                            double radDeco = textAngle * Math.PI / 180;
                            int decoX = centerX + (int)(decoRadius * Math.Cos(radDeco)) - decoSize / 2;
                            int decoY = centerY + (int)(decoRadius * Math.Sin(radDeco)) - decoSize / 2;

                            var decoRect = new Rectangle(decoX, decoY, decoSize, decoSize);

                            // Xoay icon theo góc segment (để icon luôn thẳng đứng hoặc xoay theo vòng)
                            var stateDeco = e.Graphics.Save();
                            e.Graphics.TranslateTransform(decoX + decoSize / 2, decoY + decoSize / 2);
                            e.Graphics.RotateTransform(textAngle); // Xoay theo góc segment
                            e.Graphics.TranslateTransform(-(decoX + decoSize / 2), -(decoY + decoSize / 2));

                            // ⭐ 3. VẼ ICON VỚI ĐỘ MỜ: Alpha = 0.4f (40% opacity)
                            using (var imageAttributes = new ImageAttributes())
                            {
                                ColorMatrix colorMatrix = new ColorMatrix();
                                colorMatrix.Matrix33 = 0.25f; // (điều chỉnh từ 0.0-1.0)
                                imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                                e.Graphics.DrawImage(
                                    decoImg,
                                    decoRect,
                                    0, 0, decoImg.Width, decoImg.Height,
                                    GraphicsUnit.Pixel,
                                    imageAttributes
                                );
                            }

                            // Khôi phục Graphics state
                            e.Graphics.Restore(stateDeco);
                        }
                    }

                    //5. Mechanism 2: phone + name rotated with segment (no auto-flip)
                    if (spinMode == "2" && phoneNumberList != null && phoneNumberList.Count > 0)
                    {
                        int idx = i % phoneNumberList.Count;
                        var entry = phoneNumberList[idx];
                        string phone = entry.PhoneNumber ?? string.Empty;
                        string name = entry.CustomerName ?? string.Empty;

                        // Use same font family/size logic as product name in mechanism 1
                        var phoneFont = GetCachedFont(textBaseSize, FontStyle.Bold);
                        var nameFont = GetCachedFont(nameFontSize, FontStyle.Regular);

                        // Fit width by computing scale once (avoid loops/new allocations)
                        float maxWidth = Math.Max(24f, radius * 0.9f);
                        var phoneSize = e.Graphics.MeasureString(phone, phoneFont);
                        if (phoneSize.Width > maxWidth && phoneSize.Width > 0)
                        {
                            float scale = maxWidth / phoneSize.Width;
                            float newPhoneSize = Math.Max(6f, textBaseSize * scale);
                            phoneFont = GetCachedFont(newPhoneSize, FontStyle.Bold);
                            phoneSize = e.Graphics.MeasureString(phone, phoneFont);
                        }

                        var nameSize = string.IsNullOrEmpty(name) ? new SizeF(0, 0) : e.Graphics.MeasureString(name, nameFont);
                        if (nameSize.Width > maxWidth && nameSize.Width > 0)
                        {
                            float scale = maxWidth / nameSize.Width;
                            float newNameSize = Math.Max(6f, nameFontSize * scale);
                            nameFont = GetCachedFont(newNameSize, FontStyle.Regular);
                            nameSize = e.Graphics.MeasureString(name, nameFont);
                        }

                        float totalHeight = phoneSize.Height + (string.IsNullOrEmpty(name) ? 0f : nameSize.Height);

                        // Use AntiAlias for rotated text rendering, but do not draw any shadow to reduce cost
                        var prevTextHint = e.Graphics.TextRenderingHint;
                        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                        var state = e.Graphics.Save();
                        e.Graphics.TranslateTransform(textX, textY);
                        e.Graphics.RotateTransform(textAngle);

                        // Draw phone (match mechanism 1 product name font & color), no shadow
                        e.Graphics.DrawString(phone, phoneFont, phoneBrush, -phoneSize.Width / 2f, -totalHeight / 2f);

                        // Draw name (regular, no shadow). use system brush Brushes.Black to avoid allocations
                        if (!string.IsNullOrEmpty(name))
                        {
                            e.Graphics.DrawString(name, nameFont, Brushes.Black, -nameSize.Width / 2f, -totalHeight / 2f + phoneSize.Height);
                        }

                        e.Graphics.Restore(state);
                        e.Graphics.TextRenderingHint = prevTextHint;
                    }
                    else
                    {
                        // Fallback: single-line segment text (non-rotated)
                        string text = segmentTexts.Count > 0 ? segmentTexts[i % segmentTexts.Count] : "Trống";
                        float textBase = Math.Min(16f, Math.Max(8f, radius * 0.08f));
                        var textFont = GetCachedFont(textBase, FontStyle.Bold);
                        var textSize = e.Graphics.MeasureString(text, textFont);

                        if (textSize.Width > radius * 0.85f && textSize.Width > 0)
                        {
                            float scale = (radius * 0.85f) / textSize.Width;
                            float newSize = Math.Max(6f, textBase * scale);
                            textFont = GetCachedFont(newSize, FontStyle.Bold);
                            textSize = e.Graphics.MeasureString(text, textFont);
                        }

                        using (var prizeBrush = new SolidBrush(Color.WhiteSmoke))
                        {
                            e.Graphics.DrawString(text, textFont, prizeBrush, textX - textSize.Width / 2, textY - textSize.Height / 2);
                        }
                    }

                    // Draw segment image if any (rotate image nicely)
                    Image img = segmentImageCache.Count > i ? segmentImageCache[i] : null;
                    if (img != null)
                    {
                        int imgSize = (int)(radius * 0.19);
                        int imgRadius = (int)(radius * 0.83);
                        double radImg = textAngle * Math.PI / 180;
                        int imgX = centerX + (int)(imgRadius * Math.Cos(radImg)) - imgSize / 2;
                        int imgY = centerY + (int)(imgRadius * Math.Sin(radImg)) - imgSize / 2;
                        var imgRect = new Rectangle(imgX, imgY, imgSize, imgSize);

                        var stateImg = e.Graphics.Save();
                        e.Graphics.TranslateTransform(imgX + imgSize / 2, imgY + imgSize / 2);
                        e.Graphics.RotateTransform(textAngle + 90f);
                        e.Graphics.TranslateTransform(-(imgX + imgSize / 2), -(imgY + imgSize / 2));

                        using (Pen border = new Pen(Color.White, 4))
                        {
                            e.Graphics.DrawEllipse(border, imgRect);
                        }
                        using (SolidBrush sh = new SolidBrush(Color.FromArgb(80, Color.Black)))
                        {
                            e.Graphics.FillEllipse(sh, imgRect.X + 3, imgRect.Y + 3, imgSize - 6, imgSize - 6);
                        }
                        e.Graphics.DrawImage(img, imgRect);
                        e.Graphics.Restore(stateImg);
                    }
                }
            }

            // ⭐ QUAY BUTTON - MÀU NỀN + HÌNH ẢNH OVERLAY
            int btnSize = (int)(radius * 0.3);
            if (btnSize < 45) btnSize = 45;
            int scaledBtnSize = btnSize;
            btnRect = new Rectangle(centerX - scaledBtnSize / 2, centerY - scaledBtnSize / 2, scaledBtnSize, scaledBtnSize);

            // ⭐ 1. VẼ NỀN GRADIENT (LUÔN VẼ - theo theme)
            var (centerColor, surroundColor, borderColor, textColor) = GetButtonStyle();

            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddEllipse(btnRect);
                using (var brush = new System.Drawing.Drawing2D.PathGradientBrush(path))
                {
                    brush.CenterColor = centerColor;
                    brush.SurroundColors = new[] { surroundColor };
                    e.Graphics.FillEllipse(brush, btnRect);
                }
            }

            // ⭐ 2. VẼ GLOW (hiệu ứng phát sáng)
            using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(60, centerColor)))
            {
                e.Graphics.FillEllipse(glowBrush, btnRect.X - 8, btnRect.Y - 8, btnSize + 16, btnSize + 16);
            }

            // ⭐ 3. VẼ HÌNH ẢNH OVERLAY (nếu có - đè lên màu nền)
            Image btnImage = null;
            if (spinButtonImages.ContainsKey(currentStyle))
            {
                btnImage = spinButtonImages[currentStyle];
            }

            if (btnImage != null)
            {
                // Tạo vùng clip tròn để ảnh không tràn ra ngoài
                var state = e.Graphics.Save();

                using (var clipPath = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    // Thu nhỏ vùng ảnh 1 chút để không che hết viền
                    int imgSize = (int)(scaledBtnSize * 0.5); // 
                    int imgX = centerX - imgSize / 2;
                    int imgY = centerY - imgSize / 2;
                    var imgRect = new Rectangle(imgX, imgY, imgSize, imgSize);

                    clipPath.AddEllipse(imgRect);
                    e.Graphics.SetClip(clipPath);

                    // Vẽ ảnh với độ trong suốt nhẹ để nhìn thấy màu nền
                    using (var imageAttr = new ImageAttributes())
                    {
                        ColorMatrix colorMatrix = new ColorMatrix();
                        colorMatrix.Matrix33 = 0.7f; // 70% opacity (cho phép nhìn thấy màu nền)
                        imageAttr.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                        e.Graphics.DrawImage(btnImage, imgRect,
                            0, 0, btnImage.Width, btnImage.Height,
                            GraphicsUnit.Pixel, imageAttr);
                    }

                    e.Graphics.ResetClip();
                }

                e.Graphics.Restore(state);
            }

            // ⭐ 4. VẼ VIỀN (luôn vẽ)
            using (Pen goldPen2 = new Pen(borderColor, 8))
            {
                e.Graphics.DrawEllipse(goldPen2, btnRect);
            }

            // ⭐ 5. VẼ TEXT "QUAY" (luôn hiển thị)
            string btnText = "QUAY";
            float btnFontSize = Math.Min(scaledBtnSize * 0.4f, 20f);
            if (btnFontSize < 10f) btnFontSize = 10f;
            var btnTextFont = GetCachedFont(btnFontSize, FontStyle.Bold);
            var btnTextSize = e.Graphics.MeasureString(btnText, btnTextFont);

            // Text shadow (bóng đổ)
            using (SolidBrush textShadowBrush = new SolidBrush(Color.FromArgb(120, Color.Black)))
            {
                e.Graphics.DrawString(btnText, btnTextFont, textShadowBrush,
                    centerX - btnTextSize.Width / 2 + 2, centerY - btnTextSize.Height / 2 + 2);
            }

            // Text chính (màu tùy theo có ảnh hay không)
            Color finalTextColor = btnImage != null ? Color.White : textColor;

            using (SolidBrush textBrush2 = new SolidBrush(finalTextColor))
            {
                e.Graphics.DrawString(btnText, btnTextFont, textBrush2,
                    centerX - btnTextSize.Width / 2, centerY - btnTextSize.Height / 2);
            }

            // Arrow
            int arrowHeight = (int)(radius * 0.1);
            int arrowWidth = (int)(radius * 0.15);
            int arrowCenterX = centerX;
            int arrowTipY = btnRect.Top - arrowHeight - 2;
            int arrowBaseY = btnRect.Top - 2;
            Point[] arrowPoints = {
            new Point(arrowCenterX, arrowTipY),
            new Point(arrowCenterX - arrowWidth / 2, arrowBaseY),
            new Point(arrowCenterX + arrowWidth / 2, arrowBaseY)
        };
            using (var arrowBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(arrowCenterX - arrowWidth / 2, arrowTipY, arrowWidth, arrowHeight),
                Color.Gold, Color.Orange, System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                e.Graphics.FillPolygon(arrowBrush, arrowPoints);
            }
            using (var arrowPen = new Pen(Color.DarkRed, 2))
            {
                e.Graphics.DrawPolygon(arrowPen, arrowPoints);
            }

            // Fireworks
            //if (showFirework)
            //{
            //    foreach (var p in fireworks)
            //    {
            //        using (var b = new SolidBrush(p.Color))
            //        {
            //            e.Graphics.FillEllipse(b, p.Position.X - 6, p.Position.Y - 6, 12, 12);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Lấy icon mặc định cho viền theo style
        /// </summary>
        private Image GetDefaultBorderIcon(WheelStyle style)
        {
            try
            {
                string assetFolder = Path.Combine(Application.StartupPath, "Assets", "img");
                string iconPath = null;

                // ⭐ CHỈ ĐỊNH ICON MỶC ĐỊNH CHO TỪNG STYLE
                switch (style)
                {
                    case WheelStyle.TetNguyenDan:
                        iconPath = Path.Combine(assetFolder, "flower.png"); // Icon hoa mai
                        break;

                    case WheelStyle.GiangSinh:
                        iconPath = Path.Combine(assetFolder, "santa-claus.png"); // Icon ông già Noel
                        break;

                    case WheelStyle.Halloween:
                        iconPath = Path.Combine(assetFolder, "pumpkin.png"); // Icon bí ngô
                        break;

                    case WheelStyle.TrungThu:
                        iconPath = Path.Combine(assetFolder, "lantern.png"); // Icon lồng đèn
                        break;

                    case WheelStyle.ModernNeon:
                        iconPath = Path.Combine(assetFolder, "star.png"); // Icon kim cương
                        break;

                    case WheelStyle.Default:
                    default:
                        iconPath = Path.Combine(assetFolder, "star.png"); // Icon ngôi sao mặc định
                        break;
                }

                // Load icon nếu file tồn tại
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    var bytes = File.ReadAllBytes(iconPath);
                    using (var ms = new MemoryStream(bytes))
                    {
                        var img = Image.FromStream(ms);
                        return new Bitmap(img);
                    }
                }

                return null; // Không có icon -> dùng viền màu
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Vẽ viền bánh xe bằng các icon sắp xếp theo vòng tròn (6-10 icons cố định)
        /// </summary>
        private void DrawIconBorder(Graphics g, int centerX, int centerY, int radius)
        {
            // ⭐ LẤY ICON MỶC ĐỊNH THEO STYLE (thay vì random từ cache)
            Image defaultIcon = GetDefaultBorderIcon(currentStyle);

            if (defaultIcon == null)
            {
                // Nếu không có icon mặc định, vẽ viền màu
                DrawColorBorder(g, centerX, centerY, radius);
                return;
            }

            // Vẽ bóng đổ trước
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(80, Color.Black)))
            {
                g.FillEllipse(shadowBrush, centerX - radius - 12, centerY - radius + 12, (radius * 2) + 24, (radius * 2) + 24);
            }

            // ⭐ THAM SỐ ĐIỀU CHỈNH
            int iconSize = (int)(radius * 0.10); // Kích thước icon 
            if (iconSize < 30) iconSize = 30;    // Tối thiểu 
            if (iconSize > 70) iconSize = 60;    // Tối đa 70px

            // ⭐ CỐ ĐỊNH SỐ LƯỢNG ICON: 6-10 icons
            int iconCount = 8; // Mặc định 8 icons (có thể đổi thành 6, 7, 9, 10)

            // Góc giữa mỗi icon    
            float angleStep = 360f / iconCount;

            // Vẽ từng icon theo vòng tròn (TẤT CẢ DÙNG CÙNG 1 ICON MỶC ĐỊNH)
            for (int i = 0; i < iconCount; i++)
            {
                // Tính góc hiện tại (tính theo radian)
                float angle = i * angleStep;
                double rad = angle * Math.PI / 180.0;

                // Tính vị trí icon
                int iconRadius = radius + 26; // Đặt icon ngoài viền một chút
                int iconX = centerX + (int)(iconRadius * Math.Cos(rad)) - iconSize / 2;
                int iconY = centerY + (int)(iconRadius * Math.Sin(rad)) - iconSize / 2;

                // Lưu trạng thái Graphics
                var state = g.Save();

                // Di chuyển gốc tọa độ đến tâm icon
                g.TranslateTransform(iconX + iconSize / 2, iconY + iconSize / 2);

                // Xoay icon theo góc (để hướng ra ngoài)
                g.RotateTransform(angle + 90);

                // ⭐ VẼ ICON MỶC ĐỊNH (cùng 1 icon cho tất cả vị trí)
                g.DrawImage(defaultIcon, -iconSize / 2, -iconSize / 2, iconSize, iconSize);

                // Khôi phục trạng thái Graphics
                g.Restore(state);
            }

            // Vẽ viền tròn mỏng để làm nền (tùy chọn)
            using (Pen basePen = new Pen(Color.FromArgb(150, Color.Gold), 4))
            {
                g.DrawEllipse(basePen, centerX - radius - 2, centerY - radius - 2, (radius * 2) + 4, (radius * 2) + 4);
            }

            // ⭐ DISPOSE ICON SAU KHI VẼ XONG
            try { defaultIcon?.Dispose(); }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Vẽ viền màu mặc định (fallback khi không có icon)
        /// </summary>
        private void DrawColorBorder(Graphics g, int centerX, int centerY, int radius)
        {
            // Chọn màu viền theo style
            Color borderColor1, borderColor2, borderColor3;

            switch (currentStyle)
            {
                case WheelStyle.Default:
                    borderColor1 = Color.Gold;
                    borderColor2 = Color.DarkGoldenrod;
                    borderColor3 = Color.Orange;
                    break;

                case WheelStyle.TetNguyenDan:
                    borderColor1 = Color.FromArgb(255, 215, 0);
                    borderColor2 = Color.FromArgb(220, 20, 60);
                    borderColor3 = Color.FromArgb(255, 165, 0);
                    break;

                case WheelStyle.GiangSinh:
                    borderColor1 = Color.FromArgb(220, 20, 60);
                    borderColor2 = Color.FromArgb(34, 139, 34);
                    borderColor3 = Color.FromArgb(255, 255, 255);
                    break;

                case WheelStyle.Halloween:
                    borderColor1 = Color.FromArgb(255, 140, 0);
                    borderColor2 = Color.FromArgb(75, 0, 130);
                    borderColor3 = Color.FromArgb(128, 0, 128);
                    break;

                case WheelStyle.TrungThu:
                    borderColor1 = Color.FromArgb(255, 215, 0);
                    borderColor2 = Color.FromArgb(255, 69, 0);
                    borderColor3 = Color.FromArgb(255, 255, 0);
                    break;

                case WheelStyle.ModernNeon:
                    borderColor1 = Color.FromArgb(0, 255, 255);
                    borderColor2 = Color.FromArgb(255, 0, 255);
                    borderColor3 = Color.FromArgb(57, 255, 20);
                    break;

                default:
                    borderColor1 = Color.Gold;
                    borderColor2 = Color.DarkGoldenrod;
                    borderColor3 = Color.Orange;
                    break;
            }

            int size = radius * 2;

            // Bóng đổ
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(80, Color.Black)))
            {
                g.FillEllipse(shadowBrush, centerX - radius - 12, centerY - radius + 12, size + 24, size + 24);
            }

            // Viền ngoài
            using (Pen outerPen = new Pen(borderColor1, 16))
            {
                g.DrawEllipse(outerPen, centerX - radius - 8, centerY - radius - 8, size + 16, size + 16);
            }

            // Viền giữa
            using (Pen middlePen = new Pen(borderColor2, 12))
            {
                g.DrawEllipse(middlePen, centerX - radius - 4, centerY - radius - 4, size + 8, size + 8);
            }

            // Viền trong
            using (Pen innerPen = new Pen(borderColor3, 8))
            {
                g.DrawEllipse(innerPen, centerX - radius, centerY - radius, size, size);
            }

            // Hiệu ứng phát sáng
            using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(50, borderColor1)))
            {
                g.FillEllipse(glowBrush, centerX - radius - 10, centerY - radius - 10, size + 20, size + 20);
            }
        }
        /// <summary>
        /// Lấy tất cả style cho button QUAY (center, surround, border, text)
        /// </summary>
        private (Color center, Color surround, Color border, Color text) GetButtonStyle()
        {
            var styles = new Dictionary<WheelStyle, (Color, Color, Color, Color)>
            {
                [WheelStyle.Default] = (Color.Gold, Color.DarkRed, Color.Gold, Color.Red),
                [WheelStyle.TetNguyenDan] = (Color.Gold, Color.Red, Color.FromArgb(255, 215, 0), Color.Red),
                [WheelStyle.GiangSinh] = (Color.White, Color.Green, Color.Red, Color.DarkRed),
                [WheelStyle.Halloween] = (Color.Orange, Color.DarkSlateGray, Color.Purple, Color.Red),
                [WheelStyle.TrungThu] = (Color.Yellow, Color.OrangeRed, Color.Gold, Color.Red),
                [WheelStyle.ModernNeon] = (Color.Cyan, Color.Magenta, Color.LimeGreen, Color.Yellow)
            };

            return styles.ContainsKey(currentStyle)
                ? styles[currentStyle]
                : styles[WheelStyle.Default];
        }

        /// <summary>
        /// Lấy màu text nút QUAY theo style
        /// </summary>
        private Color GetButtonTextColor()
        {
            switch (currentStyle)
            {
                case WheelStyle.GiangSinh:
                    return Color.DarkRed; // Text đỏ đậm trên nền trắng

                case WheelStyle.ModernNeon:
                    return Color.Yellow; // Text vàng neon

                case WheelStyle.Default:
                case WheelStyle.TetNguyenDan:
                case WheelStyle.Halloween:
                case WheelStyle.TrungThu:
                default:
                    return Color.Red; // Text đỏ
            }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            // Chặn bấm quay liên tiếp:
            // - không cho bấm khi đang quay (isSpinning)
            // - không cho bấm khi animation nút đang chạy (isBtnAnimating)
            // - không cho bấm khi pháo hoa đang hiển thị hoặc timer lặp pháo hoa còn active
            //bool fireworkActive = showFirework || (fireworkRepeatTimer != null && fireworkRepeatTimer.Enabled);

            //if (btnRect.Contains(e.Location) && !isSpinning && !isBtnAnimating && !fireworkActive)
            //{
            //    this.Focus();

            //    isBtnAnimating = true;
            //    btnAnimScale = 1.0f;
            //    btnAnimStep = 0;
            //    btnAnimTimer.Start();

            //    StartSpin();
            //}
            if (btnRect.Contains(e.Location) && !isSpinning)
            {
                this.Focus();
                StartSpin();
            }

        }



        //private async Task<bool> ValidatePrizesBeforeSpinAsync(string eventId)
        //{
        //    // Nếu không có event rõ ràng thì không chặn (giữ hành vi cũ)
        //    if (string.IsNullOrWhiteSpace(eventId)) return true;

        //    try
        //    {
        //        var meisais = await LuckyDrawService.GetAllMeisaiAsync(eventId, category: null, includeMuko: true);
        //        if (meisais == null || meisais.Count == 0) return true;

        //        var problems = new List<string>();
        //        foreach (var m in meisais)
        //        {
        //            var reasons = new List<string>();

        //            if (m.LUCKY_DRAW_MEISAI_MUKO_FLG.HasValue && m.LUCKY_DRAW_MEISAI_MUKO_FLG.Value == 1)
        //                reasons.Add("MUKO");

        //            try
        //            {
        //                if (m.SYURYOU_DATE.HasValue && m.SYURYOU_DATE.Value.Date < DateTime.Now.Date)
        //                    reasons.Add("Hết hạn");
        //            }
        //            catch { /* ignore */ }

        //            try
        //            {
        //                if (m.LUCKY_DRAW_MEISAI_SURYO.HasValue && m.LUCKY_DRAW_MEISAI_SURYO.Value <= 0.0)
        //                    reasons.Add("Số lượng = 0");
        //            }
        //            catch { /* ignore */ }

        //            if (reasons.Count > 0)
        //            {
        //                string id = m.LUCKY_DRAW_MEISAI_NO ?? "(no-id)";
        //                string name = !string.IsNullOrWhiteSpace(m.LUCKY_DRAW_MEISAI_NAME) ? m.LUCKY_DRAW_MEISAI_NAME : (m.LUCKY_DRAW_MEISAI_NAIYOU ?? "");
        //                problems.Add($"{id} - {name} => {string.Join(", ", reasons)}");
        //            }
        //        }

        //        if (problems.Count > 0)
        //        {
        //            int maxShow = 50;
        //            var shown = problems.Take(maxShow).ToList();
        //            string msg = $"Phát hiện {problems.Count} phần thưởng có trạng thái không hợp lệ (MUKO / Hết hạn / Số lượng = 0). Vui lòng sửa trước khi quay:\n\n" +
        //                         string.Join("\n", shown);
        //            if (problems.Count > maxShow)
        //                msg += $"\n\n... và {problems.Count - maxShow} mục khác.";

        //            // Hiển thị trên UI thread
        //            if (this.IsHandleCreated && this.InvokeRequired)
        //            {
        //                this.BeginInvoke((MethodInvoker)(() => MessageBox.Show(this, msg, "Cảnh báo phần thưởng", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
        //            }
        //            else
        //            {
        //                MessageBox.Show(this, msg, "Cảnh báo phần thưởng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //            }

        //            return false;
        //        }

        //        return true;
        //    }
        //    catch
        //    {
        //        // Nếu không thể kiểm tra (lỗi BE), không chặn quay để tránh gây nghẽn; log để debug.
        //        return true;
        //    }
        //}

        private async void StartSpin()
        {
            if (spinMode == "1")
            {
                string phone = txtPhone?.Text?.Trim();
                string customerName = txtCustomerName?.Text?.Trim();
                string invoiceNumber = txtInvoiceNumber?.Text?.Trim();

                if (phone == "Nhập tên khách hàng..." || phone == "Nhập số điện thoại...") phone = string.Empty;
                if (customerName == "Nhập tên khách hàng...") customerName = string.Empty;
                if (invoiceNumber == "Nhập số hoá đơn...") invoiceNumber = string.Empty;

                if (string.IsNullOrEmpty(phone) ||
                    string.IsNullOrEmpty(customerName) ||
                    string.IsNullOrEmpty(invoiceNumber))
                {
                    //Debug.WriteLine("StartSpin: validation failed - missing input");
                    XtraMessageBox.Show(this, "Vui lòng nhập đầy đủ Số điện thoại, Tên khách hàng và Số hoá đơn trước khi quay!",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Disable inputs
                txtPhone.Enabled = false;
                txtCustomerName.Enabled = false;
                txtInvoiceNumber.Enabled = false;

                // Pre-spin animation
                isSpinning = true;

                // record start time (will be overwritten shortly with real start time)
                spinStartTime = DateTime.Now;

                // capture BEFORE spin (whole app window)
                try
                {
                    spinBeforeImageBase64 = CaptureAppScreenshotAsBase64();
                    //Debug.WriteLine($"StartSpin: captured before image length={(spinBeforeImageBase64 != null ? spinBeforeImageBase64.Length : 0)}");
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine("StartSpin: capture before failed: " + ex.Message);
                    spinBeforeImageBase64 = null;
                }

                try
                {
                    var selectedProgramId = gridLookUpEditEvent?.EditValue as string;
                    if (string.IsNullOrWhiteSpace(selectedProgramId))
                    {
                        XtraMessageBox.Show(this, "Vui lòng chọn chương trình trước khi quay!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        isSpinning = false;
                        txtPhone.Enabled = true;
                        txtCustomerName.Enabled = true;
                        txtInvoiceNumber.Enabled = true;
                        return;
                    }

                    var program = await LuckyDrawService.GetProgramByIdAsync(selectedProgramId);
                    if (program == null || string.IsNullOrWhiteSpace(program.LUCKY_DRAW_ID))
                    {
                        XtraMessageBox.Show(this, "Chương trình không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        isSpinning = false;
                        txtPhone.Enabled = true;
                        txtCustomerName.Enabled = true;
                        txtInvoiceNumber.Enabled = true;
                        return;
                    }

                    var selectedEventId = program.LUCKY_DRAW_ID;

                    var (meisai, rv, rateLog) = await LuckyDrawService.SpinMeisaiAsync(selectedEventId, null, includeMuko: false);
                    //Debug.WriteLine("StartSpin: SpinMeisaiAsync returned");

                    if (meisai == null)
                    {
                        //Debug.WriteLine("StartSpin: meisai == null -> stopping");
                        isSpinning = false;
                        XtraMessageBox.Show(this, "Không có phần thưởng khả dụng!", "Kết quả vòng quay", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Re-enable inputs
                        txtPhone.Enabled = true;
                        txtCustomerName.Enabled = true;
                        txtInvoiceNumber.Enabled = true;
                        return;
                    }

                    lastRandomValue = rv;

                    finalWinnerIndex = segmentCodes.FindIndex(c => c == (meisai.LUCKY_DRAW_MEISAI_NO ?? string.Empty));
                    if (finalWinnerIndex < 0) finalWinnerIndex = 0;

                    float angle = 360f / numSegments;
                    float current = (wheelRotation % 360f + 360f) % 360f;
                    float desired = (360f - angle * (finalWinnerIndex + 0.5f)) % 360f;

                    // compute extra spins using existing heuristic but it's only for distance
                    int extraSpins = Math.Max(3, spinDurationSeconds / 2);
                    float delta = ((desired - current + 360f) % 360f) + extraSpins * 360f;

                    // Time-based: record startRotation and total delta, start timer
                    spinStartRotation = wheelRotation;
                    spinTotalDelta = delta;
                    spinStartTime = DateTime.Now;
                    spinDurationMs = Math.Max(5000, spinDurationSeconds * 1000); // đảm bảo tối thiểu 5s
                    if (!spinTimer.Enabled) spinTimer.Start();
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine("StartSpin: Exception when calling SpinMeisaiAsync: " + ex);
                    isSpinning = false;
                    if (spinTimer.Enabled) spinTimer.Stop();

                    XtraMessageBox.Show(this, "Lỗi khi gọi API quay thưởng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    txtPhone.Enabled = true;
                    txtCustomerName.Enabled = true;
                    txtInvoiceNumber.Enabled = true;
                }
            }
            else
            {
                // Mechanism 2: time-based spin as well
                try
                {
                    if (phoneNumberList.Count == 0)
                    {
                        XtraMessageBox.Show(this, "Danh sách số điện thoại trống! Vui lòng thêm số điện thoại trước khi quay.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (lstPrizes.SelectedItem == null)
                    {
                        XtraMessageBox.Show(this, "Vui lòng chọn một phần thưởng trước khi quay!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var selectedPrizeObj = lstPrizes.SelectedItem;
                    var selectedMeisai = selectedPrizeObj as LuckyDrawMeisaiDTO;
                    var selectedKokaku = selectedPrizeObj as LuckyDrawKokakuDTO;

                    if (selectedMeisai == null && selectedKokaku == null)
                    {
                        XtraMessageBox.Show(this, "Phần thưởng được chọn không hợp lệ! Vui lòng kiểm tra lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if ((selectedMeisai != null && selectedMeisai.LUCKY_DRAW_MEISAI_MUKO_FLG == 1) ||
                        (selectedKokaku != null && selectedKokaku.KOKAKU_MUKO_FLG == 1))
                    {
                        XtraMessageBox.Show(this, "Phần thưởng này đã được sử dụng, vui lòng chọn phần thưởng khác!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var availablePhones = phoneNumberList.Where(p => !highlightedPhoneNumbers.Contains(p.PhoneNumber)).ToList();
                    if (availablePhones.Count == 0)
                    {
                        XtraMessageBox.Show(this, "Tất cả số điện thoại đã trúng thưởng, không còn số nào để quay!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    isSpinning = true;
                    spinStartTime = DateTime.Now;

                    try { spinBeforeImageBase64 = CaptureAppScreenshotAsBase64(); } catch { spinBeforeImageBase64 = null; }

                    if (!spinTimer.Enabled) spinTimer.Start();

                    int winnerIndex = rnd.Next(availablePhones.Count);
                    var winner = availablePhones[winnerIndex];
                    var winnerPhone = winner.PhoneNumber;

                    finalWinnerIndex = -1;
                    if (segmentCodes != null && segmentCodes.Count > 0)
                    {
                        finalWinnerIndex = segmentCodes.FindIndex(c => string.Equals(c, winnerPhone, StringComparison.OrdinalIgnoreCase));
                    }
                    if (finalWinnerIndex < 0)
                    {
                        finalWinnerIndex = phoneNumberList.FindIndex(p => string.Equals(p.PhoneNumber, winnerPhone, StringComparison.OrdinalIgnoreCase));
                    }

                    if (finalWinnerIndex < 0)
                    {
                        spinTimer.Stop();
                        isSpinning = false;
                        XtraMessageBox.Show(this, $"Không thể xác định người trúng thưởng với số điện thoại: {winnerPhone}. Vui lòng thử lại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    float anglePerSegment = 360f / numSegments;
                    float currentRotation = (wheelRotation % 360f + 360f) % 360f;
                    float targetRotationAngle = (360f - anglePerSegment * (finalWinnerIndex + 0.5f)) % 360f;
                    int extraSpins2 = 4;
                    float rotationDelta = ((targetRotationAngle - currentRotation + 360f) % 360f) + extraSpins2 * 360f;

                    // Time-based
                    spinStartRotation = wheelRotation;
                    spinTotalDelta = rotationDelta;
                    spinStartTime = DateTime.Now;
                    spinDurationMs = Math.Max(5000, spinDurationSeconds * 1000); // đảm bảo tối thiểu 5s
                    // leave UI/inputs handling to end-of-spin logic in SpinTimer_Tick
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine("StartSpin (2) Exception: " + ex);
                    isSpinning = false;
                    if (spinTimer.Enabled) spinTimer.Stop();
                    XtraMessageBox.Show(this, "Lỗi khi thực hiện quay: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void SpinTimer_Tick(object sender, EventArgs e)
        {
            if (!isSpinning) return;
            if (!spinStartTime.HasValue) spinStartTime = DateTime.Now;

            try
            {
                var now = DateTime.Now;
                double elapsedMs = (now - spinStartTime.Value).TotalMilliseconds;
                double totalMs = Math.Max(1, spinDurationMs);
                double t = Math.Min(1.0, elapsedMs / totalMs);

                // ease-out cubic (smooth slow-down near end). Bạn có thể đổi hàm easing nếu muốn.
                double eased = 1.0 - Math.Pow(1.0 - t, 3);

                float newRotation = spinStartRotation + spinTotalDelta * (float)eased;
                // ensure monotonic increase
                if (float.IsNaN(newRotation)) newRotation = wheelRotation;

                wheelRotation = newRotation % 360f;
                this.Invalidate();

                if (t >= 1.0)
                {
                    // finished
                    spinTimer.Stop();
                    isSpinning = false;
                    // Ensure final exact position (avoid rounding artefacts)
                    wheelRotation = (spinStartRotation + spinTotalDelta) % 360f;
                    this.Invalidate();

                    // --- Existing completion logic (copied & slightly adapted) ---
                    try
                    {
                        if (spinMode == "1")
                        {
                            var selectedProgramId = gridLookUpEditEvent?.EditValue as string;
                            if (string.IsNullOrWhiteSpace(selectedProgramId))
                            {
                                return;
                            }

                            var program = await LuckyDrawService.GetProgramByIdAsync(selectedProgramId);
                            var selectedEventId = program?.LUCKY_DRAW_ID;
                            if (string.IsNullOrWhiteSpace(selectedEventId))
                            {
                                return;
                            }

                            var (confirmedMeisai, rv, rateLog) = await LuckyDrawService.ConfirmSpinMeisaiAsync(lastRandomValue, selectedEventId, null, includeMuko: false);

                            if (confirmedMeisai == null)
                            {
                                XtraMessageBox.Show(this, "Có lỗi xác nhận phần thưởng, vui lòng thử lại!", "Lỗi xác suất", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                txtPhone.Enabled = true;
                                txtCustomerName.Enabled = true;
                                txtInvoiceNumber.Enabled = true;
                                return;
                            }

                            this.Invalidate();

                            string afterImageBase64 = null;

                            //StartFirework(5);
                            ShowFireworks();
                            string prizeName = confirmedMeisai.LUCKY_DRAW_MEISAI_NAME ?? confirmedMeisai.LUCKY_DRAW_MEISAI_NO ?? "Phần thưởng";
                            using (var frm = new frmPrizeNotification($" Quý khách đã nhận được: {prizeName}", this))
                            {
                                frm.StartPosition = FormStartPosition.Manual;
                                frm.Location = this.PointToScreen(new Point(
                                    this.Width / 2 - frm.Width / 2,
                                    this.Height / 2 - frm.Height / 2
                                ));

                                frm.Shown += (s2, ev2) =>
                                {
                                    try
                                    {
                                        try { Application.DoEvents(); System.Threading.Thread.Sleep(40); }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                        }
                                        try
                                        {
                                            afterImageBase64 = CaptureAppScreenshotAsBase64();
                                        }
                                        catch
                                        {
                                            afterImageBase64 = null;
                                        }
                                    }
                                    catch
                                    {
                                        afterImageBase64 = null;
                                    }
                                };

                                frm.ShowDialog();
                            }

                            // save history (kept as before)
                            if (string.IsNullOrWhiteSpace(selectedProgramId))
                            {
                                XtraMessageBox.Show(this, "Vui lòng chọn chương trình (Program) trước khi lưu lịch sử.", "Thiếu chương trình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            else
                            {
                                var historyDto = new GUI.DTO.LuckyDrawHistoryDTO
                                {
                                    PROGRAME_JISSHI_ID = 0,
                                    LUCKY_DRAW_PROGRAME_ID = selectedProgramId,
                                    LUCKY_DRAW_ID = confirmedMeisai.LUCKY_DRAW_ID,
                                    TOROKU_ID = "admin",
                                    MITUMORI_NO_SANSYO = txtInvoiceNumber?.Text?.Trim(),
                                    KOKAKU_HITO_NAME = txtCustomerName?.Text?.Trim(),
                                    KOKAKU_HITO_PHONE = txtPhone?.Text?.Trim(),
                                    KOKAKU_HITO_ADDRESS = null,
                                    KENSYO_DRAW_MEISAI_ID = confirmedMeisai.LUCKY_DRAW_MEISAI_NO,
                                    KENSYO_DRAW_MEISAI_SURYO = 1.0,
                                    RATE_LOG_SPLIT = !string.IsNullOrWhiteSpace(rateLog) ? rateLog : (confirmedMeisai?.LUCKY_DRAW_MEISAI_RATE.ToString() ?? string.Empty),
                                    JISSHI_KAISHI_DATE = spinStartTime,
                                    JISSHI_SYURYOU_DATE = DateTime.Now,
                                    MAE_JISSHI_BG_IMG = spinBeforeImageBase64,
                                    ATO_JISSHI_BG_IMG = afterImageBase64,
                                    KAISHI_DATE = confirmedMeisai.KAISHI_DATE,
                                    SYURYOU_DATE = confirmedMeisai.SYURYOU_DATE

                                };

                                try
                                {
                                    string payload = JsonConvert.SerializeObject(historyDto, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                }

                                var (saveSuccess, saveMessage, jisshiId) = await LuckyDrawService.SaveHistoryAsync(historyDto);
                                if (!saveSuccess)
                                {
                                    XtraMessageBox.Show(this, "Lỗi khi lưu phần thưởng lên BE: " + saveMessage, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                else
                                {
                                    // Nếu lưu thành công, refresh danh sách prizes từ backend để cập nhật LUCKY_DRAW_MEISAI_SURYO và trạng thái MUKO
                                    try
                                    {
                                        await RefreshLstPrizesFromBackendAsync(selectedEventId);
                                    }
                                    catch
                                    {
                                        //Debug.WriteLine("RefreshLstPrizesFromBackendAsync (after save) failed: " + exRefresh.Message);
                                    }
                                }

                                // reset inputs
                                txtPhone.Enabled = true;
                                txtCustomerName.Enabled = true;
                                txtInvoiceNumber.Enabled = true;

                                // clear before-image cache variable
                                spinBeforeImageBase64 = null;

                                try { EventSelectedByUser?.Invoke(selectedProgramId); }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            // Mechanism 2 completion (reuse existing logic)
                            if (finalWinnerIndex < 0 || finalWinnerIndex >= segmentTexts.Count)
                            {
                                XtraMessageBox.Show(this, "Không thể xác định người trúng thưởng. Vui lòng thử lại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            string winnerPhone = string.Empty;
                            if (segmentCodes != null && finalWinnerIndex >= 0 && finalWinnerIndex < segmentCodes.Count)
                            {
                                winnerPhone = segmentCodes[finalWinnerIndex];
                            }
                            else if (segmentTexts != null && finalWinnerIndex >= 0 && finalWinnerIndex < segmentTexts.Count)
                            {
                                var txt = segmentTexts[finalWinnerIndex];
                                var idx = txt.IndexOf(" - ");
                                winnerPhone = idx > 0 ? txt.Substring(0, idx) : txt;
                            }

                            var winner = phoneNumberList.FirstOrDefault(p => string.Equals(p.PhoneNumber, winnerPhone, StringComparison.OrdinalIgnoreCase));

                            if (winner == default)
                            {
                                XtraMessageBox.Show(this, $"Không thể xác định người trúng thưởng với số điện thoại: {winnerPhone}. Vui lòng thử lại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            var selectedPrizeObj = lstPrizes.SelectedItem;
                            var selectedMeisai = selectedPrizeObj as LuckyDrawMeisaiDTO;
                            var selectedKokaku = selectedPrizeObj as LuckyDrawKokakuDTO;

                            string selectedPrize = null;
                            string selectedPrizeId = null;
                            double? selectedPrizeSuryo = null;

                            if (selectedMeisai != null)
                            {
                                selectedPrize = selectedMeisai.LUCKY_DRAW_MEISAI_NAIYOU ?? selectedMeisai.LUCKY_DRAW_MEISAI_NAME ?? selectedMeisai.LUCKY_DRAW_MEISAI_NO;
                                selectedPrizeId = selectedMeisai.LUCKY_DRAW_MEISAI_NO;
                                selectedPrizeSuryo = selectedMeisai.LUCKY_DRAW_MEISAI_SURYO;
                            }
                            else if (selectedKokaku != null)
                            {
                                selectedPrize = selectedKokaku.KOKAKU_HITO_NAME ?? selectedKokaku.KOKAKU_HITO_PHONE;
                                selectedPrizeId = selectedKokaku.KOKAKU_HITO_PHONE;
                                selectedPrizeSuryo = null;
                            }
                            else
                            {
                                selectedPrize = "Phần thưởng";
                            }

                            if (selectedMeisai != null)
                            {
                                lstPrizes.Invalidate();
                                lstPrizes.ClearSelected();
                            }
                            else if (selectedKokaku != null)
                            {
                                selectedKokaku.KOKAKU_MUKO_FLG = 1;
                                lstPrizes.Invalidate();
                                lstPrizes.ClearSelected();
                            }

                            try { this.Refresh(); Application.DoEvents(); }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                            }

                            string afterImageBase64 = null;
                            //StartFirework(5);
                            ShowFireworks();
                            string notif = $"Quý khách {winner.CustomerName} số điện thoại ({winner.PhoneNumber}) đã trúng phần thưởng '{selectedPrize}'!";

                            using (var frm = new frmPrizeNotification(notif, this))
                            {
                                frm.StartPosition = FormStartPosition.Manual;
                                frm.Location = this.PointToScreen(new Point(
                                    this.Width / 2 - frm.Width / 2,
                                    this.Height / 2 - frm.Height / 2
                                ));

                                frm.Shown += (s2, ev2) =>
                                {
                                    try
                                    {
                                        try { Application.DoEvents(); System.Threading.Thread.Sleep(40); }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                        }
                                        try { afterImageBase64 = CaptureAppScreenshotAsBase64(); } catch { afterImageBase64 = null; }
                                    }
                                    catch { afterImageBase64 = null; }
                                };

                                frm.ShowDialog();
                            }

                            MarkPhoneNumberAsWinner(winner.PhoneNumber);

                            AddWinnerToLstWinnersByBunrui(selectedMeisai, selectedKokaku, winner.PhoneNumber, winner.CustomerName);

                            // Save history, update backend etc (kept same as before)
                            try
                            {
                                var selectedProgramId = gridLookUpEditEvent?.EditValue as string;
                                string selectedEventId = null;
                                if (!string.IsNullOrWhiteSpace(selectedProgramId))
                                {
                                    try
                                    {
                                        var prog = await LuckyDrawService.GetProgramByIdAsync(selectedProgramId);
                                        selectedEventId = prog?.LUCKY_DRAW_ID;
                                    }
                                    catch { selectedEventId = null; }
                                }

                                if (string.IsNullOrWhiteSpace(selectedProgramId) || string.IsNullOrWhiteSpace(selectedEventId))
                                {
                                }
                                else
                                {
                                    var historyDto = new GUI.DTO.LuckyDrawHistoryDTO
                                    {
                                        PROGRAME_JISSHI_ID = 0,
                                        LUCKY_DRAW_PROGRAME_ID = selectedProgramId,
                                        LUCKY_DRAW_ID = selectedEventId,
                                        TOROKU_ID = "admin",
                                        MITUMORI_NO_SANSYO = null,
                                        KOKAKU_HITO_NAME = winner.CustomerName,
                                        KOKAKU_HITO_PHONE = winner.PhoneNumber,
                                        KOKAKU_HITO_ADDRESS = null,
                                        RATE_LOG_SPLIT = "Random",
                                        KENSYO_DRAW_MEISAI_ID = selectedPrizeId,
                                        KENSYO_DRAW_MEISAI_SURYO = selectedMeisai != null ? 1.0 : selectedPrizeSuryo,
                                        JISSHI_KAISHI_DATE = spinStartTime,
                                        JISSHI_SYURYOU_DATE = DateTime.Now,
                                        MAE_JISSHI_BG_IMG = spinBeforeImageBase64,
                                        ATO_JISSHI_BG_IMG = afterImageBase64,
                                        KAISHI_DATE = selectedMeisai?.KAISHI_DATE,
                                        SYURYOU_DATE = selectedMeisai?.SYURYOU_DATE
                                    };

                                    try
                                    {
                                        string payload = JsonConvert.SerializeObject(historyDto, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                    }

                                    var (saveSuccess, saveMessage, jisshiId) = await LuckyDrawService.SaveHistoryAsync(historyDto);
                                    if (!saveSuccess)
                                    {
                                        XtraMessageBox.Show(this, "Lỗi khi lưu lịch sử: " + saveMessage, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                    else
                                    {
                                        if (selectedKokaku != null)
                                        {
                                            {
                                                var (mSuccess, mMsg) = await LuckyDrawService.SetKokakuMukoFlagAsync(selectedEventId, selectedKokaku.KOKAKU_HITO_PHONE, 1, "admin", "Awarded");
                                            }
                                        }

                                        try { await RefreshLstPrizesFromBackendAsync(selectedEventId); } catch { }

                                        try { EventSelectedByUser?.Invoke(selectedProgramId); }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }

                            spinBeforeImageBase64 = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show(this, $"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtPhone.Enabled = true;
                        txtCustomerName.Enabled = true;
                        txtInvoiceNumber.Enabled = true;
                    }
                }
            }
            catch
            {
            }
        }

        // Hàm lưu phần thưởng lên BE
        private async Task<bool> SavePrizeWinnerToApiAsync(LuckyDrawMeisaiDTO meisai)
        {
            try
            {
                if (meisai == null) return false;

                var dto = new GUI.DTO.LuckyDrawHistoryDTO
                {
                    PROGRAME_JISSHI_ID = 0,
                    LUCKY_DRAW_PROGRAME_ID = null,
                    LUCKY_DRAW_ID = meisai.LUCKY_DRAW_ID,
                    TOROKU_ID = "admin",
                    MITUMORI_NO_SANSYO = txtInvoiceNumber?.Text?.Trim(),
                    KOKAKU_HITO_NAME = txtCustomerName?.Text?.Trim(),
                    KOKAKU_HITO_PHONE = txtPhone?.Text?.Trim(),
                    KENSYO_DRAW_MEISAI_ID = meisai.LUCKY_DRAW_MEISAI_NO,
                    KENSYO_DRAW_MEISAI_SURYO = meisai.LUCKY_DRAW_MEISAI_SURYO,
                    // timestamps: start recorded when spin started, end = now
                    JISSHI_KAISHI_DATE = spinStartTime,
                    JISSHI_SYURYOU_DATE = DateTime.Now
                };

                // Nếu LUCKY_DRAW_PROGRAME_ID chưa có, cố gắng lấy chương trình liên quan từ BE
                if (string.IsNullOrWhiteSpace(dto.LUCKY_DRAW_PROGRAME_ID) && !string.IsNullOrWhiteSpace(dto.LUCKY_DRAW_ID))
                {
                    try
                    {
                        var programs = await LuckyDrawService.GetAllProgramsAsync(dto.LUCKY_DRAW_ID);
                        var first = programs?.FirstOrDefault();
                        if (first != null)
                        {
                            try
                            {
                                var progIdProp = first.GetType().GetProperty("LUCKY_DRAW_PROGRAME_ID");
                                if (progIdProp != null)
                                {
                                    var val = progIdProp.GetValue(first) as string;
                                    if (!string.IsNullOrWhiteSpace(val))
                                        dto.LUCKY_DRAW_PROGRAME_ID = val;
                                }
                            }
                            catch { /* swallow */ }
                        }
                    }
                    catch
                    {
                    }
                }

                // Log payload để debug (Output window)
                try
                {
                    string payload = JsonConvert.SerializeObject(dto, Formatting.Indented, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                }

                var (success, message, jisshiId) = await LuckyDrawService.SaveHistoryAsync(dto);
                if (!success)
                {
                    MessageBox.Show("Lưu lịch sử thất bại: " + message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    // optional: notify parent / other modules to refresh history grid
                    try { EventSelectedByUser?.Invoke(dto.LUCKY_DRAW_PROGRAME_ID ?? string.Empty); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                }
                return success;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu phần thưởng lên BE!\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }


        private async Task RefreshLstPrizesFromBackendAsync(string eventId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eventId)) return;

                List<LuckyDrawMeisaiDTO> meisais;
                try
                {
                    // includeMuko:true để cập nhật trạng thái hiển thị (muko/expired) trong lstPrizes
                    meisais = await LuckyDrawService.GetAllMeisaiAsync(eventId, category: null, includeMuko: true) ?? new List<LuckyDrawMeisaiDTO>();
                }
                catch
                {
                    return;
                }

                Action updateUi = () =>
                {
                    try
                    {
                        for (int i = 0; i < lstPrizes.Items.Count; i++)
                        {
                            if (!(lstPrizes.Items[i] is LuckyDrawMeisaiDTO local)) continue;

                            var remote = meisais.FirstOrDefault(m =>
                                string.Equals(m.LUCKY_DRAW_MEISAI_NO, local.LUCKY_DRAW_MEISAI_NO, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(m.LUCKY_DRAW_ID, local.LUCKY_DRAW_ID, StringComparison.OrdinalIgnoreCase));

                            if (remote != null)
                            {
                                local.LUCKY_DRAW_MEISAI_SURYO = remote.LUCKY_DRAW_MEISAI_SURYO;
                                local.LUCKY_DRAW_MEISAI_MUKO_FLG = remote.LUCKY_DRAW_MEISAI_MUKO_FLG;
                            }
                        }

                        lstPrizes.Invalidate();
                    }
                    catch
                    {
                    }
                };

                if (this.IsHandleCreated && this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)(() => updateUi()));
                else
                    updateUi();

            }
            catch (Exception ex)
            {
            }
        }

        public void UpdateNumSegments(int newNumSegments) // cơ chế 2
        {
            if (newNumSegments < 1) newNumSegments = 1;

            Action updateAction = () =>
            {
                // cập nhật giá trị nội bộ
                this.numSegments = newNumSegments;

                // ensure lists exist
                segmentTexts = segmentTexts ?? new List<string>();
                segmentImages = segmentImages ?? new List<string>();
                segmentImageCache = segmentImageCache ?? new List<Image>();
                segmentCodes = segmentCodes ?? new List<string>();
                segmentRates = segmentRates ?? new List<double>();

                // Nếu đang ở Cơ chế 2, segment hiển thị từ phoneNumberList
                if (spinMode == "2")
                {
                    // Hiển thị cả Phone + Name (nếu name rỗng thì chỉ phone)
                    segmentTexts = phoneNumberList
                        .Select(p => string.IsNullOrWhiteSpace(p.CustomerName) ? p.PhoneNumber : $"{p.PhoneNumber} - {p.CustomerName}")
                        .ToList();

                    segmentImages = phoneNumberList.Select(_ => (string)null).ToList();

                    // dispose ảnh cũ nếu có
                    if (segmentImageCache != null)
                    {
                        foreach (var img in segmentImageCache) img?.Dispose();
                    }
                    segmentImageCache = phoneNumberList.Select(_ => (Image)null).ToList();

                    // segmentCodes giữ phone riêng (dùng để đối chiếu / tìm winner chính xác)
                    segmentCodes = phoneNumberList.Select(p => p.PhoneNumber).ToList();

                    // rates not applicable for phone list
                    segmentRates = phoneNumberList.Select(_ => 0d).ToList();
                }
                else // Cơ chế 1: vouchers / meisai
                {
                    // Nếu có ít hơn numSegments thì thêm placeholder "Trống"
                    if (segmentTexts.Count < this.numSegments)
                    {
                        int missing = this.numSegments - segmentTexts.Count;
                        for (int i = 0; i < missing; i++)
                        {
                            segmentTexts.Add("Trống");
                            segmentImages.Add(null);
                            segmentImageCache.Add(null);
                            segmentCodes.Add("");
                            segmentRates.Add(0d);
                        }
                    }
                    // Nếu nhiều hơn numSegments thì trim và dispose ảnh thừa
                    else if (segmentTexts.Count > this.numSegments)
                    {
                        int extra = segmentTexts.Count - this.numSegments;

                        segmentTexts.RemoveRange(this.numSegments, extra);

                        if (segmentImages != null && segmentImages.Count > this.numSegments)
                            segmentImages.RemoveRange(this.numSegments, Math.Min(extra, segmentImages.Count - this.numSegments));

                        if (segmentImageCache != null && segmentImageCache.Count > this.numSegments)
                        {
                            var toDispose = segmentImageCache.Skip(this.numSegments).ToList();
                            foreach (var img in toDispose) img?.Dispose();
                            segmentImageCache.RemoveRange(this.numSegments, Math.Min(extra, segmentImageCache.Count - this.numSegments));
                        }

                        if (segmentCodes != null && segmentCodes.Count > this.numSegments)
                            segmentCodes.RemoveRange(this.numSegments, Math.Min(extra, segmentCodes.Count - this.numSegments));

                        if (segmentRates != null && segmentRates.Count > this.numSegments)
                            segmentRates.RemoveRange(this.numSegments, Math.Min(extra, segmentRates.Count - this.numSegments));
                    }
                }

                // Vẽ lại control
                this.Invalidate();
            };

            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate { updateAction(); });
            else updateAction();
        }

        public void SetWheelBackgroundImage(Image img)
        {
            // Dispose ảnh nền cũ nếu có để tránh leak
            try
            {
                if (wheelBackgroundImage != null)
                {
                    try { wheelBackgroundImage.Dispose(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    wheelBackgroundImage = null;
                }

                if (cachedBackground != null)
                {
                    try { cachedBackground.Dispose(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                    }
                    cachedBackground = null;
                }

                if (img == null)
                {
                    this.Invalidate();
                    return;
                }

                // Clone image into a Bitmap instance we own (avoid external stream lifetime issues)
                var bmp = new Bitmap(img);

                wheelBackgroundImage = bmp;
                cachedBackground = null;
                this.Invalidate();

                try
                {
                    string path = System.IO.Path.Combine(Application.StartupPath, "wheel_bg.png");
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi lưu hình nền: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch
            {
            }
        }

        private void LoadWheelBackgroundImage()
        {
            string path = System.IO.Path.Combine(Application.StartupPath, "wheel_bg.png");
            if (System.IO.File.Exists(path))
            {
                try
                {
                    // Read file bytes and create Bitmap copy so file isn't locked
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    using (var ms = new System.IO.MemoryStream(bytes))
                    {
                        using (var img = Image.FromStream(ms))
                        {
                            // Clone to decouple from stream
                            var bmp = new Bitmap(img);

                            // Dispose old if any
                            if (wheelBackgroundImage != null)
                            {
                                try { wheelBackgroundImage.Dispose(); }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                }
                            }
                            wheelBackgroundImage = bmp;

                            // reset cachedBackground so OnPaint will rebuild it
                            if (cachedBackground != null)
                            {
                                try { cachedBackground.Dispose(); }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[MethodName] Error: {ex.Message}");
                                }
                                cachedBackground = null;
                            }
                        }
                    }
                }
                catch
                {
                    wheelBackgroundImage = null;
                }
            }
            else
            {
                wheelBackgroundImage = null;
            }
        }
        public void UpdateSegmentColors(List<Color> colors)
        {
            segmentColors = new List<Color>(colors);

            // ⭐ DISPOSE CACHE CŨ
            foreach (var kv in _segmentBrushCache)
            {
                try { kv.Value?.Dispose(); } catch { }
            }
            _segmentBrushCache.Clear();

            SaveSegmentColorsToFile(segmentColors);
            this.Invalidate();
        }

        private void SaveSegmentColorsToFile(List<Color> colors)
        {
            try
            {
                var colorList = new List<string>();
                foreach (var color in colors)
                {
                    colorList.Add(ColorTranslator.ToHtml(color));
                }
                string json = JsonConvert.SerializeObject(colorList);
                string path = Path.Combine(Application.StartupPath, "segment_colors.json");
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch { /* silent fail */ }
        }

        private void LoadSegmentColorsFromFile()
        {
            string path = System.IO.Path.Combine(Application.StartupPath, "segment_colors.json");
            if (System.IO.File.Exists(path))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(path, Encoding.UTF8);
                    var colorList = JsonConvert.DeserializeObject<List<string>>(json);
                    var loadedColors = new List<Color>();
                    foreach (var colorStr in colorList)
                    {
                        loadedColors.Add(ColorTranslator.FromHtml(colorStr));
                    }
                    if (loadedColors.Count > 0)
                    {
                        segmentColors = loadedColors;
                    }
                }
                catch
                {
                    // Nếu lỗi thì dùng màu mặc định
                }
            }
        }
        public void SetWheelBackgroundColor(Color color)
        {
            this.BackColor = color;
            this.Invalidate();
        }
        private void MarqueeTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                marqueeLabel.Left += marqueeSpeed;
                if (marqueeLabel.Left > this.Width)
                {
                    marqueeLabel.Left = -marqueeLabel.Width;
                }

                // Keep marquee behind other controls while it moves
                EnsureMarqueeBehindDockPanel();
            }
            catch { /* swallow to avoid timer crash */ }
        }

        private void marqueeLabel_Paint(object sender, PaintEventArgs e)
        {
            var lbl = sender as Label;
            var text = lbl.Text;
            var font = lbl.Font;
            var textSize = e.Graphics.MeasureString(text, font);
            int rectW = (int)textSize.Width + 16;
            int rectH = (int)textSize.Height + 8;

        }
        private void EnsureMarqueeBehindDockPanel()
        {
            try
            {
                if (marqueeLabel == null) return;
                // send marquee to back within this container so it won't draw above sibling controls
                marqueeLabel.SendToBack();
            }
            catch { /* swallow */ }
        }



        private string spinMode = "1"; // Mặc định là "Cơ chế 1"

        // Phương thức để thiết lập chế độ vòng quay
        public void SetSpinMode(string mode)
        {
            if (spinMode == mode) return;

            spinMode = mode;
            lblSpinMode.Text = $"Chế độ: {spinMode}";

            if (spinMode == "1")
            {
                txtPhone.Visible = true;
                lblPhone.Visible = true;
                txtCustomerName.Visible = true;
                lblCustomerName.Visible = true;
                txtInvoiceNumber.Visible = true;
                lblInvoiceNumber.Visible = true;

                gcPhoneNumbers.Visible = false;

                lblWarehouse.Visible = false;
                lblDateFrom.Visible = false;
                lblDateTo.Visible = false;
                lblBranch.Visible = false;  
                lblPriceMin.Visible = false;
                lblPriceMax.Visible = false;

                slueWarehouse.Visible = false;
                deDateFrom.Visible = false;
                deDateTo.Visible = false;
                ccbeBranch.Visible = false;
                sePriceMin.Visible = false;
                sePrinceMax.Visible = false;
                btnXem.Visible = false;

                lstPrizes.Visible = false;
                lstWinners.Visible = false;
                labelControl1.Visible = false;
                labelControl2.Visible = false;

                Task.Run(async () =>
                {
                    try
                    {
                        await LoadWheelVouchersAsync();
                        // invoke an toàn: tạo MethodInvoker từ delegate
                        if (this.InvokeRequired)
                            this.Invoke((MethodInvoker)delegate { UpdateNumSegments(numSegments); });
                        else
                            UpdateNumSegments(numSegments);
                    }
                    catch
                    {
                        MessageBox.Show("Không thể tải dữ liệu vòng quay. Vui lòng thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
            else if (spinMode == "2")
            {
                txtPhone.Visible = false;
                lblPhone.Visible = false;
                txtCustomerName.Visible = false;
                lblCustomerName.Visible = false;
                txtInvoiceNumber.Visible = false;
                lblInvoiceNumber.Visible = false;

                gcPhoneNumbers.Visible = true;

                lblWarehouse.Visible = true;
                lblDateFrom.Visible = true;
                lblDateTo.Visible = true;
                lblBranch.Visible = true;
                lblPriceMin.Visible = true;
                lblPriceMax.Visible = true;

                slueWarehouse.Visible = true;
                deDateFrom.Visible = true;
                deDateTo.Visible = true;
                ccbeBranch.Visible = true;
                sePriceMin.Visible = true;
                sePrinceMax.Visible = true;
                btnXem.Visible = true;

                lstPrizes.Visible = true;
                lstWinners.Visible = true;
                labelControl1.Visible = true;
                labelControl2.Visible = true;

                // Use kokakuBindingList count (bound datasource) to decide segments
                UpdateNumSegments(kokakuBindingList.Count);
            }

            this.Refresh();
        }


        public string GetSpinMode()
        {
            return spinMode; // Trả về chế độ hiện tại
        }

        // Cấu hình lstPrizes để hiển thị hình ảnh và tên phần thưởng
        private void ConfigureLstPrizes()
        {
            lstPrizes.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed; // Tùy chỉnh cách vẽ
            lstPrizes.ItemHeight = 60; // Chiều cao mỗi mục (đủ để hiển thị hình ảnh)
            lstPrizes.SelectionMode = SelectionMode.One; // Chỉ cho phép chọn một phần tử

            // Đảm bảo không đăng ký trùng
            lstPrizes.DrawItem -= LstPrizes_DrawItem;

            lstPrizes.DrawItem += LstPrizes_DrawItem;

            // Bật DoubleBuffered (non-public) để giảm chớp khi resize
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(lstPrizes, true, null);
            }
            catch { /* ignore */ }
        }

        private void LstPrizes_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            var item = lstPrizes.Items[e.Index];
            var meisai = item as LuckyDrawMeisaiDTO;
            var kokaku = item as LuckyDrawKokakuDTO;

            if (meisai == null && kokaku == null) return;

            string imgField = null;
            int? mukoFlg = null;
            string titleText = null;
            string qtyText = null;
            int? onshouNum = null;

            if (meisai != null)
            {
                imgField = meisai.LUCKY_DRAW_MEISAI_IMG;
                mukoFlg = meisai.LUCKY_DRAW_MEISAI_MUKO_FLG;
                titleText = meisai.LUCKY_DRAW_MEISAI_NAIYOU ?? meisai.LUCKY_DRAW_MEISAI_NAME ?? "Phần thưởng";
                onshouNum = meisai.LUCKY_DRAW_MEISAI_ONSHOU_NUM;
                if (meisai.LUCKY_DRAW_MEISAI_SURYO.HasValue)
                {
                    var v = meisai.LUCKY_DRAW_MEISAI_SURYO.Value;
                    qtyText = v % 1 == 0 ? $"Số lượng: {v:0}" : $"Số lượng: {v:0.##}";
                }
            }
            else
            {
                imgField = kokaku.KOKAKU_HITO_IMG;
                mukoFlg = kokaku.KOKAKU_MUKO_FLG;
                titleText = !string.IsNullOrWhiteSpace(kokaku.KOKAKU_HITO_NAME) ? kokaku.KOKAKU_HITO_NAME : kokaku.KOKAKU_HITO_PHONE ?? "Người tham gia";
                qtyText = null;
            }

            // Vẽ hình ảnh từ cache (nếu có)
            try
            {
                Rectangle imgRect = new Rectangle(e.Bounds.Left + 5, e.Bounds.Top + 5, 50, 50);
                Image imgToDraw = GetPrizeImage(imgField);

                if (imgToDraw != null)
                {
                    bool used = (meisai != null && meisai.LUCKY_DRAW_MEISAI_MUKO_FLG == 1) ||
                                (kokaku != null && kokaku.KOKAKU_MUKO_FLG == 1);

                    if (used)
                    {
                        ColorMatrix matrix = new ColorMatrix();
                        matrix.Matrix33 = 0.3f;
                        using (var attributes = new ImageAttributes())
                        {
                            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                            e.Graphics.DrawImage(imgToDraw, imgRect, 0, 0, imgToDraw.Width, imgToDraw.Height, GraphicsUnit.Pixel, attributes);
                        }
                    }
                    else
                    {
                        e.Graphics.DrawImage(imgToDraw, imgRect);
                    }
                }
            }
            catch
            {
            }

            bool isDisabled = false;
            if (meisai != null) isDisabled = (meisai.LUCKY_DRAW_MEISAI_MUKO_FLG == 1);
            else isDisabled = (kokaku?.KOKAKU_MUKO_FLG == 1);

            var mainBrush = new SolidBrush(isDisabled ? Color.Gray : e.ForeColor);
            try
            {
                int textX = e.Bounds.Left + 60;
                int textWidth = e.Bounds.Width - 65;

                // Title
                using (var titleFont = new Font(e.Font.FontFamily, e.Font.Size + 1, FontStyle.Bold))
                {
                    int titleTop = e.Bounds.Top + 8;
                    var titleRect = new Rectangle(textX, titleTop, textWidth, 20);
                    e.Graphics.DrawString(titleText ?? "Phần thưởng", titleFont, mainBrush, titleRect);
                }

                // Qty + ONSHOU (vẽ ONSHOU nhỏ kế bên qty)
                if (!string.IsNullOrEmpty(qtyText) || onshouNum.HasValue)
                {
                    using (var qtyFont = new Font(e.Font.FontFamily, Math.Max(8f, e.Font.Size - 1), FontStyle.Regular))
                    {
                        int qtyTop = e.Bounds.Top + 28;
                        var qtyRect = new Rectangle(textX, qtyTop, textWidth, 16);
                        if (!string.IsNullOrEmpty(qtyText))
                        {
                            e.Graphics.DrawString(qtyText, qtyFont, mainBrush, qtyRect);
                        }

                        if (onshouNum.HasValue)
                        {
                            string onshouText = $"#{onshouNum.Value}";
                            using (var onshouFont = new Font(e.Font.FontFamily, Math.Max(7f, qtyFont.Size - 1), FontStyle.Bold))
                            {
                                var qtyMeasure = e.Graphics.MeasureString(qtyText ?? string.Empty, qtyFont);
                                var onshouSize = e.Graphics.MeasureString(onshouText, onshouFont);

                                int onshouX = textX + (int)qtyMeasure.Width + 8;
                                int maxRight = e.Bounds.Right - 8 - (int)onshouSize.Width;
                                if (onshouX > maxRight) onshouX = maxRight;
                                int onshouY = qtyTop;

                                var rect = new Rectangle(onshouX, onshouY, (int)onshouSize.Width + 6, (int)onshouSize.Height);
                                using (var badgeBrush = new SolidBrush(Color.DarkRed))
                                using (var badgeTextBrush = new SolidBrush(Color.White))
                                {
                                    e.Graphics.FillRectangle(badgeBrush, rect);
                                    e.Graphics.DrawString(onshouText, onshouFont, badgeTextBrush, rect.X + 3, rect.Y);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                mainBrush.Dispose();
            }

            e.DrawFocusRectangle();
        }

        private void LoadPrizesToLstPrizes(List<LuckyDrawMeisaiDTO> meisais)
        {
            // Dispose và clear cache cũ trước khi refill list
            ClearPrizeImageCache();

            lstPrizes.Items.Clear();
            if (meisais == null) return;

            // Normalize mỗi item
            foreach (var m in meisais)
            {
                if (!m.LUCKY_DRAW_MEISAI_MUKO_FLG.HasValue)
                    m.LUCKY_DRAW_MEISAI_MUKO_FLG = 0;

                if (!string.IsNullOrWhiteSpace(m.LUCKY_DRAW_MEISAI_NAME))
                    m.LUCKY_DRAW_MEISAI_NAME = m.LUCKY_DRAW_MEISAI_NAME.Trim();
                if (!string.IsNullOrWhiteSpace(m.LUCKY_DRAW_MEISAI_NAIYOU))
                    m.LUCKY_DRAW_MEISAI_NAIYOU = m.LUCKY_DRAW_MEISAI_NAIYOU.Trim();
            }

            var ordered = meisais
                .OrderBy(m => m.LUCKY_DRAW_MEISAI_ONSHOU_NUM.HasValue ? m.LUCKY_DRAW_MEISAI_ONSHOU_NUM.Value : int.MaxValue)
                .ThenBy(m => m.LUCKY_DRAW_MEISAI_NAME ?? string.Empty)
                .ToList();

            foreach (var m in ordered)
            {
                lstPrizes.Items.Add(m);
            }
        }


        private void lstPrizes_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = lstPrizes.SelectedItem;
            if (selected == null) return;

            var meisai = selected as LuckyDrawMeisaiDTO;
            var kokaku = selected as LuckyDrawKokakuDTO;

            bool isUsed = false;
            // SỬA: 1 = used (nhất quán)
            if (meisai != null) isUsed = (meisai.LUCKY_DRAW_MEISAI_MUKO_FLG == 1);
            else if (kokaku != null) isUsed = (kokaku.KOKAKU_MUKO_FLG == 1);

            if (isUsed)
            {
                MessageBox.Show("Phần thưởng này đã được sử dụng, vui lòng chọn phần thưởng khác!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lstPrizes.ClearSelected();
            }
        }

        private void txtPhoneNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V) // Kiểm tra tổ hợp Ctrl + V
            {
                Task.Delay(100).ContinueWith(_ =>
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        //if (!string.IsNullOrWhiteSpace(txtPhoneNumber.Text))
                        //{
                        //    txtPhoneNumber.Refresh(); // Làm mới TextBox
                        //}
                    }));
                });
            }
        }


        private void LstPhoneNumbers_SelectedIndexChanged(object sender, EventArgs e)
        {
            //try
            //{
            //    if (lstPhoneNumbers.SelectedItem == null) return;

            //    var selected = lstPhoneNumbers.SelectedItem;

            //    if (selected is LuckyDrawKokakuDTO kokaku)
            //    {
            //        var phone = kokaku.KOKAKU_HITO_PHONE ?? string.Empty;
            //        var name = kokaku.KOKAKU_HITO_NAME ?? string.Empty;
            //        //txtPhoneNumber.Text = string.IsNullOrWhiteSpace(name) ? phone : $"{phone} - {name}";
            //    }
            //    else
            //    {
            //        // fallback nếu item là chuỗi
            //        string phone = selected.ToString();
            //        var entry = phoneNumberList.FirstOrDefault(p => p.PhoneNumber == phone);
            //        //if (entry != default)
            //        //{
            //        //    txtPhoneNumber.Text = string.IsNullOrWhiteSpace(entry.CustomerName)
            //        //        ? entry.PhoneNumber
            //        //        : $"{entry.PhoneNumber} - {entry.CustomerName}";
            //        //}
            //        //else
            //        //{
            //        //    txtPhoneNumber.Text = phone;
            //        //}
            //    }
            //}
            //catch
            //{
            //    // swallow - không để UI crash
            //}
        }

        private void ConfigureGcPhoneNumbers()
        {
            try
            {
                if (gcPhoneNumbers == null) return;

                var view = gcPhoneNumbers.MainView as GridView;
                if (view == null && gcPhoneNumbers.Views.Count > 0)
                    view = gcPhoneNumbers.Views[0] as GridView;
                if (view == null) return;

                // Clear existing columns and configure
                view.Columns.Clear();
                view.OptionsBehavior.Editable = false;
                view.OptionsSelection.EnableAppearanceFocusedRow = true;
                view.OptionsView.ShowIndicator = false;
                view.OptionsView.ColumnAutoWidth = false;

                // Add visible columns
                view.Columns.AddVisible("KOKAKU_HITO_PHONE", "Số điện thoại");
                view.Columns.AddVisible("KOKAKU_HITO_NAME", "Tên khách hàng");
                view.Columns["KOKAKU_HITO_PHONE"].Width = 115;
                view.Columns["KOKAKU_HITO_NAME"].Width = 145;

                // Bind DataSource after columns are defined (prevent auto column creation)
                gcPhoneNumbers.DataSource = kokakuBindingList;

                // Fit widths but keep user resizing possible
                try { view.BestFitColumns(); } catch { /* ignore */ }

                // Register styling / events
                view.RowCellStyle -= GridViewPhoneNumbers_RowCellStyle;
                view.RowCellStyle += GridViewPhoneNumbers_RowCellStyle;

                view.FocusedRowChanged -= GridViewPhoneNumbers_FocusedRowChanged;
                view.FocusedRowChanged += GridViewPhoneNumbers_FocusedRowChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConfigureGcPhoneNumbers error: " + ex.Message);
            }
        }

        private void GridViewPhoneNumbers_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            try
            {
                var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
                if (view == null) return;

                var row = view.GetRow(e.RowHandle) as LuckyDrawKokakuDTO;
                if (row == null) return;

                // Áp dụng highlight nếu số điện thoại nằm trong highlightedPhoneNumbers
                if ((e.Column.FieldName == "KOKAKU_HITO_PHONE" || e.Column.FieldName == "KOKAKU_HITO_NAME")
                    && !string.IsNullOrEmpty(row.KOKAKU_HITO_PHONE)
                    && highlightedPhoneNumbers.Contains(row.KOKAKU_HITO_PHONE))
                {
                    e.Appearance.ForeColor = Color.Red;
                    e.Appearance.Font = new Font(e.Appearance.Font, FontStyle.Bold);
                }
            }
            catch { /* swallow to keep UI stable */ }
        }

        private void GridViewPhoneNumbers_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            try
            {
                var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
                if (view == null) return;

                var row = view.GetRow(e.FocusedRowHandle) as LuckyDrawKokakuDTO;
                if (row == null) return;

                // Nếu bạn muốn mirror hành vi LstPhoneNumbers_SelectedIndexChanged,
                // ở đây có thể cập nhật một TextBox hoặc biến tạm. Hiện để trống.
                // Ví dụ (nếu cần): txtPhoneNumber.Text = row.KOKAKU_HITO_PHONE;
            }
            catch { }
        }

        private void LoadKokakusToLstPhoneNumbers(List<LuckyDrawKokakuDTO> kokakus)
        {
            // Marshal to UI thread if needed
            if (this.IsHandleCreated && this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)(() => LoadKokakusToLstPhoneNumbers(kokakus)));
                return;
            }

            try
            {
                // Clear previous
                phoneNumberList.Clear();

                // Update binding list safely (suspend events)
                kokakuBindingList.RaiseListChangedEvents = false;
                try
                {
                    kokakuBindingList.Clear();

                    if (kokakus != null)
                    {
                        foreach (var k in kokakus)
                        {
                            var phone = k?.KOKAKU_HITO_PHONE ?? string.Empty;
                            var name = k?.KOKAKU_HITO_NAME ?? string.Empty;

                            phoneNumberList.Add((phone, name));

                            // Add original DTO so backend operations still have full object
                            kokakuBindingList.Add(k);
                        }
                    }
                }
                finally
                {
                    kokakuBindingList.RaiseListChangedEvents = true;
                    // Refresh grid
                    (gcPhoneNumbers?.MainView as GridView)?.RefreshData();
                    gcPhoneNumbers?.Refresh();
                }

                // Sync wheel segments
                UpdateNumSegments(phoneNumberList.Count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadKokakusToLstPhoneNumbers error: {ex.Message}");
            }
        }

        // Helper: thêm người trúng vào lstWinners nhóm theo LUCKY_DRAW_MEISAI_BUNRUI
        private void AddWinnerToLstWinnersByBunrui(LuckyDrawMeisaiDTO meisai, LuckyDrawKokakuDTO kokaku, string phone, string customerName)
        {
            if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(customerName)) return;

            // Primary grouping key: ONSHOU_NUM (if present)
            int? onshou = meisai?.LUCKY_DRAW_MEISAI_ONSHOU_NUM;
            string prefix;
            if (onshou.HasValue)
            {
                prefix = $"[Giải.{onshou.Value}]";
            }
            else
            {
                // For kokaku or prizes without ONSHOU_NUM, group under generic label
                prefix = "[Giải.]";
            }

            string display = $"{prefix} {phone} - {customerName}";

            // Insert after last item with same prefix (case-insensitive), else append
            int insertIndex = -1;
            for (int i = 0; i < lstWinners.Items.Count; i++)
            {
                var it = lstWinners.Items[i]?.ToString() ?? string.Empty;
                if (it.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    insertIndex = i;
                }
            }

            if (insertIndex >= 0)
            {
                lstWinners.Items.Insert(insertIndex + 1, display);
            }
            else
            {
                lstWinners.Items.Add(display);
            }
        }

        
    }
}