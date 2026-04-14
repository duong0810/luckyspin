USE master;
GO

-- Tạo database mới
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'LuckyDraw')
BEGIN
    ALTER DATABASE LuckyDraw SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LuckyDraw;
END
GO

CREATE DATABASE LuckyDraw;
GO

USE LuckyDraw;
GO
CREATE TABLE [dbo].[LUCKY_DRAW]( -- skien quay thưởng chính
	[LUCKY_DRAW_ID] [VARCHAR](50) NOT NULL, -- khoá chính , mã skien
	[LUCKY_DRAW_NAME] [NVARCHAR](250) NULL, -- tên skien
	[LUCKY_DRAW_BUNRUI] [VARCHAR](50) NULL, -- loại skien
	[LUCKY_DRAW_TITLE] [NVARCHAR](250) NULL, -- tiêu đề hiển thị
	[LUCKY_DRAW_SLOGAN] [NVARCHAR](250) NULL, 
	[LUCKY_DRAW_BG_IMG] [VARBINARY](MAX) NULL,
	[LUCKY_DRAW_SLOT_NUM] INT NOT NULL,-- số ô vòng quay
	[TOROKU_DATE] [DATETIME] NOT NULL,  -- lưu ngày giờ
	[TOROKU_ID] [VARCHAR](12) NOT NULL, -- ID người tạo
	[KOSIN_NAIYOU] [NVARCHAR](500) NULL, -- nội dung chỉnh sửa gần nhất
	[KOSIN_DATE] [DATETIME] NULL,  -- ngày chỉnh sửa gần nhất
	[KOSIN_ID] [VARCHAR](12) NOT NULL, -- ID của người chỉnh sửa
 CONSTRAINT [PK_LUCKY_DRAW] PRIMARY KEY CLUSTERED 
(
	[LUCKY_DRAW_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
;
ALTER TABLE [dbo].[LUCKY_DRAW] ADD  CONSTRAINT [DF_LUCKY_DRAW_TOROKU_DATE]  DEFAULT (GETDATE()) FOR [TOROKU_DATE]
;


CREATE TABLE [dbo].[LUCKY_DRAW_MEISAI](  -- chi tiết vquay
	[LUCKY_DRAW_ID] [varchar](50) NOT NULL, -- ID Skien vquay
	[LUCKY_DRAW_MEISAI_NO] [varchar](5) NOT NULL, -- stt giải
	[LUCKY_DRAW_MEISAI_NAME] [nvarchar](50) NULL, -- tên giải
	[LUCKY_DRAW_MEISAI_NAIYOU] [nvarchar](150) NULL, -- ndung mô tả giải
	[LUCKY_DRAW_MEISAI_IMG] [varbinary](max) NULL, 
	[LUCKY_DRAW_MEISAI_RATE] [float] NOT NULL, -- tỉ lệ trúng qtrong
	[LUCKY_DRAW_MEISAI_SURYO] [float] NULL, -- số lượng giải thưởng
	[LUCKY_DRAW_MEISAI_ONSHOU_NUM] [int] NULL, -- Thứ tự giải, giải thưởng
	[LUCKY_DRAW_MEISAI_BUNRUI] [varchar](50) NOT NULL, --phân loại giải voucher,hoặc nhận ngay
	[LUCKY_DRAW_MEISAI_MUKO_FLG] [int] NULL, -- cờ vô hiệu hoá 0 còn 1 tắt
	[TOROKU_DATE] [datetime] NOT NULL, -- lưu ngày giờ user nhập
	[TOROKU_ID] [varchar](12) NOT NULL, -- mã user
	[KOSIN_NAIYOU] [nvarchar](500) NULL, -- sửa nội dung mô tả
	[KOSIN_DATE] [datetime] NULL, -- ngày user sửa
	[KOSIN_ID] [VARCHAR](12) NOT NULL, -- id của user sửa
	[KAISHI_DATE] [date] NULL, -- ngày bdau
	[SYURYOU_DATE] [date] NULL, -- ngay kthuc
 CONSTRAINT [PK_LUCKY_DRAW_MEISAI] PRIMARY KEY CLUSTERED 
(
	[LUCKY_DRAW_ID] ASC,
	[LUCKY_DRAW_MEISAI_NO] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
;

ALTER TABLE [dbo].[LUCKY_DRAW_MEISAI] ADD  CONSTRAINT [DF_LUCKY_DRAW_MEISAI_TOROKU_DATE]  DEFAULT (getdate()) FOR [TOROKU_DATE]
;


CREATE TABLE [dbo].[LUCKY_DRAW_PROGRAME]( -- ctrinh qthuong
	[LUCKY_DRAW_PROGRAME_ID] [VARCHAR](50) NOT NULL, -- mã lần quay thưởng 
	[LUCKY_DRAW_PROGRAME_NAME] [NVARCHAR](250) NULL, -- tên lần quay thưởng vd quay đợt 1 12-12-2025 
	[LUCKY_DRAW_ID] [VARCHAR](50) NOT NULL,  -- vị trí ở mã skien nào chọn
	[KAISHI_DATE] [DATE] NULL, -- ngày bắt đầu lấy từ meisai
	[SYURYOU_DATE] [DATE] NULL, -- ngày kết thúc lấy từ meisai
	[PROGRAME_SLOGAN] [NVARCHAR](250) NULL, -- slogan của chương trình banner
	[TOROKU_DATE] [DATETIME] NOT NULL, --- lưu ngày giờ user nhập 
	[TOROKU_ID] [VARCHAR](12) NOT NULL,  -- id của user nhập
	[KOSIN_NAIYOU] [NVARCHAR](500) NULL, ---nội dung sửa chữa
	[KOSIN_DATE] [DATETIME] NULL, -- ngày sửa
	[KOSIN_ID] [VARCHAR](12) NOT NULL, -- id của user sửa
 CONSTRAINT [PK_LUCKY_DRAW_PROGRAME] PRIMARY KEY CLUSTERED 
(
	[LUCKY_DRAW_PROGRAME_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
;

ALTER TABLE [dbo].[LUCKY_DRAW_PROGRAME] ADD  CONSTRAINT [DF_LUCKY_DRAW_PROGRAME_TOROKU_DATE]  DEFAULT (GETDATE()) FOR [TOROKU_DATE]
;

CREATE TABLE [dbo].[LUCKY_DRAW_PROGRAME_HISTORY]( -- lsu kqua qthuong pthuong
	[PROGRAME_JISSHI_ID] [decimal](18, 0) NOT NULL, -- id của lần thực hiện
	[LUCKY_DRAW_PROGRAME_ID] [varchar](50) NOT NULL, -- id của chương trình
	[LUCKY_DRAW_ID] [varchar](50) NOT NULL, -- mã sự kiện
	[MITUMORI_NO_SANSYO] [varchar](14) NULL, --tham chiếu tớihoá đơn
	[KOKAKU_HITO_NAME] [nvarchar](150) NULL, --tên khách hàng	
	[KOKAKU_HITO_PHONE] [varchar](50) NULL, -- số điện thoại khách hàng
	[KOKAKU_HITO_ADDRESS] [nvarchar](250) NULL, -- địa chỉ khách hàng
	[RATE_LOG_SPLIT] [varchar](2500) NULL, -- tỉ lệ lưu lại cấu hình thời điểm, chuỗi giải thưởng tránh trường sửa tỷ lệ
	[KENSYO_DRAW_MEISAI_ID] [varchar](50) NULL, -- lưu lại giải thường, liên kết bảng meisai
	[KENSYO_VOUCHER_CODE] [varchar](50) NULL, -- mã voucher nếu có
	[KENSYO_DRAW_MEISAI_SURYO] [float] NULL, --số lượng
	[KAISHI_DATE] [date] NULL, -- ngày bắt đầu lấy dữ liệu từ events xuống
	[SYURYOU_DATE] [date] NULL, -- ngày kết thúc lấy dữ liệu từ events xuống
	[TOROKU_DATE] [datetime] NOT NULL, -- lưu ngày giờ nhập
	[TOROKU_ID] [varchar](12) NOT NULL, -- id của user nhập 

	[JISSHI_KAISHI_DATE] [datetime] NULL, --thời điểm người dùng nhấn quay	
	[JISSHI_SYURYOU_DATE] [datetime] NULL, --thời điểm kết thúc vòng quay 
	[MAE_JISSHI_BG_IMG] [VARBINARY](MAX) NULL, --chụp màn hình trước khi bắt đầu nhấn nút quay	
	[ATO_JISSHI_BG_IMG] [VARBINARY](MAX) NULL, --chụp màn hình thời điểm trúng thưởng

	[KOKAKU_NAME_SYUTOKU] [nvarchar](150) NULL, -- tên khách hàng nhận (trường hợp nhận thay)
	[KOKAKU_PHONE_SYUTOKU] [varchar](50) NULL,  -- số điện thoại khách hàng
	[KOKAKU_SHOUMEISYO_NO_SYUTOKU] [varchar](50) NULL, -- số căn cước công dân
	[KOKAKU_ADDRESS_SYUTOKU] [varchar](50) NULL, -- địa chỉ khách hàng nhận
	[KOKAKU_SYUTOKU_DATE] [datetime] NULL, -- ngày giờ nhận
	[TANTO_SYA_NAME] [nvarchar](150) NULL, -- tên nhân viên phát giải thưởng
	[MITUMORI_NO_SYUTOKU_SANSYO] [varchar](14) NULL, -- số phiếu xuất kho trao thưởng
 CONSTRAINT [PK_LUCKY_DRAW_PROGRAME_HISTORY] PRIMARY KEY CLUSTERED 
(
	[PROGRAME_JISSHI_ID] ASC,
	[LUCKY_DRAW_PROGRAME_ID] ASC,
	[LUCKY_DRAW_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
;

ALTER TABLE [dbo].[LUCKY_DRAW_PROGRAME_HISTORY] ADD  CONSTRAINT [DF_LUCKY_DRAW_PROGRAME_HISTORY_TOROKU_DATE]  DEFAULT (getdate()) FOR [TOROKU_DATE]
;

CREATE TABLE [dbo].[LUCKY_DRAW_KOKAKU](
	[LUCKY_DRAW_ID] [VARCHAR](50) NOT NULL, -- id của sự kiện
	[KOKAKU_HITO_PHONE] [VARCHAR](20) NOT NULL, -- sdt khách hàng
	[KOKAKU_HITO_NAME] [NVARCHAR](50) NULL, -- tên khách hàng
	[KOKAKU_HITO_IMG] [VARBINARY](MAX) NULL, -- hình ảnh khách
	[KOKAKU_ADDRESS] [NVARCHAR](250) NULL, -- địa chỉ khách hàng
	[KOKAKU_SHOUMEISYO_NO] [VARCHAR](50) NULL, --cccd
	[KOKAKU_MUKO_FLG] [INT] NULL, -- cờ 0 hoặc 1 nếu khách không tham gia thì tích vào danh sách và where trong code để loại không load lên vòng quay
	[TOROKU_DATE] [DATETIME] NOT NULL, -- lưu ngày giờ 
	[TOROKU_ID] [VARCHAR](12) NOT NULL, -- id tạo
	[KOSIN_NAIYOU] [NVARCHAR](500) NULL, -- nội dung sửa
	[KOSIN_DATE] [DATETIME] NULL,-- ngày sửa
	[KOSIN_ID] [VARCHAR](12) NOT NULL, -- id của user sửa
	[KAISHI_DATE] [DATE] NULL, -- ngày bắt đầu
	[SYURYOU_DATE] [DATE] NULL, -- ngày kết thúc 
 CONSTRAINT [PK_LUCKY_DRAW_KOKAKU] PRIMARY KEY CLUSTERED 
(
	[LUCKY_DRAW_ID] ASC,
	[KOKAKU_HITO_PHONE] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
;

ALTER TABLE [dbo].[LUCKY_DRAW_KOKAKU] ADD  CONSTRAINT [DF_LUCKY_DRAW_KOKAKU_TOROKU_DATE]  DEFAULT (GETDATE()) FOR [TOROKU_DATE]
;
select * from [dbo].[LUCKY_DRAW_KOKAKU]
delete from [dbo].[LUCKY_DRAW_KOKAKU]
select * from [dbo].[LUCKY_DRAW_PROGRAME_HISTORY]
select * from dbo.LUCKY_DRAW_MEISAI