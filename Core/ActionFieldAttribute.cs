using System;

namespace XInstall.Core {
    /// <summary>
    /// Summary description for ActionFieldAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ActionFieldAttribute : Attribute {
        private string _Name = string.Empty;
        public ActionFieldAttribute(string Name) {
            this._Name = Name;
        }

        public string Name {
            get { return this._Name; }
            set { this._Name = value; }
        }
    }
}
