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
    public class MoutEstimator : IEstimator
    {
        protected Dictionary<string, Dictionary<string, double>> _fSYS; // [query, [doc, fSYS]]
        protected double _OV;
        protected Dictionary<string, Dictionary<string, bool?>> _sGEN; // [query, [doc, sGEN]]
        protected Dictionary<string, Dictionary<string, double?>> _fGEN; // [query, [doc, fGEN]]
        protected Dictionary<string, Dictionary<string, double?>> _fART; // [query, [doc, fART]]

        public MoutEstimator(IEnumerable<Run> runs, IEnumerable<Metadata> metadata)
        {
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
            this._sGEN = new Dictionary<string, Dictionary<string, bool?>>();
            this._fGEN = new Dictionary<string, Dictionary<string, double?>>();
            this._fART = new Dictionary<string, Dictionary<string, double?>>();
            foreach (var qfSYS in this._fSYS) {
                string query = qfSYS.Key;
                Dictionary<string, bool?> qsGEN = new Dictionary<string, bool?>();
                Dictionary<string, double?> qfGEN = new Dictionary<string, double?>();
                Dictionary<string, double?> qfART = new Dictionary<string, double?>();
                foreach (var qdfSYS in qfSYS.Value) {
                    string doc = qdfSYS.Key;
                    // sGEN and fGEN
                    if (genres.ContainsKey(doc)) {
                        string docGEN = genres[doc];
                        // sGEN
                        if (genres.ContainsKey(query)) {
                            qsGEN.Add(qdfSYS.Key, docGEN == genres[qfSYS.Key]);
                        } else {
                            qsGEN.Add(doc, null);
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
                        qsGEN.Add(doc, null);
                        qfGEN.Add(doc, null);
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
                        qfART.Add(doc, null);
                    }
                }
                this._sGEN.Add(query, qsGEN);
                this._fGEN.Add(query, qfGEN);
                this._fART.Add(query, qfART);
            }
        }

        public Estimate Estimate(string query, string doc)
        {
            throw new NotImplementedException();
        }
    }
}
