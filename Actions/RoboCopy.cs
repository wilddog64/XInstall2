using System;
using System.IO;
using System.Text;
using System.Xml;

/*
 * Class Name    : RoboCopy
 * Inherient     : ExternalPrg
 * Functionality : a wrapper class for an external tool RoboCopy.exe
 *
 * Created Date  : May 2003
 * Created By    : mliang
 *
 * Change Log    :
 *
 * who              when            what
 * --------------------------------------------------------------------
 * mliang           05/01/2003      Initial creation
 * mliang           01/27/2005 v1   As contructor of ActionElement
 *                                  has changed, this caused a bug
 *                                  for RoboCopy as it cannot
 *                                  recognize user defined variable.
 *
 *                                  To fixed, the class itself needed
 *                                  to be able to accept an XmlNode
 *                                  object in and pass it all the way
 *                                  to its ancestor, ActionElement
 *                                  class to have this ability.
 *
 *                                  In this fix, the following things
 *                                  happened:
 *
 *                                  1. A default constructor is removed
 *                                  2. A constructor with XmlNode
 *                                     parameter is created and pass to
 *                                     its base class, which is
 *                                     ExternalPrg and it will pass this
 *                                     information to ActionElement.
 *                                  3. Set a default value of Files to be
 *                                     an empty value; otherwise, it will
 *                                     case an exception to be generated
 *                                     during the dyamic discovery
 *                                     process.
 *                                  4. Remove custom attributes from
 *                                     ExitCode and ExitMessage properites
 *
 * mliang          01/27/2005 v2    Add BasePath property so that Robocopy
 *                                  won't reply on environment variable.
 *
 * mliang          01/31/2005       When mirroring, make sure source directory
 *                                  is not empty or abort the action as it
 *                                  could easily destory the destination
 *                                  directory.
 *
 * mliang          02/01/2005       Minor bug fix on the validate source
 *                                  directory when /mir option is given.
 *                                  The problem is the logic to test the
 *                                  empty directory should both no
 *                                  subdirectories and files exist in a
 *                                  given source directory to be true, not
 *                                  either one of them are true!!
 *
 * mliang          02/03/2005       Make sure log entry only log the error
 *                                  message once, and also check if directory
 *                                  is empty when performing a move action (if
 *                                  a source directory is empty when moving,
 *                                  error out and log a entry in event log
 *                                  database).
 */





namespace XInstall.Core.Actions
{
    /// <summary>
    /// RoboCopy class wraps the call to external program Robocopy
    /// from NT resouce kit.
    /// </summary>
    public class RoboCopy : ExternalPrg
    {
	    #region private constant variables
	    private const string _cntStrRoboCopy = "robocopy.exe";
	    private const char   _cntCQuote      = '"';
	    #endregion

	    private XmlNode _ActionNode = null;

	    #region private property method variables
	    private string _strAction     = "mir";  // default to mirror dir trees
	    private string _strSourceDir  = null;   // source directory
	    private string _strDestDir    = null;   // destination directory
	    private string _strFiles      = null;   // files to copy if any specified
	    private string _CreateDestDir = string.Empty; // create Destination directory before copying
	    private string _BasePath      = string.Empty;
	    private string _CopySub       = string.Empty;
	    private string _CopyEmpty     = string.Empty;

	    private string _strOutputFile = null;
	    private string _iNumOfRetry   = "3";      // default retry times
	    private string _iWaitTime     = "15";     // default wait time for each retry
	    private string _TestOnly      = "false";  // instruct Robocopy echo the files/directories
	    // that will be copied
	    // but no actual action will be performed.

	    private bool   _bPreSEC       = true;   // preserve security info
	    private bool   _bRestartable  = true;   // network restartable file
	    private bool   _bNoProgress   = true;   // don't display robocopy progress. true by default

