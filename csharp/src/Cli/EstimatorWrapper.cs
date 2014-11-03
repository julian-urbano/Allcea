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
using System.IO;
using net.sf.dotnetcli;

namespace jurbano.Allcea.Cli
{
    public class EstimatorWrapper : IEstimator
    {
        protected Dictionary<string, Dictionary<string, Estimate>> _judged; // [query, [doc, estimate]]

        protected IEstimator _estimator;
        protected string _name;

        protected string _metadataPath;

        public EstimatorWrapper(string name, Dictionary<string, string> parameters)
        {
            this._judged = new Dictionary<string, Dictionary<string, Estimate>>();

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
                default:
                    throw new ParseException("'" + name + "' is not a valid estimator name.");
            }
        }

        public void Initialize(IEnumerable<Run> runs, IEnumerable<Estimate> judged)
        {
            // Re-structure known judgments
            foreach (var j in judged) {
                Dictionary<string, Estimate> q = null;
                if (!this._judged.TryGetValue(j.Query, out q)) {
                    q = new Dictionary<string, Estimate>();
                    this._judged.Add(j.Query, q);
                }
                q.Add(j.Document, j);
            }
            // Instantiate estimator
            switch (this._name) {
                case "uniform":
                    // nothing to initialize
                    this._estimator = new UniformEstimator(100);
                    break;
                case "mout":
                    // read metadata
                    IEnumerable<Metadata> metadata;
                    try {
                        IReader<Metadata> reader = new TabSeparated();
                        using (StreamReader sr = new StreamReader(File.OpenRead(this._metadataPath))) {
                            metadata = reader.Read(sr);
                        }
                    } catch (Exception ex) {
                        throw new FormatException("Error reading metadata file: " + ex.Message, ex);
                    }
                    this._estimator = new MoutEstimator(runs, metadata);
                    break;
            }
        }

        public Estimate Estimate(string query, string doc)
        {
            // Check if it is already judged
            Dictionary<string, Estimate> q = null;
            if (this._judged.TryGetValue(query, out q)) {
                Estimate e = null;
                if (q.TryGetValue(doc, out e)) {
                    return e;
                }
            }
            // if not, estimate
            return this._estimator.Estimate(query, doc);
        }
    }
}