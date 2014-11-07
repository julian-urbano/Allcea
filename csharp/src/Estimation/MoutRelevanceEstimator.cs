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
    public class MoutRelevanceEstimator : IRelevanceEstimator
    {
        protected Dictionary<string, Dictionary<string, double>> _fSYS; // [query, [doc, fSYS]]
        protected double _OV;
        protected Dictionary<string, Dictionary<string, bool>> _sGEN; // [query, [doc, sGEN]]
        protected Dictionary<string, Dictionary<string, double>> _fGEN; // [query, [doc, fGEN]]
        protected Dictionary<string, Dictionary<string, double>> _fART; // [query, [doc, fART]]

        protected OrdinalLogisticRegression _model;
        protected static readonly double[] LABELS = new double[] { 5, 15, 25, 35, 45, 55, 65, 75, 85, 95 };
        protected static readonly double[] ALPHAS = new double[] { -0.5092, -1.2231, -1.7919, -2.2787, -2.7216, -3.1956, -3.8044, -4.6928, -5.9567 };
        protected static readonly double[] BETAS = new double[] { -17.4721, 0.1336, 26.455, 2.9111, 2.0443, 5.4544, -3.4851 };
        protected IRelevanceEstimator _defaultEstimator;

        public MoutRelevanceEstimator(IEnumerable<Run> runs, IEnumerable<Metadata> metadata)
        {
            // Instantiate model: fSYS, OV, fSYS:OV, fART, sGEN, fGEN, sGEN:fGEN
            this._model = new OrdinalLogisticRegression(MoutRelevanceEstimator.LABELS, MoutRelevanceEstimator.ALPHAS, MoutRelevanceEstimator.BETAS);
            this._defaultEstimator = new UniformRelevanceEstimator(100);
            // Number of systems and metadata
            int nSys = runs.Select(r => r.System).Distinct().Count();
            Dictionary<string, string> artists = new Dictionary<string, string>();// [doc, artist]
            Dictionary<string, string> genres = new Dictionary<string, string>();// [doc, genre]
            foreach (var m in metadata) {
                artists[m.Document] = m.Artist;
                genres[m.Document] = m.Genre;
            }
            // fSYS and OV
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
            // OV
            this._OV = ((double)this._fSYS.Sum(q => q.Value.Count)) / (nSys * this._fSYS.Count * runs.First().Documents.Count());
            // sGEN, fGEN and fART, replicate query-doc structure from fSYS
            this._sGEN = new Dictionary<string, Dictionary<string, bool>>();
            this._fGEN = new Dictionary<string, Dictionary<string, double>>();
            this._fART = new Dictionary<string, Dictionary<string, double>>();
            foreach (var qfSYS in this._fSYS) {
                string query = qfSYS.Key;
                Dictionary<string, bool> qsGEN = new Dictionary<string, bool>();
                Dictionary<string, double> qfGEN = new Dictionary<string, double>();
                Dictionary<string, double> qfART = new Dictionary<string, double>();
                foreach (var qdfSYS in qfSYS.Value) {
                    string doc = qdfSYS.Key;
                    // sGEN and fGEN
                    if (genres.ContainsKey(doc)) {
                        string docGEN = genres[doc];
                        // sGEN
                        if (genres.ContainsKey(query)) {
                            qsGEN.Add(qdfSYS.Key, docGEN == genres[qfSYS.Key]);
                        } else {
                            //qsGEN.Add(doc, null);
                        }
                        // fGEN
                        double docfGEN = 0;
                        int docfGENnotnull = 0;
                        // traverse all documents individually
                        foreach (var qdfSYS2 in qfSYS.Value) {
                            string doc2 = qdfSYS2.Key;
                            if (genres.ContainsKey(doc2)) {
                                string doc2GEN = genres[doc2];
                                if (docGEN == doc2GEN) {
                                    docfGEN++;
                                }
                                docfGENnotnull++;
                            }
                        }
                        qfGEN.Add(doc, docfGEN / docfGENnotnull);
                    } else {
                        //qsGEN.Add(doc, null);
                        //qfGEN.Add(doc, null);
                    }
                    // fART
                    if (artists.ContainsKey(doc)) {
                        string docART = artists[doc];
                        double docfART = 0;
                        int docfARTnotnull = 0;
                        // traverse all documents individually
                        foreach (var qdfSYS2 in qfSYS.Value) {
                            string doc2 = qdfSYS2.Key;
                            if (artists.ContainsKey(doc2)) {
                                string doc2ART = artists[doc2];
                                if (docART == doc2ART) {
                                    docfART++;
                                }
                                docfARTnotnull++;
                            }
                        }
                        qfART.Add(doc, docfART / docfARTnotnull);
                    } else {
                        //qfART.Add(doc, null);
                    }
                }
                this._sGEN.Add(query, qsGEN);
                this._fGEN.Add(query, qfGEN);
                this._fART.Add(query, qfART);
            }
        }

        public RelevanceEstimate Estimate(string query, string doc)
        {
            Dictionary<string, double> qfSYS = null;
            Dictionary<string, bool> qsGEN = null;
            Dictionary<string, double> qfGEN = null;
            Dictionary<string, double> qfART = null;
            // Do we have features for the query?
            if (this._fSYS.TryGetValue(query, out qfSYS) && this._sGEN.TryGetValue(query, out qsGEN) &&
                this._fGEN.TryGetValue(query, out qfGEN) && this._fART.TryGetValue(query, out qfART)) {
                double fSYS = 0;
                bool sGEN = false;
                double fGEN = 0;
                double fART = 0;
                // Do we have features for the document?
                if (qfSYS.TryGetValue(doc, out fSYS) && qsGEN.TryGetValue(doc, out sGEN) &&
                    qfGEN.TryGetValue(doc, out fGEN) && qfART.TryGetValue(doc, out fART)) {
                    double[] thetas = new double[] { fSYS, this._OV, fSYS * this._OV, fART, sGEN ? 1 : 0, fGEN, sGEN ? fGEN : 0 };
                    double[] eval = this._model.Evaluate(thetas);
                    return new RelevanceEstimate(query, doc, eval[0], eval[1]);
                }
            }
            // If here, some feature was missing, so return default estimate
            return this._defaultEstimator.Estimate(query, doc);
        }

        public void Update(RelevanceEstimate est)
        {
            // Nothing to do
            this._defaultEstimator.Update(est);
        }
    }
}