	    // error message
	    // enum type for the ROBOCOPY exit code
	    private enum ROBOCOPY_EXIT_CODE
	    {
		    ROBOCOPY_NOCOPY_HAPPENDED         = 0,
		    ROBOCOPY_COPY_SUCCESSFULL         = 1,
		    ROBOCOPY_EXTRAFILESDIRS_FOUND     = 2,
		    ROBOCOPY_MISMATCHED_FILESDIRS     = 4,
		    ROBOCOPY_COPYERROR_HAPPENED       = 8,
		    ROBOCOPY_INSUFFIENCET_PERMISSIONS = 16,
		    ROBOCOPY_SOURCEDIR_NOT_PROVIDED,
		    ROBOCOPY_EXTERNAL_PROGRAM_NOTFOUND,
		    ROBOCOPY_SOURCEDIR_DOESNOT_EXISTED,
		    ROBOCOPY_BOOLEAN_PARSE_ERROR,
		    ROBOCOPY_INTEGER_PARSE_ERROR,
		    ROBOCOPY_UNKNOWN_ACTION_SPECIFIED,
		    ROBOCOPY_UNABLE_REMOVING_DIRECTORY,
	    }


	    private string _strExitMessage     = null;
	    private string[] _strExitMessages  =
	    {
		    @"{0}: source dir {1} and destination dir {2} are not changed, no copy needed, exit code {3}",
		    @"{0}: successfully copy source dir {1} to destination dir {2}, exit code {3}",
		    @"{0}: extra directories/files found in source directory {1}, exit code {2}",
		    @"{0}: Mismatched files/directories detected, check log file {1}, housekeeping needed, exit code {2}",
		    @"{0}: some files/directories can't be copied, copy error or retry limit reached.  Check log file {1}, exit code {2}",
		    @"{0}: fatal error happen. Robocopy can't copy files/directories due to insufficient access privileges on the source or destination directories, exit code {1}",
		    @"{0}: source directory is not provided, exit {1}",
		    @"{0}: cannot find external program {1}, exit code {2}",
		    @"{0}: source directory {1} does not exist, exit code {2}",
		    @"{0}: boolean variable parsing error, exit code {2}",
		    @"{0}: integer variable parsing error, exit code {2}",
		    @"{0}: specified action {1} is unknown, exit code {2}",
		    @"{0}: unable remove destination directory {1}, message {2}, exit code {3}",
	    };

	    // an exit code enumeration variable
	    private ROBOCOPY_EXIT_CODE _enumRobocopyExitCode =
		ROBOCOPY_EXIT_CODE.ROBOCOPY_COPY_SUCCESSFULL;

	    private StringBuilder _sbProgArgs = new StringBuilder();
	    #endregion

	    [Action("robocopy")]
	    public RoboCopy( XmlNode ActionNode ) : base( ActionNode )
	    {
		    base.ProgramName           = _cntStrRoboCopy;
		    base.ProgramRedirectOutput = "true";
		    this._ActionNode           = ActionNode;

	    }


	    #region public property methods
	    /// <summary>
	    /// property Action -
	    ///     sets an action for robocopy to execute
	    /// </summary>
	    [Action("action", Needed=true)]
	    public string Action
	    {
		    set
		    {
			    _strAction = value;
		    }
	    }


	    /// <summary>
	    /// property SourceDirectory -
	    ///     set the source directory for Robocopy
	    ///     to copy from
	    /// </summary>
	    [Action("sourcedirectory", Needed=true)]
	    public string SourceDirectory
	    {
		    get
		    {
			    return this._strSourceDir;
		    }

		    set
		    {
			    if ( value != null )
			    {
				    this._strSourceDir = String.Format( "{0}", value );
				    base.LogItWithTimeStamp(
					String.Format( "robocopy: copy from {0}",
						       this._strSourceDir ) );
			    }
			    else
			    {
				    this._enumRobocopyExitCode =
					ROBOCOPY_EXIT_CODE.ROBOCOPY_SOURCEDIR_NOT_PROVIDED;
				    this._strExitMessage       =
					String.Format( this._strExitMessages[ this.ExitCode ],
						       this.Name, this.ExitCode );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
			    }
		    }
	    }


