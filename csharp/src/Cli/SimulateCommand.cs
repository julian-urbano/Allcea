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
    public class SimulateCommand : AbstractCommand
    {
        public override string OptionsFooter
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
        protected EvaluationTargets _target;
        protected int _batchSize;
        protected IConfidenceEstimator _confEstimator;
        protected double _confidence;

        public SimulateCommand()
        {
            base.Options = new Options();
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with system runs.").Create("i"));
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to file with known judgments.").Create("j"));
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("name").WithDescription("name of the estimator to use.").Create("e"));
            base.Options.AddOption(OptionBuilder.Factory.HasArgs().WithArgName("name=value").WithDescription("optional parameter to the estimator.").Create("p"));
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("target").WithDescription("type of estimates to target ('rel' or 'abs').").Create('t'));
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("num").WithDescription("number of documents to judge per batch.").Create('n'));
            base.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("conf").WithDescription("optional target average confidence on the estimates (defaults to " + Allcea.DEFAULT_CONFIDENCE + ").").Create("c"));
            base.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("size").WithDescription("optional target effect size to compute confidence (defaults to " + Allcea.DEFAULT_RELATIVE_SIZE + " for relative and " + Allcea.DEFAULT_ABSOLUTE_SIZE + " for absolute).").Create("s"));
            base.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("digits").WithDescription("optional number of fractional digits to output (defaults to " + Allcea.DEFAULT_DECIMAL_DIGITS + ")").Create("d"));
            base.Options.AddOption(OptionBuilder.Factory.WithDescription("shows this help message.").Create("h"));

            this._inputPath = null;
            this._judgedPath = null;
            this._estimator = null;
            this._decimalDigits = Allcea.DEFAULT_DECIMAL_DIGITS;
            this._batchSize = Allcea.DEFAULT_BATCH_SIZE;
            this._confEstimator = null;
            this._target = EvaluationTargets.Relative;
            this._confidence = Allcea.DEFAULT_CONFIDENCE;
        }

        public override void CheckOptions(CommandLine cmd)
        {
            // Target and confidence estimator
            if (cmd.HasOption('c')) {
                this._confidence = AbstractCommand.CheckConfidence(cmd.GetOptionValue('c'));
            }
            this._target = AbstractCommand.CheckTarget(cmd.GetOptionValue('t'));
            double sizeRel = Allcea.DEFAULT_RELATIVE_SIZE;
            double sizeAbs = Allcea.DEFAULT_ABSOLUTE_SIZE;
            if (cmd.HasOption('s')) {
                switch (this._target) {
                    case EvaluationTargets.Relative: sizeRel = AbstractCommand.CheckRelativeSize(cmd.GetOptionValue('s')); break;
                    case EvaluationTargets.Absolute: sizeAbs = AbstractCommand.CheckAbsoluteSize(cmd.GetOptionValue('s')); break;
                }
            }
            this._confEstimator = new NormalConfidenceEstimator(this._confidence, sizeRel, sizeAbs);
            // Double format
            if (cmd.HasOption('d')) {
                this._decimalDigits = AbstractCommand.CheckDigits(cmd.GetOptionValue('d'));
            }
            // Batches
            this._batchSize = AbstractCommand.CheckBatchSize(cmd.GetOptionValue('n'));
            // Files
            this._inputPath = AbstractCommand.CheckInputFile(cmd.GetOptionValue('i'));
            this._judgedPath = AbstractCommand.CheckJudgedFile(cmd.GetOptionValue('j'));
            // Estimator
            Dictionary<string, string> parameters = Allcea.ParseNameValueParameters(cmd.GetOptionValues('p'));
            this._estimator = new EstimatorWrapper(cmd.GetOptionValue('e'), parameters);
        }

        public override void Run()
        {
            // Read files
            IEnumerable<Run> runs = AbstractCommand.ReadInputFile(this._inputPath);
            RelevanceEstimateStore store = new RelevanceEstimateStore(AbstractCommand.ReadKnownJudgments(this._judgedPath));
            // Initialize wrapped estimator
            this._estimator.Initialize(runs, new RelevanceEstimate[] { }); // No known judgments at this point

        }
    }
}