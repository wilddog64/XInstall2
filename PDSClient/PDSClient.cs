using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using System.Xml;
using System.Xml.XPath;

using XInstall.Core;
using XInstall.Util;

namespace XInstall.Custom.Actions {
    /// <summary>
    /// Summary description for PDSClient
    /// </summary>
    /// <summary>
    /// PDSClient.cs is a class that performs the PDS web service against
    /// a given Project Server.
    /// </summary>
    /// <remarks>
    ///     The PDSClient class provides the following methods for handling
    ///     request PDS method calls/properties:
    ///
    ///     <li>
    ///         <ol>ProjectServer   - setup project server for PDS request to be sent to</ol>
    ///         <ol>PDSNewUsers     - creating logging accounts for users.
    ///                               Users info can be put into a file
    ///         </ol>
    ///         <ol>ProjectCheckin  - check-in check-outed project</ol>
    ///         <ol>ProjectCheckout - check-out project</ol>
    ///         <ol>LoadResources   - load resources into a check-out project. Resources
    ///                               can be stored in a file.</ol>
    ///     </li>
    /// </remarks>
    public class PDSCLient : DBAccess, IAction {
        // Object Type variables
        private XPathDocument  _xpdDoc               = null;
        private XmlDocument    _xdDoc                = new XmlDocument();
        private XmlNode        _xnActionNode         = null;
        private PDS            _pdsCall              = null;
        private Hashtable      _htPDSTokens          = new Hashtable();
        private Regex          _re                   = null;
        private Hashtable      _htResourceErrorTable = new Hashtable();

        // Value Type variables
        private int    _iBatchSize         = 1;
        private string _strCookie          = String.Empty;
        private string _strProjectServer   = String.Empty;
        private string _strPDSCall         = String.Empty;
        private string _strPDSToken        = String.Empty;

        // these variables can't be changed during the runtime
        private readonly string _rostrPDSAuths     = @"lgnintau.asp";
        private readonly string _rostrRequestXML   = @"<Request/>";

        // project check in/out emueration
        private Hashtable _htProjectTypesLookup   = new Hashtable();
        private Hashtable _htProjectSecurityGroup = new Hashtable();
        private Hashtable _htNewUserLookup        = new Hashtable();

        // database related member variables
        private bool _ReadFromDatabase     = true;
        private bool _UseTrustedConnection = true;
        private string _UserName           = String.Empty;
        private string _UserPassword       = String.Empty;

        // error code handling
        // enumeration for the PDS operation codes
        private enum PDS_OPR_CODE {
            PDS_OPR_SUCCESS = 0,
            PDS_OPR_URL_NOT_PROVIDED,
            PDS_OPR_URL_ERROR,
            PDS_OPR_XPATHDOC_GEN_ERROR,
            PDS_OPR_CANT_GET_PDS_COOKIE,
            PDS_OPR_HTTWEBOBJECT_ERROR,
            PDS_OPR_TOKEN_NOT_RECONG,
            PDS_OPR_METHOD_PARAMETERS_TYPE_ERROR,
            PDS_OPR_METHOD_PARAMETERS_NULLREF,
            PDS_OPR_METHOD_GENERATES_EXCEPTIONS,
            PDS_OPR_METHOD_PARAMETERS_COUNT_INCORRECT,
            PDS_OPR_METHOD_NOACCESS_RIGHT,
            PDS_OPR_MISSING_REQUIRED_ATTRIBUTE,
            PDS_OPR_FILE_NOTFOUND,
            PDS_OPR_INIT_ERROR,
            PDS_OPR_ATTRIBUTE_TYPE_INCORRECT,
            PDS_OPR_UNKNOWN_PROJECT_TYPE,
            PDS_OPR_REQUIRES_ATLEAST_ONE_ATTRIBUTE,
            PDS_OPR_METHOD_REQUIRED_PARAMETERS,
            PDS_OPR_HEADER_FIELDS_NOT_ALIGN,
            PDS_OPR_UNKNOWN_PSNEWUSER_TAG,
            PDS_OPR_WRITEING_RESOURCE_STATUS_FAIL,
        }
        private PDS_OPR_CODE _enumPDSOprCode = PDS_OPR_CODE.PDS_OPR_SUCCESS;
        private string _strExitMessage       = String.Empty;

        // error message table
        private string[] _strMessages = {
                @"{0} - Successfully connect to {1}",
                @"{0} - URL is required",
                @"{0} - Given url {1} has problem, message {2}",
                @"{0} - Unable to create XPathDocument",
                @"{0} - Cannot obtain a cookie from {1}",
                @"{0} - Error creating HttpWeb... related objects, message {1}",
                @"{0} - input token {1} is not recognized!",
                @"{0} - parameters type for method {1} is not matched, message - {2}",
                @"{0} - method {1} required parameters and you don't provide them, message - {2}!",
                @"{0} - calling method {1} generate an exception, message - {2}",
                @"{0} - number of parameters pass into method {1} is not matched, message - {2}",
                @"{0} - caller {1} does not have permission to execute this method {2}, message - {3}",
                @"{0} - attribute {0} is required by {1}!",
                @"{0} - {1}: cannot find input file {2}!",
                @"{0} - PDS initial error, message: {1}",
                @"{0} - attribute {1} of {2} is an {3} field",
                @"{0} - {1}: project type {2} is unknown!",
                @"{0} - {1} requires at least one of the attributes {2}",
                @"{0} - {1} requires parameters!",
                @"{0} - {1}: the numbers in header fields are different from the number of fields!",
                @"{0} - {1}: tag {2} is not recognized!\n",
                @"{0} - {1}: unable to write resource status info to file {2}, message: {3}",
            };

        /// <summary>
        /// A constructor that setup the PDSClient object
        /// </summary>
        /// <param name="xnActionNode">the content of the pds Xml node</param>
        /// <remarks>
        ///  PDS web service has the following header,
        ///
        ///     <request>
        ///         ...
        ///     </request>
        ///
        ///  The constructor will construct an empty element that contains <request />
        ///  and later each method can stuff different format into it.
        ///
        ///  You will also need to setup a web reference points to actual P11 Server where
        ///  stored the PDS.wsdl file
        /// </remarks>
        [Action("pds")]
        public PDSCLient( XmlNode xnActionNode ) : base() {
            this.SetupPDSTokens();                       // setup PDS token table
            this.SetupPDSErrorCodeMapping();             // Resource Error Code Table
            this._pdsCall             = new PDS();       // initialize PDS web service call
            this._pdsCall.Credentials = CredentialCache.DefaultCredentials;
            this._xnActionNode        = xnActionNode;    // get XML tokens
            this._xdDoc.LoadXml( this._rostrRequestXML );
            this._re = new Regex( @"<STATUS>(\d+)</STATUS>", RegexOptions.Multiline );

        }

#region pds properties
        /// <summary>
        /// get/set the project server we want to send request to.
        /// </summary>
        /// <remarks>
        ///     ProjectServer property will automatically construct a correct
        ///     URL for the user. For example, if you provide the server name
        ///     chengkai-01, it will return the following URL to access it:
        ///
        ///         http://chengkai-01/projectserver
        /// </remarks>
        [Action("projsrv", Needed=true)]
        public string ProjectServer {
            get {
                return this._strProjectServer;
            }
            set {
                try {
                    // if http:// is not prefixed then add it
                    // otherwise, prepends http:// infront of it.
                    if ( value.IndexOf( @"http://" ) < 0 )
                        this._strProjectServer =
                            String.Format( @"http://{0}/projectserver/", value );
                    else
                        this._strProjectServer =
                            String.Format( @"{0}/projectserver/", value );

                    // make PDS points to the URL we just created.
                    this._pdsCall.ProjectURL = value;

                }
                // handling exception and log message to file and event log
                catch ( Exception e ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_INIT_ERROR,
                        this.Name, e.Message );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }
            }
        }

