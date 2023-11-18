using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hulk.src;

namespace Hulk.src
{
    public class Parser
    {
        #region Fields 

        private LinkedList<IToken> TokensLinkedList;

        private LinkedList<LinkedListNode<IToken>[,]> IfElseLinkedList;
        private Dictionary<LinkedListNode<IToken>, LinkedListNode<IToken>> ParenthesisDictionary;

        private LetInClass LetInColletion;
        List<CompilingError> ErrorList;

        #endregion

        #region Properties

        public IToken? Output { private set; get; }
        
        public bool IsThereAnyError { get { return ErrorList.Count != 0; } private set { } }

        #endregion

        #region Constructor

        public Parser(List<IToken> tokensList)
        {
            // Inicializar los campos
            TokensLinkedList = new LinkedList<IToken>(tokensList);
            ErrorList = new List<CompilingError>();

            IfElseLinkedList = new LinkedList<LinkedListNode<IToken>[,]>();
            ParenthesisDictionary = new Dictionary<LinkedListNode<IToken>, LinkedListNode<IToken>>();

            LetInColletion = new LetInClass();

            // Evualar el input
            CheckAndEvaluate();

        }

        #endregion

        #region Metodos auxiliares 
        
        // Metodo que devuelve la lista de errores
        public List<CompilingError> GetErrors() => ErrorList;

        // Metodo para añadir un error a la lista
        private void AddErrorToList(ErrorType type, int column, string argument)
        {
            ErrorList.Add(new CompilingError(type, column, argument));
        }

        // Metodo para buscar el index de un nodo especifico dentro de la Lista enlazable
        private int SearchIndex(LinkedListNode<IToken> token)
        {
            return (token is null) ? -1 : TokensLinkedList.ToList().IndexOf(token.Value);
        }

        // Metodo para buscar el partentesis de apertura dado su correspondiente parentesis de cierre
        private LinkedListNode<IToken>? SearchOpenParentesis(LinkedListNode<IToken> closeParenthesis)
        {
            foreach (var item in ParenthesisDictionary)
            {
                if (item.Value == closeParenthesis)
                    return item.Key;
            }

            return null;
        }

        #endregion

        #region Checking and Evaluation Methods

        // Metodo principal de comprobacion y evaluacion
        private void CheckAndEvaluate()
        {
            // Comprobar preliminarmete la sintaxis y la semantica de los tokens
            if (!CheckSyntaxAndSemantic())
                return;

            // Comprobar si una funcion fue declarada correctamente
            switch (CheckInlineFunctionDeclaration(TokensLinkedList.First!))
            {
                /* Devuelve:
                   * 1 si la funcion fue declarada correctamente
                   * 0 si el input no es una declaracion de funciones
                   * -1 si existe algun error en la declaracion de funciones */
                case -1 or 1:
                    return;
                default:
                    break;
            }

            // Comprueba si una expresion `let-in` fue declarada correctamente. Y despues la evalua
            if (!CheckAndEvaluateLetInExpression(TokensLinkedList.First!))
                return;

            // Comprueba si una expresion `if-else` esta declarada correctamente
            if (!CheckIfElseExpression(TokensLinkedList.First!, 0, null, null))
                return;

            // Evaluar la expresion
            if (!ExpressionEvaluator(TokensLinkedList.First!, TokensLinkedList.Last!))
                return;

            // Comprobar la expresion evaluada
            FinalCheck();

        }

        // Comprobar preliminarmete la sintaxis y la semantica de los tokens
        private bool CheckSyntaxAndSemantic()
        {
            // Comprobar que token EOL se encuentre en la expresion como el ultimo token
            CheckEndOfLineToken();

            // Comprobar si los parentesis estan balanceados
            CheckParenthesis(new Stack<LinkedListNode<IToken>>(), TokensLinkedList.First!);
            
            // Comprobar preliminarmente operadores perdidos entre tokens
            CheckMissingOperators(TokensLinkedList.First!);

            if (IsThereAnyError)
                return false;

            return true;

            #region Funciones locales

            void CheckEndOfLineToken()
            {
                if (TokensLinkedList.Last() is not EndOfLineToken)
                {
                    AddErrorToList(ErrorType.Syntax, TokensLinkedList.Last().GetColumn(), $"Expected token `;` ");
                }
            }

            void CheckParenthesis(Stack<LinkedListNode<IToken>> OpenParentesisStack, LinkedListNode<IToken> currentToken)
            {
                // Salir del metodo
                if (currentToken is null) // The current token supposed to be is ";"
                {
                    // Comprobar si existe aun algun parentesis dentro de la pila
                    if (OpenParentesisStack.Count == 0)
                        return;
                    else
                    {
                        AddErrorToList(ErrorType.Syntax, OpenParentesisStack.Peek().Value.GetColumn(), "Missing close parenthesis `)`");
                        return;
                    }
                }

                // Comprobar si currentToken es un parentesis de apertura y adicionarlo a la pila
                if (currentToken.Value.GetTokenValueAsString() == "(")
                {
                    OpenParentesisStack.Push(currentToken);
                }
                // / Comprobar si currentToken es un parentesis de cierre
                else if (currentToken.Value.GetTokenValueAsString() == ")")
                {
                    // Comprobar que en la pila haya algun parentesis de apertura
                    LinkedListNode<IToken> start;
                    if (OpenParentesisStack.TryPop(out start!))
                    {
                        ParenthesisDictionary.Add(start, currentToken);
                    }
                    else
                    {
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), "Missing open parenthesis `(`");
                        return;
                    }
                }

                CheckParenthesis(OpenParentesisStack, currentToken.Next!);
            }

            void CheckMissingOperators(LinkedListNode<IToken> currentToken)
            {
                // Salir del metodo
                if (currentToken.Next is null)
                    return;

                // Comprobar si exite un operado que falta
                if (currentToken.Value is NumberToken or StringToken or BooleanToken
                    && currentToken.Next!.Value is NumberToken or StringToken or BooleanToken)
                {
                    AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn(), $"Missing operator between `{currentToken.Value.GetTokenValueAsString()}` and `{currentToken.Next.Value.GetTokenValueAsString()}`");
                }

