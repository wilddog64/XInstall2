DROP TABLE CP2RPMRES_ResourcesToAdd
GO
				
CREATE TABLE CP2RPMRES_ResourcesToAdd (
			 ResourceName		nvarchar(255)	NOT NULL
			,ResourceAlias		nvarchar(100)		NULL
			,ResourceEmail		nvarchar(100)		NULL
			,WasUpdated			bit				NOT NULL DEFAULT 1
			)
GO

INSERT INTO CP2RPMRES_ResourcesToAdd VALUES (
	'Lawrence Twork','NORTHAMERICA\LTWORK','ltwork@bogus.com',1)
INSERT INTO CP2RPMRES_ResourcesToAdd VALUES (
	'Leah Clelland','REDMOND\LAC','lac@bogus.com',1)
INSERT INTO CP2RPMRES_ResourcesToAdd VALUES (
	'Mark J. Brown','REDMOND\MJBROWN','mjbrown@bogus.com',1)
INSERT INTO CP2RPMRES_ResourcesToAdd VALUES (
	'Meetul Shah','REDMOND\MEETULS','meetuls@bogus.com',1)
INSERT INTO CP2RPMRES_ResourcesToAdd VALUES (
	'Michael Regan','REDMOND\MREGAN','MRegan@bogus.com',1)
INSERT INTO CP2RPMRES_ResourcesToAdd VALUES (
	'Mike Kennedy','REDMOND\MICHKEN','michken@bogus.com',1)
INSERT INTO CP2RPMRES_ResourcesToAdd VALUES (
	'Mike Lamb','REDMOND\MIKELA','mikela@bogus.com',1)
INSERT INTO CP2RPMRES_ResourcesToAdd VALUES (
	'Mike Shughrue','NORTHAMERICA\MSHUG','mshug@bogus.com',1)
INSERT INTO CP2RPMRES_ResourcesToAdd VALUES (
	'Mike Taghizadeh','REDMOND\MIKETAG','miketag@bogus.com',1)
GO