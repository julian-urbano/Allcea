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

namespace jurbano.Allcea.Estimation
{
    public class OrdinalLogisticRegression
    {
        protected double[] _labels;
        protected double[] _alphas;
        protected double[] _betas;

        public OrdinalLogisticRegression(double[] labels, double[] alphas, double[] betas)
        {
            this._labels = labels;
            this._alphas = alphas;
            this._betas = betas;
        }
        public double[] Evaluate(double[] thetas)
        {
            // Log-odds: log (P(R>=label | Theta)) / (P(R<label | Theta)) =
            double[] logOdds = new double[this._alphas.Length + 1];
            logOdds[0] = 1.0; // P(R>=0 | Theta) = 1 always
            for (int l = 1; l < logOdds.Length; l++) {
                logOdds[l] = this._alphas[l - 1]; // = alpha_label +
                for (int j = 0; j < this._betas.Length; j++) // + \sum_j
                    logOdds[l] += this._betas[j] * thetas[j]; // beta_j * theta_j
                logOdds[l] = Math.Exp(logOdds[l]) / (1.0 + Math.Exp(logOdds[l])); // inverse logit of log-odds = P(R>=label | Theta)
            }
            // Expectation and variance
            double e = 0, var = 0;
            for (int l = 0; l < logOdds.Length; l++) {
                if (l < logOdds.Length - 1) {
                    logOdds[l] = logOdds[l] - logOdds[l + 1];
                }
                e += logOdds[l] * this._labels[l];
                var += logOdds[l] * this._labels[l] * this._labels[l];
            }
            var -= e * e;

            return new double[] { e, var };
        }
    }
}