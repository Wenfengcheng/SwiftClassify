using CommandLine;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace BindingHelper
{

    [Verb("SwiftClassify", HelpText = "Tranform protocol and class name definition in ApiDefinition.cs by swift header file.")]
    public class SwiftClassifyCmdOptions
    {

        [Option('h', HelpText = "Swift header file path", MetaValue = "SwiftHeaderFile", Required = true)]
        public string SwiftHeaderFile { get; set; }

        [Option('c', HelpText = "CSharp apiDefinition file path", MetaValue = "CsharpApiDefinitionFile", Required = true)]
        public string CSharpApiDefinitionFile { get; set; }
    }

    public class SwiftClassifyCmd
    {
        private static string SWIFT_PROTOCOL = @"SWIFT_PROTOCOL\(""(?<i>[\w\d]+)""\)\r\n@protocol\s(?<n>[\w\d]+)";
        private static string SWIFT_CLASSE = @"SWIFT_CLASS\(""(?<i>[\w\d]+)""\)\r\n@interface\s(?<n>[\w\d]+)"; // \s:\s([\w\d]+)

        private static string API_PROTOCOL = @"\s\[Protocol, Model\]\r\n\s*(\[(?<b>BaseType\s*\(typeof\([\w\d]+)\)\]\r\n\s*)?interface\s*{0}\s";
        private static string API_CLASSE = @"\s\[(?<b>BaseType\s*\(typeof\([\w\d]+)\)\]\r\n\s*(\[[\w\s]+\]\n\s*)*interface\s*{0}\s";

        static String StringApi;
        static StringBuilder SbApi;

        public SwiftClassifyCmd()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SWIFT_PROTOCOL = @"SWIFT_PROTOCOL\(""(?<i>[\w\d]+)""\)\r\n@protocol\s(?<n>[\w\d]+)";
                SWIFT_CLASSE = @"SWIFT_CLASS\(""(?<i>[\w\d]+)""\)\r\n@interface\s(?<n>[\w\d]+)";

                API_PROTOCOL = @"\s\[Protocol, Model\]\r\n\s*(\[(?<b>BaseType\s*\(typeof\([\w\d]+)\)\]\r\n\s*)?interface\s*{0}\s";
                API_CLASSE = @"\s\[(?<b>BaseType\s*\(typeof\([\w\d]+)\)\]\r\n\s*(\[[\w\s]+\]\n\s*)*interface\s*{0}\s";
            }
            else
            {
                SWIFT_PROTOCOL = @"SWIFT_PROTOCOL\(""(?<i>[\w\d]+)""\)\n@protocol\s(?<n>[\w\d]+)";
                SWIFT_CLASSE = @"SWIFT_CLASS\(""(?<i>[\w\d]+)""\)\n@interface\s(?<n>[\w\d]+)";

                API_PROTOCOL = @"\s\[Protocol, Model\]\n\s*(\[(?<b>BaseType\s*\(typeof\([\w\d]+)\)\]\n\s*)?interface\s*{0}\s";
                API_CLASSE = @"\s\[(?<b>BaseType\s*\(typeof\([\w\d]+)\)\]\n\s*(\[[\w\s]+\]\n\s*)*interface\s*{0}\s";
            }
        }

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
