'---------------------------------------------------------------------------------------------------
' This function searches a computer for a specified Web Site, and
' displays information about the site.
'
' findweb [--computer|-c COMPUTER]
'         <--website|-w WEBSITE>
'         [--help|-?]
'
'COMPUTER                Computer on which users exists
'WEBSITE1,WEBSITE2       Virtual Web Sites on which directories will be created
'NAME1,PATH1,NAME2,PATH2 Virtual Directories names and paths to create
'
'Example 1		mkwebdir -c MyComputer -w "Default Web Site","Another Web Site"
'                       -v "Virtual Dir1","c:\inetpub\wwwroot\dir1","Virtual Dir2","c:\inetpub\wwwroot\dir2"
'
'---------------------------------------------------------------------------------------------------

' Force explicit declaration of all variables.
Option Explicit

On Error Resume Next

Dim oArgs, ArgNum

Dim ArgComputer, ArgWebSites, ArgVirtualDirs, ArgDirNames(), ArgDirPaths(), DirIndex
Dim ArgComputers

Set oArgs = WScript.Arguments
ArgComputers = Array("LocalHost")
ArgWebSites = "1"
ArgNum = 0
While ArgNum < oArgs.Count
	Select Case LCase(oArgs(ArgNum))
		Case "--computer","-c":
			ArgNum = ArgNum + 1
			If (ArgNum >= oArgs.Count) Then
				Call DisplayUsage
			End If	
			ArgComputers = Split(oArgs(ArgNum), ",", -1)
		Case "--website","-w":
			ArgNum = ArgNum + 1
			If (ArgNum >= oArgs.Count) Then
				Call DisplayUsage
			End If	
			ArgWebSites = oArgs(ArgNum)
		Case "--virtualdir","-v":
			ArgNum = ArgNum + 1
			If (ArgNum >= oArgs.Count) Then
				Call DisplayUsage
			End If	
			ArgVirtualDirs = Split(oArgs(ArgNum), ",", -1)
		Case "--help","-?"
			Call DisplayUsage
		Case Else:
			ArgWebSites = oArgs(ArgNum)
	End Select	

	ArgNum = ArgNum + 1
Wend

Dim foundSite
Dim compIndex
Dim bindInfo
Dim aBinding, binding

for compIndex = 0 to UBound(ArgComputers)
	set foundSite = findWeb(ArgComputers(compIndex), ArgWebSites)
	if isObject(foundSite) then
		Trace "  Web Site Number = "&foundSite.Name
		Trace "  Web Site Description = "&foundSite.ServerComment
		aBinding=foundSite.ServerBindings
		if (IsArray(aBinding)) then
			if aBinding(0) = "" then
				binding = Null
			else
				binding = getBinding(aBinding(0))
			end if
		else 
			if aBinding = "" then
				binding = Null
			else
				binding = getBinding(aBinding)
			end if
		end if
		if (IsArray(binding)) then
			Trace "    Hostname = "&binding(2)
			Trace "    Port = "&binding(1)
			Trace "    IP Address = "&binding(2)
		end if
	else
		Trace "No matching web found."
	end if
next

function getBinding(bindstr)

	Dim one, two, ia, ip, hn
	
	one=Instr(bindstr,":")
	two=Instr((one+1),bindstr,":")
	
	ia=Mid(bindstr,1,(one-1))
	ip=Mid(bindstr,(one+1),((two-one)-1))
	hn=Mid(bindstr,(two+1))
	
	getBinding=Array(ia,ip,hn)
end function

Function findWeb(computer, webname)
	On Error Resume Next

	Dim websvc, site
	dim webinfo
	Dim aBinding, binding

	set websvc = GetObject("IIS://"&computer&"/W3svc")
	if (Err <> 0) then
		exit function
	end if
	' First try to open the webname.
	set site = websvc.GetObject("IIsWebServer", webname)
	if (Err = 0) and (not isNull(site)) then
		if (site.class = "IIsWebServer") then
			' Here we found a site that is a web server.
			set findWeb = site
			exit function
		end if
	end if
	err.clear
	for each site in websvc
		if site.class = "IIsWebServer" then
			'
			' First, check to see if the ServerComment
			' matches
			'
			If site.ServerComment = webname Then
				set findWeb = site
				exit function
			End If
			aBinding=site.ServerBindings
			if (IsArray(aBinding)) then
				if aBinding(0) = "" then
					binding = Null
				else
					binding = getBinding(aBinding(0))
				end if
			else 
				if aBinding = "" then
					binding = Null
				else
					binding = getBinding(aBinding)
				end if
			end if
			if IsArray(binding) then
				if (binding(2) = webname) or (binding(0) = webname) then
					set findWeb = site
					exit function
				End If
			end if 
		end if
	next
End Function

'---------------------------------------------------------------------------------
Sub Display(Msg)
	WScript.Echo Now & ". Error Code: " & Hex(Err) & " - " & Msg
End Sub

Sub Trace(Msg)
	WScript.Echo Msg	
End Sub

Sub DisplayUsage()
	WScript.Echo " findweb [--computer|-c COMPUTER]"
	WScript.Echo "         [WEBSITE]"
	WScript.Echo "         [--help|-?]"
	WScript.Echo ""
	WScript.Echo "Finds the named web on the specified computer."
	WScript.Echo "Displays the site number, description, host name, port,"
	WScript.Echo "and IP Address"
	WScript.Echo ""
	WScript.Echo "Note, WEBSITE is the Web Site on which the directory will be created."
	WScript.Echo "The name can be specified as one of the following, in the priority specified:"
	WScript.Echo " Server Number (i.e. 1, 2, 10, etc.)"
	WScript.Echo " Server Description (i.e ""My Server"")"
	WScript.Echo " Server Host name (i.e. ""www.domain.com"")"
	WScript.Echo " IP Address (i.e., 127.0.0.1)"
	WScript.Echo ""
	WScript.Echo "Example findweb -c MACHINE www.mycompany.com"
	WScript.Quit
End Sub
'---------------------------------------------------------------------------------
