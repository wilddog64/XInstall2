using System;
using System.Collections;

using XInstall.Util;
namespace XInstall.Util {
    /// <summary>
    /// Summary description for ErrorCollection.
    /// </summary>
    public class ErrorCollection : CollectionBase, IEnumerator {

        private int _CurrentIndex = -1;

#region constroctor
        public ErrorCollection() : base() {}
#endregion

#region public methods/properties
        public int Add( Error AnError ) {
            return this.List.Add( AnError );
        }


        public void Remove( Error AnError ) {
            this.List.Remove( AnError );
        }


        public bool Contains( Error AnError ) {
            return this.List.Contains( AnError );
        }


        public Error this[ int Index ]
        {
            get { return (Error) this.List[ Index ]; }
        }


#endregion

#region overrided event handlers
        protected override void OnValidate(object value) {
            if ( !( value is Error ) )
                throw new ArgumentException( String.Format("invalid object type {0}, this collection only accept Error object", value.GetType().ToString()) );
        }
#endregion

#region IEnumerator Members

        public void Reset() {
            this._CurrentIndex = -1;
        }


        public object Current
        {
            get { return this.List[ this._CurrentIndex ]; }
        }


        public bool MoveNext() {
            // TODO:  Add ErrorCollection.MoveNext implementation
            if ( ++this._CurrentIndex < this.List.Count )
                return true;
            else
                return false;
        }

#endregion
    }
}
