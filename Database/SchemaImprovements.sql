-- ============================================================
-- SCHEMA IMPROVEMENTS — WEB_CUAHANGQUANAO
-- Run these in order on existing database
-- ============================================================
USE WEB_CUAHANGQUANAO;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================
-- [DB-01] ORDERDETAILS — Add snapshot columns (CRITICAL)
-- Prevents losing order history when products/variants change
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ORDERDETAILS') AND name = 'ProductNameSnapshot')
BEGIN
    ALTER TABLE ORDERDETAILS ADD
        ProductNameSnapshot NVARCHAR(200) NOT NULL DEFAULT '',
        SizeCodeSnapshot    VARCHAR(10)   NOT NULL DEFAULT '',
        ColorNameSnapshot   NVARCHAR(50)  NOT NULL DEFAULT '',
        SKUSnapshot         VARCHAR(50)   NOT NULL DEFAULT '';
    PRINT '[DB-01] ORDERDETAILS snapshot columns added.';
END
GO

-- ============================================================
-- [DB-02] SIZES — Add SortOrder for correct display ordering
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SIZES') AND name = 'SortOrder')
BEGIN
    ALTER TABLE SIZES ADD SortOrder INT NOT NULL DEFAULT 99;
    PRINT '[DB-02] SIZES.SortOrder added.';
END
GO

-- ============================================================
-- [DB-03] ACCOUNTS — Add audit and login tracking fields
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ACCOUNTS') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE ACCOUNTS ADD
        CreatedAt   DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt   DATETIME2 NOT NULL DEFAULT GETDATE(),
        LastLoginAt DATETIME2 NULL;
    PRINT '[DB-03] ACCOUNTS audit fields added.';
END
GO

-- ============================================================
-- [DB-04] COUPONS — Add UsedCount for fast usage limit check
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('COUPONS') AND name = 'UsedCount')
BEGIN
    ALTER TABLE COUPONS ADD UsedCount INT NOT NULL DEFAULT 0;
END
GO

-- Backfill existing UsedCount from COUPONUSAGES
UPDATE c SET c.UsedCount = cu.Cnt
FROM COUPONS c
INNER JOIN (
    SELECT CouponID, COUNT(*) AS Cnt FROM COUPONUSAGES GROUP BY CouponID
) cu ON c.CouponID = cu.CouponID;

PRINT '[DB-04] COUPONS.UsedCount added and backfilled.';
GO

-- ============================================================
-- [DB-05] CHECK CONSTRAINTS
-- ============================================================

-- DISCOUNTPROGRAMS: EndDate >= StartDate
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DiscountProgram_Dates')
BEGIN
    ALTER TABLE DISCOUNTPROGRAMS ADD CONSTRAINT CK_DiscountProgram_Dates
        CHECK (EndDate >= StartDate);
    PRINT '[DB-05a] DISCOUNTPROGRAMS date check added.';
END
GO

-- COUPONS: ValidTo > ValidFrom
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Coupon_Dates')
BEGIN
    ALTER TABLE COUPONS ADD CONSTRAINT CK_Coupon_Dates
        CHECK (ValidTo > ValidFrom);
    PRINT '[DB-05b] COUPONS date check added.';
END
GO

-- COUPONS: DiscountType must be known value
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Coupon_DiscountType')
BEGIN
    ALTER TABLE COUPONS ADD CONSTRAINT CK_Coupon_DiscountType
        CHECK (DiscountType IN ('Percentage', 'FixedAmount'));
    PRINT '[DB-05c] COUPONS DiscountType check added.';
END
GO

-- ORDERS: PaymentStatus allowed values
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Order_PaymentStatus')
BEGIN
    ALTER TABLE ORDERS ADD CONSTRAINT CK_Order_PaymentStatus
        CHECK (PaymentStatus IN ('Unpaid', 'Paid', 'Refunded', 'PartialRefund'));
    PRINT '[DB-05d] ORDERS PaymentStatus check added.';
END
GO

