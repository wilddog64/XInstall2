using System;
using System.Collections;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Reflection;
using System.Configuration;

using XInstall.Util.Log;

namespace XInstall.Core {

    /// <summary>
    /// public class DBAccess -
    ///     a class that handle the Database access methods.  This class
    ///     uses an ADO.NET's SqlClient library to access a SQL Server.
    /// </summary>
    public class DBAccess : ActionElement {
#region SQL Command Constants
        /// <summary>
        ///   private constants represent
        ///   each type of SQL statement
        ///      <ul>
        ///          <li>InsertCommand - an insert sql command</li>
        ///          <li>DeleteCommand - a delete sql command</li>
        ///          <li>UpdateCommand - an update sql command</li>
        ///          <li>SelectCommand - a select sql command</li>
        ///      </ul>
        /// </summary>

        // private constants for handling different SQL commands
        private const int InsertCommand = 0;
        private const int DeleteCommand = 1;
        private const int UpdateCommand = 2;
        private const int SelectCommand = 3;
#endregion

#region internal member variables
        ///<summary>
        ///   The following are object variables:
        ///   <ul>
        ///      <li>SqlConnection SqlConn: a connection object</li>
        ///      <li>SqlCommand    SqlCmd:  a SQL command object</li>
        ///      <li>StringBuilder _ConnectionString: a dynamic string
        ///          object that is used to build a connection string</li>
        ///      <li>SqlDataReader SqlReader: a SqlDataReader object</li>
        ///    </ul>
        ///</summary>

        private SqlConnection SqlConn           = new SqlConnection ();
        private SqlCommand    SqlCmd            = new SqlCommand ();
        private StringBuilder _ConnectionString = null;
        private SqlDataReader SqlReader         = null;
        private Hashtable     htDataView        = new Hashtable ();


        /// <summary>
        ///   the following private variable are used to build
        ///   a connection string
        ///   <ul>
        ///       <li>string _DataSource: a string variable that
        ///           represent a data source. Default to local machine</li>
        ///       <li>string _InitCatalog: an initialized database.
        ///           Default to user's prespective. In this case,
        ///           it is set to PhotoDB</li>
        ///       <li>string _IntegratedSecurity: whether to use
        ///           SQL Server's integrated security or not. Default is
        ///           set to SSPI, which will use integrated security
        ///           feature. Set to false will disable it.</li>
        ///       <li>string _PersistSecurityInfo: if we want to persist
        ///           the connection string or not. Default is not. Set it to
        ///           ture will enable persistence connection
        ///           string to file</li>
        ///       <li>string _UID: user id for access the database</li>
        ///       <li>string _PWD: password associate a given user
        ///           for accessing a particular database</li>
        ///       <li>int _PacketSize: a network packet size transfer
        ///           between SQL Server and Client.
        ///           Default is 4096 bytes</li>
        ///    </ul>
        /// </summary>
        private string _DataSource          = "(local)";
        private string _InitCatalog         = null;
        private string _IntegratedSecurity  = "SSPI";
        private string _PersistSecurityInfo = "false";
        private int    _ConnectionTimeout   = 600;
        private string _UID                 = null;
        private string _PWD                 = null;
        private int    _PacketSize          = 4096;
#endregion

        // SQL command enumeration type
        /// <summary>
        /// SqlCmdType is an enumeration type that
        /// constins a sql command to be executed.
        /// </summary>
        public enum SqlCmdType {
            InsertCommand = 0,
            DeleteCommand,
            UpdateCommand,
            SelectCommand
        };

#region public property methods
        /// <summary>
        /// property DataSource -
        ///     get/set a database source (aka SQL Server) that we connect to.
        /// </summary>
        public string DataSource {
            get {
                return _DataSource;
            }
            set {
                _DataSource = value;
            }
        }


        /// <summary>
        /// property InitCatlog -
        ///     get/set the database to be
        ///     used
        /// </summary>
        public string InitCatlog {
            get {
                return _InitCatalog;
            }
            set {
                _InitCatalog = value;
            }
        }


        /// <summary>
        /// property IntegratedSecurity -
        ///     get/set if we need to use Integrated Security (SSPI)
        /// </summary>
        public string IntegratedSecurity {
            get {
                return _IntegratedSecurity;
            }
            set {
                _IntegratedSecurity = value;
            }
        }


