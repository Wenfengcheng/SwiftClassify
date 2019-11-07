using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BindingHelper
{

    [Verb("Tranform", HelpText = "Tranform protocol and class name definition in ApiDefinition.cs by swift header file.")]
    public class SwiftClassifyCmdOptions
    {

        [Option('h', HelpText = "Swift header file path", MetaValue = "SwiftHeaderFile", Required = true)]
        public string SwiftHeaderFile { get; set; }

        [Option('c', HelpText = "CSharp apiDefinition file path", MetaValue = "CsharpApiDefinitionFile", Required = true)]
        public string CSharpApiDefinitionFile { get; set; }
    }

    public static class SwiftClassifyCmd
    {
        const string SWIFT_PROTOCOL = @"SWIFT_PROTOCOL\(""(?<i>[\w\d]+)""\)\r\n@protocol\s(?<n>[\w\d]+)";
        const string SWIFT_CLASSE = @"SWIFT_CLASS\(""(?<i>[\w\d]+)""\)\r\n@interface\s(?<n>[\w\d]+)"; // \s:\s([\w\d]+)

        const string API_PROTOCOL = @"\s\[Protocol, Model\]\r\n\s*(\[(?<b>BaseType\s*\(typeof\([\w\d]+)\)\]\r\n\s*)?interface\s*{0}\s";
        const string API_CLASSE = @"\s\[(?<b>BaseType\s*\(typeof\([\w\d]+)\)\]\r\n\s*(\[[\w\s]+\]\n\s*)*interface\s*{0}\s";

        const string MAPPING = @"(?<o>[\w\d]+)+=(?<n>[\w\d]+)";

        static String StringApi;
        static StringBuilder SbApi;

        internal static Boolean TranformBaseType(SwiftClassifyCmdOptions options)
        {
            try
            {

                // Parse
                SbApi = new StringBuilder(StringApi = File.ReadAllText(options.CSharpApiDefinitionFile));
                ModifyApi(GetItens(File.ReadAllText(options.SwiftHeaderFile)));

                // Save
                File.WriteAllText(options.CSharpApiDefinitionFile.Replace(".cs", "New.cs"), SbApi.ToString());

                // Ok
                Console.WriteLine("Done");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        //public static void ParseMapping(string input)
        //{
        //    if (string.IsNullOrWhiteSpace(input))
        //    {
        //        Mappings = new Dictionary<string, string>();
        //    }
        //    else
        //    {
        //        var matches = new Regex(MAPPING).Matches(input).GetAllMatches().Where(c => c.Success);
        //        foreach (var item in matches)
        //        {
        //            Console.WriteLine($"{item.Groups["o"].Value}, {item.Groups["n"].Value}");
        //        }

        //        Mappings = matches.ToDictionary(k => k.Groups["o"].Value, v => v.Groups["n"].Value);
        //    }
        //}

        private static Interface[] GetItens(string input)
        {
            // Protocols
            var protocols = from c in new Regex(SWIFT_PROTOCOL).Matches(input).GetAllMatches()
                            where c.Success
                            select new Protocol()
                            {
                                Name = c.Groups["n"].Value,
                                CompiledName = c.Groups["i"].Value
                            } as Interface;
            // Classes
            var classes = from c in new Regex(SWIFT_CLASSE).Matches(input).GetAllMatches()
                          where c.Success
                          select new Classe()
                          {
                              Name = c.Groups["n"].Value,
                              CompiledName = c.Groups["i"].Value
                          } as Interface;

            return protocols.Union(classes).ToArray();
        }

        private static void ModifyApi(Interface[] itens)
        {
            foreach (var item in itens)
            {
                item.Replace();
            }
        }

        private static Match[] GetAllMatches(this MatchCollection matches)
        {
            Match[] matchArray = new Match[matches.Count];
            matches.CopyTo(matchArray, 0);

            return matchArray;
        }

        private abstract class Interface
        {
            public string Name { get; set; }
            public string CompiledName { get; set; }

            protected string FinalName
            {
                get
                {
                    return Name;
                }
            }

            public abstract void Replace();
        }

        private class Protocol : Interface
        {
            public override void Replace()
            {
                var regex = new Regex(string.Format(API_PROTOCOL, FinalName)).Match(StringApi);

                string oldValue = regex.Value;
                string newValue = string.Empty;

                if (regex.Groups["b"].Success)
                {
                    var baseType = regex.Groups["b"].Value;
                    newValue = oldValue.Replace(baseType, $@"{baseType}, Name = ""{CompiledName}""");
                }
                else
                {
                    newValue = oldValue.Replace("Protocol", $@"Protocol(Name = ""{CompiledName}"")");
                }

                SbApi.Replace(oldValue, newValue);
            }
        }

        private class Classe : Interface
        {
            public override void Replace()
            {
                var regex = new Regex(string.Format(API_CLASSE, FinalName)).Match(StringApi);
                var baseType = regex.Groups["b"].Value;

                string oldValue = regex.Value;
                string newValue = oldValue.Replace(baseType, $@"{baseType}, Name = ""{CompiledName}""");

                SbApi.Replace(oldValue, newValue);
            }
        }
    }
}
