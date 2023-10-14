namespace Hulk.src
{
    public enum SystemType { Identifier, Keyword, Separator, Operator, LogicOperator, EndOfLine }

    internal class SystemToken : Tokens<string>
    {
        public override string? TokenValue => base.TokenValue;

        public SystemType SystemType { private set; get; }

        public SystemToken(SystemType type, int column, string value) : base(TokenType.System, column, value)
        {
            this.SystemType = type;
        }

        public override string ToString()
        {
            return $"Token: {this.TokenType} \t SystemType:{this.SystemType} \t Value: {this.TokenValue}";
        }
    }

    internal class IdentifierToken : SystemToken
    {
        public override string? TokenValue => base.TokenValue;

        public IdentifierToken(int column, string value) : base(SystemType.Identifier, column, value) { }
    }

    internal class KeywordToken : SystemToken
    {
        public override string? TokenValue => base.TokenValue;

        public KeywordToken(int column, string value) : base(SystemType.Keyword, column, value) { }
    }

    internal class SeparatorToken : SystemToken
    {
        public override string? TokenValue => base.TokenValue;

        public SeparatorToken(int column, string value) : base(SystemType.Separator, column, value) { }
    }

    internal class OperatorToken : SystemToken
    {
        public override string? TokenValue => base.TokenValue;

        public OperatorToken(int column, string value) : base(SystemType.Operator, column, value) { }
    }

    internal class SpecialOperatorToken : OperatorToken
    {
        public override string? TokenValue => base.TokenValue;

        public SpecialOperatorToken(int column, string value) : base(column, value) { }
    }

    internal class ArithmeticOperatorToken : OperatorToken
    {
        public override string? TokenValue => base.TokenValue;

        public ArithmeticOperatorToken(int column, string value) : base(column, value) { }
    }

    internal class LogicOperatorToken : OperatorToken
    {
        public override string? TokenValue => base.TokenValue;

        public LogicOperatorToken(int column, string value) : base(column, value) { }
    }

    internal class LogicArimeticOperatorToken : LogicOperatorToken
    {
        public override string? TokenValue => base.TokenValue;
        public LogicArimeticOperatorToken(int column, string value) : base(column, value) { }
    }

    internal class LogicBooleanOperatorToken : LogicOperatorToken
    {
        public override string? TokenValue => base.TokenValue;
        public LogicBooleanOperatorToken(int column, string value) : base(column, value) { }
    }

    internal class EndOfLineToken : SystemToken
    {
        public override string? TokenValue => base.TokenValue;

        public EndOfLineToken(int column) : base(SystemType.EndOfLine, column, ";") { }
    }
}