-- ORDERS: PaymentMethod allowed values
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Order_PaymentMethod')
BEGIN
    ALTER TABLE ORDERS ADD CONSTRAINT CK_Order_PaymentMethod
        CHECK (PaymentMethod IN ('COD', 'Online', 'BankTransfer'));
    PRINT '[DB-05e] ORDERS PaymentMethod check added.';
END
GO

-- ORDERS: OrderStatus allowed values
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Order_OrderStatus')
BEGIN
    ALTER TABLE ORDERS ADD CONSTRAINT CK_Order_OrderStatus
        CHECK (OrderStatus IN ('Pending', 'Confirmed', 'Processing', 'Shipping', 'Delivered', 'Cancelled'));
    PRINT '[DB-05f] ORDERS OrderStatus check added.';
END
GO

-- INVENTORYTRANSACTIONS: TransactionType allowed values
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_InvTx_Type')
BEGIN
    ALTER TABLE INVENTORYTRANSACTIONS ADD CONSTRAINT CK_InvTx_Type
        CHECK (TransactionType IN ('Sale', 'Receipt', 'Return', 'Adjustment', 'Issue'));
    PRINT '[DB-05g] INVENTORYTRANSACTIONS type check added.';
END
GO

-- MEMBERSHIPS: DiscountPercent between 0-100
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Membership_Discount')
BEGIN
    ALTER TABLE MEMBERSHIPS ADD CONSTRAINT CK_Membership_Discount
        CHECK (DiscountPercent BETWEEN 0 AND 100);
    PRINT '[DB-05h] MEMBERSHIPS DiscountPercent check added.';
END
GO

