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

namespace jurbano.Allcea.Model
{
    [global::System.Diagnostics.DebuggerDisplay("Query:{Query}, Doc:{Document}, E={Expectation}, Var={Variance}, Weight={Weight}")]
    public class RelevanceEstimate : Estimate
    {
        public string Query { get; protected set; }
        public string Document { get; protected set; }
        public double Weight { get; set; }

        public RelevanceEstimate(string query, string doc, double e, double var)
            : base(e, var)
        {
            this.Query = query;
            this.Document = doc;
            this.Weight = 0;
        }

        public static string GetId(string query, string doc)
        {
            return query + "\t" + doc;
        }
        public static string GetQuery(string id)
        {
            return id.Split('\t')[0];
        }
        public static string GetDocument(string id)
        {
            return id.Split('\t')[1];
        }
    }
}
