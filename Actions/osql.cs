using System;
using System.IO;
using System.Xml;
using System.Text;

using Microsoft.Win32;

namespace XInstall.Core.Actions {
    /// <summary>
    /// Summary description for osql.
    /// </summary>
    public class OSQL : ExternalPrg, IAction {

	    XmlNode _xnActionNode = null;

	    const string _cntStrSQLServerPath  =
		@"Software\Microsoft\Microsoft SQL Server\80\Tools\ClientSetup";

	    private readonly string _rostrRegistryKey = @"SQLPath";
	    private readonly string _rostrOSQL        = @"osql.exe";

	    private string _strOSQLFullPath       = null;
	    private string _strOSQLShortPathName  = null;
	    private string _strSQLServerName      = null;
	    private string _strSQLServerUser      = null;
	    private string _strSQLServerUserPWD   = null;
	    private string _strSQLStmt            = null;
	    private string _strSQLScriptFile      = null;
	    private string _strDatabaseName       = null;
	    private bool _bTrustedConnection      = true;
	    private bool _bNoHeader               = false;
	    private bool _bInputScriptFile        = false;
	    private bool _bAbortOnError           = true;
	    private bool _bNoNumbering            = true;
	    private bool _bAllowGenerateException = false;

	    // error handling variables
	    private enum OSQL_OPR_CODE {
		    OSQL_OPR_SUCCESS,
		    OSQL_OPR_SQLCLIENT_NOT_INSTALL,
		    OSQL_OPR_BOOLEAN_PARSING_ERROR,
		    OSQL_OPR_OSQL_NOT_EXIST,
		    OSQL_OPR_INDENTITY_REQUIRED,
		    OSQL_OPR_SCRIPTFILE_NOTFOUND,
		    OSQL_OPR_UNKNOWN_SUBELEMENT,
		    OSQL_OPR_ONLY_TEXTNODE_ALLOW,
		    OSQL_OPR_CUSTOM_EXCEPTION_GENERATED,
		    OSQL_OPR_START_EXECUTING,
		    OSQL_OPR_EXIT_WITH_ERRORS,
	    };
	    private OSQL_OPR_CODE _enumOSQLOprCode = OSQL_OPR_CODE.OSQL_OPR_SUCCESS;
	    private string _strExitMessage         = null;
	    private string[] _strMessages          = {
		    @"{0}: operation success",
		    @"{0}: SQL Server client tool is not installed, can't find osql utility, message - {2}",
		    @"{0}: {1} - boolean variable parsing error",
		    @"{0}: {1} is not existed in a given directory, {2}",
		    @"{0}: {1} is required when trusted connection is not used!",
		    @"{0}: given script file {1} does not exist!",
		    @"{0}: element {1} is not recognized!",
		    @"{0}: invalid element {1}, only text node allow!",
		    @"{0}: user request to generate an exception!",
		    @"{0}: start executing osql",
		    @"{0}: external program, osql.exe has return code {1}, argument in - {2}, message - {3}",
	    };

	    [Action("osql")]
	    public OSQL( XmlNode xnActionElement ) : base() {
		    // string strCmd = Environment.GetEnvironmentVariable("comspec");
		    this.Init();
		    Win32API.GetShortPathName( this._strOSQLFullPath, out this._strOSQLShortPathName );
		    base.ProgramName           = Environment.GetEnvironmentVariable("comspec") + @" /c ";
		    // base.ProgramArguments      = strShortPathName;
		    base.ProgramRedirectOutput = "false";
		    this._xnActionNode         = xnActionElement;
	    }

