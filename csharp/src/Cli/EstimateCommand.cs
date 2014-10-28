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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using jurbano.Allcea.Estimation;
using net.sf.dotnetcli;
using System.IO;

namespace jurbano.Allcea.Cli
{
    public class EstimateCommand : ICommand
    {
        public Options Options { get; protected set; }
        public string OptionsFooter
        {
            get
            {
                return "The available estimators are:"
                    + "\n  uniform  uniform distribution with the Fine scale, from 0 to 100.";
            }
        }

        public EstimateCommand()
        {
            this.Options = new Options();
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("name").WithDescription("name of the estimator to use.").Create("e"));
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with system runs.").Create("i"));
            //this.Options.AddOption(OptionBuilder.Factory.HasArgs().WithArgName("name=value").WithDescription("parameter to the estimator.").Create("p"));
            this.Options.AddOption(OptionBuilder.Factory.WithDescription("shows this help message.").Create("h"));
        }

        public void Run(CommandLine cmd)
        {
            // Input file
            string inputFile = cmd.GetOptionValue('i');
            if (!File.Exists(inputFile)) {
                Console.Error.WriteLine("Input file '" + inputFile + "' does not exist.");
                Environment.Exit(1);
            }
            // Estimator
            string estimatorName = cmd.GetOptionValue('e');
            IEstimator estimator = null;
            switch (estimatorName) {
                case "uniform":
                    estimator = new UniformEstimator(100);
                    break;
                default:
                    Console.Error.WriteLine("'" + estimatorName + "' is not a valid estimator name. See 'allcea-" + Allcea.VERSION + " estimate -h'.");
                    Environment.Exit(1);
                    break;
            }
            throw new NotImplementedException();
        }
    }
}