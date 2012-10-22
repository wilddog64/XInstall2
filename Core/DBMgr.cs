using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Data.SqlClient;
using Interop.SQLDMO;
using System.Runtime.InteropServices;

using XInstall.Util.Log;

namespace XInstall.Core {

    /// <summary>
    /// DBMgr - a base class for executing DBBackup and DBRestore classes.
    /// </summary>
    public class DBMgr : Logger {

#region login related variables
        string _strSqlServer       = "(local)";
        string _strUserName        = null;
        string _strUserPassword    = null;
        string _strExitMessage     = null;
        bool   _bTrustedConnection = true;
        bool   _bIsCompleted       = false;
        int    _iExitCode          = 0;

#endregion

        // database name
        private string _strDBName = null;
        private string _strAction = null;

#region Backup related variables
        bool   _bReqestBackup       = false;
        bool   _bInitBackupDatabase = true;
        bool   _bAllowException     = false;
        string _strBackupTo         = null;
#endregion

#region Restore related variables
        bool   _bReqestRestore     = false;
        bool   _bReplaceDatabase   = true;
        string _strRestoreFromPath = null;
        string _strDBDataFilePath  = null;
        string _strDBLogFilePath   = null;
#endregion

#region Object variables
        SQLServer _mSQLServer                = null;
        Interop.SQLDMO.Backup _bkBackupObj   = null;
        Interop.SQLDMO.Restore _rtRestoreObj = null;
#endregion

#region private property methods
#region public property methods
        /// <summary>
        /// property Action -
        ///     set the action that DBMgr need to
        ///     perform.  Valid action now is:
        ///     backup and restore.  If action other
        ///     than these two is set, the SystemException
        ///     will be thrown
        /// </summary>
        protected string Action
        {
            get { return this._strAction;  }
            set { this._strAction = value; }
        }


        /// <summary>
        /// private property RequestBackup -
        ///     get/set the value of _bRequestBackup.
        ///     true - will make DBMgr to perform
        ///     backup operation.
        /// </summary>
        protected bool RequestBackup
        {
            get { return _bReqestBackup; }
            set { _bReqestBackup = value; }
        }

        /// <summary>
        /// private property RequestRestore -
        ///     get/set the value of _bRequestRestore.
        ///     true - will make DBMgr to perform
        ///     restore operation
        /// </summary>
        protected bool RequestRestore
        {
            get { return _bReqestRestore; }
            set { _bReqestRestore = value; }
        }

        /// <summary>
        /// property SqlServerName -
        ///     get/set the value _strSqlServer
        ///     this is the SQL Server that
        ///     backup/restore operation works
        ///     against.
        /// </summary>
        protected string SqlServerName
        {
            get { return _strSqlServer; }
            set { _strSqlServer = value; }
        }

        /// <summary>
        /// property BackupTo -
        ///     get/set the path where database will
        ///     be backup to
        /// </summary>
        protected string BackupTo
        {
            get { return _strBackupTo; }
            set { this._strBackupTo = value; }
        }

        /// <summary>
        /// property UserName -
        ///     get/set the user name that uses
        ///     to connect to SQL Server.  If
        ///     isTrustedConnection is true, then
        ///     this won't take in effect.
        /// </summary>
        protected string UserName
        {
            get {
                return _strUserName;
            }
            set {
                _strUserName = value;
            }
        }

        /// <summary>
        /// property UserPassword -
        ///     get/set the password associates
        ///     with a given UserName.  If
        ///     isTrustedConnection is true, then
        ///     this won't take in effect.
        /// </summary>
        protected string UserPassword
        {
            get { return _strUserPassword; }
            set { _strUserPassword = value; }
        }

        /// <summary>
        /// property DatabaseName -
        ///     get/set the name of database to
        ///     work with
        /// </summary>
        protected string DatabaseName
        {
            get { return _strDBName; }
            set { _strDBName = value; }
        }


        /// <summary>
        /// property RestoreTo -
        ///     get/set the SQL Server you want database
        ///     restore to. It is required only when
        ///     Action is restore
        /// </summary>
        protected string RestoreTo
        {
            get { return this.SqlServerName; }
            set { this.SqlServerName = value; }
        }


        /// <summary>
        /// property RestoreFrom -
        ///     get/set the path where the backup datbase
        ///     is.
        /// </summary>
        protected string RestoreFrom
        {
            get { return _strRestoreFromPath; }
            set { _strRestoreFromPath = value; }
        }


        /// <summary>
        /// property DataFilePath -
        ///     get/set the DataFilePath required by
        ///     the restore operation
        /// </summary>
        protected string DataFilePath
        {
            get { return _strDBDataFilePath; }
            set { _strDBDataFilePath = value; }
        }

