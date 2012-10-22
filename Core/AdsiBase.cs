using System;
using System.Collections;
using System.DirectoryServices;
using System.Xml;

using Microsoft.Win32;

namespace XInstall.Core {

public enum ADSError :
    long {
        ADS_NO_ERROR                     = 0x0000000,
        ADS_ERRORS_OCCURED               = 0x0005011,
        ADS_NOMORE_ROWS                  = 0x0005012,
        ADS_BAD_PATHNAME                 = 0x80005000,
        ADS_INVALID_DONAMIN_OBJECT       = 0x80005001,
        ADS_INVALID_USER_OBJECT          = 0x80005002,
        ADS_INVALID_COMPUTER_OBJECT      = 0x80005003,
        ADS_UNKNOWN_OBJECT               = 0x80005004,
        ADS_PROPERTY_NOT_SET             = 0x80005005,
        ADS_PROPERTY_NOT_SUPPORTED       = 0x80005006,
        ADS_PROPERTY_INVALID             = 0x80005007,
        ADS_BAD_PARAMETER                = 0x80005008,
        ADS_OBJECT_UNBOUND               = 0x80005009,
        ADS_OBJECT_PROPERTY_NOT_MODIFIED = 0x8000500A,
        ADS_OBJECT_PROPERTY_MODIFIED     = 0x8000500B,
        ADS_CANT_CONVER_DATATYPE         = 0x8000500C,
        ADS_PROPERTY_NOT_FOUND           = 0x8000500D,
        ADS_OBJECT_EXIST                 = 0x8000500E,
        ADS_SCHEMA_VIOLATION             = 0x8000500F,
        ADS_COLUMN_NOT_SET               = 0x80005010,
        ADS_INVALID_FILTER               = 0x80005014,
    }

    /// <summary>
    /// AdsiBase prvoide a genernic methods and properties to
    /// access an ADSI (Active Drectory Service Interface) API
    /// </summary>
    public class AdsiBase : ActionElement {
        private DirectoryEntry _DirectoryEntry = null;
        private ADSError       _ADSError       = ADSError.ADS_NO_ERROR;

        private string _AdsiPathInfo           = string.Empty;
        private static string  _MachineName    = String.Empty;
        private static readonly string SubKey  = @"Software\Microsoft\ADs\Providers";


        /// <summary>
        ///    A default Constructor
        /// </summary>
        public AdsiBase() {}

        /// <summary>
        /// Custructor that calls the ActionElement constructor
        /// </summary>
        public AdsiBase( XmlNode ActionNode ) : base( ActionNode ) {}


        /// <summary>
        /// A protected property that returns an ADSI protocol.
        /// This property cannot be called directly.  It is provided
        /// as a place holder to be overrided.
        /// </summary>
        protected virtual string AdsiProvider {
            get { return ""; }
        }


        /// <summary>
        /// A property that returns an ADSI SchemaClass.
        /// This property cannot be called directly.  It is provided
        /// as a place holder to be overrided.
        /// </summary>
        protected virtual string SchemaClassName {
            get { return ""; }
            set {}
        }


        /// <summary>
        /// A property that returns a remote machine for
        /// ADSI to access. This property cannot be called directly.
        /// It is provided as a place holder to be overrided.
        /// </summary>
        public virtual string MachineName {
            get { return ""; }
            set {}
        }


        /// <summary>
        /// A property that returns a path points to an
        /// ADSI object
        /// </summary>
        public virtual string AdsiPath {
            get { return ""; }
            set {}
        }


        /// <summary>
        /// A property that returns a port for
        /// accessing a given ADSI object if any.
        /// This property cannot be called directly.
        /// It is provided as a place holder to be overrided.
        /// </summary>
        public virtual string Port {
            get { return ""; }
            set {}
        }


        /// <summary>
        /// This method override a default Execute method
        /// inherits from ActionElement class and call
        /// ActionElement.Execute() from within it.  The
        /// inherited object can override it to provide
        /// a special behavior.  But the decentant object
        /// has to call the base class's Execute method for
        /// everything to work.
        /// </summary>
        public override void Execute() {
            try {
                base.Execute();
            } finally {
                this.Close();
            }
        }


        /// <summary>
        /// ParseActionElement method override the base class's
        /// ParseActionElement method to initialize the ADSI object.
        /// </summary>
        protected override void ParseActionElement() {
            base.ParseActionElement();
            this.AdsiPathInfo    = String.Format( @"{0}://{1}/{2}", this.AdsiProvider, this.MachineName, this.AdsiPath );
            this._DirectoryEntry = new DirectoryEntry( this.AdsiPathInfo );

            _MachineName = this.MachineName;
        }



