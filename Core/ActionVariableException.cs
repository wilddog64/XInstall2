using System;

namespace XInstall.Core {
    /// <summary>
    /// Summary description for ActionVariableExceptioni.
    /// </summary>
    public class VariableNotDefinedException : ApplicationException {
        private string _VariableName;

        public VariableNotDefinedException() : base() {}

        public VariableNotDefinedException( string Message ) : base( Message ) {}

        public VariableNotDefinedException( string VariableName, string Message ) : base( Message ) {
            this._VariableName = VariableName;
        }


        public override string Message {
            get { return base.Message; }
        }

        public string VariableName {
            get { return this._VariableName; }
        }
    }


    public class InvalidVariableNameException : ApplicationException {
        private string _VariableName = string.Empty;

        public InvalidVariableNameException() : base() {}

        public InvalidVariableNameException( string Message ) : base( Message ) {}

        public InvalidVariableNameException( string VariableName, string Message ) : base( Message ) {
            this._VariableName = VariableName;
        }

        public override string Message {
            get { return base.Message; }
        }

        public string VariableName
        {
            get { return this._VariableName; }
        }
    }


    public class VariableExistedException : ApplicationException {
        private string _VariableName = string.Empty;

        public VariableExistedException() : base() {}

        public VariableExistedException( string Message ) : base( Message ) {}

        public VariableExistedException( string VariableName, string Message ) : base( Message ) {
            this._VariableName = VariableName;
        }

        public override string Message {
            get { return base.Message; }
        }

        public string VariableName {
            get { return this._VariableName; }
        }
    }


    public class InvalidDereferenceException : ApplicationException {
        private string _VariableName = string.Empty;

        public InvalidDereferenceException() : base() {}

        public InvalidDereferenceException( string Message ) : base( Message ) {}

        public InvalidDereferenceException( string VariableName, string Message ) : base( Message ) {
            this._VariableName = VariableName;
        }

        public override string Message {
            get { return base.Message; }
        }

        public string VariableName {
            get { return this._VariableName; }
        }
    }
}
