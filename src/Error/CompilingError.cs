using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hulk.src
{
    public enum ErrorType { Lexical, Semantic, Syntax }

    public class CompilingError
    {
        public ErrorType Type { get; private set; }

        public string? Argument { get; private set; }

        public int Column { get; private set; }

        public CompilingError(ErrorType type, int column , string argument)
        {
            Type = type;
            Argument = argument;
            Column = column;
        }

        public override string ToString()
        {
            StringBuilder error = new StringBuilder();
            error.Append($"! {this.Type.ToString().ToUpper()} ERROR ");

            if (Column > -1)
                error.Append($"(Col {this.Column}) : ");
            else
                error.Append(": ");
            
            error.Append(this.Argument);

            return error.ToString();
        }

    }
}
