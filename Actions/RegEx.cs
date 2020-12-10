using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using XInstall.Util.Log;


namespace XInstall.Core.Actions
{
    /// <summary>
    /// A class that provides an regular expression
    /// ability for searching and replacing the target
    /// string in a given file.
    /// </summary>
    public class RE : ActionElement, IAction, ICleanUp
    {

	    private Regex     _reThisRegEx       = null;
	    private FileInfo[] _fiFileInfos      = null;
	    private string    _strAction         = null;
	    private string    _strRePattern      = null;
	    private string    _strReplacement    = null;
	    private string    _strIdentifyBy     = null;
	    private ArrayList _alFiles           = null;

	    private enum REGEX_OPERATION_CODE
	    {
		    REGEX_OPR_SUCCESS       = 0,
		    REGEX_OPR_DIR_NOTFOUND,
		    REGEX_OPR_FILE_NOTFOUND,
		    REGEX_OPR_PATTERN_CANNOT_COMPILE,
		    REGEX_OPR_NO_SEARCHSTRING_SPECIFIED,
		    REGEX_OPR_UNKNOWN_ACTION,
		    REGEX_OPR_FILENAME_INVALID,
		    REGEX_OPR_FILECOPY_SECURITY_ERROR,
		    REGEX_OPR_FILEMOVETO_DIFFERENT_DRIVE,
		    REGEX_OPR_FILEPATH_TOOLONG,
		    REGEX_OPR_BACKUPFILE_NOTFOUND,
		    REGEX_OPR_RECOVER_MISSING_FILE,
	    };
	    private REGEX_OPERATION_CODE _enumRegExOpCode =
		REGEX_OPERATION_CODE.REGEX_OPR_SUCCESS;
	    private string[] _strMessages =
	    {
		    @"{0}: successfully execute action {1}, exit code {2}",
		    @"{0}: given directory {1} does not exist, exit code {2}",
		    @"{0}: given file(s) does not exist in directory {1}, exit code {2}",
		    @"{0}: given regex pattern {1} cannot be compiled, exit code {2}",
		    @"{0}: replace string is not given when spcified a replace action, exit code {1}",
		    @"{0}: specified action {1} is unknown, exit code {2}",
		    @"{0}: unable to open file, file name is invalid, exit code {2}",
		    @"{0}: user {1} does not have sufficient permssion to carry out copy operation",
		    @"{0}: file {1} is being move to different drive, exit code {2}",
		    @"{0}: path {1} is more than 248 characters, exit code {2}",
		    @"{0}: backup file {1} cannot be found, program abort {2}, exit code {3}",
		    @"{0}: restore file {1} from backup file {2}, code {3}",
	    };
	    private string   _strMessage  = null;

	    /// <summary>
	    /// public RE() -
	    ///     a Regular Expression constructor that initializs
	    ///     the state of RE object.
	    /// </summary>
	    [Action("regex")]
	    public RE()
	    {
		    this._strMessage  = this._strMessages[ (int) _enumRegExOpCode ];
		    this._strAction   = "replace";
		    this._alFiles     = new ArrayList();
		    base.OutToFile    = true;
		    base.OutToConsole = true;
	    }

	    /// <summary>
	    /// property SearchPattern -
	    ///     sets a regular expression pattern
	    ///     to be search for.
	    /// </summary>
	    [Action("searchpattern", Needed=true)]
	    public string SearchPattern
	    {
		    get
		    {
			    return this._strRePattern;
		    }
		    set
		    {
			    this._strRePattern = value;
			    Regex reRegEx      = null;
			    try
			    {
				    // create a regular expresion object
				    // with pre-compile and case-insensitiy
				    // options on
				    reRegEx = new Regex( this._strRePattern,
							 RegexOptions.Compiled   |
							 RegexOptions.IgnoreCase );
			    }
			    catch ( ArgumentException )     // capture possible compilation error
			    {
				    this._enumRegExOpCode =
					REGEX_OPERATION_CODE.REGEX_OPR_PATTERN_CANNOT_COMPILE;
				    this._strMessage =
					String.Format( this._strMessages[ (int) this._enumRegExOpCode ],
						       this.Name, this._strRePattern );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
			    }
			    this._reThisRegEx = reRegEx;
		    }
	    }

	    /// <summary>
	    /// property ReplaceWith -
	    ///     get/set a string that used to
	    ///     replace with found pattern target.
	    /// </summary>
	    [Action("replacewith", Needed=false)]
	    public string ReplaceWith
	    {
		    get
		    {
			    return this._strReplacement;
		    }
		    set
		    {
			    this._strReplacement = value;
		    }
	    }

