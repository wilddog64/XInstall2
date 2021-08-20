using System;
using System.Collections;
using System.Xml;
using Microsoft.Win32;


using XInstall.Util.Log;

namespace XInstall.Core.Actions {
  /// <summary>
  /// RegKey - a class that manipulates the registry database on local/remote machine
  /// </summary>
  public class RegKey : ActionElement {

#region private property methods variables
    private Object _objSubKeyValue  = null;

    // property method variables
    private string _strMachineName  = String.Empty;
    private string _strSubKey       = String.Empty;
    private string _strSubkeyName   = String.Empty;
    private string _strKeyName      = String.Empty;
    private string _strOperName     = String.Empty;
    private string _strExitMessage  = String.Empty;

    private bool   _bDeleteTree     = false;
    private bool   _bIsCompleted    = false;

    // registry key operation variables
    private string[] _strActions = {
      "create",
      "delete"
    };

    enum REGKEY_ACTION {
      REGKEY_CREATE,
      REGKEY_DELETE,
      REGKEY_OPEN,
      REGKEY_GETVALUE,
    }

    private REGKEY_ACTION _enumAction = REGKEY_ACTION.REGKEY_CREATE;
    private RegistryKey  _rkHKEY      = null;
    private RegistryHive _rhHive      = RegistryHive.LocalMachine;


    // error handling variables
    private int    _iExitCode       = (int) REGKEY_OPERATION_ERROR.REGKEY_OPERATION_SUCCESS;
    private bool   _bAllowException = false;

    // error messages table
    private string[] _strMessages = {
      "exit code {0}, Operation {1} key {2} complete successfully",
      "exit code {0}, Machine {1} not found",
      "exit code {0}, User {1} does not have sufficient right to access this HKEY {2}",
      "exit code {0}, Subkey is not provided!",
      "exit code {0}, user {1} does not have create key permission to key {2}",
      "exit code {0}, unauthorized create key {1} by user {2}",
      "exit code {0}, unable to create subkey {1} under {2}, HKEY {3} is closed!",
      "exit code {0}, value {1} for key {2} is null",
      "exit code {0}, cannot write value {1} to key {2}, key {3} is set to readonly!",
      "unknown hive key {0}, open HKEY_LOCAL_MACHINE instead",
      "exit code {0}, unknown regkey action {1} specified!",
      "exit code {0}, unable to delete a key {1}, it has subtrees",
      "exit code {0}, user {1} has no privilege to delete this key {2}",
      "exit code {0}, invalid subkey {1} entered"
    };

    // common registry operation error code enumration
    enum REGKEY_OPERATION_ERROR {
      REGKEY_OPERATION_SUCCESS = 0,
      REGKEY_MACHINE_NOTFOUND,
      REGKEY_USER_NO_ACCESSRIGHT,
      REGKEY_NOSUBKEY_PROVIDED,
      REGKEY_CANNOT_CREATEKEY,
      REGKEY_UNAUTH_CREATEKEY,
      REGKEY_HKEY_CLOSED,
      REGKEY_NULLVALUE,
      REGKEY_READONLY_SUBKEY,
      REGKEY_UNKNOWN_HKEY,
      REGKEY_UNKNOWN_ACTION,
      REGKEY_KEY_HAS_SUBTREE,
      REGKEY_CANNOT_DELETEKEY,
      REGKEY_INVALID_SUBKEY
    }
#endregion

#region public constructor methods

    /// <summary>
    /// public RegKey() -
    ///     a constructor that does serveral
    ///     initialization work.
    /// </summary>
    [Action("regkey", Needed=true)]
    public RegKey( XmlNode ActionNode ) : base( ActionNode ) {
      base.OutToConsole = true;
      base.OutToFile    = true;
    }

#endregion

#region public property methods

