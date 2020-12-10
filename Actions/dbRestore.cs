using System;
using System.Collections;
using System.IO;
using System.Data.SqlClient;
using System.Text;

using Interop.SQLDMO;
using XInstall.Core;
using XInstall.Util.Log;


namespace XInstall.Core.Actions
{
    /// <summary>
    /// public class DBRestore -
    ///     a class that perform a database restoration
    ///     on a given database server.
    /// </summary>

    public class DBRestore : ActionElement, ICleanUp, IAction
    {
	    private string _strFileName = null;

	    private enum DBRESTORE_OPR_CODE
	    {
		    DBRESTORE_OPR_SUCCESS = 0,
		    DBRESTORE_OPR_NOACTION_SPECIFIED,
		    DBRESTORE_OPR_INVALID_ACTION,
		    DBRESTORE_OPR_DBNAME_NOTPROVIDED,
		    DBRESTORE_OPR_BACKUPFILE_NOTEXIST,
		    DBRESTORE_OPR_BOOLEAN_PARSE_ERROR,
		    DBRESTORE_OPR_AUTOEXCEPTION_REQUIRED,
		    DBRESTORE_OPR_USERNAME_REQUIRED,
		    DBRESTORE_OPR_USERPASS_REQUIRED,
	    }

	    private DBRESTORE_OPR_CODE _enumDBRestoreOprCode =
		DBRESTORE_OPR_CODE.DBRESTORE_OPR_SUCCESS;
	    private string _strExitMessage = null;
	    private string[] _strMessages  =
	    {
		    @"{0}: successfully restore database {1}, exit code {2}",
		    @"{0}: no action specified, exit code {1}",
		    @"{0}: invalid action {1} specified, only restore allowed, exit code {2}",
		    @"{0}: database name does not provide, exit code {1}",
		    @"{0}: backup file {1} does not exist, exit code {2}",
		    @"{0}: boolean variable parsing error, exit code {1}",
		    @"{0}: exception generated upon request, exit code {1}",
		    @"{0}: user name is required when trusted connection is not used {1}",
		    @"{0}: user password is required when trusted connection is not used {1}",
	    };

	    private bool   _bReplaceDatabase             = true;
	    private bool   _bTrustedConnection           = true;
	    private bool   _bAllowGenerateException      = false;
	    private bool   _bIsComplete                  = false;
	    private string _strRestoreFromPath           = null;
	    private string _strDBDataFilePath            = null;
	    private string _strDBLogFilePath             = null;
	    private string _strDatabaseName              = null;
	    private string _strSqlServerName             = null;
	    private string _strUserName                  = null;
	    private string _strUserPassword              = null;
	    private SQLServer _mSQLServer                = null;
	    private Interop.SQLDMO.Restore _rtRestoreObj = null;



	    /// <summary>
	    /// public DBRestore() -
	    ///     a public constructor that instaniate the DBRestore
	    ///     object and perform some initialization.  By default,
	    ///     it uses a trusted connection.
	    /// </summary>
	    [Action("dbrestore")]
	    public DBRestore()
	    {
		    // specially for restore operation
		    // so set base class's RequestBackup
		    // property to true
		    base.OutToConsole = true;
		    base.OutToFile    = true;
	    }

	    /// <summary>
	    /// property DatabaseName -
	    ///     set database to be restored
	    /// </summary>
	    [Action("databasename", Needed=true)]
	    public string DatabaseName
	    {

		    get
		    {
			    return this._strDatabaseName;
		    }

		    set
		    {
			    if ( value == null || value == String.Empty )
			    {
				    this._enumDBRestoreOprCode =
					DBRESTORE_OPR_CODE.DBRESTORE_OPR_DBNAME_NOTPROVIDED;
				    this._strExitMessage       =
					String.Format( this._strMessages[ this.ExitCode ],
						       this.Name, this.ExitCode );
				    throw new Exception();
			    }

			    this._strDatabaseName = value;
		    }
	    }


