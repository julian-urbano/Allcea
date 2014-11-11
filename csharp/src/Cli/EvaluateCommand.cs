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
using System.IO;
using jurbano.Allcea.Model;
using jurbano.Allcea.Estimation;
using jurbano.Allcea.Evaluation;

namespace jurbano.Allcea.Cli
{
    public class EvaluateCommand : AbstractCommand
    {
        public override string OptionsFooter { get { return null; } }

        protected string _inputPath;
        protected string _judgedPath;
        protected string _estimatedPath;
        protected int _decimalDigits;
        protected IConfidenceEstimator _confEstimator;

        public EvaluateCommand()
        {
            base.Options = new Options();
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with system runs.").Create("i"));
            base.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("file").WithDescription("optional path to file with known judgments.").Create("j"));
            base.Options.AddOption(OptionBuilder.Factory.IsRequired().HasArg().WithArgName("file").WithDescription("path to the file with estimated judgments.").Create("e"));
            base.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("conf").WithDescription("optional confidence level for interval estimates (defaults to " + Allcea.DEFAULT_CONFIDENCE + ").").Create("c"));
            base.Options.AddOption(OptionBuilder.Factory.HasArgs(2).WithArgName("rel> <abs").WithDescription("optional target effect sizes to compute confidence (defaults to " + Allcea.DEFAULT_RELATIVE_SIZE + " and " + Allcea.DEFAULT_ABSOLUTE_SIZE + ").").Create("s"));
            base.Options.AddOption(OptionBuilder.Factory.HasArg().WithArgName("digits").WithDescription("optional number of fractional digits to output (defaults to " + Allcea.DEFAULT_DECIMAL_DIGITS + ")").Create("d"));
            base.Options.AddOption(OptionBuilder.Factory.WithDescription("shows this help message.").Create("h"));

