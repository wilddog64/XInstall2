using System;
using System.IO;
using System.DirectoryServices;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Xml;


namespace XInstall.Core.Actions
{
    public enum IIsAppMode
    {
	    IN_PROCESS = 0,
	    OUT_PROCESS,
	    POOLED_PROCESS,
    }

    public enum IISVDirAccessFlag :
    int
    {
	    MD_ACCESS_EXECUTE           = 4,
	    MD_ACCESS_NO_PHYSICALDIR    = 32768,
	    MD_ACCESS_NO_REMOTE_EXEC    = 8192,
	    MD_ACCESS_NO_REMOTE_READ    = 4096,
	    MD_ACCESS_NO_REMOTE_SCRIPT  = 16384,
	    MD_ACCESS_NO_REMOTE_WRITE   = 1024,
	    MD_ACCESS_READ              = 1,
	    MD_ACCESS_SCRIPT            = 512,
	    MD_ACCESS_SOURCE            = 16,
	    MD_ACCESS_WRITE             = 2,
    }


    /// <summary>
    /// IIsWebSite is class that inherits from AdsiBase class,
    /// which provides methods and properties to create/manipulate
    /// an IIS6 webiste.  The class uses DirectoryServices from
    /// AdsiBase class.
    /// </summary>
    public class IIsWebSite : AdsiBase
    {

	    #region private member fields
	    private XmlNode        _ActionNode  = null;
	    private Random         _Random      = new Random();
	    private DirectoryEntry _WebSite     = null;
	    // private IIsAppPool     _AppPool     = null;

	    private string  _MachineName       = string.Empty;
	    private string  _WebSiteName       = string.Empty;
	    private string  _WebSiteHeader     = string.Empty;
	    private string  _Path              = string.Empty;
	    private string  _Port              = string.Empty;
	    private string  _SchemaClassName   = string.Empty;
	    private string  _AuthProviders     = string.Empty;
	    private string  _AuthFlags         = string.Empty;
	    private string  _ServerBindings    = string.Empty;
	    private string  _ServerAutoStart   = string.Empty;
	    private string  _DefaultDoc        = string.Empty;
	    private string  _FrontPageWeb      = string.Empty;
	    private string  _DefaultAppPool    = string.Empty;
	    private string  _Action            = string.Empty;
	    private string  _CVSFile           = string.Empty;
	    private bool    _AssignDefaultPool = true;


	    private int     _SiteID            = -99;
	    #endregion

	    /// <summary>
	    /// A public constructor to initialized IIsWebSite object.
	    /// It accepts only one parameter, ActionNode, which is an
	    /// sub-node from an xml configuration file.
	    /// </summary>
	    /// <param name="ActionNode">an XmlNode type variable</param>
	    [Action("iiswebsite")]
	    public IIsWebSite( XmlNode ActionNode ) : base( ActionNode )
	    {
		    this._ActionNode = ActionNode;
	    }


	    public IIsWebSite() : base() {}
	    #region public properties

	    /// <summary>
	    /// get/set the remote/local machine name
	    /// </summary>
	    /// <remarks>
	    /// This is used by the object to determine where
	    /// a given website should be created on.  By default,
	    /// the website will be created on the localhost.
	    /// </remarks>
	    [Action("machinename", Needed=false, Default="localhost")]
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


	    /// <summary>
	    /// get/set a flag to determine if a default application pool
	    /// should be assigned to a newly created WebSite.
	    /// </summary>
	    /// <remarks>
	    /// The default application pool will be assigned to a newly
	    /// created website.  To assgin different or custom application
	    /// pool, set this property to "false", and work thereafter.
	    /// </remarks>
	    [Action("assigndefaultapppool", Needed=false, Default="true")]
	    public string AssignDefaultAppPool
	    {
		    get
		    {
			    return this._DefaultAppPool;
		    }
		    set
		    {
			    this._DefaultAppPool    = value.ToLower();
			    this._AssignDefaultPool = bool.Parse( this._DefaultAppPool );
		    }
	    }


