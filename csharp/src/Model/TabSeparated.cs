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

namespace jurbano.Allcea.Model
{
    public class TabSeparated : IReader<Run>, IWriter<Estimate>
    {
        public IEnumerable<Run> Read(TextReader tr)
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

        public void Write(TextWriter tw, IEnumerable<Estimate> estimates)
        {
            foreach (var e in estimates.OrderBy(e => e.Query).ThenBy(e=>e.Document)) {
                tw.WriteLine(string.Join("\t",
                    e.Query,
                    e.Document,
                    e.Expectation.ToString("0.####", CultureInfo.InvariantCulture),
                    e.Variance.ToString("0.####", CultureInfo.InvariantCulture)));
            }
        }
    }
}