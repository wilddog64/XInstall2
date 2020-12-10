using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// providing an ability for XInstall to call external program
    /// </summary>
    /// <remarks>
    ///     CALL class inherits from ExternalPrg and IAction Interface
    ///     for general acess from the XML configuration file
    /// </remarks>
    public class EXEC : ExternalPrg
    {
	    // an imuration for the CALL operations
	    private enum CALL_OPR_CODE
	    {
		    CALL_OPR_EXECUTE_SUCCESSFUL,
		    CALL_OPR_EXECUTE_FAILED,
		    CALL_OPR_EXTERNAL_PROGRAM_NOTEXIST,
		    CALL_OPR_START_EXECUTING,
		    CALL_OPR_DIRECTORY_NOTEXIST,
	    }


	    private string _arguments              = String.Empty;
	    private string _BasePath               = String.Empty;
	    private readonly string _roStrCurrDir  = Directory.GetCurrentDirectory();

	    // program name
	    private string _rostrProgName;

	    // error handling variables
	    private CALL_OPR_CODE _enumCallOprCode = CALL_OPR_CODE.CALL_OPR_EXECUTE_SUCCESSFUL;
	    private string _strExitMessage         = null;
	    private string[] _strMessages          =
	    {
		    @"{0}: executing program {1} complete successfully",
		    @"{0}: unable to execute program {1}, output from external program, {2}",
		    @"{0}: program {1} does not exist!",
		    @"{0}: start executing {1}",
		    @"{0}: directory - {1} does not exist!",
	    };

	    /// <summary>
	    /// an constructor that initialize the CALL class
	    /// </summary>
	    /// <remarks>
	    ///     By default, when using CALL object the
	    ///     properties OutToConsole and OutToFile that
	    ///     derives from ExternalPrg is set to true so
	    ///     that all messages from external program will
	    ///     be redirected to console and log file.
	    /// </remarks>
	    [Action("exec")]
	    public EXEC( XmlNode ActionNode ) : base( ActionNode )
	    {
		    base.OutToConsole          = true;
		    base.OutToFile             = true;
		    base.ProgramRedirectOutput = "true";
		    base.ProgramName           = Environment.GetEnvironmentVariable( "comspec" );
	    }


	    /// <summary>
	    /// Name of an external program to be called.
	    /// </summary>
	    /// <remarks>
	    ///     If full path of the external program is not provided,
	    ///     then CALL object will assume it is located at the
	    ///     same location from the calling program.
	    /// </remarks>
	    [Action("progname", Needed=true)]
	    public string ProgName
	    {
		    get
		    {
			    return this._rostrProgName;
		    }
		    set
		    {
			    string strProgramName = Path.GetFileName( value );
			    string strProgramPath = Path.GetDirectoryName( value );

			    bool bOK = strProgramPath != null && strProgramPath != String.Empty;

			    if ( bOK )
			    {
				    this.BasePath = strProgramPath;
			    }

			    if ( !File.Exists( value ) )
			    {
				    this.SetExitMessage( CALL_OPR_CODE.CALL_OPR_EXTERNAL_PROGRAM_NOTEXIST, this.Name, value );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
			    }
			    this._rostrProgName = strProgramName;
			    base.ProgramName = this._rostrProgName;

		    }
	    }


	    protected override object ObjectInstance
	    {
		    get
		    {
			    return this;
		    }
	    }

	    /// <summary>
	    /// Arguments that are passed to the calling external
	    /// program.
	    /// </summary>
	    /// <remarks>
	    ///     CALL object does not verify the arguments passed
	    ///     to the external program.  It is external program's
	    ///     responsibility to verify the accuracy of arguments.
	    /// </remarks>
	    [Action("arguments", Needed=false)]
	    public string Arguments
	    {
		    get
		    {
			    return this._arguments;
		    }
		    set
		    {
			    this._arguments = value;
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


	    [Action("skiperror", Needed=false, Default="false")]
	    public new string SkipError
	    {
		    set
		    {
			    base.SkipError = bool.Parse( value );
		    }
	    }


	    [Action("basepath", Needed=false, Default=".")]
	    public override string BasePath
	    {
		    get
		    {
			    return this._BasePath;
		    }
		    set
		    {
			    this._BasePath = value;
		    }
	    }


	    #region IAction Members

	    protected override void ParseActionElement()
	    {
		    // base.ParseActionElement();
		    string OldDirectory = Environment.CurrentDirectory;
		    if ( this.BasePath != null )
		    {
			    Directory.SetCurrentDirectory( this.BasePath );
			    base.ProgramWorkingDirectory = this.BasePath;
		    }

		    this.SetExitMessage( CALL_OPR_CODE.CALL_OPR_START_EXECUTING, this.Name, this.ProgName );
		    base.LogItWithTimeStamp( this.ExitMessage );

		    try
		    {
			    base.ParseActionElement();

			    if ( base.ProgramExitCode != 0 )
			    {
				    base.IsComplete = false;
				    this.SetExitMessage( CALL_OPR_CODE.CALL_OPR_EXECUTE_FAILED, this.Name, this.ProgName, base.ProgramOutput );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
			    }
			    else
			    {
				    base.IsComplete = true;
				    this.SetExitMessage( CALL_OPR_CODE.CALL_OPR_EXECUTE_SUCCESSFUL, this.Name, this.ProgName );
			    }
		    }
		    finally
		    {
			    Directory.SetCurrentDirectory( OldDirectory );
		    }
	    }

	    /// <summary>
	    /// gets the status of executing external program.
	    /// </summary>
	    /// <remarks>
	    ///     this function is not called directly and it is
	    ///     readonly.
	    /// </remarks>
	    public new bool IsComplete
	    {
		    get
		    {
			    return base.IsComplete;
		    }
	    }


	    /// <summary>
	    /// gets the status code from the execution of external program
	    /// </summary>
	    /// <remarks></remarks>
	    public new int ExitCode
	    {
		    get
		    {
			    return (int) this._enumCallOprCode;
		    }
	    }


	    /// <summary>
	    /// The return message corrsponding to the ExitCode
	    /// </summary>
	    /// <remarks></remarks>
	    public new string ExitMessage
	    {
		    get
		    {
			    return this._strExitMessage;
		    }
	    }


	    /// <summary>
	    /// Name of the object which is CALL.
	    /// </summary>
	    /// <remarks></remarks>
	    public new string Name
	    {
		    get
		    {
			    return this.GetType().Name;
		    }
	    }

	    protected override string ObjectName
	    {
		    get
		    {
			    return this.Name;
		    }
	    }


	    #endregion

	    /// <summary>
	    /// Setup the return message that corresponding to the exit code.
	    /// </summary>
	    /// <param name="CallOprCode">
	    ///     an enumeration type of CALL_OPR_CODE that uses to lookup
	    ///     corresponding message
	    /// </param>
	    /// <param name="objParams">
	    ///     Parameters that uses to format the
	    ///     return message
	    /// </param>
	    /// <remarks>
	    ///     SetExitMessage use CallOprCode for looking up a particular message
	    ///     from a message table.
	    /// </remarks>
	    private void
	    SetExitMessage (
		CALL_OPR_CODE CallOprCode,
		params object[] objParams )
	    {
		    this._enumCallOprCode = CallOprCode;
		    this._strExitMessage  = String.Format( this._strMessages[ this.ExitCode ], objParams );
	    }

	    protected override string GetArguments()
	    {
		    string[] strParams     = Regex.Split( this._arguments, @"[,]");
		    // string strParam        = null;
		    StringBuilder sbParams = new StringBuilder();

		    // sbParams.Append("/c ");
		    for ( int i = 0; i < strParams.Length; i++)
		    {
			    if ( strParams[i].IndexOf( Path.DirectorySeparatorChar ) > -1 )
			    {
				    strParams[i] = strParams[i].Trim();
				    if ( strParams[i].StartsWith( @"." ) )
				    {
					    strParams[i] = this.BasePath + strParams[i].Remove(0, 1);
				    }
				    else if ( strParams[i].StartsWith( @"/" ) ) {}
				    // else Win32API.GetShortPathName( strParams[i], out strParam );
				    //    sbParams.AppendFormat( @"{0} ", strParams[i] );
			    }
			    else
			    {
				    sbParams.AppendFormat( @"{0} ", strParams[i] );
			    }
		    }
		    return sbParams.ToString();
	    }
    }
}