        /// <summary>
        /// GetAdsiObjectPropertyValue is used to retrieve a gvien
        /// ADSI object property value and return a PropertyValueCollection
        /// back to the caller.
        /// </summary>
        /// <param name="PropertyName">a type of string that contains a property name</param>
        /// <returns>a type of PropertyValueCollection that contains a value of a given property</returns>
        protected PropertyValueCollection GetAdsiObjectPropertyValue( string PropertyName ) {
            PropertyCollection Properties = this._DirectoryEntry.Properties;
            PropertyValueCollection ReturnValues = null;
            if ( Properties.Contains( PropertyName ) )
                ReturnValues = Properties[ PropertyName ];

            return ReturnValues;
        }


        protected DirectoryEntry ADSIBaseObject {
            get { return this._DirectoryEntry; }
        }

        /// <summary>
        /// A protected property that get or set the ADSI Error code
        /// </summary>
        protected ADSError ADSIError
        {
            get { return this._ADSError; }
            set { this._ADSError = value; }
        }


        /// <summary>
        /// an overloaded GetAdsiObjectPropertyNames returns property names
        /// of a given ADSI object.  It returns property names from the
        /// initial created ADSI object
        /// </summary>
        /// <returns>a string array that contains property names</returns>
        protected string[] GetAdsiObjectPropertyNames() {
            string[] PropertyNames = this.GetAdsiObjectPorpertyNames( this.SchemaClassName );
            return PropertyNames;
        }


        /// <summary>
        /// GetAdsiObjectPropertyNames returns property names from a
        /// given schema class. It accept a string variable that contains
        /// a SchemaClassName defined for that ADSI object
        /// </summary>
        /// <param name="ThisSchemaClassName">
        /// A string type variable that contains a SchemaClassName
        /// </param>
        /// <returns>an array of property names</returns>
        protected string[] GetAdsiObjectPorpertyNames( string ThisSchemaClassName ) {
            PropertyCollection Properties = null;

            foreach ( DirectoryEntry de in this._DirectoryEntry.Children )
            if ( de.SchemaClassName.Equals( ThisSchemaClassName ) )
                Properties = de.Properties;
            else return null;

            string[] PropertyNames              = new string[ Properties.PropertyNames.Count ];
            IEnumerator PropertyNamesEnumerator = Properties.PropertyNames.GetEnumerator();
            int Count                           = 0;
            while ( PropertyNamesEnumerator.MoveNext() )
                PropertyNames[ Count++ ] = (string) PropertyNamesEnumerator.Current;

            return PropertyNames;
        }


        /// <summary>
        /// SetAdsiObjectProperty sets a given DirectoryEntry object's property
        /// </summary>
        /// <param name="EntryToSet">
        /// a type of DirectoryEntry to be set
        /// </param>
        /// <param name="Properties">
        /// a type of Hashtable that contains properties to be set
        /// </param>
        protected void SetAdsiObjectProperty( DirectoryEntry EntryToSet, Hashtable Properties ) {
            if ( EntryToSet != null ) {
                PropertyCollection PropertySet = EntryToSet.Properties;
                foreach ( DictionaryEntry Property in Properties ) {
                    string PropertyName = (string) Property.Key;
                    if ( PropertySet.Contains( (string) Property.Key ) ) {
                        PropertySet[ PropertyName ].Value = Property.Value;
                        EntryToSet.CommitChanges();
                        base.LogItWithTimeStamp( String.Format( "{0}: property {1} was added", this.ObjectName, PropertyName ) );
                    }
                }
            }
        }


        /// <summary>
        /// InvokeAdsiObjectMethod is an overloaded method that
        /// invokes a given ADSI object's Method with zero or more
        /// parameters.
        /// </summary>
        /// <param name="MethodName">
        /// A striing variable that contains a method to be invoked.
        /// </param>
        /// <param name="Parameters">
        /// an object parameter arrays that contains required paramters
        /// to be passed to method being invoked.
        /// </param>
        /// <returns>
        /// retrun an object from the invoke method
        /// </returns>
        /// <remarks>
        /// the return object has to be casted to an apporiate type.
        /// </remarks>
        protected object InvokeAdsiObjectMethod( string MethodName, params object[] Parameters ) {
            // return this._DirectoryEntry.Invoke( MethodName, Parameters );
            return this.InvokeAdsiObjectMethod( this._DirectoryEntry, MethodName, Parameters );
        }


