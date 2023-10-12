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

                LexicalAnalyzer tokens = new LexicalAnalyzer(input);

                if (tokens.IsThereAnyLexicalError)
                {
                    tokens.GetErrors().ForEach(i => Console.WriteLine(i));
                    continue;
                }

                if (tokens.GetTokens().Count == 0)
                    continue;

                SyntaticAnalyzer result = new SyntaticAnalyzer(tokens.GetTokens());

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