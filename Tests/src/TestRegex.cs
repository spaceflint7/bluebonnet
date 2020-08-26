
using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestRegex : BaseTest
    {
        public override void TestMain()
        {
            Test(@"(\b(\w+)[,:;]?\s?)+[?.!]",
                 @"This is one sentence. This is a second sentence.");

            Test(@"\s+|;.*$|(""(\\.?|.)*?""|,@?|[^()'`~""; \t]+|.)",
                 @"(setq defmacro");
        }

        static void Test(string pattern, string input)
        {
            PrintMatch((new Regex(pattern)).Match(input));

            /*
            var matches = Regex.Matches(input, pattern);
            foreach (Match match in matches)
                PrintMatch(match);
            */

            void PrintMatch(Match match)
            {
                // match name always "0" per Match constructor calling Group constructor with such a name

                Console.WriteLine("Match '" + match.Name + "': '" + match.Value + "' Index " + match.Index + " Length " + match.Length);

                //Console.WriteLine($"Match {match.Name}: '{match.Value}' Index {match.Index} Length {match.Length}");
                foreach (Group group in match.Groups)
                {
                    Console.WriteLine("   Group '" + group.Name + "': OK=" + group.Success + ", '" + group.Value + "' Index " + group.Index + " Length " + group.Length);
                    /*Console.Write($"   Group {group.Name}: '{group.Value}'.  Captures:");
                    foreach (Capture capture in group.Captures)
                        Console.Write($"\t'{capture.Value}'");
                    Console.WriteLine();*/
                }
            }
        }

    }
}
