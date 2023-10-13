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

        private void AddLexicalErrorToList(string argument)
        {
            ErrorList.Add(new CompilingError(ErrorType.Lexical, argument));
        }

        private void CreateTokens(string input)
        {
            StringBuilder preToken = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                if (SeparatorCharacters.Contains(input[i]))
                {
                    TokenList.Add(new SeparatorToken(input[i].ToString()));
                }
                else if (ArithmeticOperatorCharacters.Contains(input[i]))
                {
                    if (i + 1 != input.Length && input[i] == '=' && input[i + 1] == '>')
                    {
                        TokenList.Add(new OperatorToken("=>"));
                        i++;
                    }
                    else
                        TokenList.Add(new ArithmeticOperatorToken(input[i].ToString()));
                }
                else if (LogicOperatorsCharacters.Contains(input[i]))
                {
                    if (input[i] == '&' || input[i] == '|')
                        TokenList.Add(new LogicBooleanOperatorToken(input[i].ToString()));
                    else if (input[i] == '!' && (i == input.Length || input[i + 1] != '='))
                        TokenList.Add(new LogicBooleanOperatorToken(input[i].ToString()));
                    else if (i + 1 != input.Length && input[i + 1] == '=')
                    {
                        TokenList.Add(new LogicArimeticOperatorToken(input[i].ToString() + "="));
                        i++;
                    }
                    else
                        TokenList.Add(new LogicArimeticOperatorToken(input[i].ToString()));
                }
                else if (SpecialCharacters.Contains(input[i]))
                {
                    if (input[i] == ';')
                        TokenList.Add(new EndOfLineToken());
                    else if (input[i] == '@')
                        TokenList.Add(new SpecialOperatorToken("@"));
                    else if (input[i] == '=')
                    {
                        if (i + 1 != input.Length && input[i + 1] == '=')
                        {
                            TokenList.Add(new LogicArimeticOperatorToken("=="));
                            i++;
                        }
                        else if (i + 1 != input.Length && input[i + 1] == '>')
                        {
                            TokenList.Add(new SpecialOperatorToken("=>"));
                            i++;
                        }
                        else
                            TokenList.Add(new SpecialOperatorToken("="));
                    }
                }
                else if (char.IsDigit(input[i]))
                {
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
                        TokenList.Add(new NumberToken(number));
                    else
                        AddLexicalErrorToList($"Invalid token {number}");

                    preToken.Clear();
                    i--;
                }
                else if (input[i] == '"')
                {
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
                        AddLexicalErrorToList($"Invalid token {preToken}. Expected double-quotes `\"`");
                    else
                        TokenList.Add(new StringToken(preToken.ToString()));

                    preToken.Clear();

                }
                else if (input[i] == ' ')
                {
                    continue;
                }
                else
                {
                    bool invalidToken = false;
                    while (i != input.Length && input[i] != ' ')
                    {
                        if (SeparatorCharacters.Contains(input[i]) || ArithmeticOperatorCharacters.Contains(input[i]) || LogicOperatorsCharacters.Contains(input[i]) || SpecialCharacters.Contains(input[i]))
                            break;

                        if (!char.IsLetter(input[i]))
                            invalidToken = true;

                        preToken.Append(input[i]);
                        i++;
                    }

                    string preTokenSTR = preToken.ToString();

                    if (invalidToken)
                        AddLexicalErrorToList($"Invalid token {preTokenSTR}");
                    else if (KeywordCharacters.Contains(preToken.ToString()))
                        TokenList.Add(new KeywordToken(preTokenSTR));
                    else if (preTokenSTR == "true" || preTokenSTR == "false")
                        TokenList.Add(new BooleanToken(bool.Parse(preTokenSTR)));
                    else if (preTokenSTR == "PI")
                        TokenList.Add(new NumberToken(Math.PI));
                    else if (preTokenSTR == "E")
                        TokenList.Add(new NumberToken(Math.E));
                    else
                        TokenList.Add(new IdentifierToken(preTokenSTR));

                    preToken.Clear();
                    i--;
                }

            }
        }
    }
}
