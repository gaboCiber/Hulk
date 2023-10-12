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

        public CompilingError(ErrorType type, string argument)
        {
            Type = type;
            Argument = argument;
        }

        public override string ToString()
        {
            return $"! {this.Type.ToString().ToUpper()} ERROR: {this.Argument}";
        }

    }
}
