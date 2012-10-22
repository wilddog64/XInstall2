using System;
using System.Text;
using System.IO;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// class IISBack - this class wraps the iisback.vbs
    ///     into C#. It inherits the ExternalPrg class for
    ///     handling the external process.
    /// </summary>
    public class IISBack : ExternalPrg, ICleanUp, IAction
    {

	    #region constant variables
	    // the iisback.vbs vb script application
	    private const string _cntStrIIsBack   = "iisback.vbs";

	    private static string _strIIsBackFullPath = String.Empty;
	    #endregion

	    #region property methods variables
	    private string _strBackupServer   = null;
	    private string _strBackupFileName = null;
	    private string _strAction         = "backup";
	    private bool   _bAllowOverwrite   = false;
	    private bool   _bIsCompleted      = false;

	    #endregion

	    #region public constructors
	    /// <summary>
	    /// Constructor IISBack - the constructor
	    /// will initialize the external program
	    /// to be called and also check to make sure
	    /// called program does exist in the system.
	    /// </summary>
	    [Action("iisback", Needed=true)]
	    public IISBack() : base ()
	    {
		    _strIIsBackFullPath = Path.Combine( Environment.GetEnvironmentVariable( @"WINDIR" ), @"System32" ) + _cntStrIIsBack;

		    FileInfo fi = new FileInfo( _strIIsBackFullPath );
		    if ( !fi.Exists )
		    {
			    throw new Exception ( String.Format("{0} does not exist, abort", _strIIsBackFullPath));
		    }
		    base.ProgramName           = _cntStrIIsBack;
		    base.ProgramRedirectOutput = "true";
	    }


	    /// <summary>
	    /// Constructor IISBack - is an overloaded class.
	    ///     Allow caller to prvoide parameters and
	    ///     initialize the base class: ExternalPrg
	    /// </summary>
	    /// <param name="strArgs">the external program's argument</param>
	    /// <param name="strOutputFile">
	    ///     output file for writing external program's output
	    /// </param>
	    public IISBack( string strArgs, string strOutputFile ) :
	    base ( strArgs, strOutputFile )
	    {
		    FileInfo fi = new FileInfo( _strIIsBackFullPath );
		    if ( !fi.Exists )
		    {
			    base.FatalErrorMessage( ".", String.Format("{0}: {1} does not exist, abort", this.Name, _strIIsBackFullPath), 1660, 1);
		    }
		    base.ProgramName           = _cntStrIIsBack;
		    base.ProgramRedirectOutput = "true";

	    }
	    #endregion

	    #region public property methods

	    /// <summary>
	    /// property BackupFrom -
	    ///     get/set the IIS server we need to backup
	    /// </summary>
	    [Action("server", Needed=true)]
	    public string BackupFrom
	    {
		    get
		    {
			    return this._strBackupServer;
		    }
		    set
		    {
			    this._strBackupServer = value;
		    }
	    }


	    /// <summary>
	    /// property BackupFileName -
	    ///     get/set the backup file name
	    /// </summary>
	    [Action("backupfile", Needed=true)]
	    public string BackupFileName
	    {
		    get
		    {
			    if ( this._strBackupFileName == null )
			    {
				    base.FatalErrorMessage( ".", String.Format( "{0}: BackupFileName cannot be null!", this.Name ), 1660, 2);
			    }

			    return _strBackupFileName;
		    }
		    set
		    {
			    this._strBackupFileName = value;
		    }
	    }


	    /// <summary>
	    /// property OverwriteBackupFile -
	    ///     get/set the OverwriteBackupFile boolean value
	    ///     to determine if the backup file needs to be
	    ///     overwrited.
	    /// </summary>
	    [Action("allowoverwrite", Needed=false, Default=false)]
	    public string OverwriteBackupFile
	    {
		    get
		    {
			    return _bAllowOverwrite.ToString();
		    }
		    set
		    {
			    try
			    {
				    this._bAllowOverwrite = bool.Parse( value.ToString() );
			    }
			    catch ( Exception )
			    {
				    base.FatalErrorMessage( ".", "boolean variable parsing error", 1660, 3 );
			    }
		    }
	    }


	    /// <summary>
	    /// property LogFile -
	    ///     set/set the logfile to be used
	    /// </summary>
	    [Action("logfile", Needed=false, Default="auto")]
	    public string LogFile
	    {
		    get
		    {
			    return base.ProgramOutputFile;
		    }
		    set
		    {
			    base.ProgramOutputFile = value;
		    }
	    }


	    /// <summary>
	    /// property Action -
	    ///     get/set the action to be performed by IIsBack.vbs
	    ///     it can be either /backup or /restore
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
	    /// property StartTime -
	    ///     get the external program start time in
	    ///     short time format
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
	    ///    get the external program end time in
	    ///    short time format
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
	    ///     get the exit code returned by the
	    ///     external program
	    /// </summary>
	    [Action("exitcode", Needed=false)]
	    public new int ExitCode
	    {
		    get
		    {
			    return base.ProgramExitCode;
		    }
	    }

	    /// <summary>
	    /// property AllowGenerateException -
	    ///     set a flag that tells whether IIsBackup should
	    ///     generate an exception automatically.
	    /// </summary>
	    [Action("generateexception", Needed=false, Default=false)]
	    public new string AllowGenerateException
	    {
		    set
		    {
			    try
			    {
				    base.AllowGenerateException = bool.Parse( value.ToString() );
			    }
			    catch ( Exception )
			    {
				    base.FatalErrorMessage( ".", "boolean variable parsing error!", 1660, 4 );
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
	    /// set a flag to signal object to ingore any error it generates
	    /// </summary>
	    /// <remarks></remarks>
	    [Action("skiperror", Needed=false, Default="false")]
	    public new string SkipError
	    {
		    set
		    {
			    base.SkipError = bool.Parse( value );
		    }
	    }
	    #endregion

	    #region public methods
	    /// <summary>
	    /// public override void Execute() -
	    ///     override the base class's Execute()
	    ///     function.
	    /// </summary>
	    // public override void Execute()
	    // {
	    //     // get the program arguments and execute it
	    //     // by calling base class's Execute method
	    //     // base.ProgramArguments = this.Arguments;
	    //     base.Execute();

	    //     if ( this.ExitCode != 0 )
	    //     {
	    //         string strFatalMessage =
	    //             String.Format("{0} can't backup metabase for machine {1}",
	    //             this.ProgramName, this.BackupFrom);
	    //         base.FatalErrorMessage(".", strFatalMessage, 1660);
	    //     }
	    //     else
	    //         this._bIsCompleted = true;
	    // }

	    // public override void ParseActionElement()
	    // {
	    //     base.ParseActionElement ();
	    // }

	    #endregion

	    #region ICleanUp Members

	    /// <summary>
	    /// public override void RemoveIt() -
	    ///     a derived method from ICleanUp interface
	    ///     to provide the ability to perform a
	    ///     clean up operation when necessary.
	    /// </summary>
	    public override void RemoveIt()
	    {
		    // TODO:  Add IISBack.RemoveIt implementation
	    }
	    #endregion

	    #region private methods

	    /// <summary>
	    /// private string GetArguments() -
	    ///     construct a valid arguments by looking up
	    ///     each property that user fills in and returns
	    ///     it to the caller.
	    /// </summary>
	    /// <returns>a valid argument list</returns>
	    protected override string GetArguments()
	    {
		    // an argument list
		    StringBuilder sbProgArgs = new StringBuilder();

		    // check if server is given
		    if ( this.BackupFrom != null )
		    {
			    sbProgArgs.AppendFormat("/s {0}", this.BackupFrom);
		    }

		    // now we need to see what kind of action a user
		    // is asking for.
		    switch ( this._strAction )
		    {
		    case "backup":
			    sbProgArgs.AppendFormat(" /backup /b {0}", this.BackupFileName);
			    if ( this._bAllowOverwrite )
			    {
				    sbProgArgs.Append(" /overwrite");
			    }
			    break;
		    case "restore":
			    sbProgArgs.AppendFormat(" /restore /b {0}", this.BackupFileName);
			    break;
		    case "delete":
			    sbProgArgs.AppendFormat(" /delete /b {0} /v HIGHEST_VERSION");
			    break;
		    default:
			    base.FatalErrorMessage( ".",
						    String.Format("{0}: unknown action {0} specified!", this.Name, this._strAction),
						    1660,
						    -99);
			    break;
		    }

		    return sbProgArgs.ToString();
	    }
	    #endregion

	    #region IAction Members

	    /// <summary>
	    /// property ExitMessage -
	    ///     gets a message that is coresponding to the
	    ///     exit code.
	    /// </summary>
	    public new string ExitMessage
	    {
		    get
		    {
			    return base.ProgramOutput;
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
