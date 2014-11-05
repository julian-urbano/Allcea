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
                    + "\n             -p meta=file    path to file with artist-genre metadata for all documents.";
                    //+ "\n  mjud     model fitted with features about system outputs, metadata and known judgments."
                    //+ "\n             -p meta=file    path to file with artist-genre metadata for all documents."
                    //+ "\n             -p judged=file  path to file with judgments already known.";
            }
        }

        protected string _inputPath;
        protected string _judgedPath;
        protected EstimatorWrapper _estimator;
        protected int _decimalDigits;

        public EstimateCommand()
        {
            this.Options = new Options();
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("name").WithDescription("name of the estimator to use.").Create("e"));
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with system runs.").Create("i"));
            this.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("file").WithDescription("path to file with known judgments (will not be estimated).").Create("j"));
            this.Options.AddOption(OptionBuilder.Factory.HasArgs().WithArgName("name=value").WithDescription("parameter to the estimator.").Create("p"));
            this.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("digits").WithDescription("number of fractional digits to output (defaults to " + Allcea.DEFAULT_DECIMAL_DIGITS + ")").Create("d"));
            this.Options.AddOption(OptionBuilder.Factory.WithDescription("shows this help message.").Create("h"));

            this._inputPath = null;
            this._judgedPath = null;
            this._estimator = null;
            this._decimalDigits = Allcea.DEFAULT_DECIMAL_DIGITS;
        }

        public void CheckOptions(CommandLine cmd)
        {
            // Double format
            if (cmd.HasOption('d')) {
                string digitsString = cmd.GetOptionValue('d');
                if (!Int32.TryParse(digitsString, out this._decimalDigits) || this._decimalDigits < 0) {
                    throw new ArgumentException("'" + digitsString + "' is not a valid number of decimal digits to output.");
                }
            }
            // Input file
            this._inputPath = cmd.GetOptionValue('i');
            if (!File.Exists(this._inputPath)) {
                throw new ArgumentException("Input file '" + this._inputPath + "' does not exist.");
            }
            // Judgments file
            if (cmd.HasOption('j')) {
                this._judgedPath = cmd.GetOptionValue('j');
                if (!File.Exists(this._judgedPath)) {
                    throw new ArgumentException("Known judgments file '" + this._judgedPath + "' does not exist.");
                }
            }
            // Estimator
            Dictionary<string, string> parameters = Allcea.ParseNameValueParameters(cmd.GetOptionValues('p'));
            this._estimator = new EstimatorWrapper(cmd.GetOptionValue('e'), parameters);
        }

        public void Run()
        {
            TabSeparated io = new TabSeparated(this._decimalDigits);
            // Read files
            IEnumerable<Run> runs = io.ReadInputFile(this._inputPath);
            IEnumerable<RelevanceEstimate> judged = new RelevanceEstimate[] { };
            if (this._judgedPath != null) {
                judged = io.ReadKnownJudgments(this._judgedPath);
            }
            // Initialize wrapped estimator
            this._estimator.Initialize(runs, judged);
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
            List<RelevanceEstimate> estimates = new List<RelevanceEstimate>();
            foreach (var qd in querydocs) {
                foreach (var doc in qd.Value) {
                    estimates.Add(this._estimator.Estimate(qd.Key, doc));
                }
            }
            // Output estimates
            ((IWriter<RelevanceEstimate>)io).Write(Console.Out, estimates);
        }
    }
}