        /// <summary>
        /// InvokeAdsiObjectMethod invokes a method from a gvien DirectoryEntry object.
        /// This method accepts 3 parameters where:
        ///    Entry      - an DirectoryEntry object
        ///    MethodName - a method to be invoked
        ///    Parameters - parameters required by a gvien method.
        /// </summary>
        /// <param name="Entry">a type of DirectoryEntry object</param>
        /// <param name="MethodName">a string type</param>
        /// <param name="Parameters">an object parameter array</param>
        /// <returns></returns>
        protected object InvokeAdsiObjectMethod( DirectoryEntry Entry, string MethodName, params object[] Parameters ) {
            if ( Entry == null )
                return null;
            return Entry.Invoke( MethodName, Parameters );
        }


        /// <summary>
        /// AddAdsiDirectoryObject adds object to a given ADSI object
        /// container.
        /// </summary>
        /// <param name="ObjectName">
        /// a string type variable that contains an object to be added.
        /// </param>
        /// <param name="Properties">
        /// a Hashtable type object that contains properties to be applied
        /// to a newly created object.
        /// </param>
        /// <returns></returns>
        protected DirectoryEntry AddAdsiDirectoryObject( string ObjectName, Hashtable Properties ) {
            DirectoryEntry NewEntry = null;

            try {
                NewEntry = this._DirectoryEntry.Children.Add( ObjectName, this.SchemaClassName );
                if ( Properties != null )
                    this.SetAdsiObjectProperty( NewEntry, Properties );
            } catch ( Exception e ) {
                throw e;
            }
            return NewEntry;
        }


        /// <summary>
        /// DeleteAdsiDirectoryObject deletes a given object from ADSI object
        /// container.
        /// </summary>
        /// <param name="ObjectName">
        /// a string type variable that contains an object to be deleted
        /// </param>
        protected void DeleteAdsiDirectoryObject( string ObjectName ) {
            DirectoryEntry Root = this._DirectoryEntry;
            DirectoryEntry EntryToDelete = null;
            if ( SchemaClassName.Length != 0 )
                EntryToDelete = Root.Children.Find( ObjectName, this.SchemaClassName );
            else
                EntryToDelete = Root.Children.Find( ObjectName );

            if ( EntryToDelete != null ) {
                Root.Children.Remove( EntryToDelete );
                Root.CommitChanges();
            }
        }


        /// <summary>
        /// Close is an overrided method that use to close
        /// all opened ADSI object instances.  You can override
        /// this method to perform your own clean-up.
        /// </summary>
        protected virtual new void Close() {
            this._DirectoryEntry.Close();
        }


        /// <summary>
        /// GetAdsiProviders returns a set of registered ADSI providers
        /// from a given machine.
        /// </summary>
        /// <returns>
        /// an array of strings that contains names of provider registered
        /// for a given machine.
        /// </returns>
        protected static string[] GetAdsiProviders() {
            RegistryKey RegKey          = RegistryKey.OpenRemoteBaseKey( RegistryHive.LocalMachine, _MachineName );
            RegistryKey ProviderRegKey  = RegKey.OpenSubKey( SubKey );
            string[] ProviderKeys       = ProviderRegKey.GetSubKeyNames();
            string[] ReturnProviderKeys = new string[ ProviderKeys.Length ];

            int Counter = 0;
            foreach ( string ProviderKey in ProviderKeys ) {
                ReturnProviderKeys[ Counter++ ] = String.Format( @"{0}://{1}", ProviderKey, _MachineName );
            }
            return ReturnProviderKeys;
        }


        /// <summary>
        /// get all the directories from a given ADSI object root
        /// </summary>
        protected DirectoryEntries Directories {
            get { return this._DirectoryEntry.Children; }
        }


        protected string AdsiPathInfo
        {
            get { return this._AdsiPathInfo; }
            set { this._AdsiPathInfo = value; }
        }


        protected void CommitChange() {
            this._DirectoryEntry.CommitChanges();
        }


        protected DirectoryEntry AddNewEntry( string EntryName, string SchemaClassName ) {
            DirectoryEntry NewEntry = this._DirectoryEntry.Children.Add( EntryName, SchemaClassName );
            this._DirectoryEntry.CommitChanges();

            return NewEntry;
        }
    }
}