	    /// <summary>
	    /// property RestoreTo -
	    ///     set which SQL Server to
	    ///     restore database to
	    /// </summary>
	    [Action("restoreto", Needed=true)]
	    public string RestoreTo
	    {

		    get
		    {
			    return this._strSqlServerName;
		    }

		    set
		    {
			    this._strSqlServerName = value;
		    }
	    }

	    /// <summary>
	    /// property FileName -
	    ///     sets the name of file for being restored
	    /// </summary>
	    [Action("filename", Needed=true)]
	    public new string FileName
	    {

		    set
		    {
			    this._strFileName = value;
		    }
	    }

	    /// <summary>
	    /// property RestoreFrom -
	    ///     set the path that contains the
	    ///     backup database file to be restored to
	    /// </summary>
	    [Action("restorefrom", Needed=true)]
	    public string RestoreFrom
	    {

		    get
		    {
			    return this._strRestoreFromPath;
		    }

		    set
		    {
			    string strFullFilePath =
				value + Path.DirectorySeparatorChar + this._strFileName;
			    this._strRestoreFromPath = strFullFilePath;
		    }
	    }

	    [Action("username", Needed=false)]
	    public string UserName
	    {

		    get
		    {
			    if ( !this._bTrustedConnection && this._strUserName == null )
			    {
				    this._enumDBRestoreOprCode =
					DBRESTORE_OPR_CODE.DBRESTORE_OPR_USERNAME_REQUIRED;
				    this._strExitMessage       =
					String.Format( this._strMessages[ (int) this._enumDBRestoreOprCode ],
						       this.Name );
				    base.LogItWithTimeStamp( this.ExitMessage );
				    throw new Exception();

			    }

			    return this._strUserName;
		    }

		    set
		    {
			    this._strUserName = value;
		    }
	    }

	    [Action("userpassword", Needed=false)]
	    public string UserPassword
	    {

		    get
		    {
			    if ( !this._bTrustedConnection && this._strUserPassword == null )
			    {
				    this._enumDBRestoreOprCode =
					DBRESTORE_OPR_CODE.DBRESTORE_OPR_USERPASS_REQUIRED;
				    this._strExitMessage       =
					String.Format( this._strMessages[ this.ExitCode ],
						       this.Name );
				    base.LogItWithTimeStamp( this.ExitMessage );
				    throw new Exception();
			    }

			    return this._strUserPassword;
		    }

		    set
		    {
			    this._strUserPassword = value;
		    }
	    }

	    /// <summary>
	    /// Property DataPath -
	    ///     set the path for where to put
	    ///     the physical database data file
	    /// </summary>
	    [Action("datapath", Needed=true)]
	    public string DataPath
	    {

		    get
		    {
			    return this._strDBDataFilePath;
		    }

		    set
		    {
			    this._strDBDataFilePath = value;
		    }
	    }


	    /// <summary>
	    /// property LogPath -
	    ///     set the path for where to put
	    ///     the physical database log file.
	    /// </summary>
	    [Action("logpath", Needed=true)]
	    public string LogPath
	    {

		    get
		    {
			    return this._strDBLogFilePath;
		    }

		    set
		    {
			    this._strDBLogFilePath = value;
		    }
	    }

