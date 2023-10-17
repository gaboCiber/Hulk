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

                Lexer tokens = new Lexer();

                foreach (var tokenList in tokens.CreateTokens(input))
                {
                    if (tokens.IsThereAnyLexicalError)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        tokens.GetErrors().ForEach(i => Console.WriteLine(i));
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    if (tokenList.Count == 0)
                        continue;

                    Parser result = new Parser(tokenList);

                    if (result.IsThereAnyError)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        result.GetErrors().ForEach(i => Console.WriteLine(i));
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    if (result.Output is not null)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(result.Output);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }

            }
        }
    }
}