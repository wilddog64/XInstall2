using System;
using System.IO;
using System.Text;
using System.Xml;

/*
 * Class Name    : Rsync
 * Inherient     : ExternalPrg
 * Functionality : a wrapper class for an external tool Rsync.exe
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
 *                                  for Rsync as it cannot
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
    /// Rsync class wraps the call to external program Robocopy
    /// from NT resouce kit.
    /// </summary>
    public class Rsync : ExternalPrg
    {
	    #region private constant variables
	    private const string _cntStrRsync = "rsync";
	    private const char   _cntCQuote      = '"';
	    #endregion

	    private XmlNode _ActionNode = null;

	    #region private property method variables
	    private string _strSourceDir  = null;         // source directory
	    private string _strDestDir    = null;         // destination directory
	    private string _CreateDestDir = string.Empty; // create Destination directory before copying
	    private string _BasePath      = string.Empty;
	    private string _partialDir    = string.Empty;
	    private string _excluded      = string.Empty;
	    private string _included      = string.Empty;

	    // boolean options for Rsync
	    private bool _archiveOn         = true;   // arcive mode
	    private bool _compress          = false;  // compress files
	    private bool _delayUpdates      = false;  // put all update at the end of transfer
	    private bool _delete            = false;  // delete extra files
	    private bool _deleteAfter       = false;  // receiver deletes after the transfer
	    private bool _deleteBefore      = false;  // receiver deletes before transfer
	    private bool _deleteDelay       = false;  // find deletions during, delete after
	    private bool _deleteDuring      = false;  // receiver deletes during the transfer
	    private bool _deleteExcluded    = false;  // also delete excluded files
	    private bool _dryRun            = false;  // rync will report what's happening but not actually doing anything
	    private bool _itemize           = false;  // output change-summary for all updates
	    private bool _partial           = false;  // keep partially transferred files
	    private bool _progress          = false;  // show pogress
	    private bool _reserveModifyTime = true;   // reserve modification time
	    private bool _stats             = false;  // show transfer status
	    private bool _verbose           = true;   // verbose


	    // error message
	    // enum type for the RSYNC exit code
	    private enum RSYNC_EXIT_CODE
	    {
		    RSYNC_COPY_SUCCESSFULL,
		    RSYNC_EXTRAFILESDIRS_FOUND,
		    RSYNC_MISMATCHED_FILESDIRS,
		    RSYNC_COPYERROR_HAPPENED,
		    RSYNC_INSUFFIENCET_PERMISSIONS,
		    RSYNC_SOURCEDIR_NOT_PROVIDED,
		    RSYNC_EXTERNAL_PROGRAM_NOTFOUND,
		    RSYNC_SOURCEDIR_DOESNOT_EXISTED,
		    RSYNC_BOOLEAN_PARSE_ERROR,
		    RSYNC_INTEGER_PARSE_ERROR,
		    RSYNC_UNKNOWN_ACTION_SPECIFIED,
		    RSYNC_UNABLE_REMOVING_DIRECTORY,
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
	    private RSYNC_EXIT_CODE _enumRobocopyExitCode = RSYNC_EXIT_CODE.RSYNC_COPY_SUCCESSFULL;
	    private StringBuilder _sbProgArgs             = new StringBuilder();

	    #endregion

	    [Action("rsync")]
	    public Rsync( XmlNode ActionNode ) : base( ActionNode )
	    {
		    base.ProgramName           = _cntStrRsync;
		    base.ProgramRedirectOutput = "true";
		    this._ActionNode           = ActionNode;

	    }


	    #region public property methods

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
				    base.LogItWithTimeStamp( String.Format( "rsync: copy from {0}", this._strSourceDir ) );
			    }
			    else
			    {
				    this._enumRobocopyExitCode = RSYNC_EXIT_CODE.RSYNC_SOURCEDIR_NOT_PROVIDED;
				    this._strExitMessage       = String.Format( this._strExitMessages[ this.ExitCode ], this.Name, this.ExitCode );
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
			    base.LogItWithTimeStamp( String.Format( "rsync:copy to {0}", this._strDestDir ) );
		    }
	    }

	    [Action("archive", Needed=false, Default="true")]
	    public string Archive
	    {
		    set
		    {
			    _archiveOn = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("compress", Needed=false, Default="false")]
	    public string Compress
	    {
		    set
		    {
			    _compress = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("delayUpdates", Needed=false, Default="false")]
	    public string delayUpdates
	    {
		    set
		    {
			    _delayUpdates = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("delete", Needed=false, Default="false")]
	    public string delete
	    {
		    set
		    {
			    _delete = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("deleteAfter", Needed=false, Default="false")]
	    public string deleteAfter
	    {
		    set
		    {
			    _deleteAfter = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("deleteBefore", Needed=false, Default="false")]
	    public string deleteBefore
	    {
		    set
		    {
			    _deleteBefore = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("deleteDelay", Needed=false, Default="false")]
	    public string deleteDelay
	    {
		    set
		    {
			    _deleteDelay = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("deleteDuring", Needed=false, Default="false")]
	    public string deleteDuring
	    {
		    set
		    {
			    _deleteDuring = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("deleteExcluded", Needed=false, Default="false")]
	    public string deleteExcluded
	    {
		    set
		    {
			    _deleteExcluded = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("itemizeChanges", Needed=false, Default="false")]
	    public string itemizeChanges
	    {
		    set
		    {
			    _itemize = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("partial", Needed=false, Default="false")]
	    public string partial
	    {
		    set
		    {
			    _partial = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("progress", Needed=false, Default="false")]
	    public string progress
	    {
		    set
		    {
			    _progress = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("reserveModifyTime", Needed=false, Default="false")]
	    public string reserveModifyTime
	    {
		    set
		    {
			    _reserveModifyTime = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("stats", Needed=false, Default="false")]
	    public string stats
	    {
		    set
		    {
			    _stats = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("verbose", Needed=false, Default="false")]
	    public string verbose
	    {
		    set
		    {
			    _verbose = bool.Parse( value.ToString() );
		    }
	    }

	    [Action("dryRun", Needed=false, Default="false")]
	    public string dryRun
	    {
		    set
		    {
			    _dryRun = bool.Parse( value.ToString() );
		    }
	    }

	    /// <summary>
	    /// property exlcudedFiles
	    /// get/set files need to be excluded during the rsync copying
	    /// </summary>
	    [Action("excludeFiles", Needed=false, Default="")]
	    public string ExcludeFiles
	    {
		    get
		    {
			    return this._excluded;
		    }
		    set
		    {
			    this._excluded = value;
		    }
	    }

	    [Action("includeFiles", Needed=false, Default="")]
	    public string IncludeFiles
	    {
		    get
		    {
			    return this._included;
		    }
		    set
		    {
			    this._included = value;
		    }
	    }


	    /// <summary>
	    /// property ExitCode -
	    ///     gets the return code from Rsync
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
	    ///     set a flag that tells the Rsync object
	    ///     should generate an exception or not.
	    /// </summary>
	    [Action("generateexception", Needed=false, Default="false")]
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
				    this._enumRobocopyExitCode = RSYNC_EXIT_CODE.RSYNC_BOOLEAN_PARSE_ERROR;
				    this._strExitMessage       = String.Format( this._strExitMessages [ this.ExitCode ], this.Name, this.ExitCode );
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
		    if ( (RSYNC_EXIT_CODE) base.ProgramExitCode == RSYNC_EXIT_CODE.RSYNC_COPYERROR_HAPPENED ||
			    (RSYNC_EXIT_CODE) base.ProgramExitCode == RSYNC_EXIT_CODE.RSYNC_INSUFFIENCET_PERMISSIONS )
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

		    if ( CreateDestDir )
		    {
			    try
			    {
				    this._strDestDir = String.Format( @"\\{0}", this._strDestDir.Trim( new char[] { '"', '\\' } ) );
				    if ( !Directory.Exists( this._strDestDir ) )
				    {
					    Directory.CreateDirectory( this._strDestDir );
				    }
			    }
			    catch ( Exception e )
			    {
				    this._strExitMessage = String.Format( "{0}: unable to create directory {1} - {2}", base.Name, this._strDestDir, e.Message );
				    throw new Exception( this._strExitMessage );
			    }
		    }

		    _sbProgArgs.Append( "-" );
		    if ( _archiveOn )   // archive is on
		    {
			    _sbProgArgs.Append( "a" );
		    }

		    if ( _compress )   // compress is on
		    {
			    _sbProgArgs.Append( "z" );
		    }

		    if ( _dryRun )   // show what rsync does but not actually doing anything
		    {
			    _sbProgArgs.Append( "n" );
		    }

		    if ( _progress )   // show progress
		    {
			    _sbProgArgs.Append( "P" );
		    }

		    if ( _itemize )   // itemize
		    {
			    _sbProgArgs.Append( "i" );
		    }

		    if ( _delayUpdates )   // delay update is on
		    {
			    _sbProgArgs.Append( " --delay-update " );
		    }

		    if ( _delete )   // enable deletion
		    {
			    _sbProgArgs.Append( " --delete " );
		    }

		    if ( _deleteAfter )   // enable delete after
		    {
			    _sbProgArgs.Append( " --delete-after " );
		    }

		    if ( _deleteBefore )   // enable delete before
		    {
			    _sbProgArgs.Append( " --delete-before " );
		    }

		    if ( _deleteDelay )   // enable delay deletion
		    {
			    _sbProgArgs.Append( " --delete-delay " );
		    }

		    if ( _deleteDuring )   // enable delete during
		    {
			    _sbProgArgs.Append( " --delete-during " );
		    }

		    if ( _deleteExcluded )   // enable delete excluded files at destination
		    {
			    _sbProgArgs.Append( " --delete-excluded " );
		    }

		    if ( _partial )   //
		    {
			    _sbProgArgs.Append( " --partial " );
		    }

		    this._sbProgArgs.AppendFormat(" {0} {1} ", this._strSourceDir, this._strDestDir);
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
