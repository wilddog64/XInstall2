using System;
using System.DirectoryServices;
using System.Collections;
using System.Reflection;
using System.Xml;

namespace XInstall.Core.Actions
{
    public enum AppPoolIdentityType : int
    {
	    LOCALSYSTEM = 0,
	    LOCALSERVICE,
	    NETWORKSERVICE,
	    USERDEFINED,
    }


    public enum AppPoolActionType
    {
	    CREATE = 0,
	    DELETE = 1,
	    STOP   = 2,
	    START  = 3,
    }


    /// <summary>
    /// Summary description for IIsAppPool.
    /// </summary>
    public class IIsAppPool : AdsiBase
    {

	    // enumeration type
	    private AppPoolIdentityType _AppPoolIdentity = AppPoolIdentityType.NETWORKSERVICE;
	    private AppPoolActionType _ActionType        = AppPoolActionType.CREATE;

	    // reference types
	    private XmlNode _ActionNode             = null;
	    private Hashtable _AppPoolProperties   = new Hashtable();

	    // value types
	    private string _AppPoolName             = String.Empty;
	    private string _MachineName             = string.Empty;
	    private string _WAMUserName             = string.Empty;
	    private string _WAMUserPass             = string.Empty;
	    private string _AppPoolIdentityType     = string.Empty;
	    private string _AppPoolState            = string.Empty;
	    private string _AppAutoStart            = string.Empty;
	    private string _Action                  = string.Empty;
	    private string _SchemaClassName         = @"IIsApplicationPools";
	    private string _AppPoolRecycleRequests  = string.Empty;
	    private string _PeriodicRestartRequests = string.Empty;

	    private readonly string _AdsiProvider        = @"IIS";
	    private readonly string _AdsiPath            = @"W3SVC/AppPools";
	    private readonly string _IIsApplicationPools = @"IIsApplicationPools";
	    private readonly string _IIsApplicationPool  = @"IIsApplicationPool";


	    [Action("applicationpool")]
	    public IIsAppPool( XmlNode xn ) : base( xn )
	    {
		    this._ActionNode = xn;
	    }


	    #region protected properties

	    protected override string AdsiProvider
	    {
		    get
		    {
			    return this._AdsiProvider;
		    }
	    }


	    protected override object ObjectInstance
	    {
		    get
		    {
			    return this;
		    }
	    }


	    protected override string SchemaClassName
	    {
		    get
		    {
			    return this._SchemaClassName;
		    }
		    set
		    {
			    this._SchemaClassName = value;
		    }
	    }


	    #endregion

	    #region public properties

	    public override string AdsiPath
	    {
		    get
		    {
			    return this._AdsiPath;
		    }
	    }


	    [Action("machinename", Needed=false, Default=".")]
	    public override string MachineName
	    {
		    get
		    {
			    return this._MachineName;
		    }
		    set
		    {
			    this._MachineName = value;
		    }
	    }


	    [Action("apppoolname", Needed=false, Default="")]
	    [ADSI("AppPoolName")]
	    public string AppPoolName
	    {
		    get
		    {
			    return this._AppPoolName;
		    }
		    set
		    {
			    this._AppPoolName = value;
		    }
	    }


	    [Action("username", Needed=false, Default="")]
	    [ADSI("WAMUserName")]
	    public string AppPoolUserName
	    {
		    get
		    {
			    return this._WAMUserName;
		    }
		    set
		    {
			    this._WAMUserName = value;
		    }
	    }


	    [Action("password", Needed=false, Default="")]
	    [ADSI("WAMUserPass")]
	    public string AppPoolUserPass
	    {
		    get
		    {
			    return this._WAMUserPass;
		    }
		    set
		    {
			    this._WAMUserPass = value;
		    }
	    }


	    [Action("apppoolidentity", Needed=false, Default="NETWORKSERVICE")]
	    [ADSI("AppPoolIdentityType")]
	    public string AppPoolIdentity
	    {
		    get
		    {
			    return this._AppPoolIdentity.ToString();
		    }
		    set
		    {
			    switch ( value.ToUpper() )
			    {
			    case "LOCALSYSTEM":
				    this._AppPoolIdentityType = AppPoolIdentityType.LOCALSYSTEM.ToString();
				    break;
			    case "LOCALSERVICE":
				    this._AppPoolIdentityType = AppPoolIdentityType.LOCALSERVICE.ToString();
				    break;
			    case "NETWORKSERVICE":
				    this._AppPoolIdentityType = AppPoolIdentityType.NETWORKSERVICE.ToString();
				    break;
			    case "USERDEFINED":
				    this._AppPoolIdentityType = AppPoolIdentityType.USERDEFINED.ToString();
				    break;
			    default:
				    throw new ArgumentException(
					String.Format( "{0}: unknown parameter {1}", this.Name, value ),
					"AppPoolIdentity" );
			    }
		    }
	    }


	    [Action("recyclerequest", Needed=false, Default="false")]
	    [ADSI("AppPoolRecycleRequest")]
	    public string AppPoolRecycleRequests
	    {
		    get
		    {
			    return this._AppPoolRecycleRequests;
		    }
		    set
		    {
			    this._AppPoolRecycleRequests = value;
		    }
	    }


