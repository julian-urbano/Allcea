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
using jurbano.Allcea.Estimation;
using jurbano.Allcea.Model;

namespace jurbano.Allcea.Cli
{
    public class RelevanceEstimateStore : IRelevanceEstimator
    {
        protected Dictionary<string, RelevanceEstimate> _estimates; // [querydoc, estimate]

        public RelevanceEstimateStore(IEnumerable<RelevanceEstimate> estimates)
        {
            this._estimates = new Dictionary<string, RelevanceEstimate>();
            this.Update(estimates);
        }

        public RelevanceEstimate Estimate(string query, string doc)
        {
            string id = RelevanceEstimate.GetId(query, doc);
            RelevanceEstimate e = null;
            if (this._estimates.TryGetValue(id, out e)) {
                return e;
            }
            throw new ArgumentException("No estimate available for document '" + doc + "' to query '" + query + "'.");
        }
        public void Update(RelevanceEstimate estimate)
        {
            string id = RelevanceEstimate.GetId(estimate.Query, estimate.Document);
            this._estimates[id] = estimate;
        }
        public void Update(IEnumerable<RelevanceEstimate> estimates)
        {
            foreach (var estimate in estimates) {
                this.Update(estimate);
            }
        }
    }
}
