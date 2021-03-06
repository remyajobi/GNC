USE [GoogleNewsCrawler]
GO
/****** Object:  Table [dbo].[Beat]    Script Date: 10/26/2016 19:28:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Beat](
	[BeatIId] [int] IDENTITY(1,1) NOT NULL,
	[BeatTopic] [varchar](250) NOT NULL,
 CONSTRAINT [PK_Beats] PRIMARY KEY CLUSTERED 
(
	[BeatIId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[SubBeat]    Script Date: 10/26/2016 19:28:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[SubBeat](
	[SubBeatId] [int] IDENTITY(1,1) NOT NULL,
	[SubBeatTopic] [varchar](250) NOT NULL,
	[BeatId] [int] NOT NULL,
 CONSTRAINT [PK_SubBeat] PRIMARY KEY CLUSTERED 
(
	[SubBeatId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Journalist]    Script Date: 10/26/2016 19:28:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Journalist](
	[JournalistId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](250) NULL,
	[Designation] [varchar](250) NULL,
	[Tags] [varchar](250) NULL,
	[state] [varchar](80) NULL,
	[country] [varchar](80) NULL,
	[Email] [varchar](250) NOT NULL,
	[MediaLink] [varchar](max) NULL,
	[Website] [varchar](max) NULL,
	[Association] [varchar](max) NULL,
	[AboutInfo] [varchar](max) NULL,
 CONSTRAINT [PK_Journalist] PRIMARY KEY CLUSTERED 
(
	[JournalistId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Article]    Script Date: 10/26/2016 19:28:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Article](
	[ArticleId] [int] IDENTITY(1,1) NOT NULL,
	[PublicationDate] [date] NOT NULL,
	[ArticleTitle] [varchar](max) NOT NULL,
	[JournalistId] [int] NOT NULL,
	[SubBeatId] [int] NOT NULL,
 CONSTRAINT [PK_Article] PRIMARY KEY CLUSTERED 
(
	[ArticleId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO

/****** Object:  ForeignKey [FK_Article_Journalist]    Script Date: 10/26/2016 19:28:57 ******/
ALTER TABLE [dbo].[Article]  WITH CHECK ADD  CONSTRAINT [FK_Article_Journalist] FOREIGN KEY([JournalistId])
REFERENCES [dbo].[Journalist] ([JournalistId])
GO
ALTER TABLE [dbo].[Article] CHECK CONSTRAINT [FK_Article_Journalist]
GO
/****** Object:  ForeignKey [FK_Article_SubBeat]    Script Date: 10/26/2016 19:28:57 ******/
ALTER TABLE [dbo].[Article]  WITH CHECK ADD  CONSTRAINT [FK_Article_SubBeat] FOREIGN KEY([SubBeatId])
REFERENCES [dbo].[SubBeat] ([SubBeatId])
GO
ALTER TABLE [dbo].[Article] CHECK CONSTRAINT [FK_Article_SubBeat]
GO
/****** Object:  ForeignKey [FK_SubBeat_Beat]    Script Date: 10/26/2016 19:28:57 ******/
ALTER TABLE [dbo].[SubBeat]  WITH CHECK ADD  CONSTRAINT [FK_SubBeat_Beat] FOREIGN KEY([BeatId])
REFERENCES [dbo].[Beat] ([BeatIId])
GO
ALTER TABLE [dbo].[SubBeat] CHECK CONSTRAINT [FK_SubBeat_Beat]
GO

INSERT INTO [GoogleNewsCrawler].[dbo].[Beat]([BeatTopic]) VALUES ('Business')
GO
INSERT INTO [GoogleNewsCrawler].[dbo].[Beat]([BeatTopic]) VALUES ('Entertainment')
GO
INSERT INTO [GoogleNewsCrawler].[dbo].[Beat]([BeatTopic]) VALUES ('Sports')
GO
INSERT INTO [GoogleNewsCrawler].[dbo].[Beat]([BeatTopic]) VALUES ('Technology')
GO
INSERT INTO [GoogleNewsCrawler].[dbo].[Beat]([BeatTopic]) VALUES ('Science')
GO
INSERT INTO [GoogleNewsCrawler].[dbo].[Beat]([BeatTopic]) VALUES ('Health')
GO
INSERT INTO [GoogleNewsCrawler].[dbo].[Beat]([BeatTopic]) VALUES ('Society')
GO
INSERT INTO [GoogleNewsCrawler].[dbo].[Beat]([BeatTopic]) VALUES ('Politics')
GO
INSERT INTO [GoogleNewsCrawler].[dbo].[Beat]([BeatTopic]) VALUES ('World')
GO
INSERT INTO [GoogleNewsCrawler].[dbo].[Beat]([BeatTopic]) VALUES ('Local')

GO

CREATE UNIQUE NONCLUSTERED INDEX [Journalist_Email_Unique] ON [dbo].[Journalist] 
(
	[Email] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = ON, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