        /// <summary>
        /// property LogFilePath -
        ///     get/set the LogFilePath requested by
        ///     restore operation
        /// </summary>
        protected string LogFilePath
        {
            get { return _strDBLogFilePath; }
            set { _strDBLogFilePath = value; }
        }
#endregion

        /// <summary>
        /// property isTrustedConnectionOn -
        ///     get/set the value of _bTrustedConnection
        ///     true for trusted connection; false for
        ///     SQL Server logging.
        /// </summary>
        public bool isTrustedConnectionOn
        {
            get { return _bTrustedConnection; }
            set { _bTrustedConnection = value; }
        }


        /// <summary>
        /// property OutputFile -
        ///     set the location of file where output to be
        ///     written to
        /// </summary>
        public string OutputFile
        {
            set { base.FileName = value; }
        }

        /// <summary>
        /// property ExitCode
        ///     gets the return code from the object execution
        /// </summary>
        public int ExitCode
        {
            get { return this._iExitCode; }
        }

        /// <summary>
        /// property ExitMessage
        ///     gets the message that corresponding to the
        ///     return code from the object execution
        /// </summary>
        public string ExitMessage
        {
            get { return this._strExitMessage; }
        }

        /// <summary>
        /// property IsComplete -
        ///     get/set the state of the object execution.
        ///     true for object execute successfully complete;
        ///     false otherwise.
        /// </summary>
        public bool IsComplete
        {
            get { return this._bIsCompleted; }
            set { this._bIsCompleted = value; }
        }

#endregion

#region public constructs

        /// <summary>
        /// a public constructor that initializes the DBMgr object.
        /// </summary>
        /// <remarks>
        /// When initializes, the constructor will allow logged message
        /// to be written to console and file by default.
        /// </remarks>
        public DBMgr () {
            base.OutToConsole = true;
            base.OutToFile    = true;
        }

#endregion

#region protected property methods
        /// <summary>
        /// a protected property that sets a flag
        /// to allow an object to generate an
        /// exception automatically.
        /// </summary>
        /// <remarks>
        /// by default, the property is set to false.
        /// If you want to test an exception under
        /// normal stituation, set this property
        /// to true.
        /// </remarks>
        /// <example>
        ///     object.AllowGenerateException = true;
        ///     will make DBMgr to generate an exception.
        /// </example>
        protected bool AllowGenerateException
        {
            get { return this._bAllowException; }
            set { this._bAllowException = value; }
        }
#endregion

#region public methods

        /// <summary>
        /// This method will execute the required action by
        /// the request from the derived class.  The valid
        /// action it accepted are, backup and restore.
        /// </summary>
        /// <remarks>
        ///     This is virtual methods which means, it can
        ///     be override by the class who inherits from
        ///     it.
        /// </remarks>
        /// <example>
        /// The following statements,
        ///
        ///     object.Action = "backup";
        ///     object.Execute();
        ///
        ///     will execute a backup operation.
        /// </example>
        public virtual void Execute () {
            if ( this.AllowGenerateException ) {
                this.AllowGenerateException = false;
                throw new Exception("required an exception to be generated!");
            }

            try {
                this.ConnectServer();
                switch ( this._strAction ) {
                    case "backup":
                        this.BackupDatabase();
                        break;
                    case "restore":
                        this.RestoreDB();
                        break;
                    default:
                        throw new SystemException("unknown action accquired!");
                }
            } catch ( Exception e ) {
                this._iExitCode      = 1;
                this._strExitMessage = e.Message;
                base.FatalErrorMessage( ".", this._strExitMessage, 1660 );

            }
            this.IsComplete = true;
        }

        /// <summary>
        /// This method close the connection to a given SQL Server.
        /// </summary>
        public new void Close () {
            _mSQLServer.DisConnect ();
        }


#endregion

#region private callback functions
        /// <summary>
        /// This is a called back function that prints out
        /// the percentage completed so far for a restoration
        /// </summary>
        /// <param name="strMsg">message comes in</param>
        /// <param name="iPercent">percentage completed</param>
        protected virtual void _rtRestoreObj_PercentComplete( string strMsg, int iPercent ) {
            Console.WriteLine ("\t{0}", strMsg);
        }

        /// <summary>
        /// This is a callback function that shows the percentage complete
        /// so far by the backup operation.
        /// </summary>
        /// <param name="strMsg">message indicate the backup process</param>
        /// <param name="iPercent">percentage completed so far</param>
        protected virtual void _bkBackupObj_PercentComplete( string strMsg, int iPercent ) {
            Console.WriteLine ("\t{0}", strMsg);
        }
#endregion

#region private utility functions

