using System.Text;

namespace Hulk.src
{
    public class Lexer
    {

        #region Fields, Constructor and Properties
        
        // Lista de caracteres y palabras
        List<char> SpecialCharacters = new List<char>(new char[] { ';', '=', '@' });
        List<char> SeparatorCharacters = new List<char>(new char[] { '(', ')', ',' });
        List<char> ArithmeticOperatorCharacters = new List<char>(new char[] { '+', '-', '*', '/', '%', '^' });
        List<char> LogicOperatorsCharacters = new List<char>(new char[] { '>', '<', '!', '|', '&' });
        List<string> KeywordCharacters = new List<string>(new string[] { "if", "then", "else", "let", "in", "function" });

        // Lista de tokens
        List<TokenInterface> TokenList;

        // Lista de errores lexicos
        List<CompilingError> ErrorList;

        // Constructor
        public Lexer()
        {
            // Inicializar los campos 
            TokenList = new List<TokenInterface>();
            ErrorList = new List<CompilingError>();
        }

        // Propiedad que evalua si se encontraron errores lexicos en el input
        public bool IsThereAnyLexicalError { get => ErrorList.Count != 0; }

        #endregion

        #region Metodos

        // Metodo que devuelve los errores lexicos
        public List<CompilingError> GetErrors() => ErrorList;

        // Metodo para añadir un error lexico a la lista
        private void AddLexicalErrorToList(int column, string argument)
        {
            ErrorList.Add(new CompilingError(ErrorType.Lexical, column, argument));
        }

        // Metodo que convierte el texto de input en una lista de tokens