	    /// <summary>
	    /// get/set what action needs to be done
	    /// </summary>
	    /// <remarks>
	    /// This property determines what action need to be taken
	    /// by IIsWebSite object.  Currently, it supports the following
	    /// verbs:
	    ///     create - create a website
	    ///     delete - delete a website
	    ///     stop   - stop a gvien website
	    ///     update - update properties for a given site
	    /// </remarks>
	    [Action("action", Needed=false, Default="create")]
	    public string Action
	    {
		    get
		    {
			    return this._Action.ToLower();
		    }
		    set
		    {
			    this._Action = value;
		    }
	    }

	    /// <summary>
	    /// get/set a Website name.
	    /// </summary>
	    /// <remarks>
	    /// This is actually a property in IIS metabase that
	    /// used to describe a given website.
	    /// </remarks>
	    [Action("websitename", Needed=false, Default="DefaultWebSite")]
	    [ADSI("ServerComment")]
	    public string ServerComment
	    {
		    get
		    {
			    return this._WebSiteName;
		    }
		    set
		    {
			    this._WebSiteName = value;
		    }
	    }


	    /// <summary>
	    /// get/set the permission for a given website
	    /// </summary>
	    /// <remarks>
	    /// Internally, is it a bitmask field inside ADSI
	    /// programming interface.  The valid values are
	    /// here:
	    ///     Friendly Name       Bismask
	    ///     AuthAnonymous           1
	    ///     AuthBasic               2
	    ///     AuthMD5                16
	    ///     AuthPassport           64
	    ///
	    /// </remarks>
	    [Action("authmethods", Needed=false, Default="4")]
	    [ADSI("AuthFlags")]
	    public string AuthFlags
	    {
		    get
		    {
			    return this._AuthFlags;
		    }
		    set
		    {
			    this._AuthFlags = value;
		    }
	    }


	    /// <summary>
	    /// get/set the ServerBindings property in IIS ADSI directory
	    /// object.
	    /// </summary>
	    /// <remarks>
	    /// This property specifies a string that IIS uses to
	    /// determine which network endpoints are used by
	    /// a website instance.  The string format is ip:port:hostname
	    /// </remarks>
	    /// <example>
	    ///     object.ServerBindings = ":80:"
	    ///     will bind port 80 to a given website.
	    /// </example>
	    [Action("serverbindings", Needed=false, Default=":85:")]
	    [ADSI("ServerBindings")]
	    public string ServerBindings
	    {
		    get
		    {
			    return this._ServerBindings;
		    }
		    set
		    {
			    this._ServerBindings = value;
		    }
	    }



	    /// <summary>
	    /// get/set authentication providers that can be used by a
	    /// given website when Integrated Windows Authentication is
	    /// enabled.
	    /// </summary>
	    /// <remarks>
	    /// If need to setup more than one providers, use comma-delimited to
	    /// seperate them.
	    /// </remarks>
	    [Action("ntauthenticationproviders", Needed=false, Default="NTLM")]
	    [ADSI("NTAuthenticationProviders")]
	    public string NTAuthenticationProviders
	    {
		    get
		    {
			    return this._AuthProviders;
		    }
		    set
		    {
			    this._AuthProviders = value;
		    }
	    }

	    /// <summary>
	    /// get/set a value to indicate if front page extension will be used
	    /// </summary>
	    /// <remarks>
	    /// Setting one to this property will cause FrontPage Manager to
	    /// create files required for FrontPage Server Extensions; setting 0
	    /// will deleted those files.
	    /// </remarks>
	    [Action("frontpageweb", Needed=false, Default="1")]
	    [ADSI("FrontPageWeb")]
	    public string FrontPageWeb
	    {
		    get
		    {
			    return this._FrontPageWeb;
		    }
		    set
		    {
			    this._FrontPageWeb = value;
		    }
	    }


