using System;
using System.IO;
using System.Xml;

using XInstall.Util;
using XInstall.Util.Log;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// Summary description for MakeDir.
    /// </summary>
    public class MakeDir : ActionElement
    {
	    private string _DirectoryName = String.Empty;

	    [Action("mkdir")]
	    public MakeDir( XmlNode ActionNode ) : base( ActionNode ) {}


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


	    public new string Name
	    {
		    get
		    {
			    return this.GetType().Name;
		    }
	    }


	    [Action("dirname", Needed=true)]
	    public string DirectoryName
	    {
		    get
		    {
			    return this._DirectoryName;
		    }
		    set
		    {
			    this._DirectoryName = value;
			    if (this._DirectoryName.Length > 255 )
			    {
				    throw new PathTooLongException(
					String.Format( "Path {0} is too long", this._DirectoryName ) );
			    }
		    }
	    }


	    protected override void ParseActionElement()
	    {
		    base.ParseActionElement();

		    try
		    {
			    if ( Directory.Exists( this.DirectoryName ) )
			    {
				    base.LogItWithTimeStamp( string.Format("{0}: Directory {1} is already existed!",
									   this.Name, this.DirectoryName) );
			    }
			    else
			    {
				    Directory.CreateDirectory( this.DirectoryName );
				    base.LogItWithTimeStamp(
					String.Format( "{0}: Directory {1} is created",
						       this.Name, this.DirectoryName ) );
			    }
		    }
		    catch ( Exception e )
		    {
			    base.FatalErrorMessage(
				".", String.Format( "{0}: unable to create directory {1}, reason {2}",
						    this.Name, this.DirectoryName, e.Message ), 1660 );
			    throw;
		    }
		    base.IsComplete = true;
	    }
    }
}
