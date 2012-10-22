using System;
using System.Collections;
using System.Xml;
using System.Reflection;

namespace XInstall.Core {
    /// <summary>
    /// Summary description for ActionNodeCollection.
    /// </summary>
    public class ActionNodeCollection : ActionLoader, IEnumerator {
        private ArrayList _alActionNodeList      = null;

        private int       _iActionNodeIdx        = 0;
        private int       _iCurrentActionNodeIdx = -1;

        /// <summary>
        /// public ActionNodeCollection( string strActionNodeAssembly ) -
        /// accepts the name of an assembly file and loaded into
        /// memory for looking up particular classes
        /// </summary>
        /// <param name="strActionNodeAssembly">name of the assembly being loaded</param>
        public ActionNodeCollection( string strActionNodeAssembly ) : base ( strActionNodeAssembly ) {
            this._alActionNodeList = new ArrayList();
        }


        public ActionNodeCollection( XmlNode ActionNode ) : base ( ActionNode ) {
            this._alActionNodeList = new ArrayList();
        }


        // public ActionNodeCollection( XmlNode ActionNode ) {}

        /// <summary>
        /// public void Add( XmlNode xnActionNode ) -
        ///     add an Xml action node into our ActionNodeCollection
        /// </summary>
        /// <param name="xnActionNode"></param>
        /// <returns>returns the object that is successfully added to the collection</returns>
        public object Add( XmlNode xnActionNode ) {
            // create object by calling base class's CreateObject method
            // and if object is successfully created, add it to our collection
            // and increment the index.

            object objConstructor = base.CreateObject( xnActionNode );
            if ( objConstructor != null ) {
                this._iActionNodeIdx++;
                this._alActionNodeList.Add( objConstructor );
            }

            return objConstructor;
        }


        /// <summary>
        /// property Count -
        ///     gets the number of object in our collection
        /// </summary>
        public int Count
        {
            get { return this._iActionNodeIdx; }
        }


        /// <summary>
        /// property IAction this[ int iActionIdx ] -
        ///     get the current object in the collection.
        ///     this is an indexer method.
        /// </summary>
        public ActionElement this[ int iActionIdx ]
        {
            get {
                // range checking: when bad index encounter,
                // throw an IndexOutOfRangeException exception
                if ( iActionIdx < 0 || iActionIdx > this._iActionNodeIdx )
                    throw new IndexOutOfRangeException( String.Format("{0}: index {1} is out of range",
                                      this.GetType().Name, iActionIdx));
                return this._alActionNodeList[ iActionIdx ] as ActionElement;

            }
        }


        public ActionElement this[ object Obj ]
        {
            get {
                int ObjIndex = this._alActionNodeList.IndexOf( Obj );
                if ( ObjIndex > -1 )
                    return (ActionElement) this._alActionNodeList[ ObjIndex ];
                return null;
            }
        }


        public new Hashtable ActionObjectTable
        {
            get { return base.ActionObjectTable; }
        }


#region IEnumerator Members

        /// <summary>
        /// public void Reset() -
        ///     resets the index to -1
        /// </summary>
        public void Reset() {
            this._iCurrentActionNodeIdx = -1;
        }

        /// <summary>
        /// public object Current -
        ///     gets the current object in the list
        /// </summary>
        public object Current
        {
            get {
                // range checking, throw an exception if
                // index is out of range (underflow or overflow)
                if ( this._iCurrentActionNodeIdx < 0 || this._iCurrentActionNodeIdx > this._iActionNodeIdx )
                    throw new IndexOutOfRangeException(
                        "index is out of range!");

                // return the current object
                return
                    this._alActionNodeList[ this._iCurrentActionNodeIdx ];
            }
        }

        /// <summary>
        /// public bool MoveNext() -
        ///     increments the index and move to next object
        /// </summary>
        /// <returns>
        /// return true when index is within range, false otherwise
        /// </returns>
        public bool MoveNext() {
            this._iCurrentActionNodeIdx++;
            return this._iCurrentActionNodeIdx >= this._iActionNodeIdx ? false : true;
        }

        public int LoadStatus
        {
            get { return base.ExitCode; }
        }

        public string Message
        {
            get { return base.ExitMessage; }
        }

#endregion
    }
}