        /// <summary>
        /// Sets boolean variable to indicate if the object should run or not
        /// By default, it is set to true, which means object should always
        /// be runnable
        /// </summary>
        [Action("runnable", Needed=false, Default="true")]
        public new string Runnable {
            set {
                base.Runnable = bool.Parse( value );
            }
        }

        /// <summary>
        /// Sets a boolean variable to tell object should generate an exception
        /// or not. By default, this is set to false, which means object should
        /// not generate an exception.
        /// </summary>
        [Action("allowgenerateexception", Needed=false, Default="false")]
        public new string AllowGenerateException {
            set {
                base.AllowGenerateException = bool.Parse( value );
            }
        }

        /// <summary>
        /// get the PDS security cookie
        /// </summary>
        /// <remarks>
        ///     The property actually calls the private property
        ///     PDSCookie to get the cookie
        /// </remarks>
        [Action("pdscookie", Needed=false)]
        public string CookieValue {
            get {
                return this.PDSCookie;
            }
        }

        [Action("fromdb", Needed=false, Default="true")]
        public string ReadFromDatabase {
            set {
                this._ReadFromDatabase = bool.Parse( value );
            }
        }

        [Action("trustedconnection", Needed=false, Default="true")]
        public string TrustedConnection {
            set {
                this._UseTrustedConnection = bool.Parse( value );
            }
        }

        [Action("dbuser", Needed=false, Default="")]
        public string DBUserName {
            get {
                return this._UserName;
            }
            set {
                this._UserName = value;
            }
        }

        [Action("dbuserpasswd", Needed=false, Default="")]
        public string DBUserPassword {
            get {
                return this._UserPassword;
            }
            set {
                this._UserPassword = value;
            }
        }

        [Action("skiperror", Needed=false, Default="false")]
        public new string SkipError {
            set {
                base.SkipError = bool.Parse( value );
            }
        }

#endregion pds properties

#region private utility methods/properties

        // setup PDS recognized tokens
        private void SetupPDSTokens() {
            //  PDSClient recognized tokens
            this._htPDSTokens.Add( @"logininfo",           @"GetLoginInfo" );
            this._htPDSTokens.Add( @"pdsinfo",             @"GetPDSInfo" );
            this._htPDSTokens.Add( @"loadresources",       @"LoadResources" );
            this._htPDSTokens.Add( @"pdsnewusers",         @"PSNewUsers");
            this._htPDSTokens.Add( @"pdsadduserstogroups", @"PSNewUsers" );
            this._htPDSTokens.Add( @"projectcheckout",     @"ProjectInOut" );
            this._htPDSTokens.Add( @"projectcheckin",      @"ProjectInOut" );

            // project type lookup table
            this._htProjectTypesLookup.Add( @"regular_project",  0 );
            this._htProjectTypesLookup.Add( @"project_template", 1 );
            this._htProjectTypesLookup.Add( @"global_template",  2 );
            this._htProjectTypesLookup.Add( @"resource_global",  3 );

            // project security group name
            this._htProjectSecurityGroup.Add( @"admin", @"Administrators" );
            this._htProjectSecurityGroup.Add( @"exec",  @"Executives" );
            this._htProjectSecurityGroup.Add( @"pfm",   @"Portfolio Managers" );
            this._htProjectSecurityGroup.Add( @"pm",    @"Project Managers" );
            this._htProjectSecurityGroup.Add( @"rm",    @"Resource Managers" );
            this._htProjectSecurityGroup.Add( @"tl",    @"Team Leads" );
            this._htProjectSecurityGroup.Add( @"tm",    @"Team Members" );

            // new user keyword
            this._htNewUserLookup.Add( @"username", @"PSUserName" );
            this._htNewUserLookup.Add( @"ntacct",   @"PSUserNTAccount" );
            this._htNewUserLookup.Add( @"adguid",   @"PSUserADGUID" );
            this._htNewUserLookup.Add( @"email",    @"PSUserEmail" );
            this._htNewUserLookup.Add( @"phonetic", @"PSUserPhonetic" );
            this._htNewUserLookup.Add( @"sg",       @"PSGroupName" );
        }

        private void SetupPDSErrorCodeMapping() {
            // setup resource error mapping table
            this._htResourceErrorTable.Add( 2000, @"Resouce Not Found!" );
            this._htResourceErrorTable.Add( 2001, @"Resource Already Exists!" );
            this._htResourceErrorTable.Add( 2002, @"Resource Checked Out by Other User!" );
            this._htResourceErrorTable.Add( 2004, @"Resource Parameters are not valid!" );
            this._htResourceErrorTable.Add( 2006, @"Resource Does Not Checkout" );
            this._htResourceErrorTable.Add( 2007, @"Resource Can't Be Deactivate When It is Checkout!" );
            this._htResourceErrorTable.Add( 2008, @"Resource Is Already Deactivate!" );
            this._htResourceErrorTable.Add( 2009, @"Resource Already Deactiate!" );
            this._htResourceErrorTable.Add( 2010, @"Resource Exceed Maximum Numbers!" );
            this._htResourceErrorTable.Add( 2016, @"Resource Name Is Not Valid!" );
            this._htResourceErrorTable.Add( 2017, @"Resource Name Is Too Long!" );
            this._htResourceErrorTable.Add( 2018, @"Resource Initial Is Too Long!" );
            this._htResourceErrorTable.Add( 2025, @"Resource Checkout!" );
            this._htResourceErrorTable.Add( 2026, @"Resource NT Account Is Invalid!" );
            this._htResourceErrorTable.Add( 2027, @"Resource Name Is Already Existed!" );
            this._htResourceErrorTable.Add( 2028, @"Resource NT Account Is Already In Use!" );
            this._htResourceErrorTable.Add( 2042, @"Resource Name Suffix Is Too Long" );
            this._htResourceErrorTable.Add( 2043, @"Resource Name Suffix Required!" );
        }

        // private property method that retrieve the PDS security cookie
        private string PDSCookie {
            get {
                if ( this._strCookie == String.Empty )
                    this._strCookie = this.GetPDSCookie();
                if (this._strCookie.StartsWith(@"0"))
                    this._strCookie = this._strCookie.TrimStart( '0' );
                return this._strCookie;
            }
        }

