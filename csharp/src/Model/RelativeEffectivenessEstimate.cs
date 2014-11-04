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

namespace jurbano.Allcea.Model
{
    [global::System.Diagnostics.DebuggerDisplay("Systems:{SystemA}-{SystemB}, Query:{Query}, E={Expectation}, Var={Variance}")]
    public class RelativeEffectivenessEstimate
    {
        public string SystemA { get; protected set; }
        public string SystemB { get; protected set; }
        public string Query { get; protected set; }
        public double Expectation { get; protected set; }
        public double Variance { get; protected set; }

        public RelativeEffectivenessEstimate(string systemA, string systemB, string query, double e, double var)
        {
            this.SystemA = systemA;
            this.SystemB = systemB;
            this.Query = query;
            this.Expectation = e;
            this.Variance = var;
        }
    }
}