        /// <summary>
        /// property UserName -
        ///     get/set the database user used to
        ///     connect to a given database server
        /// </summary>
        public string UserName {
            get {
                return this._UID;
            }
            set {
                this._UID = value;
            }
        }

        /// <summary>
        /// property UserPassword -
        ///     get/set the password associate with the user
        ///     connect to the database server.
        /// </summary>
        public string UserPassword {
            get {
                return this._PWD;
            }
            set {
                this._PWD = value;
            }
        }

        /// <summary>
        /// property persistSecurityInfo -
        ///     get/set if the security information
        ///     needs to be persisted on a disk
        /// </summary>
        public string PersistSecurityInfo {
            get {
                return _PersistSecurityInfo;
            }
            set {
                _PersistSecurityInfo = value;
            }
        }


        /// <summary>
        /// property PacketSize -
        ///     get/set the packet size used for
        ///     trafering data between client and sql
        ///     server.
        /// </summary>
        public int PacketSize {
            get {
                return _PacketSize;
            }
            set {
                _PacketSize = value;
            }
        }

        /// <summary>
        /// property ConnectionString -
        ///     gets the connection string used to
        ///     connect to a given sql server.
        /// </summary>
        public string ConnectionString {
            get {
                return _ConnectionString.ToString ();
            }
        }


        /// <summary>
        /// property isConnect -
        ///     gets a boolean value to indicate if
        ///     a given connection is open
        /// </summary>
        public bool isConnect {
            get {
                return SqlConn.State == ConnectionState.Open
                       ? true : false;
            }
        }
#endregion

#region various constractors

        /// <summary>
        /// public DBAccess() -
        ///     a initialize constructor that do nothing.
        /// </summary>
        public DBAccess () : base() {}

        /// <summary>
        /// public DBAccess( string DataSource, string InitCatalog ) -
        ///     a constructor that takes two parameters,
        ///     DataSource - a SQL server to connect to
        ///     InitCatalog - a database on a given SQL server to be used.
        ///
        ///     Note: it will use the Windows Authentication to connect to
        ///     sql server.  The user who use this constructor has to have
        ///     a Domain Account that is allow to access a given SQL Server.
        /// </summary>
        /// <param name="DataSource">string type variable that accepts the name of a sql server</param>
        /// <param name="InitCatalog">string type variable that accepts the name of a database</param>
        public DBAccess (string DataSource, string InitCatalog) {
            _DataSource  = DataSource;
            _InitCatalog = InitCatalog;

            BuildConnectionString ();

            try {
                if ( SqlConn.State != ConnectionState.Open ) {
                    SqlConn.ConnectionString = _ConnectionString.ToString ();
                    SqlConn.Open ();
                }
            } catch ( SqlException se ) {
                StringBuilder sbExceptionMsg = new StringBuilder ();
                sbExceptionMsg.AppendFormat("unable connect to server: {0}, database: {1}, reason: {2}", 
                                                               SqlConn.DataSource, 
                                                               SqlConn.Database, 
                                                               se.Message);
                throw new Exception (sbExceptionMsg.ToString ());
            }
        }


