using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Xml;

using XInstall.Util.Log;

// using XInstall.Core;
/*
 * Class Name    : ActionLoader
 * Inherient     : ActionElement
 * Functionality : Load object library and classes dynamically
 *
 * Created Date  : May 2003
 * Created By    : mliang
 *
 * Change Log    :
 *
 * who              when            what
 * --------------------------------------------------------------
 * mliang           05/01/2003      initial creation
 * mliang           01/27/2005      Added statement to
 *                                  determined if an object
 *                                  is a class or not in method
 *                                  InitialContructors (line 458)
 * mliang           01/31/2005      Append timestamp to the current_date
 */
namespace XInstall.Core {
	/// <summary>
	/// public class ClassLoader -
	///     Load class and instaniciate it when
	///     necessary.  Also use attrbiute to verify
	///     if a given property does need to be present
	///     as an Xml node attribute and set the value
	///     accordingly.
	/// </summary>
	public class ActionLoader : ActionElement {

		// Private variables.
		private Assembly _assLoadedAssembly = null;  // loaded assembly (.dll file)


		// a private enumration for the internal
		// return code
		private enum ACTIONLOADER_OPERATION_CODE
		{
		   OPR_SUCCESS = 0,
		   OPR_SPECIFY_ACTION_NOTFOUND,
		   OPR_CANNOT_FIND_MEMBER,
		   OPR_MEMBER_REQUIRED,
		   OPR_CONSTRUCTOR_NAME_NEEDED,
		   OPR_MEMBER_NEEDED_VALUE,
		   OPR_ASSEMBLY_FILE_NOTFOUND,
		   OPR_ASSEMBLY_NULL_FILENAME,
		   OPR_ASSEMBLY_SECURITY_ERROR,
		   OPR_ASSEMBLY_MEMBER_ISNOT_DECLARED,
		   OPR_ASSEMBLY_PARAMS_ISNULL,
		   OPR_ASSEMBLY_TARGET_MEMBER_EXCEPTION,
		   OPR_ASSEMBLY_TARGET_PARAMS_COUNT_NOTMATCH,
		   OPR_ASSEMBLY_TARGET_PARAMS_TYPE_NOTMATCH,
		   OPR_ASSEMBLY_TARGET_METHOD_INVOKE_DENY,
		   OPR_ASSEMBLY_TARGET_TYPE_MODIFIER_NOTMATCH,
		   OPR_ASSEMBLY_TARGET_ACTION_NOTFOUND,
		   OPR_ASSEMBLY_TARGET_ACTION_PROPERTY_NOTFOUND,
		}


		private ACTIONLOADER_OPERATION_CODE _cocOperationCode =
		   ACTIONLOADER_OPERATION_CODE.OPR_SUCCESS;

		// error messages table
		private string[] _strMessages =
		   {
		      @"{0}: successfully loaded assembly {1}",
		      @"{0}: given action {1} does not have a coresponding class defined, exit code {2}",
		      @"{0}: unable to find a given constructor's member {1}, exit code {2}",
		      @"{0}: member {1} is a required but you didn't provide a value, exit code {2}",
		      @"{0}: constructor name is not provided!, exit code",
		      @"{0}: member {1} does not have a value associate with it and no default value is set",
		      @"{0}: specified assembly {1} cannot be found!",
		      @"{0}: assembly file to be loaded cannot be a null value",
		      @"{0}: user {1} does not have sufficient permissions to load this assembly {2}, exit code {3}",
		      @"{0}: member {1} of action {2} is not defined, exit code {3}",
		      @"{0}: argument is null reference and invoked method {1} of {2} is not static, exit code {2}",
		      @"{0}: target member {1} or constructor {2} generates an exception, exit code {3}",
		      @"{0}: target constructor {1}'s member {2} parameters not matched, exit code {3}",
		      @"{0}: parameter type is not matched with target member {1}'s parameter type, exit code {2}",
		      @"{0}: caller {1} does not have sufficient permission to invoke the method {2}, exit code {3}",
		      @"{0}: target and modifier does not match, exit code {2}",
		      @"{0}: specified action {1} cannot be found, exit code {2}",
		      @"{0}: specified property {1} of action {2} cannot be found, exit code {3}",
		   };

		private string _strExitMessage = null;
		// private AppDomain _AppDomain   = null;

