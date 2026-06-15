IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610165518_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260610165518_InitialCreate', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610170549_AddSearchNormalizedName'
)
BEGIN
    ALTER TABLE [PRODUCTS] ADD [SearchNormalizedName] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610170549_AddSearchNormalizedName'
)
BEGIN
    CREATE INDEX [IX_Products_SearchNormalizedName] ON [PRODUCTS] ([SearchNormalizedName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610170549_AddSearchNormalizedName'
)
BEGIN
    UPDATE PRODUCTS SET SearchNormalizedName = LOWER(ProductName);
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'á', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'à', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ạ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ả', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ã', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'â', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ấ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ầ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ậ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ẩ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ẫ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ă', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ắ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ằ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ặ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ẳ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ẵ', N'a');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Á', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'À', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ạ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ả', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ã', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Â', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ấ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ầ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ậ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ẩ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ẫ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ă', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ắ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ằ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ặ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ẳ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ẵ', N'A');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'é', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'è', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ẹ', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ẻ', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ẽ', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ê', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ế', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ề', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ệ', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ể', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ễ', N'e');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'É', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'È', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ẹ', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ẻ', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ẽ', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ê', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ế', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ề', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ệ', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ể', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ễ', N'E');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ó', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ò', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ọ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ỏ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'õ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ô', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ố', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ồ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ộ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ổ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ỗ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ơ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ớ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ờ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ợ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ở', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ỡ', N'o');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ó', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ò', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ọ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ỏ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Õ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ô', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ố', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ồ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ộ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ổ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ỗ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ơ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ớ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ờ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ợ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ở', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ỡ', N'O');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ú', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ù', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ụ', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ủ', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ũ', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ư', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ứ', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ừ', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ự', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ử', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ữ', N'u');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ú', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ù', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ụ', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ủ', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ũ', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ư', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ứ', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ừ', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ự', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ử', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ữ', N'U');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'í', N'i');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ì', N'i');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ị', N'i');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ỉ', N'i');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ĩ', N'i');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Í', N'I');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ì', N'I');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ị', N'I');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ỉ', N'I');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ĩ', N'I');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'đ', N'd');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Đ', N'D');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ý', N'y');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ỳ', N'y');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ỵ', N'y');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ỷ', N'y');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'ỹ', N'y');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ý', N'Y');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ỳ', N'Y');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ỵ', N'Y');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ỷ', N'Y');
    UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'Ỹ', N'Y');

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610170549_AddSearchNormalizedName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260610170549_AddSearchNormalizedName', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614134530_ShipperUATFeatures'
)
BEGIN
    ALTER TABLE [ORDERSTATUSHISTORY] ADD [ActionType] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614134530_ShipperUATFeatures'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [AssignedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614134530_ShipperUATFeatures'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [AssignedShipperId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614134530_ShipperUATFeatures'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [DeliveredAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614134530_ShipperUATFeatures'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [DeliveryFailureReason] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614134530_ShipperUATFeatures'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [DeliveryFailureReasonCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614134530_ShipperUATFeatures'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [ShippingStartedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614134530_ShipperUATFeatures'
)
BEGIN
    CREATE INDEX [IX_ORDERS_AssignedShipperId] ON [ORDERS] ([AssignedShipperId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614134530_ShipperUATFeatures'
)
BEGIN
    ALTER TABLE [ORDERS] ADD CONSTRAINT [FK_ORDERS_ACCOUNTS_AssignedShipperId] FOREIGN KEY ([AssignedShipperId]) REFERENCES [ACCOUNTS] ([UserId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614134530_ShipperUATFeatures'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614134530_ShipperUATFeatures', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614154550_UniqueEmailConstraint'
)
BEGIN

    IF EXISTS (SELECT * FROM sys.objects WHERE name = 'UQ__ACCOUNTS__A9D105344376EEBF' AND type = 'UQ')
    BEGIN
        ALTER TABLE ACCOUNTS DROP CONSTRAINT UQ__ACCOUNTS__A9D105344376EEBF
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614154550_UniqueEmailConstraint'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ACCOUNTS]') AND [c].[name] = N'Email');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [ACCOUNTS] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [ACCOUNTS] ALTER COLUMN [Email] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614154550_UniqueEmailConstraint'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ACCOUNTS_Email] ON [ACCOUNTS] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614154550_UniqueEmailConstraint'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614154550_UniqueEmailConstraint', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614160016_AddAccountRowVersion'
)
BEGIN
    ALTER TABLE [ACCOUNTS] ADD [RowVersion] rowversion NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614160016_AddAccountRowVersion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614160016_AddAccountRowVersion', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614174513_AddDeliveryAttemptInfo'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [DeliveryAttemptCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614174513_AddDeliveryAttemptInfo'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [DeliveryRescheduleReason] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614174513_AddDeliveryAttemptInfo'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [LastDeliveryAttemptAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614174513_AddDeliveryAttemptInfo'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [NextDeliveryDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614174513_AddDeliveryAttemptInfo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614174513_AddDeliveryAttemptInfo', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615003531_AddDiscountProgramAudit'
)
BEGIN
    ALTER TABLE [DISCOUNTPROGRAMS] ADD [RowVersion] rowversion NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615003531_AddDiscountProgramAudit'
)
BEGIN
    CREATE TABLE [DISCOUNTPROGRAMAUDITS] (
        [AuditID] int NOT NULL IDENTITY,
        [ProgramID] int NOT NULL,
        [ActionType] nvarchar(max) NOT NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [ChangedByUserId] int NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [DiscountProgramProgramID] int NOT NULL,
        [ChangedByAccountUserId] int NOT NULL,
        CONSTRAINT [PK_DISCOUNTPROGRAMAUDITS] PRIMARY KEY ([AuditID]),
        CONSTRAINT [FK_DISCOUNTPROGRAMAUDITS_ACCOUNTS_ChangedByAccountUserId] FOREIGN KEY ([ChangedByAccountUserId]) REFERENCES [ACCOUNTS] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DISCOUNTPROGRAMAUDITS_DISCOUNTPROGRAMS_DiscountProgramProgramID] FOREIGN KEY ([DiscountProgramProgramID]) REFERENCES [DISCOUNTPROGRAMS] ([ProgramID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615003531_AddDiscountProgramAudit'
)
BEGIN
    CREATE INDEX [IX_DISCOUNTPROGRAMAUDITS_ChangedByAccountUserId] ON [DISCOUNTPROGRAMAUDITS] ([ChangedByAccountUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615003531_AddDiscountProgramAudit'
)
BEGIN
    CREATE INDEX [IX_DISCOUNTPROGRAMAUDITS_DiscountProgramProgramID] ON [DISCOUNTPROGRAMAUDITS] ([DiscountProgramProgramID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615003531_AddDiscountProgramAudit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615003531_AddDiscountProgramAudit', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    ALTER TABLE [SHIPPINGADDRESSES] ADD [DistrictId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    ALTER TABLE [SHIPPINGADDRESSES] ADD [Note] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    ALTER TABLE [SHIPPINGADDRESSES] ADD [ProvinceId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    ALTER TABLE [SHIPPINGADDRESSES] ADD [WardId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [DeliveryNote] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [ShippingFullAddress] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    CREATE TABLE [PROVINCES] (
        [ProvinceId] int NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_PROVINCES] PRIMARY KEY ([ProvinceId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    CREATE TABLE [DISTRICTS] (
        [DistrictId] int NOT NULL,
        [ProvinceId] int NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_DISTRICTS] PRIMARY KEY ([DistrictId]),
        CONSTRAINT [FK_DISTRICTS_PROVINCES_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [PROVINCES] ([ProvinceId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    CREATE TABLE [WARDS] (
        [WardId] int NOT NULL,
        [DistrictId] int NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_WARDS] PRIMARY KEY ([WardId]),
        CONSTRAINT [FK_WARDS_DISTRICTS_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [DISTRICTS] ([DistrictId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    CREATE INDEX [IX_SHIPPINGADDRESSES_DistrictId] ON [SHIPPINGADDRESSES] ([DistrictId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    CREATE INDEX [IX_SHIPPINGADDRESSES_ProvinceId] ON [SHIPPINGADDRESSES] ([ProvinceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    CREATE INDEX [IX_SHIPPINGADDRESSES_WardId] ON [SHIPPINGADDRESSES] ([WardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    CREATE INDEX [IX_DISTRICTS_ProvinceId] ON [DISTRICTS] ([ProvinceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    CREATE INDEX [IX_WARDS_DistrictId] ON [WARDS] ([DistrictId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    ALTER TABLE [SHIPPINGADDRESSES] ADD CONSTRAINT [FK_SHIPPINGADDRESSES_DISTRICTS_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [DISTRICTS] ([DistrictId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    ALTER TABLE [SHIPPINGADDRESSES] ADD CONSTRAINT [FK_SHIPPINGADDRESSES_PROVINCES_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [PROVINCES] ([ProvinceId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    ALTER TABLE [SHIPPINGADDRESSES] ADD CONSTRAINT [FK_SHIPPINGADDRESSES_WARDS_WardId] FOREIGN KEY ([WardId]) REFERENCES [WARDS] ([WardId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615035933_AddAdministrativeBoundaries'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615035933_AddAdministrativeBoundaries', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615063445_AddAddressEnhancements'
)
BEGIN
    ALTER TABLE [SHIPPINGADDRESSES] ADD [AddressName] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615063445_AddAddressEnhancements'
)
BEGIN
    ALTER TABLE [SHIPPINGADDRESSES] ADD [Latitude] numeric(9,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615063445_AddAddressEnhancements'
)
BEGIN
    ALTER TABLE [SHIPPINGADDRESSES] ADD [Longitude] numeric(9,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615063445_AddAddressEnhancements'
)
BEGIN
    ALTER TABLE [ORDERS] ADD [ShippingAddressId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615063445_AddAddressEnhancements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615063445_AddAddressEnhancements', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615064033_AddOrderIndexesForRecommendation'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId_OrderDate] ON [ORDERS] ([CustomerId], [OrderDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615064033_AddOrderIndexesForRecommendation'
)
BEGIN
    CREATE INDEX [IX_Orders_ShippingAddressId] ON [ORDERS] ([ShippingAddressId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615064033_AddOrderIndexesForRecommendation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615064033_AddOrderIndexesForRecommendation', N'9.0.6');
END;

COMMIT;
GO