        /// <summary>
        /// public DBAccess( string DataSource,
        ///                  string InitCatalog,
        ///                  string UserID,
        ///                  string UserPWD)
        ///     an overloaded constructor that takes 4 parameters:
        ///
        ///         DataSource  - a valid SQL Server
        ///         InitCatalog - an existing database on a SQL Server
        ///         UserID      - a user who has correnct permission to asscess
        ///                       the sql server and database on it.
        ///         UserPWD     - a password associates with a given user to
        ///                       access to that SQL Server and database on it.
        ///
        ///     Note: by using this constructor, you instruct the object to use
        ///           SQL Server standard login method to access a SQL Server.
        ///           The user and password you provided must have already existed
        ///           and the correct permissions has to be set.
        /// </summary>
        /// <param name="DataSource">string type variable that accepts the name of a SQL Server</param>
        /// <param name="InitCatalog">string type variable that accepts the name of a database on a SQL Server</param>
        /// <param name="UserID">a valid user on the SQL Server</param>
        /// <param name="UserPWD">a valid password assoicates with a user</param>
        public DBAccess( string DataSource, string InitCatalog, string UserID, string UserPWD) {
            _DataSource  = DataSource;
            _InitCatalog = InitCatalog;
            _UID         = UserID;
            _PWD         = UserPWD;
            BuildConnectionString ();

            try {
                if ( SqlConn.State != ConnectionState.Open ) {
                    SqlConn.ConnectionString = _ConnectionString.ToString ();
                    SqlConn.Open ();
                }
            } catch ( SqlException se ) {
                StringBuilder sbExceptionMsg = new StringBuilder ();
                sbExceptionMsg.AppendFormat("unable connect to server: {0}, database: {1}, reason: {2}",
                                            SqlConn.DataSource, SqlConn.Database, se.Message);
                throw new Exception(sbExceptionMsg.ToString ());
            }

        }

#endregion

#region private utility methods
        /// <summary>
        /// private void BuildConnectionString() -
        ///     a prviate method to build the SQL Server
        ///     connection string.
        /// </summary>
        private void BuildConnectionString () {
            if ( _ConnectionString == null) {
                _ConnectionString = new StringBuilder ();
            }

            if ( this.UserName            != null &&
                    this.UserPassword        != null &&
                    this.UserName.Length     > 0     &&
                    this.UserPassword.Length > 0 ) {
                _IntegratedSecurity = "false";
            }

            _ConnectionString.AppendFormat("data source={0};",           this.DataSource);
            _ConnectionString.AppendFormat("initial catalog={0};",       this.InitCatlog);
            _ConnectionString.AppendFormat("integrated security={0};",   this.IntegratedSecurity);
            _ConnectionString.AppendFormat("persist security info={0};", this.PersistSecurityInfo);
            _ConnectionString.AppendFormat("packet size={0};",           this.PacketSize);
            _ConnectionString.AppendFormat("Connect Timeout={0};",       this._ConnectionTimeout);

            if ( this.UserName               != null &&
                    this.UserPassword        != null &&
                    this.UserName.Length     > 0     &&
                    this.UserPassword.Length > 0     &&
                    this.IntegratedSecurity.IndexOf("false") > -1 ) {
                _ConnectionString.AppendFormat ("uid={0};", this.UserName);
                _ConnectionString.AppendFormat ("pwd={0};", this.UserPassword);
            }
        }



        public DataRow[] ExecuteStoredProc( string StoredProc,
                                            string OrderByCol,
                                            string PersistentFile,
                                            params SqlParameter[] SqlParams ) {
            SqlCommand     SqlStoredProc = null;
            SqlDataAdapter sda           = null;
            DataSet        ds            = new DataSet();
            DataRow[]      Rows          = null;
            try {
                SqlStoredProc             = new SqlCommand( StoredProc, this.SqlConn );
                SqlStoredProc.CommandType = CommandType.StoredProcedure;
                if ( SqlStoredProc.Connection.State != ConnectionState.Open )
                    SqlStoredProc.Connection.Open();

                if ( SqlParams.Length > 0 )
                    foreach ( SqlParameter SqlParam in SqlParams )
                    SqlStoredProc.Parameters.Add( SqlParam );

                if ( OrderByCol.Length    > 0 &&
                        PersistentFile.Length > 0 &&
                        SqlParams.Length == 0 ) {
                    sda = new SqlDataAdapter( SqlStoredProc );
                    sda.Fill( ds );
                    if ( OrderByCol.Length > 0 )
                        Rows = ds.Tables[0].Select( null, OrderByCol );
                    else
                        Rows = ds.Tables[0].Select();

                    if ( PersistentFile.Length > 0 )
                        ds.WriteXml( PersistentFile );

                } else {
                    SqlStoredProc.ExecuteNonQuery();
                }


            } catch ( SqlException ) {
                throw;
            }
            return Rows;
        }



