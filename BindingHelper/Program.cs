using CommandLine;
using CommandLine.Text;
using System;

namespace BindingHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            var supportedOptions = new Type[]
            {
                typeof(SwiftClassifyCmdOptions),
                typeof(OCClassifyCmdOptions)
            };

            var parseResult = CommandLine.Parser.Default.ParseArguments(args, supportedOptions);
            if (parseResult.Tag == ParserResultType.Parsed)
            {
                Parsed<Object> pr = parseResult as Parsed<Object>;
                if (pr.Value.GetType() == typeof(SwiftClassifyCmdOptions))
                {
                    SwiftClassifyCmd.TranformBaseType(pr.Value as SwiftClassifyCmdOptions);
                }
                else if (pr.Value.GetType() == typeof(OCClassifyCmdOptions))
                {
                    OCClassifyCmd.UpdateDefinition(pr.Value as OCClassifyCmdOptions);
                }
            }
            else if (parseResult.Tag == ParserResultType.NotParsed)
            {
                HelpText.AutoBuild(parseResult);
            }

            Console.ReadLine();
        }
    }
}
