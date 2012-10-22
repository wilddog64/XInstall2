using System;

namespace XInstall.Core {
    /// <summary>
    /// ActionAttribute - an attribute class that can
    /// be used to apply to each action class.  The attribute
    /// can be applied to following object:
    ///
    ///     Constructors, Properties, and methods
    ///
    /// </summary>
    [AttributeUsage( AttributeTargets.Constructor |
                     AttributeTargets.Property)]
    public class ActionAttribute : Attribute {
#region private member variables
        private string _strName          = null;
        private bool   _bNeeded          = true;
        private Object _objDefault       = null;

#endregion

        /// <summary>
        /// ActionAttribute - a constructor that accepts
        ///                   one parameter:
        /// </summary>
        /// <param name="strName">Name of the attribute</param>
        /// <param name="bNeeded">flag indicates if the attribute is required</param>
        public ActionAttribute(string strName, bool bNeeded) {
            this._strName = strName;
            this._bNeeded = bNeeded;
        }

        /// <summary>
        /// public ActionAttribute( string strName, bool bNeeded, object objDefault ) -
        ///     an overloaded constructor that takes three parameters:
        ///     strName, bNeeded, objDefault, and strChildNodeName
        /// </summary>
        /// <param name="strName">name of the attribute</param>
        /// <param name="bNeeded">flag indicate that a given attribute is rquired</param>
        /// <param name="objDefault">a default value for the attribute</param>
        public ActionAttribute(string strName,
                               bool   bNeeded,
                               object objDefault) {
            this._strName    = strName;
            this._bNeeded    = bNeeded;
            this._objDefault = objDefault;
        }

        /// <summary>
        /// public ActionAttribute( string strName ) -
        ///     an overloaded constructor that takes only one parameter,
        ///     strName.
        /// </summary>
        /// <param name="strName">name of the attribute</param>
        public ActionAttribute(string strName) {
            this._strName = strName;
        }


        /// <summary>
        /// property Name -
        ///     get/set the name of an attribute
        /// </summary>
        public string Name {
            get { return this._strName; }
            set { this._strName = value; }
        }

        /// <summary>
        /// property Needed -
        ///     get/set a boolean property that
        ///     indicates whether this
        ///     attribute is required or
        ///     not
        /// </summary>
        public bool Needed {
            get { return this._bNeeded; }
            set { this._bNeeded = value; }
        }

        /// <summary>
        /// property Default -
        ///     get/set a default value for the given
        ///     attribute
        /// </summary>
        public Object Default {
            get { return this._objDefault; }
            set { this._objDefault = value; }
        }
    }
}
