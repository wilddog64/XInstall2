using System;
using System.IO;
using System.Text;
using System.Xml;

using Microsoft.WindowsInstaller;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// Summary description for MSI.
    /// </summary>
    public class MSI : ActionElement, IAction
    {
	    // error code hanling variable
	    private enum MSI_OPR_CODE
	    {
		    MSI_OPR_SUCCESS = 0,
	    }

	    private XmlNode _ActionNode            = null;
	    private string _PackageLocation        = String.Empty;

	    // some options for Microsoft Installer
	    private bool _SilentInstall            = true;
	    private InstallUILevel _installUILevel = InstallUILevel.None;

	    private bool _EnableLogging            = true;
	    private InstallLogModes _instLogMode   = InstallLogModes.Info;
	    private string _InstallLogFileName     = String.Empty;

	    // commands that feed to Microsoft Installer command line
	    private bool _RemoveAll                = false;
	    private StringBuilder _InstallCommands = new StringBuilder();

	    // for security setting
	    private bool _UseHighSecurity  = true;
	    private bool _SQLWinAuth       = true;
	    private bool _CreateNewDB      = true;

	    [Action("projectinstall")]
	    public MSI( XmlNode ActionNode ) : base()
	    {
		    this._ActionNode = ActionNode;
	    }

	    #region MSI public methods/properties

	    [Action("silentinstall", Needed=false, Default="false")]
	    public string SilentInstallation
	    {
		    set
		    {
			    this._SilentInstall = bool.Parse( value );
			    if ( this._SilentInstall )
			    {
				    this._installUILevel = InstallUILevel.Basic;
				    base.LogItWithTimeStamp( "Perform silent installation" );
			    }
			    else
			    {
				    this._installUILevel = InstallUILevel.Default;
			    }
		    }
	    }

	    [Action("packagelocation", Needed=true)]
	    public string PackageLocation
	    {
		    get
		    {
			    return this._PackageLocation;
		    }
		    set
		    {
			    this._PackageLocation = value;
			    base.LogItWithTimeStamp(
				String.Format( @"MSI package located at: {0}",
					       this._PackageLocation ) );
		    }
	    }

	    [Action("enablelogging", Needed=false, Default="true")]
	    public string EnableLogging
	    {
		    set
		    {
			    this._EnableLogging = bool.Parse( value );
			    if ( !this._EnableLogging )
			    {
				    this._instLogMode = InstallLogModes.None;
				    base.LogItWithTimeStamp( "logging is enabled" );
			    }
			    else
			    {
				    base.LogItWithTimeStamp( "logging is disabled" );
			    }
		    }
	    }

	    [Action("installlogfile", Needed=false, Default="auto")]
	    public string InstallLogFile
	    {
		    get
		    {
			    return this._InstallLogFileName;
		    }
		    set
		    {
			    this._InstallLogFileName = value;
			    if ( this._InstallLogFileName.ToLower().Equals("auto") )
				    this._InstallLogFileName = Path.ChangeExtension(
								   Path.GetFileName( this.PackageLocation ), "log" );
		    }
	    }

	    [Action("removeall", Needed=false, Default="false")]
	    public string RemoveAll
	    {
		    set
		    {
			    this._RemoveAll = bool.Parse( value );
			    if ( this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"request remove project server" );
				    this._InstallCommands.Append( "REMOVE=ALL" );
			    }
		    }
	    }

	    [Action("projectserveruser", Needed=false, Default="MSProjectServerUser")]
	    public string ProjectServerUser
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"add ProjectServerUser" );
				    this._InstallCommands.AppendFormat( @"PSNAME={0}", value );
			    }
		    }
	    }

	    [Action("projectserveruserpassword", Needed=false, Default="(9Longhorn)")]
	    public string ProjectServerUserPassword
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding ProjectServerUserPassword" );
				    this._InstallCommands.AppendFormat( @"PSPASSWORD={0}", value );
			    }
		    }
	    }

	    [Action("projectuser", Needed=false, Default="MSProjectUser")]
	    public string ProjectUser
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding ProjectUser" );
				    this._InstallCommands.AppendFormat( @"PRJNAME={0}", value );
			    }
		    }
	    }

	    [Action("projectuserpassword", Needed=false, Default="(9Longhorn")]
	    public string ProjectUserPassword
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding ProjectUser Password" );
				    this._InstallCommands.AppendFormat( @"PRJPASSWORD={0}", value );
			    }
		    }
	    }


	    [Action("webroot", Needed=false, Default="1")]
	    public string WebSiteRoot
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding Web Root" );
				    this._InstallCommands.AppendFormat( @"WEBVROOT={0}", value );
			    }
		    }
	    }

	    [Action("intraneturlservername", Needed=false, Default="local")]
	    public string IntraNetServerName
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding intra net url server" );
				    this._InstallCommands.AppendFormat( @"INTRANETURL={0}", value );
			    }
		    }

	    }

	    [Action("extraneturlservername", Needed=false, Default="")]
	    public string ExtraNetServerName
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    if ( value.Length != 0 )
				    {
					    base.LogItWithTimeStamp( @"adding extra net server name" );
					    this._InstallCommands.AppendFormat(
						@"EXTRANETURL=https://www.{0}.com", value );
				    }
			    }
		    }
	    }

	    [Action("projectserveradminpwd", Needed=false, Default="(9Longhorn")]
	    public string ProjectServerAdminPassword
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding project server admin password" );
				    this._InstallCommands.AppendFormat( @"PRJSVRADMINPWD={0}", value );
			    }
		    }
	    }

	    [Action("usehighsecurity", Needed=false, Default="true")]
	    public string UseHighSecurity
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding ProjectUser Password" );
				    this._UseHighSecurity = bool.Parse(value);
				    if ( this._UseHighSecurity )
				    {
					    this._InstallCommands.Append( @"AddLocal=PrjSvrDBInfoSecurityHigh" );
				    }
				    else
				    {
					    this._InstallCommands.Append( @"AddLocal=PrjSvrDBInfoSecurityLow" );
				    }
			    }
		    }
	    }

	    [Action("sqlservername", Needed=true)]
	    public string SQLServerName
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding SQL Server" );
				    this._InstallCommands.AppendFormat( @"SERVERNAME={0}", value );
			    }
		    }
	    }

	    [Action("sqldbname", Needed=true)]
	    public string SQLDBName
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding SQL Server Password" );
				    this._InstallCommands.AppendFormat( @"SQLDBNAME={0}", value );
			    }
		    }
	    }

	    [Action("sqlwinauth", Needed=false, Default="true")]
	    public string SQLAuthType
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding SQL Authzentication Password" );
				    this._SQLWinAuth = bool.Parse( value );
				    if ( this._SQLWinAuth )
				    {
					    this._InstallCommands.Append( "SQLAUTHTYPE=WIN" );
				    }
				    else
				    {
					    this._InstallCommands.Append( "SQLAUTHTYPE=SQL" );
				    }
			    }
		    }

	    }

	    [Action("createnewdb", Needed=false, Default="true")]
	    public string CreateNewProjectServerDatabase
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding create new project database" );
				    this._CreateNewDB = bool.Parse( value );
				    if ( this._CreateNewDB )
				    {
					    this._InstallCommands.Append( "AddLocal=PrjSvrCreateDBFeature" );
				    }
			    }
		    }
	    }

	    [Action("saname", Needed=false, Default="sa")]
	    public string SAName
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding SANAME" );
				    this._InstallCommands.AppendFormat( "SANAME={0}", value );
			    }
		    }
	    }

	    [Action("sapassword", Needed=false, Default="03ck18")]
	    public string SAPassword
	    {
		    set
		    {
			    if ( !this._RemoveAll )
			    {
				    base.LogItWithTimeStamp( @"adding SAPASSWORD" );
				    this._InstallCommands.AppendFormat( "SA={0}", value );
			    }
		    }

	    }

	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable
	    {
		    set
		    {
			    base.Runnable = bool.Parse( value );
		    }
	    }

	    #endregion

	    #region override method comes from ActionElement

	    /// <summary>
	    ///
	    /// </summary>
	    public override void ParseActionElement()
	    {
		    Installer.SetInternalUI( this._installUILevel );
		    Installer.EnableLog( this._instLogMode, this.InstallLogFile );
		    base.LogItWithTimeStamp(
			String.Format( "Command line parameters {0}", this._InstallCommands.ToString() ) );
		    Installer.InstallProduct( this.PackageLocation, this._InstallCommands.ToString() );
	    }

	    public override string ObjectName
	    {
		    get
		    {
			    return this.Name;
		    }
	    }

	    #endregion

	    #region IAction Members

	    public override void Execute()
	    {
		    // TODO:  Add MSI.Execute implementation
		    base.Execute();
		    base.IsComplete = true;
	    }

	    public new bool IsComplete
	    {
		    get
		    {
			    // TODO:  Add MSI.IsComplete getter implementation
			    return base.IsComplete;
		    }
	    }

	    public new string ExitMessage
	    {
		    get
		    {
			    // TODO:  Add MSI.ExitMessage getter implementation
			    return null;
		    }
	    }

	    public new string Name
	    {
		    get
		    {
			    return this.GetType().Name.ToLower();
		    }
	    }

	    public new int ExitCode
	    {
		    get
		    {
			    // TODO:  Add MSI.ExitCode getter implementation
			    return 0;
		    }
	    }
	    #endregion
    }
}
