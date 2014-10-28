// Copyright (C) 2014  Julián Urbano <urbano.julian@gmail.com>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/.

using jurbano.Allcea.Cli;
using net.sf.dotnetcli;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace jurbano.Allcea
{
    public class Allcea
    {
        public const string VERSION = "1.0";
        public const string COPYRIGHT_NOTICE =
            "Allcea " + Allcea.VERSION + "  Copyright (C) 2014  Julian Urbano <urbano.julian@gmail.com>"
            + "\nThis program comes with ABSOLUTELY NO WARRANTY."
            + "\nThis is free software, and you are welcome to redistribute it"
            + "\nunder the terms of the GNU Lesser General Public License version 3.";

        public static void Main(string[] args)
        {
            if (args.Length > 0) {
                // Check CLI command name
                string commandName = args[0].ToLower();
                ICommand command = null;
                switch (commandName) {
                    case "-h":
                        Allcea.PrintMainUsage(null);
                        Environment.Exit(0);
                        break;
                    case "estimate":
                        command = new EstimateCommand();
                        break;
                    //case "evaluate": break;
                    //case "next": break;
                    //case "simulate": break;
                    default:
                        Console.Error.WriteLine("'" + commandName + "' is not a valid allcea-" + Allcea.VERSION + " command. See 'allcea-" + Allcea.VERSION + " -h'.");
                        Environment.Exit(1);
                        break;
                }
                Options options = command.Options;
                // help? Cannot wait to parse CLI options because it will throw exception before
                if (options.HasOption("h") && args.Contains("-h")) {
                    Allcea.PrintUsage(null, commandName, options, command.OptionsFooter);
                    Environment.Exit(0);
                }
                // Parse CLI options
                try {
                    Parser parser = new BasicParser();
                    CommandLine cmd = parser.Parse(options, args.Skip(1).ToArray());
                    // If we have extra CLI options the Parse method doesn't throw exception. Handle here
                    if (cmd.Args==null || cmd.Args.Length != 0) {
                        throw new ParseException("Unused option(s): " + string.Join(",", cmd.Args));
                    }
                    // Run command
                    command.Run(cmd);
                } catch (Exception e) {
                    Allcea.PrintUsage(e.Message,commandName, options, command.OptionsFooter);
                    Environment.Exit(1);
                }
            } else {
                // No CLI options
                Allcea.PrintMainUsage(null);
                Environment.Exit(1);
            }
        }
        protected static void PrintMainUsage(string msg)
        {
            Allcea.PrintUsage(msg,
                "<command> [-h] [...]",
                "command  the command to run.\n"
                + "-h       shows this help message.",
                "The available commands are (run 'allcea-" + Allcea.VERSION + " <command> -h' for specific help):"
                + "\n  estimate  to estimate relevance judgments."
                //+ "\n  evaluate  to evaluate systems with estimated judgments."
                //+ "\n  next      to obtain the most informative documents to judge next."
                //+ "\n  simulate  to simulate the execution of estimate, evaluate and next."
                );
        }
        protected static void PrintUsage(string msg, string command, Options options, string footer)
        {
            HelpFormatter formatter = new HelpFormatter() { SyntaxPrefix = "" };
            StringWriter wr = new StringWriter();
            wr.Write(command + " ");
            formatter.PrintUsage(wr, Int32.MaxValue, null, options);
            string usageOptions = wr.ToString().Trim();
            wr.Dispose();
            string optionsString = formatter.RenderOptions(new StringBuilder(), Int32.MaxValue, options, 0, 2).ToString();
            Allcea.PrintUsage(msg, usageOptions, optionsString, footer);
        }
        protected static void PrintUsage(string msg, string usageOptions, string options, string footer)
        {
            StringBuilder sb = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(msg)) {
                sb.AppendLine(msg);
                sb.AppendLine();
            }
            sb.AppendLine("usage: allcea-" + Allcea.VERSION + " " + usageOptions);
            sb.AppendLine();
            sb.AppendLine(options);
            if (!String.IsNullOrWhiteSpace(footer)) {
                sb.AppendLine();
                sb.AppendLine(footer);
            }
            sb.AppendLine();
            sb.AppendLine(Allcea.COPYRIGHT_NOTICE);

            Console.Error.WriteLine(sb);
        }
    }
}