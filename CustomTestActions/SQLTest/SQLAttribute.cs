using System;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for SQLAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class    |
                    AttributeTargets.Property)]
    public class SQLAttribute : Attribute {
        private string _Name     = String.Empty;
        private bool   _Required = false;
        private string _Default  = null;

        public SQLAttribute( string Name ) {
            this._Name     = Name;
            this._Required = true;
        }


        public SQLAttribute( string Name, string Default ) {
            this._Name     = Name;
            this._Default  = Default;
            this._Required = false;
        }


        public SQLAttribute( string Name, bool Required, string Default ) {
            this._Name     = Name;
            this._Required = Required;
            this._Default  = Default;
        }


        public string Name
        {
            get {
                return this._Name;
            }
            set {
                this._Name = value;
            }
        }


        public bool Required
        {
            get {
                return this._Required;
            }
            set {
                this._Required = value;
            }
        }


        public string Default
        {
            get {
                return this._Default;
            }
            set {
                this._Default = value;
            }
        }
    }
}
