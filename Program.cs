using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Earleytech
{
    internal class Program
    {
        static void Main()
        {
            string? input;
            do
            {
                input = Console.ReadLine();
                Console.WriteLine(Strings.Stringify(input, Strings.StringifyOptions.LongText));
            } while (input != null && input.Trim().ToLower() != "exit");
        }
    }
}
