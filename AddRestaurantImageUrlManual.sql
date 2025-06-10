-- 检查列是否已存在，如果不存在则添加
IF NOT EXISTS (SELECT * FROM sys.columns 
                WHERE Name = N'ImageUrl'
                AND Object_ID = Object_ID(N'dbo.Restaurants'))
BEGIN
    -- 添加ImageUrl列
    ALTER TABLE dbo.Restaurants
    ADD ImageUrl NVARCHAR(255) NULL;
    
    PRINT 'ImageUrl列已添加到Restaurants表';
END
ELSE
BEGIN
    PRINT 'ImageUrl列已存在于Restaurants表';
END 