namespace Hulk.src
{
    internal class NumberToken : Tokens<double>
    {
        public override double TokenValue => base.TokenValue;

        public NumberToken(int column, double value) : base(TokenType.Number, column, value) { }

        public override string ToString()
        {
            return TokenValue.ToString();
        }
    }
}