                CheckMissingOperators(currentToken.Next!);
            }
            #endregion


        }

        // Comprobar si una funcion fue declarada correctamente
        private int CheckInlineFunctionDeclaration(LinkedListNode<IToken>? currentToken)
        {
            // Salir del metodo
            if (currentToken!.Value is EndOfLineToken)
                return 0;

            // Entrar en el comprobador:
            if (currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "function")
            {
                // Comprobar que la palabra clave `function` sea el 1er token
                if (currentToken != TokensLinkedList.First)
                {
                    AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn(), "Function declaration must be a sigle and only line");
                    return -1;
                }

                // Comprobar que se haya declarado una unica funcion
                LinkedListNode<IToken>? token = currentToken.Next;
                while (token!.Value is not EndOfLineToken)
                {
                    if (token.Value is KeywordToken && token.Value.GetTokenValueAsString() == "function")
                    {
                        AddErrorToList(ErrorType.Semantic, token.Value.GetColumn(), "Only one function can be declared per line");
                        return -1;
                    }

                    token = token.Next;
                }

                currentToken = currentToken.Next;

                // Comprobar el identificador
                if (currentToken!.Value is not IdentifierToken)
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected an identifier type token after `{currentToken.Previous!.Value.GetTokenValueAsString()}`");
                    return -1;
                }
                
                // Guardar el nombre de la funcion y comprobar si ya exite una funcion llamada del mismo mod
                string functionName = currentToken.Value.GetTokenValueAsString();
                if (InlineFunctionClass.ExistFunction(functionName) || BuiltInFunctionClass.IsBuilInFunction(functionName))
                {
                    AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn(), $"The function `{functionName}` already exists");
                    return -1;
                }

                currentToken = currentToken.Next;

                // Comrpobar que el proximo token sea un parentesis de apertura
                if (!(currentToken!.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == "("))
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `(` after identifier `{currentToken.Previous!.Value.GetTokenValueAsString()}`");
                    return -1;
                }

                // Crear la lista de parametros
                List<string>? param;

                // Funcion sin parametros
                if (currentToken.Next!.Value is SeparatorToken && currentToken.Next.Value.GetTokenValueAsString() == ")") /// Function without any parameters
                {
                    param = null;
                    currentToken = currentToken.Next!;
                }
                // Funcion con parametros
                else
                {
                    // Obtener los parametros
                    param = new List<string>();
                    do
                    {
                        currentToken = currentToken!.Next;

                        // Comprobar si el token siguiente es un identificador
                        if (currentToken!.Value is not IdentifierToken)
                        {
                            AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected an identifier type token after `{currentToken.Previous!.Value.GetTokenValueAsString()}`");
                            return -1;
                        }

                        // Añadir el parametro a la lista
                        param.Add(currentToken.Value.GetTokenValueAsString());
                        currentToken = currentToken.Next;

                        // Comprobar que el proximo token sea ',' para multiples parametros
                        if(currentToken!.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == ",")
                        {
                            continue;
                        }

                        // Comprobar si el token es `)` para salir del bucle
                        else if (currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == ")")
                            break;

                        // En caso contrario hay un error en la declaracion de la funcion
                        else
                        {
                            AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `)` or `,` after identifier `{currentToken.Previous!.Value.GetTokenValueAsString()}`");
                            return -1;
                        }
                    } while (true);
                }

                currentToken = currentToken.Next;

                // Comprobar que el token sea el operado de funcion 
                if (!(currentToken!.Value is SpecialOperatorToken && currentToken.Value.GetTokenValueAsString() == "=>"))
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `=>` after token `)`");
                    return -1;
                }

                // Comprobar que el cuerpo no este conformado solo por el token EOL
                if(currentToken.Next!.Value is EndOfLineToken)
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Missing the body expression of the function `{functionName}` after token `=>`");
                    return -1;
                }
                
                // Añadir los token que falta una lista, los cuales van a componer el cuerpo de la funcion
                LinkedList<IToken> body = new LinkedList<IToken>();
                do
                {
                    currentToken = currentToken.Next;

                    // Comprobar por varaibles no declaradas dentro de los parametros
                    if(currentToken!.Value is IdentifierToken)
                    {
                        if (!param!.Contains(currentToken.Value.GetTokenValueAsString()))
                        {
                            if(currentToken.Next!.Value.GetTokenValueAsString() != "(")
                            {
                                AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn(), $"The variable `{currentToken!.Value.GetTokenValueAsString()}` is not declared");
                                return -1;
                            }
                        }
                    }

                    body.AddLast(currentToken!.Value);

                } while (currentToken.Value is not EndOfLineToken);

                // Añadir el nombre de la funcion, loss parametros y el cuerpo a la clase InlineFuction
                InlineFunctionClass.AddFunction(functionName);
                InlineFunctionClass.AddParametersNameToLastFunction(param);
                InlineFunctionClass.AddBodyToLastFunction(body);

                return 1;
            }

            return CheckInlineFunctionDeclaration(currentToken.Next);
        }

        // Comprueba si una expresion `let-in` fue declarada correctamente. Y despues la evalua
        private bool CheckAndEvaluateLetInExpression(LinkedListNode<IToken>? currentToken)
        {
            // Salir de la comprobracion
            if (currentToken!.Value is EndOfLineToken)
            {
                // Comprobar que no halla quedado ninguna expresion `let-in` en la pila de la clase Let.
                // En caso afirmativo removerlos y dejar la pila vacia.
                while (LetInColletion.PeekLastLet() is not null)
                {
                    TokensLinkedList.Remove(LetInColletion.PeekLastLet()!);
                    LetInColletion.RemoveLastLet();
                }

                return true;
            }

            // Comprobar que el token sea `let` e iniciar la comprobacion lexica de una expresion `let-in`
            if (currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "let")
            {
                // Añadir el un nuevo `let` a la clase
                LetInColletion.AddLet(currentToken);

                // Comprobar el lexico de la declaracion 
                if (!CheckLetInExpression(currentToken.Next!, out currentToken!))
                    return false;

                // En caso que no haya ocurrido ningun error, continuar con la evaluacion de la expresion `let-in`
            }

            // Comprobar que el token sea una variable declarada en el let y no una funcion
            if (currentToken.Value is IdentifierToken && LetInColletion.ConstainsVariable(currentToken.Value.GetTokenValueAsString())
                && currentToken.Next!.Value.GetTokenValueAsString() != "(")
            {
                TokensLinkedList.AddAfter(currentToken, LetInColletion.PeekLastValue(currentToken.Value.GetTokenValueAsString()));
                currentToken = currentToken.Next;
                TokensLinkedList.Remove(currentToken!.Previous!);
            }

            // Comprobar si el token actual es `)`
            if (currentToken.Value.GetTokenValueAsString() == ")")
            {
                // Comprobar si el parentesis que lo abrio se encuentra antes de la declaracion de la expresion `let-in`
                if (SearchIndex(SearchOpenParentesis(currentToken)!) < SearchIndex(LetInColletion.PeekLastLet()!))
                {
                    TokensLinkedList.Remove(LetInColletion.PeekLastLet()!);
                    LetInColletion.RemoveLastLet();
                }
            }

            // Continuar evaluando y comprobando hasta el final de la instruccion (EOL)
            return CheckAndEvaluateLetInExpression(currentToken.Next);

            #region Funcion local

            // Comprobar el lexico de la declaracion de una expresion `let-in`
            bool CheckLetInExpression(LinkedListNode<IToken>? currentToken, out LinkedListNode<IToken>? actualToken)
            {
                // Variable que guardara al final del metodo el token por donde se quedo la evaluacion 
                actualToken = null;
                
                // Varaible que guardadara el nombre de las variables declaradas en el let
                string variableName;

                // Comprobar el lexico
                do
                {                  
                    // Comprobar si el token es un identificador
                    if (currentToken!.Value is not IdentifierToken)
                    {
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected an identifier type token");
                        return false;
                    }

                    // Guardar el nombre del identificador, añadirlo el nombre la clase LetInColletion y actualizar
                    variableName = currentToken.Value.GetTokenValueAsString();
                    LetInColletion.AddVariableNameToLastLet(variableName);
                    Update();

                    // Comprobar que el token sea `=`
                    if (!(currentToken.Value is SpecialOperatorToken && currentToken.Value.GetTokenValueAsString() == "="))
                    {
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `=` after identifier `{variableName}`");
                        return false;
                    }

                    // Actualizar
                    Update();

                    // Crear una variable que guardara el resultado de la evaluacion de la expresion asociada con la variable
                    IToken? output;

                    // Si la evaluacion de la expresion asociada a la variable da falso, entonces ocurrio un error durandte la evaluacion
                    if (EvaluateLetVariable(out output))
                    {
                        LetInColletion.AddVariableValue(variableName, new LinkedListNode<IToken>(output));
                    }
                    else
                    {
                        return false;
                    }

                    // Continuar la comprobacion si se encuentra un token de tipo `,` y actualizar, sino salir del bucle
                    if (currentToken!.Value.GetTokenValueAsString() == ",")
                        Update();
                    else
                        break;

                } while (true);

                if (!(currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "in"))
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `=` after identifier `{variableName}`");
                    return false;
                }

                Update();
                actualToken = currentToken;
                return true;

                #region SubFunciones Locales

                // Actulizar el valor del nodo actual y elemininarlo
                void Update()
                {
                    currentToken = currentToken.Next!;
                    TokensLinkedList.Remove(currentToken.Previous!);

                }

                // Evaluar la expresion asociada a una variable
                bool EvaluateLetVariable(out IToken output)
                {
                    // Crear una lista de tokens que guardaran la expresion asociada a la variable, la cual luego sera parseada
                    List<IToken> tokens = new List<IToken>();
                    
                    string tokenValue;

                    // Crear una variable para llevar la cuenta de cuantos posibles `let` anidados se encuentran dentro de la expresion
                    int letCount = 0;

                    // Iterar por la lista enlazable
                    do
                    {
                        // Combrobar si el token actual sea `in`
                        if (currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "in")
                        {
                            // Comprobar cuandos `let` anidados quedan
                            if (letCount == 0)
                                break;
                            else
                                letCount--;
                        }

                        // Adicionar a la lista el token actual
                        tokens.Add(currentToken.Value);

                        // Comprobar si el token es `(`
                        if (currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == "(")
                        {
                            // Directamente adicionar a la lista todos los tokens hasta el correspondiente `)`
                            LinkedListNode<IToken> closeParenthesis = ParenthesisDictionary[currentToken];
                            do
                            {
                                Update();
                                tokens.Add(currentToken.Value);

                            } while (currentToken != closeParenthesis);
                        }

                        // Comrobar que el token actual sea `let` y aumentar el contador
                        else if (currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "let")
                        {
                            letCount++;
                        }

                        // Actualizar
                        Update();
                        tokenValue = currentToken.Value.GetTokenValueAsString();

                    } while (tokenValue != "," && tokenValue != ";");

                    // Comprobar que la lista no esta vacia
                    if (tokens.Count == 0)
                    {
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected expression after token '='");
                        output = null!;
                        return false;
                    }

                    // Comprobar que el token actual no se EOL
                    if (currentToken.Value.GetTokenValueAsString() == ";")
                    {
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Missing token 'in' in `let-in` expression");
                        output = null!;
                        return false;
                    }

                    // Adicionar a la lista de la expresion asociada a la variable `let` un token EOL
                    tokens.Add(new EndOfLineToken(tokens.Last().GetTokenValueAsString().Length + tokens.Last().GetColumn()));

                    // Parsear la lista 
                    Parser result = new Parser(tokens)!;

                    // Comprobar que no haya ningun error
                    if (result.IsThereAnyError)
                    {
                        // En caso que haya algun error, adicionarlos a la lista de errores de este objeto
                        output = null!;
                        ErrorList.AddRange(result.GetErrors());
                        return false;
                    }

                    // Devolver el rultado de la exprsion parseada
                    output = result.Output!;
                    return true;

                } 

                #endregion
            }

            #endregion
        }

        // Comprueba si una expresion `if-else` esta declarada correctamente
        private bool CheckIfElseExpression(LinkedListNode<IToken>? currentToken, double actualIfPart, LinkedListNode<LinkedListNode<IToken>[,]>? currentIf, LinkedListNode<LinkedListNode<IToken>[,]>? sourceIf)
        {
            // TODO Crear una nueva estructura o clase para el if

            // Comprobar el si el token sea EOL
            if (currentToken!.Value is EndOfLineToken)
            {
                /* Comprobar por que parte del if se encuentra:
                    * 0: no se declaro ningun if
                    * 1: solo se encrontro la parte de evaluacion del if, es decir las que esta entre parentesis
                    * 1.5: falta la parte `else` del if
                    * 2: se encontro el identificador `else` pero ninguna expresion a la que evaluar
                    * 2.5: la expresion `if-else` esta declarada correctamente
                */
                switch (actualIfPart)
                {
                    case 0:
                        return true;
                    case 1:
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), "Missing the `body` part of an `if` expression");
                        return false;
                    case 1.5 or 2:
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(),"Missing the `else` part of an `if` expression");
                        return false;
                    case 2.5:

                        // Adionar a la matriz los valores faltantes y que concluyen la declaracion de la expresion `if-else`
                        currentIf!.Value[3, 1] = currentToken;
                        currentIf!.Value[0, 1] = currentToken;

                        // Combropar si no hay un `if` padre del actual `if`
                        if (sourceIf is null)
                            return true;
                        else
                            return CheckIfElseExpression(currentToken, 2.5, sourceIf, null);
                    default:
                        break;
                }
            }

            // Comprobar si el token sea `if` y comenzar la comprobacion de la declaracion
            if (currentToken.Value.GetTokenValueAsString() == "if")
            {
                // Comprobar si el nuevo `if` se encuentra anidado dentro de otro `if`
                if (currentIf is not null)
                {
                    // Comrpobar por que parte se quedo el `if` padre

                    // Comprobar si el nuevo `if` esta anidado dentro de la evaluacion de los parentesis del `if` padre
                    if (currentIf.Value[1, 1] is not null)
                    {
                        // En caso contrario ver en que parte se encuentra, si en el `body` o en el `else`
                        // En ambos casos adiocionar el `if` como inicio del body o del else si todavio no han inciado
                        if (currentIf.Value[2, 0] is null && actualIfPart == 1)
                            currentIf.Value[2, 0] = currentToken;
                        else if (currentIf.Value[3, 0] is null && actualIfPart == 2)
                            currentIf.Value[3, 0] = currentToken;

                    }

                    // Asignar al padre del nuevo `if`
                    sourceIf = currentIf;
                }

                // Adionar a la lista el nuevo `if` 
                IfElseLinkedList.AddLast(new LinkedListNode<LinkedListNode<IToken>[,]>(new LinkedListNode<IToken>[4, 2]));
                
                // Establecer el `if` como actual
                currentIf = IfElseLinkedList.Last!;

                // Establecer el inicio del `if`
                currentIf!.Value[0, 0] = currentToken;

                //Actualizar el token y comprobar que sea un parentesis de apertura
                currentToken = currentToken.Next;
                if (currentToken!.Value.GetTokenValueAsString() != "(")
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), "Missing open parenthesis `(` after `if`");
                    return false;
                }

                // Establecer el token de incio de la expresion boolenana a evaluar
                currentIf.Value[1, 0] = currentToken;

                // Seguir la comprobacion hasta encontrar el correspondiente parentesis de cierre.
                return CheckIfElseExpression(currentToken.Next, 0.5, currentIf, sourceIf);
            }

            // Comrpobar si token actual es el parentesis de cierre correspondiente inicio de la expresion booleana 
            if (actualIfPart == 0.5 && ParenthesisDictionary[currentIf!.Value[1, 0]] == currentToken)
            {
                // Asignar el token de cierre de la expresion boolena
                currentIf.Value[1, 1] = currentToken;

                // Seguir con la comprobacion hasta encontrar un else
                return CheckIfElseExpression(currentToken.Next, 1, currentIf, sourceIf);
            }

            // Comprobar el primer token despues del parentesis de cierre
            if (actualIfPart == 1)
            {
                // Si el token actual es `else` entonces la expresion `if-else` esta declarada incorrectamente
                if (currentToken.Value.GetTokenValueAsString() == "else")
                {
                    return CheckIfElseExpression(TokensLinkedList.Last, actualIfPart, currentIf, sourceIf);
                }
                // Sino se asigna el token de inicio del `body`
                else
                {
                    currentIf!.Value[2, 0] = currentToken;
                    return CheckIfElseExpression(currentToken.Next, 1.5, currentIf, sourceIf);
                }

            }

            // Comprobar que el token sea `else` y asignar el token de cierre del body
            if (currentToken.Value.GetTokenValueAsString() == "else" && actualIfPart == 1.5)
            {
                currentIf!.Value[2, 1] = currentToken!;
                return CheckIfElseExpression(currentToken.Next, 2, currentIf, sourceIf);
            }

            // Asignar el el primer token despues del `else` como su inicio
            if (actualIfPart == 2)
            {
                currentIf!.Value[3, 0] = currentToken;
                return CheckIfElseExpression(currentToken.Next, 2.5, currentIf, sourceIf);
            }

            // Comprobar los token que pueden finalizar `else` del if actual
            if (actualIfPart == 2.5)
            {
                switch (currentToken.Value.GetTokenValueAsString())
                {
                    // Comprobar que sea un `else` del if padre
                    case "else":
                        // Asignar los tokens de cierre tanto del `else` como del `if` 
                        currentIf!.Value[3, 1] = currentToken;
                        currentIf.Value[0, 1] = currentToken;

                        // Asignar como `if` actual al padre
                        currentIf = sourceIf;

                        // Asignar la parte por donde se quedo el `if` padre
                        actualIfPart = 1.5;
                        currentToken = currentToken.Previous;
                        break;

                    // Comprobar el parentesis de cierre
                    case ")":

                        // Buscar el parentesis de apertura correspondiente al cierre del token actual
                        LinkedListNode<IToken> openParenthesis = new LinkedListNode<IToken>(new SeparatorToken(-1, "("));
                        foreach (var item in ParenthesisDictionary)
                        {
                            if (item.Value == currentToken)
                            {
                                openParenthesis = item.Key;
                                break;
                            }
                        }

                        // Comprobar que el parentesis de apertura se haya declarado antes que el `if` actual 
                        if (SearchIndex(openParenthesis) < SearchIndex(currentIf!.Value[0, 0]))
                        {
                            // Asignar los tokens de cierre tanto del `else` como del `if`
                            currentIf.Value[3, 1] = currentToken;
                            currentIf.Value[0, 1] = currentToken;

                            // Comprobar si existe un `if` padre y saber en que parte se quedo
                            if (sourceIf is not null && sourceIf.Value[0, 1] is null)
                            {
                                if (sourceIf.Value[1, 1] is null)
                                    actualIfPart = 0.5;
                                else if (sourceIf.Value[2, 0] is null)
                                    actualIfPart = 1;
                                else if (sourceIf.Value[2, 1] is null)
                                    actualIfPart = 1.5;
                                else if (sourceIf.Value[3, 0] is null)
                                    actualIfPart = 2;
                                else if (sourceIf.Value[3, 1] is null || sourceIf.Value[0, 1] is null)
                                    actualIfPart = 2.5;
                            }
                            else
                            {
                                actualIfPart = 0;
                            }

                            currentIf = sourceIf;
                            currentToken = currentToken.Previous;
                        }

                        break;
                }
            }

            return CheckIfElseExpression(currentToken!.Next, actualIfPart, currentIf, sourceIf);

        }

        // Evaluador de expresiones
        private bool ExpressionEvaluator(LinkedListNode<IToken>? startToken, LinkedListNode<IToken>? endToken)
        {
            // Inicializar una serie de colas y pilas que guardaran los tokens de los operadores
            Queue<LinkedListNode<IToken>> SumRestOpetorStack = new Queue<LinkedListNode<IToken>>();
            Queue<LinkedListNode<IToken>> MultDivOperatorStack = new Queue<LinkedListNode<IToken>>();
            Queue<LinkedListNode<IToken>> PowOperatorStack = new Queue<LinkedListNode<IToken>>();
            Queue<LinkedListNode<IToken>> LogicArimeticOperatorQueue = new Queue<LinkedListNode<IToken>>();
            LinkedList<LinkedListNode<IToken>> NotOperatorStack = new LinkedList<LinkedListNode<IToken>>();
            //Stack<LinkedListNode<IToken>> NotOperatorStack = new Stack<LinkedListNode<IToken>>();
            Queue<LinkedListNode<IToken>> LogicBooleanOperatorQueue = new Queue<LinkedListNode<IToken>>();
            Queue<LinkedListNode<IToken>> ConcatenationOperatorQueue = new Queue<LinkedListNode<IToken>>();

            
            // Llenar las colas de los operadores y evuluar los parentesis, funciones y expresiones if-else
            if (!FillOperatorsQueueAndPreliminarEvaluation(startToken!, endToken!))
                return false;

            // Evualuar las expresiones aritmeticas y booleanas
            if (!ArimeticAndLogicEvaluator())
                return false;

            return true;

            #region Funciones locales 
            
            bool FillOperatorsQueueAndPreliminarEvaluation(LinkedListNode<IToken> currentToken, LinkedListNode<IToken> finalToken)
            {
                // Comprobar si el token actual el ultimo de la evaluacion
                if (currentToken == finalToken || currentToken.Value is EndOfLineToken)
                    return true;

                // Comprobar si el token es un operador y adicionarlo a la queue correspondiente
                if (currentToken.Value is OperatorToken)
                {
                    if (currentToken.Value is ArithmeticOperatorToken)
                    {
                        switch (currentToken.Value.GetTokenValueAsString())
                        {
                            case "+" or "-":
                                SumRestOpetorStack.Enqueue(currentToken);
                                break;
                            case "*" or "/" or "%":
                                MultDivOperatorStack.Enqueue(currentToken);
                                break;
                            case "^":
                                PowOperatorStack.Enqueue(currentToken);
                                break;
                            default:
                                break;
                        }
                    }

                    else if (currentToken.Value is LogicArimeticOperatorToken)
                    {
                        LogicArimeticOperatorQueue.Enqueue(currentToken);
                    }

                    else if (currentToken.Value is LogicBooleanOperatorToken)
                    {
                        if (currentToken.Value.GetTokenValueAsString() == "!")
                            NotOperatorStack.AddLast(currentToken);
                        //NotOperatorStack.Push(currentToken);
                        else
                            LogicBooleanOperatorQueue.Enqueue(currentToken);
                    }

                    else if (currentToken.Value is SpecialOperatorToken)
                    {
                        switch (currentToken.Value.GetTokenValueAsString())
                        {
                            case "=>":
                                AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), "The operator `=>` can only be used for function declaration");
                                return false;
                            case "@":
                                ConcatenationOperatorQueue.Enqueue(currentToken);
                                break;
                            default:
                                break;
                        }
                    }

                }

                // Comprobar si el token es un parentesis de apertura `(` y evaluar la expresion entre los parentesis
                else if (currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == "(")
                {
                    // Comrobar que el proximo no sea un parentesis de cierre
                    if (currentToken.Next!.Value.GetTokenValueAsString() == ")")
                    {
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected expression inside the parethensis");
                        return false;
                    }

                    // Comprobar que el parentesis de cierre no salga fuera de los limetes de la evaluacion
                    if (SearchIndex(ParenthesisDictionary[currentToken]) > SearchIndex(finalToken))
                    {
                        AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn(), $" The close parenthesis `)` is outside the limits of evaluation");
                        return false;
                    }

                    // Evaluar la expresion dentro de los parentesis
                    if (ExpressionEvaluator(currentToken.Next!, ParenthesisDictionary[currentToken]))
                    {
                        // Asignar nuevamente las token de evaluacion
                        if (finalToken.Next is null && finalToken.Previous is null)
                            finalToken = ParenthesisDictionary[currentToken];

                        if (finalToken == ParenthesisDictionary[currentToken])
                            finalToken = finalToken.Next!;

                        currentToken = currentToken.Next!;

                        // Remover los parentesis de la lita de tokens
                        TokensLinkedList.Remove(ParenthesisDictionary[currentToken!.Previous!]);
                        TokensLinkedList.Remove(currentToken.Previous!);

                        // TODO VER si en el parentesis no quedo otro token
                    }
                    else
                        return false;
                }

                // Comprobar si el token es `if` y evaluar la expresion`if-else`
                else if (currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "if")
                {
                    // Iterar por la lista
                    foreach (var ifToken in IfElseLinkedList)
                    {
                        // Comprobar que el `if` sea igual al token `if` actual
                        if (ifToken[0, 0] == currentToken)
                        {
                            // Evaluar la expresion entre parentesis
                            if (!ExpressionEvaluator(ifToken[1, 0].Next, ifToken[1, 1].Previous))
                                return false;

                            // Comprobar que el resultado de la evaluacion sea de tipo booleano
                            if (ifToken[1, 0].Next!.Value is not BooleanToken)
                            {
                                AddErrorToList(ErrorType.Semantic, ifToken[0, 0].Next!.Value.GetColumn(), "The expression to evaluate in the `if-else` expression is not a boolean type");
                                return false;
                            }

                            // Crear una variable que guarde el resultado de la evaluacion del `if` completo
                            LinkedListNode<IToken>? ifResult;

                            if (((BooleanToken)ifToken[1, 0].Next!.Value).TokenValue)
                            {
                                if (!ExpressionEvaluator(ifToken[2, 0], ifToken[2, 1]))
                                    return false;

                                ifResult = ifToken[3, 0].Previous!.Previous;

                            }
                            else
                            {
                                if (!ExpressionEvaluator(ifToken[3, 0], ifToken[3, 1]))
                                    return false;

                                ifResult = ifToken[2, 1].Next!;
                            }

                            /*
                            //// Evaluar y guardar en una variable la parte `body` del `if`
                            //LinkedListNode<IToken>? bodyResult;
                            //int savePrintListCount = PrintList.Count;
                            //if (!ExpressionEvaluator(ifToken[2, 0], ifToken[2, 1]))
                            //    return false;

                            //int saveBodyPrintListCount = PrintList.Count;
                            //bodyResult = ifToken[3, 0].Previous!.Previous;

                            //// Evaluar y guardar en una variable la parte `else` del `if`
                            //LinkedListNode<IToken>? elseResult;
                            //if (!ExpressionEvaluator(ifToken[3, 0], ifToken[3, 1]))
                            //    return false;

                            //int savElsePrintListCount = PrintList.Count;
                            //elseResult = ifToken[0, 1].Previous;

                            //// Crear una variable que guarde el resultado de la evaluacion del `if` completo
                            //LinkedListNode<IToken>? ifResult = ((((BooleanToken)ifToken[1, 0].Next!.Value).TokenValue)) ? bodyResult : elseResult;

                            //if (((BooleanToken)ifToken[1, 0].Next!.Value).TokenValue)
                            //{
                            //    ifResult = bodyResult;

                            //    if (saveBodyPrintListCount != savElsePrintListCount)
                            //        PrintList.RemoveRange(saveBodyPrintListCount, savElsePrintListCount - saveBodyPrintListCount);
                            //}
                            //else
                            //{
                            //    ifResult = elseResult;
                            //    if (savePrintListCount != saveBodyPrintListCount)
                            //        PrintList.RemoveRange(savePrintListCount, saveBodyPrintListCount - savePrintListCount);
                            //}
                            */

                            // Eliminar los token relacionados con el `if` que faltan y quedarse solo con el resultado de la evaluacion
                            while (currentToken.Next.Value != ifToken[0, 1].Value)
                            {
                                TokensLinkedList.Remove(currentToken.Next);
                            }

                            TokensLinkedList.AddAfter(currentToken, ifResult);
                            currentToken = currentToken.Next;
                            TokensLinkedList.Remove(currentToken.Previous);

                            return true;
                        }
                    }
                }

                // Comprobar si el token es una funcion existente y evaluarla
                else if (currentToken.Value is IdentifierToken)
                {
                    if (!EvaluateInleneFunction())
                    {
                        return false;
                    }
                }

                // Comprobar que no falte ningun operador despues de la evaluacion preliminal de algunas expresiones
                if (currentToken.Value is NumberToken or StringToken or BooleanToken &&
                    ((currentToken.Previous is not null && currentToken.Previous.Value is NumberToken or StringToken or BooleanToken) ||
                    (currentToken.Next is not null && currentToken.Next.Value is NumberToken or StringToken or BooleanToken)))
                {
                    AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn(), $"Missing operator between expressions");
                    return false;
                }

                return FillOperatorsQueueAndPreliminarEvaluation(currentToken.Next!, finalToken!);

                bool EvaluateInleneFunction()
                {
                    // Guardar el id y referencia de la funcion
                    IToken function = currentToken.Value;
                    string id = currentToken.Value.GetTokenValueAsString();
                    bool builtInFunction = false;

                    // Comprobar que sea una funcion ya contruida por el sistema
                    if (BuiltInFunctionClass.IsBuilInFunction(id))
                        builtInFunction = true;

                    // Comprobar que sea una funcion declarada por el usuario
                    if (!InlineFunctionClass.ExistFunction(id) && !builtInFunction)
                    {
                        AddErrorToList(ErrorType.Semantic, function.GetColumn(), $"The function or variable `{id}` is not declared");
                        return false;
                    }

                    Update();

                    // Comrpobar el parentesis de apertura
                    if (!(currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == "("))
                    {
                        AddErrorToList(ErrorType.Syntax, function.GetColumn(), $"Expected token `(` after identifier `{id}`");
                        return false;
                    }

                    // Guardar una referencia del parenteis de cierre
                    LinkedListNode<IToken> closeParenthesis = ParenthesisDictionary[currentToken];

                    Update();

                    // Obtener cuantos parametros acepta la funcion
                    int maxParams = (builtInFunction) ? BuiltInFunctionClass.NumberOfParameters(id) : InlineFunctionClass.NumberOfParameters(id);

                    // Crear una lista de tokens para los parametros 
                    LinkedList<IToken>[]? paramsList;

                    // Comprobar si la funcion no posee parametros
                    if ((maxParams == 0 && currentToken != closeParenthesis) || (maxParams != 0 && currentToken == closeParenthesis))
                    {
                        AddErrorToList(ErrorType.Semantic, function.GetColumn(), $"Function `{id}` must receives `{maxParams}` parameter(s)");
                        return false;
                    }
      
                    if (maxParams == 0)
                        paramsList = null;
                    else
                    {
                        // Inicaliar la lista
                        paramsList = new LinkedList<IToken>[maxParams];
                        int index = 0;

                        // Obtener los parametros
                        do
                        {
                            // Salir del bucle si se llega a la cantidad maxima de parametros
                            if (index == maxParams)
                                break;

                            // Inicializar una nueva instacia de la lista con respecto a una posicion 
                            if (paramsList[index] is null)
                                paramsList[index] = new LinkedList<IToken>();

                            // Salir del bucle si se encuetra con el cierre de apertura (funcion llamanda exitosamente) o en EOL (funcion mal llamada)
                            if (currentToken == closeParenthesis || currentToken.Value is EndOfLineToken)
                                break;

                            // Comprobar si exiten mas parametrsos
                            if (currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == ",")
                            {
                                Update();
                                index++;
                                continue;
                            }

                            // Adiocianar a la lista de parametros el token y removerlo de la lista de token 
                            paramsList[index].AddLast(currentToken.Value);
                            currentToken = currentToken.Next!;
                            TokensLinkedList.Remove(currentToken.Previous!);

                        } while (true);

                        // Comprobar que el numero de parametos que fuera indicado correctamente
                        if (index != maxParams - 1)
                        {
                            AddErrorToList(ErrorType.Semantic, function.GetColumn(), $"Function `{id}` must receives `{maxParams}` parameter(s)");
                            return false;
                        }

                        // Comprobar el parentesis de cierre
                        if (!(currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == ")"))
                        {
                            AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `)` after token `{currentToken.Previous!.Value.GetTokenValueAsString()}`");
                            return false;
                        }
                    }

                    Update();

                    // Evaluar la funcion
                    if (builtInFunction)
                    {
                        if (!BuiltInFunctionClass.EvaluateParams(function, paramsList, ErrorList))
                        {
                            return false;
                        }

                        // Adicioonar a la lista de tokens el resultado y remover los parametros de las clases de funciones
                        TokensLinkedList.AddBefore(currentToken, BuiltInFunctionClass.EvaluateFunction(function.GetColumn(), id));
                        BuiltInFunctionClass.RemoveParameterValues();
                    }
                    else
                    {
                        if (!InlineFunctionClass.AddParametersValues(id, paramsList, ErrorList))
                        {
                            return false;
                        }

                        IToken? output;
                        if (!InlineFunctionClass.EvaluateFunction(id, ErrorList, out output))
                        {
                            return false;
                        }

                        // Adicioonar a la lista de tokens el resultado y remover los parametros de las clases de funciones
                        TokensLinkedList.AddBefore(currentToken, output!);
                        InlineFunctionClass.RemoveParameterValues(id);
                    }

                    currentToken = currentToken.Previous!;

                    return true;

                    void Update()
                    {
                        currentToken = currentToken.Next!;
                        TokensLinkedList.Remove(currentToken.Previous!);
                    }
                }

            }

            bool ArimeticAndLogicEvaluator()
            {
                // Comprobar cuantos elementos quedad en la cola y evaluarlos
                
                if (PowOperatorStack.Count != 0)
                    return OperatorsEvaluator(PowOperatorStack.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                if (MultDivOperatorStack.Count != 0)
                    return OperatorsEvaluator(MultDivOperatorStack.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                if (SumRestOpetorStack.Count != 0)
                    return OperatorsEvaluator(SumRestOpetorStack.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                if (ConcatenationOperatorQueue.Count != 0)
                    return OperatorsEvaluator(ConcatenationOperatorQueue.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                if (LogicArimeticOperatorQueue.Count != 0)
                    return OperatorsEvaluator(LogicArimeticOperatorQueue.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                if (NotOperatorStack.Count != 0)
                {
                    if (!OperatorsEvaluator(NotOperatorStack.Last()))
                        return false;

                    NotOperatorStack.RemoveLast();
                    ArimeticAndLogicEvaluator();
                }
                if (LogicBooleanOperatorQueue.Count != 0)
                    return OperatorsEvaluator(LogicBooleanOperatorQueue.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                return true;

            }

            bool OperatorsEvaluator(LinkedListNode<IToken> operador)
            {
                // Comprobar que tipo de operador es el token
                if (operador.Value is ArithmeticOperatorToken)
                {
                    // Comprobar que los tokens del al lado del operador sean de tipo numero
                    if (!CheckPreviusAndNextArimeticToken(operador))
                        return false;

                    // Castear los tokens
                    double result = 0;
                    double leftOperand = ((NumberToken)operador.Previous!.Value).TokenValue;
                    double rightOperand = ((NumberToken)operador.Next!.Value).TokenValue;
                    
                    // Obtener el resultado de la operacion
                    switch (operador.Value.GetTokenValueAsString())
                    {
                        case "+":
                            result = leftOperand + rightOperand;
                            break;
                        case "-":
                            result = leftOperand - rightOperand;
                            break;
                        case "*":
                            result = leftOperand * rightOperand;
                            break;
                        case "/":
                            result = leftOperand / rightOperand;
                            break;
                        case "%":
                            result = leftOperand % rightOperand;
                            break;
                        case "^":
                            result = Math.Pow(leftOperand, rightOperand);
                            break;
                        default:
                            break;
                    }

                    // Adicionar a la lista el resultado
                    TokensLinkedList.AddAfter(operador.Next, new LinkedListNode<IToken>(new NumberToken(operador.Previous!.Value.GetColumn(), result)));
                }
                else if (operador.Value is LogicArimeticOperatorToken)
                {
                    // Comprobar que los tokens del al lado del operador
                    if (!CheckPreviusAndNextArimeticToken(operador))
                        return false;
                    
                    bool result = true;

                    if (operador.Value.GetTokenValueAsString() == "==")
                    {
                        if(operador.Next!.Value is NumberToken)
                        {
                            result = ((NumberToken)operador.Previous!.Value).TokenValue == ((NumberToken)operador.Next!.Value).TokenValue;
                        }
                        else if (operador.Next!.Value is BooleanToken)
                        {
                            result = ((BooleanToken)operador.Previous!.Value).TokenValue == ((BooleanToken)operador.Next!.Value).TokenValue;
                        }
                        else if (operador.Next!.Value is StringToken)
                        {
                            result = ((StringToken)operador.Previous!.Value).StringValue == ((StringToken)operador.Next!.Value).StringValue;
                        }                       
                    }
                    else if (operador.Value.GetTokenValueAsString() == "!=")
                    {
                        if (operador.Next!.Value is NumberToken)
                        {
                            result = ((NumberToken)operador.Previous!.Value).TokenValue != ((NumberToken)operador.Next!.Value).TokenValue;
                        }
                        else if (operador.Next!.Value is BooleanToken)
                        {
                            result = ((BooleanToken)operador.Previous!.Value).TokenValue != ((BooleanToken)operador.Next!.Value).TokenValue;
                        }
                        else if (operador.Next!.Value is StringToken)
                        {
                            result = ((StringToken)operador.Previous!.Value).StringValue != ((StringToken)operador.Next!.Value).StringValue;
                        }
                    }
                    else
                    {
                        if(operador.Previous!.Value is not NumberToken || operador.Next!.Value is not NumberToken)
                        {
                            AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn(), $"Operator `{operador.Value.GetTokenValueAsString()}` can only be used between number expressions");
                            return false;
                        }

                        double leftOperand = ((NumberToken)operador.Previous!.Value).TokenValue;
                        double rightOperand = ((NumberToken)operador.Next!.Value).TokenValue;

                        // Evaluar la operacion
                        switch (operador.Value.GetTokenValueAsString())
                        {
                            case ">":
                                result = leftOperand > rightOperand;
                                break;
                            case ">=":
                                result = leftOperand >= rightOperand;
                                break;
                            case "<":
                                result = leftOperand < rightOperand;
                                break;
                            case "<=":
                                result = leftOperand <= rightOperand;
                                break;
                            default:
                                break;
                        }
                    }

                    // Adicionar a la lista el resultado
                    TokensLinkedList.AddAfter(operador.Next, new LinkedListNode<IToken>(new BooleanToken(operador.Previous!.Value.GetColumn(), result)));

                }       
                else if (operador.Value is LogicBooleanOperatorToken)
                {
                    // Comprobar que los tokens del al lado del operador sean de tipo booleano
                    bool isNotOperator;
                    if (!CheckPreviusAndNextLogicToken(operador, out isNotOperator))
                        return false;

                    // Crear la variable resultado y castear los token
                    bool result = true;
                    bool leftOperand = (isNotOperator) ? true : ((BooleanToken)operador.Previous!.Value).TokenValue;
                    bool rightOperand = ((BooleanToken)operador.Next!.Value).TokenValue;

                    // Evaluar la operacion
                    switch (operador.Value.GetTokenValueAsString())
                    {
                        case "&":
                            result = leftOperand & rightOperand;
                            break;
                        case "|":
                            result = leftOperand | rightOperand;
                            break;
                        case "!":
                            // Como el operador `!` no necesita de un operador izquierdo, se crea uno nuevo como mascara
                            TokensLinkedList.AddBefore(operador, new LinkedListNode<IToken>(new BooleanToken(-1, true)));
                            result = !rightOperand;
                            break;
                        default:
                            break;
                    }

                    // Adiconar a la lista el resultado
                    TokensLinkedList.AddAfter(operador.Next, new LinkedListNode<IToken>(new BooleanToken(operador.Previous!.Value.GetColumn(), result)));
                }
                else if (operador.Value is SpecialOperatorToken && operador.Value.GetTokenValueAsString() == "@")
                {
                    // Comprobar los tipos de los token al lado del operador. El operador `@` admite la concatenacion de string, numbers y booleanos
                    if (!CheckPreviusAndNextConcatenationToken(operador))
                        return false;

                    // Crear las variable de los operandos y guardar su valor como strings 
                    string leftOperand, rightOperand;

                    if (operador.Previous!.Value is StringToken)
                        leftOperand = ((StringToken)operador.Previous!.Value).StringValue;
                    else
                        leftOperand = operador.Previous!.Value.GetTokenValueAsString();

                    if (operador.Next!.Value is StringToken)
                        rightOperand = ((StringToken)operador.Next!.Value).StringValue;
                    else
                        rightOperand = operador.Next!.Value.GetTokenValueAsString();

                    // Adicionar a la lista el resultado de la concatenacion
                    TokensLinkedList.AddAfter(operador.Next, new StringToken(operador.Previous!.Value.GetColumn(), "\"" + leftOperand + rightOperand + "\""));
                }

                // Eliminar de la lista el token operador y los operandos
                TokensLinkedList.Remove(operador.Previous!);
                TokensLinkedList.Remove(operador.Next!);
                TokensLinkedList.Remove(operador);

                return true;

                // Comprobar los operandos de un token aritmetico
                bool CheckPreviusAndNextArimeticToken(LinkedListNode<IToken> operador)
                {
                    // Comprobar si un operador aritmetico se utilice entre dos tipos diferentes
                    if ((operador.Previous is not null && operador.Previous.Value is not SystemToken) && (operador.Next is not null && operador.Next.Value is not SystemToken) &&
                        operador.Previous!.Value.GetType().Name != operador.Next!.Value.GetType().Name)
                    {
                        AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn(), $"Operator `{operador.Value.GetTokenValueAsString()}` cannot be used between two different types");
                        return false;
                    }

                    // Comprobar el operando izquierdo
                    if (operador.Previous is null || operador.Previous.Value is SystemToken)
                    {
                        // Comprobar si el operador es un signo de menos `-` y no halla un operando izquierdo, lo cual significa que es un numero negativo
                        if (operador.Value.GetTokenValueAsString() == "-" && (operador.Next is not null && operador.Next.Value is NumberToken))
                        {
                            // Adicionar un cero `0` en el operando izquierdo
                            TokensLinkedList.AddBefore(operador, new NumberToken(operador.Value.GetColumn() - 1, 0));
                            return true;
                        }

                        AddErrorToList(ErrorType.Syntax, operador.Value.GetColumn(), $"Expected expression before operator `{operador.Value.GetTokenValueAsString()}`");
                        return false;
                    }

                    // Comprobar el operando derecho
                    if (operador.Next is null || operador.Next.Value is SystemToken)
                    {
                        if(operador.Next is not null && operador.Next.Value.GetTokenValueAsString() == "!")
                        {
                            NotOperatorStack.Remove(operador.Next);
                            if (OperatorsEvaluator(operador.Next))
                            {          
                                return true;
                            }
                        }
                        else   
                            AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn(), $"Expected expression after operator  `{operador.Value.GetTokenValueAsString()}`");
                        
                        return false;
                    }

                    return true;
                }

                bool CheckPreviusAndNextLogicToken(LinkedListNode<IToken> operador, out bool IsNotOperator)
                {
                    // Comprobar que el operado sea not `!` para no comprobar la parte izquierda
                    if (operador.Value.GetTokenValueAsString() == "!")
                        IsNotOperator = true;
                    else
                        IsNotOperator = false;

                    // Comprobar el operando izquierdo
                    if (!IsNotOperator && (operador.Previous is null || operador.Previous.Value is not BooleanToken))
                    {
                        AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn(), $"Expected boolean expression before operator `{operador.Value.GetTokenValueAsString()}`");
                        return false;
                    }

                    // Comprobar el operando derecho
                    if (operador.Next is null || operador.Next.Value is not BooleanToken)
                    {
                        AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn(), $"Expected boolean expression after operator `{operador.Value.GetTokenValueAsString()}`");
                        return false;
                    }

                    return true;
                }

                bool CheckPreviusAndNextConcatenationToken(LinkedListNode<IToken> operador)
                {
                    // Comprobar que los operando no sean de tipo null o del sistema
                    if (operador.Previous!.Value is null or SystemToken || operador.Next!.Value is null or SystemToken)
                    {
                        AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn(), $"Operator `@` can only be used between a string, a number or a boolean expression");
                        return false;
                    }

                    return true;
                }
            } 
            
            #endregion
        }

        // Comprobar el resultado de la evaluacion del input
        private void FinalCheck()
        {
            // Comprobar si el input fue solo un token EOL `;`
            if(TokensLinkedList.Count == 1 && TokensLinkedList.Last!.Value is EndOfLineToken)
            {
                Output = null;
                return;
            }

            // Comprobar cuantos tokens quedaron luego de la evaluacion.
            // Para una evaluacion exitosa solo deben quedar en la lista el resultado y el token EOL `;`
            if (TokensLinkedList.Count > 2 || (TokensLinkedList.First!.Value is SystemToken))
            {
                AddErrorToList(ErrorType.Semantic, -1 ,"The input expession cannot be correctly evaluated");
                Output = null;
                return;
            }

            // Asignar el resultado de la evaluacion al output
            Output = TokensLinkedList.First!.Value;
        } 
        #endregion
    }
}

