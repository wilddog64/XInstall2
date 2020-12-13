using System;
using System.Collections;
using System.DirectoryServices;
using System.Reflection;
using System.Xml;

namespace XInstall.Core.Actions {
    /// <summary>
    /// Summary description for IIsVirtualDir.
    /// </summary>
    public class IIsVirtualDir : AdsiBase {
	    private XmlNode         _ActionNode      = null;

	    private string          _MachineName          = string.Empty;
	    private string          _WebSiteName          = string.Empty;
	    private string          _VDirName             = String.Empty;
	    private string          _ActionVerb           = string.Empty;
	    private string          _AssignDefaultAppPool = @"true";
	    private string          _PhysicalPath         = @"c:\inetpub\wwwroot";
	    private readonly string _SchemaClassName      = @"IIsWebVirtualDir";
	    private readonly string _AdsiProvider         = @"IIS";
	    private readonly string _AdsiPath             = @"W3SVC";
	    private readonly string _DefaultAppPool       = @"DefaultAppPool";
	    private int             _SiteID               = 1;

	    [Action("iisvdir")]
	    public IIsVirtualDir( XmlNode ActionNode ) : base( ActionNode ) {
		    this._ActionNode = ActionNode;
	    }


	    public IIsVirtualDir( int SiteID, XmlNode ActionNode ) : base( ActionNode ) {
		    this._ActionNode = ActionNode;
		    this._SiteID     = SiteID;
	    }


	    public IIsVirtualDir( string MachineName, int SiteID, XmlNode ActionNode ) : base( ActionNode ) {
		    this._ActionNode  = ActionNode;
		    this._SiteID      = SiteID;
		    this._MachineName = MachineName;
	    }



	    [Action("MachineName", Needed=false, Default="localhost")]
	    public override string MachineName {
		    get {
			    return base.MachineName;
		    }
		    set {
			    base.MachineName = value;
		    }
	    }


