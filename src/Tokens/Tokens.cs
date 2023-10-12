using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hulk
{
    public enum TokenType { Number, String, Boolean, System }
    public enum SystemType { Identifier, Keyword, Separator, Operator, LogicOperator, EndOfLine }
    public enum ErrorType { Lexical, Semantic, Syntax}

    public interface TokenInterface
    {
        public string GetTokenValueAsString();
    }

    public class Tokens<T>: TokenInterface
    {
        public virtual T? TokenValue { private set; get; }
        
        public TokenType TokenType { private set; get; }

        public Tokens(TokenType type, T value)
        {
            this.TokenValue = value;
            this.TokenType = type;
        }

        public override string ToString()
        {
            return $"Token: {this.TokenType} \t Value: {this.TokenValue}";
        }

        public void ChangeValue(T newValue)
        {
            this.TokenValue = newValue;
        }

        public string GetTokenValueAsString()
        {
            return TokenValue.ToString();
        }
    }

    internal class NumberToken : Tokens<double>
    {
        public override double TokenValue => base.TokenValue;

        public NumberToken(double value) : base(TokenType.Number, value) { }

        public override string ToString()
        {
            return TokenValue.ToString();
        }
    }

    internal class StringToken : Tokens<string>
    {
        public override string? TokenValue => base.TokenValue;

        public string StringValue { get; private set; }

        public StringToken(string value) : base(TokenType.String, value) 
        {
            StringValue = TokenValue.Substring(1, TokenValue.Length - 2);
        }

        public override string ToString()
        {
            return StringValue;
        }
    }

    internal class BooleanToken : Tokens<bool>
    {
        public override bool TokenValue => base.TokenValue;

        public BooleanToken(bool value) : base(TokenType.Boolean, value) { }

        public override string ToString()
        {
            return TokenValue.ToString();
        }
    }

    internal class SystemToken : Tokens<string>
    {
        public override string? TokenValue => base.TokenValue;

        public SystemType SystemType { private set; get; }

        public SystemToken(SystemType type, string value) : base(TokenType.System, value)
        {
            this.SystemType = type;
        }

        public override string ToString()
        {
            return $"Token: {this.TokenType} \t SystemType:{this.SystemType} \t Value: {this.TokenValue}";
        }
    }

    internal class IdentifierToken: SystemToken
    {
        public override string? TokenValue => base.TokenValue;

        public IdentifierToken(string value): base(SystemType.Identifier, value) { }
    }

    internal class KeywordToken: SystemToken
    {
        public override string? TokenValue => base.TokenValue;

        public KeywordToken(string value) : base(SystemType.Keyword, value) { }
    }

    internal class SeparatorToken : SystemToken
    {
        public override string? TokenValue => base.TokenValue;

        public SeparatorToken(string value) : base(SystemType.Separator, value) { }
    }

    internal class OperatorToken : SystemToken
    {
        public override string? TokenValue => base.TokenValue;

        public OperatorToken(string value) : base(SystemType.Operator, value) { }
    }

    internal class SpecialOperatorToken : OperatorToken
    {
        public override string? TokenValue => base.TokenValue;

        public SpecialOperatorToken(string value) : base(value) { }
    }

    internal class ArithmeticOperatorToken : OperatorToken
    {
        public override string? TokenValue => base.TokenValue;

        public ArithmeticOperatorToken(string value) : base(value) { }
    }

    internal class LogicOperatorToken : OperatorToken
    {
        public override string? TokenValue => base.TokenValue;

        public LogicOperatorToken(string value) : base(value) { }
    }

    internal class LogicArimeticOperatorToken: LogicOperatorToken
    {
        public override string? TokenValue => base.TokenValue;
        public LogicArimeticOperatorToken(string value): base(value) { }
    }

    internal class LogicBooleanOperatorToken : LogicOperatorToken
    {
        public override string? TokenValue => base.TokenValue;
        public LogicBooleanOperatorToken(string value) : base(value) { }
    }

    internal class EndOfLineToken : SystemToken
    {
        public override string? TokenValue => base.TokenValue;

        public EndOfLineToken() : base(SystemType.EndOfLine, ";") { }
    }
}