        // GetPDSCookie returns the scurity cookie obtained from a given project server
        // This use HttpWebRequest object to accomplish the task.
        private string GetPDSCookie() {
            HttpWebRequest hwrRequest = null;
            string strPDSCookie = string.Empty;

            // make sure url is passed in
            if ( this.ProjectServer == String.Empty ) {
                this.SetExitMessage( PDS_OPR_CODE.PDS_OPR_URL_NOT_PROVIDED,
                                     this.Name );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }

            // now create a request and setup a credential
            HttpWebResponse hwrResp = null;
            try {
                string strAuthURL =
                    String.Format( "{0}{1}", this.ProjectServer, this._rostrPDSAuths );
                hwrRequest = (HttpWebRequest) WebRequest.Create( strAuthURL );
                CookieContainer ccCookie = new CookieContainer();
                hwrRequest.CookieContainer = ccCookie;
                hwrRequest.Credentials = CredentialCache.DefaultCredentials;

                // get response back
                hwrResp = (HttpWebResponse) hwrRequest.GetResponse();
                if ( hwrResp.StatusCode != HttpStatusCode.OK ) {
                    this.SetExitMessage( PDS_OPR_CODE.PDS_OPR_URL_ERROR,
                                         this.Name, this.ProjectServer, hwrResp.StatusDescription );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }
            } catch ( WebException we ) {
                this.SetExitMessage(
                    PDS_OPR_CODE.PDS_OPR_HTTWEBOBJECT_ERROR,
                    this.Name, we.Message);
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }


            // now load the XML stream into XPath Document and prepare for navigation
            this._xpdDoc       = new XPathDocument( hwrResp.GetResponseStream() );
            XPathNavigator xpn = null;
            if ( this._xpdDoc != null ) {
                xpn = this._xpdDoc.CreateNavigator();
                XPathNodeIterator xpniCookie = xpn.Select( @"Reply/Cookie" );
                if ( xpniCookie != null )
                    strPDSCookie = xpniCookie.Current.Value;
                else {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_CANT_GET_PDS_COOKIE,
                        this.Name,  this.ProjectServer );
                    base.FatalErrorMessage( ".",
                                            this.ExitMessage,
                                            1660 );
                }
            } else {
                this.SetExitMessage(
                    PDS_OPR_CODE.PDS_OPR_XPATHDOC_GEN_ERROR,
                    this.Name );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }

