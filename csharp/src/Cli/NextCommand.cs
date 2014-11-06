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
    public class NextCommand : AbstractCommand
    {
        public override string OptionsFooter
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
            base.Options = new Options();
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with system runs.").Create("i"));
            base.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("file").WithDescription("optional path to file with known judgments.").Create("j"));
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with estimated judgments.").Create("e"));
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("target").WithDescription("type of estimates to target ('rel' or 'abs').").Create('t'));
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("num").WithDescription("number of batches that will be judged.").Create('b'));
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("num").WithDescription("number of documents per batch that will be judged.").Create('n'));
            base.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("conf").WithDescription("optional target average confidence on the estimates (defaults to " + Allcea.DEFAULT_CONFIDENCE + ").").Create("c"));
            base.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("size").WithDescription("optional target effect size to compute confidence (defaults to " + Allcea.DEFAULT_RELATIVE_SIZE + " for relative and " + Allcea.DEFAULT_ABSOLUTE_SIZE + " for absolute).").Create("s"));
            base.Options.AddOption(OptionBuilder.Factory.WithDescription("shows this help message.").Create("h"));

            this._inputPath = null;
            this._judgedPath = null;
            this._estimatedPath = null;
            this._decimalDigits = Allcea.DEFAULT_DECIMAL_DIGITS;
            this._batchNum = Allcea.DEFAULT_NUMBER_OF_BATCHES;
            this._batchSize = Allcea.DEFAULT_BATCH_SIZE;
            this._confEstimator = null;
        }

        public override void CheckOptions(CommandLine cmd)
        {
            // Target and confidence estimator
            double confidence = Allcea.DEFAULT_CONFIDENCE;
            if (cmd.HasOption('c')) {
                confidence = AbstractCommand.CheckConfidence(cmd.GetOptionValue('c'));
            }
            EvaluationTargets target = AbstractCommand.CheckTarget(cmd.GetOptionValue('t'));
            double sizeRel = Allcea.DEFAULT_RELATIVE_SIZE;
            double sizeAbs = Allcea.DEFAULT_ABSOLUTE_SIZE;
            if (cmd.HasOption('s')) {
                switch (target) {
                    case EvaluationTargets.Relative: sizeRel = AbstractCommand.CheckRelativeSize(cmd.GetOptionValue('s')); break;
                    case EvaluationTargets.Absolute: sizeAbs = AbstractCommand.CheckAbsoluteSize(cmd.GetOptionValue('s')); break;
                }
            }
            this._confEstimator = new NormalConfidenceEstimator(confidence, sizeRel, sizeAbs);
            // Batches
            this._batchNum = AbstractCommand.CheckBatchNumber(cmd.GetOptionValue('b'));
            this._batchSize = AbstractCommand.CheckBatchSize(cmd.GetOptionValue('n'));
            // Files
            this._inputPath = AbstractCommand.CheckInputFile(cmd.GetOptionValue('i'));
            if (cmd.HasOption('j')) {
                this._judgedPath = AbstractCommand.CheckJudgedFile(cmd.GetOptionValue('j'));
            }
            this._estimatedPath = AbstractCommand.CheckEstimatedFile(cmd.GetOptionValue('e'));
        }

        public override void Run()
        {
            throw new NotImplementedException();
        }
    }
}