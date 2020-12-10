using System;
using System.Text;
using System.IO;

using XInstall.Core;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// The IISVDir class wrap the call to iisvdir.vbs
    /// visual basic script.  The class provides these
    /// functions: create/delete a virtual directory.
    /// </summary>
    public class IISVDir : ExternalPrg, ICleanUp, IAction
    {
	    #region private constant variables;
	    private const string _cntStrIIsVDir     = @"IIsVDir.vbs";
	    private const string _cntDefaultWebSite = @"w3svc/1/root";
	    private const string _cntStrIIsVBPath   = @"c:\WINDOWS\system32";
	    private const char   _cntCQuote         = '"';
	    #endregion

	    #region private property method variables
	    private string _strIIsVDirFullPath   = String.Empty;

	    private string _strAction           = "create";
	    private string _strTargetIIsServer  = null;
	    private string _strWebSiteUserName  = null;
	    private string _strWebSiteUserPass  = null;
	    private string _strWebSiteName      = null;
	    private string _strVirtualDirName   = null;
	    private string _strPhysicalPath     = null;
	    private string _strAppFriendlyName  = null;
	    private string _strAppOutputFile    = null;
	    private string _strExitMessage      = null;
	    private bool   _bIsCompleted        = false;
	    private bool   _bIgnoreError        = false;

	    private enum IISVDIR_OPR_CODE
	    {
		    IISVDIR_OPR_SUCCESS = 0,
		    IISVDIR_OPR_BOOLEAN_PARSING_ERROR,
		    IISVDIR_OPR_OPERATION_FAILED,
		    IISVDIR_OPR_EXTERNAL_PROGRAM_NOTFOUND,
		    IISVDIR_OPR_AUTOEXCEPTION_GENERATED,
		    IISVDIR_OPR_INVALID_ACTION_SPECIFIED,
	    }

	    private IISVDIR_OPR_CODE _enumIISOprCode = IISVDIR_OPR_CODE.IISVDIR_OPR_SUCCESS;
	    private string[] _strMessages =
	    {
		    "{0}: successfully {1} virtual directory on {2} website {3}, return code {4}",
		    "{0}: boolean variable parsing error, exit code {1}",
		    "{0}: operation {1} virtual directory {2} for {3} on server {4} failed, exit code {5}",
		    "{0}: external program {1} cannot be found, exit code {2}",
		    "{0}: auto generate exception requested, exit code {1}",
		    "{0}: invalid action spcified, only create and delete are accepted, exit code {1}",
	    };
	    #endregion

	    #region public constructors

	    /// <summary>
	    /// public IISVDir - construct the IISVDir object and
	    ///     initialize the program to be called
	    /// </summary>
	    [Action("iisvdir", Needed=true)]
	    public IISVDir() : base ()
	    {
		    //
		    // Initialize the object
		    //
		    _strIIsVDirFullPath = Path.Combine( Environment.GetEnvironmentVariable( "WINDIR" ), @"System32" ) +
					  Path.DirectorySeparatorChar                                   +
					  _cntStrIIsVDir;
		    this.Init();
	    }

	    /// <summary>
	    /// Overloaded constructor IISVDir -
	    ///     to initlazed the object
	    /// </summary>
	    /// <param name="strProgArguments">the arguments for the iisvdir.vbs</param>
	    /// <param name="strProgOutputFile">output log file for the iisvidr.vbs</param>
	    public IISVDir( string strProgArguments, string strProgOutputFile ) : base( strProgArguments, strProgOutputFile )
	    {
		    this.Init();
		    base.ProgramName           = _cntStrIIsVDir;
		    base.ProgramRedirectOutput = "true";
	    }
	    #endregion

	    #region public property methods

	    /// <summary>
	    /// property Action -
	    ///     get/set the action that iisvdir will take. It
	    ///     accepts the following actions:
	    ///
	    ///     create - create a virtual directory
	    ///     delete - delete a virtual directory
	    ///     query  - query a given virtual directory's
	    ///              existence
	    /// </summary>
	    [Action("action", Needed=true)]
	    public string Action
	    {
		    set
		    {
			    this._strAction = value;
		    }
	    }


	    /// <summary>
	    /// property TargetServer -
	    ///     get/set the target iis web server
	    ///     we are going to work on
	    /// </summary>
	    [Action("targetserver", Needed=true)]
	    public string TargetServer
	    {
		    get
		    {
			    return this._strTargetIIsServer;
		    }
		    set
		    {
			    this._strTargetIIsServer = value;
		    }
	    }

	    /// <summary>
	    /// property WebSite -
	    ///     get/set the virtual directory we'll be
	    ///     creating
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
			    if ( _strWebSiteName == null )
			    {
				    this._strWebSiteName = _cntDefaultWebSite;
			    }
		    }
	    }


	    /// <summary>
	    /// property method WebSiteUser -
	    ///     the user that used to maintain the given website
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
	    /// property method WebSiteUserPassword -
	    ///     the password associates with the WebSiteUser
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
	    /// property MapPath -
	    ///     get/set the actual path for a given
	    ///     virtual directory
	    /// </summary>
	    [Action("mappath", Needed=true)]
	    public string MapPath
	    {
		    get
		    {
			    return this._strPhysicalPath;
		    }
		    set
		    {
			    this._strPhysicalPath = value;
		    }
	    }


	    /// <summary>
	    /// property WebFriendlyName -
	    ///     get/set the alias for the virtual
	    ///     directory
	    /// </summary>
	    [Action("appfriendlyname", Needed=false)]
	    public string WebFriendlyName
	    {
		    get
		    {
			    return this._strAppFriendlyName;
		    }
		    set
		    {
			    this._strAppFriendlyName = _cntCQuote + value + _cntCQuote;
		    }
	    }

	    /// <summary>
	    /// property method WebVirtualDirectory -
	    ///     get/set the virtual directory name
	    /// </summary>
	    [Action("virtualdirname", Needed=true)]
	    public string WebVirtualDirectory
	    {
		    get
		    {
			    return this._strVirtualDirName;
		    }
		    set
		    {
			    this._strVirtualDirName = _cntCQuote + value + _cntCQuote;
		    }
	    }


	    /// <summary>
	    /// property OutputFile -
	    ///     get/set the output log file
	    /// </summary>
	    [Action("outputfile", Needed=false, Default="auto")]
	    public new string OutputFile
	    {
		    set
		    {
			    _strAppOutputFile      = value;
			    if ( _strAppOutputFile.Equals( "auto" ) )
				    _strAppOutputFile  = Directory.GetCurrentDirectory() +
							 Path.DirectorySeparatorChar     +
							 @"logs"                         +
							 Path.DirectorySeparatorChar     +
							 Path.ChangeExtension( Path.GetFileName ( Environment.GetCommandLineArgs()[0] ), ".log" );
			    base.ProgramOutputFile = _strAppOutputFile;
		    }
	    }


	    /// <summary>
	    /// property StartTime -
	    ///     get the external program start time
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
	    ///     get the external program end time
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
	    ///     get the exit code of external program
	    /// </summary>
	    [Action("exitcode", Needed=false)]
	    public new int ExitCode
	    {
		    get
		    {
			    return (int) this._enumIISOprCode;
		    }
	    }

	    /// <summary>
	    /// property ExitMessage -
	    ///     gets a message that is coresponding to the
	    ///     exit code.
	    /// </summary>
	    public new string ExitMessage
	    {
		    get
		    {
			    return this._strExitMessage;
		    }
	    }


	    /// <summary>
	    /// property Arguments -
	    ///     get the external program's argument list
	    /// </summary>
	    public string Arguments
	    {
		    get
		    {
			    return this.GetArguments();
		    }
	    }

	    /// <summary>
	    /// property AllowGenerateException -
	    ///     sets a flag that tells whether IIsVDir should
	    ///     generate an exception automatically
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
				    this._enumIISOprCode = IISVDIR_OPR_CODE.IISVDIR_OPR_BOOLEAN_PARSING_ERROR;
				    this._strExitMessage = String.Format( this._strMessages[ this.ExitCode ], this.Name, this.ExitCode );
				    throw;
			    }
		    }
	    }

	    /// <summary>
	    /// set a flag to signal object to ingore any error it generates
	    /// </summary>
	    /// <remarks></remarks>
	    [Action("skiperror", Needed=false, Default="false")]
	    public string IgnoreError
	    {
		    set
		    {
			    try
			    {
				    this._bIgnoreError = bool.Parse( value );
			    }
			    catch ( Exception )
			    {
				    this._enumIISOprCode = IISVDIR_OPR_CODE.IISVDIR_OPR_BOOLEAN_PARSING_ERROR;
				    this._strExitMessage = String.Format( this._strMessages[ this.ExitCode ], this.Name, this.ExitCode );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
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

	    #endregion

	    #region public methods
	    /// <summary>
	    /// public override void Execute() -
	    ///     the override method will rewrite
	    ///     base class's Execute method for
	    ///     performing the special action required
	    ///     by it.  It will first examine the
	    /// </summary>
	    public override void Execute()
	    {

		    // setup arguemnt list for external program
		    // to execute and execute the external program
		    // base.ProgramArguments = GetArgumentList();
		    base.Execute();

		    // this._strPrevArgs = _sbProgArgs.ToString();
		    // _sbProgArgs = new StringBuilder();

		    // if the return code is not zero, then
		    // we have problem to create virtual directory
		    // on a given server's IIS webserver. Log message
		    // on event log database and exit the XInstall programs
		    if ( base.ProgramExitCode != 0 )
		    {
			    this._enumIISOprCode =
				IISVDIR_OPR_CODE.IISVDIR_OPR_OPERATION_FAILED;
			    this._strExitMessage =
				String.Format( this._strMessages[ this.ExitCode ],
					       this.Name, this._strAction, this.WebVirtualDirectory,
					       this.WebSiteName, this.TargetServer, this.ExitCode );
			    if ( !this._bIgnoreError )
			    {
				    throw new Exception( this.ExitMessage );
			    }

		    }
		    else
		    {
			    this._bIsCompleted = true;
		    }
	    }

	    #endregion

	    #region private methods

	    /// <summary>
	    /// private void Init() -
	    ///     Provide initial checking the existence of iisvidr.vbs
	    /// </summary>
	    private void Init()
	    {
		    FileInfo fi = new FileInfo( _strIIsVDirFullPath );
		    if ( !fi.Exists )
		    {
			    this._enumIISOprCode = IISVDIR_OPR_CODE.IISVDIR_OPR_EXTERNAL_PROGRAM_NOTFOUND;
			    this._strExitMessage = String.Format( this._strMessages[ this.ExitCode ], this.Name, _cntStrIIsVDir, this.ExitCode );
			    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode);
		    }
		    base.ProgramName = _strIIsVDirFullPath;
	    }


	    /// <summary>
	    /// private bool QueryWebSite( strWebSiteName )
	    ///     Query if a given virtual directory does
	    ///     exit.
	    /// </summary>
	    /// <param name="strWebSiteName">virtual directory to be checked</param>
	    /// <returns></returns>
	    private bool QueryWebSite ( string strWebSiteName )
	    {
		    this.Action                = "query";
		    this.WebVirtualDirectory   = strWebSiteName;
		    this.ProgramRedirectOutput = "true";
		    this.Execute();

		    return this.ExitCode == 0 ? true : false;
	    }

	    protected override string GetArguments()
	    {
		    string strArguments       = null;
		    StringBuilder sbProgArgs = new StringBuilder();

		    if ( base.ProgramName.IndexOf(_cntStrIIsVDir) == -1 )
		    {
			    base.ProgramName = _cntStrIIsVDir;
		    }

		    if ( this.TargetServer != null )
		    {
			    sbProgArgs.AppendFormat(" /s {0}", this.TargetServer);
		    }
		    if ( this.WebSiteUserName != null )
		    {
			    sbProgArgs.AppendFormat(" /u {0}", this.WebSiteUserName);
		    }
		    if ( this.WebSiteUserPassword != null )
		    {
			    sbProgArgs.AppendFormat(" /p {0}", this.WebSiteUserPassword);
		    }

		    switch ( _strAction )
		    {
		    case "create":
			    sbProgArgs.AppendFormat(" /create {0}", this.WebSiteName);
			    if ( this.WebFriendlyName != null )
			    {
				    sbProgArgs.AppendFormat(" {0}", this.WebFriendlyName);
			    }
			    if ( this.MapPath != null )
			    {
				    sbProgArgs.AppendFormat(" {0}", this.MapPath);
			    }
			    break;

		    case "delete":
			    sbProgArgs.AppendFormat(" /delete {0}/{1}/{2}",
						    this.WebSiteName, this.WebVirtualDirectory,
						    this.WebFriendlyName);
			    break;

		    case "query":
			    sbProgArgs.AppendFormat(" /query {0}/{1}", this.WebSiteName, this.WebVirtualDirectory);
			    break;

		    default:
			    this._enumIISOprCode = IISVDIR_OPR_CODE.IISVDIR_OPR_INVALID_ACTION_SPECIFIED;
			    this._strExitMessage = String.Format (this._strMessages[ this.ExitCode ], this.Name, this.ExitCode );
			    throw new Exception();
		    }

		    strArguments = sbProgArgs.ToString();

		    return strArguments;
	    }
	    #endregion

	    #region ICleanUp Members

	    /// <summary>
	    /// public override voide RemoveIt() -
	    ///     implement an ICleanUp interface RemoveIt()
	    ///     method to provide a way to remove created
	    ///     virtual directory
	    /// </summary>
	    public override void RemoveIt()
	    {
		    base.RemoveIt();
		    this.Action="delete";
		    this.Execute();
	    }

	    /// <summary>
	    /// property IsComplete
	    ///     get/set the state of Action
	    /// </summary>
	    public new bool IsComplete
	    {
		    get
		    {
			    return this._bIsCompleted;
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
			    return this.GetType().Name.ToLower();
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