-- ============================================================
-- [DB-06] MISSING INDEXES
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_OrderStatus' AND object_id = OBJECT_ID('ORDERS'))
    CREATE INDEX IX_Orders_OrderStatus ON ORDERS(OrderStatus, OrderDate DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_CustomerId' AND object_id = OBJECT_ID('ORDERS'))
    CREATE INDEX IX_Orders_CustomerId ON ORDERS(CustomerId, OrderDate DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Gender' AND object_id = OBJECT_ID('PRODUCTS'))
    CREATE INDEX IX_Products_Gender ON PRODUCTS(Gender) WHERE IsActive = 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductVariants_ColorId' AND object_id = OBJECT_ID('PRODUCTVARIANTS'))
    CREATE INDEX IX_ProductVariants_ColorId ON PRODUCTVARIANTS(ColorID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductVariants_SizeId' AND object_id = OBJECT_ID('PRODUCTVARIANTS'))
    CREATE INDEX IX_ProductVariants_SizeId ON PRODUCTVARIANTS(SizeID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CouponUsages_CouponId' AND object_id = OBJECT_ID('COUPONUSAGES'))
    CREATE INDEX IX_CouponUsages_CouponId ON COUPONUSAGES(CouponID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderDetails_VariantId' AND object_id = OBJECT_ID('ORDERDETAILS'))
    CREATE INDEX IX_OrderDetails_VariantId ON ORDERDETAILS(VariantID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryTx_VariantId' AND object_id = OBJECT_ID('INVENTORYTRANSACTIONS'))
    CREATE INDEX IX_InventoryTx_VariantId ON INVENTORYTRANSACTIONS(VariantID, TransactionType);
GO

PRINT '[DB-06] Indexes created.';
GO

-- ============================================================
-- [DB-07] TRIGGERS
-- ============================================================

-- TR-01: Auto-update PRODUCTS.UpdatedAt
CREATE OR ALTER TRIGGER TR_Products_UpdatedAt
ON PRODUCTS AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PRODUCTS SET UpdatedAt = GETDATE()
    WHERE ProductID IN (SELECT ProductID FROM inserted);
END;
GO

-- TR-02: Auto-update PRODUCTVARIANTS.UpdatedAt
CREATE OR ALTER TRIGGER TR_ProductVariants_UpdatedAt
ON PRODUCTVARIANTS AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PRODUCTVARIANTS SET UpdatedAt = GETDATE()
    WHERE VariantID IN (SELECT VariantID FROM inserted);
END;
GO

-- TR-03: Auto-update COUPONS.UpdatedAt
CREATE OR ALTER TRIGGER TR_Coupons_UpdatedAt
ON COUPONS AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE COUPONS SET UpdatedAt = GETDATE()
    WHERE CouponID IN (SELECT CouponID FROM inserted);
END;
GO

-- TR-04: Auto-update CARTS.UpdatedAt
CREATE OR ALTER TRIGGER TR_Carts_UpdatedAt
ON CARTS AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CARTS SET UpdatedAt = GETDATE()
    WHERE CartID IN (SELECT CartID FROM inserted);
END;
GO

-- TR-05: Auto-update ACCOUNTS.UpdatedAt
CREATE OR ALTER TRIGGER TR_Accounts_UpdatedAt
ON ACCOUNTS AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- Don't update UpdatedAt when only LastLoginAt changes
    IF UPDATE(UserName) OR UPDATE(Email) OR UPDATE(PasswordHash) OR UPDATE(Status) OR UPDATE(CustomerId)
    BEGIN
        UPDATE ACCOUNTS SET UpdatedAt = GETDATE()
        WHERE UserId IN (SELECT UserId FROM inserted);
    END
END;
GO

-- TR-06: Auto-increment CUSTOMERS.TotalOrders when order created
CREATE OR ALTER TRIGGER TR_Orders_IncrementTotalOrders
ON ORDERS AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CUSTOMERS
    SET TotalOrders = TotalOrders + 1,
        UpdatedAt   = GETDATE()
    WHERE CustomerId IN (
        SELECT DISTINCT CustomerId FROM inserted WHERE CustomerId IS NOT NULL
    );
END;
GO

-- TR-07: Auto-increment COUPONS.UsedCount when CouponUsage inserted
CREATE OR ALTER TRIGGER TR_CouponUsages_IncrUsedCount
ON COUPONUSAGES AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE COUPONS
    SET UsedCount = UsedCount + 1,
        UpdatedAt = GETDATE()
    WHERE CouponID IN (SELECT DISTINCT CouponID FROM inserted);
END;
GO

-- TR-08: Auto-upgrade CUSTOMERS.MembershipID based on RewardPoints
CREATE OR ALTER TRIGGER TR_Customers_UpgradeMembership
ON CUSTOMERS AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(RewardPoints)
    BEGIN
        UPDATE c
        SET c.MembershipID = m.MembershipID,
            c.UpdatedAt    = GETDATE()
        FROM CUSTOMERS c
        INNER JOIN inserted i ON c.CustomerId = i.CustomerId
        INNER JOIN MEMBERSHIPS m ON i.RewardPoints >= m.MinPoint
            AND (m.MaxPoint IS NULL OR i.RewardPoints <= m.MaxPoint)
        WHERE m.MembershipID != c.MembershipID;
    END
END;
GO

-- TR-09: Validate DISCOUNTPROGRAMS EndDate >= StartDate
CREATE OR ALTER TRIGGER TR_DiscountPrograms_ValidateDates
ON DISCOUNTPROGRAMS AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted WHERE EndDate < StartDate)
    BEGIN
        RAISERROR('DISCOUNTPROGRAMS: EndDate must be >= StartDate.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- TR-10: Validate COUPONS ValidTo > ValidFrom
CREATE OR ALTER TRIGGER TR_Coupons_ValidateDates
ON COUPONS AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted WHERE ValidTo <= ValidFrom)
    BEGIN
        RAISERROR('COUPONS: ValidTo must be after ValidFrom.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- ============================================================
-- [DB-08] PRODUCTIMAGES — Add IsMain flag
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PRODUCTIMAGES') AND name = 'IsMain')
BEGIN
    ALTER TABLE PRODUCTIMAGES ADD IsMain BIT NOT NULL DEFAULT 0;
    PRINT '[DB-08] PRODUCTIMAGES.IsMain added.';
END
GO

-- ============================================================
-- STORED PROCEDURES
-- ============================================================

-- SP-01: Validate and apply coupon
CREATE OR ALTER PROCEDURE sp_ValidateCoupon
    @CouponCode   VARCHAR(50),
    @OrderAmount  NUMERIC(18,2),
    @CustomerId   INT = NULL,
    @CouponID     INT OUTPUT,
    @DiscountAmt  NUMERIC(18,2) OUTPUT,
    @ErrorMessage NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @DiscountAmt  = 0;
    SET @ErrorMessage = NULL;
    SET @CouponID     = 0;

    DECLARE @c TABLE (
        CouponID      INT,
        DiscountType  NVARCHAR(20),
        DiscountValue NUMERIC(18,2),
        MinOrderValue NUMERIC(18,2),
        UsageLimit    INT,
        UsedCount     INT,
        ValidFrom     DATETIME2,
        ValidTo       DATETIME2,
        IsActive      BIT
    );

    INSERT INTO @c
    SELECT CouponID, DiscountType, DiscountValue, MinOrderValue,
           UsageLimit, UsedCount, ValidFrom, ValidTo, IsActive
    FROM COUPONS WHERE CouponCode = @CouponCode;

    IF NOT EXISTS (SELECT 1 FROM @c)
    BEGIN
        SET @ErrorMessage = N'Mã giảm giá không tồn tại.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM @c WHERE IsActive = 0)
    BEGIN
        SET @ErrorMessage = N'Mã giảm giá đã bị vô hiệu hóa.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM @c WHERE GETDATE() < ValidFrom)
    BEGIN
        SET @ErrorMessage = N'Mã giảm giá chưa có hiệu lực.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM @c WHERE GETDATE() > ValidTo)
    BEGIN
        SET @ErrorMessage = N'Mã giảm giá đã hết hạn.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM @c WHERE @OrderAmount < MinOrderValue)
    BEGIN
        SET @ErrorMessage = N'Đơn hàng chưa đạt giá trị tối thiểu để dùng mã này.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM @c WHERE UsageLimit IS NOT NULL AND UsedCount >= UsageLimit)
    BEGIN
        SET @ErrorMessage = N'Mã giảm giá đã hết lượt sử dụng.';
        RETURN;
    END

    -- Calculate discount
    SELECT @CouponID = CouponID,
           @DiscountAmt = CASE
               WHEN DiscountType = 'Percentage'
                   THEN @OrderAmount * DiscountValue / 100.0
               ELSE DiscountValue
           END
    FROM @c;

    -- Cap percentage discount at order amount
    IF @DiscountAmt > @OrderAmount SET @DiscountAmt = @OrderAmount;
END;
GO

-- SP-02: Get effective price for a variant (with discount program)
CREATE OR ALTER PROCEDURE sp_GetEffectivePrice
    @VariantID    INT,
    @EffectivePrice NUMERIC(18,2) OUTPUT,
    @OriginalPrice  NUMERIC(18,2) OUTPUT,
    @DiscountPct    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @DiscountPct = 0;

    SELECT
        @OriginalPrice = pv.SellingPrice,
        @DiscountPct   = ISNULL(dp.DiscountPercent, 0),
        @EffectivePrice = CASE
            WHEN dp.ProgramID IS NOT NULL AND dp.IsActive = 1
                 AND dp.StartDate <= CAST(GETDATE() AS DATE)
                 AND dp.EndDate   >= CAST(GETDATE() AS DATE)
            THEN CAST(pv.SellingPrice * (1 - dp.DiscountPercent / 100.0) AS NUMERIC(18,2))
            ELSE pv.SellingPrice
        END
    FROM PRODUCTVARIANTS pv
    INNER JOIN PRODUCTS p ON pv.ProductID = p.ProductID
    LEFT JOIN DISCOUNTPROGRAMS dp ON p.ProgramID = dp.ProgramID
    WHERE pv.VariantID = @VariantID;
END;
GO

PRINT 'Schema improvements completed successfully.';
GO