    /// <summary>
    /// property Action -
    ///     set an action to be carried out
    ///     by RegKey class.  Valid actions
    ///     are create and delete.
    /// </summary>
    /// <remarks>
    ///     A valid action that will be carried out
    ///     are, create and delete.
    /// </remarks>
    /// <example>
    ///     object.Action = "create" or
    ///     object.Action = "delete" will have the RegKey
    ///     object to create or delete a given registry key.
    /// </example>
    [Action("action", Needed=true)]
    public string Action {
      set {
        switch ( value ) {
          case "create":
            this._enumAction = REGKEY_ACTION.REGKEY_CREATE;
            break;
          case "open":
            this._enumAction = REGKEY_ACTION.REGKEY_OPEN;
            break;
          case "delete":
            this._enumAction = REGKEY_ACTION.REGKEY_DELETE;
            break;
          default:
            this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_UNKNOWN_ACTION;
            break;
        }
      }
    }


    /// <summary>
    /// property MachineName -
    ///     get/set a machine that the RegKey will
    ///     work on.
    /// </summary>
    /// <remarks>
    ///     This property will make RegKey talk to
    ///     a remote machine if the assigned one is
    ///     a valid server.  If no value is provided,
    ///     then RegKey will talk to the local machine.
    /// </remarks>
    /// <example>
    ///     object.MachineName = "chengkai-01" will make
    ///     RegKey to operate on machine chengkai-01.
    /// </example>
    [Action("machinename", Needed=false, Default="local")]
    public string MachineName {
      get {
        return _strMachineName;
      }

      set {
        _strMachineName = value;
      }
    }


    /// <summary>
    /// property BaseHiveKey -
    ///     get/set a base Hive Key for a given machine
    /// </summary>
    [Action("basehkey", Needed=false, Default="localmachine")]
    public string BaseHiveKey {
      get {
        return this._rhHive.ToString();
      }

      set {
        this._rhHive = this.GetHKEY( value.ToString() );
      }
    }


    /// <summary>
    /// property Subkey -
    ///     get/set the subkey of a given base hive key
    ///     that the RegKey works on.
    /// </summary>
    [Action("subkey", Needed=false, Default="")]
    public string Subkey {
      get {
        return this._strSubKey;
      }

      set {
        this._strSubKey = value;
      }
    }


    /// <summary>
    /// property Path -
    ///     get/set the path to the key/value pair
    ///     in a give registry database that RegKey
    ///     works on
    /// </summary>
    [Action("path", Needed=false, Default="")]
    public string Path {
      get {
        return this._strSubkeyName;
      }

      set {
        this._strSubkeyName = value;
      }
    }


    /// <summary>
    /// property KeyName -
    ///     get/set the Key of registry to be
    ///     create for what RegKey works on
    /// </summary>
    [Action("keyname", Needed=false, Default="")]
    public string KeyName {
      get {
        return this._strKeyName;
      }

      set {
        this._strKeyName = value;
      }
    }

    /// <summary>
    /// property SubKeyValue -
    ///     get/set the value of a created
    ///     key
    /// </summary>
    [Action("value", Needed=false, Default="")]
    public string SubkeyValue {
      set
      {
        this._objSubKeyValue = value;
      }
    }

    /// <summary>
    /// property OutputFile -
    ///     get/set the output file for
    ///     RegKey to log the information
    /// </summary>
    [Action("outputfile", Needed=false, Default="auto")]
    public new string OutputFile {
      set {
        base.FileName = value;
      }
    }

    /// <summary>
    /// property ExitCode -
    ///     gets the exit code return from
    ///     the RegKey operation.
    /// </summary>
    [Action("exitcode", Needed=false)]
    public new int ExitCode {
      get {
        return this._iExitCode;
      }
    }


    /// <summary>
    /// property ExitMessage -
    ///     gets the message corresponding
    ///     to the exit code
    /// </summary>
    [Action("exitmessage", Needed=false)]
    public new string ExitMessage {
      get {
        this._strExitMessage =  this.GetExitMessage();
        return this._strExitMessage;
      }
    }