	    /// <summary>
	    /// get/set a list of default documents that will returns to client
	    /// if no file name is included in the client's request.
	    /// </summary>
	    /// <remarks>
	    /// The default document will be returned to client if
	    /// EnableDefaultDoc is set to true for a given directory.
	    /// </remarks>
	    [Action("defaultdocument", Needed=false, Default="Default.aspx, Default.htm")]
	    [ADSI("DefaultDoc")]
	    public string DefaultDoc
	    {
		    get
		    {
			    return this._DefaultDoc;
		    }
		    set
		    {
			    this._DefaultDoc = value;
		    }
	    }



	    /// <summary>
	    /// set a value to determine if this object should run or not
	    /// </summary>
	    /// <remarks>
	    /// This is a write only property, when set to false the object
	    /// will not be executed.
	    /// </remarks>
	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable
	    {
		    set
		    {
			    base.Runnable = bool.Parse( value );
		    }
	    }


	    /// <summary>
	    /// get/set a flag to indicate if this object should generate an
	    /// excpetion or not.
	    /// </summary>
	    /// <remarks>
	    /// The purpose of the property is sole used for testing only.  Do
	    /// not set it to true when running in a production environment.
	    /// </remarks>
	    [Action("AllowGenerateException", Needed=false, Default="false")]
	    public new string AllowGenerateException
	    {
		    set
		    {
			    base.AllowGenerateException = bool.Parse( value );
		    }
	    }

	    [Action("cvsfile", Needed=false, Default="")]
	    public string CVSFile
	    {
		    get
		    {
			    return this._CVSFile;
		    }
		    set
		    {
			    this._CVSFile = value;
		    }
	    }

	    /// <summary>
	    ///  get the name of a running object instance.
	    /// </summary>
	    public new string Name
	    {
		    get
		    {
			    return this.GetType().Name;
		    }
	    }
	    #endregion


	    /// <summary>
	    ///
	    /// </summary>
	    #region public override properties
	    protected override string ObjectName
	    {
		    get
		    {
			    return this.Name;
		    }
	    }
	    #endregion

	    #region protected overrided properties
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


	    protected override string AdsiProvider
	    {
		    get
		    {
			    return "IIS";
		    }
	    }


	    protected override object ObjectInstance
	    {
		    get
		    {
			    return this;
		    }
	    }


	    public override string AdsiPath
	    {
		    get
		    {
			    return "W3SVC";
		    }
	    }

	    public DirectoryEntries WebSites
	    {
		    get
		    {
			    return base.Directories;
		    }
	    }



	    public override string Port
	    {
		    get
		    {
			    return this._Port;
		    }
		    set
		    {
			    this._Port = value;
		    }
	    }

	    #endregion

	    #region protected override methods

	    protected override void ParseActionElement()
	    {
		    base.ParseActionElement ();
		    string Method = "unknown";
		    try
		    {
			    switch ( this.Action )
			    {
			    case "create":
				    Method       = String.Format( "CreateWebSite( {0}, {1} )", "ServerComment", "_ActionNode" );
				    this.WebSite = this.CreateWebSite( this.ServerComment, this._ActionNode );
				    break;

			    case "stop":
				    break;

			    case "delete":
				    Method = String.Format( "DeleteWebSite( {0} )", "ServerComment" );
				    this.DeleteWebSite( this.ServerComment );
				    break;

			    case "update":
				    Method = String.Format( "UpdateWebSiteProperties( {0}, {1} )", "SiteID", "_ActionNode" );
				    this.UpdateWebSiteProperties( this.SiteID.ToString(), this._ActionNode );
				    break;

			    case "replace":
				    Method = "replace";
				    this.DeleteWebSite( this.ServerComment );
				    IIsWebSite.DeleteAppPool( this._ActionNode, this.MachineName );
				    this.WebSite = this.CreateWebSite( this.ServerComment, this._ActionNode );
				    break;

			    case "listwebsites":
				    this.PrintWebsites();
				    break;

			    case "writecvs":
				    this.PrintWebsites( this.CVSFile );
				    break;

			    default:
				    base.FatalErrorMessage( ".", String.Format( "{0}: unrecognized action verb {1}", this.Name, this.Action ), 1660 );
				    break;
			    }
		    }
		    catch ( System.ComponentModel.Win32Exception w32e )
		    {
			    this.SetExitMessage( "AdsiPath {0}, Action {1}, Method {2} - Win32 error: {3}, Win32 error code {4} - website deleted",
						 base.AdsiPathInfo, this.Action, Method, w32e.Message, w32e.ErrorCode );
			    this.RemoveWebSite( this.ServerComment, this.MachineName, this._ActionNode );
			    throw new Exception( this.ExitMessage );
		    }
		    catch ( System.Runtime.InteropServices.COMException ee )
		    {
			    this.SetExitMessage( "AdsiPath {0}, Action {1}, Method {2} - com object error: {3}, error code {4} - website deleted",
						 base.AdsiPathInfo, this.Action, Method, ee.Message, ee.ErrorCode );
			    this.RemoveWebSite( this.ServerComment, this.MachineName, this._ActionNode );
			    throw new Exception( this.ExitMessage );
		    }
		    catch ( Exception ex )
		    {
			    this.SetExitMessage( "AdsiPath {0}, Action {1}, Method {2} - general error: {3}, website deleted !!",
						 base.AdsiPathInfo, this.Action, Method, ex.Message );
			    this.RemoveWebSite( this.ServerComment, this.MachineName, this._ActionNode );
			    throw new Exception( this.ExitMessage );

		    }
	    }


