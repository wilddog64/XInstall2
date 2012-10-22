using System;
using System.IO;
using System.Xml;

using XInstall.Util;
using XInstall.Util.Log;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// Summary description for Rmdir.
    /// </summary>

    public class Rmdir : ActionElement
    {
	    private string _DirectoryName = String.Empty;

	    [Action("rmdir")]
	    public Rmdir( XmlNode ActionNode ) : base( ActionNode )
	    {
		    //
		    // TODO: Add constructor logic here
		    //
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
		    }
	    }


	    protected override void ParseActionElement()
	    {
		    base.ParseActionElement();

		    if ( Directory.Exists( this.DirectoryName ) )
		    {
			    Directory.Delete( this.DirectoryName, true );
			    base.LogItWithTimeStamp( string.Format("{0}: Directory {1} is removed!",
								   this.Name, this.DirectoryName) );

		    }
		    else
		    {
			    base.FatalErrorMessage( ".",
						    String.Format( "{0}: Directory {1} does not exist!!",
								   this.Name, this.DirectoryName ), 1660 );
		    }

		    base.IsComplete = true;
	    }
    }
}
