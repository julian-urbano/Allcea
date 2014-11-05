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
using net.sf.dotnetcli;
using jurbano.Allcea.Model;
using jurbano.Allcea.Evaluation;
using System.IO;

namespace jurbano.Allcea.Cli
{
    public class NextCommand : ICommand
    {
        public Options Options { get; protected set; }
        public string OptionsFooter
        {
            get { return null; }
        }

        protected string _inputPath;
        protected string _judgedPath;
        protected string _estimatedPath;
        protected int _decimalDigits;
        protected int _batchNum;
        protected int _batchSize;
        protected IConfidenceEstimator _confEstimator;

        public NextCommand()
        {
            this.Options = new Options();
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with system runs.").Create("i"));
            this.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("file").WithDescription("optional path to file with known judgments.").Create("j"));
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with estimated judgments.").Create("e"));
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("target").WithDescription("type of estimates to target ('rel' or 'abs').").Create('t'));
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("num").WithDescription("number of batches that will be judged.").Create('b'));
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("num").WithDescription("number of documents per batch that will be judged.").Create('n'));
            this.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("conf").WithDescription("optional target average confidence on the estimates (defaults to " + Allcea.DEFAULT_CONFIDENCE + ").").Create("c"));
            this.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("size").WithDescription("optional target effect size to compute confidence (defaults to " + Allcea.DEFAULT_RELATIVE_SIZE + " for relative and " + Allcea.DEFAULT_ABSOLUTE_SIZE + " for absolute).").Create("s"));
            this.Options.AddOption(OptionBuilder.Factory.WithDescription("shows this help message.").Create("h"));

            this._inputPath = null;
            this._judgedPath = null;
            this._estimatedPath = null;
            this._decimalDigits = Allcea.DEFAULT_DECIMAL_DIGITS;
            this._batchNum = Allcea.DEFAULT_NUMBER_OF_BATCHES;
            this._batchSize = Allcea.DEFAULT_BATCH_SIZE;
            this._confEstimator = null;
        }

        public void CheckOptions(CommandLine cmd)
        {
            // Target and confidence estimator
            double confidence = Allcea.DEFAULT_CONFIDENCE;
            if (cmd.HasOption('c')) {
                string confidenceString = cmd.GetOptionValue('c');
                if (!Double.TryParse(confidenceString, out confidence) || confidence < 0 || confidence >= 1) {
                    throw new ArgumentException("'" + confidenceString + "' is not a valid target average confidence level.");
                }
            }
            string targetString = cmd.GetOptionValue('t').ToLower();
            if (targetString == "rel") {
                double size = Allcea.DEFAULT_RELATIVE_SIZE;
                if (cmd.HasOption('s')) {
                    string sizeString = cmd.GetOptionValue('s');
                    if (!Double.TryParse(sizeString, out size) || size < 0 || size >= 1) {
                        throw new ArgumentException("'" + sizeString + "' is not a valid target relative effect size.");
                    }
                }
                this._confEstimator = new NormalConfidenceEstimator(confidence, size, Allcea.DEFAULT_ABSOLUTE_SIZE);
            } else if (targetString == "abs") {
                double size = Allcea.DEFAULT_ABSOLUTE_SIZE;
                if (cmd.HasOption('s')) {
                    string sizeString = cmd.GetOptionValue('s');
                    if (!Double.TryParse(sizeString, out size) || size < 0 || size >= 1) {
                        throw new ArgumentException("'" + sizeString + "' is not a valid target absolute effect size.");
                    }
                }
                this._confEstimator = new NormalConfidenceEstimator(confidence, Allcea.DEFAULT_RELATIVE_SIZE, size);
            } else {
                throw new ArgumentException("'" + targetString + "' is not a valid type of estimates to target.");
            }
            // Batches
            string batchesString = cmd.GetOptionValue('b');
            if (!Int32.TryParse(batchesString, out this._batchNum) || this._batchNum < 1) {
                throw new ArgumentException("'" + batchesString + "' is not a valid number of batches.");
            }
            string numString = cmd.GetOptionValue('n');
            if (!Int32.TryParse(numString, out this._batchSize) || this._batchSize < 1) {
                throw new ArgumentException("'" + numString + "' is not a valid number of documents per batch.");
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
            // Estimates file
            this._estimatedPath = cmd.GetOptionValue('e');
            if (!File.Exists(this._estimatedPath)) {
                throw new ArgumentException("Estimated judgments file '" + this._estimatedPath + "' does not exist.");
            }
        }

        public void Run()
        {
            throw new NotImplementedException();
        }
    }
}