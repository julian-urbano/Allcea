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
        protected EvaluationTargets _target;
        protected int _batchNum;
        protected int _batchSize;
        protected IConfidenceEstimator _confEstimator;
        protected double _confidence;

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
            // Read files
            IEnumerable<Run> runs = AbstractCommand.ReadInputFile(this._inputPath);
            IEnumerable<RelevanceEstimate> judged = new RelevanceEstimate[] { };
            if (this._judgedPath != null) {
                judged = AbstractCommand.ReadKnownJudgments(this._judgedPath);
            }
            IEnumerable<RelevanceEstimate> estimates = AbstractCommand.ReadEstimatedJudgments(this._estimatedPath);
            // Instantiate estimate store and measure
            RelevanceEstimateStore store = new RelevanceEstimateStore(estimates);
            store.Update(judged);
            IMeasure measure = new CG(100); //TODO: max relevance

            // Compile list of all query-doc-sys-rank tuples
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> qdsRanks = AbstractCommand.ToQueryDocumentSystemRanks(runs);
            // Re-structure estimates
            Dictionary<string, Dictionary<string, RelevanceEstimate>> qdEstimates = new Dictionary<string, Dictionary<string, RelevanceEstimate>>();
            foreach (var est in estimates) {
                Dictionary<string, RelevanceEstimate> dEstimates = null;
                if (!qdEstimates.TryGetValue(est.Query, out dEstimates)) {
                    dEstimates = new Dictionary<string, RelevanceEstimate>();
                    qdEstimates.Add(est.Query, dEstimates);
                }
                dEstimates.Add(est.Document, est);
            }
            // Remove judged query-docs
            foreach (var j in judged) {
                Dictionary<string, RelevanceEstimate> dEstimates = null;
                if (qdEstimates.TryGetValue(j.Query, out dEstimates)) {
                    dEstimates.Remove(j.Document);
                    if (dEstimates.Count == 0) {
                        qdEstimates.Remove(j.Query);
                    }
                }
            }

            bool needsNext = false;
            // Re-structure runs for efficient access
            Dictionary<string, Dictionary<string, Run>> sqRuns = AbstractCommand.ToSystemQueryRuns(runs);
            if (this._target == EvaluationTargets.Relative) {
                // Estimate per-query relative effectiveness
                Dictionary<string, Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>> ssqRels =
                    EvaluateCommand.GetSystemSystemQueryRelatives(sqRuns, measure, store, this._confEstimator);
                // Average (already sorted)
                List<RelativeEffectivenessEstimate> relSorted = EvaluateCommand.GetSortedMeanRelatives(ssqRels, this._confEstimator);

                if (relSorted.Average(r => r.Confidence) < this._confidence) {
                    needsNext = true;
                    measure.ComputeQueryDocumentWeights(qdEstimates, qdsRanks, ssqRels);
                }
            } else {
                // Estimate per-query absolute effectiveness
                Dictionary<string, Dictionary<string, AbsoluteEffectivenessEstimate>> sqAbss =
                    EvaluateCommand.GetSystemQueryAbsolutes(sqRuns, measure, store, this._confEstimator);
                // Average and sort
                List<AbsoluteEffectivenessEstimate> absSorted = EvaluateCommand.GetSortedMeanAbsolutes(sqAbss, this._confEstimator);

                if (absSorted.Average(a => a.Confidence) < this._confidence) {
                    needsNext = true;
                    measure.ComputeQueryDocumentWeights(qdEstimates, qdsRanks, sqAbss);
                }
            }

            List<List<RelevanceEstimate>> batches = new List<List<RelevanceEstimate>>();
            if (needsNext) {
                foreach (var dEstimates in qdEstimates) {
                    string query = dEstimates.Key;
                    var sorted = dEstimates.Value.Select(w => w.Value).OrderByDescending(w => w.Weight).ToList();
                    int added = 0;
                    while (sorted.Count > 0 && added < this._batchNum) {
                        var next = sorted.Take(this._batchSize);
                        batches.Add(new List<RelevanceEstimate>(next));
                        sorted.RemoveRange(0, next.Count());
                        added++;
                    }
                }
                batches = batches.OrderByDescending(b => b.Sum(r => r.Weight)).ToList();
            }
            for (int b = 0; b < this._batchNum && b < batches.Count; b++) {
                Console.WriteLine("# Batch: " + (b + 1));
                Console.WriteLine("# Weight: " + batches[b].Sum(r => r.Weight));
                Console.WriteLine("###################");
                foreach (var d in batches[b]) {
                    Console.WriteLine(d.Query + "\t" + d.Document);
                }
            }
        }
    }
}