            this._strPDSCall = string.Format( @"{0}{1}", this.ProjectServer, this._strPDSCall );
            return strPDSCookie;
        }

        // SendPDSRequest - sent a PDS request to a given ProjectServer
        private string SendPDSRequest(string strXml) {
            string strReturn = String.Empty;
            strReturn        = this._pdsCall.SoapXMLRequest( this.PDSCookie, strXml );
            return strReturn;
        }

        // SetExitMessage - setup message for writing to file and event log
        private void SetExitMessage( PDS_OPR_CODE enumPDSCode, params object[] objParams ) {
            this._enumPDSOprCode = enumPDSCode;
            this._strExitMessage = String.Format( this._strMessages[ this.ExitCode ], objParams );
        }

        // CallMethod - call PDSClient method dynamically base on xml input
        public void CallMethod( object obj, XmlNode xn ) {
            // obtain PDS token and perform a lookup to see
            // if given token is valid and throw an approperiate
            // exception if it can't be recognized
            this._strPDSToken    = xn.Name.ToLower();
            string strMethodName = String.Empty;
            object[] objParams   = null;
            if ( this._htPDSTokens.ContainsKey( this._strPDSToken ) ) {
                strMethodName = (string) this._htPDSTokens[ this._strPDSToken ];
                objParams     = new object[3] { strMethodName, xn, xn.InnerXml };
            } else {
                this.SetExitMessage(
                    PDS_OPR_CODE.PDS_OPR_TOKEN_NOT_RECONG,
                    this.Name, strMethodName);
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }

            // get object type and retrieve method from it. if method can be obtained,
            // we call it by using reflection invoke method.
            Type tObjectType     = obj.GetType();
            MethodInfo miMethod  = tObjectType.GetMethod( strMethodName );
            if ( miMethod != null ) {
                try {
                    miMethod.Invoke( this, objParams );
                } catch ( ArgumentException ae ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_METHOD_PARAMETERS_TYPE_ERROR,
                        this.Name, strMethodName,
                        ae.GetBaseException().Message );
                    throw ae;
                } catch ( TargetException te ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_METHOD_PARAMETERS_NULLREF,
                        this.Name, strMethodName,
                        te.GetBaseException().Message );
                    throw te;
                } catch ( TargetInvocationException tie ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_METHOD_GENERATES_EXCEPTIONS,
                        this.Name, strMethodName,
                        tie.GetBaseException().Message );
                    throw tie;
                } catch ( TargetParameterCountException tpce ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_METHOD_PARAMETERS_COUNT_INCORRECT,
                        this.Name, strMethodName,
                        tpce.GetBaseException().Message );
                    throw tpce;
                } catch ( MethodAccessException mae ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_METHOD_NOACCESS_RIGHT,
                        this.Name, Environment.UserDomainName, strMethodName,
                        mae.GetBaseException().Message );
                    throw mae;
                }
            }
        }

        // CreateResource - based on Header and fields information passed in, create a proper
        //                  XML node. There's one overloaded function for this that only accept
        //                  the string fields
        private XmlNode CreateResource ( ref string[] strHeader, ref string[] strFields ) {
            // setup method name
            string strMethodName = @"CreateResource";

            // check if the length of strHeader and strFields are the same
            if ( strHeader.Length != strFields.Length ) {
                this.SetExitMessage(
                    PDS_OPR_CODE.PDS_OPR_HEADER_FIELDS_NOT_ALIGN,
                    this.Name, strMethodName );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }

            // create resource XML node
            XmlNode xnResource =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"Resource", String.Empty );
            for ( int i = 0; i < strHeader.Length; i++ ) {
                XmlNode xn =
                    this._xdDoc.CreateNode( XmlNodeType.Element, strHeader[i].Trim(), String.Empty );
                xn.InnerText = strFields[i].Trim();
                xnResource.AppendChild( xn );
            }

            // return the node we just create
            return xnResource;
        }

        // CreateNewUser - wraps PDS call, PSNewUser.
        //     PSNewUser has the following format:
        //
        //         <PSNewUser>
        //            <PSUsers>
        //                 <PSUser>
        //                    <PSUserName></PSUserName>
        //                    <PSUserNTAccount</PSUserNTAccount>
        //                    <PSUserADGUID></PSUserADGUID>
        //                    <PSUserEmail></PSUserEmail>
        //                    <PSUserPhonetic><?PSUserPhonetic>
        //                    <PSGroups>
        //                        <PSGroup>
        //                              <PSGroupName></PSGroupName>
        //                        </PSGroup>
        //                    </PSGroups>
        //               </PSUser>
        //          </PSUsers>
        //        </PSNewUser>
        private XmlNode CreateNewUser( string[] strHeader, string[] strFields, bool bPDSNewUser ) {
            // method signature
            string strMethodName = @"CreateNewUser";

            // PSNewUser PDS request header
            XmlNode xnUsers = null;
            if ( bPDSNewUser )
                xnUsers =
                    this._xdDoc.CreateNode( XmlNodeType.Element, @"PSNewUser", String.Empty );
            else
                xnUsers =
                    this._xdDoc.CreateNode( XmlNodeType.Element, @"PSUsers", String.Empty );

            // PSUsers node and it child nodes
            XmlNode xnPSUsers   =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"PSUsers", String.Empty );

            XmlNode xnPSUser    = null;
            xnPSUser            =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"PSUser", String.Empty );
            if ( !bPDSNewUser ) {
                XmlNode xnPSUserName =
                    this._xdDoc.CreateNode( XmlNodeType.Element, @"PSUserName", String.Empty );
                xnPSUser.AppendChild( xnPSUserName );
            }

            xnPSUsers.AppendChild( xnPSUser );

            // PSGroups node and it child nodes
            XmlNode xnPSGroups  =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"PSGroups", String.Empty );
            XmlNode xnPSGroup   = null;
            xnPSGroup = this._xdDoc.CreateNode( XmlNodeType.Element, @"PSGroup", String.Empty );
            if ( !bPDSNewUser ) {
                XmlNode xnPSGroupName =
                    this._xdDoc.CreateNode( XmlNodeType.Element, @"PSGroupName", String.Empty );
                xnPSGroups.AppendChild( xnPSGroupName );
            }
            xnPSGroups.AppendChild( xnPSGroup );
            xnPSUser.AppendChild( xnPSGroups );


            xnUsers.AppendChild( xnPSUsers );

            if ( strHeader.Length != strFields.Length ) {
                this.SetExitMessage(
                    PDS_OPR_CODE.PDS_OPR_HEADER_FIELDS_NOT_ALIGN,
                    this.Name, strMethodName );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }

            string strTagName = String.Empty;
            for ( int i = 0; i < strHeader.Length; i++ ) {
                if ( this._htNewUserLookup.ContainsKey( strHeader[i].Trim(null).ToLower() ) ) {
                    strTagName = (string) this._htNewUserLookup[ strHeader[i].Trim(null).ToLower() ];
                    if ( !strTagName.Equals( @"PSGroupName" ) ) {
                        XmlNode xn =
                            this._xdDoc.CreateNode( XmlNodeType.Element, strTagName, String.Empty );
                        xn.InnerText = strFields[i];
                        xnPSUser.AppendChild( xn );
                    } else {
                        XmlNode xn =
                            this._xdDoc.CreateNode( XmlNodeType.Element, strTagName, String.Empty );
                        xn.InnerText =
                            (string) this._htProjectSecurityGroup[ strFields[i].Trim(null).ToLower() ];
                        xnPSGroup.AppendChild( xn );
                    }
                } else {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_UNKNOWN_PSNEWUSER_TAG,
                        this.Name, strMethodName, strHeader[i] );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }
            }

            return xnPSUsers;
        }

        // AddResources - create a property sub-node for the ResourcesAdd PDS
        //                method
        //
        //                ResourcesAdd has the following XML format
        //
        //                   <ResourcesAdd>
        //                         <Resources>
        //                              <Name></Name>
        //                              <NTAccount></NTAccount>
        //                              <EmailAddress></EmailAddress>
        //                              <!-- others -->
        //                         <Resources>
        //                   </ResourcesAdd>
        //
        // For this call, it only create Name, NTAccount, and EmailAddress elements
        private XmlNode AddResources(
            XmlDocument xdDoc,
            string      strResourceName,
            string      strResourceEMailAddress,
            string      strResourceNTAccount ) {
            XmlNode xnResource     =
                xdDoc.CreateNode( XmlNodeType.Element, @"Resource", String.Empty );

            // create inner node for Resource
            // <Name>
            XmlNode xnResourceName =
                xdDoc.CreateNode( XmlNodeType.Element, @"Name", String.Empty );
            xnResourceName.InnerText = strResourceName.Trim(null);

            // <NTAccount>
            XmlNode xnResourceNTAccount =
                xdDoc.CreateNode( XmlNodeType.Element, @"NTAccount", String.Empty );
            xnResourceNTAccount.InnerText = strResourceNTAccount.Trim(null);

            // <EmailAddaress>
            XmlNode xnResourceEMailAddress =
                xdDoc.CreateNode( XmlNodeType.Element, @"EmailAddress", String.Empty );
            xnResourceEMailAddress.InnerText = strResourceEMailAddress.Trim(null);

            // Append elements to Resource XML node
            xnResource.AppendChild( xnResourceName );
            xnResource.AppendChild( xnResourceNTAccount );
            xnResource.AppendChild( xnResourceEMailAddress );

            return xnResource;
        }

        // GetReturnStatus - return PDS status code by parsing returned XML
        //                   from a PDS request
        private int GetReturnStatus( string strReturnXml ) {
            int iReturnCode = -99;
            Match m = this._re.Match( strReturnXml );
            if ( m.Success ) {
                iReturnCode = int.Parse( m.Groups[1].Captures[0].Value );
            }
            return iReturnCode;
        }

        // WriteResourceStatusInfo - Write return status from ResourcesAdd PDS service call
        //                           It also plug in the reason why PDS call is failing.
        private void WriteResurceStatusInfo( int iStatus, XmlDocumentFragment xdf, string strOutputFile ) {
            string strMethodName = @"WriteResourceStatusInfo";  // method signature
            StringBuilder sbBuffer = new StringBuilder();       // string buffer for writing to file

            // header
            string strHeader = String.Format( @"{0}|{1}|{2}|{3}\n",
                                              @"RowID", @"ResourceName",
                                              @"ResourceNTAccount", @"ReplyStatus",
                                              @"RejectReason" );

            // we only need to write a record whose return status is not zero
            if ( iStatus != 0 ) {
                try {

                    // open file for writing
                    using ( StreamWriter sw = new StreamWriter( strOutputFile )  ) {
                        // write header
                        sw.WriteLine( strHeader );

                        // collect allow the required XML nodes and loop throught it
                        XmlNodeList xnl = xdf.SelectNodes( @"Reply/ResourcesAdd/Resources" );
                        for ( int i = 0; i < xnl.Count; i++ ) {
                            XmlNode xn = xnl[i];

                            // grab necessary values
                            string strResourceName   =
                                xn.SelectSingleNode( @"Resource/Name" ).InnerText;
                            int iReplyStatus         =
                                int.Parse(
                                    xn.SelectSingleNode( @"Resource/ReplyStatus" ).InnerText );
                            string strRejectReason   =
                                this._htResourceErrorTable[ iReplyStatus ].ToString();
                            int iRowID               = int.Parse(
                                                           xn.SelectSingleNode( @"Resource/RowID" ).InnerText );

                            // append to string buffer
                            sbBuffer.AppendFormat(
                                "{0}|{1}|{2}|{3}\n",
                                iRowID, strResourceName,
                                iReplyStatus, strRejectReason);
                        }
                        // and write to file
                        sw.WriteLine( sbBuffer );
                    }

                } catch ( Exception e ) { // if anything went wrong, report it.
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_WRITEING_RESOURCE_STATUS_FAIL,
                        this.Name, strMethodName, strOutputFile, e.Message );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }
            }
        }

        private XmlDocumentFragment AddUsersToGroups( string strUserName, string strGroupName ) {
            // method signature
            // string strMethodName = @"AddUsersToGroups";

            // create an Xml Document Fragment object
            XmlDocumentFragment xdfDoc  = this._xdDoc.CreateDocumentFragment();

            // PSAddUsersToGroups
            XmlNode xnPSAddUsersToGroups =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"PSAddUsersToGroups", String.Empty );

            // PSUsers
            XmlNode xnPSUsers =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"PSUsers", String.Empty );
            XmlNode xnPSUserName =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"PSUserName", String.Empty );
            xnPSUsers.AppendChild( xnPSUserName );

            // PSGroups
            XmlNode xnPSGroups =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"PSGroups", String.Empty );
            XmlNode xnPSGroupName =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"PSGroupName", String.Empty );

            // append PSUsers and PSGroups to PSAddUsersToGroups
            xnPSAddUsersToGroups.AppendChild( xnPSUsers );
            xnPSAddUsersToGroups.AppendChild( xnPSGroups );
            xdfDoc.AppendChild( xnPSAddUsersToGroups );

            if ( strUserName != String.Empty )
                xnPSUserName.InnerText = strUserName;
            if ( strGroupName != String.Empty )
                xnPSGroupName.InnerText = strGroupName;

            return xdfDoc;
        }

        // GetTable - accept a name of table that contains resources information
        //            and return an SqlDataAdapter object
        private SqlDataAdapter GetTable( string DBTableName ) {
            // create SqlDataAdapter object
            SqlDataAdapter sda = base.Select(
                                     String.Format( @"SELECT * FROM {0}", DBTableName ) );

            // generate an update command with proper parameters
            SqlCommand UpdateCommand = new SqlCommand(
                                           String.Format(
                                               @"update {0} set WasUpdated = 0, PDSErrorCode = @PDSErrorCode where FullName = @FullName",
                                               DBTableName ), base.ConnectionObject );
            UpdateCommand.Parameters.Add( "@FullName", SqlDbType.NVarChar, 510, @"FullName" );
            UpdateCommand.Parameters.Add( "@PDSErrorCode", SqlDbType.Int, 4, @"PDSErrorCode" );
            // assign command object to SqlDataAdapter object
            sda.UpdateCommand = UpdateCommand;

            // return this object
            return sda;
        }

