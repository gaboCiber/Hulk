using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hulk.src
{
    internal class Function
    {
        // Nombre de la function
        internal string Identifier { get; set; }

        // Lista que contiene los nombres de las variables
        internal List<string>? ParamsName { get; set; }

        /* Diccionario que tiene como:
            * Key: el nombre de la variable
            * Value: una pila con los valores de la variable
         */
        internal Dictionary<string, Stack<LinkedListNode<IToken>>> Params { get; set; }

        // Una lista enlazable que contiene el cuerpo de la funcion
        internal LinkedList<IToken> Body { get; set; }

        public Function(string name)
        {
            // Inicializar las compos 
            Identifier = name;
            ParamsName = new List<string>();
            Params = new Dictionary<string, Stack<LinkedListNode<IToken>>>();
            Body = new LinkedList<IToken>();
        }

        // Devolver una copia cuerpo
        public LinkedList<IToken> GetBody()
        {
            return new LinkedList<IToken>(Body);
        }

        // Remover los valores de los parametros cuando sale de la funcion
        public void RemoveParams()
        {
            bool isNull = false;
            foreach (var item in Params)
            {
                if (item.Value.Peek() is null)
                {
                    isNull = true;
                    break;
                }

                item.Value.Pop();
            }

            // Si la pila esta vacia inicializar de nuevo el diccionario
            if (isNull)
                Params = new Dictionary<string, Stack<LinkedListNode<IToken>>>();
        }

    }
    
    internal static class InlineFunctionClass
    {
        #region Propiedad y contructor
        // Diccionario que enlaza los nombres de la funciones con su respectivo obejto Function
        static Dictionary<string, Function> functions;

        static InlineFunctionClass()
        {
            // Inizilaizar el diccionario
            functions = new Dictionary<string, Function>();
        } 
        #endregion

        #region Metodos relacionados con la declaracion de funciones

        // Adicionar una funcion
        public static void AddFunction(string name)
        {
            functions.Add(name, new Function(name));
        }

        // Adicionar los parametros a la ultima funcion añadida
        public static void AddParametersNameToLastFunction(List<string>? param)
        {
            functions.Last().Value.ParamsName = param;
        }

        // Adicionar el cuerpo a la ultima funcion añadida
        public static void AddBodyToLastFunction(LinkedList<IToken> body)
        {
            functions.Last().Value.Body = body;
        }

        #endregion

        #region Metodos relacionados con la comprobacion de funciones
        
        // Comprobar si una funcion existe
        public static bool ExistFunction(string identifier)
        {
            return functions.ContainsKey(identifier);
        }

        // Comprobar si una funcion posee una cantidad de parametros
        public static bool CheckNumberOfParameters(string identifier, int numberOfParameters)
        {
            return numberOfParameters == NumberOfParameters(identifier);
        }

        // Comprobar cuantos parametros posee una funcion
        public static int NumberOfParameters(string identifier)
        {
            if (functions.ContainsKey(identifier)) // La funcion existe
                if (functions[identifier].ParamsName is null)
                    return 0;
                else
                    return functions[identifier].ParamsName!.Count;
            else // La funcion no exite
                return -1;

        }

        #endregion

        #region Metodos relacionados con la evaluacion de funciones
        
        // Adicionar los valores a los parametros a la funcion
        public static bool AddParametersValues(string identifier, LinkedList<IToken>[]? parameters, List<CompilingError> errorList)
        {
            if (parameters is null)
                return true;

            // Recorrer la lista de parametros, evaluarlos y adicionarlos a la funcion
            int index = 0;
            foreach (var item in parameters)
            {
                // Añadir un token EOL al final de la lista del cuerpo del parametro
                item.AddLast(new EndOfLineToken(item.Last!.Value.GetColumn() + item.Last.Value.GetTokenValueAsString().Length));
                
                // Evaluar el parametro
                Parser result = new Parser(item.ToList());

                // Comprobrar errores
                if (result.IsThereAnyError)
                {
                    errorList = result.GetErrors();
                    return false;
                }
                
                // Guardar el nombre de la funcion
                string paramertValue = functions[identifier].ParamsName![index];

                // Comprobar si el parametro existe dentro diccionario de la clase Function
                if (!functions[identifier].Params.ContainsKey(paramertValue))
                {
                    functions[identifier].Params.Add(paramertValue, new Stack<LinkedListNode<IToken>>());
                }

                // Adicionar un valor al parametro
                functions[identifier].Params[paramertValue].Push(new LinkedListNode<IToken>(result.Output!));
                index++;
            }

            return true;

        }

        // Evaluar el cuepo de la funcion
        public static bool EvaluateFunction(string identifier, List<CompilingError> errorList, out IToken? output)
        {
            // Crear una copia del cuerpo de la funcion
            LinkedList<IToken> bodyCopy = functions[identifier].GetBody();

            // Comprobar el numero de parametros
            if (NumberOfParameters(identifier) > 0)
                GetValues(bodyCopy.First); // Reemplazar los parametros por sus respectivos valores

            // Evaluar el cuerpo
            Parser result = new Parser(bodyCopy.ToList());

            // Comprobar errores
            if (result.IsThereAnyError)
            {
                errorList.AddRange(result.GetErrors());
                output = null;
                return false;
            }

            // Devolver el resultado
            output = result.Output;
            return true;

            // Funcion local destinada a remplazar los parametros por sus respectivos valores
            void GetValues(LinkedListNode<IToken>? currentToken)
            {
                if (currentToken!.Value is EndOfLineToken)
                    return;

                if (currentToken.Value is IdentifierToken && functions[identifier].Params.ContainsKey(currentToken.Value.GetTokenValueAsString()))
                {
                    IToken value = functions[identifier].Params[currentToken.Value.GetTokenValueAsString()].Peek().Value;
                    bodyCopy.AddAfter(currentToken, new LinkedListNode<IToken>(value)!);
                    currentToken = currentToken.Next;
                    bodyCopy.Remove(currentToken!.Previous!);
                }

                GetValues(currentToken.Next);

            }
        }

        // Remover los valores de los parametros de la funcion
        public static void RemoveParameterValues(string identifier)
        {
            functions[identifier].RemoveParams();
        } 
        #endregion
    }

    internal static class BuiltInFunctionClass
    {
        #region Propiedades y Constructor
        // Enumeracion que posee los tipos de funciones `buil-in` del sistema
        enum Function { sqrt, sin, cos, exp, log, rand, print, exit }

        // Lista que contiene los valores de los parametros evaluados
        static List<IToken> EvaluatedParamsList;

        static BuiltInFunctionClass()
        {
            EvaluatedParamsList = new List<IToken>();
        }

        #endregion

        #region Metodos relacionados con la comprobacion de funciones
        // Comprobar si la funcion `buil-in` existe
        public static bool IsBuilInFunction(string id)
        {
            return Enum.IsDefined(typeof(Function), id);
        }

        // Comprobar el numero de parametros
        public static int NumberOfParameters(string id)
        {
            switch (Enum.Parse<Function>(id))
            {
                case Function.sqrt or Function.sin or Function.cos or Function.exp or Function.print:
                    return 1;
                case Function.log:
                    return 2;
                case Function.rand or Function.exit:
                    return 0;
                default:
                    return -1;
            }
        } 
        #endregion

        #region Metodos relacionados con la evaluacion de funciones

        // Evaluar la funcion
        public static IToken EvaluateFunction(int col, string id)
        {
            // Determinar el tipo de funcion
            switch (Enum.Parse<Function>(id))
            {
                case Function.sqrt:
                    return new NumberToken(col, Math.Sqrt(((NumberToken)EvaluatedParamsList[0]).TokenValue));
                case Function.sin:
                    return new NumberToken(col, Math.Sin(((NumberToken)EvaluatedParamsList[0]).TokenValue));
                case Function.cos:
                    return new NumberToken(col, Math.Cos(((NumberToken)EvaluatedParamsList[0]).TokenValue));
                case Function.exp:
                    return new NumberToken(col, Math.Exp(((NumberToken)EvaluatedParamsList[0]).TokenValue));
                case Function.log:
                    return new NumberToken(col, Math.Log(((NumberToken)EvaluatedParamsList[1]).TokenValue, ((NumberToken)EvaluatedParamsList[0]).TokenValue));
                case Function.rand:
                    return new NumberToken(col, new Random().NextDouble());
                case Function.print:

                    Console.ForegroundColor = ConsoleColor.Magenta;

                    if (EvaluatedParamsList[0] is EndOfLineToken) // Imprir una lina vacia si la funcion print no recibio parametros
                        Console.WriteLine();
                    else if (EvaluatedParamsList[0] is StringToken)
                        Console.WriteLine(((StringToken)EvaluatedParamsList[0]).StringValue);
                    else
                        Console.WriteLine(EvaluatedParamsList[0].ToString()!);

                    Console.ForegroundColor = ConsoleColor.White;

                    return EvaluatedParamsList[0];
                case Function.exit:
                    Environment.Exit(0);
                    return null;
                default:
                    return null!;
            }
        }

        // Evaluar los parametros
        public static bool EvaluateParams(IToken function, LinkedList<IToken>[]? paramsList, List<CompilingError> errorList)
        {

            if (paramsList is null)
                return true;

            // Iterar por la lista
            foreach (var item in paramsList)
            {
                // Añadir un token EOL al final de la lista del cuerpo del parametro
                item.AddLast(new EndOfLineToken(item.Last!.Value.GetColumn() + item.Last.Value.GetTokenValueAsString().Length));

                // Evaluar el parametro
                Parser result = new Parser(item.ToList());

                // Comprobar errores
                if (result.IsThereAnyError)
                {
                    errorList.AddRange(result.GetErrors());
                    return false;
                }

                // Comprobar el tipo del resultado en dependencia de la funcion
                switch (Enum.Parse<Function>(function.GetTokenValueAsString()))
                {
                    case Function.sqrt or Function.sin or Function.cos or Function.exp or Function.log:
                        if (result.Output is not NumberToken)
                        {
                            errorList.Add(new CompilingError(ErrorType.Semantic, function.GetColumn(), $"The function `{function.GetTokenValueAsString()}` must receives a numeric expression in the parameter(s)"));
                            return false;
                        }
                        break;
                    default:
                        break;
                }

                EvaluatedParamsList.Add(result.Output!);
            }

            return true;
        }

        // Remover los parametros
        public static void RemoveParameterValues()
        {
            EvaluatedParamsList = new List<IToken>();
        } 

        #endregion

    }
}