		// a private hashtable to store loaded classes
		private Hashtable _htTypeLookupTable = new Hashtable();

		/// <summary>
		/// public ActionLoader() -
		///     Initialize the ClassLoader object and
		///     loads desired assembly into memory and
		///     build a lookup table for types in it.
		/// </summary>
		public ActionLoader( XmlNode ActionNode ) : base( ActionNode ) {
			this.CreateAppDomain();
		}


		/// <summary>
		/// public ClassLoader( string strAssemblyName ) -
		///     an overloaded constructor that accepts a
		///     name of assembly to be loaded.
		/// </summary>
		/// <param name="strAssemblyName">
		/// name of an assembly being loaded
		/// </param>
		public ActionLoader ( string strAssemblyName ) {
			string strAssembly2Load = null;
			// this.CreateAppDomain();
			if ( strAssemblyName == null )
				strAssembly2Load = Environment.CurrentDirectory + Path.DirectorySeparatorChar  + "XInstall.Core.Actions.dll";
			else
				strAssembly2Load  = strAssemblyName;
				this.LoadAssembly = strAssembly2Load;
			this.Init();
		}
		
		/// <summary>
		/// property Name -
		///     gets the class name of the object.
		/// </summary>
		public new string Name {
		   get { return this.GetType().Name; }
		}


		/// <summary>
		/// property LoadAssembly -
		///     sets the assembly file to be loaded
		/// </summary>
		public string LoadAssembly {
		   set {
			   string strAssemblyFile    = value;
			   AssemblyName assemblyName = new AssemblyName();
			   assemblyName.CodeBase     = String.Format("file://{0}", strAssemblyFile);
			   try {
				   this._assLoadedAssembly = Assembly.LoadFile( value );
				   // this._assLoadedAssembly = this._AppDomain.Load( assemblyName );
				   this.BuildConstructorLookupTable();
			   } 
				 catch ( ArgumentNullException ) {
				   this._cocOperationCode = ACTIONLOADER_OPERATION_CODE.OPR_ASSEMBLY_NULL_FILENAME;
				   this._strExitMessage   = String.Format( this._strMessages[ (int) this._cocOperationCode ], this.Name, this.ExitCode );
				   base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
			   } 
				 catch ( FileNotFoundException ) {
				   this._cocOperationCode = ACTIONLOADER_OPERATION_CODE.OPR_ASSEMBLY_FILE_NOTFOUND;
				   this._strExitMessage = String.Format( this._strMessages[ (int) this._cocOperationCode ], this.Name, strAssemblyFile, this.ExitCode );
				   base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
			   } 
				 catch ( System.Security.SecurityException ) {
				   this._cocOperationCode = ACTIONLOADER_OPERATION_CODE.OPR_ASSEMBLY_SECURITY_ERROR;
				   this._strExitMessage = String.Format( this._strMessages[ (int) this._cocOperationCode ], this.Name, Environment.UserDomainName, strAssemblyFile, this.ExitCode );
				   base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
			   }
			   this._cocOperationCode = ACTIONLOADER_OPERATION_CODE.OPR_SUCCESS;
			   this._strExitMessage   = String.Format( this._strMessages[ (int) this._cocOperationCode ], this.Name, strAssemblyFile );
			   base.LogItWithTimeStamp( this._strExitMessage );
		   }
		}


		/// <summary>
		/// property ExitCode -
		///     gets an exitcode from the ClassLoader object
		/// </summary>
		public new int ExitCode {
		   get { return (int) this._cocOperationCode; }
		}


		/// <summary>
		/// property ExitMessage -
		///     gets the error message from the object
		/// </summary>
		public new string ExitMessage {
		   get { return this._strExitMessage; }
		}


		/// <summary>
		/// public object CreateObject( XmlNode xnActionNode ) -
		///     instaniciate an object by using an XmlNode passes in and
		///     return it.
		/// </summary>
		/// <param name="xnActionNode">an xml node</param>
		/// <returns>
		/// the instance of an object when successfully find it;
		/// otherwise, null is returned.
		/// </returns>
		public object CreateObject( XmlNode xnActionNode ) {

			// required specific role to execute this method
      // System.Security.Permissions.PrincipalPermission Perm =
      // new System.Security.Permissions.PrincipalPermission(
      // null, @"180Solutions\All Operations" );