        public ArrayList ExecuteStoredProc( string         StoredProc,
                                            bool           HasMultiResults,
                                            SqlParameter[] SqlParams ) {
            ArrayList Results    = new ArrayList();
            SqlCommand MySqlCmd  = new SqlCommand( StoredProc, this.SqlConn );
            MySqlCmd.CommandType = CommandType.StoredProcedure;

            try {
                if ( MySqlCmd.Connection.State != ConnectionState.Open )
                    MySqlCmd.Connection.Open();

                if ( SqlParams.Length > 0 )
                    foreach( SqlParameter SqlParam in SqlParams )
                    MySqlCmd.Parameters.Add( SqlParam );

                SqlDataReader sdr = MySqlCmd.ExecuteReader();

                // first result set
                try {

                    while ( sdr.Read() ) {
                        // string ColName = sdr.GetName(0);
                        ArrayList Fields = new ArrayList();
                        for ( int i = 0; i < sdr.FieldCount; i++ ) {
                            string FieldName = sdr.GetName(i);
                            string Value     = String.Format( "{0}:{1}",
                                               FieldName,
                                               sdr.GetSqlValue(i) );
                            Fields.Add( Value );

                        }
                        if ( Fields != null &&
                                Fields.Count > 0)
                            Results.Add( Fields );
                    }

                    // check to see if we have more and advance
                    // to next.

                    while ( sdr.NextResult() ) {
                        while ( sdr.Read() ) {
                            ArrayList Fields = new ArrayList();
                            for ( int i = 0; i < sdr.FieldCount; i++ ) {
                                string FieldName = sdr.GetName(i);
                                string Value     =
                                    String.Format( "{0}:{1}",
                                                   FieldName,
                                                   sdr.GetSqlValue(i) );
                                Fields.Add( Value );
                            }
                            Results.Add( Fields );
                        }
                    }
                } catch ( SqlException se ) {
                    base.LogItWithTimeStamp( se.Message );
                } finally {
                    sdr.Close();
                }
            } catch ( SqlException ) {
                throw;
            }


            return Results;
        }


        public ArrayList ExecuteStoredProc( string         StoredProcName,
                                            string         ColName,
                                            SqlParameter[] SqlParams ) {
            ArrayList Results    = new ArrayList();
            SqlCommand MySqlCmd  = new SqlCommand( StoredProcName, this.SqlConn );
            MySqlCmd.CommandType = CommandType.StoredProcedure;

            try {
                if ( MySqlCmd.Connection.State != ConnectionState.Open )
                    MySqlCmd.Connection.Open();

                if ( SqlParams.Length > 0 )
                    foreach( SqlParameter SqlParam in SqlParams )
                    MySqlCmd.Parameters.Add( SqlParam );

                SqlDataReader sdr = MySqlCmd.ExecuteReader();

                // first result set
                try {
                    while ( sdr.Read() ) {
                        ArrayList Fields = new ArrayList();
                        for ( int i = 0; i < sdr.FieldCount; i++ ) {
                            string Value = String.Format( "{0}:{1}",
                                                          ColName,
                                                          sdr[ColName] );
                            Fields.Add( Value );

                        }
                        if ( Fields != null && Fields.Count > 0)
                            Results.Add( Fields );
                    }

                    // check to see if we have more and advance
                    // to next.

                    while ( sdr.NextResult() ) {
                        while ( sdr.Read() ) {
                            ArrayList Fields = new ArrayList();
                            for ( int i = 0; i < sdr.FieldCount; i++ ) {
                                string Value = String.Format( "{0}:{1}", ColName, sdr[ColName] );
                                Fields.Add( Value );
                            }
                            Results.Add( Fields );
                        }
                    }
                } catch ( SqlException se ) {
                    base.LogItWithTimeStamp( se.Message );
                } finally {
                    sdr.Close();
                }
            } catch ( SqlException ) {
                throw;
            }


            return Results;

        }


