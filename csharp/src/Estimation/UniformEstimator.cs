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

namespace jurbano.Allcea.Estimation
{
    [global::System.Diagnostics.DebuggerDisplay("MaxRelevance={MaxRelevance}")]
    public class UniformEstimator : IEstimator
    {
        public int MaxRelevance { get; protected set; }

        public UniformEstimator(int maxrelevance)
        {
            if (maxrelevance < 1) {
                throw new ArgumentException("The maximum relevance level cannot be less than 1.");
            }
            this.MaxRelevance = maxrelevance;
        }

        public Estimate Estimate(string query, string doc)
        {
            return new Estimate(query, doc, this.MaxRelevance / 2.0, (Math.Pow(this.MaxRelevance + 1, 2) - 1.0) / 12.0);
        }
    }
}