			object ObjInstance = null;
			try {
				// Demand a request secuirty principle
        // Perm.Demand();

				// perform an object lookup and instaniciation
				ObjInstance = base.CreateObject( xnActionNode.Name, xnActionNode, this.ActionObjectTable );
			} 
			catch ( System.Security.SecurityException  se ) {
				base.SetExitMessage( "{0} : {1} - you are not allow to execute this method - reason {2}!", this.Name, "CreateObject", se.Message );
				base.FatalErrorMessage( ".", base.ExitMessage, 1660 );
			}

			return ObjInstance;
		}


		/// <summary>
		/// property ActionObjectList -
		///     gets a list of all available action objects
		/// </summary>
		public Hashtable ActionObjectTable {
		   get { return this._htTypeLookupTable; }
		}


		/// <summary>
		/// private Hashtable LookForProperties( ConstructorInfo ci ) -
		///     an overloaded method to Lookup the properties in a
		///     given constructor.  This method does not invoke any
		///     property method at all.  All it does it to get the
		///     properties and their related attributes and then
		///     add the attributes values into a hash table.
		/// </summary>
		/// <param name="ci">
		/// ConstructorInfo object that contains the Action object
		/// </param>
		/// <returns>
		/// a hash table that has all the attributes related to a
		/// given constructor
		/// </returns>
		private Hashtable LookForProperties( ConstructorInfo ci, string strTypeName ) {
			Hashtable htCtorInfoTable = new Hashtable();
			Hashtable htPropsTable    = new Hashtable();
			object[] objParams        = new object[0];
			object objCtor = ci.Invoke( objParams );

			if ( objCtor != null ) {
				// get type and properties of a give constructor
				Type tThisType             = objCtor.GetType();
				PropertyInfo[] piPropInfos = tThisType.GetProperties();
				if ( piPropInfos != null ) {
					// now go through each property and retrieve
					// their attributes. Then add the attribute
					// into a hash table.
					for ( int i = 0; i < piPropInfos.Length; i++ ) {
						PropertyInfo pi        = piPropInfos[i];
						object[] objAttributes = pi.GetCustomAttributes(false);
						if ( objAttributes != null ) {
							for ( int j = 0; j < objAttributes.Length; j++ ) {
								ActionAttribute aa = objAttributes[j] as ActionAttribute;
								if ( aa != null )
									htPropsTable.Add( aa.Name, aa.Needed );
							}
						}
					}
					htCtorInfoTable.Add( strTypeName, htPropsTable );
				}
			}

			// return the hash table back to the caller.
			return htCtorInfoTable;
		}


		/// <summary>
		/// private Init() -
		///     Initlaize the ClassLoader object. It will loaded
		///     the desired assembly file and build a type lookup
		///     table for looking particular object and instaniate
		///     it.
		/// </summary>
		private void Init() {
			// build Constructor Lookup table
			this.BuildConstructorLookupTable();
		}


		/// <summary>
		/// private void BuildConstructorLookupTable() -
		///     build up an constructor Hashtable for later
		///     use.  It also instaniate the object.
		/// </summary>
		private void BuildConstructorLookupTable() {
			// initialize variables

			Type[] LoadTypes = this._assLoadedAssembly.GetTypes();
			Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
			// SearchConstructors( LoadTypes );
			this.InitialConstructors( LoadTypes );

			// LoadTypes = CurrentAssembly.GetTypes();
			Module[] Modules = CurrentAssembly.GetLoadedModules();
			LoadTypes        = Modules[0].GetTypes();
			// SearchConstructors( LoadTypes );
			this.InitialConstructors( LoadTypes );
		}

