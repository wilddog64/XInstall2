using System;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for URLAttribute.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public class PerfMonAttribute : Attribute {
        private string _Name     = string.Empty;
        private string _Default  = string.Empty;
        private bool   _Required = true;

        public PerfMonAttribute( string Name ) {
            this._Name     = Name;
        }


        public PerfMonAttribute( string Name, string Default ) {
            this._Name     = Name;
            this._Required = false;
            this._Default  = Default;
        }


        public PerfMonAttribute( string Name, bool Required ) {
            this._Name     = Name;
            this._Required = Required;
        }


        public PerfMonAttribute( string Name, bool Required, string Default ) {
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
