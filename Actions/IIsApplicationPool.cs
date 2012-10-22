using System;
using System.Collections;
using System.Management;
using System.Xml;
using System.Xml.XPath;

namespace XInstall.Core.Actions
{
/// <summary>
/// Summary description for IIsApplicationPool.
/// </summary>
public class IIsApplicationPools : WmiBase
{
	private XmlNode           _ActionNode = null;
	private ManagementObject  _AppPools   = null;

	private string _MachineName = String.Empty;
	private string _UserName    = string.Empty;
	private string _UserPass    = string.Empty;

	private readonly string _AppPoolPathFormat     = @"IIS://{0}/w3svc/AppPools";
	private readonly string _ManagementScopeFormat = @"\\{0}\root\MicrosoftIISV2";

	private const string EnumAppsInPool = @"EnumAppsInPool";

	#region public constructors
	[Action("iisapplicationpools")]
	public IIsApplicationPools( XmlNode ActionNode ) : base( ActionNode )
	{
		this._ActionNode = ActionNode;
	}
	#endregion

	#region public override properties
	[Action("machinename", Needed=false, Default="true")]
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


	[Action("username", Needed=false, Default="")]
	public override string UserName
	{
		get
		{
			return this._UserName;
		}
		set
		{
			this._UserName = value;
		}
	}


	[Action("userpass", Needed=false, Default="")]
	public override string UserPass
	{
		get
		{
			return this._UserPass;
		}
		set
		{
			this._UserPass = value;
		}
	}


	public override string ObjectName
	{
		get
		{
			return this.Name;
		}
	}


	public override string NamespacePath
	{
		    get
		    {
			    return String.Format( this._ManagementScopeFormat, this.MachineName );
		    }
	}

	public override string RelativePath
	{
		get
		{
			string RelativePath =
			    String.Format( "{0}='{1}'", this.Key, this.Location );
			return RelativePath;
		}

	}


	public override string Key
	{
		get
		{
			return "IIsApplicationPools";
		}

	}

	protected override string Location
	{
		get
		{
			return "W3SVC/AppPools";
		}
	}


	#endregion

	#region public properties

	[Action("runnable", Needed=false, Default="true")]
	public new string Runnable
	{
		set
		{
			base.Runnable = bool.Parse( value );
		}
	}


	public new string Name
	{
		get
		{
			return this.GetType().Name;
		}
	}


	#endregion

	#region protected properties
	protected override object ObjectInstance
	{
		get
		{
			return this;
		}
	}


	#endregion

	#region public override methods
	public override void Execute()
	{
		base.Execute ();
		this._AppPools = base.GetManagementObject();
		this.CreateAppPool( "TestApp" );
		// this.GetAllProperties();
	}

	#endregion

	#region private properties
	private string AppPoolPath
	{
		get
		{
			return String.Format( this._AppPoolPathFormat, this.MachineName );
		}
	}
	#endregion


	#region private static methods

	#endregion

	#region private methods

	private void GetAllProperties()
	{
		PropertyDataCollection Properties = this._AppPools.Properties;

		foreach ( PropertyData pd in Properties )
		{
			base.LogItWithTimeStamp( pd.Name + ": " + pd.Value );
		}
	}

	private void CreateAppPool( string AppPoolName )
	{
		object[] Params = new object[] { "IIsApplicationPool", AppPoolName };
		this._AppPools.InvokeMethod( "Create", Params );
	}
	#endregion
}
}
