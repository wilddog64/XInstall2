using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;


/*
 * Class Name    : ActionVariables
 * Inherient     : None
 * Functionality : ActionVariables is a static class that takes care of
 *                 variable adding/extracting
 *
 * Created Date  : May 2003
 * Created By    : mliang
 *
 * Change Log    :
 *
 * who              when            what
 * --------------------------------------------------------------------
 * mliang           12/15/2004      Initial creation
 * mliang           01/28/2005      Fix bug presenting in ScanVariable
 *                                  that cannot parse more than one
 *                                  variable in a given input line.
 */

namespace XInstall.Core {
    /// <summary>
    /// Summary description for ActionVariable.
    /// </summary>
    class ActionVariables {
        const int      MAX_VARIABLE_LEN = 64;

        static private StringDictionary  _Variables         = new StringDictionary();
        static private Regex             _ValueExtractor    = new Regex( @"\${(\w+)}" );
        static private Regex             _VariableValidator = new Regex( @"[a-zA-Z,0..9]+" );
        // static private

        private ActionVariables() {}

        public static void Add( string Name, string Value ) {
            Add( Name, Value, false );
        }


        public static void Add( string Name, string Value, bool Overwrite ) {
            if ( _VariableValidator.IsMatch( Name ) ) {
                if ( !_Variables.ContainsKey( Name ) )
                    _Variables.Add( Name, Value );
                else if ( !Overwrite && _Variables.ContainsKey( Name ) ) {
                    throw new VariableExistedException( Name, "variable already existed!" );
                } 
								else {
                    if ( _Variables.ContainsKey( Name ) )
                        _Variables[ Name ] = Value;
                }
            } else
                throw new InvalidVariableNameException( Name, "variable name is not valid" );
        }


        public static string GetValue( string VariableName ) {
            string ReturnValue = string.Empty;

            if ( !_VariableValidator.IsMatch( VariableName ) )
                throw new InvalidVariableNameException( VariableName, "variable name is not valid" );

            Match m = _ValueExtractor.Match( VariableName );
            if ( m.Success ) {
                VariableName = m.Groups[1].Value;
                if ( _Variables.ContainsKey( VariableName ) )
                    ReturnValue = _Variables[ VariableName ];
                else
                    throw new VariableNotDefinedException( VariableName, "variable is not defined");
            } 
						else {
                throw new InvalidDereferenceException( VariableName, "use ${} to deference a variable!" );
            }

            return ReturnValue;
        }


        public static string DirectGetValue( string VariableName ) {
            string ReturnValue = VariableName;
            if ( ActionVariables._Variables.ContainsKey( VariableName ) )
                ReturnValue = ActionVariables._Variables[ VariableName ];

            return ReturnValue;
        }


        public static string ScanVariable( string InputString ) {
            if ( InputString == null )
                return InputString;

            if ( _ValueExtractor.IsMatch( InputString ) ) {
                Match m = _ValueExtractor.Match( InputString );
                while ( m.Success ) {
                    string VariableName = m.Groups[1].Value;
                    // int Pos = m.Index;
                    if ( _Variables.ContainsKey( VariableName ) ) {
                        string Value = _Variables[ VariableName ];
                        // InputString = _ValueExtractor.Replace( InputString, Value );
                        string ReplaceString = "${" + VariableName + "}";
                        InputString = InputString.Replace( ReplaceString, Value );
                    } else {
                        throw new VariableNotDefinedException(
                            String.Format( "Variable {0} does not exist!", InputString ) );
                    }
                    m = _ValueExtractor.Match( InputString );
                }
            }

            return InputString;
        }


        public static bool IsVariableExist( string VariableName ) {
            return ActionVariables._Variables.ContainsKey( VariableName );
        }

        public static void Clear() {
            ActionVariables._Variables.Clear();
        }
    }
}
