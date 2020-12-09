SELECT * from Members


SELECT * from MemberRelationships
SELECT * from MemberTypes

SELECT m.MemberID GroupID, 
       cast(m.MemberName as varchar(20)) GroupName, 
       m1.MemberID RoleID,
       cast(m1.MemberName as varchar(20)) RoleName,
       cast(m.MemberDescription as varchar(20)) MemberDescription
  FROM members m 
 INNER join MemberTypes mt
    ON (m.MemberTypeID = mt.MemberTypeID)
 INNER join MemberRelationships mr
    ON (m.MemberID = mr.MemberID)
 INNER join Members m1 
    ON ( mr.MemberRelationID = m1.MemberID)
 WHERE m.MemberTypeID = 4