            this._inputPath = null;
            this._judgedPath = null;
            this._estimatedPath = null;
            this._decimalDigits = Allcea.DEFAULT_DECIMAL_DIGITS;
            this._confEstimator = null;
        }

        public override void CheckOptions(CommandLine cmd)
        {
            // Confidence estimator
            double confidence = Allcea.DEFAULT_CONFIDENCE;
            double sizeRel = Allcea.DEFAULT_RELATIVE_SIZE;
            double sizeAbs = Allcea.DEFAULT_ABSOLUTE_SIZE;
            if (cmd.HasOption('c')) {
                confidence = AbstractCommand.CheckConfidence(cmd.GetOptionValue('c'));
            }
            if (cmd.HasOption('s')) {
                string[] sizeStrings = cmd.GetOptionValues('s');
                if (sizeStrings.Length != 2) {
                    throw new ArgumentException("Must provide two target effect sizes: relative and absolute.");
                }
                sizeRel = AbstractCommand.CheckRelativeSize(sizeStrings[0]);
                sizeAbs = AbstractCommand.CheckAbsoluteSize(sizeStrings[1]);
            }
            this._confEstimator = new NormalConfidenceEstimator(confidence, sizeRel, sizeAbs);
            // Double format
            if (cmd.HasOption('d')) {
                this._decimalDigits = AbstractCommand.CheckDigits(cmd.GetOptionValue('d'));
            }
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

            // Re-structure runs for efficient access
            Dictionary<string, Dictionary<string, Run>> sqRuns = AbstractCommand.ToSystemQueryRuns(runs);

            // Estimate per-query absolute effectiveness
            Dictionary<string, Dictionary<string, AbsoluteEffectivenessEstimate>> sqAbss =
                EvaluateCommand.GetSystemQueryAbsolutes(sqRuns, measure, store, this._confEstimator);
            // Average and sort
            List<AbsoluteEffectivenessEstimate> absSorted = EvaluateCommand.GetSortedMeanAbsolutes(sqAbss, this._confEstimator);

            // Estimate per-query relative effectiveness
            Dictionary<string, Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>> ssqRels =
                EvaluateCommand.GetSystemSystemQueryRelatives(sqRuns, measure, store, this._confEstimator);
            // Average (already sorted)
            List<RelativeEffectivenessEstimate> relSorted = EvaluateCommand.GetSortedMeanRelatives(ssqRels, this._confEstimator);

            // Output estimates
            TabSeparated io = new TabSeparated(this._decimalDigits);
            ((IWriter<AbsoluteEffectivenessEstimate>)io).Write(Console.Out, absSorted);
            ((IWriter<RelativeEffectivenessEstimate>)io).Write(Console.Out, relSorted);
        }

        internal static Dictionary<string, Dictionary<string, AbsoluteEffectivenessEstimate>> GetSystemQueryAbsolutes(
            Dictionary<string, Dictionary<string, Run>> sqRuns,
            IMeasure measure, IRelevanceEstimator relEstimator, IConfidenceEstimator confEstimator)
        {
            Dictionary<string, Dictionary<string, AbsoluteEffectivenessEstimate>> sqAbss = new Dictionary<string, Dictionary<string, AbsoluteEffectivenessEstimate>>();
            foreach (var sqRun in sqRuns) {
                Dictionary<string, AbsoluteEffectivenessEstimate> qAbs = new Dictionary<string, AbsoluteEffectivenessEstimate>();
                foreach (var qRun in sqRun.Value) {
                    qAbs.Add(qRun.Key, measure.Estimate(qRun.Value, relEstimator, confEstimator));
                }
                sqAbss.Add(sqRun.Key, qAbs);
            }
            return sqAbss;
        }
        internal static List<AbsoluteEffectivenessEstimate> GetSortedMeanAbsolutes(
            Dictionary<string, Dictionary<string, AbsoluteEffectivenessEstimate>> sqAbss,
            IConfidenceEstimator confEstimator)
        {
            // Compute means
            List<AbsoluteEffectivenessEstimate> absSorted = new List<AbsoluteEffectivenessEstimate>();
            foreach (var sqAbsEst in sqAbss) {
                double e = sqAbsEst.Value.Sum(qAbsEst => qAbsEst.Value.Expectation);
                double var = sqAbsEst.Value.Sum(qAbsEst => qAbsEst.Value.Variance);
                e /= sqAbsEst.Value.Count;
                var /= sqAbsEst.Value.Count * sqAbsEst.Value.Count;
                Estimate est = new Estimate(e, var);

                absSorted.Add(new AbsoluteEffectivenessEstimate(sqAbsEst.Key, "[all]",
                    e, var,
                    confEstimator.EstimateInterval(est), confEstimator.EstimateAbsoluteConfidence(est)));
            }
            // and sort
            absSorted = absSorted.OrderByDescending(est => est.Expectation).ToList();
            return absSorted;
        }
        internal static Dictionary<string, Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>> GetSystemSystemQueryRelatives(
            Dictionary<string, Dictionary<string, Run>> sqRuns,
            IMeasure measure, IRelevanceEstimator relEstimator, IConfidenceEstimator confEstimator)
        {
            Dictionary<string, Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>> ssqRelEstimates =
                new Dictionary<string, Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>>(); // [sysA [sysB [query rel]]]
            string[] allSystems = sqRuns.Keys.ToArray();

            Parallel.For(0, allSystems.Length - 1, i => {
                string sysA = allSystems[i];
                var runsA = sqRuns[sysA];
                Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>> sqRelEstimates = new Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>();
                for (int j = i + 1; j < allSystems.Length; j++) {
                    Dictionary<string, RelativeEffectivenessEstimate> qRelEstimates = new Dictionary<string, RelativeEffectivenessEstimate>();
                    string sysB = allSystems[j];
                    var runsB = sqRuns[sysB];
                    foreach (var qRun in runsA) {
                        qRelEstimates.Add(qRun.Key, measure.Estimate(qRun.Value, runsB[qRun.Key], relEstimator, confEstimator));
                    }
                    sqRelEstimates.Add(sysB, qRelEstimates);
                }
                lock (ssqRelEstimates) {
                    ssqRelEstimates.Add(sysA, sqRelEstimates);
                }
            });

            //for (int i = 0; i < allSystems.Length - 1; i++) {
            //    string sysA = allSystems[i];
            //    var runsA = sqRuns[sysA];
            //    Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>> sqRelEstimates = new Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>();
            //    for (int j = i + 1; j < allSystems.Length; j++) {
            //        Dictionary<string, RelativeEffectivenessEstimate> qRelEstimates = new Dictionary<string, RelativeEffectivenessEstimate>();
            //        string sysB = allSystems[j];
            //        var runsB = sqRuns[sysB];
            //        foreach (var qRun in runsA) {
            //            qRelEstimates.Add(qRun.Key, measure.Estimate(qRun.Value, runsB[qRun.Key], relEstimator, confEstimator));
            //        }
            //        sqRelEstimates.Add(sysB, qRelEstimates);
            //    }
            //    ssqRelEstimates.Add(sysA, sqRelEstimates);
            //}
            return ssqRelEstimates;
        }
        internal static List<RelativeEffectivenessEstimate> GetSortedMeanRelatives(
            Dictionary<string, Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>> ssqRels,
            IConfidenceEstimator confEstimator)
        {
            // Compute means
            List<RelativeEffectivenessEstimate> rels = new List<RelativeEffectivenessEstimate>();
            foreach (var sqRels in ssqRels) {
                foreach (var qRels in sqRels.Value) {
                    string sysA = sqRels.Key;
                    string sysB = qRels.Key;
                    double e = qRels.Value.Values.Sum(relEst => relEst.Expectation);
                    double var = qRels.Value.Values.Sum(relEst => relEst.Variance);
                    e /= qRels.Value.Values.Count;
                    var /= qRels.Value.Values.Count * qRels.Value.Values.Count;
                    if (e < 0) {
                        e = -e;
                        sysA = qRels.Key;
                        sysB = sqRels.Key;
                    }
                    Estimate est = new Estimate(e, var);
                    rels.Add(new RelativeEffectivenessEstimate(sysA, sysB, "[all]",
                        e, var,
                        confEstimator.EstimateInterval(est), confEstimator.EstimateRelativeConfidence(est)));
                }
            }
            // and sort
            var groups = rels.GroupBy(r => r.SystemA).OrderByDescending(g => g.Count());
            List<RelativeEffectivenessEstimate> relSorted = new List<RelativeEffectivenessEstimate>();
            foreach (var group in groups) {
                relSorted.AddRange(group.OrderBy(r => r.Expectation));
            }
            return relSorted;
        }
    }
}