	    [Action("identifyby", Needed=false, Default="")]
	    public string IdentifyBy
	    {
		    get
		    {
			    return this._strIdentifyBy;
		    }
		    set
		    {
			    this._strIdentifyBy = value;
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
	    /// property Files -
	    ///     set files to be searched and replaced for.
	    /// </summary>
	    /// <remarks>
	    /// this property will take the following steps in order to
	    /// perform a search/replace operation:
	    ///
	    ///     1. Retrives the files that user wants to do search/replace
	    ///     2. Backup files with .bak.
	    ///     3. Stores files into an array.
	    ///
	    /// Note: the Files property can accepts a file pattern search for
	    ///       looking up files. For example, given action*.cs will search
	    ///       all the files that has action as a prefix and .cs as subfix.
	    /// </remarks>
	    [Action("file", Needed=true)]
	    public string Files
	    {
		    set
		    {
			    // retrieving file name and path components.
			    string strFilePattern = Path.GetFileName( value );
			    string strDirPath     = Path.GetDirectoryName( value );

			    // check if a given path does exist.
			    DirectoryInfo di = new DirectoryInfo( strDirPath );
			    if ( !di.Exists )
			    {
				    this._enumRegExOpCode =
					REGEX_OPERATION_CODE.REGEX_OPR_DIR_NOTFOUND;
				    this._strMessage      =
					String.Format( this._strMessages[ (int) this._enumRegExOpCode ],
						       this.Name, strDirPath, this.ExitCode );
				    // throw new Exception( this.ExitMessage );
				    base.FatalErrorMessage( ".", this.ExitMessage, this.ExitCode );
			    }

			    // now retrieving all the files.
			    this._fiFileInfos  = di.GetFiles( strFilePattern );
			    try
			    {
				    // perform a backup operation and also delete the
				    // original files ... we'll read from backup files
				    // later but first, we will save file information
				    // in our internal array.
				    foreach ( FileInfo fi in this._fiFileInfos )
				    {
					    // string strBackupFileName = Path.ChangeExtension( fi.FullName, ".bak" );
					    this._alFiles.Add( fi );

				    }
			    }
			    // capture all possible exceptions.
			    catch ( ArgumentNullException )
			    {
				    this._enumRegExOpCode =
					REGEX_OPERATION_CODE.REGEX_OPR_FILENAME_INVALID;
				    this._strMessage      =
					String.Format(
					    this._strMessages[ (int) this._enumRegExOpCode ],
					    this.Name, this.ExitCode);
				    throw new Exception( this.ExitMessage );
				    // base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
			    catch ( ArgumentException )
			    {
				    this._enumRegExOpCode =
					REGEX_OPERATION_CODE.REGEX_OPR_FILENAME_INVALID;
				    this._strMessage      =
					String.Format(
					    this._strMessages[ (int) this._enumRegExOpCode ],
					    this.Name, this.ExitCode);
				    throw new Exception( this.ExitMessage );
				    // base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
			    catch ( System.Security.SecurityException )
			    {
				    this._enumRegExOpCode =
					REGEX_OPERATION_CODE.REGEX_OPR_FILECOPY_SECURITY_ERROR;
				    this._strMessage      =
					String.Format( this._strMessages[ (int) this._enumRegExOpCode ],
						       this.Name, Environment.UserDomainName, this.ExitCode);
				    throw new Exception( this.ExitMessage );
				    // base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
			    catch ( System.UnauthorizedAccessException uae )
			    {
				    this._enumRegExOpCode =
					REGEX_OPERATION_CODE.REGEX_OPR_FILEMOVETO_DIFFERENT_DRIVE;
				    this._strMessage      =
					String.Format( this._strMessages[ (int) this._enumRegExOpCode ],
						       this.Name, uae.Message, this.ExitCode );
				    throw new Exception( this.ExitMessage );
				    // base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
			    catch ( PathTooLongException ptle )
			    {
				    this._enumRegExOpCode =
					REGEX_OPERATION_CODE.REGEX_OPR_FILEPATH_TOOLONG;
				    this._strMessage      =
					String.Format( this._strMessages[ (int) this._enumRegExOpCode ],
						       this.Name, ptle.Message, this.ExitCode );
				    throw new Exception( this.ExitMessage );
				    // base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
			    catch ( NotSupportedException )
			    {
				    this._enumRegExOpCode =
					REGEX_OPERATION_CODE.REGEX_OPR_FILENAME_INVALID;
				    this._strMessage      =
					String.Format(
					    this._strMessages[ (int) this._enumRegExOpCode ],
					    this.Name, this.ExitCode);
				    throw new Exception( this.ExitMessage );
				    // base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
	    }

	    /// <summary>
	    /// private void Replace() -
	    ///     perform an actual search/replace operation against
	    ///     give files.
	    /// </summary>
	    private void Replace()
	    {
		    // if the replacement string is not found, then
		    // throw an exception.
		    if ( this.ReplaceWith == null ||
			    this.ReplaceWith  == String.Empty )
		    {
			    this._enumRegExOpCode =
				REGEX_OPERATION_CODE.REGEX_OPR_NO_SEARCHSTRING_SPECIFIED;
			    this._strMessage      =
				String.Format( this._strMessages[ (int) this._enumRegExOpCode ],
					       this.Name, this.ExitCode );
			    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
		    }

		    // prepare for search and replace.
		    // initialize StreamReader and StreamWriter objects
		    StreamReader sr = null;
		    StreamWriter sw = null;
		    for ( int i = 0; i < this._alFiles.Count; i++ )
		    {
			    // creating StreamReader and StreamWriter for
			    // a given file
			    string strStringModified = null;
			    string strFileName = ( this._alFiles[ i ] as FileInfo ).FullName;
			    string strBackupFileName =
				Path.ChangeExtension( ( this._alFiles[ i ] as FileInfo ).FullName, ".bak" );
			    if ( !File.Exists( strBackupFileName ) )
			    {
				    File.Copy ( strFileName, strBackupFileName, true );
			    }
			    sr = new StreamReader( strBackupFileName );
			    sw = new StreamWriter( strFileName );

			    // now processing the input string from the file
			    string s = null;
    //                bool MatchOn = false;
    //                if ( this.IdentifyBy.Length != 0 )
    //                    MatchOn = true;
			    while ( (s = sr.ReadLine()) != null )
			    {
				    // check if string contains the pattern we want,
				    // and if so replace it and write to file;
				    // otherwise simple copy string from backup file
				    // to new file.

				    //if ( this.IdentifyBy != )

				    Match m = this._reThisRegEx.Match( s );
				    if ( m.Success )
				    {
					    strStringModified =
						this._reThisRegEx.Replace( this.SearchPattern, this.ReplaceWith );
					    sw.WriteLine( strStringModified );
					    base.LogItWithTimeStamp(
						String.Format( "{0} was replaced by {1}",
							       this.SearchPattern, this.ReplaceWith ) );
				    }
				    else
				    {
					    sw.WriteLine( s );
				    }
			    }
		    }

		    // close all the stream objects.
		    sr.Close();
		    sw.Close();

	    }

	    #region IAction Members

	    protected override void ParseActionElement()
	    {
		    switch ( this._strAction )
		    {
		    case "replace":
			    this.Replace();
			    break;
		    default:
			    this._enumRegExOpCode =
				REGEX_OPERATION_CODE.REGEX_OPR_UNKNOWN_ACTION;
			    this._strMessage      =
				String.Format( this._strMessages[ (int) this._enumRegExOpCode ],
					       this.Name, this._strAction, this.ExitCode );
			    throw new Exception( this.ExitMessage );
		    }
		    base.IsComplete = true;

	    }


	    /// <summary>
	    /// property Action -
	    ///     sets an action to be perform.
	    /// </summary>
	    /// <remarks>
	    /// The valid action for use is replace.
	    /// </remarks>
	    [Action("action", Needed=true)]
	    public string Action
	    {
		    set
		    {
			    this._strAction = value;
		    }
	    }

	    /// <summary>
	    /// property ExitCode -
	    ///     gets the exit code from the execution.
	    /// </summary>
	    /// <remarks>
	    ///     0 means successfully execution a given action;
	    ///     any other value means an error has happened.
	    /// </remarks>
	    public new int ExitCode
	    {
		    get
		    {
			    return (int) this._enumRegExOpCode;
		    }
	    }

	    public new bool IsComplete
	    {
		    get
		    {
			    return base.IsComplete;
		    }
	    }

	    /// <summary>
	    /// property ExitMessage -
	    ///     gets the error message that is corresponding to
	    ///     the exit code.
	    /// </summary>
	    /// <remarks>
	    ///     this property is primary used for the logging an
	    ///     exception happened during the execution stage or
	    ///     when program is checking the property setup.
	    /// </remarks>
	    public new string ExitMessage
	    {
		    get
		    {
			    return this._strMessage;
		    }
	    }

	    /// <summary>
	    /// property Name -
	    ///     gets the object name.
	    /// </summary>
	    /// <remarks>
	    ///     this is used for logging the exception and letting
	    ///     user know which action object has an exception
	    ///     happen.
	    /// </remarks>
	    public new string Name
	    {
		    get
		    {
			    return this.GetType().Name.ToLower();
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

	    #region ICleanUp Members

	    /// <summary>
	    /// public void RemoveIt() -
	    ///     reverse the operation that the object has
	    ///     performed.
	    /// </summary>
	    public void RemoveIt()
	    {
		    foreach ( FileInfo fi in this._fiFileInfos )
		    {
			    string strBackupFile = Path.ChangeExtension( fi.FullName, ".bak" );
			    if ( File.Exists( strBackupFile ) )
			    {
				    File.Copy( strBackupFile, fi.FullName, true );
				    fi.Delete();
			    }
		    }
	    }

	    #endregion
    }
}
