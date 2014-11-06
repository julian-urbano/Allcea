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
                string confidenceString = cmd.GetOptionValue('c');
                if (!Double.TryParse(confidenceString, out confidence) || confidence < 0 || confidence >= 1) {
                    throw new ArgumentException("'" + confidenceString + "' is not a valid confidence level for interval estimates.");
                }
            }
            if (cmd.HasOption('s')) {
                string[] sizeStrings = cmd.GetOptionValues('s');
                if (sizeStrings.Length != 2) {
                    throw new ArgumentException("Must provide two target effect sizes: relative and absolute.");
                }
                if (!Double.TryParse(sizeStrings[0], out sizeRel) || sizeRel < 0 || sizeRel >= 1) {
                    throw new ArgumentException("'" + sizeStrings[1] + "' is not a valid target relative effect size.");
                }
                if (!Double.TryParse(sizeStrings[1], out sizeAbs) || sizeAbs < 0 || sizeAbs >= 1) {
                    throw new ArgumentException("'" + sizeStrings[1] + "' is not a valid target absolute effect size.");
                }
            }
            this._confEstimator = new NormalConfidenceEstimator(confidence, sizeRel, sizeAbs);
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
            // Estimates file
            this._estimatedPath = cmd.GetOptionValue('e');
            if (!File.Exists(this._estimatedPath)) {
                throw new ArgumentException("Estimated judgments file '" + this._estimatedPath + "' does not exist.");
            }
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
            RelevanceEstimateStore store = new RelevanceEstimateStore(judged, estimates);
            IMeasure measure = new CG(100); //TODO: max relevance

            // Re-structure runs for efficient access
            Dictionary<string, Dictionary<string, Run>> sqRuns = new Dictionary<string, Dictionary<string, Run>>(); // [sys [query run]]
            foreach (Run r in runs) {
                Dictionary<string, Run> qRuns = null;
                if (!sqRuns.TryGetValue(r.System, out qRuns)) {
                    qRuns = new Dictionary<string, Run>();
                    sqRuns.Add(r.System, qRuns);
                }
                qRuns.Add(r.Query, r);
            }

            // Estimate per-query absolute effectiveness
            Dictionary<string, Dictionary<string, AbsoluteEffectivenessEstimate>> sqAbsEstimates =
                new Dictionary<string, Dictionary<string, AbsoluteEffectivenessEstimate>>(); // [sys [query abs]]
            foreach (var sqRun in sqRuns) {
                Dictionary<string, AbsoluteEffectivenessEstimate> qAbs = new Dictionary<string, AbsoluteEffectivenessEstimate>();
                foreach (var qRun in sqRun.Value) {
                    qAbs.Add(qRun.Key, measure.Estimate(qRun.Value, store, this._confEstimator));
                }
                sqAbsEstimates.Add(sqRun.Key, qAbs);
            }
            // Average and sort
            List<AbsoluteEffectivenessEstimate> absSorted = new List<AbsoluteEffectivenessEstimate>();
            foreach (var sqAbsEst in sqAbsEstimates) {
                double e = sqAbsEst.Value.Sum(qAbsEst => qAbsEst.Value.Expectation);
                double var = sqAbsEst.Value.Sum(qAbsEst => qAbsEst.Value.Variance);
                e /= sqAbsEst.Value.Count;
                var /= sqAbsEst.Value.Count * sqAbsEst.Value.Count;

                Estimate est = new Estimate(e, var);

                absSorted.Add(new AbsoluteEffectivenessEstimate(sqAbsEst.Key, "[all]",
                    e, var,
                    this._confEstimator.EstimateInterval(est), this._confEstimator.EstimateAbsoluteConfidence(est)));
            }
            absSorted = absSorted.OrderByDescending(est => est.Expectation).ToList();

            // Estimate per-query relative effectiveness
            Dictionary<string, Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>> ssqRelEstimates =
                new Dictionary<string, Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>>(); // [sysA [sysB [query rel]]]
            for (int i = 0; i < absSorted.Count - 1; i++) {
                string sysA = absSorted[i].System;
                var runsA = sqRuns[sysA];
                Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>> sqRelEstimates = new Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>();
                for (int j = i + 1; j < absSorted.Count; j++) {
                    Dictionary<string, RelativeEffectivenessEstimate> qRelEstimates = new Dictionary<string, RelativeEffectivenessEstimate>();
                    string sysB = absSorted[j].System;
                    var runsB = sqRuns[sysB];
                    foreach (var qRun in runsA) {
                        qRelEstimates.Add(qRun.Key, measure.Estimate(qRun.Value, runsB[qRun.Key], store, this._confEstimator));
                    }
                    sqRelEstimates.Add(sysB, qRelEstimates);
                }
                ssqRelEstimates.Add(sysA, sqRelEstimates);
            }
            // Average (already sorted)
            List<RelativeEffectivenessEstimate> relSorted = new List<RelativeEffectivenessEstimate>();
            for (int i = 0; i < absSorted.Count - 1; i++) {
                string sysA = absSorted[i].System;
                Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>> sqRelEstimates = ssqRelEstimates[sysA];
                for (int j = i + 1; j < absSorted.Count; j++) {
                    string sysB = absSorted[j].System;
                    Dictionary<string, RelativeEffectivenessEstimate> qRelEstimates = sqRelEstimates[sysB];
                    double e = qRelEstimates.Values.Sum(relEst => relEst.Expectation);
                    double var = qRelEstimates.Values.Sum(relEst => relEst.Variance);
                    e /= qRelEstimates.Values.Count;
                    var /= qRelEstimates.Values.Count * qRelEstimates.Values.Count;

                    Estimate est = new Estimate(e, var);

                    relSorted.Add(new RelativeEffectivenessEstimate(sysA, sysB, "[all]",
                        e, var,
                        this._confEstimator.EstimateInterval(est), this._confEstimator.EstimateRelativeConfidence(est)));
                }
            }

            // Output estimates
            TabSeparated io = new TabSeparated(this._decimalDigits);
            Console.WriteLine("---------------------------");
            Console.WriteLine("Mean Absolute Effectiveness");
            Console.WriteLine("---------------------------");
            ((IWriter<AbsoluteEffectivenessEstimate>)io).Write(Console.Out, absSorted);
            Console.WriteLine();
            Console.WriteLine("---------------------------");
            Console.WriteLine("Mean Relative Effectiveness");
            Console.WriteLine("---------------------------");
            ((IWriter<RelativeEffectivenessEstimate>)io).Write(Console.Out, relSorted);
        }
    }
}