	    #endregion

	    #region private methods

	    private void RemoveWebSite( string WebSiteName, string MachineName, XmlNode ActionNode )
	    {
		    this.DeleteWebSite( WebSiteName );
		    IIsWebSite.DeleteAppPool( ActionNode, MachineName );
		    base.LogItWithTimeStamp(
			string.Format( "{0}: {1} website {2} on {3} removed",
				       this.Name, "RemoveWebSite", WebSiteName, MachineName ) );
	    }

	    private DirectoryEntry CreateWebSite( string WebSiteName, XmlNode xn )
	    {
		    this.SchemaClassName            = "IIsWebServer";
		    Hashtable WebSiteProperties     = new Hashtable();
		    Hashtable WebSiteVDirProperties = new Hashtable();

		    this.SiteID               = this._Random.Next( 65536 );
		    DirectoryEntry NewWebSite =
			(DirectoryEntry) base.InvokeAdsiObjectMethod(
			    "Create", this.SchemaClassName, SiteID );

		    base.LogItWithTimeStamp(
			String.Format( "{0}: {1} website {2} on {3} was created",
				       this.Name, "CreateWebSite", WebSiteName, this.MachineName ) );

		    this.BindingAppVirtualDirectroy( NewWebSite, this.SiteID, xn );

		    WebSiteProperties = this.GetWebSiteProperties();
		    base.SetAdsiObjectProperty( NewWebSite, WebSiteProperties );

		    NewWebSite.CommitChanges();

		    return NewWebSite;
	    }


	    private void UpdateWebSiteProperties( string WebSiteName, XmlNode xn )
	    {
		    this.SchemaClassName = "IIsWebServer";
		    if ( this.WebSite == null )
		    {
			    this.WebSite = this.FindWebSite( WebSiteName );
		    }

		    Hashtable WebSiteProperties = this.GetWebSiteProperties();
		    base.SetAdsiObjectProperty( WebSite, WebSiteProperties );
		    this.BindingAppVirtualDirectroy( WebSite, this.SiteID, xn, false );
	    }


	    private void DeleteWebSite( string WebSiteName )
	    {
		    this.SchemaClassName = "IIsWebServer";

		    if ( this.WebSite == null )
		    {
			    this.WebSite = this.FindWebSite( WebSiteName );
		    }

		    if ( this.WebSite == null )
		    {
			    return;
		    }

		    base.DeleteAdsiDirectoryObject( this.SiteID.ToString() );
		    base.LogItWithTimeStamp(
			String.Format( "{0}: Site {1} deleted", this.Name, WebSiteName ) );
	    }


