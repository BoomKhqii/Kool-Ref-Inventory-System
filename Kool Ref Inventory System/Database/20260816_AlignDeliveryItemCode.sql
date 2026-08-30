SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS
(
    SELECT 1
    FROM sys.columns AS columnInfo
    INNER JOIN sys.types AS typeInfo
        ON columnInfo.user_type_id = typeInfo.user_type_id
    WHERE columnInfo.object_id = OBJECT_ID(N'dbo.DeliveryProcessedItem')
      AND columnInfo.name = N'itemId'
      AND typeInfo.name <> N'nvarchar'
)
BEGIN
    ALTER TABLE dbo.DeliveryProcessedItem
        ALTER COLUMN itemId nvarchar(50) NOT NULL;
END;

-- ItemList currently has legacy data where a code may be duplicated. Once those
-- duplicates are corrected, rerunning this migration will enforce unique item
-- codes and add the product foreign key automatically.
IF NOT EXISTS
(
    SELECT itemId
    FROM dbo.ItemList
    GROUP BY itemId
    HAVING COUNT(*) > 1
)
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ItemList')
          AND name = N'UX_ItemList_ItemId'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_ItemList_ItemId
            ON dbo.ItemList (itemId);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_DeliveryProcessedItem_ItemList'
    )
    BEGIN
        ALTER TABLE dbo.DeliveryProcessedItem WITH CHECK
            ADD CONSTRAINT FK_DeliveryProcessedItem_ItemList
            FOREIGN KEY (itemId) REFERENCES dbo.ItemList (itemId);
    END;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_DeliveryProcessedItem_DeliveryReport'
)
BEGIN
    ALTER TABLE dbo.DeliveryProcessedItem WITH CHECK
        ADD CONSTRAINT FK_DeliveryProcessedItem_DeliveryReport
        FOREIGN KEY (deliveryReceipt) REFERENCES dbo.DeliveryReport (deliveryReceipt);
END;

COMMIT TRANSACTION;
