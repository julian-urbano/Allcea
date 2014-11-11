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
        protected Dictionary<string, double> _fSYS; // [querydoc, fSYS]
        protected double _OV;
        protected Dictionary<string, double> _aSYS; // [querydoc, aSYS]
        protected Dictionary<string, double> _aART; // [querydoc, aART]

        protected Dictionary<string, List<double>> _sRels; // [sys, [rel]]
        protected Dictionary<string, List<double>> _qaRels; // [queryartist, [rel]]
        protected bool _needsUpdate;

        protected Dictionary<string, string> _dArtists; // [doc, artist]
        protected Dictionary<string, Dictionary<string, Dictionary<string, int>>> _qdsRanks;

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
            this._dArtists = new Dictionary<string, string>();
            foreach (var m in metadata) {
                this._dArtists[m.Document] = m.Artist;
            }
            // fSYS
            this._fSYS = new Dictionary<string, double>();
            this._aSYS = new Dictionary<string, double>();
            this._aART = new Dictionary<string, double>();
            this._sRels = new Dictionary<string, List<double>>();
            this._qaRels = new Dictionary<string, List<double>>();
            foreach (var run in runs) {
                string query = run.Query;
                foreach (string doc in run.Documents) {
                    string id = RelevanceEstimate.GetId(query, doc);
                    // fSYS
                    double fSYS = 0;
                    this._fSYS.TryGetValue(id, out fSYS);
                    this._fSYS[id] = fSYS + 1.0 / nSys;

                    this._aSYS[id] = 0;
                    this._aART[id] = 0;
                    // sRels
                    if (!this._sRels.ContainsKey(run.System)) {
                        this._sRels[run.System] = new List<double>();
                    }
                    // qaRels
                    string artist = null;
                    if (this._dArtists.TryGetValue(doc, out artist) && !this._qaRels.ContainsKey(query + "\t" + artist)) {
                        this._qaRels[query + "\t" + artist] = new List<double>();
                    }
                }
            }
            // OV
            this._OV = ((double)this._fSYS.Count) / (nSys * (runs.Count()/nSys) * runs.First().Documents.Count());

            this._qdsRanks = jurbano.Allcea.Cli.AbstractCommand.ToQueryDocumentSystemRanks(runs);

            // Incorporate known judgments
            foreach (var est in judged) {
                this.Update(est);
            }
            this._needsUpdate = true;
        }

        public RelevanceEstimate Estimate(string query, string doc)
        {
            if (this._needsUpdate) {
                this.DoUpdate();
            }
            string id = RelevanceEstimate.GetId(query, doc);
            double fSYS = 0;
            double aSYS = 0;
            double aART = 0;
            // Do we have features?
            if (this._fSYS.TryGetValue(id, out fSYS) && this._aSYS.TryGetValue(id, out aSYS) && this._aART.TryGetValue(id, out aART)) {
                double[] thetas = new double[] { fSYS, aSYS, aART };
                double[] eval = this._model.Evaluate(thetas);
                return new RelevanceEstimate(query, doc, eval[0], eval[1]);
            }
            // If here, some feature was missing, so return default estimate
            return this._defaultEstimator.Estimate(query, doc);
        }

        public void Update(RelevanceEstimate est)
        {
            // qsRels
            foreach (var sRanks in this._qdsRanks[est.Query][est.Document]) {
                this._sRels[sRanks.Key].Add(est.Expectation);
            }
            // qaRels
            string artist = null;
            if (this._dArtists.TryGetValue(est.Document, out artist)) {
                this._qaRels[est.Query + "\t" + artist].Add(est.Expectation);
            }

            this._needsUpdate = true;

            this._defaultEstimator.Update(est);
        }
        protected void DoUpdate()
        {
            foreach (var dsRanks in this._qdsRanks) {
                string query = dsRanks.Key;
                foreach (var sRanks in dsRanks.Value) {
                    string doc = sRanks.Key;
                    string id = RelevanceEstimate.GetId(query, doc);

                    // aSYS
                    double aSYS = 0;
                    bool hasaSYS = false;
                    foreach (string sys in sRanks.Value.Keys) { // systems that retrieved d for q
                        var rels = this._sRels[sys];
                        if (rels.Count != 0){
                            hasaSYS = true;
                            aSYS += rels.Average();
                        }
                    }
                    if (hasaSYS) { // If we don't have judgments yet related to q and d, don't estimate
                        this._aSYS[id] = aSYS / sRanks.Value.Count;
                    } else {
                        this._aSYS.Remove(id);
                    }
                    // aART
                    string artist = null;
                    if (this._dArtists.TryGetValue(doc, out artist) && artist != "VARIOUS ARTISTS") {
                        var rels = this._qaRels[query + "\t" + artist];
                        if (rels.Count != 0) { // If we don't have judgments yet related to q and d, don't estimate
                            this._aART[id] = rels.Average();
                        } else {
                            this._aART.Remove(id);
                        }
                    } else {
                        this._aART.Remove(id);
                    }
                }
            }

            this._needsUpdate = false;
        }

        public double[] Features(string query, string doc)
        {
            if (this._needsUpdate) {
                this.DoUpdate();
            }
            string id = RelevanceEstimate.GetId(query, doc);

            double fSYS, aSYS, aART;
            if (!this._fSYS.TryGetValue(id, out fSYS)) {
                fSYS = double.NaN;
            }
            if (!this._aSYS.TryGetValue(id, out aSYS)) {
                aSYS = double.NaN;
            }
            if (!this._aART.TryGetValue(id, out aART)) {
                aART = double.NaN;
            }
            return new double[] { fSYS, this._OV, aSYS, aART };
        }
    }
}
