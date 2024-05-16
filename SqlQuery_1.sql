/* What is the purpose of this script? */
/* How else could this be done? */

DROP TABLE IF EXISTS #TranDateFixedBatchUpdates

CREATE TABLE #TranDateFixedBatchUpdates
    ([LINE_UNIQUE_KEY] [int] NOT NULL)

DECLARE @MIN_TRAN_DATE [DATETIME] = (SELECT CAST(MIN([DATE_SPK]) AS DATETIME) FROM [dbo].[DIM_DATE]);

INSERT INTO #TranDateFixedBatchUpdates
SELECT [LINE_UNIQUE_KEY] FROM [Finance].[Transactions] WHERE TRAN_DATE_FIXED < @MIN_TRAN_DATE

DECLARE @RowsToProcess INT
SELECT @RowsToProcess = COUNT(*) FROM #TranDateFixedBatchUpdates
PRINT 'Rows to process ' + CONVERT(VARCHAR(MAX),@RowsToProcess)

DECLARE @BatchSize INT = 10000;

DECLARE @idxtable TABLE ([LINE_UNIQUE_KEY] INT)
WHILE EXISTS (SELECT 1 FROM #TranDateFixedBatchUpdates)
    BEGIN
        INSERT INTO @idxtable SELECT TOP (@BatchSize) [LINE_UNIQUE_KEY] FROM #TranDateFixedBatchUpdates
        
        UPDATE 
            t
        SET TRAN_DATE_FIXED = @MIN_TRAN_DATE
        FROM [Finance].[Transactions] t
            INNER JOIN @idxtable it ON t.[LINE_UNIQUE_KEY] = it.[LINE_UNIQUE_KEY] 

        DELETE #TranDateFixedBatchUpdates WHERE [LINE_UNIQUE_KEY] IN (SELECT [LINE_UNIQUE_KEY] FROM @idxtable)
        DELETE @idxtable
       
        SELECT GETUTCDATE()
        WAITFOR DELAY '00:00:00.05'
    END 