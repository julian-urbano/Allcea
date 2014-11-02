﻿// Copyright (C) 2014  Julián Urbano <urbano.julian@gmail.com>
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
using jurbano.Allcea.Model;
using net.sf.dotnetcli;
using System.IO;

using jurbano.Allcea.Estimation;

namespace jurbano.Allcea.Cli
{
    public class EstimateCommand : ICommand
    {
        public Options Options { get; protected set; }
        public string OptionsFooter
        {
            get
            {
                return "The available estimators and their parameters are:"
                    + "\n  uniform  uniform distribution with the Fine scale, from 0 to 100."
                    + "\n  mout     model fitted with features about system outputs and metadata."
                    + "\n             -p meta=file   path to file with artist-genre metadata for all documents."
                    + "\n  mjud     model fitted with features about system outputs, metadata and known judgments."
                    + "\n             -p meta=file   path to file with artist-genre metadata for all documents."
                    + "\n             -p known=file  path to file with judgments already known.";
            }
        }

        protected string InputPath { get; set; }
        protected EstimatorWrapper Estimator { get; set; }

        public EstimateCommand()
        {
            this.Options = new Options();
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("name").WithDescription("name of the estimator to use.").Create("e"));
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with system runs.").Create("i"));
            this.Options.AddOption(OptionBuilder.Factory.HasArgs().WithArgName("name=value").WithDescription("parameter to the estimator.").Create("p"));
            this.Options.AddOption(OptionBuilder.Factory.WithDescription("shows this help message.").Create("h"));

            this.InputPath = null;
            this.Estimator = null;
        }

        public void CheckOptions(CommandLine cmd)
        {
            // Input file
            this.InputPath = cmd.GetOptionValue('i');
            if (!File.Exists(this.InputPath)) {
                throw new ArgumentException("Input file '" + this.InputPath + "' does not exist.");
            }
            // Estimator
            Dictionary<string, string> parameters = Allcea.ParseNameValueParameters(cmd.GetOptionValues('p'));
            this.Estimator = new EstimatorWrapper(cmd.GetOptionValue('e'), parameters);
        }

        public void Run()
        {
            // Read input file
            IEnumerable<Run> runs = null;
            try {
                IReader<Run> runReader = new TabSeparated();
                using (StreamReader sr = new StreamReader(File.OpenRead(this.InputPath))) {
                    runs = runReader.Read(sr);
                }
            } catch (Exception ex) {
                throw new FormatException("Error reading input file: " + ex.Message, ex);
            }
            // Initialize wrapped estimator
            this.Estimator.Initialize(runs);
            // Compile list of all query-doc pairs
            Dictionary<string, HashSet<string>> querydocs = new Dictionary<string, HashSet<string>>();
            foreach (var run in runs) {
                HashSet<string> docs = null;
                if (!querydocs.TryGetValue(run.Query, out docs)) {
                    docs = new HashSet<string>();
                    querydocs.Add(run.Query, docs);
                }
                docs.UnionWith(run.Documents);
            }
            // Estimate relevance of all query-doc pairs
            List<Estimate> estimates = new List<Estimate>();
            foreach (var qd in querydocs) {
                foreach (var doc in qd.Value) {
                    estimates.Add(this.Estimator.Estimate(qd.Key, doc));
                }
            }
            // Output estimates
            IWriter<Estimate> estWriter = new TabSeparated();
            estWriter.Write(Console.Out, estimates);
        }
    }
}