using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using XInstall.Util;

// using XInstall.Core;
/*
 * Class Name    : Blat
 * Inherient     : ActionElement
 * Functionality : Wrap Blat.exe into this class so that XInstall2 can
 *                 send an email out
 *
 * Created Date  : May 2003
 * Created By    : mliang
 *
 * Change Log    :
 *
 * who              when            what
 * --------------------------------------------------------------
 * mliang           01/27/2005      initial creation
 * mliang           01/31/2005      change -from parameter to -f
 *                                  so that blat.exe won't complain
 *                                  that it needs to have server and
 *                                  sender to be set in register
 *                                  database.
 * mliang           02/01/2005      Add logic in GetArguments()
 *                                  override method to make sure that
 *                                  either a sender or from attribute
 *                                  needs to be supplied; otherwise,
 *                                  an fatal error is thrown.
 *                                  Add -priority option to this
 *                                  wrapper class, so that scripter
 *                                  can specify the priority of an
 *                                  email (0 for low, and 1 for high;
 *                                  by default, it is 0).
 */

namespace XInstall.Core.Actions
{
    /// <summary>
    /// Summary description for Blat.
    /// </summary>
    public class Blat : ExternalPrg
    {
	    private const string BLATEXEC = @"blat.exe";

	    private bool   _SendHtml      = true;
	    private bool   _SendUseFile   = true;

	    private string _BlatPath      = string.Empty;
	    private string _Sender        = string.Empty;
	    private string _From          = string.Empty;
	    private string _TempFile      = Path.Combine( Path.GetTempPath(),
					    Path.GetTempFileName() );
	    private string _Recipients    = string.Empty;
	    private string _Subject       = string.Empty;
	    private string _Message       = string.Empty;
	    private string _Attachment    = string.Empty;
	    private string _SmtpServer    = string.Empty;
	    private string _FilterLogInfo = string.Empty;
	    private string _Priority      = "0";
	    private string[] _Attachments = null;


	    private XmlNode _ActionNode   = null;
	    private Regex   _Regex        = null;

	    [Action("blat")]
	    public Blat( XmlNode ActionNode ) : base( ActionNode )
	    {
		    this._ActionNode = ActionNode;
	    }


	    [Action("blatpath", Needed=true)]
	    public string BlatPath
	    {
		    get
		    {
			    return this._BlatPath;
		    }
		    set
		    {
			    this._BlatPath = value;
			    if ( !Directory.Exists( this._BlatPath ) )
			    {
				    base.FatalErrorMessage( ".", String.Format( "path {0} does not exist", this._BlatPath ), 1660 );
			    }
		    }
	    }


	    [Action("sender", Needed=false, Default="")]
	    public string Sender
	    {
		    get
		    {
			    return this._Sender;
		    }
		    set
		    {
			    this._Sender = value;
		    }
	    }


	    [Action("from", Needed=false, Default="")]
	    public string From
	    {
		    get
		    {
			    return this._From;
		    }
		    set
		    {
			    this._From = value;
		    }
	    }


	    [Action("subject", Needed=true)]
	    public string Subject
	    {
		    get
		    {
			    return this._Subject;
		    }
		    set
		    {
			    this._Subject = value;
		    }
	    }


	    [Action("recipients", Needed=true)]
	    public string Recipients
	    {
		    get
		    {
			    return this._Recipients;
		    }
		    set
		    {
			    this._Recipients = value;
		    }
	    }


	    [Action("message", Needed=false, Default="")]
	    public string Message
	    {
		    get
		    {
			    return string.Format( "{0}", this._Message );
		    }
		    set
		    {
			    this._Message = value;
			    base.LogItWithTimeStamp( String.Format( "{0}: Message being sent, {1}", this.Name, this.Message ) );
		    }
	    }


	    [Action("attachments", Needed=false, Default="")]
	    public string Attachements
	    {
		    get
		    {
			    return this._Attachment;
		    }
		    set
		    {
			    this._Attachment = value;
			    if ( this._Attachment.IndexOf( @";" ) > -1  )
			    {
				    this._Attachments = this._Attachment.Split( ';' );
			    }
			    else if ( this._Attachment.Length > 0 )
				    this._Attachments = new string[1] { this._Attachment };
		    }
	    }


	    [Action("smtpserver", Needed=false, Default="localhost")]
	    public string SMTPServer
	    {
		    get
		    {
			    return this._SmtpServer;
		    }
		    set
		    {
			    this._SmtpServer = value;
		    }
	    }


	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable
	    {
		    set
		    {
			    base.Runnable = bool.Parse(value);
		    }
	    }


	    [Action("skiperror", Needed=false, Default="false")]
	    public new string SkipError
	    {
		    set
		    {
			    base.SkipError = bool.Parse( value );
		    }
	    }


