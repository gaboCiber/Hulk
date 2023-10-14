namespace Hulk.src
{
    internal class BooleanToken : Tokens<bool>
    {
        public override bool TokenValue => base.TokenValue;

        public BooleanToken(int column, bool value) : base(TokenType.Boolean, column, value) { }

        public override string ToString()
        {
            return TokenValue.ToString();
        }
    }
}