        /// <summary>
        /// private SqlDataAdapter CreateSqlDataAdapter( string sqlStmt, SqlCmdType CmdType ) -
        ///     a private method that creates an SqlDataAdapter object and returns it
        ///     back to the caller.  It accepts two parameters:
        ///
        ///         string sqlStmt     - a valid T-SQL statment.
        ///         SqlCmdType CmdType - a type of command you want to execute.
        ///
        ///     When successfully executed, a SqlDataAdapter object will be returned.
        /// </summary>
        /// <param name="sqlStmt">string type variable that accept a valid SQL Statement</param>
        /// <param name="CmdType">
        ///     an enumeration type of SqlCmdType that accepts the following
        ///     enumeration:
        ///
        ///         Insert
        /// </param>
        /// <returns></returns>
        private SqlDataAdapter CreateSqlDataAdapter (string sqlStmt, SqlCmdType CmdType) {
            int SqlCmd                       = (int) CmdType;
            SqlDataAdapter mySqlAdapter      = new SqlDataAdapter();
            mySqlAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

            try {

                if ( SqlConn.State != ConnectionState.Open )
                    SqlConn.Open();

                SqlCommand mySqlCommand = new SqlCommand (sqlStmt, SqlConn);
                switch (SqlCmd) {
                    case InsertCommand:
                        mySqlAdapter.InsertCommand = mySqlCommand;
                        break;
                    case DeleteCommand:
                        mySqlAdapter.UpdateCommand = mySqlCommand;
                        break;
                    case UpdateCommand:
                        mySqlAdapter.DeleteCommand = mySqlCommand;
                        break;
                    case SelectCommand:
                        mySqlAdapter.SelectCommand = mySqlCommand;
                        break;
                    default:
                        throw new System.Exception ("unknown command type!!");
                }
            } catch ( SqlException e ) {
                throw new Exception ("CreateSqlDataAdapter: error happen" +
                                     e.Message);
            } finally {
                SqlConn.Close();
            }

            return mySqlAdapter;
        }


#endregion

#region public methods

        /// <summary>
        /// public void Connect () - a method that connects to
        /// a given SQL server
        /// </summary>
        public void Connect () {
            try {
                // if connection is not open, then open it.
                if (SqlConn.State != ConnectionState.Open ) {
                    BuildConnectionString ();
                    SqlConn.ConnectionString = _ConnectionString.ToString ();
                    SqlConn.Open ();
                }
            } catch ( SqlException e ) {
                throw e;
            }
        }


        /// <summary>
        /// public void Connect( string ConnectionString ) -
        ///     an overloaded method that accepts one parameter
        /// </summary>
        /// <param name="ConnectionString"></param>
        public void Connect ( string ConnectionString ) {
            try {
                if ( SqlConn.State != ConnectionState.Open ) {
                    SqlConn.ConnectionString = ConnectionString;
                    SqlConn.Open ();
                }
            } catch ( SqlException se ) {
                throw se;
            }
        }


        /// <summary>
        /// public void ExecuteNonQuery( string strSQLStmt ) -
        ///     Executing a non select SQL statement such as
        ///     Insert, Delete, and Update.  Accepts one parameter,
        ///     strSQLStmt which is a valid DML statment.
        /// </summary>
        /// <param name="strSQLStmt">
        /// string type of variable that contains a valid SQL statment
        /// that don't return any record set.
        /// </param>
        public void ExecuteNonQuery( string strSQLStmt ) {
            try {
                if ( this.SqlConn.State != ConnectionState.Open )
                    this.SqlConn.Open();
                SqlCommand SqlCmd = new SqlCommand( strSQLStmt, this.SqlConn );
                SqlCmd.CommandText = strSQLStmt;
                SqlCmd.ExecuteNonQuery ();
            } 
            catch ( SqlException se ) {
                throw se;
            } 
            finally {
                this.SqlConn.Close();
            }

        }


        /// <summary>
        /// public SqlDataReader RunQuery( string sqlStmt ) -
        ///     executing a given T-SQL statment and return the
        ///     SqlDataReader object back to the caller.
        ///
        ///     Note: this method execute a sql statment that has
        ///     a return result set.  For statment that don't return
        ///     result set <see cref="DBAccess.ExecuteNonQuery"/>
        /// </summary>
        /// <param name="sqlStmt">
        /// type of string variable that contains a valid sql statement that
        /// will return result set.
        /// </param>
        /// <returns></returns>
        public SqlDataReader RunQuery (string sqlStmt) {
            SqlCmd.Connection  = SqlConn;
            SqlCmd.CommandText = sqlStmt;

            try {
                if ( SqlCmd.Connection.State != ConnectionState.Open )
                    SqlCmd.Connection.Open();
                SqlReader = SqlCmd.ExecuteReader ();
            } catch ( SqlException e ) {
                throw e;
            } finally {
                SqlCmd.Connection.Close();
            }
            return SqlReader;
        }


