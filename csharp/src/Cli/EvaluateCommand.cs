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
using net.sf.dotnetcli;
using System.IO;
using jurbano.Allcea.Model;

namespace jurbano.Allcea.Cli
{
    public class EvaluateCommand : ICommand
    {
        public Options Options { get; protected set; }

        public string OptionsFooter
        {
            get { return null; }
        }

        protected string _inputPath;
        protected string _judgedPath;
        protected string _estimatedPath;

        public EvaluateCommand()
        {
            this.Options = new Options();
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with system runs.").Create("i"));
            this.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("file").WithDescription("path to file with known judgments.").Create("j"));
            this.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with estimated judgments.").Create("e"));
            this.Options.AddOption(OptionBuilder.Factory.WithDescription("shows this help message.").Create("h"));

            this._inputPath = null;
            this._judgedPath = null;
            this._estimatedPath = null;
        }

        public void CheckOptions(CommandLine cmd)
        {
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
            // Read files
            IReadHelper reader = new TabSeparated();
            IEnumerable<Run> runs = reader.ReadInputFile(this._inputPath);
            IEnumerable<RelevanceEstimate> judged = new RelevanceEstimate[] { };
            if (this._judgedPath != null) {
                judged = reader.ReadKnownJudgments(this._judgedPath);
            }
            IEnumerable<RelevanceEstimate> estimates = reader.ReadEstimatedJudgments(this._estimatedPath);
            // Instantiate estimate store and measure
            RelevanceEstimateStore store = new RelevanceEstimateStore(judged, estimates);
            // Re-structure runs
            // Estimate effectiveness
            // Output estimates
        }
    }
}