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
    public abstract class AbstractCommand
    {
        public Options Options { get; protected set; }
        public abstract string OptionsFooter { get; }

        public abstract void CheckOptions(CommandLine cmd);
        public abstract void Run();

        public static IEnumerable<Run> ReadInputFile(string file)
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
        public static IEnumerable<RelevanceEstimate> ReadKnownJudgments(string file)
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
        public static IEnumerable<RelevanceEstimate> ReadEstimatedJudgments(string file)
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
        public static IEnumerable<Metadata> ReadMetadata(string file)
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
