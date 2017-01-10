using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace TestGUI
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                var input = Console.ReadLine();
                var eparser = new EParserLib.EParser(input);
                Console.WriteLine(eparser.RPN);
                Console.WriteLine(eparser.Calculate());
                Console.WriteLine();
            }
        }
    }
}
