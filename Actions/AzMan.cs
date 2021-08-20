using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml;

using Microsoft.Interop.Security.AzRoles;

namespace XInstall.Core.Actions {
  /// <summary>
  /// a class to build an Authorization Manager store
  /// </summary>
  public class AzMan : DBAccess, IAction {
    // the XML node contains insturctions to build AzMan
    // store
    private XmlNode _ActionNode   = null;

    // these are prviate variables that corresponding to the class properties
    private string _ConnectTo     = String.Empty;   // database server connected
    private string _DBName        = String.Empty;   // database to be used
    private string _StoreLocation = String.Empty;   // where should we put the store file
    private string _ActionTable   = String.Empty;   // table we want to use if any
    private string _AppName       = String.Empty;   // AzMan's application name
    private string _Description   = String.Empty;   // AzMan appliation description
    private string _ActionQuery   = String.Empty;   // query that send to database
    private string _XmlOutputFile = String.Empty;   // the output xml file
    private string _XmlFileName   = String.Empty;   // the input xml file

    // boolean variables
    private bool   _ReadFromDB    = true;       // should data be read from database or not
    private bool   _DeleteStore   = true;       // delete xml file before creating it
    private bool   _WriteToXml    = true;       // write AzMan info into an Xml file or not
    private bool   _LoadFromXmlFile = false;    // load data from xml

    // store type: can be XML or Active Directory (AD)
    private enum STORETYPE {
      XML = 0,
      AD,
    }
    STORETYPE _StoreType = STORETYPE.XML;

    private enum AZSTOREAGE_TYPE {
      AZ_ACCESS_CHECK = 0,
      AZ_CREATE_STORE,
      AZ_MANAGE_STORE_ONLY,
      AZ_BATCH_UPDATE,
    }
    // private AZSTOREAGE_TYPE _azStoreType = 0;

    private enum CLASS_TYPE {
      OPERATION = 1,
      TASK,
      ROLE,
      GROUP
    }

    /// <summary>
    /// contructor that initializes the AzMan object.
    /// </summary>
    /// <param name="xn">an xml node that contains instructions to
    /// create AzMan store</param>
    [Action("azman")]
    public AzMan( XmlNode xn ) : base() {
      this._ActionNode = xn;
    }

#region public methods/properties

    /// <summary>
    /// get/set a database server that AzMan object connects to
    /// </summary>
    [Action("connectto", Needed=true)]
    public string ConnectTo {
      get {
        return this._ConnectTo;
      }
      set {
        this._ConnectTo = value;
      }
    }

    /// <summary>
    /// get/set the database AzMan object talks to
    /// </summary>
    [Action("dbname", Needed=true)]
    public string DBName {
      get {
        return this._DBName;
      }
      set {
        this._DBName = value;
      }
    }

    /// <summary>
    /// get/set the table that contains the operations information
    /// </summary>
    [Action("actiontable", Needed=false)]
    public string ActionTable {
      get {
        return String.Format(@"select ActionID, Name from {0}", this._ActionTable);
      }
      set {
        this._ActionTable = value;
      }
    }

    /// <summary>
    /// get/set the query that run against the database table
    /// </summary>
    [Action("actionquery", Needed=false)]
    public string ActionSQLQuery {
      get {
        return this._ActionQuery;
      }
      set {
        this._ActionQuery = value;
      }
    }

    /// <summary>
    /// get/set the AzMan store location
    /// </summary>
    [Action("storelocation", Needed=true)]
    public string StoreLocation {
      get {
        return this._StoreLocation;
      }
      set {
        this._StoreLocation = value;
      }
    }

    /// <summary>
    /// set the AzMan store type
    /// </summary>
    [Action("storetype", Needed=false, Default="XML")]
    public string StoreType {
      set {
        switch ( value.ToUpper() ) {
          case @"XML":
            this._StoreType = STORETYPE.XML;
            break;
          case @"AD":
            this._StoreType = STORETYPE.AD;
            break;
          default:
            string Message = @"unknown store type: {0}";
            throw new ArgumentException( Message, value );
        }
      }
    }

    /// <summary>
    /// get/set a flag to indicate if AzMan should read data from a file or
    /// database.
    /// </summary>
    [Action("readfromdb", Needed=false, Default="true")]
    public string ReadFromDB {
      set {
        this._ReadFromDB = bool.Parse( value );
      }
    }

    /// <summary>
    /// get/set the Application Name for the AzMan
    /// </summary>
    [Action("appname", Needed=true)]
    public string AppName {
      get {
        return this._AppName;
      }
      set {
        this._AppName = value;
      }
    }


    /// <summary>
    /// set a flag to indicate if AzMan should delete a store file or not
    /// before creating an AzMan store file
    /// </summary>
    [Action("delstore", Needed=false, Default="true")]
    public string DeleteStore {
      set {
        this._DeleteStore = bool.Parse( value );
      }
    }

    /// <summary>
    /// get/set a description for the AzMan application object
    /// </summary>
    [Action("description", Needed=false, Default="")]
    public string Description {
      get {
        return this._Description;
      }
      set {
        this._Description = value;
      }
    }