	    /// <summary>
	    /// property DestinationDirectory -
	    ///     sets the destination directory
	    ///     for robocopy to copy to.
	    /// </summary>
	    [Action("destinationdirectory", Needed=true)]
	    public string DestinationDirectory
	    {
		    get
		    {
			    return this._strDestDir;
		    }
		    set
		    {
			    _strDestDir = value;
			    base.LogItWithTimeStamp(
				String.Format( "robocopy:copy to {0}",
					       this._strDestDir ) );
		    }
	    }


	    /// <summary>
	    /// property RetryTimes -
	    ///     sets how may times should
	    ///     robocopy should try before
	    ///     it gives up.  Default is 3
	    ///     times.
	    /// </summary>
	    [Action("retrytimes", Needed=false, Default="3")]
	    public string RetryTimes
	    {
		    get
		    {
			    return _iNumOfRetry;
		    }
		    set
		    {
			    _iNumOfRetry = value;
		    }
	    }

	    [Action("testonly", Needed=false, Default="false")]
	    public string TestOnly
	    {
		    get
		    {
			    return this._TestOnly;
		    }
		    set
		    {
			    this._TestOnly = value;
		    }
	    }

	    /// <summary>
	    /// property WaitTime -
	    ///     sets time to wait for each retry.
	    ///     default to 15 seconds.
	    /// </summary>
	    [Action("waittime", Needed=false, Default="15")]
	    public string WaitTime
	    {
		    get
		    {
			    return _iWaitTime;
		    }
		    set
		    {
			    _iWaitTime = value;
		    }
	    }


	    /// <summary>
	    /// property OutputFile -
	    ///     sets the output log file for
	    ///     robocopy.
	    /// </summary>
	    [Action("outputfile", Needed=false, Default="auto")]
	    public new string OutputFile
	    {
		    get
		    {
			    return this._strOutputFile;
		    }
		    set
		    {
			    if ( value.Equals("auto") )
				    this._strOutputFile  = Directory.GetCurrentDirectory() +
							   Path.DirectorySeparatorChar                        +
							   @"logs"                                            +
							   Path.DirectorySeparatorChar                        +
							   Path.ChangeExtension(
							       Path.GetFileName (
								   Environment.GetCommandLineArgs()[0] ),
							       ".log" );
			    base.ProgramOutputFile  = this._strOutputFile;
		    }
	    }


