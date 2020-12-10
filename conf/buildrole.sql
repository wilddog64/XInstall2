--Create Groups
INSERT INTO Members (MemberTypeID, MemberName, MemberDescription) VALUES (4, 'Redmond\RPMDev_RQ', 'Redmond\RPMDev_RQ')
INSERT INTO Members (MemberTypeID, MemberName, MemberDescription) VALUES (4, 'Redmond\RPMDev_RM', 'Redmond\RPMDev_RM')
INSERT INTO Members (MemberTypeID, MemberName, MemberDescription) VALUES (4, 'Redmond\RPMDev_PDM', 'Redmond\RPMDev_PDM')
INSERT INTO Members (MemberTypeID, MemberName, MemberDescription) VALUES (4, 'Redmond\RPMDev_TM', 'Redmond\RPMDev_TM')


--Associate Requestor Role to Group(s)
INSERT INTO MemberRelationships VALUES (89, 84)

--Associate RM Role to Group(s)
INSERT INTO MemberRelationships VALUES (90, 85)

--Associate PDM Role to Group(s)
INSERT INTO MemberRelationships VALUES (91, 87)

--Associate TM Role to Group(s)
INSERT INTO MemberRelationships VALUES (92, 86)


getmems