        /// <summary>
        /// public SqlDataAdapter RunQuery( string sqlStmt, SqlCmdType CmdType ) -
        ///     an overloaded method that return a SqlDataAdapter object and
        ///     accepts two parameters:
        ///
        ///         sqlStmt - a vaild SQL Statement
        ///         CmdType - a type of command being executed.
        /// </summary>
        /// <param name="sqlStmt">string type of variable that contains a valid T-SQL statement</param>
        /// <param name="CmdType">
        /// an enumeration type of SqlCmdType contains one of the following commands:
        ///     Insert, Delete, Update, and Select
        /// </param>
        /// <returns></returns>
        public SqlDataAdapter RunQuery (string sqlStmt, SqlCmdType CmdType) {
            return CreateSqlDataAdapter (sqlStmt, CmdType);
        }


        /// <summary>
        /// public DataSet RunQuery( string DataSetName, string sqlStmt, SqlCmdType, cmdType ) -
        ///     an overloaded the method that takes 3 parameters:
        ///         DataSetName, sqlStmt, and cmdType
        ///     and return a DataSet object when called is successfully executed.
        /// </summary>
        /// <param name="DataSetName">a string type variable that contains the name of the DataSet</param>
        /// <param name="sqlStmt">a string type variable that contains a valid T-SQL statement</param>
        /// <param name="cmdType">an enumeration type of SqlCmdType</param>
        /// <returns></returns>
        public DataSet RunQuery (string     DataSetName,
                                 string     sqlStmt,
                                 SqlCmdType cmdType) {
            DataSet ds = null;
            try {
                ds = new DataSet (DataSetName);
                SqlDataAdapter mySqlDataAdapter = CreateSqlDataAdapter (sqlStmt, cmdType);
                mySqlDataAdapter.Fill (ds, DataSetName);
            } catch ( SqlException e ) {
                throw e;
            }
            return ds;
        }


        /// <summary>
        /// public SqlDataAdapter Insert( string insertStmt ) -
        ///     This method perform an insert against a give table.
        ///     The parameter insertStmt is a valid T-SQL insert
        ///     statement.
        ///
        ///     Note: The method use SqlDataAdapter to insert data
        ///     into the underline table.  It works on single table
        ///     only.
        /// </summary>
        /// <param name="insertStmt">
        /// string type variable that accepts a valid T-SQL insert statement
        /// </param>
        /// <returns>SqlDataAdapter object</returns>
        public SqlDataAdapter Insert (string insertStmt) {
            return CreateSqlDataAdapter(insertStmt, SqlCmdType.InsertCommand);
        }


        /// <summary>
        /// return the connection objects
        /// </summary>
        public SqlConnection ConnectionObject
        {
            get {
                return this.SqlConn;
            }
        }


        /// <summary>
        /// public DataSet Insert( string dataSetName, string insertStmt ) -
        ///     an overloaded Insert method that inserts data into an
        ///     underline table and return a DataSet object back to caller.
        /// </summary>
        /// <param name="dataSetName">string variable that contains the name of a DataSet</param>
        /// <param name="insertStmt">string variable that contains a valid T-SQL insert statment</param>
        /// <returns>DataSet object</returns>
        public DataSet Insert (string dataSetName, string insertStmt) {
            return RunQuery(dataSetName, insertStmt, SqlCmdType.InsertCommand);
        }


        /// <summary>
        /// public SqlDataAdapter Delete( string deleteStmt ) -
        ///     This method perform a delete operation on a given
        ///     table and return the SqlDataAdapter object.
        /// </summary>
        /// <param name="deleteStmt">string variable that contains a valid T-SQL delete statment</param>
        /// <returns>SqlDataAdapter object</returns>
        public SqlDataAdapter Delete (string deleteStmt) {
            return CreateSqlDataAdapter (deleteStmt, SqlCmdType.DeleteCommand);
        }


        /// <summary>
        /// public DataSet Delete( string dataSetName, deleteStmt ) -
        ///     an overloaded method that perform a delete operation against
        ///     a given table and return the DataSet object
        /// </summary>
        /// <param name="dataSetName">string variable that contains the name of the DataSet</param>
        /// <param name="deleteStmt">string variable that contains a valid T-SQL delete statement</param>
        /// <returns></returns>
        public DataSet Delete (string dataSetName, string deleteStmt) {
            return RunQuery (dataSetName, deleteStmt, SqlCmdType.DeleteCommand);
        }