#endregion

#region IAction Members

        /// <summary>
        /// an overrided interface method that takes in charge
        /// of executing each PDS request
        /// </summary>
        /// <remarks></remarks>
        public override void Execute() {
            // base.ActionElementObject = (IActionElement) this;
            base.Execute();
            base.IsComplete = true;
        }

        /// <summary>
        /// returns the object exection state
        /// </summary>
        /// <remarks></remarks>
        public new bool IsComplete {
            get {
                return base.IsComplete;
            }
        }

        /// <summary>
        /// interface property that gets the message from PDSClient object.
        /// </summary>
        public new string ExitMessage {
            get {
                return this._strExitMessage;
            }
        }

        /// <summary>
        /// an interface property that returns the object name
        /// </summary>
        /// <remarks></remarks>
        public new string Name {
            get {
                return this.GetType().Name;
            }
        }

        /// <summary>
        /// an interface property that returns the exit code
        /// </summary>
        /// <remarks></remarks>
        public new int ExitCode {
            get {
                return (int) this._enumPDSOprCode;
            }
        }

#endregion

#region IActionElement Members

        /// <summary>
        /// an interface method that return the object name
        /// </summary>
        protected override string ObjectName {
            get {
                return this.Name;
            }
        }

        /// <summary>
        /// an interface method that parse the Xml Node content
        /// </summary>
        //  <remarks>
        //    this overrided method is called by Execute() method from the
        //    ActionElement. Be sure you override the Execute method and call
        //    base class' Execute method; otherwise, nothing will happened.
        //  </remarks>
        protected override void ParseActionElement() {
            // call base class's ParseActionElement method
            base.ParseActionElement();

            // call base class's Execute() method and retrive
            // security cookie from a given project server
            string strCookie = this.PDSCookie;

            string strToken  = String.Empty;
            // visit each node in a given XML node
            try {
                for ( int i = 0; i < this._xnActionNode.ChildNodes.Count; i++ ) {
                    XmlNode xn = this._xnActionNode.ChildNodes[i];
                    if ( xn.NodeType != XmlNodeType.Comment )
                        this.CallMethod( this, xn );
                }
            } catch ( Exception e ) {
                base.LogItWithTimeStamp( e.Message );
            } finally {
                XmlNode xn = this._xnActionNode.SelectSingleNode( @"projectcheckin" );
                if ( xn !=  null )
                    this.CallMethod( this, xn );
            }
        }

#endregion

