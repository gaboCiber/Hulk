using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hulk.src
{
    internal class Function
    {
        internal string Identifier { get; set; }

        internal List<string>? ParamsName { get; set; }

        internal Dictionary<string, Stack<LinkedListNode<TokenInterface>>> Params { get; set; }

        internal LinkedList<TokenInterface> Body { get; set; }

        public Function(string name)
        {
            Identifier = name;
            ParamsName = new List<string>();
            Params = new Dictionary<string, Stack<LinkedListNode<TokenInterface>>>();
            Body = new LinkedList<TokenInterface>();
        }

        public LinkedList<TokenInterface> GetBody()
        {
            return new LinkedList<TokenInterface>(Body);
        }

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

            if (isNull)
                Params = new Dictionary<string, Stack<LinkedListNode<TokenInterface>>>();
        }

    }
    
    internal static class InlineFunctionClass
    {
        static Dictionary<string, Function> functions;
        static int number;

        static InlineFunctionClass()
        {
            functions = new Dictionary<string, Function>();
        }

        public static void AddFunction(string name)
        {
            functions.Add(name, new Function(name));
        }

        public static void AddParametersNameToLastFunction( List<string>? param)
        {
            functions.Last().Value.ParamsName = param;
        }

        public static void AddBodyToLastFunction(LinkedList<TokenInterface> body)
        {
            functions.Last().Value.Body = body;
        }

        //--------------------------------------------------------------

        public static bool ExistFunction(string identifier)
        {
            return functions.ContainsKey(identifier);
        }

        public static bool CheckNumberOfParameters(string identifier ,int numberOfParameters)
        {
            return numberOfParameters == NumberOfParameters(identifier);
        }
        
        public static int NumberOfParameters(string identifier)
        {
            if (functions.ContainsKey(identifier))
                if (functions[identifier].ParamsName is null)
                    return 0;
                else
                    return functions[identifier].ParamsName!.Count;
            else
                return -1;

        }

        public static bool AddParametersValues(string identifier, LinkedList<TokenInterface>[]? parameters, List<CompilingError> errorList)
        {
            if (parameters is null)
                return true;

            int index = 0;
            foreach (var item in parameters)
            {
                item.AddLast(new EndOfLineToken());
                Parser result = new Parser(item.ToList());

                if (result.IsThereAnyError)
                {
                    errorList = result.GetErrors();
                    return false;
                }

                string paramertValue = functions[identifier].ParamsName![index];

                if (!functions[identifier].Params.ContainsKey(paramertValue))
                {
                    functions[identifier].Params.Add(paramertValue, new Stack<LinkedListNode<TokenInterface>>());
                }

                functions[identifier].Params[paramertValue].Push(new LinkedListNode<TokenInterface>(result.Output!));
                index++;
            }

            return true;
           
        }

        public static bool EvaluateFunction(string identifier, List<CompilingError> errorList, out TokenInterface? output)
        {
            LinkedList<TokenInterface> bodyCopy = functions[identifier].GetBody();            
            
            if(NumberOfParameters(identifier) > 0)
                GetValues(bodyCopy.First);

            Parser result = new Parser(bodyCopy.ToList());

            if (result.IsThereAnyError)
            {
                errorList.AddRange(result.GetErrors());
                output = null;
                return false;
            }

            output = result.Output;
            return true;

            void GetValues(LinkedListNode<TokenInterface>? currentToken)
            {
                if (currentToken!.Value is EndOfLineToken)
                    return;

                if(currentToken.Value is IdentifierToken && functions[identifier].Params.ContainsKey(currentToken.Value.GetTokenValueAsString()))
                {
                    TokenInterface value = functions[identifier].Params[currentToken.Value.GetTokenValueAsString()].Peek().Value;
                    bodyCopy.AddAfter(currentToken, new LinkedListNode<TokenInterface>(value)!);
                    currentToken = currentToken.Next;
                    bodyCopy.Remove(currentToken!.Previous!);
                }

                GetValues(currentToken.Next);

            }
        }

        public static void RemoveParameterValues(string identifier)
        {
            functions[identifier].RemoveParams();
        }
    }

    internal static class BuiltInFunctionClass
    {
        enum Function { sqrt, sin, cos, exp, log, rand, print, exit}

        static List<TokenInterface> EvaluatedParamsList;

        static BuiltInFunctionClass()
        {
            EvaluatedParamsList = new List<TokenInterface>();
        }

        public static bool IsBuilInFunction(string id)
        {
            return Enum.IsDefined(typeof(Function), id);
        }

        public static int NumberOfParameters(string id)
        {
            switch (Enum.Parse<Function>(id))
            {
                case Function.sqrt or Function.sin or  Function.cos or Function.exp or Function.print:
                    return 1;
                case Function.log:
                    return 2;
                case Function.rand or Function.exit:
                    return 0;
                default:
                    return -1;
            }
        }

        public static TokenInterface EvaluateFunction(string id)
        {                 
            switch (Enum.Parse<Function>(id))
            {
                case Function.sqrt:
                    return new NumberToken(Math.Sqrt(((NumberToken)EvaluatedParamsList[0]).TokenValue));
                case Function.sin:
                    return new NumberToken(Math.Sin(((NumberToken)EvaluatedParamsList[0]).TokenValue));
                case Function.cos:
                    return new NumberToken(Math.Cos(((NumberToken)EvaluatedParamsList[0]).TokenValue));
                case Function.exp:
                    return new NumberToken(Math.Exp(((NumberToken)EvaluatedParamsList[0]).TokenValue));
                case Function.log:
                    return new NumberToken(Math.Log( ((NumberToken)EvaluatedParamsList[1]).TokenValue, ((NumberToken)EvaluatedParamsList[0]).TokenValue));
                case Function.rand:
                    return new NumberToken(new Random().NextDouble());
                case Function.print:
                    if(EvaluatedParamsList[0] is EndOfLineToken)
                        Console.WriteLine();
                    else 
                        Console.WriteLine(EvaluatedParamsList[0].GetTokenValueAsString());
                    return EvaluatedParamsList[0];
                case Function.exit:
                    Environment.Exit(0);
                    return new NumberToken(double.NaN);
                default:
                    return new NumberToken(double.NaN);
            }
        }

        public static bool EvaluateParams(LinkedList<TokenInterface>[]? paramsList, List<CompilingError> errorList)
        {

            if (paramsList is null)
                return true;

            foreach (var item in paramsList)
            {
                item.AddLast(new EndOfLineToken());
                Parser result = new Parser(item.ToList());

                if (result.IsThereAnyError)
                {
                    errorList.AddRange(result.GetErrors());
                    return false;
                }

                EvaluatedParamsList.Add(result.Output!);
            }

            return true;
        }

        public static void RemoveParameterValues()
        {
            EvaluatedParamsList = new List<TokenInterface>();
        }

    }
}
