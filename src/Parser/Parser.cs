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

        private LinkedList<TokenInterface> TokensLinkedList;

        private LinkedList<LinkedListNode<TokenInterface>[,]> IfElseLinkedList;
        private Dictionary<LinkedListNode<TokenInterface>, LinkedListNode<TokenInterface>> ParenthesisDictionary;

        private LetInClass LetInColletion;
        List<CompilingError> ErrorList;

        #endregion

        #region Properties
        public TokenInterface? Output { private set; get; }

        public bool IsThereAnyError { get { return ErrorList.Count != 0; } private set { } }

        #endregion

        #region Constructor

        public Parser(List<TokenInterface> tokensList)
        {
            // Inicializar los campos y las propiedades
            TokensLinkedList = new LinkedList<TokenInterface>(tokensList);
            ErrorList = new List<CompilingError>();

            IfElseLinkedList = new LinkedList<LinkedListNode<TokenInterface>[,]>();
            ParenthesisDictionary = new Dictionary<LinkedListNode<TokenInterface>, LinkedListNode<TokenInterface>>();

            LetInColletion = new LetInClass();

            CheckAndEvaluate();

        }

        #endregion

        #region Auxiliar Metodos 
        public List<CompilingError> GetErrors() => ErrorList;

        private void AddErrorToList(ErrorType type, int column, string argument)
        {
            IsThereAnyError = true;
            ErrorList.Add(new CompilingError(type, column, argument));
        }

        private int SearchNumber(LinkedListNode<TokenInterface> token)
        {
            return (token is null) ? -1 : TokensLinkedList.ToList().IndexOf(token.Value);
        }

        private LinkedListNode<TokenInterface>? SearchOpenParentesis(LinkedListNode<TokenInterface> closeParenthesis)
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
        private void CheckAndEvaluate()
        {

            if (!CheckSyntaxAndSemantic())
                return;

            switch (CheckInlineFunctionDeclaration(TokensLinkedList.First!))
            {
                case -1 or 1:
                    return;
                default:
                    break;
            }

            if (!CheckAndEvaluateLetInExpression(TokensLinkedList.First!))
                return;

            if (!CheckIfElseExpression(TokensLinkedList.First!, 0, null, null))
                return;

            if (!ExpressionEvaluator(TokensLinkedList.First!, TokensLinkedList.Last!))
                return;

            FinalCheck();

        }

        private bool CheckSyntaxAndSemantic()
        {
            CheckEndOfLineToken();
            CheckParenthesis(new Stack<LinkedListNode<TokenInterface>>(), TokensLinkedList.First!);
            CheckTypes(TokensLinkedList.First!);

            if (IsThereAnyError)
                return false;

            return true;

            void CheckEndOfLineToken()
            {
                if (TokensLinkedList.Last() is not EndOfLineToken)
                {
                    AddErrorToList(ErrorType.Syntax, -1,$"Expected token `;` at the end of the line");
                }
            }

            void CheckParenthesis(Stack<LinkedListNode<TokenInterface>> OpenParentesisStack, LinkedListNode<TokenInterface> currentToken)
            {
                if (currentToken is null) // The current token supposed to be is ";"
                {
                    if (OpenParentesisStack.Count == 0)
                        return;
                    else
                    {
                        AddErrorToList(ErrorType.Syntax, OpenParentesisStack.Peek().Value.GetColumn(), "Missing close parenthesis `)`");
                        return;
                    }
                }

                if (currentToken.Value.GetTokenValueAsString() == "(")
                {
                    OpenParentesisStack.Push(currentToken);
                }
                else if (currentToken.Value.GetTokenValueAsString() == ")")
                {
                    LinkedListNode<TokenInterface> start;
                    if (OpenParentesisStack.TryPop(out start))
                    {
                        ParenthesisDictionary.Add(start, currentToken);
                    }
                    else
                    {
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), "Missing open parenthesis `(`");
                        return;
                    }
                }

                CheckParenthesis(OpenParentesisStack, currentToken.Next);
            }

            void CheckTypes(LinkedListNode<TokenInterface> currentToken)
            {
                if (currentToken.Next is null) // The current token supposed to be is ";"
                    return;

                if (currentToken.Value is NumberToken or StringToken or BooleanToken
                    && currentToken.Next!.Value is NumberToken or StringToken or BooleanToken)
                {
                    AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn() , $"Missing operator between `{currentToken.Value.GetTokenValueAsString()}` and `{currentToken.Next.Value.GetTokenValueAsString()}`");
                }

                CheckTypes(currentToken.Next!);
            }
        }

        private int CheckInlineFunctionDeclaration(LinkedListNode<TokenInterface>? currentToken)
        {
            if (currentToken!.Value is EndOfLineToken)
                return 0;

            if (currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "function")
            {
                if (currentToken != TokensLinkedList.First)
                {
                    AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn(), "Function declaration must be a sigle and only line");
                    return -1;
                }

                currentToken = currentToken.Next;

                if (currentToken!.Value is not IdentifierToken)
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected an identifier type token after `{currentToken.Previous!.Value.GetTokenValueAsString()}`");
                    return -1;
                }
                
                string functionName = currentToken.Value.GetTokenValueAsString();
                if (InlineFunctionClass.ExistFunction(functionName))
                {
                    AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn(), $"The function `{functionName}` already exists");
                    return -1;
                }

                currentToken = currentToken.Next;

                if (!(currentToken!.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == "("))
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `(` after identifier `{currentToken.Previous!.Value.GetTokenValueAsString()}`");
                    return -1;
                }

                List<string>? param;

                if (currentToken.Next!.Value is SeparatorToken && currentToken.Next.Value.GetTokenValueAsString() == ")") /// Function without any parameters
                {
                    param = null;
                    currentToken = currentToken.Next!;
                }
                else // Function with parameters
                {
                    param = new List<string>();
                    do
                    {
                        currentToken = currentToken.Next;

                        if (currentToken!.Value is not IdentifierToken)
                        {
                            AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected an identifier type token after `{currentToken.Previous!.Value.GetTokenValueAsString()}`");
                            return -1;
                        }

                        param.Add(currentToken.Value.GetTokenValueAsString());
                        currentToken = currentToken.Next;

                    } while (currentToken!.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == ",");

                    if (!(currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == ")"))
                    {
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `)` after identifier `{currentToken.Previous!.Value.GetTokenValueAsString()}`");
                        return -1;
                    }
                }

                currentToken = currentToken.Next;

                if (!(currentToken!.Value is SpecialOperatorToken && currentToken.Value.GetTokenValueAsString() == "=>"))
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `=>` after token `)`");
                    return -1;
                }

                LinkedList<TokenInterface> body = new LinkedList<TokenInterface>();
                do
                {
                    currentToken = currentToken.Next;
                    body.AddLast(currentToken!.Value);

                } while (currentToken.Value is not EndOfLineToken);


                InlineFunctionClass.AddFunction(functionName);
                InlineFunctionClass.AddParametersNameToLastFunction(param);
                InlineFunctionClass.AddBodyToLastFunction(body);

                return 1;
            }

            return CheckInlineFunctionDeclaration(currentToken.Next);
        }

        private bool CheckAndEvaluateLetInExpression(LinkedListNode<TokenInterface>? currentToken)
        {
            if (currentToken!.Value is EndOfLineToken)
            {
                while (LetInColletion.PeekLastLet() is not null)
                {
                    TokensLinkedList.Remove(LetInColletion.PeekLastLet()!);
                    LetInColletion.RemoveLastLet();
                }

                return true;
            }

            if (currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "let")
            {
                LetInColletion.AddLet(currentToken);

                if (!CheckLetInExpression(currentToken.Next!, out currentToken!))
                    return false;
            }

            if (currentToken.Value is IdentifierToken && LetInColletion.ConstainsVariable(currentToken.Value.GetTokenValueAsString())
                && currentToken.Next!.Value.GetTokenValueAsString() != "(")
            {
                TokensLinkedList.AddAfter(currentToken, LetInColletion.PeekLastValue(currentToken.Value.GetTokenValueAsString()));
                currentToken = currentToken.Next;
                TokensLinkedList.Remove(currentToken!.Previous!);
            }

            if (currentToken.Value.GetTokenValueAsString() == ")")
            {
                if (SearchNumber(SearchOpenParentesis(currentToken)!) < SearchNumber(LetInColletion.PeekLastLet()!))
                {
                    TokensLinkedList.Remove(LetInColletion.PeekLastLet()!);
                    LetInColletion.RemoveLastLet();

                    //if(LetInColletion.PeekLastLet() is null)
                    // return true;
                }
            }

            return CheckAndEvaluateLetInExpression(currentToken.Next);

            bool CheckLetInExpression(LinkedListNode<TokenInterface>? currentToken, out LinkedListNode<TokenInterface>? actualToken)
            {
                actualToken = null;
                string variableName;

                do
                {
                    if (currentToken!.Value.GetTokenValueAsString() == ",")
                        Update();

                    if (currentToken.Value is not IdentifierToken)
                    {
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected an identifier type token");
                        return false;
                    }

                    variableName = currentToken.Value.GetTokenValueAsString();
                    LetInColletion.AddVariableNameToLastLet(variableName);
                    Update();

                    if (!(currentToken.Value is SpecialOperatorToken && currentToken.Value.GetTokenValueAsString() == "="))
                    {
                        AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `=` after identifier `{variableName}`");
                        return false;
                    }

                    Update();

                    TokenInterface? output;
                    if (EvaluateLetVariable(out output))
                    {
                        LetInColletion.AddVariableValue(variableName, new LinkedListNode<TokenInterface>(output));
                    }
                    else
                    {
                        return false;
                    }


                } while (currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == ",");

                if (!(currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "in"))
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), $"Expected token `=` after identifier `{variableName}`");
                    return false;
                }

                Update();
                actualToken = currentToken;
                return true;

                void Update()
                {
                    currentToken = currentToken.Next!;
                    TokensLinkedList.Remove(currentToken.Previous!);

                }

                bool EvaluateLetVariable(out TokenInterface output)
                {
                    List<TokenInterface> tokens = new List<TokenInterface>();
                    string tokenValue;
                    int letCount = 0;

                    do
                    {
                        if (currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "in")
                        {
                            if (letCount == 0)
                                break;
                            else
                                letCount--;
                        }

                        tokens.Add(currentToken.Value);

                        if (currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == "(")
                        {
                            LinkedListNode<TokenInterface> closeParenthesis = ParenthesisDictionary[currentToken];
                            do
                            {
                                Update();
                                tokens.Add(currentToken.Value);

                            } while (currentToken != closeParenthesis);
                        }

                        else if (currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "let")
                        {
                            letCount++;
                        }

                        Update();
                        tokenValue = currentToken.Value.GetTokenValueAsString();

                        // TODO Implementar let sin parentisis

                    } while (tokenValue != "," && tokenValue != ";");

                    tokens.Add(new EndOfLineToken(tokens.Last().GetTokenValueAsString().Length + tokens.Last().GetColumn()));

                    Parser result = new Parser(tokens)!;

                    if (result.IsThereAnyError)
                    {
                        output = null;
                        ErrorList.AddRange(result.GetErrors());
                        return false;
                    }

                    output = result.Output!;
                    return true;

                }
            }

        }

        private bool CheckIfElseExpression(LinkedListNode<TokenInterface>? currentToken, double actualIfPart, LinkedListNode<LinkedListNode<TokenInterface>[,]>? currentIf, LinkedListNode<LinkedListNode<TokenInterface>[,]>? sourceIf)
        {

            if (currentToken!.Value is EndOfLineToken)
            {
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
                        currentIf.Value[3, 1] = currentToken;
                        currentIf.Value[0, 1] = currentToken;

                        if (sourceIf is null)
                            return true;
                        else
                            return CheckIfElseExpression(currentToken, 2.5, sourceIf, null);
                    default:
                        break;
                }
            }

            if (currentToken.Value.GetTokenValueAsString() == "if")
            {
                if (currentIf is not null)
                {
                    if (currentIf.Value[1, 1] is not null)
                    {
                        if (currentIf.Value[2, 0] is null && actualIfPart == 1)
                            currentIf.Value[2, 0] = currentToken;
                        else if (currentIf.Value[3, 0] is null && actualIfPart == 2)
                            currentIf.Value[3, 0] = currentToken;

                    }
                    sourceIf = currentIf;
                }


                IfElseLinkedList.AddLast(new LinkedListNode<LinkedListNode<TokenInterface>[,]>(new LinkedListNode<TokenInterface>[4, 2]));
                currentIf = IfElseLinkedList.Last!;

                // The start if the if
                currentIf!.Value[0, 0] = currentToken;

                // The expresion to evaluate
                currentToken = currentToken.Next;

                if (currentToken!.Value.GetTokenValueAsString() != "(")
                {
                    AddErrorToList(ErrorType.Syntax, currentToken.Value.GetColumn(), "Missing open parenthesis `(` after `if`");
                    return false;
                }

                currentIf.Value[1, 0] = currentToken;

                return CheckIfElseExpression(currentToken.Next, 0.5, currentIf, sourceIf);
            }

            if (actualIfPart == 0.5 && ParenthesisDictionary[currentIf.Value[1, 0]] == currentToken)
            {
                currentIf.Value[1, 1] = currentToken;
                return CheckIfElseExpression(currentToken.Next, 1, currentIf, sourceIf);
            }

            if (actualIfPart == 1)
            {
                if (currentToken.Value.GetTokenValueAsString() == "else")
                {
                    return CheckIfElseExpression(TokensLinkedList.Last, actualIfPart, currentIf, sourceIf);
                }
                else
                {
                    currentIf.Value[2, 0] = currentToken;
                    return CheckIfElseExpression(currentToken.Next, 1.5, currentIf, sourceIf);
                }

            }

            if (currentToken.Value.GetTokenValueAsString() == "else" && actualIfPart == 1.5)
            {
                currentIf.Value[2, 1] = currentToken!;
                return CheckIfElseExpression(currentToken.Next, 2, currentIf, sourceIf);
            }

            if (actualIfPart == 2)
            {
                currentIf.Value[3, 0] = currentToken;
                return CheckIfElseExpression(currentToken.Next, 2.5, currentIf, sourceIf);
            }

            if (actualIfPart == 2.5)
            {
                switch (currentToken.Value.GetTokenValueAsString())
                {
                    case "else":
                        currentIf!.Value[3, 1] = currentToken;
                        currentIf.Value[0, 1] = currentToken;
                        currentIf = sourceIf;
                        actualIfPart = 1.5;
                        currentToken = currentToken.Previous;
                        break;
                    case ")":

                        LinkedListNode<TokenInterface> openParenthesis = new LinkedListNode<TokenInterface>(new SeparatorToken(-1, "("));
                        foreach (var item in ParenthesisDictionary)
                        {
                            if (item.Value == currentToken)
                            {
                                openParenthesis = item.Key;
                                break;
                            }
                        }

                        if (TokensLinkedList.ToList().IndexOf(openParenthesis.Value) < TokensLinkedList.ToList().IndexOf(currentIf.Value[0, 0].Value))
                        {
                            currentIf.Value[3, 1] = currentToken;
                            currentIf.Value[0, 1] = currentToken;

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
                                //currentIf = null;
                            }

                            currentIf = sourceIf;
                            currentToken = currentToken.Previous;
                        }

                        break;
                }
            }

            return CheckIfElseExpression(currentToken!.Next, actualIfPart, currentIf, sourceIf);

        }

        private bool ExpressionEvaluator(LinkedListNode<TokenInterface>? startToken, LinkedListNode<TokenInterface>? endToken)
        {
            Queue<LinkedListNode<TokenInterface>> SumRestOpetorStack = new Queue<LinkedListNode<TokenInterface>>();
            Queue<LinkedListNode<TokenInterface>> MultDivOperatorStack = new Queue<LinkedListNode<TokenInterface>>();
            Queue<LinkedListNode<TokenInterface>> PowOperatorStack = new Queue<LinkedListNode<TokenInterface>>();
            Queue<LinkedListNode<TokenInterface>> LogicArimeticOperatorQueue = new Queue<LinkedListNode<TokenInterface>>();
            Stack<LinkedListNode<TokenInterface>> NotOperatorStack = new Stack<LinkedListNode<TokenInterface>>();
            Queue<LinkedListNode<TokenInterface>> LogicBooleanOperatorQueue = new Queue<LinkedListNode<TokenInterface>>();
            Queue<LinkedListNode<TokenInterface>> ConcatenationOperatorQueue = new Queue<LinkedListNode<TokenInterface>>();

            if (!FillOperatorsStack(startToken, endToken))
                return false;

            if (!ArimeticAndLogicEvaluator())
                return false;

            return true;

            bool FillOperatorsStack(LinkedListNode<TokenInterface> currentToken, LinkedListNode<TokenInterface> finalToken)
            {
                if (currentToken == finalToken || currentToken.Value is EndOfLineToken)
                    return true;

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
                        NotOperatorStack.Push(currentToken);
                    else
                        LogicBooleanOperatorQueue.Enqueue(currentToken);
                }

                else if (currentToken.Value is SpecialOperatorToken && currentToken.Value.GetTokenValueAsString() == "@")
                {
                    ConcatenationOperatorQueue.Enqueue(currentToken);
                }

                else if (currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == "(")
                {
                    if (TokensLinkedList.ToList().IndexOf(ParenthesisDictionary[currentToken].Value) > TokensLinkedList.ToList().IndexOf(finalToken.Value))
                    {
                        AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn(), $" The close parenthesis `)` is outside the limits of evaluation");
                        return false;
                    }

                    if (ExpressionEvaluator(currentToken.Next!, ParenthesisDictionary[currentToken]))
                    {
                        if (finalToken.Next is null && finalToken.Previous is null)
                            finalToken = ParenthesisDictionary[currentToken];

                        if (finalToken == ParenthesisDictionary[currentToken])
                            finalToken = finalToken.Next!;

                        currentToken = currentToken.Next!;
                        TokensLinkedList.Remove(ParenthesisDictionary[currentToken!.Previous!]);
                        TokensLinkedList.Remove(currentToken.Previous!);

                        // TODO VER si en el parentesis no quedo otro token
                    }
                    else
                        return false;
                }

                else if (currentToken.Value is KeywordToken && currentToken.Value.GetTokenValueAsString() == "if")
                {
                    foreach (var ifToken in IfElseLinkedList)
                    {
                        if (ifToken[0, 0] == currentToken)
                        {
                            if (!ExpressionEvaluator(ifToken[1, 0].Next, ifToken[1, 1].Previous))
                                return false;

                            if (ifToken[1, 0].Next!.Value is not BooleanToken)
                            {
                                AddErrorToList(ErrorType.Semantic, ifToken[0, 0].Next!.Value.GetColumn(), "The expression to evaluate in the `if-else` expression is not a boolean type");
                                return false;
                            }

                            LinkedListNode<TokenInterface>? ifResult;
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

                                ifResult = ifToken[0, 1].Previous;
                            }

                            while (true)
                            {
                                if (currentToken.Next!.Value == ifToken[0, 1].Value)
                                {
                                    TokensLinkedList.AddBefore(currentToken, ifResult!);
                                    TokensLinkedList.Remove(currentToken);
                                    currentToken = ifResult!;
                                    break;
                                }

                                TokensLinkedList.Remove(currentToken.Next);
                            }

                            return true;
                        }
                    }
                }

                else if (currentToken.Value is IdentifierToken)
                {
                    if (!EvaluateInleneFunction())
                    {
                        return false;
                    }
                }

                if(currentToken.Value is NumberToken or StringToken or BooleanToken && 
                    ((currentToken.Previous is not null && currentToken.Previous.Value is NumberToken or StringToken or BooleanToken) ||
                    (currentToken.Next is not null && currentToken.Next.Value is NumberToken or StringToken or BooleanToken)))
                {
                    AddErrorToList(ErrorType.Semantic, currentToken.Value.GetColumn(), $"Missing operator");
                    return false;
                }

                return FillOperatorsStack(currentToken.Next!, finalToken!);

                bool EvaluateInleneFunction()
                {
                    TokenInterface function = currentToken.Value;
                    string id = currentToken.Value.GetTokenValueAsString();
                    bool builtInFunction = false;

                    if (BuiltInFunctionClass.IsBuilInFunction(id))
                        builtInFunction = true;

                    if (!InlineFunctionClass.ExistFunction(id) && !builtInFunction)
                    {
                        AddErrorToList(ErrorType.Semantic, function.GetColumn() ,$"The function or variable `{id}` is not declared");
                        return false;
                    }

                    Update();

                    if (!(currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == "("))
                    {
                        AddErrorToList(ErrorType.Syntax, function.GetColumn(), $"Expected token `(` after identifier `{id}`");
                        return false;
                    }

                    LinkedListNode<TokenInterface> closeParenthesis = ParenthesisDictionary[currentToken];

                    Update();

                    int maxParams = (builtInFunction) ? BuiltInFunctionClass.NumberOfParameters(id) : InlineFunctionClass.NumberOfParameters(id);
                    LinkedList<TokenInterface>[]? paramsList;

                    if ( (maxParams == 0 && currentToken != closeParenthesis) || (maxParams != 0 && currentToken == closeParenthesis))
                    {
                        AddErrorToList(ErrorType.Semantic, function.GetColumn(), $"Function `{id}` must receives `{maxParams}` parameter(s)");
                        return false;
                    }

                    if (maxParams == 0)
                        paramsList = null;
                    else
                    {
                        paramsList = new LinkedList<TokenInterface>[maxParams];
                        int index = 0;

                        do
                        {
                            if (index == maxParams)
                                break;

                            if (paramsList[index] is null)
                                paramsList[index] = new LinkedList<TokenInterface>();

                            if (currentToken == closeParenthesis || currentToken.Value is EndOfLineToken)
                                break;

                            if (currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == ",")
                            {
                                Update();
                                index++;
                                continue;
                            }

                            paramsList[index].AddLast(currentToken.Value);
                            currentToken = currentToken.Next!;
                            TokensLinkedList.Remove(currentToken.Previous!);

                        } while (true);

                        if (index != maxParams - 1)
                        {
                            AddErrorToList(ErrorType.Semantic, function.GetColumn(), $"Function `{id}` must receives `{maxParams}` parameter(s)");
                            return false;
                        }

                        //if (!(currentToken.Value is SeparatorToken && currentToken.Value.GetTokenValueAsString() == ")"))
                        //{
                        //    AddErrorToList(ErrorType.Syntax, $"Expected token `)` after token `{currentToken.Previous!.Value.GetTokenValueAsString()}`");
                        //    return false;
                        //}
                    }

                    Update();

                    string error;
                    if (builtInFunction)
                    {
                        if (!BuiltInFunctionClass.EvaluateParams(paramsList, ErrorList))
                        {
                            return false;
                        }

                        TokensLinkedList.AddBefore(currentToken, BuiltInFunctionClass.EvaluateFunction(function.GetColumn(),id));
                        BuiltInFunctionClass.RemoveParameterValues();
                    }
                    else
                    {
                        if (!InlineFunctionClass.AddParametersValues(id, paramsList, ErrorList))
                        {
                            return false;
                        }

                        TokenInterface? output;
                        if (!InlineFunctionClass.EvaluateFunction(id, ErrorList, out output))
                        {
                            return false;
                        }

                        TokensLinkedList.AddBefore(currentToken, output);
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
                if (PowOperatorStack.Count != 0)
                    return OperatorsEvaluator(PowOperatorStack.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                if (MultDivOperatorStack.Count != 0)
                    return OperatorsEvaluator(MultDivOperatorStack.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                if (SumRestOpetorStack.Count != 0)
                    return OperatorsEvaluator(SumRestOpetorStack.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                if (LogicArimeticOperatorQueue.Count != 0)
                    return OperatorsEvaluator(LogicArimeticOperatorQueue.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                if (NotOperatorStack.Count != 0)
                    return OperatorsEvaluator(NotOperatorStack.Pop()) ? ArimeticAndLogicEvaluator() : false;

                if (LogicBooleanOperatorQueue.Count != 0)
                    return OperatorsEvaluator(LogicBooleanOperatorQueue.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                if (ConcatenationOperatorQueue.Count != 0)
                    return OperatorsEvaluator(ConcatenationOperatorQueue.Dequeue()) ? ArimeticAndLogicEvaluator() : false;

                return true;

            }

            bool OperatorsEvaluator(LinkedListNode<TokenInterface> operador)
            {
                if (operador.Value is ArithmeticOperatorToken)
                {
                    bool noth;
                    if (!CheckPreviusAndNextArimeticToken(operador, out noth))
                        return false;

                    double result = 0;      
                    double leftOperand = ((NumberToken)operador.Previous!.Value).TokenValue;
                    double rightOperand = ((NumberToken)operador.Next!.Value).TokenValue;
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

                    TokensLinkedList.AddAfter(operador.Next, new LinkedListNode<TokenInterface>(new NumberToken(operador.Previous!.Value.GetColumn(), result)));
                }
                else if (operador.Value is LogicArimeticOperatorToken)
                {
                    bool stringComparer;
                    if (!CheckPreviusAndNextArimeticToken(operador, out stringComparer))
                        return false;

                    bool result = true;

                    if (stringComparer)
                    {
                        string leftOperand = ((StringToken)operador.Previous!.Value).StringValue;
                        string rightOperand = ((StringToken)operador.Next!.Value).StringValue;

                        switch (operador.Value.GetTokenValueAsString())
                        {
                            case "==":
                                result = leftOperand == rightOperand;
                                break;
                            case "!=":
                                result = leftOperand != rightOperand;
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        double leftOperand = ((NumberToken)operador.Previous!.Value).TokenValue;
                        double rightOperand = ((NumberToken)operador.Next!.Value).TokenValue;

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
                            case "==":
                                result = leftOperand == rightOperand;
                                break;
                            case "!=":
                                result = leftOperand != rightOperand;
                                break;
                            default:
                                break;
                        }
                    }

                    TokensLinkedList.AddAfter(operador.Next, new LinkedListNode<TokenInterface>(new BooleanToken(operador.Previous!.Value.GetColumn(),result)));
                }
                else if (operador.Value is LogicBooleanOperatorToken)
                {
                    bool isNotOperator;

                    if (!CheckPreviusAndNextLogicToken(operador, out isNotOperator))
                        return false;

                    bool result = true;
                    bool leftOperand = (isNotOperator) ? true : ((BooleanToken)operador.Previous!.Value).TokenValue;
                    bool rightOperand = ((BooleanToken)operador.Next!.Value).TokenValue;

                    switch (operador.Value.GetTokenValueAsString())
                    {
                        case "&":
                            result = leftOperand & rightOperand;
                            break;
                        case "|":
                            result = leftOperand | rightOperand;
                            break;
                        case "!":
                            TokensLinkedList.AddBefore(operador, new LinkedListNode<TokenInterface>(new BooleanToken(-1,true)));
                            result = !rightOperand;
                            break;
                        default:
                            break;
                    }

                    TokensLinkedList.AddAfter(operador.Next, new LinkedListNode<TokenInterface>(new BooleanToken(operador.Previous!.Value.GetColumn(), result)));
                }
                else if (operador.Value is SpecialOperatorToken && operador.Value.GetTokenValueAsString() == "@")
                {
                    if (!CheckPreviusAndNextConcatenationToken(operador))
                        return false;

                    string leftOperand, rightOperand;

                    if (operador.Previous!.Value is StringToken)
                        leftOperand = ((StringToken)operador.Previous!.Value).StringValue;
                    else
                        leftOperand = operador.Previous!.Value.GetTokenValueAsString();

                    if (operador.Next!.Value is StringToken)
                        rightOperand = ((StringToken)operador.Next!.Value).StringValue;
                    else
                        rightOperand = operador.Next!.Value.GetTokenValueAsString();

                    TokensLinkedList.AddAfter(operador.Next, new StringToken(operador.Previous!.Value.GetColumn(), "\"" + leftOperand + rightOperand + "\""));
                }

                // Eliminar los tokens
                TokensLinkedList.Remove(operador.Previous!);
                TokensLinkedList.Remove(operador.Next!);
                TokensLinkedList.Remove(operador);

                return true;

                bool CheckPreviusAndNextArimeticToken(LinkedListNode<TokenInterface> operador, out bool stringComparer)
                {
                    stringComparer = false;
                    switch (operador.Value.GetTokenValueAsString())
                    {
                        case "==" or "!=":
                            if (operador.Previous!.Value is StringToken && operador.Next!.Value is StringToken)
                            {
                                stringComparer = true;
                                return true;
                            }
                            break;
                    }

                    if ((operador.Previous is not null && operador.Previous.Value is not SystemToken) && (operador.Next is not null && operador.Next.Value is not SystemToken) &&
                        operador.Previous!.Value.GetType().Name != operador.Next!.Value.GetType().Name)
                    {
                        AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn() ,$"Operator `{operador.Value.GetTokenValueAsString()}` cannot be used between two different types");
                        return false;
                    }

                    if (operador.Previous is null || operador.Previous.Value is not NumberToken)
                    {
                        if (operador.Value.GetTokenValueAsString() == "-" && (operador.Next is not null && operador.Next.Value is NumberToken))
                        {
                            TokensLinkedList.AddBefore(operador, new NumberToken(-1,0));
                            return true;
                        }

                        AddErrorToList(ErrorType.Syntax, operador.Value.GetColumn(), $"Expected number expression before operator `{operador.Value.GetTokenValueAsString()}`");
                        return false;
                    }

                    if (operador.Next is null || operador.Next.Value is not NumberToken)
                    {
                        AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn(), $"Expected number expression after operator  `{operador.Value.GetTokenValueAsString()}`");
                        return false;
                    }

                    return true;
                }

                bool CheckPreviusAndNextLogicToken(LinkedListNode<TokenInterface> operador, out bool IsNotOperator)
                {
                    if (operador.Value.GetTokenValueAsString() == "!")
                        IsNotOperator = true;
                    else
                        IsNotOperator = false;

                    if (!IsNotOperator && (operador.Previous is null || operador.Previous.Value is not BooleanToken))
                    {
                        AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn(), $"Expected boolean expression before operator `{operador.Value.GetTokenValueAsString()}`");
                        return false;
                    }

                    if (operador.Next is null || operador.Next.Value is not BooleanToken)
                    {
                        AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn(), $"Expected boolean expression after operator `{operador.Value.GetTokenValueAsString()}`");
                        return false;
                    }

                    return true;
                }

                bool CheckPreviusAndNextConcatenationToken(LinkedListNode<TokenInterface> operador)
                {
                    if (operador.Previous!.Value is null or SystemToken || operador.Next!.Value is null or SystemToken)
                    {
                        AddErrorToList(ErrorType.Semantic, operador.Value.GetColumn(), $"Operator `@` can only be used between a string, a number or a boolean expression");
                        return false;
                    }

                    return true;
                }
            }
        }

        private void FinalCheck()
        {
            if (TokensLinkedList.ToList().TrueForAll(i => i is EndOfLineToken))
            {
                Output = null;
                return;
            }

            if (TokensLinkedList.Count > 2 || (TokensLinkedList.First!.Value is SystemToken))
            {
                AddErrorToList(ErrorType.Semantic, -1 ,"The input expession cannot be correctly evaluated");
                Output = null;
                return;
            }

            Output = TokensLinkedList.First!.Value;
        } 
        #endregion
    }
}