	    /// <summary>
	    /// property PreserveSecInfo -
	    ///     set the boolean value that
	    ///     direct whether robocopy should
	    ///     preseve security information
	    ///     when copy files from source to
	    ///     destination. Default is on.
	    /// </summary>
	    [Action("preservesecinfo", Needed=false, Default="false")]
	    public string PreserveSecInfo
	    {
		    set
		    {
			    this._bPreSEC = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("noprogress", Needed=false, Default="true")]
	    public string NoProgress
	    {
		    set
		    {
			    _bNoProgress = bool.Parse(value.ToString());
		    }
	    }


	    /// <summary>
	    /// property Restartable -
	    ///     sets a boolean value that directs
	    ///     whether robocopy should perform
	    ///     network restartable copy when copy files
	    ///     from source to destination.   Default is
	    ///     on.
	    /// </summary>
	    [Action("restartable", Needed=false, Default="true")]
	    public string Restartable
	    {
		    set
		    {
			    this._bRestartable =
				bool.Parse( value.ToString() );
		    }

	    }


	    /// <summary>
	    /// property Files -
	    ///     sets files to be copied.  Multiple files
	    ///     should use comma as a sepeator.
	    /// </summary>
	    [Action("files", Needed=false, Default="")]
	    public string Files
	    {
		    get
		    {
			    return this._strFiles;
		    }
		    set
		    {
			    this._strFiles = value;
		    }
	    }


	    /// <summary>
	    /// property ExitCode -
	    ///     gets the return code from RoboCopy
	    /// </summary>
	    public new int ExitCode
	    {
		    get
		    {
			    return (int) this._enumRobocopyExitCode;
		    }
	    }


	    /// <summary>
	    /// property ExitMessage -
	    ///     gets the message associates with
	    ///     the return code from Robocopy.
	    /// </summary>
	    public new string ExitMessage
	    {
		    get
		    {
			    return this._strExitMessage;
		    }
	    }


	    /// <summary>
	    /// property AllowGenerateException -
	    ///     set a flag that tells the RoboCopy object
	    ///     should generate an exception or not.
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
				    this._enumRobocopyExitCode =
					ROBOCOPY_EXIT_CODE.ROBOCOPY_BOOLEAN_PARSE_ERROR;
				    this._strExitMessage     =
					String.Format( this._strExitMessages [ this.ExitCode ],
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


	    [Action("createdestdir", Needed=false, Default="false")]
	    public string CreateDestDir
	    {
		    get
		    {
			    return this._CreateDestDir;
		    }
		    set
		    {
			    this._CreateDestDir = value;
		    }
	    }



	    [Action("copysub", Needed=false, Default="false")]
	    public string CopySub
	    {
		    get
		    {
			    return this._CopySub;
		    }
		    set
		    {
			    this._CopySub = value;
		    }
	    }


	    [Action("copyempty", Needed=false, Default="false")]
	    public string CopyEmpty
	    {
		    get
		    {
			    return this._CopyEmpty;
		    }
		    set
		    {
			    this._CopyEmpty = value;
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


	    #endregion

	    #region public override methods

	    /// <summary>
	    /// public override void Execute() -
	    ///     carry out the robocopy action
	    /// </summary>
	    public override void Execute()
	    {
		    // supply the argument and execute the program
		    // base.ProgramArguments = this.Arguments;
		    base.Execute ();

		    // These exit code should raise an exception
		    if ( (ROBOCOPY_EXIT_CODE) base.ProgramExitCode ==
			    ROBOCOPY_EXIT_CODE.ROBOCOPY_COPYERROR_HAPPENED ||
			    (ROBOCOPY_EXIT_CODE) base.ProgramExitCode ==
			    ROBOCOPY_EXIT_CODE.ROBOCOPY_INSUFFIENCET_PERMISSIONS )
		    {
			    throw new Exception( this.ExitMessage );
		    }
	    }


	    #endregion

	    #region protected methods

	    protected override object ObjectInstance
	    {
		    get
		    {
			    return this;
		    }
	    }


	    #endregion

	    #region public methods

	    /// <summary>
	    /// public override void RemoveIt() -
	    ///     a derived method from ICleanUp interface
	    ///     to perform a clean up operation when
	    ///     requires.
	    /// </summary>

	    #endregion

	    #region private methods
	    /// <summary>
	    /// protected override string GetArguments() -
	    ///     return an arguments for the robocopy program
	    /// </summary>
	    /// <returns></returns>
	    protected override string GetArguments()
	    {

		    string strArguments = null;

		    // first construct source and destination directories
		    bool CreateDestDir = bool.Parse( this.CreateDestDir );
		    bool IncludeSub    = bool.Parse( this.CopySub );
		    bool IncludeEmpty  = bool.Parse( this.CopyEmpty );
		    bool TestOnly      = bool.Parse( this.TestOnly );

		    if ( CreateDestDir )
		    {
			    try
			    {
				    this._strDestDir = String.Format( @"\\{0}",
								      this._strDestDir.Trim( new char[] { '"', '\\' } ) );
				    if ( !Directory.Exists( this._strDestDir ) )
				    {
					    Directory.CreateDirectory( this._strDestDir );
				    }
			    }
			    catch ( Exception e )
			    {
				    this._strExitMessage = String.Format( "{0}: unable to create directory {1} - {2}",
									  base.Name, this._strDestDir, e.Message );
				    throw new Exception( this._strExitMessage );
			    }
		    }
		    this._sbProgArgs.AppendFormat(" {0} {1} ",
						  this._strSourceDir,
						  this._strDestDir);

		    // if files was supplied then we append files to our argument list
		    if ( this._strFiles != null )
		    {
			    this._sbProgArgs.AppendFormat( " {0} ", this._strFiles);
		    }

		    if ( TestOnly )
		    {
			    this._sbProgArgs.Append( " /L " );
		    }

		    if (_bNoProgress)
		    {
			    this._sbProgArgs.Append(" /NP ");
		    }

		    // now we need to determine each different action
		    string[] Dirs  = null;
		    string[] Files = null;
		    bool EmptyDir  = false;

		    switch ( _strAction )
		    {
			    // handling mirror operation
		    case "mir":
			    Dirs  = Directory.GetDirectories( this.SourceDirectory );
			    Files = Directory.GetFiles( this.SourceDirectory );
			    EmptyDir = Dirs.Length == 0 && Files.Length == 0;
			    if ( EmptyDir )
				    base.FatalErrorMessage( ".",
							    String.Format(
								"source directory {0} is empty, which could destory destinate dir",
								this.SourceDirectory ), 1660, false );
			    this._sbProgArgs.AppendFormat("/MIR /R:{0} /W:{1} ",
							  this.RetryTimes, this.WaitTime);
			    break;

			    // handling move operation
		    case "move":
			    Dirs  = Directory.GetDirectories( this.SourceDirectory );
			    Files = Directory.GetFiles( this.SourceDirectory );
			    EmptyDir = Dirs.Length == 0 && Files.Length == 0;
    //                        if ( EmptyDir )
    //                            base.FatalErrorMessage( ".",
    //                                String.Format(
    //                                "source directory {0} is empty, move action abort!",
    //                                this.SourceDirectory ), 1660, false );
			    this._sbProgArgs.AppendFormat("/MOVE /R:{0} /W:{1} ",
							  this.RetryTimes, this.WaitTime);
			    if ( IncludeSub )
			    {
				    this._sbProgArgs.Append( "/S " );
			    }
			    if ( IncludeEmpty )
			    {
				    this._sbProgArgs.Append( "/E " );
			    }
			    break;

			    // handling copy sub/empty directories
		    case "copysub":
			    this._sbProgArgs.AppendFormat("/S /E /R:{0} /W:{1} ",
							  this.RetryTimes, this.WaitTime);
			    break;

			    // purge the files in destination that are no longer in
			    // source directory
		    case "purge":
			    this._sbProgArgs.AppendFormat("/PURGE /R:{0} /W:{1} ",
							  this.RetryTimes, this.WaitTime);
			    break;

			    // create directory structure and zero length files in the
			    // destination directory
		    case "create":
			    this._sbProgArgs.Append("/CREATE");
			    break;
		    default:
			    this._enumRobocopyExitCode =
				ROBOCOPY_EXIT_CODE.ROBOCOPY_UNKNOWN_ACTION_SPECIFIED;
			    this._strExitMessage       =
				String.Format( this._strExitMessages[ this.ExitCode ],
					       this.Name, this._strAction, this.ExitCode );
			    base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
			    break;
		    }

		    // check other boolean variables:
		    // if requires to preserve the security information then
		    // append /SEC into our list
		    if ( this._bPreSEC )
		    {
			    this._sbProgArgs.Append("/SEC ");
		    }

		    // if requires to copy restartable file then append /Z into it
		    if ( this._bRestartable )
		    {
			    this._sbProgArgs.Append("/Z ");
		    }

		    strArguments = this._sbProgArgs.ToString();
		    this._sbProgArgs.Remove(0, strArguments.Length);
		    return strArguments;
	    }


	    #endregion

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


	    #region IAction Members

	    public new bool IsComplete
	    {
		    get
		    {
			    return base.IsComplete;
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
    }
}
