﻿// Copyright (C) 2014  Julián Urbano <urbano.julian@gmail.com>
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
using jurbano.Allcea.Estimation;
using jurbano.Allcea.Model;

namespace jurbano.Allcea.Cli
{
    public class RelevanceEstimateStore : IRelevanceEstimator
    {
        protected Dictionary<string, Dictionary<string, RelevanceEstimate>> _judged;
        protected Dictionary<string, Dictionary<string, RelevanceEstimate>> _estimated;

        public RelevanceEstimateStore(IEnumerable<RelevanceEstimate> judged, IEnumerable<RelevanceEstimate> estimated)
        {
            // Re-structure known judgments
            this._judged = new Dictionary<string, Dictionary<string, RelevanceEstimate>>();
            foreach (var j in judged) {
                Dictionary<string, RelevanceEstimate> q = null;
                if (!this._judged.TryGetValue(j.Query, out q)) {
                    q = new Dictionary<string, RelevanceEstimate>();
                    this._judged.Add(j.Query, q);
                }
                q.Add(j.Document, j);
            }
            // Re-structure estimated judgments
            this._estimated = new Dictionary<string, Dictionary<string, RelevanceEstimate>>();
            foreach (var e in estimated) {
                Dictionary<string, RelevanceEstimate> q = null;
                if (!this._estimated.TryGetValue(e.Query, out q)) {
                    q = new Dictionary<string, RelevanceEstimate>();
                    this._estimated.Add(e.Query, q);
                }
                q.Add(e.Document, e);
            }
        }

        public RelevanceEstimate Estimate(string query, string doc)
        {
            // Check if it is already judged
            Dictionary<string, RelevanceEstimate> q = null;
            if (this._judged.TryGetValue(query, out q)) {
                RelevanceEstimate e = null;
                if (q.TryGetValue(doc, out e)) {
                    return e;
                }
            }
            // If not, return the estimated judgments
            if (this._estimated.TryGetValue(query, out q)) {
                RelevanceEstimate e = null;
                if (q.TryGetValue(doc, out e)) {
                    return e;
                }
            }
            throw new ArgumentException("No estimate available for document '"+doc+"' to query '"+query+"'.");
        }
    }
}