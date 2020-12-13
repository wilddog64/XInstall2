using System;
using System.Collections;
using System.ServiceProcess;
using System.Reflection;
using System.Xml;

using XInstall.Core;
using XInstall.Util;

namespace XInstall.Core.Actions {
    public class SC : ActionElement, IAction {
	    private XmlNode _ActionNode   = null;
	    private Hashtable _Actions    = new Hashtable();
	    // private ServiceController _sc = null;
	    private string _ServerName    = String.Empty;
	    private string _Action        = String.Empty;

	    private enum SC_ACTIONS_FLAG {
		    SC_STOP = 0,
		    SC_START,
		    SC_PAUSE,
		    SC_RESTART,
		    SC_INSTALL,
		    SC_DELETE,
		    SC_UNKNOWN,
	    }

	    // for error handling code
	    private enum SC_OPR_CODE {
		    SC_OPR_SUCCESS = 0,
		    SC_OPR_ACTION_NOT_DEFINED,
		    SC_OPR_BOOLEAN_PARSING_ERROR,
		    SC_OPR_METHOD_INVOCATION_ERROR,
		    SC_OPR_UNRECONGIZED_TAG,
		    SC_OPR_MISSING_ATTRIBUTES,
	    }
	    private SC_OPR_CODE _ScOprCode = SC_OPR_CODE.SC_OPR_SUCCESS;

	    string[] _Messages = {
		    "{0}: {1} operation complete successfully!",
		    "{0}: required action {1} is not defined!",
		    "{0}: {1} error parsing boolean variable!",
		    "{0}: invoked method {1} generates an exception, the message is {2}!",
		    "{0}: {1} encount unrecongnized tag {2}!",
		    "{0}: missing attributes {1} for {2}!",
	    };
	    string _strExitMessage = String.Empty;

	    [Action("sc", Needed=true)]
	    public SC( XmlNode ActionNode ) {
		    this._ActionNode = ActionNode;
		    // this._sc               = new ServiceController();
		    this.SetupActionsTable();
	    }

	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable {
		    set {
			    base.Runnable = bool.Parse(value);
		    }
	    }

	    [Action("skiperror", Needed=false, Default="false")]
	    public new string SkipError {
		    set {
			    base.SkipError = bool.Parse(value);
		    }
	    }

	    [Action("servername", Needed=false, Default=".")]
	    public string ServerName {
		    get {
			    return this._ServerName;
		    }
		    set {
			    this._ServerName = value;
			    // this._sc.MachineName = this._ServerName;
		    }
	    }

	    [Action("action", Needed=false)]
	    public string Action {
		    get {
			    return this._Action;
		    }
		    set {
			    this._Action = value;
			    //                switch ( this._Action ) {
			    //                case "start":
			    //                    scAction = SC_ACTIONS_FLAG.SC_START;
			    //                    break;
			    //                case "stop":
			    //                    scAction = SC_ACTIONS_FLAG.SC_STOP;
			    //                    break;
			    //                case "pause":
			    //                    scAction = SC_ACTIONS_FLAG.SC_PAUSE;
			    //                    break;
			    //                case "restart":
			    //                    scAction = SC_ACTIONS_FLAG.SC_RESTART;
			    //                    break;
			    //                case "install":
			    //                    scAction = SC_ACTIONS_FLAG.SC_INSTALL;
			    //                    break;
			    //                case "delete":
			    //                    scAction = SC_ACTIONS_FLAG.SC_INSTALL;
			    //                    break;
			    //                default:
			    //                    scAction = SC_ACTIONS_FLAG.SC_UNKNOWN;
			    //                    break;
			    //                }
		    }
	    }

	    #region private methods/properties
	    private void SetupActionsTable() {
		    this._Actions.Add( @"stop",    "StopService"    );
		    this._Actions.Add( @"start",   "StartService"   );
		    this._Actions.Add( @"pause",   "PauseService"   );
		    this._Actions.Add( @"restart", "RestartService" );
		    this._Actions.Add( @"install", "InstallService" );
		    this._Actions.Add( @"delete",  "DeleteService"  );
	    }