	    [Action("RestartRequest", Needed=false, Default="2000")]
	    [ADSI("PeriodicRestartRequests")]
	    public string PeriodicRestartRequests
	    {
		    get
		    {
			    return this._PeriodicRestartRequests;
		    }
		    set
		    {
			    this._PeriodicRestartRequests = value;
		    }
	    }


	    public new string Name
	    {
		    get
		    {
			    return this.GetType().Name;
		    }
	    }


	    public override string ObjectName
	    {
		    get
		    {
			    return this.Name;
		    }
	    }


	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable
	    {
		    set
		    {
			    base.Runnable = bool.Parse( value );
		    }
	    }


	    [Action("action", Needed=false, Default="create")]
	    public string Action
	    {
		    get
		    {
			    return this._Action;
		    }
		    set
		    {
			    this._Action = value;
		    }
	    }

	    #endregion

	    #region private properties

	    private string IIsApplicationPools
	    {
		    get
		    {
			    return this._IIsApplicationPools;
		    }
	    }


	    private string IIsApplicationPool
	    {
		    get
		    {
			    return this._IIsApplicationPool;
		    }
	    }


	    #endregion

	    #region public methods

	    public override void Execute()
	    {
		    base.Execute();
	    }


	    public override void ParseActionElement()
	    {
		    base.ParseActionElement();

		    this.FillADSIProperties( this._ActionNode );
		    this.ReadXmlProerties( this._ActionNode );
		    switch ( this.GetActionType( this.Action ) )
		    {
		    case AppPoolActionType.CREATE:
			    this.CreateAppPool();
			    break;
		    case AppPoolActionType.DELETE:
			    break;
		    case AppPoolActionType.START:
			    break;
		    case AppPoolActionType.STOP:
			    break;
		    }
	    }


	    #endregion

	    #region private methods


	    private void ReadXmlProerties( XmlNode ActionNode )
	    {
		    if ( ActionNode.HasChildNodes )
		    {
			    foreach ( XmlNode xn in ActionNode.ChildNodes )
			    {
				    string PropertyName  = xn.Name;
				    string PropertyValue = xn.InnerText;
				    this.AppPoolProperties.Add( PropertyName, PropertyValue );
			    }
		    }
	    }


	    private void FillADSIProperties( XmlNode xn )
	    {
		    Type MyType = this.GetType();

		    PropertyInfo[] PropertyInfos = MyType.GetProperties();

		    foreach ( PropertyInfo pi in PropertyInfos )
		    {
			    object[] AdsiAttributes = pi.GetCustomAttributes( typeof (ADSIAttribute), false );

			    if ( AdsiAttributes.Length != 0 )
			    {
				    string PropertyName = ( AdsiAttributes[0] as ADSIAttribute ).Name;
				    XmlNode PropertyNameNode = xn.SelectSingleNode( PropertyName );
				    if ( PropertyNameNode != null )
				    {

					    object Value = (string) PropertyNameNode.InnerText;
					    object[] Params = new object[1] { Value };
					    pi.GetSetMethod().Invoke( this, Params  );

				    }
			    }
		    }
	    }


	    private AppPoolActionType GetActionType( string Action )
	    {
		    AppPoolActionType ActionType = AppPoolActionType.CREATE;

		    switch ( Action.ToLower() )
		    {
		    case "create":
			    ActionType = AppPoolActionType.CREATE;
			    break;
		    case "delete":
			    ActionType = AppPoolActionType.DELETE;
			    break;
		    case "start":
			    ActionType = AppPoolActionType.START;
			    break;
		    case "stop":
			    ActionType = AppPoolActionType.STOP;
			    break;
		    default:
			    base.FatalErrorMessage(
				this.MachineName,
				String.Format( "{0}: {1} - unknown action type {2}",
					       this.Name, "GetActionType", Action ), 1660 );
			    break;
		    }
		    return ActionType;
	    }


	    private DirectoryEntry CreateAppPool()
	    {

		    if ( this.AppPoolName.Length == 0 )
		    {
			    if ( this.AppPoolProperties.ContainsKey( "AppPoolName".ToLower() ) )
			    {
				    this.AppPoolName = (string) this.AppPoolProperties[ "AppPoolName" ];
			    }
			    else
			    {
				    this.AppPoolName = this.AppPoolName;
			    }
		    }

		    DirectoryEntry AppPool = base.AddNewEntry( this.AppPoolName, this.IIsApplicationPool );

		    return AppPool;
	    }


	    #endregion

	    #region private properties

	    private Hashtable AppPoolProperties
	    {
		    get
		    {
			    return this._AppPoolProperties;
		    }
	    }



	    #endregion

	    #region static methods

	    public static IIsAppPool Create( XmlNode xn, string MachineName )
	    {

		    IIsAppPool AppPool  = new IIsAppPool( xn );
		    AppPool.MachineName = MachineName;

		    AppPool.Execute();
		    return AppPool;
	    }


	    #endregion
    }
}
