using System;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for URLAttribute.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public class URLAttribute : Attribute {
        private string _Name     = string.Empty;
        private string _Default  = string.Empty;
        private bool   _Required = true;

        public URLAttribute( string Name ) {
            this._Name     = Name;
        }


        public URLAttribute( string Name, string Default ) {
            this._Name     = Name;
            this._Required = false;
            this._Default  = Default;
        }


        public URLAttribute( string Name, bool Required ) {
            this._Name     = Name;
            this._Required = Required;
        }


        public URLAttribute( string Name, bool Required, string Default ) {
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


        public string Default
        {
            get {
                return this._Default;
            }
            set {
                this._Default = value;
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

    }
}
