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

        public RelativeEffectivenessEstimate Estimate(Run runA, Run runB, IRelevanceEstimator estimator)
        {
            double e = 0, var = 0;

            // Traverse docs retrieved by A
            HashSet<string> inRunA = new HashSet<string>(); // retrieved by run A
            foreach (string doc in runA.Documents) {
                RelevanceEstimate docEst = estimator.Estimate(runA.Query, doc);
                e += docEst.Expectation;
                var += docEst.Variance;
                inRunA.Add(doc);
            }
            // Traverse docs retrieved by B
            foreach (string doc in runB.Documents) {
                RelevanceEstimate docEst = estimator.Estimate(runB.Query, doc);
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

            return new RelativeEffectivenessEstimate(runA.System, runB.System, runA.Query, e, var);
        }
        public AbsoluteEffectivenessEstimate Estimate(Run run, IRelevanceEstimator estimator)
        {
            double e = 0, var = 0;

            // Traverse docs retrieved
            foreach (string doc in run.Documents) {
                RelevanceEstimate docEst = estimator.Estimate(run.Query, doc);
                e += docEst.Expectation;
                var += docEst.Variance;
            }
            // Compute average
            e /= run.Documents.Count();
            var /= run.Documents.Count() * run.Documents.Count();
            // Normalize between 0 and 1
            e /= this.MaxRelevance;
            var /= this.MaxRelevance * this.MaxRelevance;

            return new AbsoluteEffectivenessEstimate(run.System, run.Query, e, var);
        }
    }
}