    /// <summary>
    /// set a flag to indicate if database info should be
    /// written to database or not
    /// </summary>
    [Action("toxml", Needed=false, Default="true")]
    public string ToXml {
      set {
        this._WriteToXml = bool.Parse( value );
      }
    }

    /// <summary>
    /// get/set the xml file name to be written to
    /// </summary>
    [Action("xmlfilename", Needed=false)]
    public string OutputXmlFile {
      get {
        return this._XmlOutputFile;
      }
      set {
        this._XmlOutputFile = value;
      }
    }

    [Action("loadfromxml", Needed=false, Default="false")]
    public string LoadFromXml {
      set {
        this._LoadFromXmlFile = bool.Parse( value );
      }
    }

    [Action("xmlfile", Needed=false, Default="")]
    public string XmlFile {
      get {
        return this._XmlFileName;
      }
      set {
        this._XmlFileName = value;
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

#region derived method from ActionElement
    /// <summary>
    ///  an override method to parse the XML node from config.xml file
    /// </summary>
    public override void ParseActionElement() {
      this.CreateStoreFromDB();
    }

    public override string ObjectName {
      get {
        return this.Name;
      }
    }
#endregion

#region private methods/properties
    private string GetStoreLocation() {
      string Location = String.Empty;
      switch (this._StoreType) {
        case STORETYPE.XML:
          Location = String.Format( @"msxml://{0}", this.StoreLocation );
          break;
        case STORETYPE.AD:
          Location = String.Format( @"msldap://{0}", this.StoreLocation );
          break;
      }
      return Location;
    }


    private void CreateStoreFromDB() {
      // Prepare AzMan
      AzAuthorizationStoreClass AzStore = new AzAuthorizationStoreClass();

      // delete the xml file if one already exist!
      // and create new one
      if ( this._DeleteStore )
        if ( File.Exists( this.StoreLocation ) ) {
          File.Delete( this.StoreLocation );
        }

      AzStore.Initialize( (int) tagAZ_PROP_CONSTANTS.AZ_AZSTORE_FLAG_CREATE,
          this.GetStoreLocation(),
          null );
      AzStore.Submit( 0, null );

      // now create AzMan AzApp ...
      IAzApplication AzApp = AzStore.CreateApplication( this.AppName, null );
      AzApp.Description    = this.Description;
      AzApp.Submit( 0, null );

      // connect to database
      base.DataSource         = this.ConnectTo;
      base.InitCatlog         = this.DBName;
      base.IntegratedSecurity = "true";
      base.Connect();

      // various variables for AzMan
      IAzOperation  AzOp = null;
      SqlDataReader sdr  = null;
      IAzRole AzRole     = null;
      IAzTask AzTask     = null;
      IAzScope AzScope   = null;

      string sTaskName = string.Empty;
      string sMemName = string.Empty;

      try {
        // obtains a content from the AzMan XML node
        XmlNode SQL = this._ActionNode.SelectSingleNode( @"sql" );
        if ( SQL != null ) {
          this.ActionSQLQuery = SQL.FirstChild.Value.ToString();
        }
        sdr = base.RunQuery( this.ActionSQLQuery );

        // first query
        // create operation
        while ( sdr.Read() ) {
          AzOp = AzApp.CreateOperation( sdr[@"MemName"].ToString(), null );
          AzApp.Submit( 0, null );
          AzOp.Description = sdr[@"MemDesc"].ToString();
          AzOp.Submit( 0, null );
          AzOp.OperationID = Convert.ToInt32( sdr[@"MemID"] );
          AzOp.Submit( 0, null );
          base.LogItWithTimeStamp(
              String.Format( "Operation ID:{0} - Name: {1}",
                AzOp.OperationID, AzOp.Name ) );
        }

        // second query
        // create tasks
        Hashtable Tasks = new Hashtable();
        if ( sdr.NextResult() ) {
          while ( sdr.Read() ) {
            string MemName = sdr["MemName"].ToString();
            string MemDesc = sdr["MemDesc"].ToString();
            string[] TaskInfo =
            { sdr["MemName"].ToString(), sdr["MemDesc"].ToString() };
            AzTask = AzApp.CreateTask( sdr["MemName"].ToString(), null );
            AzApp.Submit( 0, null );
            AzTask.Description = sdr["MemDesc"].ToString();
            AzTask.Submit( 0, null );
            Tasks.Add( Convert.ToInt32(sdr["MemID"]), AzTask );
            base.LogItWithTimeStamp( String.Format( "TaskID: {0}, TaskName:{1}", sdr["MemID"], MemName) );
          }
        }

        // create Scope
        AzScope = AzApp.CreateScope( @"AllRoutines", null );
        AzApp.Submit( 0, null );
        AzScope.Description = @"RPM Authorization Scope";
        AzScope.Submit ( 0, null );


        // third query
        // add operations to Tasks
        Hashtable Trace = new Hashtable();
        if ( sdr.NextResult() ) {
          while ( sdr.Read() ) {
            int MemParID = Convert.ToInt32( sdr[@"MemParID"] );
            int OpID     = Convert.ToInt32( sdr[@"OpID"] );
            string MemName = sdr[@"MemName"].ToString();
            if ( Tasks.ContainsKey ( MemParID ) ) {
              IAzTask azTask = (IAzTask) Tasks[MemParID];
              sTaskName = azTask.Name;
              sMemName  = MemName;
              if ( !Trace.ContainsKey( azTask.Name ) && !Trace.ContainsValue( MemName.Trim(null) ) ) {

                if ( OpID > 500 ) {
                  azTask.AddTask( MemName.Trim(null), 0 );
                  azTask.Submit( 0, null );
                  base.LogItWithTimeStamp( String.Format( "Task: {0} ID:{1} - Nest Task {2}", azTask.Name, MemParID, MemName ) );
                } else {
                  azTask.AddOperation( MemName.Trim(null), null );
                  azTask.Submit( 0, null );
                  Trace.Add( azTask.Name, MemName.Trim(null) );
                  base.LogItWithTimeStamp( String.Format( "Task:{0} ID:{1} - Operation {2}", azTask.Name, MemParID, MemName ) );
                }

              } else {
                if ( OpID > 500 ) {
                  azTask.AddTask( MemName.Trim(null), 0 );
                  azTask.Submit( 0, null );
                  base.LogItWithTimeStamp(
                      String.Format( "Task: {0} ID:{1} - Nest Task {2}", azTask.Name, MemParID, MemName ) );
                } else {
                  Trace.Clear();
                  azTask.AddOperation( MemName.Trim(null), null );
                  azTask.Submit( 0, null );
                  Trace.Add( MemName.Trim(null), MemName.Trim(null) );
                  base.LogItWithTimeStamp(
                      String.Format( "Task:{0} ID: {1} - Operation: {2}", azTask.Name, MemParID, MemName ) );
                }
              }
            }
          }
        }

        // fourth query
        // Create Role Task Definitions
        int iMemID = 0;
        Hashtable Roles = new Hashtable();
        if ( sdr.NextResult() ) {
          while ( sdr.Read() ) {
            iMemID      = Convert.ToInt32( sdr[@"MemID"] );
            sMemName    = sdr[@"MemName"].ToString();
            AzTask      = AzApp.CreateTask( sMemName, null );
            AzApp.Submit( 0, null );
            AzTask.IsRoleDefinition = 1;
            AzTask.Submit( 0, null );

            AzRole = AzScope.CreateRole( sMemName, null );
            AzScope.Submit(0, null);
            AzRole.Description = sdr["MemDesc"].ToString();
            AzRole.Submit( 0, null );

            Roles.Add ( iMemID, AzRole );
            base.LogItWithTimeStamp( String.Format( "Role ID:{0} RoleName:{1}", iMemID, sMemName ) );
          }
        }

        // fifth query
        // Assoicate Role Tasks with Roles
        Trace.Clear();
        if ( sdr.NextResult() ) {
          while ( sdr.Read() ) {
            int MemParID = Convert.ToInt32( sdr[@"MemParID"] );
            string MemName = sdr[@"MemName"].ToString();
            IAzRole azRole = (IAzRole) Roles[MemParID];
            if ( !Trace.ContainsKey( MemName ) ) {
              azRole.AddTask( MemName, null );
              azRole.Submit( 0, null );
              Trace.Add( MemName, MemName );
            } else {
              Trace.Clear();
              azRole.AddTask( MemName, null );
              azRole.Submit( 0, null );
              Trace.Add( MemName, MemName );
            }
          }
        }

        // sixth query
        // create nt group and associate it with role
        Trace.Clear();
        if ( sdr.NextResult() ) {
          while ( sdr.Read() ) {
            object RoleID    = sdr["RoleID"];
            string GroupName = sdr["GroupName"].ToString();
            if ( Roles.ContainsKey( RoleID ) ) {
              IAzRole azRole = (IAzRole) Roles[RoleID];
              azRole.AddMemberName( GroupName, null );
              azRole.Submit(0, null);
            }
          }
        }
      } catch ( Exception e ) {
        Console.WriteLine( "break at task {0} - operation {1}", sTaskName, sMemName );
        throw e;
      } finally {
        sdr.Close();
      }
    }

    private string[] FindParentName( int ParentID )
    {
      string SqlQuery = String.Format( @"SELECT MemberName, MemberDescription FROM Members WHERE MemberID = {0}", ParentID.ToString() );
      SqlDataReader sdr = base.RunQuery( SqlQuery );

      string[] Info = { null, null };
      if ( sdr != null ) {
        while ( sdr.Read() ) {
          Info[0] = sdr[0].ToString();
          Info[1] = sdr[1].ToString();
        }
      }

      return Info;
    }
#endregion


#region IAction Members

    public override void Execute() {
      base.Execute();
      base.IsComplete = true;
    }

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
