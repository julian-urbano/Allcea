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
using jurbano.Allcea.Estimation;

namespace jurbano.Allcea.Evaluation
{
    [global::System.Diagnostics.DebuggerDisplay("MaxRelevance={MaxRelevance}")]
    public class CG : IMeasure
    {
        public int MaxRelevance { get; protected set; }

        public CG(int maxrelevance)
        {
            if (maxrelevance < 1) {
                throw new ArgumentException("The maximum relevance level cannot be less than 1.");
            }
            this.MaxRelevance = maxrelevance;
        }

        public RelativeEffectivenessEstimate Estimate(Run runA, Run runB, IRelevanceEstimator relEstimator, IConfidenceEstimator confEstimator)
        {
            double e = 0, var = 0;

            // Traverse docs retrieved by A
            HashSet<string> inRunA = new HashSet<string>(); // retrieved by run A
            foreach (string doc in runA.Documents) {
                RelevanceEstimate docEst = relEstimator.Estimate(runA.Query, doc);
                e += docEst.Expectation;
                var += docEst.Variance;
                inRunA.Add(doc);
            }
            // Traverse docs retrieved by B
            foreach (string doc in runB.Documents) {
                RelevanceEstimate docEst = relEstimator.Estimate(runB.Query, doc);
                e -= docEst.Expectation;
                if (inRunA.Contains(doc)) {
                    // If retrieved in both runs, does not contribute to variance
                    var -= docEst.Variance;
                } else {
                    var += docEst.Variance;
                }
            }
            // Compute average
            e /= inRunA.Count;
            var /= inRunA.Count * inRunA.Count;
            // Normalize between 0 and 1
            e /= this.MaxRelevance;
            var /= this.MaxRelevance * this.MaxRelevance;

            Estimate est = new Estimate(e, var);

            return new RelativeEffectivenessEstimate(runA.System, runB.System, runA.Query,
                e, var,
                confEstimator.EstimateInterval(est), confEstimator.EstimateRelativeConfidence(est));
        }
        public AbsoluteEffectivenessEstimate Estimate(Run run, IRelevanceEstimator relEstimator, IConfidenceEstimator confEstimator)
        {
            double e = 0, var = 0;

            // Traverse docs retrieved
            foreach (string doc in run.Documents) {
                RelevanceEstimate docEst = relEstimator.Estimate(run.Query, doc);
                e += docEst.Expectation;
                var += docEst.Variance;
            }
            // Compute average
            e /= run.Documents.Count();
            var /= run.Documents.Count() * run.Documents.Count();
            // Normalize between 0 and 1
            e /= this.MaxRelevance;
            var /= this.MaxRelevance * this.MaxRelevance;

            Estimate est = new Estimate(e, var);

            return new AbsoluteEffectivenessEstimate(run.System, run.Query,
                e, var,
                confEstimator.EstimateInterval(est), confEstimator.EstimateAbsoluteConfidence(est));
        }

        public void ComputeQueryDocumentWeights(
            Dictionary<string, Dictionary<string, RelevanceEstimate>> qdEstimates,
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> qdsRanks,
            Dictionary<string, Dictionary<string, Dictionary<string, RelativeEffectivenessEstimate>>> ssqRels)
        {
            int nSys = ssqRels.Count + 1;
            // Iterate query-docs
            foreach (var dEstimates in qdEstimates) {
                string query = dEstimates.Key;
                foreach (var estimate in dEstimates.Value) {
                    estimate.Value.Weight = 0;
                    string doc = estimate.Key;
                    // n(n-1)/2 system pairs may contain it, assume they all do and subtract
                    estimate.Value.Weight = nSys * (nSys - 1) / 2;
                    // c systems contain it, so c(c-1)/2 pairs contribute weight 0
                    int count = qdsRanks[query][doc].Count;
                    estimate.Value.Weight -= (count * (count - 1) / 2);
                    // c systems don't contain it, so c(c-1)/2 pairs contribute weight 0 
                    count = nSys - count;
                    estimate.Value.Weight -= (count * (count - 1) / 2);
                }
            }
        }
        public void ComputeQueryDocumentWeights(
            Dictionary<string, Dictionary<string, RelevanceEstimate>> qdEstimates,
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> qdsRanks,
            Dictionary<string, Dictionary<string, AbsoluteEffectivenessEstimate>> sqAbss)
        {
            // Iterate query-docs
            foreach (var dEstimates in qdEstimates) {
                string query = dEstimates.Key;
                foreach (var estimate in dEstimates.Value) {
                    estimate.Value.Weight = 0;
                    string doc = estimate.Key;
                    // Iterate all sys-sys
                    var ranks = qdsRanks[query][doc];
                    foreach (var rank in ranks) {
                        estimate.Value.Weight += sqAbss[rank.Key][query].Variance;
                    }
                }
            }
        }
    }
}
