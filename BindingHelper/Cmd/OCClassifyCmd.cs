using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;

namespace BindingHelper
{

    [Verb("OCClassify", HelpText = "Fix building error in ApiDefinition.cs.")]
    public class OCClassifyCmdOptions
    {

        [Option('c', HelpText = "CSharp apiDefinition file path", MetaValue = "CsharpApiDefinitionFile", Required = true)]
        public string CSharpApiDefinitionFile { get; set; }
    }


    public class OCClassifyCmd
    {

        private static string VERIFY = @"(?<a>\s\[Verify\s*\(\w*\)\]\n)";

        static String StringApi;
        static StringBuilder SbApi;

        static OCClassifyCmd()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                VERIFY = @"(?<a>\s\[Verify\s*\(\w*\)\]\r\n)";
            }
            else
            {
                VERIFY = @"(?<a>\s\[Verify\s*\(\w*\)\]\n)";
            }
        }

        internal static Boolean UpdateDefinition(OCClassifyCmdOptions options)
        {
            try
            {

                // Parse
                SbApi = new StringBuilder(StringApi = File.ReadAllText(options.CSharpApiDefinitionFile));

                // Remove verify attribute
                RemoveVerifyAttribute();

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

        private static void RemoveVerifyAttribute()
        {
            try
            {
                var result = (from c in new Regex(VERIFY).Matches(StringApi).GetAllMatches()
                              where c.Success
                              select c.Groups["a"].Value).ToList().Distinct();

                foreach (var item in result)
                {
                    SbApi.Replace(item, "");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
}