	    [Action("websitename", Needed=false, Default="")]
	    public string WebSiteName {
		    get {
			    return this._WebSiteName;
		    }
		    set {
			    this._WebSiteName = value;
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
	    public string AssignDefaultAppPool {
		    get {
			    return this._AssignDefaultAppPool;
		    }
	    }


	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable {
		    set {
			    base.Runnable = bool.Parse( value );
		    }
	    }


	    [Action("vdirname", Needed=true)]
	    public string VirtualDirectoryName {
		    get {
			    return this._VDirName;
		    }
		    set {
			    this._VDirName = value;
		    }
	    }


	    [Action("path", Needed=true)]
	    public string Path {
		    get {
			    return this._PhysicalPath;
		    }
		    set {
			    this._PhysicalPath = value;
		    }
	    }


	    [Action("action", Needed=true)]
	    public string Action {
		    get {
			    return this._ActionVerb;
		    }
		    set
		    {
			    this._ActionVerb = value;
		    }
	    }


	    public new string Name {
		    get {
			    return this.GetType().Name;
		    }
	    }


	    protected override string ObjectName {
		    get {
			    return this.GetType().Name;
		    }
	    }


	    protected override void ParseActionElement() {
		    base.ParseActionElement ();

		    DirectoryEntry VDir = null;
		    switch ( this.Action.ToLower() ) {
		    case "create":
			    VDir = this.CreateVirutalDirectory( this.WebSiteName, this.VirtualDirectoryName );
			    break;
		    case "delete":
			    break;
		    case "update":
			    break;
		    case "pause":
			    break;
		    default:
			    base.FatalErrorMessage( ".", String.Format( "{0}: unrecognized action verb: {1}", this.Name, this.Action ), 1660 );
			    break;
		    }
	    }


	    protected override string AdsiProvider {
		    get {
			    return this._AdsiProvider;
		    }
	    }


	    protected override string SchemaClassName {
		    get {
			    return this._SchemaClassName;
		    }
	    }


	    protected override object ObjectInstance {
		    get {
			    return this;
		    }
	    }


	    public int SiteID {
		    get {
			    return this._SiteID;
		    }
		    set {
			    this._SiteID = value;
		    }
	    }


	    public override string AdsiPath {
		    get {
			    return this._AdsiPath;
		    }
	    }


	    #region public methods

    //        public DirectoryEntry CreateVirtualDirectory( DirectoryEntry WebSite  )
    //        {
    //            return this.BindingAppVirtualDirectroy( WebSite, this._ActionNode );
    //        }


    //        public DirectoryEntry UpdatevirtualDirectory( string WebSiteName )
    //        {
    //            DirectoryEntry WebSite = this.FindWebSite( WebSiteName );
    //            this.BindingAppVirtualDirectroy( WebSite, this.SiteID, this._ActionNode, false );
    //        }


	    #endregion

	    #region public static methods

	    public static IIsVirtualDir Create( XmlNode xn ) {
		    IIsVirtualDir ThisVirtualDir = new IIsVirtualDir( xn );
		    return ThisVirtualDir;
	    }


	    public static IIsVirtualDir Create( int SiteID, XmlNode xn ) {
		    IIsVirtualDir ThisVirtualDir = new IIsVirtualDir( SiteID, xn );
		    return ThisVirtualDir;
	    }


	    public static IIsVirtualDir Create( string MachineName, int SiteID, XmlNode xn ) {
		    IIsVirtualDir ThisVirtualDir = new IIsVirtualDir( MachineName, SiteID, xn );
		    return ThisVirtualDir;
	    }



	    // private static AssignValue ( object ThisType
	    #endregion

	    #region private methods

	    private Hashtable GetVDirProertyValues( XmlNode xn ) {
		    Hashtable PropertyCollection = new Hashtable();

		    if ( xn.HasChildNodes )
			    foreach ( XmlNode ChildNode in xn.ChildNodes ) {
				    if ( ChildNode.HasChildNodes ) {
					    PropertyCollection = this.GetVDirProertyValues( ChildNode );
				    }
				    else {
					    PropertyCollection.Add( xn.Name, xn.Value );
				    }
			    }

		    return PropertyCollection;
	    }


	    private void BindingAppVirtualDirectroy( DirectoryEntry VDir, XmlNode xn ) {

		    if ( VDir != null ) {
			    Hashtable VDirProperties = this.GetVDirProertyValues( xn );
			    VDirProperties.Add( "AppRoot", String.Format( "LM/W3SVC/{0}/Root", this.SiteID ) );
			    base.SetAdsiObjectProperty( VDir, VDirProperties );
		    }

		    if ( this.AssignDefaultAppPool.ToLower().Equals("true") ) {
			    this.SetDefaultAppPool( VDir );
		    }
	    }


	    private DirectoryEntry CreateVirutalDirectory( string WebSiteName, string VirtualDirectoryName ) {
		    DirectoryEntry WebSite = this.FindWebSite( WebSiteName );
		    DirectoryEntry VDir    = null;

		    if ( WebSite != null ) {
			    VDir = WebSite.Children.Add( VirtualDirectoryName, this.SchemaClassName );
			    this.BindingAppVirtualDirectroy( VDir, this._ActionNode );
		    }

		    bool AllowCreateDefaultAppPool = bool.Parse( this.AssignDefaultAppPool );
		    if ( AllowCreateDefaultAppPool ) {
			    this.SetDefaultAppPool( VDir );
		    }
		    return VDir;
	    }


	    private void SetDefaultAppPool( DirectoryEntry Entry ) {
		    object[] Params = {2, this.DefaultAppPool, false };
		    Entry.Invoke( "AppCreate3", Params );
	    }


	    private DirectoryEntry FindWebSite( string WebSiteName ) {

		    string SchemaClassName = @"IIsWebServer";
		    DirectoryEntries Entries = this.Directories;
		    DirectoryEntry FoundEntry = null;
		    foreach ( DirectoryEntry Entry in Entries ) {
			    if ( Entry.SchemaClassName == SchemaClassName )
				    if ( Entry.Properties[ "ServerComment" ].Value.Equals( WebSiteName ) ) {
					    FoundEntry = Entry;
					    break;
				    }
		    }

		    this.SiteID = Convert.ToInt32(FoundEntry.Name);
		    return FoundEntry;
	    }


	    private bool DirectoryExist() {
		    return DirectoryEntry.Exists( this.IIsPath );
	    }


	    private bool DirectoryExist( string Path ) {
		    return DirectoryEntry.Exists( Path );
	    }

	    #endregion

	    #region private properties

	    private string IIsPath {
		    get {
			    string ThisPath = String.Format( @"{0}:\\{1}\{2}\{3}\Root", this.AdsiProvider, this.MachineName, this.IIsPath );
			    return ThisPath;
		    }
	    }


	    private string DefaultAppPool {
		    get {
			    return this._DefaultAppPool;
		    }

	    }

	    #endregion
    }
}
