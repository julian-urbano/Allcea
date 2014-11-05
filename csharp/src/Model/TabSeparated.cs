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
using System.IO;
using System.Globalization;
using jurbano.Allcea.Evaluation;

namespace jurbano.Allcea.Model
{
    public class TabSeparated : IReader<Run>,
        IWriter<RelevanceEstimate>, IReader<RelevanceEstimate>,
        IReader<Metadata>,
        IWriter<RelativeEffectivenessEstimate>, IWriter<AbsoluteEffectivenessEstimate>,
        IReadHelper
    {
        protected string _doubleFormat;

        public TabSeparated(int decimalDigits)
        {
            this._doubleFormat = "0.";
            for (int i = 0; i < decimalDigits; i++) {
                this._doubleFormat += "#";
            }
        }
        public TabSeparated() : this(Allcea.DEFAULT_DECIMAL_DIGITS) { }

        IEnumerable<Run> IReader<Run>.Read(TextReader tr)
        {
            List<Run> runs = new List<Run>();

            string prevSystem = null;
            string prevQuery = null;
            List<string> prevDocs = new List<string>();

            int lineNumber = 1;
            string line = tr.ReadLine();
            while (line != null) {
                string[] parts = line.Split('\t'); // system query doc
                if (parts.Length != 3) {
                    throw new FormatException("line " + lineNumber + " is not well-formatted.");
                }
                string system = parts[0];
                string query = parts[1];
                string doc = parts[2];

                // If the query changes, add the previous run and start a new one
                if (prevQuery == null || prevQuery != query) {
                    // Add the previous one only if it's not null (first query)
                    if (prevQuery != null) {
                        runs.Add(new Run(prevSystem, prevQuery, prevDocs));
                    }
                    prevSystem = system;
                    prevQuery = query;
                    prevDocs.Clear();
                }
                prevDocs.Add(doc);

                line = tr.ReadLine();
                lineNumber++;
            }
            // In case no second run was read
            if (prevQuery != null) {
                runs.Add(new Run(prevSystem, prevQuery, prevDocs));
            }

            return runs;
        }

        void IWriter<RelevanceEstimate>.Write(TextWriter tw, IEnumerable<RelevanceEstimate> estimates)
        {
            foreach (var e in estimates.OrderBy(e => e.Query).ThenBy(e => e.Document)) {
                tw.WriteLine(string.Join("\t",
                    e.Query,
                    e.Document,
                    e.Expectation.ToString(this._doubleFormat, CultureInfo.InvariantCulture),
                    e.Variance.ToString(this._doubleFormat, CultureInfo.InvariantCulture)));
            }
        }
        IEnumerable<RelevanceEstimate> IReader<RelevanceEstimate>.Read(TextReader tr)
        {
            List<RelevanceEstimate> estimates = new List<RelevanceEstimate>();

            int lineNumber = 1;
            string line = tr.ReadLine();
            while (line != null) {
                string[] parts = line.Split('\t'); // query doc E Var

                if (parts.Length < 3 || parts.Length > 4) {
                    throw new FormatException("line " + lineNumber + " is not well-formatted.");
                }
                string query = parts[0];
                string doc = parts[1];
                double expectation = 0;
                if (!double.TryParse(parts[2], out expectation)) {
                    throw new FormatException("line " + lineNumber + " is not well-formatted.");
                }
                double variance = 0;
                if (parts.Length == 4) { // We have Var too
                    if (!double.TryParse(parts[3], out variance)) {
                        throw new FormatException("line " + lineNumber + " is not well-formatted.");
                    }
                }

                estimates.Add(new RelevanceEstimate(query, doc, expectation, variance));

                line = tr.ReadLine();
                lineNumber++;
            }

            return estimates;
        }

        void IWriter<AbsoluteEffectivenessEstimate>.Write(TextWriter tw, IEnumerable<AbsoluteEffectivenessEstimate> estimates)
        {
            IConfidenceEstimator confidence = new NormalConfidenceEstimator();
            List<double> confidences = new List<double>();
            tw.WriteLine(string.Join("\t", "Sys", "E", "Var", "[E", "E]", "Conf"));
            foreach (var estimate in estimates) {
                double conf = confidence.EstimateAbsoluteConfidence(estimate.Expectation, estimate.Variance, .01);
                confidences.Add(conf);
                double[] interval = confidence.EstimateInterval(estimate.Expectation, estimate.Variance, .95);

                tw.WriteLine(string.Join("\t", estimate.System,
                        estimate.Expectation.ToString(this._doubleFormat, CultureInfo.InvariantCulture), estimate.Variance.ToString(this._doubleFormat, CultureInfo.InvariantCulture),
                        interval[0].ToString(this._doubleFormat, CultureInfo.InvariantCulture), interval[1].ToString(this._doubleFormat, CultureInfo.InvariantCulture),
                        conf.ToString(this._doubleFormat, CultureInfo.InvariantCulture)));
            }
            tw.WriteLine();
            tw.WriteLine("Average Confidence: " + confidences.Average().ToString(this._doubleFormat, CultureInfo.InvariantCulture));
        }
        
        void IWriter<RelativeEffectivenessEstimate>.Write(TextWriter tw, IEnumerable<RelativeEffectivenessEstimate> estimates)
        {
            IConfidenceEstimator confidence = new NormalConfidenceEstimator();
            List<double> confidences = new List<double>();
            tw.WriteLine(string.Join("\t", "SysA", "SysB", "E", "Var", "[E", "E]", "Conf"));
            foreach (var estimate in estimates) {
                double conf = confidence.EstimateRelativeConfidence(estimate.Expectation, estimate.Variance, 0);
                confidences.Add(conf);
                double[] interval = confidence.EstimateInterval(estimate.Expectation, estimate.Variance, .95);

                tw.WriteLine(string.Join("\t", estimate.SystemA, estimate.SystemB,
                        estimate.Expectation.ToString(this._doubleFormat, CultureInfo.InvariantCulture), estimate.Variance.ToString(this._doubleFormat, CultureInfo.InvariantCulture),
                        interval[0].ToString(this._doubleFormat, CultureInfo.InvariantCulture), interval[1].ToString(this._doubleFormat, CultureInfo.InvariantCulture),
                        conf.ToString(this._doubleFormat, CultureInfo.InvariantCulture)));
            }
            tw.WriteLine();
            tw.WriteLine("Average Confidence: " + confidences.Average().ToString(this._doubleFormat, CultureInfo.InvariantCulture));
        }

        IEnumerable<Metadata> IReader<Metadata>.Read(TextReader tr)
        {
            List<Metadata> metadata = new List<Metadata>();

            int lineNumber = 1;
            string line = tr.ReadLine();
            while (line != null) {
                string[] parts = line.Split('\t'); // doc artist genre
                if (parts.Length != 3) {
                    throw new FormatException("line " + lineNumber + " is not well-formatted.");
                }
                string doc = parts[0];
                string artist = parts[1];
                string genre = parts[2];

                metadata.Add(new Metadata(doc, artist, genre));

                line = tr.ReadLine();
                lineNumber++;
            }

            return metadata;
        }
        
        public IEnumerable<Run> ReadInputFile(string file)
        {
            IEnumerable<Run> runs = null;
            try {
                IReader<Run> runReader = new TabSeparated();
                using (StreamReader sr = new StreamReader(File.OpenRead(file))) {
                    runs = runReader.Read(sr);
                }
            } catch (Exception ex) {
                throw new FormatException("Error reading input file: " + ex.Message, ex);
            }
            return runs;
        }
        public IEnumerable<RelevanceEstimate> ReadKnownJudgments(string file)
        {
            IEnumerable<RelevanceEstimate> judged = null;
            try {
                IReader<RelevanceEstimate> runReader = new TabSeparated();
                using (StreamReader sr = new StreamReader(File.OpenRead(file))) {
                    judged = runReader.Read(sr);
                }
            } catch (Exception ex) {
                throw new FormatException("Error reading known judgments file: " + ex.Message, ex);
            }
            return judged;
        }
        public IEnumerable<RelevanceEstimate> ReadEstimatedJudgments(string file)
        {
            IEnumerable<RelevanceEstimate> estimates = null;
            try {
                IReader<RelevanceEstimate> runReader = new TabSeparated();
                using (StreamReader sr = new StreamReader(File.OpenRead(file))) {
                    estimates = runReader.Read(sr);
                }
            } catch (Exception ex) {
                throw new FormatException("Error reading estimated judgments file: " + ex.Message, ex);
            }
            return estimates;
        }
        public IEnumerable<Metadata> ReadMetadata(string file)
        {
            IEnumerable<Metadata> metadata;
            try {
                IReader<Metadata> reader = new TabSeparated();
                using (StreamReader sr = new StreamReader(File.OpenRead(file))) {
                    metadata = reader.Read(sr);
                }
            } catch (Exception ex) {
                throw new FormatException("Error reading metadata file: " + ex.Message, ex);
            }
            return metadata;
        }

    }
}