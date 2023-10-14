namespace Hulk.src
{
    internal class StringToken : Tokens<string>
    {
        public override string? TokenValue => base.TokenValue;

        public string StringValue { get; private set; }

        public StringToken(int column, string value) : base(TokenType.String, column, value) 
        {
            StringValue = TokenValue.Substring(1, TokenValue.Length - 2);
        }

        public override string ToString()
        {
            return StringValue;
        }
    }
}