		private bool SearchConstructors( Type[] Types ) {
			ConstructorInfo ciConstructorInfo = null;
			object[] objAttributes            = null;
			ActionAttribute aa                = null;

			Type t     = null;
			bool Found = false;
			try {
				// get types from loaded assembly and go
				// through each one of them.
				try {
					for ( int iTypeIdx = 0; iTypeIdx < Types.Length; iTypeIdx++ ) {
						// retrieve class information by calling Type's GetConstructor methods. As we
						// are looking for the constructor that don't need parameters, Type.EmptyTypes
						// enumration is used.
						t  = Types[iTypeIdx];
						if ( t.BaseType == null )
							continue;
						if ( !t.BaseType.IsClass )
							continue;

						ciConstructorInfo = t.GetConstructor( BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Standard, Type.EmptyTypes, null);

						// if a particular class is found we invoke its constructor and add it to our hash table
						object[] objs = null;
						if ( ciConstructorInfo != null ) {
							objs = new object[2];
							objAttributes = ciConstructorInfo.GetCustomAttributes(false);
							if ( objAttributes.Length > 0 ) {
								aa      = (ActionAttribute) objAttributes[0];
								objs[0] = ciConstructorInfo;
								objs[1] = null;
								Found   = true;
							}
						} 
            else {
							Type[] tTypes = new Type[1]{ typeof( XmlNode ) };
							ciConstructorInfo = t.GetConstructor( BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Standard, tTypes, null );
							if ( ciConstructorInfo != null ) {
								objAttributes = ciConstructorInfo.GetCustomAttributes(false);
								if ( objAttributes.Length > 0 ) {
									aa = objAttributes[0] as ActionAttribute;
									if ( aa.Name.Equals("base") )
										continue;
									else {
										objs    = new object[2];
										aa      = (ActionAttribute) objAttributes[0];
										objs[0] = ciConstructorInfo;
										objs[1] = "true";
										Found   = true;
									}
								}
							} 
              else continue;
						}
						if ( aa != null && !this._htTypeLookupTable.Contains( aa.Name ) )
							this._htTypeLookupTable.Add( aa.Name, objs );
					}
				} 
        catch ( Exception e ) {
					base.FatalErrorMessage( ".", e.Message, 1660, false );
				}
			} 
      catch ( ArgumentNullException ) {
				this._cocOperationCode = ACTIONLOADER_OPERATION_CODE.OPR_CONSTRUCTOR_NAME_NEEDED;
				this._strExitMessage   = String.Format( this._strMessages[ (int) this._cocOperationCode ], this.Name, this.ExitCode);
				base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
			} 
      catch ( ArgumentException ) {
				this._cocOperationCode = ACTIONLOADER_OPERATION_CODE.OPR_ASSEMBLY_TARGET_TYPE_MODIFIER_NOTMATCH;
				this._strExitMessage   = String.Format( this._strMessages[ (int) this._cocOperationCode ], this.Name, this.ExitCode);
				base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
			} 
      catch ( Exception e ) {
				throw e;
			}

			return Found;
		}

		private void InitialConstructors( Type[] Types ) {
			ConstructorInfo Ctor = null;
			object[] Attributes  = null;
			ActionAttribute aa   = null;
			bool Found           = false;

			Type t = null;
			try {
				for ( int i = 0; i < Types.Length; i++ ) {
					t = Types[i];
					if ( !t.IsClass )
						continue;
					object[] objs = null;

					// get constructor without any parameter
					Ctor = t.GetConstructor( BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Standard, Type.EmptyTypes, null);

					if ( Ctor != null ) {
						Attributes = Ctor.GetCustomAttributes( typeof(ActionAttribute), false );
						if ( Attributes.Length > 0 ) {
							aa    = (ActionAttribute) Attributes[0];
							Found = true;
							objs  = new object[] { Ctor };
							goto AddObject;
						}
					}

					// get constructor with one parameter, XmlNode
					Type[] MyTypes = new Type[1] { typeof( XmlNode ) };
					Ctor           = t.GetConstructor( BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Standard, MyTypes, null );
					if ( Ctor != null ) {
						Attributes = Ctor.GetCustomAttributes( typeof(ActionAttribute), false );
						if ( Attributes.Length > 0 ) {
							aa    = (ActionAttribute) Attributes[0];
							Found = true;
							objs  = new object[] { Ctor, typeof( XmlNode ) };
							goto AddObject;
						}
					}

					// get constructor with 2 parameters: XmlNode and Hashtable
					MyTypes = new Type[2] { typeof( XmlNode ), typeof( Hashtable ) };
					Ctor = t.GetConstructor( BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Standard, MyTypes, null );
					if ( Ctor != null ) {
						Attributes = Ctor.GetCustomAttributes( typeof(ActionAttribute), false );
						if ( Attributes.Length > 0 ) {
							aa    = (ActionAttribute) Attributes[0];
							Found = true;
							objs  = new object[] { Ctor, typeof( XmlNode ), typeof( Hashtable ) };
							goto AddObject;
						}
					}

AddObject:
					if ( Found && !this.ActionObjectTable.ContainsKey( aa.Name ) ) {
						this.ActionObjectTable.Add( aa.Name, objs );
						Found = false;
					}
				}
			} 
      catch {}
		}