	    private void CallMethod( object obj, XmlNode xn ) {
		    // get method name from an input XML
		    string Tag        = xn.Name.ToLower();
		    string MethodName = String.Empty;
		    object[] Params   = null;
		    if ( this._Actions.ContainsKey( Tag ) ) {
			    MethodName = (string) this._Actions[ Tag ];
			    Params = new object[2] { MethodName, xn };
		    }
		    else {
			    this.SetExitMessage(
				SC_OPR_CODE.SC_OPR_ACTION_NOT_DEFINED,
				this.Name, MethodName);
			    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
		    }

		    // retrieve type information from an object
		    Type t = obj.GetType();
		    MethodInfo mi = t.GetMethod( MethodName );
		    if ( mi != null ) {
			    try {
				    mi.Invoke( this, Params );
			    }
			    catch ( Exception e ) {
				    this.SetExitMessage(
					SC_OPR_CODE.SC_OPR_METHOD_INVOCATION_ERROR,
					this.Name, MethodName, e.Message );
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
	    }

	    // SetExitMessage - setup message for writing to file and event log
	    private void SetExitMessage( SC_OPR_CODE ScOprCode, params object[] objParams ) {
		    this._ScOprCode = ScOprCode;
		    this._strExitMessage = String.Format( this._Messages[ this.ExitCode ], objParams );
	    }

	    private void StopService( string MethodName, XmlNode xn ) {}

	    private void StartService( string MethodName, XmlNode xn ) {}

	    private void PauseService( string MethodName, XmlNode xn ) {}

	    private void RestartService( string MethodName, XmlNode xn ) {

		    XmlNode MachineName = xn.Attributes.GetNamedItem( "machinename" );
		    XmlNode ServiceName = xn.Attributes.GetNamedItem( "servicename" );
		    ArrayList MachineInfos = new ArrayList();
		    if ( MachineName != null && ServiceName != null ) {
			    ServiceController sc = new ServiceController( ServiceName.Value, MachineName.Value );
			    MachineInfos.Add( sc );
		    }
		    else {
			    foreach ( XmlNode ChildNode in xn.ChildNodes ) {
				    if ( !ChildNode.Name.Equals( "machine" ) ) {
					    this.SetExitMessage(
						SC_OPR_CODE.SC_OPR_UNRECONGIZED_TAG,
						this.Name, MethodName, ChildNode.Name );
					    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
				    }
				    else {
					    MachineName = ChildNode.Attributes.GetNamedItem( "name" );
					    ServiceName = ChildNode.Attributes.GetNamedItem( "servicename" );
					    if ( MachineName.Value != null && ServiceName.Value != null ) {
						    ServiceController sc =
							new ServiceController( ServiceName.Value, MachineName.Value );
						    MachineInfos.Add( sc );
					    }
					    else {
						    this.SetExitMessage(
							SC_OPR_CODE.SC_OPR_MISSING_ATTRIBUTES,
							this.Name, "name, servicename", MethodName );
						    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
					    }
				    }
			    }
		    }

		    IEnumerator Machines = MachineInfos.GetEnumerator();
		    while ( Machines.MoveNext() ) {
			    ServiceController sc = (ServiceController) Machines.Current;
			    if ( sc.CanStop ) {
				    sc.Stop();
				    sc.Start();
			    }

		    }
	    }

	    private void InstallService( string MethodName, XmlNode xn ) {}

	    private void DeleteService( string Methodname, XmlNode xn ) {}
	    #endregion

	    #region IAction Members

	    public override void Execute() {
		    // TODO:  Add ServiceControl.Execute implementation
		    base.Execute();
	    }

	    public override void ParseActionElement() {
		    base.ParseActionElement ();
		    foreach ( XmlNode xn in this._ActionNode.ChildNodes ) {
			    this.CallMethod( this, xn );
		    }
	    }

	    public new bool IsComplete {
		    get {
			    // TODO:  Add ServiceControl.IsComplete getter implementation
			    return false;
		    }
	    }

	    public new string ExitMessage {
		    get {
			    // TODO:  Add ServiceControl.ExitMessage getter implementation
			    return null;
		    }
	    }

	    public new string Name {
		    get {
			    // TODO:  Add ServiceControl.Name getter implementation
			    return this.GetType().Name;
		    }
	    }

	    public override string ObjectName {
		    get {
			    return this.Name;
		    }
	    }

	    public new int ExitCode {
		    get {
			    // TODO:  Add ServiceControl.ExitCode getter implementation
			    return (int) this._ScOprCode;
		    }
	    }

	    #endregion
    }
}