	    private void BindingAppVirtualDirectroy( DirectoryEntry NewSite, int SiteID, XmlNode xn )
	    {
		    this.BindingAppVirtualDirectroy( NewSite, SiteID, xn, true );
	    }


	    private void BindingAppVirtualDirectroy( DirectoryEntry NewSite, int SiteID, XmlNode xn, bool Create )
	    {
		    this.SchemaClassName = "IISWebVirtualDir";

		    DirectoryEntry SiteAppVirDir = null;
		    if ( Create )
		    {
			    SiteAppVirDir = NewSite.Children.Add( "Root", this.SchemaClassName );
		    }
		    else
		    {
			    SiteAppVirDir = NewSite.Children.Find( "Root", this.SchemaClassName );
		    }

		    //            Hashtable AppPoolProperties =
		    //                IIsWebSite.GetAppPoolProperties( xn.SelectSingleNode( "ApplicationPool" ) );
		    //            string AppPoolName = xn.SelectSingleNode( "ApplicationPool/AppPoolName" ).InnerText;
		    //
		    //            if ( !this._AssignDefaultPool && AppPoolName.Length > 0 )
		    //            {
		    //                DirectoryEntry AppPool =
		    //                    IIsWebSite.CreateAppPool( SiteAppVirDir,
		    //                                              AppPoolName,
		    //                                              IIsAppMode.IN_PROCESS,
		    //                                              this.MachineName,
		    //                                              false );
		    //                IIsWebSite.SetAppPoolProperties( AppPool, AppPoolProperties );
		    //            }

		    //            if ( SiteAppVirDir != null && AppPoolName.Length > 0 )
		    //            {
		    Hashtable VDirProperties = this.GetVDirProertyValues( xn );
		    VDirProperties.Add( "AppRoot", String.Format( "LM/W3SVC/{0}/Root", SiteID ) );
		    base.SetAdsiObjectProperty( SiteAppVirDir, VDirProperties );
		    //            }

		    //            if ( this._AssignDefaultPool )
		    //                IIsWebSite.BindingDefaultAppPool( SiteAppVirDir, this.MachineName );
		    //            else
		    //            {
		    //                if ( AppPoolName.Length > 0 )
		    //                   IIsWebSite.CreateAppPool( SiteAppVirDir,
		    //                                             AppPoolName,
		    //                                             IIsAppMode.IN_PROCESS,
		    //                                             this.MachineName,
		    //                                             true );
		    //            }
	    }


	    private Hashtable GetWebSiteProperties()
	    {
		    Hashtable PropertyCollection = new Hashtable();
		    Type      ThisType           = this.GetType();
		    PropertyInfo[] PropertyInfo = ThisType.GetProperties();

		    for ( int i = 0; i < PropertyInfo.Length; i++ )
		    {
			    PropertyInfo pi         = PropertyInfo[i];
			    object[] ADSIAttributes = pi.GetCustomAttributes( typeof( ADSIAttribute ), false);
			    if (ADSIAttributes.Length != 0 )
			    {
				    string PropertyName  = ( ADSIAttributes[0] as ADSIAttribute ).Name;
				    object PropertyValue = pi.GetGetMethod().Invoke( this, null );
				    PropertyCollection.Add( PropertyName, PropertyValue );
			    }
		    }

		    return PropertyCollection;
	    }


	    private DirectoryEntry FindWebSite( string WebSiteName )
	    {
		    DirectoryEntries Entries = this.Directories;
		    DirectoryEntry FoundEntry = null;
		    if ( Entries != null )
		    {
			    foreach ( DirectoryEntry Entry in Entries )
			    {
				    if ( Entry.SchemaClassName == this.SchemaClassName )
					    if ( Entry.Properties[ "ServerComment" ].Value.Equals( WebSiteName ) )
					    {
						    FoundEntry = Entry;
						    break;
					    }
			    }

			    if ( FoundEntry != null )
			    {
				    this.SiteID = Convert.ToInt32(FoundEntry.Name);
			    }
		    }

		    return FoundEntry;
	    }


