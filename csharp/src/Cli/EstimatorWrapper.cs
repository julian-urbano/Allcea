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
using System.IO;
using net.sf.dotnetcli;

namespace jurbano.Allcea.Cli
{
    public class EstimatorWrapper : IRelevanceEstimator
    {
        protected Dictionary<string, RelevanceEstimate> _judged; // [query-doc, estimate]

        protected IRelevanceEstimator _estimator;
        protected string _name;

        protected string _metadataPath;

        public EstimatorWrapper(string name, Dictionary<string, string> parameters)
        {
            this._judged = new Dictionary<string, RelevanceEstimate>();

            this._name = name;
            this._estimator = null;
            switch (this._name) {
                case "uniform":
                    if (parameters.Count != 0) {
                        throw new ParseException("Estimator 'uniform' does not have parameters.");
                    }
                    break;
                case "mout":
                    if (parameters.Count != 1 || !parameters.ContainsKey("meta")) {
                        throw new ParseException("Invalid parameters for estimator 'mout'.");
                    }
                    this._metadataPath = parameters["meta"];
                    if (!File.Exists(this._metadataPath)) {
                        throw new ArgumentException("Metadata file '" + this._metadataPath + "' does not exist.");
                    }
                    break;
                case "mjud":
                    if (parameters.Count != 1 || !parameters.ContainsKey("meta")) {
                        throw new ParseException("Invalid parameters for estimator 'mjud'.");
                    }
                    this._metadataPath = parameters["meta"];
                    if (!File.Exists(this._metadataPath)) {
                        throw new ArgumentException("Metadata file '" + this._metadataPath + "' does not exist.");
                    }
                    break;
                default:
                    throw new ParseException("'" + name + "' is not a valid estimator name.");
            }
        }

        public void Initialize(IEnumerable<Run> runs, IEnumerable<RelevanceEstimate> judged)
        {
            // Re-structure known judgments
            foreach (var j in judged) {
                string id = RelevanceEstimate.GetId(j.Query, j.Document);
                this._judged[id] = j;
            }
            // Instantiate estimator
            switch (this._name) {
                case "uniform":
                    // nothing to initialize
                    this._estimator = new UniformRelevanceEstimator(100);
                    break;
                case "mout":
                    // read metadata
                    IEnumerable<Metadata> metadata = AbstractCommand.ReadMetadata(this._metadataPath);
                    this._estimator = new MoutRelevanceEstimator(runs, metadata);
                    break;
                case "mjud":
                    // read metadata
                    metadata = AbstractCommand.ReadMetadata(this._metadataPath);
                    this._estimator = new MjudRelevanceEstimator(runs, metadata, judged);
                    break;
            }
        }

        public RelevanceEstimate Estimate(string query, string doc)
        {
            // Check if it is already judged
            string id = RelevanceEstimate.GetId(query, doc);
            RelevanceEstimate e = null;
            if (this._judged.TryGetValue(id, out e)) {
                return e;
            }
            // if not, estimate
            return this._estimator.Estimate(query, doc);
        }

        public void Update(RelevanceEstimate estimate)
        {
            // Add to list of judged
            string id = RelevanceEstimate.GetId(estimate.Query, estimate.Document);
            this._judged[id] = estimate;
            // and update wrapped estimator as well
            this._estimator.Update(estimate);
        }
    }
}
