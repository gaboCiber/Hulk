using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hulk.src
{
    public enum TokenType { Number, String, Boolean, System }

    public interface TokenInterface
    {
        public string GetTokenValueAsString();
        public int GetColumn();
    }

    public class Tokens<T>: TokenInterface
    {
        public virtual T? TokenValue { private set; get; }
        
        public TokenType TokenType { private set; get; }

        public int Column { get; private set; }

        public Tokens(TokenType type, int column, T value)
        {
            this.TokenValue = value;
            this.TokenType = type;
            this.Column = column;
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

        public int GetColumn()
        {
            return Column;
        }
    }

}
