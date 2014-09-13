using System;

namespace ConfidencePoolAnalyzer
{
    static class LiveWinProbability
    {
        //TODO: what about overtime, what will the minutesRemaining be like?
        public static double Estimate(int awayScore, int homeScore, double homeSpread, double minutesRemaining)
        {
            const double stdev = 13.45;
            int margin = awayScore - homeScore;
            double minRem = minutesRemaining / 60;
            double revRem = 1 / minRem;

            double result = (1 - NormDist(margin + 0.5, -homeSpread * minRem, stdev / Math.Sqrt(revRem), true))
                + (0.5 * (NormDist(margin + 0.5, -homeSpread * minRem, stdev / Math.Sqrt(revRem), true) 
                - NormDist(margin - 0.5, -homeSpread * minRem, stdev / Math.Sqrt(revRem), true)));

            return result;

            // (1 - NORMDIST( ((B2)+0.5), (-B3*(B4/60)), (13.45/SQRT((60/B4))) ,TRUE)) + (0.5*(NORMDIST( ((B2)+0.5), (-B3*(B4/60)), (13.45/SQRT((60/B4))), TRUE)-NORMDIST(((B2)-0.5),(-B3*(B4/60)),(13.45/SQRT((60/B4))),TRUE)))
        }

        static double NormDist(double x, double mean, double std, bool cumulative)
        {
            if (cumulative) return Phi(x, mean, std);
            double tmp = 1 / ((Math.Sqrt(2 * Math.PI) * std));
            return tmp * Math.Exp(-.5 * Math.Pow((x - mean) / std, 2));
        }

        static double Erf(double z)
        {
            double t = 1.0 / (1.0 + 0.5 * Math.Abs(z));
            // use Horner's method
            double ans = 1 - t * Math.Exp(-z * z - 1.26551223 +
            t * (1.00002368 +
            t * (0.37409196 +
            t * (0.09678418 +
            t * (-0.18628806 +
            t * (0.27886807 +
            t * (-1.13520398 +
            t * (1.48851587 +
            t * (-0.82215223 +
            t * (0.17087277))))))))));
            if (z >= 0) return ans;
            return -ans;
        }

        // cumulative normal distribution
        static double Phi(double z)
        {
            return 0.5 * (1.0 + Erf(z / (Math.Sqrt(2.0))));
        }

        // cumulative normal distribution with mean mu and std deviation sigma
        static double Phi(double z, double mu, double sigma)
        {
            return Phi((z - mu) / sigma);
        }
    }
}
