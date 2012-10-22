set svc = createobject("wbemscripting.swbemlocator").connectserver(".","root\cimv2")

sysinfo = "[SysInfo]" & vbCrLf
sysinfo = sysinfo & "; Only first word in the Make is used by the OEM script." & vbCrLf
sysinfo = sysinfo & "; Example:  " & "Dell Computer Corporation " & "=" & " Dell" & vbCrLf


' get make and model
for each comp in svc.execquery("select manufacturer, model from win32_computersystem")
	sysinfo = sysinfo & "Make=" & comp.manufacturer & vbCrLf
	sysinfo = sysinfo & "Model=" & comp.model & vbCrLf
next

' get bios version

for each bios in svc.execquery("select name, smbiosbiosversion from win32_bios")
	sysinfo = sysinfo & "Bios=" & bios.name & bios.smbiosbiosversion & vbCrLf
next

' get nics
for each nic in svc.execquery("select name from win32_networkadapter where adaptertype = 'ethernet 802.3'")
	sysinfo = sysinfo & "NIC=" & nic.name & vbCrLf
next


' get cdrom
for each cdrom in svc.execquery("select name from Win32_CDROMDrive")
	sysinfo = sysinfo & "CDROM=" & cdrom.name & vbCrLf
next

wscript.echo sysinfo