        public IEnumerable<List<TokenInterface>> CreateTokens(string input)
        {
            // Crear una variable para guardar un posible tokens
            StringBuilder preToken = new StringBuilder();

            // Iterar por el input
            for (int i = 0, col = 1; i < input.Length; i++, col = i + 1)
            {
                // Evaluar el caracter y si es posible añadirlo a las lista de tokens
                if (SeparatorCharacters.Contains(input[i]))
                {
                    // Tokens: '(', ')', ','
                    TokenList.Add(new SeparatorToken(col, input[i].ToString()));
                }
                else if (ArithmeticOperatorCharacters.Contains(input[i]))
                {
                    // Tokens: '+', '-', '*', '/', '%', '^'
                    TokenList.Add(new ArithmeticOperatorToken(col, input[i].ToString()));
                }
                else if (LogicOperatorsCharacters.Contains(input[i]))
                {
                    // Tokens: '&' , '|'
                    if (input[i] == '&' || input[i] == '|')
                        TokenList.Add(new LogicBooleanOperatorToken(col, input[i].ToString()));

                    // Token: '!'
                    else if (input[i] == '!' && (i == input.Length || input[i + 1] != '='))
                        TokenList.Add(new LogicBooleanOperatorToken(col, input[i].ToString()));

                    // Tokens: '>=', '<=' , '!='
                    else if (i + 1 != input.Length && input[i + 1] == '=')
                    {
                        TokenList.Add(new LogicArimeticOperatorToken(col, input[i].ToString() + "="));
                        i++;
                    }

                    // Tokens: '>', '<'
                    else
                        TokenList.Add(new LogicArimeticOperatorToken(col, input[i].ToString()));
                }
                else if (SpecialCharacters.Contains(input[i]))
                {
                    // Token especial EOL: ';' 
                    if (input[i] == ';')
                    {
                        // Añadir el token EOL
                        TokenList.Add(new EndOfLineToken(col));

                        // Devolverlo al bucle foreach que lo llama desde el metodo Main en la clase Program
                        yield return (List<TokenInterface>)TokenList;

                        // Inicializar de nuevo las variables
                        TokenList = new List<TokenInterface>();
                        ErrorList = new List<CompilingError>();

                    }

                    // Token de concatenacion de string: '@'
                    else if (input[i] == '@')
                        TokenList.Add(new SpecialOperatorToken(col, "@"));

                    // Tokens que contiene '='
                    else if (input[i] == '=')
                    {
                        // Token logico: '=='
                        if (i + 1 != input.Length && input[i + 1] == '=')
                        {
                            TokenList.Add(new LogicArimeticOperatorToken(col, "=="));
                            i++;
                        }

                        // Token para la declaracion de funciones: '=>'
                        else if (i + 1 != input.Length && input[i + 1] == '>')
                        {
                            TokenList.Add(new SpecialOperatorToken(col, "=>"));
                            i++;
                        }

                        // Token para la asignacion '=' en expresiones let-in: '='
                        else
                            TokenList.Add(new SpecialOperatorToken(col, "="));
                    }
                }
                else if (char.IsDigit(input[i]))
                {
                    // Evaluar si el token es un numero

                    // Comprobar la cantidad de token creados
                    if (TokenList.Count >= 1)
                    {
                        // Comprobrar si el ultimo token es el signo menos '-'
                        if (TokenList[TokenList.Count - 1] is ArithmeticOperatorToken && ((ArithmeticOperatorToken)TokenList[TokenList.Count - 1]).TokenValue == "-")
                        {
                            // Añadir el caracter menos al token numero que va ser creado
                            preToken.Append("-");

                            // Comprobar el cararter anterior al '-' si es un numero o un identificador.

                            // En caso positivo cambiar '-' por '+'. Ejemplo 3 + (-3) o i + (-3)
                            if (TokenList.Count >= 2 && (TokenList[TokenList.Count - 2] is NumberToken or IdentifierToken || TokenList[TokenList.Count - 2].GetTokenValueAsString() == ")"))
                                ((ArithmeticOperatorToken)TokenList[TokenList.Count - 1]).ChangeValue("+");

                            // En caso negativo eleminar '-' 
                            else
                                TokenList.RemoveAt(TokenList.Count - 1);
                        }
                    }

                    // Iterar por el input para crear el token numero
                    while (i != input.Length && input[i] != ' ')
                    {
                        if (SeparatorCharacters.Contains(input[i]) || ArithmeticOperatorCharacters.Contains(input[i]) || LogicOperatorsCharacters.Contains(input[i]) || SpecialCharacters.Contains(input[i]))
                            break;

                        preToken.Append(input[i]);
                        i++;
                    }

                    // Intentar parsear el posible token
                    double number;
                    if (double.TryParse(preToken.ToString(), out number))
                        TokenList.Add(new NumberToken(col, number));
                    else
                        AddLexicalErrorToList(col, $"Invalid token `{preToken}`");

                    preToken.Clear();
                    i--;
                }
                else if (input[i] == '"')
                {
                    // Evaluar si el token es un string
                    preToken.Append(input[i]);
                    i++;

                    // Iterar por el input
                    while (true)
                    {
                        // Romper el ciclo si llegamos al final del input. Se creo un token invalido
                        if (i == input.Length)
                            break;

                        // Rompero el cilco si se encuenta otro caracter '"'. Se creo un token valido
                        if (input[i] == '"')
                        {
                            preToken.Append(input[i]);
                            break;
                        }

                        // Evaluar los carecters especiales que aparecen en los string
                        if (input[i] == '\\' && i < input.Length - 1)
                        {
                            switch (input[i + 1])
                            {
                                case '\"': // Añardir al string el caracter '"'
                                    preToken.Append("\"");
                                    i += 1;
                                    break;
                                case 'n': // Añardir al string una nueva linea
                                    preToken.Append("\n");
                                    i += 1;
                                    break;
                                case 't': // Añardir al string un espacio tabulador
                                    preToken.Append("\t");
                                    i += 1;
                                    break;
                                default:
                                    preToken.Append(input[i]);
                                    break;
                            }
                        }
                        else
                        {
                            preToken.Append(input[i]);
                        }

                        i++;
                    }

                    // Evaluar si el ultimo caracter del string es '"'
                    if (preToken[preToken.Length - 1] != '\"')
                        AddLexicalErrorToList(col, $"Invalid token {preToken}. Expected double-quotes `\"` to close the sting token");
                    else
                        TokenList.Add(new StringToken(col, preToken.ToString()));

                    preToken.Clear();

                }
                else if (input[i] == ' ')
                {
                    continue;
                }
                else
                {
                    // Evualuar si hay token valido
                    bool invalidToken = false;

                    // Iterar por el input
                    while (i != input.Length && input[i] != ' ')
                    {
                        if (SeparatorCharacters.Contains(input[i]) || ArithmeticOperatorCharacters.Contains(input[i]) || LogicOperatorsCharacters.Contains(input[i]) || SpecialCharacters.Contains(input[i]))
                            break;

                        // Un token solo puede estar compuestos numeros, letras y los caracteres de operadores
                        if (!char.IsLetterOrDigit(input[i]))
                            invalidToken = true;

                        preToken.Append(input[i]);
                        i++;
                    }

                    string preTokenSTR = preToken.ToString();

                    // Evaluar si es un token invalido
                    if (invalidToken)
                        AddLexicalErrorToList(col, $"Invalid token `{preTokenSTR}`");

                    // Evaluar si el token es una palabra clave: "if", "then", "else", "let", "in", "function"
                    else if (KeywordCharacters.Contains(preToken.ToString()))
                        TokenList.Add(new KeywordToken(col, preTokenSTR));

                    // Evaluar si el token es un literal booleano
                    else if (preTokenSTR == "true" || preTokenSTR == "false")
                        TokenList.Add(new BooleanToken(col, bool.Parse(preTokenSTR)));

                    // Evaluar si el token es un literal constante.
                    else if (preTokenSTR == "PI")
                        TokenList.Add(new NumberToken(col, Math.PI));
                    else if (preTokenSTR == "E")
                        TokenList.Add(new NumberToken(col, Math.E));

                    // En caso contrario el token es un identificados
                    else
                        TokenList.Add(new IdentifierToken(col, preTokenSTR));

                    preToken.Clear();
                    i--;
                }

            }

            yield return (List<TokenInterface>)TokenList;
        } 
        #endregion

    }
}
