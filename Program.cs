using System.Threading.Channels;
using Hulk.src;

namespace Hulk
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("> ");

                string input = Console.ReadLine()!;

                Lexer tokens = new Lexer(input);

                if (tokens.IsThereAnyLexicalError)
                {
                    tokens.GetErrors().ForEach(i => Console.WriteLine(i));
                    continue;
                }

                if (tokens.GetTokens().Count == 0)
                    continue;

                Parser result = new Parser(tokens.GetTokens());

                if (result.IsThereAnyError)
                {
                    result.GetErrors().ForEach(i => Console.WriteLine(i));
                    continue;
                }

                if(result.Output is not null)
                    Console.WriteLine(result.Output);
            }
        }
    }
}