	    private Hashtable GetVDirProertyValues( XmlNode xn )
	    {
		    Hashtable PropertyCollection = null;

		    if ( xn.HasChildNodes )
		    {
			    PropertyCollection = new Hashtable( xn.ChildNodes.Count );
			    XmlNodeList XmlChildNodes = xn.ChildNodes;
			    foreach ( XmlNode XmlChildNode in XmlChildNodes )
			    {
				    string NodeName = XmlChildNode.Name;
				    switch ( NodeName.ToLower() )
				    {
				    case "path":
					    string RemotePath =
						String.Format( @"\\{0}\{1}",
							       this.MachineName, XmlChildNode.InnerText.Replace( ':', '$' ) );
					    System.IO.Directory.CreateDirectory( RemotePath );
					    break;
				    case "applicationpool":
					    continue;
				    case "iisvdir":
					    continue;
				    }
				    if ( !PropertyCollection.ContainsKey( NodeName ) )
				    {
					    PropertyCollection.Add( NodeName, XmlChildNode.InnerText );
				    }
			    }
		    }

		    return PropertyCollection;
	    }


	    #endregion

	    #region public static methods - deal with ApplicationPool
	    public static DirectoryEntry CreateAppPool( DirectoryEntry NewSite,
		    string         AppPoolName,
		    IIsAppMode     AppMode,
		    string         MachineName,
		    bool           Binding )
	    {
		    String AdsiPath = String.Format( "IIS://{0}/W3SVC/AppPools", MachineName );

		    DirectoryEntry AppPools = null;
		    DirectoryEntry AppPool  = null;

		    if ( !Binding )
		    {
			    try
			    {
				    AppPools = new DirectoryEntry( AdsiPath );
				    AppPool  = AppPools.Children.Add(AppPoolName, "IIsApplicationPool" );
				    AppPools.CommitChanges();
			    }
			    finally
			    {
				    AppPools.Close();
			    }
		    }
		    else
			    NewSite.Invoke( "AppCreate3", new object[]
		    {
			    AppMode, AppPoolName, true
		    }
				  );

		    return AppPool;
	    }


	    public static void DeleteAppPool( XmlNode AppPoolNode, string MachineName )
	    {
		    string AppPoolName = AppPoolNode.SelectSingleNode( "ApplicationPool/AppPoolName" ).InnerText;
		    String AdsiPath = String.Format( "IIS://{0}/W3SVC/AppPools", MachineName );

		    DirectoryEntry AppPools = null;
		    DirectoryEntry AppPool  = null;

		    if ( AppPoolName.Length > 0 )
		    {
			    try
			    {
				    AppPools = new DirectoryEntry( AdsiPath );
				    AppPool  = AppPools.Children.Find( AppPoolName, "IIsApplicationPool" );

				    if ( AppPool != null )
				    {
					    AppPools.Children.Remove( AppPool );
					    AppPools.CommitChanges();
				    }
			    }
			    catch (  Exception e )
			    {
				    if ( e.Message == "System cannot find the specified path" )
				    {
					    return;
				    }
			    }
			    finally
			    {
				    AppPools.Close();
			    }

		    }
	    }


	    public static void UpdateAppPool( XmlNode AppPoolNode, string MachineName )
	    {
		    String AdsiPath = String.Format( "IIS://{0}/W3SVC/AppPools", MachineName );
		    string AppPoolName = AppPoolNode.SelectSingleNode( "ApplicationPool/AppPoolName" ).InnerText;

		    DirectoryEntry AppPools = null;
		    DirectoryEntry AppPool  = null;

		    if ( AppPoolName.Length > 0 )
		    {

			    try
			    {
				    AppPools = new DirectoryEntry( AdsiPath );
				    AppPool = AppPools.Children.Find( AppPoolName, "IIsApplicationPool" );

				    if ( AppPool != null )
				    {
					    Hashtable AppPoolProperties = IIsWebSite.GetAppPoolProperties( AppPoolNode );
					    IIsWebSite.SetAppPoolProperties( AppPool, AppPoolProperties );
				    }
			    }
			    finally
			    {
				    AppPools.Close();
			    }
		    }

	    }