    /// <summary>
    /// property ToFile -
    ///     set the flag to have log message redirect to
    ///     file
    /// </summary>
    [Action("tofile", Needed=false, Default="true")]
    public string ToFile {
      set {
        base.OutToFile = bool.Parse( value.ToString() );
      }
    }

    /// <summary>
    /// property ToConsole -
    ///     set the flag to have log message write to
    ///     console.
    /// </summary>
    [Action("toconsole", Needed=false, Default="true")]
    public string ToConsole {
      set {
        base.OutToFile = bool.Parse( value.ToString() );
      }
    }

    /// <summary>
    /// property AllowGenerateException -
    ///     set the flag to indicate that
    ///     RegKey should generate an
    ///     exception automatically (for
    ///     testing purpose).
    /// </summary>
    [Action("generateexception", Needed=false, Default="false")]
    public new string AllowGenerateException {
      set {
        this._bAllowException =
          bool.Parse( value.ToString() );
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

    protected override object ObjectInstance {
      get {
        return this;
      }
    }

    protected override string ObjectName {
      get {
        return this.Name;
      }
    }

#endregion

#region private property methods


    /// <summary>
    /// private property OperationName -
    ///     get/set the operation that
    ///     will be carried out by RegKey
    /// </summary>
    private string OperationName {
      get {
        return this._strOperName;
      }

      set {
        this._strOperName = value;
      }
    }

    /// <summary>
    /// prviate property DeleteTree -
    ///     get/set the flag to indicate that
    ///     the key that regkey try to delete
    ///     is a tree
    /// </summary>
    private bool DeleteTree {
      get {
        return this._bDeleteTree;
      }

      set {
        this._bDeleteTree = value;
      }
    }

#endregion

#region public Methods

    protected override void ParseActionElement() {
      // if require to generate an exception
      // then throw an exception out.
      if ( this._bAllowException ) {
        throw new Exception("required exception to be generated");
      }

      // open the registry key to be work
      this.OpenRegKey();
      switch ( _enumAction ) {
        case REGKEY_ACTION.REGKEY_CREATE:
          RegistryKey rk = this.OpenSubKey();
          this.SetSubkeyValue( rk );
          break;
        case REGKEY_ACTION.REGKEY_DELETE:
          this.DeleteSubKey();
          break;
        case REGKEY_ACTION.REGKEY_OPEN:
          break;
      }

      // check the exit code and throw an exception
      // if necessary.
      if ( this._iExitCode == (int) REGKEY_OPERATION_ERROR.REGKEY_OPERATION_SUCCESS ) {
        this.OperationName =
          this._strActions[ (int) this._enumAction ];
        this.GetExitMessage();
      }
      else if ( this._iExitCode == (int) REGKEY_OPERATION_ERROR.REGKEY_MACHINE_NOTFOUND ) {
        this.OperationName =
          this._strActions[ (int) this._enumAction ];
        this.GetExitMessage();
        throw new Exception( this.OperationName );
      }
      else {
        throw new Exception (this.ExitMessage);
      }

      this._bIsCompleted = true;
    }

#endregion

#region private methods

    /// <summary>
    /// private void OpenRegKey() -
    ///     Open a given registry key on
    ///     the assigned machine. When certain
    ///     conditions meets, the following exceptions
    ///     will be thrown:
    ///
    ///     <ul>
    ///         <li>
    ///             ArugmentException - cannot find the machine
    ///             to open the registry database
    ///         </li>
    ///         <li>
    ///             SecurityException - user has no privilege
    ///             to open registry database on a given machine
    ///         </li>
    ///     </ul>
    ///
    /// </summary>
    private void OpenRegKey() {
      this._iExitCode    = (int)
        REGKEY_OPERATION_ERROR.REGKEY_OPERATION_SUCCESS;
      this.OperationName = "OpenRegKey";
      try {
        this._rkHKEY = RegistryKey.OpenRemoteBaseKey( this._rhHive, this.MachineName );
      }
      catch ( System.ArgumentException ) {

        this._iExitCode = ( int ) REGKEY_OPERATION_ERROR.REGKEY_MACHINE_NOTFOUND;
      }
      catch ( System.Security.SecurityException ) {
        this._iExitCode = ( int ) REGKEY_OPERATION_ERROR.REGKEY_USER_NO_ACCESSRIGHT;
      }
    }

    /// <summary>
    /// private void SetSubkeyValue (RegistryKey rk) -
    ///     Set the value of a given key. The following exceptions will be
    ///     thrown when certain conditions met:
    ///
    ///     <ul>
    ///         <li>
    ///             ArgumentNullException - no value is provided
    ///         </li>
    ///         <li>
    ///             UnauthorizedAccessException - unauthorized access
    ///             a given key.
    ///         </li>
    ///         <li>
    ///             ObjectDisposedException - a key is already closed.
    ///         </li>
    ///         <li>
    ///             SecurityException - user has no privilege to set
    ///             value for a given key.
    ///         </li>
    ///     </ul>
    /// </summary>
    /// <param name="rk">a type of RegistryKey that is being set the value</param>
    private void SetSubkeyValue( RegistryKey rk ) {
      try {
        // this._rkHKEY.SetValue( this.SubkeyName, this._objSubKeyValue );
        rk.SetValue( this.KeyName, this._objSubKeyValue );
      }
      catch ( System.ArgumentNullException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_NULLVALUE;
      }
      catch ( System.UnauthorizedAccessException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_READONLY_SUBKEY;
      }
      catch ( System.ObjectDisposedException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_HKEY_CLOSED;
      }
      catch ( System.Security.SecurityException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_USER_NO_ACCESSRIGHT;
      }
      finally {
        rk.Close();
        this._rkHKEY.Close();
      }
    }

    /// <summary>
    /// private RegistryKey CreateSubKey( RegistryKey rk ) -
    ///     Create/open a subkey rk. The following exceptions will be
    ///     thrown when failed:
    ///
    ///     <ul>
    ///         <li>
    ///             ArugmentNullException - no key is provided to be created
    ///         </li>
    ///         <li>
    ///             SecurityException - user has no privilege to create a
    ///             give key.
    ///         </li>
    ///         <li>
    ///             UnauthorizedAccessException - unauthorized access to
    ///             a given key.
    ///         </li>
    ///         <li>
    ///             ObjectDisposedException - a key being created is already
    ///             closed.
    ///         </li>
    ///     </ul>
    /// </summary>
    /// <param name="rk">a type of RegistryKey that is being created/opened</param>
    /// <returns>return subkey that is successfully created; otherwise, return null</returns>
    private RegistryKey CreateSubKey( RegistryKey rk ) {
      RegistryKey rkNewKey = null;

      try {
        rkNewKey = rk.CreateSubKey( this.Path );
      }
      catch ( System.ArgumentNullException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_NOSUBKEY_PROVIDED;
      }
      catch ( System.Security.SecurityException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_CANNOT_CREATEKEY;
      }
      catch ( System.UnauthorizedAccessException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_UNAUTH_CREATEKEY;
      }
      catch ( System.ObjectDisposedException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_HKEY_CLOSED;
      }
      return rkNewKey;
    }

    /// <summary>
    /// private RegistryKey() -
    ///     Opens a given registry key. The followng exceptions
    ///     will be generated when failed to operate:
    ///
    ///     <ul>
    ///         <li>
    ///             ArugmentNullException - no key provided to be opened.
    ///         </li>
    ///         <li>
    ///             ObjectDisposedException - a key is closed.
    ///         </li>
    ///         <li>
    ///             SecurityException - user does not have a privilige to
    ///             open a give key.
    ///         </li>
    ///     </ul>
    ///
    /// </summary>
    /// <returns>return the key that is successfully opened; otherwise, null</returns>
    private RegistryKey OpenSubKey () {
      RegistryKey rkThisKey = null;
      this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_OPERATION_SUCCESS;
      this.OperationName = "OpenSubKey";
      try {
        rkThisKey = this._rkHKEY.OpenSubKey( this.Subkey, true );
      }
      catch ( System.ArgumentNullException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_NOSUBKEY_PROVIDED;
      }
      catch ( System.ObjectDisposedException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_HKEY_CLOSED;
      }
      catch ( System.Security.SecurityException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_UNAUTH_CREATEKEY;
      }

      return this._enumAction == REGKEY_ACTION.REGKEY_CREATE ?  this.CreateSubKey( rkThisKey ) : rkThisKey;

    }


    /// <summary>
    /// private void DeleteSubKey() -
    ///     Delete a given registry key. The following exceptions
    ///     will be thrown when failed:
    ///
    ///         <ul>
    ///             <li>
    ///                 InvalidOperationException -
    ///                     if the key to be deleted is contains a
    ///                     tree. You can set DeleteTree to be true
    ///                     for deleting it.
    ///             </li>
    ///             <li>
    ///                 ArgumentNullException -
    ///                     no key is provided to be deleted.
    ///             </li>
    ///             <li>
    ///                 SecurityException -
    ///                     user does not have privilege to delete
    ///                     a given subkey.
    ///             </li>
    ///         </ul>
    ///
    /// </summary>
    private void DeleteSubKey() {
      RegistryKey rk     = this.OpenSubKey();
      this._iExitCode    = (int) REGKEY_OPERATION_ERROR.REGKEY_OPERATION_SUCCESS;
      this.OperationName = "DeleteSubKey";
      try {
        if ( this.DeleteTree ) {
          rk.DeleteSubKeyTree( this.Path );
        }
        else {
          rk.DeleteSubKey( this.Path );
        }
      }
      catch ( System.InvalidOperationException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_KEY_HAS_SUBTREE;
        rk.DeleteSubKeyTree( this.Path );
      }
      catch ( System.ArgumentNullException ) {
        this._iExitCode = (int) REGKEY_OPERATION_ERROR.REGKEY_NOSUBKEY_PROVIDED;
      }
      catch ( System.Security.SecurityException ) {
        this._iExitCode =
          (int) REGKEY_OPERATION_ERROR.REGKEY_CANNOT_DELETEKEY;
      }
    }


    /// <summary>
    /// private string GetExitMessage() -
    ///     maps a message to a corresponding exit code and
    ///     returns it.
    /// </summary>
    /// <returns>message corresponding to a given exit code</returns>
    private string GetExitMessage() {
      string strReturnMessage = null;

      switch ( (REGKEY_OPERATION_ERROR) this.ExitCode  ) {
        case REGKEY_OPERATION_ERROR.REGKEY_OPERATION_SUCCESS:
          strReturnMessage = String.Format(
              this._strMessages[ this.ExitCode ],
              this._iExitCode,
              this.OperationName,
              this.BaseHiveKey + @"\" + this.Subkey  + this.Path +  @"\" +
              this.KeyName,
              this.BaseHiveKey);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_MACHINE_NOTFOUND:
          strReturnMessage = String.Format(
              this._strMessages[ this.ExitCode ],
              this._iExitCode,
              this.MachineName);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_USER_NO_ACCESSRIGHT:
          strReturnMessage =
            String.Format( this._strMessages[ this.ExitCode ],
                this._iExitCode,
                Environment.UserDomainName,
                this.BaseHiveKey);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_NOSUBKEY_PROVIDED:
          strReturnMessage =
            String.Format(
                this._strMessages[ this.ExitCode ],
                this._iExitCode);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_CANNOT_CREATEKEY:
          strReturnMessage = String.Format(
              this._strMessages[ this._iExitCode ],
              this._iExitCode,
              Environment.UserDomainName,
              this.Subkey);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_UNAUTH_CREATEKEY:
          strReturnMessage =
            String.Format( this._strMessages[ this._iExitCode ],
                this._iExitCode,
                this.Subkey,
                Environment.UserDomainName);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_HKEY_CLOSED:
          strReturnMessage = String.Format(
              this._strMessages[ this._iExitCode ],
              this._iExitCode,
              this.Subkey,
              this.BaseHiveKey,
              this.BaseHiveKey);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_NULLVALUE:
          strReturnMessage = String.Format(
              this._strMessages[ this._iExitCode ],
              this._iExitCode,
              this._objSubKeyValue, this.Subkey);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_READONLY_SUBKEY:
          strReturnMessage = String.Format(
              this._strMessages[ this._iExitCode ],
              this._iExitCode,
              this._objSubKeyValue, this.Subkey, this.Subkey);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_UNKNOWN_HKEY:
          strReturnMessage = String.Format(
              this._strMessages[ this._iExitCode ],
              this._iExitCode,
              this.BaseHiveKey);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_KEY_HAS_SUBTREE:
          strReturnMessage = String.Format(
              this._strMessages[ this._iExitCode ],
              this._iExitCode,
              this.Subkey);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_CANNOT_DELETEKEY:
          strReturnMessage = String.Format(
              this._strMessages[ this._iExitCode ],
              this._iExitCode,
              Environment.UserDomainName, this.Subkey);
          break;
        case REGKEY_OPERATION_ERROR.REGKEY_INVALID_SUBKEY:
          strReturnMessage = String.Format(
              this._strMessages[ this._iExitCode ],
              this._iExitCode,
              this.Subkey);
          break;

      }
      base.LogItWithTimeStamp ( strReturnMessage );
      return strReturnMessage;
    }


    /// <summary>
    /// private RegistryHive GetHKEY( string strValue ) -
    ///     for a given string value returns its corresponding
    ///     HIVE KEY.
    /// </summary>
    /// <param name="strValue">name of HIVE KEY in string type</param>
    /// <returns>
    /// corresponding RegistryHive type value when success; othwise,
    /// default to RegistryHive.LocalMachine
    /// </returns>
    private RegistryHive GetHKEY ( String strValue ) {
      RegistryHive rh = RegistryHive.ClassesRoot;
      switch ( strValue ) {
        case "classesroot":
          rh = RegistryHive.ClassesRoot;
          break;

        case "currentuser":
          rh = RegistryHive.CurrentUser;
          break;

        case "localmachine":
          rh = RegistryHive.LocalMachine;
          break;

        case "currentconfig":
          rh = RegistryHive.CurrentConfig;
          break;

        case "users":
          rh = RegistryHive.Users;
          break;

        case "perfmondata":
          rh = RegistryHive.PerformanceData;
          break;

        default:
          rh = RegistryHive.LocalMachine;
          break;
      }
      return rh;
    }

#endregion

#region ICleanUp Members

    /// <summary>
    /// public void RemoveIt() -
    ///     this method belongs to ICleanUp interface
    ///     that provides the ability to remove what
    ///     it has done.
    /// </summary>
    public void RemoveIt() {
      this.Action = "delete";
      if ( this.Path.IndexOf( @"\" ) > 0 ) {
        this.Path = this.Path.Substring(0, this.Path.IndexOf(@"\"));
        this.DeleteTree = true;
      }
      this.Execute();
    }

    /// <summary>
    /// property IsComplete
    ///     get/set the state of Action
    /// </summary>
    public new bool IsComplete {
      get {
        return this._bIsCompleted;
      }
    }

    /// <summary>
    /// property Name -
    ///     gets the name of constructor
    /// </summary>
    public new string Name {
      get {
        return this.GetType().Name;
      }
    }
#endregion
  }
}
