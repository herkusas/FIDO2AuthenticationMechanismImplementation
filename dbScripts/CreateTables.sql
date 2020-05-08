USE [FIDO]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Credentials](
	[UserId] [nvarchar](150) NOT NULL,
	[Id] [nvarchar](150) NOT NULL,
	[PublicKeyCredentialType] [nvarchar](512) NOT NULL,
	[PublicKey] [nvarchar](max) NOT NULL,
	[UserHandle] [nvarchar](max) NOT NULL,
	[SignatureCounter] [int] NOT NULL,
	[CredentialType] [nvarchar](150) NOT NULL,
	[RegistrationDate] [datetime] NOT NULL,
	[AAGuid] [uniqueidentifier] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [nvarchar](50) NULL,
	[Name] [nvarchar](512) NULL,
	[DisplayName] [nvarchar](512) NULL
) ON [PRIMARY]
GO
