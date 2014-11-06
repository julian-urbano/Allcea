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
using jurbano.Allcea.Model;

namespace jurbano.Allcea.Evaluation
{
    [global::System.Diagnostics.DebuggerDisplay("Normal, Conf={_confidence}, RelSize={_sizeRel}, AbsSize={_sizeAbs}")]
    public class NormalConfidenceEstimator : IConfidenceEstimator
    {        
        protected double _confidence;
        protected double _sizeRel;
        protected double _sizeAbs;

        public NormalConfidenceEstimator(double confidence, double sizeRel, double sizeAbs)
        {
            this._confidence = confidence;
            this._sizeRel = sizeRel;
            this._sizeAbs = sizeAbs;
        }

        public double[] EstimateInterval(Estimate e)
        {
            double z = NormalConfidenceEstimator.Quantile((1.0 - this._confidence) / 2.0);
            double len = Math.Abs(z * Math.Sqrt(e.Variance));
            return new double[] { e.Expectation - len, e.Expectation + len };
        }
        public double EstimateRelativeConfidence(Estimate e)
        {
            return NormalConfidenceEstimator.CDF((e.Expectation-this._sizeRel) / Math.Sqrt(e.Variance));
        }
        public double EstimateAbsoluteConfidence(Estimate e)
        {
            return 1.0 - 2 * NormalConfidenceEstimator.CDF(-this._sizeAbs / Math.Sqrt(e.Variance));
        }

        protected static double CDF(double x)
        {
            // From http://www.johndcook.com/csharp_phi.html

            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x) / Math.Sqrt(2.0);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return 0.5 * (1.0 + sign * y);
        }
        protected static double Quantile(double p)
        {
            // From http://www.johndcook.com/csharp_phi_inverse.html

            if (p <= 0.0 || p >= 1.0) {
                string msg = String.Format("Invalid input argument: {0}.", p);
                throw new ArgumentOutOfRangeException(msg);
            }

            // See article above for explanation of this section.
            if (p < 0.5) {
                // F^-1(p) = - G^-1(p)
                return -NormalConfidenceEstimator.RationalApproximation(Math.Sqrt(-2.0 * Math.Log(p)));
            } else {
                // F^-1(p) = G^-1(1-p)
                return NormalConfidenceEstimator.RationalApproximation(Math.Sqrt(-2.0 * Math.Log(1.0 - p)));
            }
        }
        protected static double RationalApproximation(double t)
        {
            // Abramowitz and Stegun formula 26.2.23.
            // The absolute value of the error should be less than 4.5 e-4.
            double[] c = { 2.515517, 0.802853, 0.010328 };
            double[] d = { 1.432788, 0.189269, 0.001308 };
            return t - ((c[2] * t + c[1]) * t + c[0]) /
                        (((d[2] * t + d[1]) * t + d[0]) * t + 1.0);
        }
    }
}
