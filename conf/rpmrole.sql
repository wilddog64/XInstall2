select * from Members


select * from MemberRelationships
select * from MemberTypes

select m.MemberID GroupID, 
       cast(m.MemberName as varchar(20)) GroupName, 
       m1.MemberID RoleID,
       cast(m1.MemberName as varchar(20)) RoleName,
       cast(m.MemberDescription as varchar(20)) MemberDescription
  from Members m 
 inner join MemberTypes mt
    on (m.MemberTypeID = mt.MemberTypeID)
 inner join MemberRelationships mr
    on (m.MemberID = mr.MemberID)
 inner join Members m1 
    on ( mr.MemberRelationID = m1.MemberID)
 where m.MemberTypeID = 4