        /// <summary>
        /// RestoreDB - Restore a database
        ///             from a given location that has backup database file
        ///             No parameters are needed.
        /// </summary>
        private void RestoreDB () {
            base.LogItWithTimeStamp ( String.Format("start restoring database:{0}, file:{1}", this.DatabaseName, this.RestoreFrom) );
            Console.WriteLine ("\tpercentage completed so far:\n");
            if ( _rtRestoreObj != null ) {
                // Setup required parameters. All required ones can be
                // retrieve from property methods
                _rtRestoreObj.Database        = this.DatabaseName;
                _rtRestoreObj.Files           = this.RestoreFrom;
                _rtRestoreObj.RelocateFiles   = this.GetDataFileList ( this.RestoreTo );
                _rtRestoreObj.ReplaceDatabase = _bReplaceDatabase;

                // setup a callback function and start restoring database
                _rtRestoreObj.PercentComplete += new RestoreSink_PercentCompleteEventHandler( _rtRestoreObj_PercentComplete );
                _rtRestoreObj.SQLRestore ( _mSQLServer );
            }
            base.LogItWithTimeStamp( String.Format("complete restoring database:{0}", this.DatabaseName) );

        }


        /// <summary>
        /// Private BackupDatabase() - a private method that use to
        ///                            backup database from a given database server
        /// </summary>
        private void BackupDatabase () {
            base.LogItWithTimeStamp ( String.Format("start backing up database:{0} to file:{1}", this.DatabaseName, this.BackupTo) );

            // setup required parameters for calling SQLBackup method from SQLDMO
            // object
            _bkBackupObj.Database   = this.DatabaseName;
            _bkBackupObj.Files      = this.BackupTo;
            _bkBackupObj.Initialize = this._bInitBackupDatabase;

            // setup callbackup function and invoke SQLBackup method.
            _bkBackupObj.PercentComplete += new BackupSink_PercentCompleteEventHandler(_bkBackupObj_PercentComplete);
            _bkBackupObj.SQLBackup( _mSQLServer );
            base.LogItWithTimeStamp( String.Format("backup database {0} to {1} complete successfully", this.DatabaseName, this.BackupTo) );
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
        private string GetDataFileList ( string strRestoreToServer ) {
            // TODO: GetDataFileList function need to retrieve logical database names

            StringBuilder lsbRestoreList = new StringBuilder ();

            try {
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
                        lalLogicalDBName.Add( lsdrResult[0] );
                dba.Close ();

                // the format request by SQLDMO for restoring database
                // is [logical_database_name], [physical_datafile_path],
                //    [logical_log_name], [physical_logfile_name] and
                // here we construct it as requested and return it to
                // the caller
                lsbRestoreList.AppendFormat ("[{0}], [{1}], [{2}], [{3}]",
                                             lalLogicalDBName[0],
                                             this.DataFilePath + Path.DirectorySeparatorChar + lalLogicalDBName[0]+ ".mdf",
                                             lalLogicalDBName[1], this.LogFilePath           + Path.DirectorySeparatorChar
                                             + lalLogicalDBName[1] +  ".ldf");
            } catch ( Exception e ) {
                throw e;
            }

            return lsbRestoreList.ToString();
        }

        /// <summary>
        /// private void ConnectServer() -
        ///     Connect to a given SQL Server.
        ///     If isTrustedConnectionOn is true then
        ///     we use Windows Authentication; otherwise,
        ///     user name and password is requred to
        ///     connect to a given server
        /// </summary>
        private void ConnectServer () {
            if ( _mSQLServer != null )
                return;
            try {
                _mSQLServer = new SQLServer ();
                if ( this.isTrustedConnectionOn ) {
                    _mSQLServer.LoginSecure = true;
                }

                _mSQLServer.Connect( SqlServerName, UserName, UserPassword );
                if ( _mSQLServer != null ) {
                    if ( this.RequestBackup == true )
                        _bkBackupObj = new Interop.SQLDMO.BackupClass ();
                    if ( this.RequestRestore == true )
                        _rtRestoreObj = new Interop.SQLDMO.RestoreClass ();
                }

            } catch ( Exception e ) {
                StringBuilder sbErrorMessage = new StringBuilder ();
                sbErrorMessage.AppendFormat
                ("Unable to connect to SQL Server {0}, reason: {1}",
                 SqlServerName, e.ToString());
            }
        }

        /// <summary>
        /// a protected property that gets the SQLServer object.
        /// </summary>
        protected SQLServer ThisServer
        {
            get {
                return this._mSQLServer;
            }
        }

        /// <summary>
        /// a protected method that removed the database restored to
        /// a given server.
        /// </summary>
        /// <param name="strDBName">name of the database to be removed</param>
        protected void RemoveDatabase(string strDBName) {
            this._mSQLServer.Databases.Item( strDBName, "dbo" ).Remove();
        }
#endregion
    }
}