	    /// <summary>
	    /// property TrustedConnection -
	    ///     set a flag that indicates if trusted connection
	    ///     is used.
	    /// </summary>
	    [Action("trustedconnection", Needed=false, Default="true")]
	    public string TrustedConnection
	    {

		    set
		    {
			    try
			    {
				    this._bIsComplete = bool.Parse( value.ToString() );

			    }
			    catch
			    {
				    this._enumDBRestoreOprCode =
					DBRESTORE_OPR_CODE.DBRESTORE_OPR_BOOLEAN_PARSE_ERROR;
				    this._strExitMessage       =
					String.Format( this._strMessages[ this.ExitCode ],
						       this.Name, this.ExitCode )
					;
				    throw new Exception();
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
	    /// RestoreDB - Restore a database
	    ///             from a given location that has backup database file
	    ///             No parameters are needed.
	    /// </summary>
	    private void RestoreDB ()
	    {
		    base.LogItWithTimeStamp ( String.Format("start restoring database:{0}, file:{1}",
							    this.DatabaseName, this.RestoreFrom) );
		    Console.WriteLine ("\tpercentage completed so far:\n");

		    if ( _rtRestoreObj != null )
		    {
			    // Setup required parameters. All required ones can be
			    // retrieve from property methods
			    _rtRestoreObj.Database        = this.DatabaseName;
			    _rtRestoreObj.Files           = this.RestoreFrom;
			    _rtRestoreObj.RelocateFiles   = this.GetDataFileList ( this.RestoreTo );
			    _rtRestoreObj.ReplaceDatabase = _bReplaceDatabase;

			    // setup a callback function and start restoring database
			    _rtRestoreObj.PercentComplete +=
				new RestoreSink_PercentCompleteEventHandler
				( _rtRestoreObj_PercentComplete );
			    _rtRestoreObj.SQLRestore ( _mSQLServer );
		    }

		    base.LogItWithTimeStamp( String.Format("complete restoring database:{0}",

							   this.DatabaseName) );

	    }

	    /// <summary>
	    /// set flag to indicate if object is going to skip any error
	    /// </summary>
	    [Action("skiperror", Needed=false, Default="false")]
	    public new string SkipError
	    {

		    set
		    {
			    base.SkipError = bool.Parse( value );
		    }
	    }

	    /// <summary>
	    /// private void ConnectServer() -
	    ///     Connect to a given SQL Server.
	    ///     If isTrustedConnectionOn is true then
	    ///     we use Windows Authentication; otherwise,
	    ///     user name and password is requred to
	    ///     connect to a given server
	    /// </summary>
	    private void ConnectServer ()
	    {
		    if ( this._mSQLServer != null )
		    {
			    return;
		    }

		    try
		    {
			    _mSQLServer = new SQLServer ();

			    if ( this._bTrustedConnection )
			    {
				    _mSQLServer.LoginSecure = true;
			    }

			    _mSQLServer.Connect( this.RestoreTo, UserName, UserPassword );

			    if ( _mSQLServer != null )
			    {
				    _rtRestoreObj = new Interop.SQLDMO.RestoreClass ();
			    }

		    }
		    catch ( Exception e )
		    {
			    this._strExitMessage =
				String.Format(
				    "Unable to connect to SQL Server {0}, reason: {1}",
				    this.RestoreFrom, e.ToString());
			    base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );

		    }
	    }

	    /// <summary>
	    /// a protected method that removed the database restored to
	    /// a given server.
	    /// </summary>
	    /// <param name="strDBName">name of the database to be removed</param>
	    private void RemoveDatabase(string strDBName)
	    {
		    this._mSQLServer.Databases.Item( strDBName, "dbo" ).Remove();
	    }

	    /// <summary>
	    /// private string GetDataFileList ( strRestoreToServer ) -
	    ///     is a help function that used to retrive logical database names
	    ///     from a given database backup file. One parameter, strRestoreToServer,
	    ///     is accepted.  The parameter should be a SQL Server that the restored
	    ///     file is going to be put.
	    /// </summary>
	    /// <param name="strRestoreToServer"></param>
	    /// <returns></returns>
	    private string GetDataFileList ( string strRestoreToServer )
	    {
		    // TODO: GetDataFileList function need to retrieve logical database names

		    StringBuilder lsbRestoreList = new StringBuilder ();

		    try
		    {
			    // Always connect to server's master database by using trusted
			    // connection. The account who run this need to have sa privilege
			    // on sql server
			    DBAccess dba = new DBAccess ( strRestoreToServer, "master" );

			    // Construct sql statement to retrieve backup set's logical
			    // database name
			    String lstrGetFileListStmt =
				String.Format ("restore filelistonly from disk = '{0}'",
					       this.RestoreFrom);
			    ArrayList lalLogicalDBName = new ArrayList();
			    SqlDataReader lsdrResult = dba.RunQuery(lstrGetFileListStmt);

			    if ( lsdrResult != null )
				    while ( lsdrResult.Read() )
				    {
					    lalLogicalDBName.Add( lsdrResult[0] );
				    }

			    dba.Close ();

			    // the format request by SQLDMO for restoring database
			    // is [logical_database_name], [physical_datafile_path],
			    //    [logical_log_name], [physical_logfile_name] and
			    // here we construct it as requested and return it to
			    // the caller
			    lsbRestoreList.AppendFormat ("[{0}], [{1}], [{2}], [{3}]",
							 lalLogicalDBName[0],
							 this.DataPath + Path.DirectorySeparatorChar
							 + this.DatabaseName + ".mdf",
							 lalLogicalDBName[1], this.LogPath  + Path.DirectorySeparatorChar
							 + this.DatabaseName +  ".ldf");

		    }
		    catch ( Exception e )
		    {
			    throw e;
		    }

		    return lsbRestoreList.ToString();
	    }

	    /// <summary>
	    /// This is a called back function that prints out
	    /// the percentage completed so far for a restoration
	    /// </summary>
	    /// <param name="strMsg">message comes in</param>
	    /// <param name="iPercent">percentage completed</param>
	    protected virtual void _rtRestoreObj_PercentComplete
	    ( string strMsg, int iPercent )
	    {
		    Console.WriteLine ("\t{0}", strMsg);
	    }

	    /// <summary>
	    /// property AllowGenerateException -
	    ///     set a flag to tell whether DBRestore should
	    ///     generate an exception automatically.
	    /// </summary>
	    [Action("generateexception", Needed=false, Default="false")]
	    public new string AllowGenerateException
	    {

		    set
		    {
			    try
			    {
				    this._bAllowGenerateException =
					bool.Parse( value.ToString() );

			    }
			    catch
			    {
				    this._enumDBRestoreOprCode =
					DBRESTORE_OPR_CODE.DBRESTORE_OPR_BOOLEAN_PARSE_ERROR;
				    this._strExitMessage       =
					String.Format( this._strMessages[ this.ExitCode ],
						       this.Name, this.ExitCode )
					;
				    throw new Exception();
			    }
		    }
	    }

	    /// <summary>
	    /// property ExitCode -
	    ///     gets the return code from the operation
	    /// </summary>
	    public new int ExitCode
	    {

		    get
		    {
			    return (int) this._enumDBRestoreOprCode;
		    }
	    }

	    /// <summary>
	    /// property ExitMessage -
	    ///     gets the message that is coresponding to
	    ///     the exit code.
	    /// </summary>
	    public new string ExitMessage
	    {

		    get
		    {
			    return this._strExitMessage;
		    }
	    }


	    protected override void ParseActionElement()
	    {
		    base.ParseActionElement();
		    this.ConnectServer();
		    this.RestoreDB();
		    this._bIsComplete = true;
	    }

	    public bool IsCompelete
	    {

		    get
		    {
			    return base.IsComplete;
		    }
	    }

	    /// <summary>
	    /// public void RemoveIt() -
	    ///     a method that derives from ICleanUp interface
	    ///     to perform a cleanup when required.  It will
	    ///     drop the database it restored from the database
	    ///     server.
	    /// </summary>
	    public void RemoveIt()
	    {
		    // base.ThisServer.Databases.Item(base.DatabaseName, "dbo").Remove();
		    this.RemoveDatabase( this.DatabaseName );
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

	    protected override string ObjectName
	    {

		    get
		    {
			    return this.Name;
		    }
	    }

    }
}