	private void SetExitMessage (
	   ACTIONLOADER_OPERATION_CODE cocOprCode,
	   params object[] objParams ) {
			this._cocOperationCode = cocOprCode;
			this._strExitMessage   = String.Format( this._strMessages[ this.ExitCode ], objParams );
		}


		private void CreateAppDomain() {
			// AppDomainSetup AppInfo  = new AppDomainSetup();
			// AppInfo.ApplicationBase = Environment.CurrentDirectory;
			// this._AppDomain         = AppDomain.CreateDomain("XInstall2", null, AppInfo);
			base.LogItWithTimeStamp( String.Format( "{0}: {1}", this.Name, "AppDomain Created" ) );
			this.InitGlobalVariables();
			base.LogItWithTimeStamp( String.Format( "{0}: {1}", this.Name, "Global Variables Initialized" ) );
		}


		private void InitGlobalVariables() {
			// Initialize some default variables
			if ( !ActionVariables.IsVariableExist( "current_date" ) )
				ActionVariables.Add( "current_date", DateTime.Now.ToString( "MM.dd.yy_HHmmss" ) );

			if ( !ActionVariables.IsVariableExist( "friendly_current_date" ) )
				ActionVariables.Add( "friendly_current_date", DateTime.Now.ToString( "MM/dd/yy HH:mm:ss" ) );

			if ( !ActionVariables.IsVariableExist( "computer_name" ) )
				ActionVariables.Add( "computer_name", Environment.GetEnvironmentVariable( "COMPUTERNAME" ) );

			string LogDirectory = Environment.GetEnvironmentVariable( "temp" );
			if ( !ActionVariables.IsVariableExist( "log_directory" ) ) ActionVariables.Add( "log_directory", LogDirectory );

			if ( !ActionVariables.IsVariableExist( "current_unc_log_directory" ) ) {
				LogDirectory = LogDirectory.Replace( @"\", @"/" );
                LogDirectory = LogDirectory.Replace( @":", @"$" );
				string ComputerName = ActionVariables.GetValue( "${computer_name}" );
				string Current_UNC_Log_Directory = String.Format( "file://{0}/{1}", ComputerName, LogDirectory );
				ActionVariables.Add( "current_unc_log_directory", Current_UNC_Log_Directory );
			}

			if ( !ActionVariables.IsVariableExist( "user_domain" ) )
				ActionVariables.Add( "user_domain", Environment.GetEnvironmentVariable( "USERDOMAIN" ) );

			if ( !ActionVariables.IsVariableExist( "logon_user" ) )
				ActionVariables.Add( "logon_user", Environment.GetEnvironmentVariable( "USERNAME" ) );

			if ( !ActionVariables.IsVariableExist( "user_dns_domain" ) )
				ActionVariables.Add( "user_dns_domain", Environment.GetEnvironmentVariable( "USERDNSDOMAIN" ) );

			if ( !ActionVariables.IsVariableExist( "windir" ) )
				ActionVariables.Add( "windir", Environment.GetEnvironmentVariable( "WINDIR" ) );

			if ( !ActionVariables.IsVariableExist( "System32" ) )
				ActionVariables.Add( "System32", Environment.GetEnvironmentVariable( "WINDIR" ) + System.IO.Path.DirectorySeparatorChar + "System32", true );

			if ( !ActionVariables.IsVariableExist( "temp" ) )
				ActionVariables.Add( "temp", Environment.GetEnvironmentVariable( "TEMP" ) );

			if ( !ActionVariables.IsVariableExist( "program_files" ) )
				ActionVariables.Add( "program_files", Environment.GetEnvironmentVariable( "ProgramFiles" ), true );

			if ( !ActionVariables.IsVariableExist( "appdata" ) )
				ActionVariables.Add( "appdata", Environment.GetEnvironmentVariable( "AppData" ), true );
		}
	}
}
