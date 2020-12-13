using System;
using System.IO;

namespace XInstall.Core.Actions {
    /// <summary>
    /// Summary description for CopyFile.
    /// </summary>
    public class CopyFile : ActionElement {
	    private DirectoryInfo _di            = null;
	    private FileInfo[]    _fiFileSet     = null;

	    private string _strFileName          = null;
	    private string _strCopyFrom          = null;
	    private string _strCopyTo            = null;
	    private bool   _bAllowResetAttribute = true;
	    private bool   _bAllowOverwrite      = true;
	    private bool   _bAllowCreateDir      = true;
	    private bool   _bAllowException      = false;

	    #region public constructor
	    /// <summary>
	    /// public CopyFile() -
	    ///     a public constructor that create/initialize
	    ///     the CopyFile object
	    /// </summary>
	    [Action("copyfile", Needed=true)]
	    public CopyFile() : base() {}

	    #endregion

	    #region public property methods
	    /// <summary>
	    /// property FileName -
	    ///     get/set the file to be copied
	    /// </summary>
	    [Action("filename", Needed=true)]
	    public new string FileName {
		    get {
			    return this._strFileName;
		    }
		    set {
			    this._strFileName = value;
		    }
	    }


	    /// <summary>
	    /// property From -
	    ///     get/set the source where the file being copied
	    ///     located
	    /// </summary>
	    [Action("from", Needed=true)]
	    public string From {
		    get {
			    return this._strCopyFrom;
		    }
		    set {
			    this._strCopyFrom = value;
		    }
	    }


	    /// <summary>
	    /// property To -
	    ///     get/set the location where the file to be copied to
	    /// </summary>
	    [Action("to", Needed=true)]
	    public string To {
		    get {
			    return this._strCopyTo;
		    }
		    set {
			    this._strCopyTo = value;
		    }
	    }

	    /// <summary>
	    /// property AllowresetAttribute -
	    ///     get/set the flag that nodifies if a given file's
	    ///     attributes should be reset.  Default set to true
	    /// </summary>
	    [Action("resetfileattributes", Needed=false, Default="true")]
	    public string AllowResetAttribute {
		    set {
			    this._bAllowResetAttribute = value.Equals("true") ? true : false;
		    }
	    }


	    /// <summary>
	    /// property AllowOverwrite -
	    ///     get/set a flag that notify if a file being copied already
	    ///     exists in the destination should be overwrited.  By default,
	    ///     this set to true.
	    /// </summary>
	    [Action("allowoverwrite", Needed=false, Default="true")]
	    public string AllowOverwrite {
		    set {
			    this._bAllowOverwrite = bool.Parse( value.ToString() );
		    }
	    }


	    /// <summary>
	    /// property AllowCreateDir -
	    ///     get/set a flag that nodify CopyFile shold create
	    ///     a directory in the destination if one does not exist.
	    ///     Default this is set to true
	    /// </summary>
	    [Action("allowcreatedir", Needed=false, Default="true")]
	    public string AllowCreateDir {
		    set {
			    this._bAllowCreateDir = bool.Parse( value.ToString() );
		    }
	    }

	    /// <summary>
	    /// property AllowGenerateExecption -
	    ///     get/set a flag to notify CopyFile object should
	    ///     generate an exception automatically.  By default
	    ///     this is set to false.
	    /// </summary>
	    [Action("generateexception", Needed=false, Default="false")]
	    public new string AllowGenerateException {
		    set {
			    this._bAllowException = bool.Parse( value.ToString() );
		    }
	    }

	    /// <summary>
	    /// set a flag to indicate if the action should be run or not
	    /// </summary>
	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable {
		    set {
			    base.Runnable = bool.Parse( value );
		    }
	    }
	    #endregion

	    #region internal private utility function
	    /// <summary>
	    /// private void Init() -
	    ///     a private method that initlaize the CopyFile object.
	    ///     It will perform the following checking:
	    ///         <ul>
	    ///         <li>source directory of a given file</li>
	    ///         <li>the existence of file being copied</li>
	    ///         </ul>
	    ///     If AllowCreateDir is set to true, then create directory
	    ///     in the destination
	    /// </summary>
	    private void Init() {

		    // get ready to check the existence of file
		    DirectoryInfo ldiCopyToDir = new DirectoryInfo ( this.To );
		    this._di = new DirectoryInfo ( this._strCopyFrom );
		    if ( !this._di.Exists ) {
			    string ErrMessage = String.Format ("Directory:{0} does not exist!", this._strCopyFrom);
			    throw new Exception (ErrMessage);
		    }

		    // get file to be copied
		    this._fiFileSet = this._di.GetFiles ( this.FileName );
		    if ( this._fiFileSet.Length == 0 ) {
			    string ErrMessage = String.Format("File: {0} does not exist in directory: {1}", this.FileName, this._di.FullName);
			    throw new Exception ( ErrMessage );
		    }

		    // create directory in the destination if it is required to do so
		    if ( !ldiCopyToDir.Exists && this._bAllowCreateDir ) {
			    Directory.CreateDirectory( ldiCopyToDir.FullName );
		    }
	    }

	    #endregion

	    protected override void ParseActionElement() {

		    // Initialize CopyFile action
		    Init();

		    // perform a copy operation here
		    string strDestFile = null;
		    try {
			    foreach ( FileInfo fi in this._fiFileSet ) {
				    strDestFile  = this.To + Path.DirectorySeparatorChar + fi.Name;
				    FileInfo lfi = new FileInfo ( strDestFile );
				    if ( lfi.Exists ) {
					    File.SetAttributes( strDestFile, FileAttributes.Normal );
					    fi.CopyTo( strDestFile, true );
				    }
				    else {
					    fi.CopyTo( strDestFile );
				    }
				    base.LogItWithTimeStamp( String.Format( @"{0}: successfully copy {1} from {2} to {3}", this.Name, this.FileName, this.From, this.To ) );
			    }
		    }
		    catch ( Exception e ) {
			    base.FatalErrorMessage( ".", String.Format( @"fail to copy {0} from {1} to {2}: reason - {3}", this.FileName, this.From, this.To, e.Message ), 1660);
		    }
	    }
	    #region IAction Members

	    public new bool IsComplete {
		    get {
			    return base.IsComplete;
		    }
	    }

	    public new string ExitMessage {
		    get {
			    return null;
		    }
	    }

	    protected override string ObjectName {
		    get {
			    return this.Name;
		    }
	    }

	    public new string Name {
		    get {
			    return this.GetType().Name;
		    }
	    }

	    public new int ExitCode {
		    get {
			    return 0;
		    }
	    }

	    #endregion
    }
}