	    #region public property
	    [Action("trustedconnection", Needed=false, Default="true")]
	    public string TrustedConnection {
		    set {
			    try {
				    this._bTrustedConnection = bool.Parse( value );
			    }
			    catch ( Exception ) {
				    this.SetExitMessage(
					OSQL_OPR_CODE.OSQL_OPR_BOOLEAN_PARSING_ERROR,
					this.Name, @"TrustedConnection");
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
	    }

	    [Action("noheader", Needed=false, Default="false")]
	    public string NoHeader {
		    set {
			    try {
				    this._bNoHeader = bool.Parse( value );
			    }
			    catch ( Exception ) {
				    this.SetExitMessage(
					OSQL_OPR_CODE.OSQL_OPR_BOOLEAN_PARSING_ERROR,
					this.Name, @"NoHeader");
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
	    }

	    [Action("sqlserver", Needed=false, Default=".")]
	    public string SQLServerName {
		    get {
			    return this._strSQLServerName;
		    }
		    set {
			    this._strSQLServerName = value;
		    }
	    }

	    [Action("username", Needed=false)]
	    public string SQLServerUser {
		    get {
			    if ( this._strSQLServerUser == null ) {
				    this.SetExitMessage(
					OSQL_OPR_CODE.OSQL_OPR_INDENTITY_REQUIRED,
					this.Name, @"username");
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
			    return this._strSQLServerUser;
		    }
		    set {
			    this._strSQLServerUser = value;
		    }
	    }

	    [Action("password", Needed=false)]
	    public string SQLServerPassword {
		    get {
			    if ( this._strSQLServerUserPWD == null ) {
				    this.SetExitMessage(
					OSQL_OPR_CODE.OSQL_OPR_INDENTITY_REQUIRED,
					this.Name, @"password");
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
			    return this._strSQLServerUserPWD;
		    }
		    set {
			    this._strSQLServerUserPWD = value;
		    }
	    }

	    [Action("dbname", Needed=false)]
	    public string DatabaseName {
		    get {
			    return this._strDatabaseName;
		    }
		    set {
			    this._strDatabaseName = value;
		    }
	    }

	    [Action("sqlscriptfile", Needed=false)]
	    public string ScriptFileName {
		    get {
			    if ( !File.Exists( this._strSQLScriptFile ) ) {
				    this.SetExitMessage(
					OSQL_OPR_CODE.OSQL_OPR_SCRIPTFILE_NOTFOUND,
					this.Name, this._strSQLScriptFile);
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }

			    return this._strSQLScriptFile;
		    }
		    set {
			    this._strSQLScriptFile = value;
			    this._bInputScriptFile = true;
		    }
	    }

	    [Action("sql", Needed=false)]
	    public string SQLStatement {
		    get {
			    return String.Format( @"""{0}""", this._strSQLStmt );
		    }
		    set {
			    if ( this._bInputScriptFile ) {
				    this._bInputScriptFile = false;
			    }
			    this._strSQLStmt = value;
		    }
	    }

	    [Action("abortonerror", Needed=false, Default="true")]
	    public string AbortOnError {
		    set {
			    try {
				    this._bAbortOnError = bool.Parse( value );
			    }
			    catch ( Exception ) {
				    this.SetExitMessage(
					OSQL_OPR_CODE.OSQL_OPR_BOOLEAN_PARSING_ERROR,
					this.Name, @"AbortOnError");
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
	    }

	    [Action("nonumber", Needed=false, Default="true")]
	    public string NoNumbering {
		    set {
			    try {
				    this._bNoNumbering = bool.Parse( value );
			    }
			    catch ( Exception ) {
				    this.SetExitMessage(
					OSQL_OPR_CODE.OSQL_OPR_BOOLEAN_PARSING_ERROR,
					this.Name, @"NoNumbering" );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
	    }

	    [Action("generateexception", Needed=false, Default="false")]
	    public new string AllowGenerateException {
		    set {
			    try {
				    this._bAllowGenerateException = bool.Parse( value );
			    }
			    catch ( Exception ) {
				    this.SetExitMessage(
					OSQL_OPR_CODE.OSQL_OPR_BOOLEAN_PARSING_ERROR,
					this.Name, @"AllowGenerateException" );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
	    }

	    [Action("outputfile", Needed=false, Default="auto")]
	    public new string OutputFile {
		    set {
			    string strOutputFile = null;
			    if ( value.ToLower().Equals("auto") ) {
				    strOutputFile = Path.ChangeExtension( Path.GetFileNameWithoutExtension(
					    Environment.GetCommandLineArgs()[0] ), ".log" );
			    }
			    base.ProgramOutputFile = value;
		    }
	    }

	    public string Arguments {
		    get {
			    return this.GetArgumentList();
		    }
	    }

	    #endregion

	    #region private methods
	    private void Init() {
		    base.OutToConsole = true;
		    base.OutToFile    = true;

		    // open local machine's registry database and point
		    // to local machine
		    try {

			    RegistryKey rk     = RegistryKey.OpenRemoteBaseKey( RegistryHive.LocalMachine, "." );
			    RegistryKey rkSQL  = rk.OpenSubKey( _cntStrSQLServerPath );
			    string strOSQLPath = rkSQL.GetValue( this._rostrRegistryKey ).ToString();
			    rkSQL.Close();

			    this._strOSQLFullPath = strOSQLPath + Path.DirectorySeparatorChar + @"binn" + Path.DirectorySeparatorChar + this._rostrOSQL;
			    if ( !File.Exists( this._strOSQLFullPath ) ) {
				    this.SetExitMessage( OSQL_OPR_CODE.OSQL_OPR_OSQL_NOT_EXIST, this.Name, this._rostrOSQL, strOSQLPath );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
		    catch ( Exception e ) {
			    this.SetExitMessage( OSQL_OPR_CODE.OSQL_OPR_SQLCLIENT_NOT_INSTALL, this.Name, e.Message );
			    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
		    }
	    }

	    private void SetExitMessage( OSQL_OPR_CODE enumOsqlOprCode, params object[] objParams ) {
		    this._enumOSQLOprCode = enumOsqlOprCode;
		    this._strExitMessage  = String.Format( this._strMessages[ this.ExitCode ], objParams );
	    }

	    private string GetArgumentList() {
		    StringBuilder sbArgumentList = new StringBuilder();

		    // setup the SQL Server to connect to
		    sbArgumentList.AppendFormat( " /S {0} ", this.SQLServerName );

		    // setup how to connect to a given SQL Server
		    // if trusted connection is not use, then SQL standard login will kick in
		    if ( this._bTrustedConnection ) {
			    sbArgumentList.Append( "/E " );
		    }
		    else
			    sbArgumentList.AppendFormat( "/U {0} /P {1} ",
							 this.SQLServerUser,
							 this.SQLServerPassword );

		    // osql will abort when error is happening during the execution of
		    // sql statement or script.
		    if ( this._bAbortOnError ) {
			    sbArgumentList.Append( "/b " );
		    }

		    // required header not to be printed
		    if ( this._bNoHeader ) {
			    sbArgumentList.Append( "/h-1 " );
		    }

		    // is script input from file or it is from command line
		    if ( this._bInputScriptFile ) {

			    if ( this.ScriptFileName.IndexOf( @"\" ) == -1 )
				    sbArgumentList.AppendFormat( "/i {0} ",
								 Environment.CurrentDirectory + Path.DirectorySeparatorChar +
								 this.ScriptFileName );
			    else {
				    sbArgumentList.AppendFormat( "/i {0} ", this.ScriptFileName );
			    }
		    }
		    else {
			    this.SQLStatement = this.GetSQLStmtElement();
			    sbArgumentList.AppendFormat( "/Q {0} ", this.SQLStatement );
		    }
		    sbArgumentList.Append( @" 2>&1" );

		    return sbArgumentList.ToString();
	    }

	    #endregion

	    #region IAction Members

	    public override void Execute() {
		    // if user wants an exception to be generated
		    if ( this._bAllowGenerateException ) {
			    this.SetExitMessage(
				OSQL_OPR_CODE.OSQL_OPR_CUSTOM_EXCEPTION_GENERATED, this.Name );
			    this.FatalErrorMessage( ".", this.ExitMessage, 1660 );
		    }

		    base.ProgramArguments = String.Format( @" ""{0} {1}""", this._strOSQLShortPathName, this.Arguments );

		    this.SetExitMessage( OSQL_OPR_CODE.OSQL_OPR_START_EXECUTING, this.Name );
		    base.LogItWithTimeStamp( this.ExitMessage );
		    base.Execute();
		    if ( base.ProgramExitCode > 0 )
			    base.FatalErrorMessage( ".", @"error happened",
						    1660 );

		    base.IsComplete = true;
		    this.SetExitMessage( OSQL_OPR_CODE.OSQL_OPR_SUCCESS, this.Name );
		    base.LogItWithTimeStamp( this.ExitMessage );
	    }

	    public new bool IsComplete {
		    get {
			    return base.IsComplete;
		    }
	    }

	    public new string ExitMessage {
		    get {
			    return this._strExitMessage;
		    }
	    }

	    public new string Name {
		    get {
			    return this.GetType().Name;
		    }
	    }

	    public new int ExitCode {
		    get {
			    // TODO:  Add OSQL.ExitCode getter implementation
			    return (int) this._enumOSQLOprCode;
		    }
	    }

	    #endregion

	    #region IActionElement Members

	    public new string ObjectName {
		    get {
			    // TODO:  Add OSQL.ObjectName getter implementation
			    return null;
		    }
	    }

	    private string GetSQLStmtElement() {
		    string strSqlStmt = null;
		    if ( this._xnActionNode.HasChildNodes )
			    foreach ( XmlNode xnSQLNode in this._xnActionNode.ChildNodes ) {
				    if ( xnSQLNode.Name.Equals( @"sqlstatement" ) ) {
					    foreach ( XmlNode xnTextNode in xnSQLNode ) {
						    if ( xnTextNode.NodeType != XmlNodeType.Text ) {
							    this.SetExitMessage( OSQL_OPR_CODE.OSQL_OPR_ONLY_TEXTNODE_ALLOW, this.Name, xnTextNode.Name );
							    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
						    }
						    strSqlStmt += xnTextNode.Value.Trim();
					    }

				    }
				    else {
					    this.SetExitMessage(
						OSQL_OPR_CODE.OSQL_OPR_UNKNOWN_SUBELEMENT,
						this.Name, xnSQLNode.Name );
					    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
				    }
			    }
		    return String.Format( @"{0}", strSqlStmt);
	    }

	    #endregion
    }
}
