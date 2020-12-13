using System;
using System.Collections;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

using System.Xml;

using XInstall.Util;

/*
 * Class name    : SendMail
 *
 * Inherit from  : ActionElement
 *
 * Xml tag name  : smtpmail
 *
 * Functionality : send an email by using smtp server.  This class
 *                 use System.Web.Mail to talk to smtp mail server.
 *
 * Created Date  : March 2005
 *
 * Created By    : mliang
 *
 * Change Log    :
 *
 * who              when            what
 * --------------------------------------------------------------
 * mliang           03/11/2005      Initial creation
 *
 * mliang           03/23/2005      Instead of using Regular Expression
 *                                  to search fatal error message from
 *                                  Errors collection object, it uses
 *                                  Error.Level == LEVEL.FATAL to get
 *                                  it.  This modification is done in
 *                                  The ParseActionElement method.
 */
namespace XInstall.Core.Actions {
    /// <summary>
    /// Summary description for SmtpMail.
    /// </summary>
    public class SendMail : ActionElement {
	    private MailMessage  _MailMessage   = null;
	    private Regex        _Regex         = null;
	    private MailPriority _MailPriority = MailPriority.Normal;
	    // private MailFormat   _MailFormat   = MailFormat.Html;
	    private Encoding     _MailEncoding = Encoding.Default;

	    private string       _Server        = String.Empty;
	    private string       _From          = String.Empty;
	    private string       _To            = String.Empty;
	    private string       _Subject       = String.Empty;
	    private string       _FilterLogInfo = String.Empty;
	    private string       _Message       = String.Empty;


	    [Action("smtpmail")]
	    public SendMail(XmlNode ActionNode ) : base( ActionNode ) {
		    this._MailMessage.BodyEncoding = this._MailEncoding;
		    // this._MailMessage.BodyFormat   = this._MailFormat;

		    this._MailMessage.Priority     = this._MailPriority;
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
			    base.SkipError = bool.Parse( value );
		    }
	    }



	    [Action("allowgenerateexception", Needed=false, Default="false")]
	    public new string AllowGenerateException {
		    set {
			    base.AllowGenerateException = bool.Parse( value );
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


	    public new string Name {
		    get {
			    return this.GetType().Name;
		    }
	    }


	    [Action("smtpserver", Needed=true)]
	    public string Server {
		    get {
			    return this._Server;
		    }
		    set {
			    this._Server = value;
			    base.LogItWithTimeStamp( String.Format( "{0}: Smtp Server {1}", this.Name, this._Server ) );
		    }
	    }


	    [Action("sender", Needed=false, Default="currentuser")]
	    public string From {
		    get {
			    return this._From;
		    }
		    set {
			    if ( value == "currentuser" ) {
				    string CurrentUser =
					String.Format( "{0}@{1}",
						       Environment.GetEnvironmentVariable( "USERNAME" ),
						       Environment.GetEnvironmentVariable( "USERDNSDOMAIN" ) );
				    value = CurrentUser;
			    }
			    this._From = value;
			    base.LogItWithTimeStamp(
				String.Format( "{0}: Send from {1}", this.Name, this._From ) );
		    }
	    }


	    [Action("recipients", Needed=true)]
	    public string To {
		    get {
			    return this._To;
		    }
		    set
		    {
			    this._To = value;
			    base.LogItWithTimeStamp( String.Format( "{0}: Send to {1}", this.Name, this._To ) );
		    }
	    }


	    [Action("subject", Needed=false, Default="")]
	    public string Subject {
		    get {
			    return this._Subject;
		    }
		    set {
			    this._Subject = value;
		    }
	    }


	    [Action("message", Needed=false, Default="")]
	    public string Body {
		    get {
			    return this._Message;
		    }
		    set {
			    this._Message = value;
		    }
	    }


	    [Action("filterloginfo", Needed=false, Default="")]
	    public string FilterLogInfo {
		    get {
			    if ( this._FilterLogInfo.Length > 0 ) {
				    this._Regex = new Regex( this._FilterLogInfo );
			    }
			    return this._FilterLogInfo;

		    }
		    set {
			    this._FilterLogInfo = value;
		    }
	    }


	    protected override void ParseActionElement() {
		    base.ParseActionElement();

		    SmtpClient MailClient  = new SmtpClient(this.Server);
		    MailClient.Credentials = CredentialCache.DefaultNetworkCredentials;

		    _MailMessage         = new MailMessage(this.From, this.To);
		    _MailMessage.Subject = this.Subject;

		    ArrayList ALogMessages = new ArrayList();
		    bool UseRegEx = false;

		    if ( this._Regex != null )
			    UseRegEx = true;

		    for ( int i = 0; i < base.Errors.Count; i++ ) {
			    Error AnError     = base.Errors[i];
			    string LogMessage = AnError.ToString();

			    if ( AnError.Level == LEVEL.FATAL )
				    ALogMessages.Add( AnError.ToString() );

			    if ( UseRegEx ) {
				    Match m = this._Regex.Match( LogMessage );
				    if ( m.Success )
					    ALogMessages.Add( LogMessage );
			    }
		    }

		    if ( ALogMessages != null && ALogMessages.Count > 0 ) {
			    foreach ( string LogMessage in ALogMessages ) {
				    this.Body += String.Format("<BR>{0}", LogMessage );
			    }
		    }

		    this._MailMessage.Body = this.Body;

		    base.LogItWithTimeStamp( String.Format( "{0}: Message to be sent {1}", this.Name, this.Body ) );

		    _MailMessage.Body = this.Body;
		    try {
			    // MailClient.Send( this._MailMessage );
			    MailClient.Send(_MailMessage);
		    }
		    catch ( Exception e ) {
			    base.IsComplete = false;
			    base.FatalErrorMessage( ".", String.Format( "{0}: unable to deliever message b/c {1}", this.Name, e.Message ), 1660 );
			    throw;
		    }
		    base.IsComplete = true;
		    base.LogItWithTimeStamp(
			String.Format( "{0}: message sent successfully!!", this.Name ) );
	    }

    }
}