	    [Action("allowgenerateexception", Needed=false, Default="false")]
	    public new string AllowGenerateException
	    {
		    set
		    {
			    base.AllowGenerateException = bool.Parse( value );
		    }
	    }


	    [Action("priority", Needed=false, Default="0")]
	    public string Priority
	    {
		    get
		    {
			    return this._Priority;
		    }
		    set
		    {
			    this._Priority = value;
		    }
	    }


	    [Action("filterloginfo", Needed=false, Default="")]
	    public string FilterLogInfo
	    {
		    get
		    {
			    return this._FilterLogInfo;
		    }
		    set
		    {
			    this._FilterLogInfo = value;
			    this._Regex         = new Regex( this._FilterLogInfo );
		    }
	    }


	    [Action("sendashtml", Needed=false, Default="true")]
	    public string SendAsHtml
	    {
		    set
		    {
			    this._SendHtml = bool.Parse( value );
		    }
	    }


	    [Action("sendusefile", Needed=false, Default="true")]
	    public string SendUseFile
	    {
		    set
		    {
			    this._SendUseFile = bool.Parse( value );
		    }
	    }

	    protected override string GetArguments()
	    {
		    StringBuilder BlatCmdLine = new StringBuilder();
		    base.EchoBlank            = false;

		    string BlatExec = this.BlatPath + Path.DirectorySeparatorChar + BLATEXEC;
		    if ( !File.Exists( BlatExec ) )
		    {
			    base.FatalErrorMessage( ".", String.Format("{0}: executable program: {1} does not exist!", base.ObjectName, BlatExec), 1660 );
		    }

		    if ( !this._SendUseFile )
		    {
			    BlatCmdLine.AppendFormat( "/c echo {0} | {1} - ", this.Message.Length != 0 ?  this.Message : this.GetMessage(), BlatExec );
		    }
		    else
		    {
			    using( StreamWriter w = new StreamWriter( this._TempFile ) )
			    {
				    w.WriteLine( this.Message );
			    }
			    base.ProgramName = BlatExec;
			    BlatCmdLine.AppendFormat( " {0} ", this._TempFile );
		    }

		    if ( this.From.Length == 0 && this.Sender.Length == 0 )
			    base.FatalErrorMessage( ".",
						    String.Format( "{0}: you have to specify a sender via either a from or sender attribute", base.ObjectName ),
						    1660 );

		    if ( ( this.Sender.Length > 0 || this.From.Length > 0 ) &&
			    this.From.Length == 0 )
		    {
			    BlatCmdLine.AppendFormat( "-f {0} ", this.Sender );
		    }

		    if ( this._SendHtml )
		    {
			    BlatCmdLine.Append( "-html " );
		    }

		    if ( this.Priority.Length > 0 )
		    {
			    BlatCmdLine.AppendFormat( "-priority {0} ", this.Priority );
		    }

		    if ( this.SMTPServer.Length != 0 )
		    {
			    BlatCmdLine.AppendFormat( "-server {0} ", this.SMTPServer );
		    }

		    if ( this.Recipients.Length != 0 )
		    {
			    BlatCmdLine.AppendFormat( "-to \"{0}\" ", this.Recipients );
		    }

		    if ( this.Subject.Length != 0 )
		    {
			    BlatCmdLine.AppendFormat( "-subject \"{0}\" ", this.Subject );
		    }

		    return BlatCmdLine.ToString();
	    }


	    protected override object ObjectInstance
	    {
		    get
		    {
			    return this;
		    }
	    }


	    protected override string ObjectName
	    {
		    get
		    {
			    return this.Name;
		    }
	    }


	    public int BlatExitCode
	    {
		    get
		    {
			    return base.ProgramExitCode;
		    }
	    }


	    public new string Name
	    {
		    get
		    {
			    return this.GetType().Name;
		    }
	    }


	    public override void Execute()
	    {
		    // base.ProgramName = BlatExec;
		    base.ProgramRedirectOutput = "true";

		    if ( this.FilterLogInfo.Length > 0 )
		    {
			    ErrorCollection Errors = base.Errors;
			    for ( int i = 0; i < Errors.Count; i++ )
			    {
				    Error AnError = Errors[i];
				    string LogMessage = AnError.ToString();
				    if ( this._Regex != null )
				    {
					    Match m = this._Regex.Match( LogMessage );
					    if ( m.Success )
					    {
						    StringBuilder sb = new StringBuilder();
						    for ( int j = 1; j <= m.Groups.Count; j++ )
						    {
							    sb.AppendFormat( " {0}\r\n", m.Groups[j].Value );
						    }

						    if ( sb != null )
						    {
							    this.Message += sb.ToString();
						    }
						    else
						    {
							    this.Message += LogMessage;
						    }
					    }
				    }
			    }
		    }
		    base.Execute();
	    }

	    private string GetMessage()
	    {
		    XmlNode Message = this._ActionNode.SelectSingleNode( @"./message" );
		    return Message.Value;
	    }
    }
}
