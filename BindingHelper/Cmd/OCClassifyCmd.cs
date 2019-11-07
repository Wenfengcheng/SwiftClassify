using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;

namespace BindingHelper
{

    [Verb("Update", HelpText = "Fix building error in ApiDefinition.cs.")]
    public class OCClassifyCmdOptions
    {

        [Option('c', HelpText = "CSharp apiDefinition file path", MetaValue = "CsharpApiDefinitionFile", Required = true)]
        public string CSharpApiDefinitionFile { get; set; }
    }


    public class OCClassifyCmd
    {

        const string VERIFY = @"(?<a>\s\[Verify\s*\(\w*\)\]\n)";

        static String StringApi;
        static StringBuilder SbApi;

        internal static Boolean UpdateDefinition(OCClassifyCmdOptions options)
        {
            try
            {

                // Parse
                SbApi = new StringBuilder(StringApi = File.ReadAllText(options.CSharpApiDefinitionFile));

                var result = (from c in new Regex(VERIFY).Matches(StringApi).GetAllMatches()
                             where c.Success
                             select c.Groups["a"].Value).ToList().Distinct();

                foreach (var item in result)
                {
                    SbApi.Replace(item, "");
                }

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
    }
}
