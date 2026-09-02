/* =========================
   WARNING CATEGORY
   ========================= */
IF OBJECT_ID('ws.WarningCategory','U') IS NULL
BEGIN
    CREATE TABLE ws.WarningCategory
    (
        WarningId BIGINT NOT NULL,
        CategoryId INT NOT NULL,
        CreatedOn DATETIME2(0) NOT NULL,
        CreatedBy VARCHAR(100) NOT NULL,

        CONSTRAINT PK_WarningCategory 
            PRIMARY KEY (WarningId, CategoryId)
    );
END


IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_WarningCategory_Warning')
BEGIN
    ALTER TABLE ws.WarningCategory
    ADD CONSTRAINT FK_WarningCategory_Warning
    FOREIGN KEY (WarningId) REFERENCES ws.Warning(WarningId);
END


IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_WarningCategory_Category')
BEGIN
    ALTER TABLE ws.WarningCategory
    ADD CONSTRAINT FK_WarningCategory_Category
    FOREIGN KEY (CategoryId) REFERENCES ws.TransgressionCategory(CategoryId);
END