	    public static void BindingDefaultAppPool( DirectoryEntry Entry, string MachineName )
	    {
		    IIsWebSite.CreateAppPool( Entry,
					      "DefaultAppPool",
					      IIsAppMode.IN_PROCESS,
					      MachineName,
					      false );
	    }


	    public static Hashtable GetAppPoolProperties( XmlNode AppPoolInfo )
	    {
		    Hashtable AppPoolProperties = new Hashtable();

		    if ( AppPoolInfo.HasChildNodes )
		    {
			    Hashtable TempProperties = new Hashtable();
			    string AppPoolName       = String.Empty;
			    XmlNodeList AppPoolNodes = AppPoolInfo.ChildNodes;
			    foreach ( XmlNode AppPoolProperty in AppPoolNodes )
			    {
				    if ( AppPoolProperty.Name == "AppPoolName" )
				    {
					    AppPoolName = AppPoolProperty.InnerText;
					    continue;
				    }
				    TempProperties.Add( AppPoolProperty.Name, AppPoolProperty.InnerText );
			    }
			    AppPoolProperties.Add( AppPoolName, TempProperties );
		    }
		    return AppPoolProperties;
	    }


	    public static void SetAppPoolProperties( DirectoryEntry AppPool, Hashtable AppPoolProperties )
	    {
		    if ( AppPool != null & AppPoolProperties != null )
		    {
			    Hashtable Properties = (Hashtable) AppPoolProperties[ AppPool.Name ];
			    foreach ( DictionaryEntry AppPoolProperty in Properties )
			    {
				    AppPool.Properties[ (string) AppPoolProperty.Key ].Value = AppPoolProperty.Value;
				    AppPool.CommitChanges();
			    }
		    }
	    }


	    // public static DirectoryEntry CreateVDir( DirectoryEntry
	    #endregion

	    #region private properties

	    private int SiteID
	    {
		    get
		    {
			    // return this._SiteID;
			    if ( this._SiteID == -99 )
			    {
				    this.SchemaClassName = "IIsWebServer";
				    this.WebSite         = this.FindWebSite( this.ServerComment );
			    }
			    return this._SiteID;
		    }
		    set
		    {
			    this._SiteID = value;
		    }
	    }


	    private DirectoryEntry WebSite
	    {
		    get
		    {
			    return this._WebSite;
		    }
		    set
		    {
			    this._WebSite = value;
		    }
	    }


	    private void PrintWebsites()
	    {
		    base.LogItWithTimeStamp(
			String.Format( "{0}: Host Machine: {1}", this.ObjectName, this.MachineName ) );

		    try
		    {
			    foreach ( DirectoryEntry de in this.WebSites )
			    {
				    base.LogItWithTimeStamp(
					String.Format( "{0}: {1}", this.ObjectName,
						       de.Properties[ "ServerComment" ].Value.ToString() ) );
			    }
		    }
		    catch {}
	    }

	    private void PrintWebsites( string FileName )
	    {
		    StringBuilder Buffer = new StringBuilder();
		    this.PrintWebsites();

		    if ( File.Exists( FileName ) )
		    {
			    File.Delete( FileName );
		    }

		    using( StreamWriter sw = new StreamWriter( FileName, false ) )
		    {
			    // Buffer.Append( this.MachineName + "\n" );
			    foreach( DirectoryEntry de in this.WebSites )
			    {
				    try
				    {
					    if (de.Properties.Contains( "ServerComment" ))
						    Buffer.AppendFormat( "{0}\n",
									 de.Properties["ServerComment"].Value.ToString() );
				    }
				    catch
				    {
				    }
			    }
			    Buffer = Buffer.Remove( Buffer.Length - 1, 1 );
			    sw.WriteLine( Buffer.ToString() );
			    sw.Flush();

		    }
	    }

	    #endregion
    }
}
