using System;
using System.Collections;
using System.Xml;

namespace XInstall.Core {

    /// <summary>
    /// A collection class for ActionPackage object.  This object
    /// inherits CollectionBase that provided a strong typed object
    /// collection.
    /// </summary>
    public class ActionPackageCollection : CollectionBase {
#region private variables
        private ActionPackage   _ActionPackage    = null;
        private enum PACKAGE_COLLECTION_OPR_CODE {
            PACKAGE_ADDED_SUCCESSFULLY = 0,
            PACKAGE_INDEX_OUTOF_RANGE,
            PACKAGE_ACTIONPACKAGE_NOTFOUND,
        }
        private PACKAGE_COLLECTION_OPR_CODE _enumPkgCollOprCode = PACKAGE_COLLECTION_OPR_CODE.PACKAGE_ADDED_SUCCESSFULLY;
        private string _strExitMessage                          = null;
        private string[] _strMessages  = {
                "{0}: package {1} loaded successfully",
                "{0}: index out of range for accessing package located at {1}",
                "{0}: specified action package {1} cannot be found!",
        };
#endregion

        public ActionPackageCollection( string strDll2Load ) {
            this._ActionPackage = new ActionPackage( strDll2Load );
        }


        public ActionPackageCollection() {}


        public int Add( ActionPackage apActionPackage ) {
            return base.List.Add( apActionPackage );
        }


        public int Add( XmlNode xnActionPackage ) {
            ActionPackage ap = new ActionPackage( xnActionPackage );
            return base.List.Add( ap );
        }

        public int Add( XmlNode ActionPackage, string DllPath ) {
            ActionPackage ap = new ActionPackage( ActionPackage, DllPath );
            return base.List.Add( ap );
        }


        public void Remove( ActionPackage apActionPackage ) {
            base.List.Remove( apActionPackage );
        }


        public new int Count
        {
            get { return base.List.Count; }
        }


        public ActionElement this[ int iActionPackageIdx ]
        {
            get {
                if ( iActionPackageIdx < 0 || iActionPackageIdx > base.List.Count ) {
                    this._enumPkgCollOprCode = PACKAGE_COLLECTION_OPR_CODE.PACKAGE_INDEX_OUTOF_RANGE;
                    this._strExitMessage     = String.Format( this._strMessages[ this.ExitCode ], this.Name, iActionPackageIdx );
                }

                ActionElement ActionObject = List[ iActionPackageIdx ] as ActionElement;
                return ActionObject;
            }
        }


        public string Name
        {
            get { return this.GetType().Name; }
        }


        public string ExitMessage
        {
            get {
                return this._strExitMessage;
            }
        }


        public int ExitCode
        {
            get { return (int) this._enumPkgCollOprCode; }
        }


    }
}
