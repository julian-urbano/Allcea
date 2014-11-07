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
using jurbano.Allcea.Model;

namespace jurbano.Allcea.Estimation
{
    [global::System.Diagnostics.DebuggerDisplay("MaxRelevance={MaxRelevance}")]
    public class UniformRelevanceEstimator : IRelevanceEstimator
    {
        public int MaxRelevance { get; protected set; }

        public UniformRelevanceEstimator(int maxrelevance)
        {
            if (maxrelevance < 1) {
                throw new ArgumentException("The maximum relevance level cannot be less than 1.");
            }
            this.MaxRelevance = maxrelevance;
        }

        public RelevanceEstimate Estimate(string query, string doc)
        {
            return new RelevanceEstimate(query, doc, this.MaxRelevance / 2.0, (Math.Pow(this.MaxRelevance + 1, 2) - 1.0) / 12.0);
        }

        public void Update(RelevanceEstimate est)
        {
            // Nothing to do
        }
    }
}
