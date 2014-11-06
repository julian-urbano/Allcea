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
using jurbano.Allcea.Evaluation;

namespace jurbano.Allcea.Cli
{
    public abstract class AbstractCommand
    {
        public Options Options { get; protected set; }
        public abstract string OptionsFooter { get; }

        public abstract void CheckOptions(CommandLine cmd);
        public abstract void Run();

        internal static string CheckInputFile(string path)
        {
            return AbstractCommand.CheckFile(path, "Input");
        }
        internal static string CheckJudgedFile(string path)
        {
            return AbstractCommand.CheckFile(path, "Known judgments");
        }
        internal static string CheckEstimatedFile(string path)
        {
            return AbstractCommand.CheckFile(path, "Estimated judgments");
        }
        protected static string CheckFile(string path, string name)
        {
            if (!File.Exists(path)) {
                throw new ArgumentException(name + " file '" + path + "' does not exist.");
            }
            return path;
        }
        internal static int CheckDigits(string digits)
        {
            int digitsInt = Allcea.DEFAULT_DECIMAL_DIGITS;
            if (!Int32.TryParse(digits, out digitsInt) || digitsInt < 0) {
                throw new ArgumentException("'" + digits + "' is not a valid number of fractional digits to output.");
            }
            return digitsInt;
        }
        internal static double CheckConfidence(string conf)
        {
            double confDouble = Allcea.DEFAULT_CONFIDENCE;
            if (!Double.TryParse(conf, out confDouble) || confDouble <= 0 || confDouble >= 1) {
                throw new ArgumentException("'" + conf + "' is not a valid confidence level.");
            }
            return confDouble;
        }
        internal static double CheckRelativeSize(string size)
        {
            double sizeDouble = Allcea.DEFAULT_RELATIVE_SIZE;
            if (!Double.TryParse(size, out sizeDouble) || sizeDouble < 0) {
                throw new ArgumentException("'" + size + "' is not a valid relative effect size.");
            }
            return sizeDouble;
        }
        internal static double CheckAbsoluteSize(string size)
        {
            double sizeDouble = Allcea.DEFAULT_ABSOLUTE_SIZE;
            if (!Double.TryParse(size, out sizeDouble) || sizeDouble <= 0) {
                throw new ArgumentException("'" + size + "' is not a valid absolute effect size.");
            }
            return sizeDouble;
        }
        internal static EvaluationTargets CheckTarget(string target)
        {
            if (target.StartsWith("rel", StringComparison.InvariantCultureIgnoreCase)) {
                return EvaluationTargets.Relative;
            } else if (target.StartsWith("abs", StringComparison.InvariantCultureIgnoreCase)) {
                return EvaluationTargets.Absolute;
            } else {
                throw new ArgumentException("'" + target + "' is not a valid type of estimates to target.");
            }
        }
        internal static int CheckBatchNumber(string num)
        {
            int numInt = Allcea.DEFAULT_NUMBER_OF_BATCHES;
            if (!Int32.TryParse(num, out numInt) || numInt < 1) {
                throw new ArgumentException("'" + num + "' is not a valid number of batches.");
            }
            return numInt;
        }
        internal static int CheckBatchSize(string size){
            int sizeInt = Allcea.DEFAULT_BATCH_SIZE;
            if (!Int32.TryParse(size, out sizeInt) || sizeInt < 1) {
                throw new ArgumentException("'" + size + "' is not a valid number of documents per batch.");
            }
            return sizeInt;
        }

        internal static IEnumerable<Run> ReadInputFile(string file)
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
        internal static IEnumerable<RelevanceEstimate> ReadKnownJudgments(string file)
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
        internal static IEnumerable<RelevanceEstimate> ReadEstimatedJudgments(string file)
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
        internal static IEnumerable<Metadata> ReadMetadata(string file)
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