        /// <summary>
        /// public SqlDataAdapter Update( string updateStmt ) -
        ///     Perform an update operation against an underline table
        ///     and return a SqlDataAdapter object.
        /// </summary>
        /// <param name="updateStmt">string variable that contains a valid T-SQL unpdate statment</param>
        /// <returns>SqlDataAdapter object</returns>
        public SqlDataAdapter Update (string updateStmt) {
            return CreateSqlDataAdapter(updateStmt, SqlCmdType.UpdateCommand);
        }


        /// <summary>
        /// public DataSet Update( string dataSetName, string updateStmt ) -
        ///     an overloaded method that perform an update operation against
        ///     an underline table and return a DataSet object.
        /// </summary>
        /// <param name="dataSetName">string variable that contains the name of the DataSet</param>
        /// <param name="updateStmt">string variable that contains a valid T-SQL update statement</param>
        /// <returns></returns>
        public DataSet Update(string dataSetName, string updateStmt) {
            return RunQuery(dataSetName, updateStmt, SqlCmdType.UpdateCommand);
        }


        /// <summary>
        /// public SqlDataAdapter Select( string selectStmt ) -
        ///     Perform a select operation against underlinng
        ///     database and return the SqlDataAdapter object.
        /// </summary>
        /// <param name="selectStmt">string variable that contains a valid T-SQL select statement</param>
        /// <returns>a SqlDataAdapter object</returns>
        public SqlDataAdapter Select (string selectStmt) {
            return CreateSqlDataAdapter (selectStmt, SqlCmdType.SelectCommand);
        }


        /// <summary>
        /// public DataSet Select( string dataSetName, string selectStmt ) -
        ///     an overloaded method that performs a select operation on the
        ///     underlining database tables and returns a DataSet object.
        /// </summary>
        /// <param name="dataSetName">string variable contains the name of the DataSet</param>
        /// <param name="selectStmt">string variable contains a valid T-SQL select statement</param>
        /// <returns></returns>
        public DataSet Select (string dataSetName, string selectStmt) {
            DataSet ds = RunQuery (dataSetName, selectStmt, SqlCmdType.SelectCommand);

            if ( !htDataView.ContainsKey (dataSetName) )
                htDataView.Add (dataSetName, ds.Tables[dataSetName].DefaultView);
            return ds;
        }


        /// <summary>
        /// public DataView this[string dataSetName] -
        ///     an indexer method that retrive a given
        ///     DataSet object from the internal hash table.
        ///     string DataSetName is the key used to search
        ///     in the hash table.
        /// </summary>
        public DataView this[string dataSetName]
        {
            get {
                if ( htDataView.ContainsKey (dataSetName) )
                    return (DataView) htDataView[dataSetName];
                return null;
            }
        }


        /// <summary>
        /// public SqlDataReader ResultSet -
        ///     gets a SqlDataReader object.
        /// </summary>
        public SqlDataReader ResultSet
        {
            get { return SqlReader; }
        }


        /// <summary>
        /// public void CloseResultSet() -
        ///     Close an open SqlDataReader object.
        /// </summary>
        public void CloseResultSet () {
            SqlReader.Close();
        }


        /// <summary>
        /// public new void Close() -
        ///     Close an open connection object.
        /// </summary>
        public new void Close () {
            try {
                if (SqlConn != null)
                    SqlConn.Dispose ();
            } catch ( SqlException e ) {
                Console.WriteLine (e.ToString ());
            }
        }
#endregion
    }


#region testing code is commented out
    /*
        class TestDBAccess
        {
           public static void Main ()
           {
             DBAccess db = new DBAccess 
               ("12.230.153.29", "DVD", "mikelia", "1234");
             db.Connect ();
             db.RunQuery 
               ("select AcademyAwardID, AcademyAwardYear, " +
                       "AwardType, MovieID, MoviePersonID " +
                       "from AcademyAward order by AcademyAwardID");
             Console.WriteLine ("AcademyAwardID, AcademyAwardYear, " +
                                "AwardType, MovieID, MoviePersonID, " +
                                "AwardType, MovieID, MoviePersonID" +
                                "-----------------------------------------");
             while ( db.ResultSet.Read () ) {
               Console.WriteLine (db.ResultSet[0]);
             }
             db.CloseResultSet ();
             db.RunQuery ("select adtrackid from adtrack");
             db.CloseResultSet ();
             db.Close();
           }
        }
    */

#endregion
}