#region PDS web service methods defined here

        /// <summary>
        /// a method that wraps the PDSInfo web service request and send
        /// it to a given Project Server
        /// </summary>
        /// <param name="strMethodName">name of the method, GetPDSInfo</param>
        /// <param name="xn">an XML node for GetPDSInfo</param>
        /// <param name="strInXML">an string that contains an PDS XML request</param>
        /// <remarks>
        ///     GetPDSInfo wraps the following PDS request,
        ///
        ///         <Request>
        ///             <PDSInfo />
        ///         </Request>
        ///
        ///     And send it to a Project Server for processing
        /// </remarks>
        public void GetPDSInfo( string strMethodName, XmlNode xn, string strInXML ) {
            XmlDocumentFragment xcfDoc = this._xdDoc.CreateDocumentFragment();
            xcfDoc.InnerXml = @"<PDSInfo />";
            this._xdDoc.DocumentElement.AppendChild( xcfDoc );

            string strXml = this.SendPDSRequest( this._xdDoc.InnerXml ).TrimEnd( null );
            base.LogItWithTimeStamp ( strXml );
            this._xdDoc.DocumentElement.RemoveAll();
        }


        /// <summary>
        /// Wraps PDS request <ResourcesAdd /> into PDSClient object
        /// </summary>
        /// <param name="strMethodName">Name of the Method, LoadResources</param>
        /// <param name="xn">Xml Node</param>
        /// <param name="strInXML">Xml in string</param>
        ///
        /// <remarks>
        ///     the PDS request <ResourcesAdd /> has the following format,
        ///
        ///     <Request>
        ///         <ResourcesAdd>
        ///             <Resources>
        ///                 <Resource>
        ///                     <Name>...</Name>
        ///                 </Resource>
        ///             </Resources>
        ///         </ResourcesAdd>
        ///     </Request>
        ///
        /// <Name /> is a required element. There are other optional elements, please
        ///          referes to PDS document.
        /// Note:
        ///     1. Before a resource can be added to the ProjectServer, a given
        ///        project has to be checkout.  There're 4 default projects that can
        ///        be used. Please refer to ProjectInOut for details
        ///     2. Only one resource can be add per PDS ResourcesAdd request
        ///     3. When resource successfully added to the resource pool, a status of
        ///        0 will be return; otherwise, please check PDSREF.chm for the status
        ///        code greater than 0.
        /// </remarks>
        public void LoadResources( string strMethodName, XmlNode xn, string strInXML ) {
            // retrieve attribute from an Xml Node, loadresources
            XmlAttributeCollection xac = xn.Attributes;
            XmlNode xnFromFile         = xac.GetNamedItem( @"fromfile" );
            XmlNode xnDataSource       = xac.GetNamedItem( @"datasource" );
            XmlNode xnInitCatalog      = xac.GetNamedItem( @"initcatalog" );
            XmlNode xnDBTable          = xac.GetNamedItem( @"dbtable" );
            XmlNode xnBatchSize        = xac.GetNamedItem( @"batchsize" );
            XmlNode xnLimit            = xac.GetNamedItem( @"limit" );
            XmlNode xnOffset           = xac.GetNamedItem( @"offset" );
            XmlNode xnHasHeader        = xac.GetNamedItem( @"headerline" );
            XmlNode xnDelima           = xac.GetNamedItem( @"delima" );
            XmlNode xnRejectFile       = xac.GetNamedItem( @"rejectfile" );
            XmlNode xnGenerateException = xac.GetNamedItem( @"generateexception" );

            bool GenerateException = false;
            if ( xnGenerateException != null )
                GenerateException = bool.Parse(xnGenerateException.Value);

            if ( GenerateException )
                base.FatalErrorMessage( ".", "Client requests to generate an exception!!", 1660);

            // fromfile is a required attribute, so we croak if it is not presented
            // we also need to make sure if a given input file does exist.
            string strFileName = String.Empty;
            SqlDataAdapter sda = null;
            if ( !this._ReadFromDatabase ) {

                if ( xnFromFile == null ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_MISSING_REQUIRED_ATTRIBUTE,
                        this.Name, @"fromfile", strMethodName);
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }
                strFileName = xnFromFile.Value;
                if ( !File.Exists( strFileName ) ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_FILE_NOTFOUND,
                        this.Name, strMethodName, strFileName);
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }
            } else {

                // if trusted connection is not used, then
                // username and password are required
                if ( !this._UseTrustedConnection ) {
                    if ( this.UserName.Length == 0 ) {
                        this.SetExitMessage(
                            PDS_OPR_CODE.PDS_OPR_MISSING_REQUIRED_ATTRIBUTE,
                            this.Name, @"UserName", @"PDSClient");
                        base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    }
                    if ( this.UserPassword.Length == 0 ) {
                        this.SetExitMessage(
                            PDS_OPR_CODE.PDS_OPR_MISSING_REQUIRED_ATTRIBUTE,
                            this.Name, @"UserName", @"PDSClient");
                        base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    }
                    base.UserName     = this.DBUserName;
                    base.UserPassword = this.DBUserPassword;
                }

                // make sure we have data source
                if ( xnDataSource == null ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_MISSING_REQUIRED_ATTRIBUTE,
                        this.Name, @"datasource", strMethodName );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }

                // check if database is provided
                if ( xnInitCatalog == null ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_MISSING_REQUIRED_ATTRIBUTE,
                        this.Name, @"initcatalog", strMethodName );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }

                // we also want to make sure table name is provided
                if ( xnDBTable == null ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_MISSING_REQUIRED_ATTRIBUTE,
                        this.Name, @"dbtable", strMethodName);
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }

                // assign DataSource and InitCatlog
                base.DataSource = xnDataSource.Value;
                base.InitCatlog = xnInitCatalog.Value;

                // connect to a given SQL server and database
                base.Connect();

                // get SqlDataAdapter object
                sda = this.GetTable( xnDBTable.Value );
            }

            // now we check if batchsize attribute is existed. if not we will assign
            // a default value for it
            if ( xnBatchSize != null )
                try {
                    this._iBatchSize = int.Parse ( xnBatchSize.Value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_ATTRIBUTE_TYPE_INCORRECT,
                        this.Name,
                        @"BatchSize",
                        strMethodName,
                        @"integer" );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }

            // if limit is provided, grab and validate it.
            int iLimit = -99;
            if ( xnLimit != null ) {
                try {
                    iLimit = int.Parse( xnLimit.Value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_ATTRIBUTE_TYPE_INCORRECT,
                        this.Name,
                        strMethodName,
                        @"integer" );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }
            }

            // where we should start reading from file
            int iOffset = -99;
            if ( xnOffset != null )
                try {
                    iOffset = int.Parse( xnOffset.Value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_ATTRIBUTE_TYPE_INCORRECT,
                        this.Name,
                        strMethodName,
                        @"integer" );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }

            bool bHeaderLine = false;
            if ( xnHasHeader != null )
                try {
                    bHeaderLine = bool.Parse( xnHasHeader.Value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_ATTRIBUTE_TYPE_INCORRECT,
                        this.Name, strMethodName, @"bool" );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }

            // get delima character if one is provided; otherwise,
            // default to | character
            char cDelima = '|';
            if ( xnDelima != null )
                cDelima = xnDelima.Value.ToCharArray(0, 1)[0];

            // get reject data file name from the attribute.
            // we default this to reject.dat if nothing is provided
            string strRejectFile =
                xnRejectFile != null  ?
                xnRejectFile.Value    :
                @"reject.dat";


            // create a document fragment object and add required elements into it.
            // <ResourcesAdd>
            //      <Resources></Resources>
            // </ResourcesAdd>
            XmlDocumentFragment xdfDoc = this._xdDoc.CreateDocumentFragment();
            XmlNode xnResourcesNew  =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"ResourcesAdd", String.Empty );
            XmlNode xnResources     =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"Resources", String.Empty );
            xnResourcesNew.AppendChild( xnResources );
            xdfDoc.AppendChild( xnResourcesNew );

            // now open a file for reading
            // using( StreamReader sr = new StreamReader( strFileName ) ) {
            // object variable
            XmlDocumentFragment xdfStatusInfo = this._xdDoc.CreateDocumentFragment();
            // various counters
            int iRecordCount   = 0;     // how many records do we process so far
            int iBatchCount    = 0;     // how many batches do we send so far
            int iLineCount     = 0;     // where are we now?
            int iRC            = 0;     // return code from PDS method
            bool bHeader       = false;
            string[] strHeader = null;
            DataTable dt = new DataTable();

            // while there're more lines to process
            // while ( sr.Peek() >= 0 ) {
            // fill DataTable object, dt
            if ( sda != null )
                sda.Fill( dt );

            if ( dt.Rows.Count == 0 ) {
                base.LogItWithTimeStamp(
                    String.Format( "Table {0} has no data", xnDBTable.Value ) );
                return;
            }
            foreach ( DataRow dr in dt.Rows ) {

                // assign required items
                // dr[0] - FullName
                // dr[1] - EmailAddress
                // dr[2] - NTLogin
                string FullName     = dr["FullName"]     != DBNull.Value ?
                                      (string) dr["FullName"]     : "" ;
                string EmailAddress = dr["EmailAddress"] != DBNull.Value ?
                                      (string) dr["EmailAddress"] : "" ;
                string NTLoginID    = dr["NTLoginID"]    != DBNull.Value ?
                                      (string) dr["NTLoginID"]    : "" ;
                string[] strItems = { FullName, EmailAddress, NTLoginID };

                iLineCount++;
                // check to see if it is time to send records
                if ( ++iRecordCount >= this._iBatchSize ) {

                    // create inner XML Element
                    // <Resource>
                    //    <Name></Name>
                    //    <NTAccount></NTAccount>
                    //    <EmailAddress></EmailAddress>
                    // </Resource>
                    XmlNode xnResource = null;
                    if ( bHeader )
                        xnResource = this.CreateResource( ref strHeader, ref strItems );
                    else
                        xnResource =
                            this.AddResources( this._xdDoc,
                                               strItems[0],
                                               strItems[1],
                                               strItems[2]);

                    // Append Resource element to Resources node
                    xnResources.AppendChild(xnResource);

                    // if offset is set and we are not reach it yet, then
                    // jump to the next line until we reach it.
                    if ( iOffset != -99 && ++iLineCount <= iOffset ) {
                        base.LogItWithTimeStamp(
                            String.Format( @"Line {0} skipped", iLineCount ) );
                        continue;
                    }

                    if ( iLimit > 0 )
                        if ( ++iBatchCount > iLimit )
                            break;

                    // add document fragment to our XML Document object
                    // and turn off the console output
                    this._xdDoc.DocumentElement.AppendChild( xdfDoc );
                    base.OutToConsole = false;

                    // send PDS web service request, log the return value from it
                    // and turn on the console output.
                    string strReturn = String.Empty;
                    try {
                        strReturn = this.SendPDSRequest ( this._xdDoc.OuterXml );
                        base.LogItWithTimeStamp (
                            String.Format( @"return status {0}", strReturn ) );
                    } catch ( Exception ) {
                        base.LogItWithTimeStamp ( String.Format( @"return status {0} - {1}",
                                                  strReturn, this._xdDoc.OuterXml ) );
                    }


                    // get return status from PDS call and if status is not zero, then update
                    // table field WasUpdated to 0
                    iRC = this.GetReturnStatus( strReturn );
                    if ( iRC != 0 ) {
                        strReturn = strReturn.Insert(
                                        strReturn.IndexOf( @"</ReplyStatus>" ) +
                                        @"</ReplyStatus>".Length,
                                        String.Format(
                                            @"<RowID>{0}</RowID>", iLineCount  ) );

                        xdfStatusInfo.InnerXml += strReturn;
                        dr["WasUpdated"] = "false";
                        dr["PDSErrorCode"] = iRC;
                        base.LogItWithTimeStamp(
                            String.Format( "Reply: {0} \n - XML: {1}",
                                           xdfStatusInfo.InnerXml,
                                           this._xdDoc.OuterXml ));
                    }

                    // turn on console output
                    base.OutToConsole = true;

                    // cleanup node for the next call
                    xnResources.RemoveAll();
                    base.LogItWithTimeStamp(
                        String.Format(
                            @"sent {0} records to project",
                            iRecordCount
                        ) );

                    iRecordCount = 0;
                }
            }

            this.WriteResurceStatusInfo( iRC, xdfStatusInfo, strRejectFile );

            // Get changed rows and update the table in SQL Server
            DataTable ChangedTable = dt.GetChanges( DataRowState.Modified );
            sda.Update(ChangedTable);
            // }

            // remove all child nodes from document
            this._xdDoc.DocumentElement.RemoveAll();
        }

        /// <summary>
        /// Create a new user logging for a given ProjectServer by sending a
        /// <PDSNewUsers /> request
        /// </summary>
        /// <param name="strMethodName"></param>
        /// <param name="xn"></param>
        /// <param name="strInXML"></param>
        public void PSNewUsers( string strMethodName, XmlNode xn, string strInXML  ) {
            XmlAttributeCollection xac = xn.Attributes;
            bool bPDSNewUsers = true;
            if ( strMethodName.Equals( @"pdsadduserstogroups" ) )
                bPDSNewUsers = false;

            XmlNode xnFromFile = xac.GetNamedItem( @"fromfile" );
            if ( xnFromFile == null ) {
                this.SetExitMessage(
                    PDS_OPR_CODE.PDS_OPR_MISSING_REQUIRED_ATTRIBUTE,
                    this.Name, @"fromfile", strMethodName );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }

            string strFileName = xnFromFile.Value;
            if ( !File.Exists( strFileName ) ) {
                this.SetExitMessage(
                    PDS_OPR_CODE.PDS_OPR_FILE_NOTFOUND,
                    this.Name, strMethodName, strFileName );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }

            XmlNode xnBatchSize = xac.GetNamedItem( @"batchsize" );
            if ( xnBatchSize != null )
                try {
                    this._iBatchSize = int.Parse( xnBatchSize.Value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_ATTRIBUTE_TYPE_INCORRECT,
                        @"batchsize", strMethodName, @"integer");
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }

            XmlNode xnHeaderLine = xac.GetNamedItem( @"headerline" );
            bool bHasHeader      = false;
            if ( xnHeaderLine != null ) {
                try {
                    bHasHeader = bool.Parse( xnHeaderLine.Value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_ATTRIBUTE_TYPE_INCORRECT,
                        this.Name, strMethodName, @"bool" );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }
            }

            // retrieve delima character if any; otherwise, set it default to | character
            XmlNode xnDelima = xac.GetNamedItem( @"delima" );
            char cDelima     = '|';
            if ( xnDelima != null )
                cDelima = xnDelima.Value.ToCharArray()[0];

            XmlDocumentFragment xdfDoc = this._xdDoc.CreateDocumentFragment();
            using ( StreamReader sr = new StreamReader( strFileName ) ) {
                int iRecordCount   = 0;
                bool bUseHeader    = false;
                string[] strHeader = null;

                while ( sr.Peek() >= 0 ) {
                    if ( bHasHeader ) {
                        strHeader  = sr.ReadLine().Split( cDelima );
                        bHasHeader = false;
                        bUseHeader = true;
                        continue;
                    }

                    string[] strItems = sr.ReadLine().Split( '|' );
                    if ( strItems[0].StartsWith( @"--" ) )
                        continue;

                    // a judgement that if we want to use PDSNewUsers call or
                    // PDSAddUsersToGroups call
                    XmlNode xnUserHeader = null;
                    if ( bPDSNewUsers )
                        xnUserHeader =
                            this._xdDoc.CreateNode( XmlNodeType.Element, @"PSNewUser", String.Empty );
                    else
                        xnUserHeader =
                            this._xdDoc.CreateNode( XmlNodeType.Element, @"PSAddUsersToGroup", String.Empty );

                    XmlNode xnPSUser = null;
                    if ( bUseHeader ) {
                        xnPSUser = this.CreateNewUser( strHeader, strItems, bPDSNewUsers );
                        xnUserHeader.AppendChild( xnPSUser );
                    }

                    if ( ++iRecordCount >= this._iBatchSize ) {

                        // first create user
                        this._xdDoc.DocumentElement.AppendChild( xnUserHeader );
                        string strReturn = this.SendPDSRequest( this._xdDoc.InnerXml );
                        int iRC = this.GetReturnStatus( strReturn );
                        this._xdDoc.DocumentElement.RemoveChild( xnUserHeader );

                        base.LogItWithTimeStamp(
                            String.Format( "creats {0} records sent to project", iRecordCount ) );

                        base.OutToConsole = false;
                        base.LogItWithTimeStamp(
                            String.Format( "records are {0}", this._xdDoc.InnerXml ) );
                        base.LogItWithTimeStamp(
                            String.Format( "return status: {0} ", strReturn ) );
                        base.OutToConsole = true;

                        iRecordCount = 0;
                        this._xdDoc.DocumentElement.RemoveAll();
                    }
                }
            }
            this._xdDoc.DocumentElement.RemoveAll();
            // return 0;
        }

        /// <summary>
        /// perform PDS ProjectCheckin & ProjectCheckout service call
        /// </summary>
        /// <param name="strMethodName">name of the method, usually it is the method signature</param>
        /// <param name="xn">the XmlNode</param>
        /// <param name="strInXML">Xml in string format</param>
        /// <remarks>
        ///     ProjectInOut will create ProjectCheckin and ProjectCheckout request base on
        ///     a given XmlNode name.  If a given XML node name is projectcheckin then
        ///     <ProjectCheckin /> request will be created.  If node name is projectcheckout,
        ///     then <ProjectCheckout /> request will be created.  The ProjectCheckin and
        ///     ProjectCheckout has the following format,
        ///
        ///         <ProjectsCheckin>
        ///             <Project>
        ///                 <ProjectType></ProjectType>
        ///                 <ProjectID></ProjectID>
        ///                 <ProjectName></ProjectName>
        ///                 <ProjectSummaryRecord></ProjectSummaryRecord>
        ///             </Project>
        ///             <Forced></Forced>
        ///             <ServerPath></ServerPath>
        ///         </ProjectCheckin>
        ///
        ///         <ProjectsCheckout>
        ///             <Project>
        ///                 <ProjectType></ProjectType>
        ///                 <ProjectID></ProjectID>
        ///                 <ProjectName></ProjectName>
        ///             </Project>
        ///         </ProjectCheckout>
        ///     ProjectType is a required element and it can be one of the followings,
        ///
        ///         0 -> Regular project (this is default)
        ///         1 -> Project template
        ///         2 -> Global template
        ///         3 -> Resource global
        ///
        ///     Others are optional and please refer to PDSREF.chm for detail usage.
        /// </remarks>
        public void ProjectInOut( string strMethodName, XmlNode xn, string strInXML  ) {
            // get attribute collection from a given Xml node
            XmlAttributeCollection xac = xn.Attributes;

            // retrieve projecttype attribute and validate the value within it
            XmlNode xnProjectType = xac.GetNamedItem( @"projecttype" );
            int iProjectType      = -99;
            if ( xnProjectType != null ) {
                string strProjectType = xnProjectType.Value.ToLower();
                if ( !this._htProjectTypesLookup.ContainsKey( strProjectType ) ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_UNKNOWN_PROJECT_TYPE,
                        this.Name,
                        strMethodName,
                        strProjectType );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }
                try {
                    iProjectType = (int) this._htProjectTypesLookup[ strProjectType ];
                } catch ( Exception ) {
                    this.SetExitMessage(
                        PDS_OPR_CODE.PDS_OPR_ATTRIBUTE_TYPE_INCORRECT,
                        this.Name,
                        strMethodName,
                        @"integer" );
                    // base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    throw new Exception( this.ExitMessage );
                }
            }

            // get projectid if it is presented
            XmlNode xnProjectID = xac.GetNamedItem( @"projectid" );
            int iProjectID      = -99;
            if ( xnProjectID != null )
                iProjectID = int.Parse(xnProjectID.Value);

            // get project name if it is presented
            XmlNode xnProjectName = xac.GetNamedItem( @"projectname" );
            string strProjectName = string.Empty;
            if ( xnProjectName != null )
                strProjectName = xnProjectName.Value;

            // at least one of the attribute need to present
            bool bHasOne = iProjectType != -99 ||
                           iProjectID   != -99 ||
                           strProjectName != String.Empty;
            if ( !bHasOne ) {
                this.SetExitMessage(
                    PDS_OPR_CODE.PDS_OPR_REQUIRES_ATLEAST_ONE_ATTRIBUTE,
                    this.Name,
                    strMethodName,
                    @"projecttype, projectid, projectname" );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }

            // now creating a PDS ProjectCheckout request
            XmlDocumentFragment xdfDoc = this._xdDoc.CreateDocumentFragment();

            // determine if an Xml is projectcheckout or projectcheckin request as
            // the head for such request is different
            string strNodeName = xn.Name.ToLower();
            string strProjectInOut = String.Empty;
            if ( strNodeName.Equals( @"projectcheckout" ) )
                strProjectInOut = @"ProjectsCheckout";
            else if ( strNodeName.Equals( @"projectcheckin" ) )
                strProjectInOut = @"ProjectsCheckin";
            XmlNode xnProjectsInOut =
                this._xdDoc.CreateNode( XmlNodeType.Element, strProjectInOut, String.Empty );

            // create sub-components
            XmlNode xnProject          =
                this._xdDoc.CreateNode( XmlNodeType.Element, @"Project", String.Empty );
            xnProjectsInOut.AppendChild( xnProject );
            if ( iProjectType != -99 )
                xnProject.InnerXml +=
                    String.Format( @"<ProjectType>{0}</ProjectType>", iProjectType );
            if ( iProjectID != -99 )
                xnProject.InnerXml
                += String.Format( @"<ProjectID>{0}</ProjectID>", iProjectID );
            if ( strProjectName != String.Empty )
                xnProject.InnerXml
                += String.Format( @"<ProjectName>{0}</ProjectName>", strProjectName );
            xdfDoc.AppendChild( xnProjectsInOut );

            // add document fragment into our document object
            // and send our request to PDS server
            this._xdDoc.DocumentElement.AppendChild( xdfDoc );
            base.LogItWithTimeStamp(
                String.Format( @"send PDS request {0} to server {1}",
                               this._xdDoc.InnerXml, this.ProjectServer  ) );
            int iRC = 0;
            try {
                string strReturn = this.SendPDSRequest( this._xdDoc.InnerXml );
                iRC = this.GetReturnStatus( strReturn );
            } catch ( Exception e ) {
                base.LogItWithTimeStamp( e.Message + " " + this._xdDoc.InnerXml );
            }

            base.LogItWithTimeStamp(
                String.Format( @"return status for {0} is {1}", strProjectInOut, iRC ) );

            // return code 1007 means project has been check-out already
            // so we simply return without raise an exception
            if ( iRC == 1007 )
                return;
            else if ( iRC != 0 )
                throw new Exception(
                    String.Format( "Error happened during {0} - {1}", xnProjectsInOut.Name,
                                   this._xdDoc.InnerXml ) );

            // cleanup document object
            this._xdDoc.DocumentElement.RemoveAll();
        }
#endregion
    }
}
