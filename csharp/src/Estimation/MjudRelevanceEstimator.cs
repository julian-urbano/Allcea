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


namespace jurbano.Allcea.Estimation
{
    public class MjudRelevanceEstimator : IRelevanceEstimator
    {
        protected Dictionary<string, Dictionary<string, double>> _fSYS; // [query, [doc, fSYS]]
        protected Dictionary<string, Dictionary<string, double>> _aSYS; // [query, [sys, aSYS]]
        protected Dictionary<string, Dictionary<string, double>> _aART; // [query, [artist, aART]]

        protected OrdinalLogisticRegression _model;
        protected static readonly double[] LABELS = new double[] { 5, 15, 25, 35, 45, 55, 65, 75, 85, 95 };
        protected static readonly double[] ALPHAS = new double[] { -2.7554, -4.9168, -7.0128, -9.001, -10.8548, -12.7158, -14.6722, -16.8831, -19.2536 };
        protected static readonly double[] BETAS = new double[] { 0.7954, 0.0128, 0.2078 };
        protected IRelevanceEstimator _defaultEstimator;

        public MjudRelevanceEstimator(IEnumerable<Run> runs, IEnumerable<Metadata> metadata, IEnumerable<RelevanceEstimate> judged)
        {
            // Instantiate model: fSYS, aSYS, aART
            this._model = new OrdinalLogisticRegression(MjudRelevanceEstimator.LABELS, MjudRelevanceEstimator.ALPHAS, MjudRelevanceEstimator.BETAS);
            this._defaultEstimator = new MoutRelevanceEstimator(runs, metadata);

            // Number of systems and metadata
            int nSys = runs.Select(r => r.System).Distinct().Count();
            Dictionary<string, string> artists = new Dictionary<string, string>(); // [doc, artist]
            foreach (var m in metadata) {
                artists[m.Document] = m.Artist;
            }
            // fSYS
            this._fSYS = new Dictionary<string, Dictionary<string, double>>();
            foreach (var run in runs) {
                Dictionary<string, double> qfSYS = null;
                if (!this._fSYS.TryGetValue(run.Query, out qfSYS)) {
                    qfSYS = new Dictionary<string, double>();
                    this._fSYS.Add(run.Query, qfSYS);
                }
                foreach (var doc in run.Documents) {
                    double qdfSYS = 0;
                    qfSYS.TryGetValue(doc, out qdfSYS);
                    qfSYS[doc] = qdfSYS + 1.0 / nSys;
                }
            }

            // TODO

            // Incorporate known judgments
            foreach (var est in judged) {
                this.Update(est);
            }
        }

        public RelevanceEstimate Estimate(string query, string doc)
        {
            Dictionary<string, double> qfSYS = null;
            Dictionary<string, double> qaSYS = null;
            Dictionary<string, double> qaART = null;
            // Do we have features for the query?
            if (this._fSYS.TryGetValue(query, out qfSYS) && this._aSYS.TryGetValue(query, out qaSYS) &&this._aART.TryGetValue(query, out qaART) ) {
                double fSYS = 0;
                double aSYS = 0;
                double aART = 0;
                // Do we have features for the document?
                if (qfSYS.TryGetValue(doc, out fSYS) && qaSYS.TryGetValue(doc, out aSYS) && qaART.TryGetValue(doc, out aART)) {
                    double[] thetas = new double[] { fSYS, aSYS, aART };
                    double[] eval = this._model.Evaluate(thetas);
                    return new RelevanceEstimate(query, doc, eval[0], eval[1]);
                }
            }
            // If here, some feature was missing, so return default estimate
            return this._defaultEstimator.Estimate(query, doc);
        }

        public void Update(RelevanceEstimate est)
        {
            // TODO

            this._defaultEstimator.Update(est);
        }
    }
}
