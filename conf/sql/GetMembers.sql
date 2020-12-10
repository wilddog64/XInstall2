-- GetMembers 0
-- GetMembers 84
if exists ( select id from sysobjects where name='GetMems' )
     drop procedure GetMems
go

CREATE PROCEDURE GetMems
AS
BEGIN
SET NOCOUNT ON
CREATE TABLE #RTable (ResID INT NULL, MemID INT NULL, MemParID INT NULL,
	MemTypeID INT NULL, CFlag INT NULL, MemName varchar(40) NULL, MemDesc varchar(40) NULL)
DECLARE @ResRow INT, @CFlagVal INT
SET @ResRow = 1

DECLARE Level3 CURSOR
READ_ONLY
FOR SELECT MemberID, MemberName, MemberDescription, MemberTypeID
	FROM Members	
	WHERE MemberTypeID = 3 AND IsActive = 1

DECLARE @MID INT, @MName varchar(50), @MDesc varchar(50), @MPar INT, @MType INT, @MFlag INT
SELECT @MPar = NULL
SELECT @MFlag = 0

OPEN Level3
FETCH NEXT FROM Level3 INTO @MID, @MName, @MDesc, @MType

WHILE (@@fetch_status <> -1)
BEGIN
	IF (@@fetch_status <> -2)
	BEGIN
		
		INSERT INTO #RTable 
		VALUES (@ResRow, @MID, NULL, @MType, @MFlag, @MName, @MDesc)
		SELECT @ResRow = @ResRow + 1
		
		DECLARE Level2 CURSOR
		READ_ONLY
		FOR SELECT M.MemberID, M.MemberName, M.MemberDescription, M.MemberTypeID
		FROM MemberRelationships R INNER JOIN Members M ON R.MemberRelationID = M.MemberID
		WHERE R.MemberID = @MID

		DECLARE @M2ID INT, @M2Name varchar(50), @M2Desc varchar(50), @M2Type INT
		OPEN Level2

		FETCH NEXT FROM Level2 INTO @M2ID, @M2Name, @M2Desc, @M2Type
		WHILE (@@fetch_status <> -1)
		BEGIN
			IF (@@fetch_status <> -2)
			BEGIN
/* Here, the parent is the MID from the outer cursor loop */			
				INSERT INTO #RTable 
				VALUES (@ResRow, @M2ID, @MID, @M2Type, @MFlag, @M2Name, @M2Desc)
				SELECT @ResRow = @ResRow + 1
			
				DECLARE Level1 CURSOR
				READ_ONLY
				FOR SELECT M.MemberID, M.MemberName, M.MemberDescription, M.MemberTypeID 
				FROM MemberRelationships R INNER JOIN Members M ON R.MemberRelationID = M.MemberID
				WHERE R.MemberID = @M2ID 

				DECLARE @M3ID INT, @M3Name varchar(50), @M3Desc varchar(50), @M3Type INT
				OPEN Level1

				FETCH NEXT FROM Level1 INTO @M3ID, @M3Name, @M3Desc, @M3Type
				WHILE (@@fetch_status <> -1)
				BEGIN
					IF (@@fetch_status <> -2)
					BEGIN
/* Here, the parent is the M2ID from the middle cursor loop */
						SET @MFlag = 0
						INSERT INTO #RTable 
						VALUES (@ResRow, @M3ID, @M2ID, @M3Type, @MFlag, @M3Name, @M3Desc)
						SELECT @ResRow = @ResRow + 1
					END
					FETCH NEXT FROM Level1 INTO @M3ID, @M3Name, @M3Desc, @M3Type
					 
				END
				UPDATE #RTable SET CFlag = CFlag + 1 WHERE ResID = ( @ResRow -1 )
				CLOSE Level1
				DEALLOCATE Level1	
			
			END
			FETCH NEXT FROM Level2 INTO @M2ID, @M2Name, @M2Desc, @M2Type
		END
		UPDATE #RTable SET CFlag = CFlag + 1 WHERE ResID = ( @ResRow -1 )
		CLOSE Level2
		DEALLOCATE Level2
		
	END
	FETCH NEXT FROM Level3 INTO @MID, @MName, @MDesc, @MType
END
UPDATE #RTable SET CFlag = CFlag - 1 WHERE ResID = ( @ResRow -1 )
CLOSE Level3
DEALLOCATE Level3

SELECT ResID, MemID, MemParID, MemTypeID, CFlag, MemName, MemDesc 
FROM #RTable 
ORDER BY ResID

END

-- select * from membertypes

-- GetMems