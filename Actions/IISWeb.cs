using System;
using System.IO;
using System.Text;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// public class IISWeb -
    ///     class that performs a website setup on a given
    ///     IIS server.
    /// </summary>
    public class IISWeb : ExternalPrg, ICleanUp, IAction
    {
	    #region private constant variables
	    private const string _cntIIsWebApp      = @"iisweb.vbs";
	    private const string _cntIIsDefaultRoot = @"c:\inetpub\wwwroot";
	    private const string _cntIIsDefaultPort = "80";
	    #endregion

	    #region private property method variables
	    // path of current iisweb.vbs
	    private static string _strIIsWebAppFullPath = String.Empty;
	    private string _strAction            = "create";
	    private string _strTargetServer      = null;
	    private string _strWebSiteName       = null;
	    private string _strWebSiteRoot       = _cntIIsDefaultRoot;
	    private string _strWebSitePort       = _cntIIsDefaultPort;
	    private string _strWebSiteHostName   = null;
	    private string _strWebSiteUserName   = null;
	    private string _strWebSiteUserPass   = null;
	    private string _strWebSiteIP         = null;
	    private bool   _bWebSiteDontStart    = true;

	    private enum IISWEB_OPR_CODE
	    {
		    IISWEB_OPR_SUCCESS = 0,
		    IISWEB_OPR_BOOLEAN_PARSE_ERROR,
		    IISWEB_OPR_AUTOEXCEPTION_GENERATED,
		    IISWEB_OPR_EXTERNAL_PROGRAM_NOTFOUND,
		    IISWEB_OPR_INVALID_ACTION_SPECIFIED,
	    };

	    private IISWEB_OPR_CODE _enumIISWebOprCode = IISWEB_OPR_CODE.IISWEB_OPR_SUCCESS;
	    private string[] _strMessages =
	    {
		    "{0}: operation {1} {2} on server {3} complete successfully, exit code {4}",
		    "{0}: boolean variable parsing error, exit code {1}",
		    "{0}: request generate an exception by user, exit code {1}",
		    "{0}: external program {1} cannot be found, exit code {1}",
		    "{0}: invalid action encounter, only create, delete, start, stop, and pause are allowed, exit code {1}",
	    };
	    private string _strExitMessage = null;

	    #endregion

	    /// <summary>
	    /// constructor IISWeb initiates the reuqirement for
	    /// creating IISWeb object;
	    /// </summary>
	    [Action("iisweb", Needed=true)]
	    public IISWeb() : base()
	    {
		    //
		    // TODO: Add constructor logic here
		    //
		    Init();
	    }


	    #region public property methods

	    /// <summary>
	    /// property TargetWebServer -
	    ///     get/set the target web server
	    ///     we need to work on.
	    /// </summary>
	    [Action("targetserver", Needed=true)]
	    public string TargetWebServer
	    {
		    get
		    {
			    return this._strTargetServer;
		    }
		    set
		    {
			    this._strTargetServer = value;
		    }
	    }


	    /// <summary>
	    /// property WebSiteUserName -
	    ///     get/set the user id used to work with
	    ///     a given website
	    /// </summary>
	    [Action("websiteuser", Needed=false)]
	    public string WebSiteUserName
	    {
		    get
		    {
			    return this._strWebSiteUserName;
		    }
		    set
		    {
			    this._strWebSiteUserName = value;
		    }
	    }


	    /// <summary>
	    /// property WebSiteUserPassword -
	    ///     get/set the password that associates with
	    ///     WebSiteuserName
	    /// </summary>
	    [Action("websiteuserpassword", Needed=false)]
	    public string WebSiteUserPassword
	    {
		    get
		    {
			    return this._strWebSiteUserPass;
		    }
		    set
		    {
			    this._strWebSiteUserPass = value;
		    }
	    }


	    /// <summary>
	    /// property WebSiteRoot -
	    ///     get/set the root directory for a given
	    ///     website that are going to be created
	    /// </summary>
	    [Action("websiteroot", Needed=true)]
	    public string WebSiteRoot
	    {
		    get
		    {
			    return this._strWebSiteRoot;
		    }
		    set
		    {
			    if ( value == null )
			    {
				    this._strWebSiteRoot = _cntIIsDefaultRoot;
			    }
			    else
			    {
				    this._strWebSiteRoot = value;
			    }
		    }
	    }


	    /// <summary>
	    /// property WebSiteName -
	    ///     get/set the name that will appear on
	    ///     the MMC (Microsoft Managment Console)
	    /// </summary>
	    [Action("websitename", Needed=true)]
	    public string WebSiteName
	    {
		    get
		    {
			    return this._strWebSiteName;
		    }
		    set
		    {
			    this._strWebSiteName = value;
		    }
	    }


	    /// <summary>
	    /// property WebSiteHostName -
	    ///     get/set the hostname that is
	    ///     assigned to the website we
	    ///     just created
	    /// </summary>
	    [Action("websitehostname", Needed=false)]
	    public string WebSiteHostName
	    {
		    get
		    {
			    return this._strWebSiteHostName;
		    }
		    set
		    {
			    this._strWebSiteHostName = value;
		    }
	    }


	    /// <summary>
	    /// property WebSitePort -
	    ///     get/set the port that assigns to
	    ///     the website being created.
	    /// </summary>
	    [Action("port", Needed=false)]
	    public string WebSitePort
	    {
		    get
		    {
			    return this._strWebSitePort;
		    }
		    set
		    {
			    if ( value == null )
			    {
				    this._strWebSitePort = _cntIIsDefaultPort;
			    }
			    else
			    {
				    this._strWebSitePort = value;
			    }
		    }
	    }


	    /// <summary>
	    /// property WebSiteIP -
	    ///     get/set the IP address that
	    ///     associates with the website
	    ///     being created.
	    /// </summary>
	    [Action("websiteip", Needed=false)]
	    public string WebSiteIP
	    {
		    get
		    {
			    return this._strWebSiteIP;
		    }
		    set
		    {
			    this._strWebSiteIP = value;
		    }
	    }

	    /// <summary>
	    /// property WebSiteDontStart -
	    ///     get/set the boolean value that indicates
	    ///     a given website should start after created
	    /// </summary>
	    [Action("websitedontstart", Needed=false, Default="true")]
	    public string WebSiteDontStart
	    {
		    get
		    {
			    string strWebSiteDontStart =
				this._bWebSiteDontStart.ToString();
			    return strWebSiteDontStart.ToLower();
		    }
		    set
		    {
			    try
			    {
				    this._bWebSiteDontStart = bool.Parse( value.ToString() );
			    }
			    catch ( Exception )
			    {
				    // base.FatalErrorMessage( ".", "boolean variable parsing error", 1660, 3);
				    this._enumIISWebOprCode =
					IISWEB_OPR_CODE.IISWEB_OPR_BOOLEAN_PARSE_ERROR;
				    this._strExitMessage    =
					String.Format( this._strMessages [ this.ExitCode ],
						       this.Name, this.ExitCode );
				    throw;
			    }
		    }
	    }

	    /// <summary>
	    /// set a flag to indicate if the action should be run or not
	    /// </summary>
	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable
	    {
		    set
		    {
			    base.Runnable = bool.Parse( value );
		    }
	    }

	    /// <summary>
	    /// property Arguments -
	    ///     get the arguments that use by the iisvdir
	    ///     external vb script program.
	    /// </summary>
	    public string Arguments
	    {
		    get
		    {
			    return this.GetArguments();
		    }
	    }

	    /// <summary>
	    /// property StartTime -
	    ///     get the external program starting time
	    /// </summary>
	    public string StartTime
	    {
		    get
		    {
			    return base.ProgramStartTime.ToShortTimeString();
		    }
	    }


	    /// <summary>
	    /// property EndTime -
	    ///     get the external program ending time
	    /// </summary>
	    public string EndTime
	    {
		    get
		    {
			    return base.ProgramExitTime.ToShortTimeString();
		    }
	    }


	    /// <summary>
	    /// property ExitCode -
	    ///     get the exit code return from the external program.
	    /// </summary>
	    [Action("exitcode", Needed=false)]
	    public new int ExitCode
	    {
		    get
		    {
			    return (int) this._enumIISWebOprCode;
		    }
	    }


	    /// <summary>
	    /// property OutputFile -
	    ///     set the output file where the external program's
	    ///     output should direct to.
	    /// </summary>
	    public new string OutputFile
	    {
		    set
		    {
			    base.ProgramOutputFile = value;
		    }
	    }

	    /// <summary>
	    /// property AllowGenerateException -
	    ///     sets a flag that tells whether IIsWeb should
	    ///     generate an exception automatically.
	    /// </summary>
	    [Action("generateexception", Needed=false, Default="false")]
	    public new string AllowGenerateException
	    {
		    set
		    {
			    try
			    {
				    base.AllowGenerateException =
					bool.Parse( value.ToString() );
			    }
			    catch ( Exception )
			    {
				    // base.FatalErrorMessage( ".", "boolean variable pasing error!", 1660, 4 );
				    this._enumIISWebOprCode =
					IISWEB_OPR_CODE.IISWEB_OPR_BOOLEAN_PARSE_ERROR;
				    this._strExitMessage =
					String.Format( this._strMessages[ this.ExitCode ],
						       this.Name, this.ExitCode );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
	    }

	    /// <summary>
	    /// set a flag to signal object to ingore any error it generates
	    /// </summary>
	    /// <remarks></remarks>
	    [Action("skiperror", Needed=false, Default="false")]
	    public new string SkipError
	    {
		    set
		    {
			    try
			    {
				    base.SkipError = bool.Parse( value );
			    }
			    catch ( Exception )
			    {
				    this._enumIISWebOprCode =
					IISWEB_OPR_CODE.IISWEB_OPR_BOOLEAN_PARSE_ERROR;
				    this._strExitMessage =
					String.Format( this._strMessages[ this.ExitCode ],
						       this.Name, this.ExitCode );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
	    }
	    #endregion

	    #region public override methods
	    public override void ParseActionElement()
	    {
		    base.ParseActionElement();
		    // get iisweb.vbs's console output
		    string ExtPrgOutput = base.ProgramOutput;
		    string ErrorMsg     = String.Empty;
		    bool   Error        = false;
		    int    pos          = ExtPrgOutput.IndexOf( @"not found" );
		    if ( pos > -1 )
		    {
			    ErrorMsg =
				String.Format( @"Website {0} {1}", this.WebSiteName,
					       ExtPrgOutput.Substring( pos, @"not found".Length ) );
			    Error = true;
		    }
		    // cleanup variables.
		    base.ProgramArguments = null;

		    if ( Error )
		    {
			    base.IsComplete=false;
			    throw new Exception( ErrorMsg );
		    }
		    else
		    {
			    base.IsComplete = true;
		    }
	    }

	    #endregion

	    #region private methods

	    /// <summary>
	    /// private void Init() -
	    ///     initializes various variables and
	    ///     performs a necessary checking.
	    /// </summary>
	    private void Init()
	    {
		    _strIIsWebAppFullPath = Path.Combine(
						Environment.GetEnvironmentVariable(@"WINDIR"),
						@"System32" ) + Path.DirectorySeparatorChar + _cntIIsWebApp;

		    FileInfo fi = new FileInfo( _strIIsWebAppFullPath );
		    if ( !fi.Exists )
		    {
			    this._enumIISWebOprCode = IISWEB_OPR_CODE.IISWEB_OPR_EXTERNAL_PROGRAM_NOTFOUND;
			    this._strExitMessage    =
				String.Format( this._strMessages[ this.ExitCode ], this.Name, _cntIIsWebApp, this.ExitCode );
			    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode);
		    }

		    base.ProgramName           = _strIIsWebAppFullPath;
		    base.ProgramRedirectOutput = "true";

	    }

	    /// <summary>
	    /// private string GetArgumentList() -
	    ///     setup a argument list for iisweb.vbs
	    ///     to create website.
	    /// </summary>
	    /// <returns></returns>
	    protected override string GetArguments()
	    {
		    StringBuilder sbProgArgs = new StringBuilder();

		    // append server, user name, and user password
		    // to our argument list if anyone is provided
		    string strArguments = null;
		    if ( base.ProgramName.IndexOf(_strIIsWebAppFullPath) == -1 )
		    {
			    base.ProgramName = _strIIsWebAppFullPath;
		    }

		    // check if any of these variables are provided and append
		    // necessary switches.
		    if ( this.TargetWebServer != null )
		    {
			    sbProgArgs.AppendFormat(" /s {0}", this.TargetWebServer);
		    }
		    if ( this.WebSiteUserName != null )
		    {
			    sbProgArgs.AppendFormat(" /u {0}", this.WebSiteUserName);
		    }
		    if ( this.WebSiteUserPassword != null )
		    {
			    sbProgArgs.AppendFormat(" /p {0}", this.WebSiteUserPassword);
		    }

		    // check out what action we need to carry out
		    // a valid action includes: create, delete,
		    // start, and stop.  Once we found the required action,
		    // we also need to create sub-arguments for these action
		    // if it is required.
		    switch ( _strAction )
		    {
		    case "create":
			    sbProgArgs.Append(" /create");
			    sbProgArgs.AppendFormat(" {0} {1} /b {2}", this.WebSiteRoot, this.WebSiteName, this.WebSitePort);

			    if ( this.WebSiteHostName != null )
			    {
				    sbProgArgs.AppendFormat (" /d {0}", this.WebSiteHostName);
			    }
			    if ( this.WebSiteIP != null )
			    {
				    bProgArgs.AppendFormat(" /i {0}", this.WebSiteIP);
			    }
			    sbProgArgs.AppendFormat(" {0}",
						    this.WebSiteDontStart.Equals("true") ? "/dontstart " : "");
			    break;
		    case "delete":
			    sbProgArgs.AppendFormat(" /delete {0}", this.WebSiteName);
			    break;
		    case "start":
			    sbProgArgs.AppendFormat(" /start {0}", this.WebSiteName);
			    break;
		    case "stop":
			    sbProgArgs.AppendFormat(" /stop {0}", this.WebSiteName);
			    break;
		    case "pause":
			    sbProgArgs.AppendFormat(" /pause {0}", this.WebSiteName);
			    break;
		    default:
			    this._enumIISWebOprCode =
				IISWEB_OPR_CODE.IISWEB_OPR_INVALID_ACTION_SPECIFIED;
			    this._strExitMessage    =
				String.Format( this._strMessages[ this.ExitCode ],
					       this.Name, this.ExitCode );
			    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode);
			    break;
		    }

		    strArguments = sbProgArgs.ToString();

		    return strArguments;
	    }
	    #endregion

	    #region ICleanUp Members

	    /// <summary>
	    /// public override void RemoveIt() -
	    ///     a method drives from ICleanUp interface
	    ///     that performs a clean up when required.
	    /// </summary>
	    public override void RemoveIt()
	    {
		    base.RemoveIt();
		    this.Action = "delete";
		    this.Execute();
	    }

	    #endregion

	    #region IAction Members

	    /// <summary>
	    /// property XInstall.Core.IAction.Action -
	    ///     get the action that will be performed
	    ///     by IISWeb object.
	    /// </summary>
	    [Action("action", Needed=true)]
	    public string Action
	    {
		    get
		    {
			    return this._strAction;
		    }
		    set
		    {
			    this._strAction = value;
		    }
	    }

	    /// <summary>
	    /// property ExitMessage -
	    ///     gets the message that is corresponding to the exit code
	    ///     from the operation.
	    /// </summary>
	    public new string ExitMessage
	    {
		    get
		    {
			    return this._strExitMessage;
		    }
	    }

	    /// <summary>
	    /// property IsComplete
	    ///     get/set the state of Action
	    /// </summary>
	    public new bool IsComplete
	    {
		    get
		    {
			    return base.IsComplete;
		    }
	    }

	    /// <summary>
	    /// property Name -
	    ///     gets the name of constructor
	    /// </summary>
	    public new string Name
	    {
		    get
		    {
			    return this.GetType().Name;
		    }
	    }

	    public override string ObjectName
	    {
		    get
		    {
			    return this.Name;
		    }
	    }

	    #endregion
    }
}
