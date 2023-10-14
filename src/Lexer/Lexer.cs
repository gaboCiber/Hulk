using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hulk.src
{
    public class Lexer
    {
        List<char> SpecialCharacters = new List<char>(new char[] { ';', '=', '@' });
        List<char> SeparatorCharacters = new List<char>(new char[] { '(', ')', ',' });
        List<char> ArithmeticOperatorCharacters = new List<char>(new char[] { '+', '-', '*', '/', '%', '^' });
        List<char> LogicOperatorsCharacters = new List<char>(new char[] { '>', '<', '!', '|', '&' });
        List<string> KeywordCharacters = new List<string>(new string[] { "if", "then", "else", "let", "in", "function" });

        List<TokenInterface> TokenList;
        List<CompilingError> ErrorList;

        public Lexer(string input)
        {
            TokenList = new List<TokenInterface>();
            ErrorList = new List<CompilingError>();
            CreateTokens(input);

            // TODO Implementar multiples comandos en una misma linea
        }

        public bool IsThereAnyLexicalError { get => ErrorList.Count != 0; }

        public List<TokenInterface> GetTokens() => TokenList;

        public List<CompilingError> GetErrors() => ErrorList;

        private void AddLexicalErrorToList(int column, string argument)
        {
            ErrorList.Add(new CompilingError(ErrorType.Lexical, column, argument));
        }

        private void CreateTokens(string input)
        {
            StringBuilder preToken = new StringBuilder();

            for (int i = 0, col = 1; i < input.Length; i++, col=i+1)
            {
                if (SeparatorCharacters.Contains(input[i]))
                {
                    TokenList.Add(new SeparatorToken(col, input[i].ToString()));
                }
                else if (ArithmeticOperatorCharacters.Contains(input[i]))
                {
                    TokenList.Add(new ArithmeticOperatorToken(col, input[i].ToString()));
                }
                else if (LogicOperatorsCharacters.Contains(input[i]))
                {
                    if (input[i] == '&' || input[i] == '|')
                        TokenList.Add(new LogicBooleanOperatorToken(col, input[i].ToString()));
                    else if (input[i] == '!' && (i == input.Length || input[i + 1] != '='))
                        TokenList.Add(new LogicBooleanOperatorToken(col, input[i].ToString()));
                    else if (i + 1 != input.Length && input[i + 1] == '=')
                    {
                        TokenList.Add(new LogicArimeticOperatorToken(col, input[i].ToString() + "="));
                        i++;
                    }
                    else
                        TokenList.Add(new LogicArimeticOperatorToken(col, input[i].ToString()));
                }
                else if (SpecialCharacters.Contains(input[i]))
                {
                    if (input[i] == ';')
                        TokenList.Add(new EndOfLineToken(col));
                    else if (input[i] == '@')
                        TokenList.Add(new SpecialOperatorToken(col, "@"));
                    else if (input[i] == '=')
                    {
                        if (i + 1 != input.Length && input[i + 1] == '=')
                        {
                            TokenList.Add(new LogicArimeticOperatorToken(col, "=="));
                            i++;
                        }
                        else if (i + 1 != input.Length && input[i + 1] == '>')
                        {
                            TokenList.Add(new SpecialOperatorToken(col, "=>"));
                            i++;
                        }
                        else
                            TokenList.Add(new SpecialOperatorToken(col, "="));
                    }
                }
                else if (char.IsDigit(input[i]))
                {
                    int firstCharCol = col;

                    if (TokenList.Count >= 1)
                    {
                        if (TokenList[TokenList.Count - 1] is ArithmeticOperatorToken && ((ArithmeticOperatorToken)TokenList[TokenList.Count - 1]).TokenValue == "-")
                        {
                            preToken.Append("-");

                            if (TokenList.Count >= 2 && (TokenList[TokenList.Count - 2] is NumberToken or IdentifierToken || TokenList[TokenList.Count - 2].GetTokenValueAsString() == ")"))
                                ((ArithmeticOperatorToken)TokenList[TokenList.Count - 1]).ChangeValue("+");
                            else
                                TokenList.RemoveAt(TokenList.Count - 1);
                        }
                    }

                    while (i != input.Length && input[i] != ' ')
                    {
                        if (SeparatorCharacters.Contains(input[i]) || ArithmeticOperatorCharacters.Contains(input[i]) || LogicOperatorsCharacters.Contains(input[i]) || SpecialCharacters.Contains(input[i]))
                            break;

                        preToken.Append(input[i]);
                        i++;
                    }

                    double number;
                    if (double.TryParse(preToken.ToString(), out number))
                        TokenList.Add(new NumberToken(firstCharCol, number));
                    else
                        AddLexicalErrorToList(firstCharCol, $"Invalid token `{preToken}`");

                    preToken.Clear();
                    i--;
                }
                else if (input[i] == '"')
                {
                    int firstCharCol = col;
                    preToken.Append(input[i]);
                    i++;

                    while (true)
                    {
                        if (i == input.Length)
                            break;

                        if (input[i] == '"')
                        {
                            preToken.Append(input[i]);
                            break;
                        }

                        if (input[i] == '\\' && i < input.Length - 1)
                        {
                            switch (input[i + 1])
                            {
                                case '\"':
                                    preToken.Append("\"");
                                    i += 1;
                                    break;
                                case 'n':
                                    preToken.Append("\n");
                                    i += 1;
                                    break;
                                case 't':
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

                    if (preToken[preToken.Length - 1] != '\"')
                        AddLexicalErrorToList(firstCharCol, $"Invalid token {preToken}. Expected double-quotes `\"`");
                    else
                        TokenList.Add(new StringToken(firstCharCol, preToken.ToString()));

                    preToken.Clear();

                }
                else if (input[i] == ' ')
                {
                    continue;
                }
                else
                {
                    int firstCharCol = col;
                    bool invalidToken = false;

                    while (i != input.Length && input[i] != ' ')
                    {
                        if (SeparatorCharacters.Contains(input[i]) || ArithmeticOperatorCharacters.Contains(input[i]) || LogicOperatorsCharacters.Contains(input[i]) || SpecialCharacters.Contains(input[i]))
                            break;

                        if (!char.IsLetterOrDigit(input[i] ))
                            invalidToken = true;

                        preToken.Append(input[i]);
                        i++;
                    }

                    string preTokenSTR = preToken.ToString();

                    if (invalidToken)
                        AddLexicalErrorToList(firstCharCol, $"Invalid token {preTokenSTR}");
                    else if (KeywordCharacters.Contains(preToken.ToString()))
                        TokenList.Add(new KeywordToken(firstCharCol,preTokenSTR));
                    else if (preTokenSTR == "true" || preTokenSTR == "false")
                        TokenList.Add(new BooleanToken(firstCharCol ,bool.Parse(preTokenSTR)));
                    else if (preTokenSTR == "PI")
                        TokenList.Add(new NumberToken(firstCharCol, Math.PI));
                    else if (preTokenSTR == "E")
                        TokenList.Add(new NumberToken(firstCharCol, Math.E));
                    else
                        TokenList.Add(new IdentifierToken(firstCharCol, preTokenSTR));

                    preToken.Clear();
                    i--;
                }

            }
        }
    }
}
