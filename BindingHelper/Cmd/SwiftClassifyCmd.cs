using CommandLine;
using Newtonsoft.Json;
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
        private static string SWIFT_PROTOCOL = null; //@"SWIFT_PROTOCOL\(""(?<i>[\w\d]+)""\)\r\n@protocol\s(?<n>[\w\d]+)";
        private static string SWIFT_CLASSE = null; //@"SWIFT_CLASS\(""(?<i>[\w\d]+)""\)\r\n@interface\s(?<n>[\w\d]+)"; 

        private static string API_PROTOCOL = null; //@"\s\[Protocol, Model\]\r\n\s*(\[(?<b>BaseType\s*\(typeof\([\w\d]+)\)\]\r\n\s*)?interface\s*{0}\s";
        private static string API_CLASSE = null; //@"\s\[(?<b>BaseType\s*\(typeof\([\w\d]+)\)\]\r\n\s*(\[[\w\s]+\]\n\s*)*interface\s*{0}\s";

        static String StringApi;
        static StringBuilder SbApi;

        static SwiftClassifyCmd()
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
                NLogHelper.Logger.Info("Start to tranform basetype name definition.");

                // Read
                try
                {
                    SbApi = new StringBuilder(StringApi = File.ReadAllText(options.CSharpApiDefinitionFile));
                }
                catch (Exception readEx)
                {
                    NLogHelper.Logger.Error(string.Format("Failed to read api definition (ApiDefinitionFile={0}, error info:{1}", options.CSharpApiDefinitionFile, JsonConvert.SerializeObject(readEx)));
                }
                NLogHelper.Logger.Info("Read all api definitions successfully.");

                // Parse and replace
                try
                {
                    string swiftDefinitions = File.ReadAllText(options.SwiftHeaderFile);
                    ModifyApi(GetItens(swiftDefinitions));
                }
                catch (Exception swiftEx)
                {
                    NLogHelper.Logger.Error(string.Format("Failed to read swift definition (SwiftHeaderFile={0}, error info:{1}", options.SwiftHeaderFile, JsonConvert.SerializeObject(swiftEx)));
                }
                NLogHelper.Logger.Info("Get all swift definitions successfully.");

                // Save
                try
                {
                    File.WriteAllText(options.CSharpApiDefinitionFile.Replace(".cs", "New.cs"), SbApi.ToString());
                }
                catch (Exception writeEx)
                {
                    NLogHelper.Logger.Error(string.Format("Failed to write name definition (ApiDefinitionFile={1}, error info:{2}", options.SwiftHeaderFile, options.CSharpApiDefinitionFile, JsonConvert.SerializeObject(writeEx)));
                }
                NLogHelper.Logger.Info("Write to new api definitions successfully.");

                // Ok
                NLogHelper.Logger.Info("Done.");

                return true;
            }
            catch (Exception ex)
            {
                NLogHelper.Logger.Error(string.Format("Failed to tranform basetype name definition (SwiftHeaderFile={0}, ApiDefinitionFile={1}, error info:{2}", options.SwiftHeaderFile, options.CSharpApiDefinitionFile, JsonConvert.SerializeObject(ex)));
                return false;
            }
        }

        private static Interface[] GetItens(string input)
        {
            try
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
            catch (Exception ex)
            {
                NLogHelper.Logger.Error(string.Format("Failed to parse orgin name and complied name (input={0}, error info:{1}", input, JsonConvert.SerializeObject(ex)));
                return null;
            }
        }

        private static void ModifyApi(Interface[] itens)
        {
            if (itens == null || !itens.Any())
                return;
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
