USE [PitStop]
GO
/****** Object:  Table [dbo].[Benchmark]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Benchmark](
	[benchMarkId] [int] IDENTITY(1,1) NOT NULL,
	[BenchmarkText] [nvarchar](max) NULL,
	[templateId] [int] NULL,
 CONSTRAINT [PK_Benchmark] PRIMARY KEY CLUSTERED 
(
	[benchMarkId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EmployeeAchievments]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EmployeeAchievments](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[Date] [datetime] NOT NULL,
	[AchievmentTypeID] [bigint] NOT NULL,
	[Description] [nvarchar](max) NULL,
 CONSTRAINT [PK_EmployeeAchievments] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Escalation]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Escalation](
	[EscalationId] [int] IDENTITY(1,1) NOT NULL,
	[EmployeeName] [nvarchar](255) NOT NULL,
	[EmployeeId] [uniqueidentifier] NOT NULL,
	[JobTitle] [nvarchar](255) NOT NULL,
	[Department] [nvarchar](255) NOT NULL,
	[ManagerName] [nvarchar](255) NOT NULL,
	[ManagerId] [uniqueidentifier] NOT NULL,
	[InitialEscalationDate] [datetime] NULL,
	[EscalationStatusId] [int] NOT NULL,
	[ReasonsForPoorPerformance] [nvarchar](max) NULL,
	[MeasuresToImprovePerformance] [nvarchar](max) NULL,
	[EmployeeSignature] [nvarchar](max) NULL,
	[ManagerSignature] [nvarchar](max) NULL,
	[InitialEscalationDocument] [varbinary](max) NULL,
 CONSTRAINT [PK__Escalati__6C7956D08014D1FD] PRIMARY KEY CLUSTERED 
(
	[EscalationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EscalationExclusion_Teams]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EscalationExclusion_Teams](
	[TeamID] [int] IDENTITY(1,1) NOT NULL,
	[OperatorTeam] [nvarchar](255) NOT NULL,
	[Excluded] [bit] NOT NULL,
	[SourceSystem] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[TeamID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EscalationExclusion_Users]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EscalationExclusion_Users](
	[UserID] [nvarchar](255) NOT NULL,
	[DisplayName] [nvarchar](255) NOT NULL,
	[Excluded] [bit] NOT NULL,
 CONSTRAINT [PK__Escalati__1788CCAC34B34E9A] PRIMARY KEY CLUSTERED 
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EscalationFollowUp]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EscalationFollowUp](
	[FollowUpId] [int] IDENTITY(1,1) NOT NULL,
	[EscalationId] [int] NULL,
	[FollowUpWeek] [int] NOT NULL,
	[FollowUpDate] [datetime] NOT NULL,
	[KPIAchieved] [bit] NOT NULL,
	[Notes] [nvarchar](max) NULL,
	[FollowUpDocument] [varbinary](max) NULL,
	[ReasonsForPoorPerformance] [nvarchar](max) NULL,
	[MeasuresToImprovePerformance] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[FollowUpId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EscalationKPI]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EscalationKPI](
	[KPIId] [int] IDENTITY(1,1) NOT NULL,
	[EscalationId] [int] NULL,
	[Period] [nvarchar](50) NOT NULL,
	[KPIObjective] [nvarchar](max) NOT NULL,
	[EmployeeCurrentPerformance] [nvarchar](max) NOT NULL,
	[TargetKPI] [nvarchar](max) NOT NULL,
	[Deadline] [datetime] NOT NULL,
	[FollowUpId] [int] NULL,
	[AgreedUponTarget] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[KPIId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ExcludedChangeLog]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExcludedChangeLog](
	[LogID] [int] IDENTITY(1,1) NOT NULL,
	[EventDescription] [nvarchar](100) NULL,
	[ChangeDate] [datetime] NULL,
	[NewExcluded] [bit] NULL,
	[OppositeExcluded] [bit] NULL,
	[UserID] [nvarchar](50) NULL,
 CONSTRAINT [PK_ExcludedChangeLog] PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lookup_AchievementType]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lookup_AchievementType](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Value] [nvarchar](255) NOT NULL,
	[Active] [bit] NULL,
 CONSTRAINT [PK_Lookup_AchievementType] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lookup_EscalationStatus]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lookup_EscalationStatus](
	[EscalationStatusId] [int] IDENTITY(1,1) NOT NULL,
	[StatusName] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[EscalationStatusId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lookup_PitStopStatus]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lookup_PitStopStatus](
	[lookupStatusId] [int] IDENTITY(1,1) NOT NULL,
	[LookupStatusValue] [nvarchar](150) NULL,
 CONSTRAINT [PK_Lookup_PitStopStatus] PRIMARY KEY CLUSTERED 
(
	[lookupStatusId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lookup_TaskType]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lookup_TaskType](
	[TaskTypeID] [int] IDENTITY(1,1) NOT NULL,
	[TaskType] [varchar](150) NOT NULL,
 CONSTRAINT [PK_Lookup_TaskType] PRIMARY KEY CLUSTERED 
(
	[TaskTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Okt pitstops]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Okt pitstops](
	[EMPID] [nvarchar](50) NOT NULL,
	[Date] [date] NOT NULL,
	[AchievementType] [tinyint] NOT NULL,
	[Description_Optional] [nvarchar](1) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PitStopActions]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PitStopActions](
	[actionId] [int] IDENTITY(1,1) NOT NULL,
	[discussionId] [int] NULL,
	[ActionToBeTaken] [nvarchar](2000) NULL,
	[ByWhom] [nvarchar](150) NULL,
	[ByWhen] [datetime] NULL,
 CONSTRAINT [PK_PitStopActions] PRIMARY KEY CLUSTERED 
(
	[actionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PitStopDiscussion]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PitStopDiscussion](
	[discussionId] [int] IDENTITY(1,1) NOT NULL,
	[discussionOn] [datetime] NULL,
	[discussionWith] [nvarchar](150) NULL,
	[discussionMadeBy] [nvarchar](150) NULL,
	[Rating] [decimal](5, 2) NULL,
	[exceptionalPerformanceNote] [nvarchar](max) NULL,
	[needingOfPerformance] [nvarchar](max) NULL,
	[potensialIncreasedResponsibility] [nvarchar](max) NULL,
	[consequences] [nvarchar](max) NULL,
	[employeeFeedback] [nvarchar](max) NULL,
	[lineManagerFeedback] [nvarchar](max) NULL,
	[employeeSignature] [nvarchar](max) NULL,
	[employeeSignedOn] [datetime] NULL,
	[lineManagerSignature] [nvarchar](max) NULL,
	[lineManagerSignedOn] [datetime] NULL,
	[nextPerformanceReviewDate] [datetime] NULL,
	[lookupStatusId] [int] NULL,
	[templateId] [int] NULL,
 CONSTRAINT [PK_PitStopDiscussion] PRIMARY KEY CLUSTERED 
(
	[discussionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PitStopDiscussionDetail]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PitStopDiscussionDetail](
	[detailId] [int] IDENTITY(1,1) NOT NULL,
	[discussionId] [int] NULL,
	[questionId] [int] NULL,
	[Weighting] [decimal](6, 2) NULL,
	[Score] [decimal](3, 1) NULL,
	[WeightedScore] [decimal](6, 2) NULL,
	[EmployeePerformance] [nvarchar](max) NULL,
	[InputValue] [decimal](5, 2) NULL,
 CONSTRAINT [PK_PitStopDiscussionDetail] PRIMARY KEY CLUSTERED 
(
	[detailId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QuestionBenchmarkRules]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QuestionBenchmarkRules](
	[benchMarkRuleId] [int] IDENTITY(1,1) NOT NULL,
	[questionId] [int] NOT NULL,
	[incrementedValue] [decimal](3, 1) NOT NULL,
	[expressionValueStart] [int] NOT NULL,
	[expressionValueEnd] [int] NOT NULL,
 CONSTRAINT [PK_QuestionBenchmarkRules] PRIMARY KEY CLUSTERED 
(
	[benchMarkRuleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Questions]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Questions](
	[questionId] [int] IDENTITY(1,1) NOT NULL,
	[Objective] [nvarchar](200) NULL,
	[Measurement] [nvarchar](500) NULL,
	[Weighting] [decimal](5, 2) NULL,
	[IsActive] [bit] NOT NULL,
	[templateId] [int] NULL,
	[target] [int] NULL,
 CONSTRAINT [PK_Questions] PRIMARY KEY CLUSTERED 
(
	[questionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RatingScale]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RatingScale](
	[ratingScaleId] [int] IDENTITY(1,1) NOT NULL,
	[ratingDescription] [nvarchar](400) NULL,
	[ratingMetric] [nvarchar](400) NULL,
	[ratingValue] [int] NULL,
 CONSTRAINT [PK_RatingScale] PRIMARY KEY CLUSTERED 
(
	[ratingScaleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SharePointFiles]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SharePointFiles](
	[rowKeyId] [int] IDENTITY(1,1) NOT NULL,
	[discussionId] [int] NOT NULL,
	[SharepointFileID] [nvarchar](200) NOT NULL,
	[SharepointFileName] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_SharePointFiles] PRIMARY KEY CLUSTERED 
(
	[rowKeyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaskList]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaskList](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[DueDate] [datetime] NOT NULL,
	[UserId] [varchar](50) NOT NULL,
	[TemplateId] [int] NOT NULL,
	[Completed] [bit] NOT NULL,
	[CompletedDate] [datetime] NULL,
	[TaskTypeId] [int] NOT NULL,
	[CreatedBy] [varchar](50) NOT NULL,
	[PitStopDiscussionId] [int] NULL,
	[EscalationId] [int] NULL,
	[EscalationFollowUpId] [int] NULL,
	[IsDeleted] [bit] NOT NULL,
 CONSTRAINT [PK_TaskList] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaskListHistory]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaskListHistory](
	[TaskID] [int] NOT NULL,
	[DueDate] [datetime] NULL,
	[Reason] [nchar](500) NULL,
	[Date] [datetime] NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaskMeasure]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaskMeasure](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TaskId] [bigint] NOT NULL,
	[EscalationId] [int] NOT NULL,
	[MeasureDescription] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime] NULL,
	[IsCompleted] [bit] NULL,
	[CompletedDate] [datetime] NULL,
	[AssignedTo] [nvarchar](50) NOT NULL,
	[FollowUpEscalationId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Templates]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Templates](
	[templateId] [int] IDENTITY(1,1) NOT NULL,
	[teamplateName] [nvarchar](200) NULL,
	[teamplateDescription] [nvarchar](max) NULL,
	[templateCreatedOn] [datetime] NULL,
	[templateCreatedBy] [nvarchar](150) NULL,
	[actionsMandatory] [bit] NOT NULL,
	[signAtOwnDeskAvailable] [bit] NOT NULL,
 CONSTRAINT [PK_Templates] PRIMARY KEY CLUSTERED 
(
	[templateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[Escalation] ADD  CONSTRAINT [DF__Escalatio__Escal__73BA3083]  DEFAULT ((1)) FOR [EscalationStatusId]
GO
ALTER TABLE [dbo].[EscalationExclusion_Teams] ADD  DEFAULT ((0)) FOR [Excluded]
GO
ALTER TABLE [dbo].[EscalationExclusion_Users] ADD  CONSTRAINT [DF__Escalatio__Exclu__5224328E]  DEFAULT ((0)) FOR [Excluded]
GO
ALTER TABLE [dbo].[EscalationFollowUp] ADD  DEFAULT (getdate()) FOR [FollowUpDate]
GO
ALTER TABLE [dbo].[Questions] ADD  CONSTRAINT [DF_Questions_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[TaskList] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[TaskMeasure] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[TaskMeasure] ADD  DEFAULT ((0)) FOR [IsCompleted]
GO
ALTER TABLE [dbo].[TaskMeasure] ADD  DEFAULT ('Operator') FOR [AssignedTo]
GO
ALTER TABLE [dbo].[Templates] ADD  CONSTRAINT [DF_Templates_actionsMandatory_1]  DEFAULT ((0)) FOR [actionsMandatory]
GO
ALTER TABLE [dbo].[Templates] ADD  CONSTRAINT [DF_Templates_signAtOwnDeskAvailable_1]  DEFAULT ((0)) FOR [signAtOwnDeskAvailable]
GO
ALTER TABLE [dbo].[Benchmark]  WITH CHECK ADD  CONSTRAINT [FK_Benchmark_Templates] FOREIGN KEY([templateId])
REFERENCES [dbo].[Templates] ([templateId])
GO
ALTER TABLE [dbo].[Benchmark] CHECK CONSTRAINT [FK_Benchmark_Templates]
GO
ALTER TABLE [dbo].[EmployeeAchievments]  WITH CHECK ADD  CONSTRAINT [FK_EmployeeAchievments_Lookup_AchievementType] FOREIGN KEY([AchievmentTypeID])
REFERENCES [dbo].[Lookup_AchievementType] ([ID])
GO
ALTER TABLE [dbo].[EmployeeAchievments] CHECK CONSTRAINT [FK_EmployeeAchievments_Lookup_AchievementType]
GO
ALTER TABLE [dbo].[EscalationFollowUp]  WITH CHECK ADD  CONSTRAINT [FK__Escalatio__Escal__797309D9] FOREIGN KEY([EscalationId])
REFERENCES [dbo].[Escalation] ([EscalationId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[EscalationFollowUp] CHECK CONSTRAINT [FK__Escalatio__Escal__797309D9]
GO
ALTER TABLE [dbo].[EscalationKPI]  WITH CHECK ADD  CONSTRAINT [FK__Escalatio__Escal__76969D2E] FOREIGN KEY([EscalationId])
REFERENCES [dbo].[Escalation] ([EscalationId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[EscalationKPI] CHECK CONSTRAINT [FK__Escalatio__Escal__76969D2E]
GO
ALTER TABLE [dbo].[PitStopActions]  WITH CHECK ADD  CONSTRAINT [FK_PitStopActions_PitStopDiscussion] FOREIGN KEY([discussionId])
REFERENCES [dbo].[PitStopDiscussion] ([discussionId])
GO
ALTER TABLE [dbo].[PitStopActions] CHECK CONSTRAINT [FK_PitStopActions_PitStopDiscussion]
GO
ALTER TABLE [dbo].[PitStopDiscussion]  WITH CHECK ADD  CONSTRAINT [FK_PitStopDiscussion_Lookup_PitStopStatus] FOREIGN KEY([lookupStatusId])
REFERENCES [dbo].[Lookup_PitStopStatus] ([lookupStatusId])
GO
ALTER TABLE [dbo].[PitStopDiscussion] CHECK CONSTRAINT [FK_PitStopDiscussion_Lookup_PitStopStatus]
GO
ALTER TABLE [dbo].[PitStopDiscussionDetail]  WITH CHECK ADD  CONSTRAINT [FK_PitStopDiscussionDetail_PitStopDiscussion] FOREIGN KEY([discussionId])
REFERENCES [dbo].[PitStopDiscussion] ([discussionId])
GO
ALTER TABLE [dbo].[PitStopDiscussionDetail] CHECK CONSTRAINT [FK_PitStopDiscussionDetail_PitStopDiscussion]
GO
ALTER TABLE [dbo].[PitStopDiscussionDetail]  WITH CHECK ADD  CONSTRAINT [FK_PitStopDiscussionDetail_Questions] FOREIGN KEY([questionId])
REFERENCES [dbo].[Questions] ([questionId])
GO
ALTER TABLE [dbo].[PitStopDiscussionDetail] CHECK CONSTRAINT [FK_PitStopDiscussionDetail_Questions]
GO
ALTER TABLE [dbo].[QuestionBenchmarkRules]  WITH CHECK ADD  CONSTRAINT [FK_QuestionBenchmarkRules_Questions] FOREIGN KEY([questionId])
REFERENCES [dbo].[Questions] ([questionId])
GO
ALTER TABLE [dbo].[QuestionBenchmarkRules] CHECK CONSTRAINT [FK_QuestionBenchmarkRules_Questions]
GO
ALTER TABLE [dbo].[Questions]  WITH CHECK ADD  CONSTRAINT [FK_Questions_Templates] FOREIGN KEY([templateId])
REFERENCES [dbo].[Templates] ([templateId])
GO
ALTER TABLE [dbo].[Questions] CHECK CONSTRAINT [FK_Questions_Templates]
GO
ALTER TABLE [dbo].[SharePointFiles]  WITH CHECK ADD  CONSTRAINT [FK_SharePointFiles_PitStopDiscussion] FOREIGN KEY([discussionId])
REFERENCES [dbo].[PitStopDiscussion] ([discussionId])
GO
ALTER TABLE [dbo].[SharePointFiles] CHECK CONSTRAINT [FK_SharePointFiles_PitStopDiscussion]
GO
ALTER TABLE [dbo].[TaskMeasure]  WITH CHECK ADD  CONSTRAINT [FK_TaskMeasures_Escalation] FOREIGN KEY([EscalationId])
REFERENCES [dbo].[Escalation] ([EscalationId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TaskMeasure] CHECK CONSTRAINT [FK_TaskMeasures_Escalation]
GO
ALTER TABLE [dbo].[TaskMeasure]  WITH CHECK ADD  CONSTRAINT [FK_TaskMeasures_TaskList] FOREIGN KEY([TaskId])
REFERENCES [dbo].[TaskList] ([ID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TaskMeasure] CHECK CONSTRAINT [FK_TaskMeasures_TaskList]
GO
/****** Object:  StoredProcedure [dbo].[UpdateTeamAndUsersExcluded]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UpdateTeamAndUsersExcluded]
    @OperatorTeam  NVARCHAR(200),
    @Excluded      BIT,
    @updateUser    NVARCHAR(100),
    @UserIDs       NVARCHAR(MAX),
    @SourceSystem  NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        ---------------------------------------------------
        -- 1) Update the team
        ---------------------------------------------------
        UPDATE EscalationExclusion_Teams
        SET Excluded = @Excluded
        WHERE OperatorTeam = @OperatorTeam
          AND SourceSystem = @SourceSystem;

        -- Log the team change
        INSERT INTO ExcludedChangeLog (
            EventDescription, ChangeDate, NewExcluded, OppositeExcluded, UserID
        )
        VALUES (
            @OperatorTeam, GETDATE(), @Excluded,
            CASE WHEN @Excluded = 1 THEN 0 ELSE 1 END,
            @updateUser
        );

        ---------------------------------------------------
        -- 2) Prepare user list (trim + distinct)
        ---------------------------------------------------
        DECLARE @UserTable TABLE (UserID NVARCHAR(100) PRIMARY KEY);

        INSERT INTO @UserTable (UserID)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@UserIDs, ',')
        WHERE LTRIM(RTRIM(value)) <> '';

        ---------------------------------------------------
        -- 2b) Insert any missing users from AD (with name)
        --     If AD has no match, fallback to sensible display name
        ---------------------------------------------------
        INSERT INTO EscalationExclusion_Users (UserID, DisplayName, Excluded)
        SELECT
            u.UserID,
            COALESCE(
                NULLIF(ad.DisplayName, ''),
                LTRIM(RTRIM(
                    CONCAT(
                        COALESCE(NULLIF(ad.GivenName, ''), ''),
                        CASE WHEN NULLIF(ad.GivenName, '') IS NOT NULL AND NULLIF(ad.Surname, '') IS NOT NULL THEN ' ' ELSE '' END,
                        COALESCE(NULLIF(ad.Surname, ''), '')
                    )
                )),
                NULLIF(ad.UserPrincipalName, ''),
                u.UserID
            ) AS DisplayName,
            @Excluded
        FROM @UserTable u
        LEFT JOIN [ADUsers].[dbo].[ADUser] ad
       ON CONVERT(NVARCHAR(100), ad.ADUserID) = u.UserID
        LEFT JOIN EscalationExclusion_Users e
               ON e.UserID = u.UserID
        WHERE e.UserID IS NULL; -- only add new rows

        ---------------------------------------------------
        -- 3) Update users (existing + just-inserted)
        ---------------------------------------------------
        UPDATE u
        SET u.Excluded = @Excluded
        FROM EscalationExclusion_Users u
        INNER JOIN @UserTable t ON u.UserID = t.UserID;

        ---------------------------------------------------
        -- 4) Log each user change (covers all in @UserTable)
        ---------------------------------------------------
        INSERT INTO ExcludedChangeLog (
            EventDescription, ChangeDate, NewExcluded, OppositeExcluded, UserID
        )
        SELECT 
            CONCAT('User ', t.UserID, ' change'),
            GETDATE(),
            @Excluded,
            CASE WHEN @Excluded = 1 THEN 0 ELSE 1 END,
            @updateUser
        FROM @UserTable t;

        COMMIT TRANSACTION;

        ---------------------------------------------------
        -- 5) Return updated data
        ---------------------------------------------------
        SELECT 
            'Team updated' AS Action,
            TeamID,
            OperatorTeam,
            Excluded
        FROM EscalationExclusion_Teams
        WHERE OperatorTeam = @OperatorTeam
          AND SourceSystem = @SourceSystem;  -- keep return aligned

        SELECT 
            'Users updated' AS Action,
            u.UserID,
            u.DisplayName,
            u.Excluded
        FROM EscalationExclusion_Users u
        WHERE u.UserID IN (SELECT UserID FROM @UserTable);

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT 
            ERROR_NUMBER()  AS ErrorNumber,
            ERROR_MESSAGE() AS ErrorMessage;
    END CATCH
END;
GO
/****** Object:  StoredProcedure [dbo].[UpdateUserExcluded]    Script Date: 2026/01/21 08:43:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UpdateUserExcluded]
    @UserID NVARCHAR(100),
    @updateUser NVARCHAR(100),
    @Excluded BIT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate inputs
        IF @UserID IS NULL OR LTRIM(RTRIM(@UserID)) = ''
            THROW 50001, 'UserID must be provided.', 1;

        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM EscalationExclusion_Users WHERE UserID = @UserID)
            THROW 50003, 'User not found.', 1;

        BEGIN TRANSACTION;

        -- Update the user's excluded status
        UPDATE EscalationExclusion_Users
        SET Excluded = @Excluded
        WHERE UserID = @UserID;

        -- Log the change
        INSERT INTO ExcludedChangeLog (
            EventDescription,
            ChangeDate,
            NewExcluded,
            OppositeExcluded,
            UserID
        )
        VALUES (
            CONCAT(@UserID, ' change'),
            GETDATE(),
            @Excluded,
            CASE WHEN @Excluded = 1 THEN 0 ELSE 1 END,
            @updateUser
        );

        COMMIT TRANSACTION;

        -- Return the updated record
        SELECT 
            UserID,
            DisplayName,
            Excluded
        FROM EscalationExclusion_Users
        WHERE UserID = @UserID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT 
            ERROR_NUMBER() AS ErrorNumber,
            ERROR_MESSAGE() AS ErrorMessage;
    END CATCH
END;
GO
