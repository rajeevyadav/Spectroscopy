﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
/* 
    * MINPACK-1 Least Squares Fitting Library
    *
    * Original public domain version by B. Garbow, K. Hillstrom, J. More'
    *   (Argonne National Laboratory, MINPACK project, March 1980)
    * 
    * Translation to C Language by S. Moshier (moshier.net)
    * Translation to C# Language by D. Cuccia (http://davidcuccia.wordpress.com)
    * 
    * Enhancements and packaging by C. Markwardt
    *   (comparable to IDL fitting routine MPFIT
    *    see http://cow.physics.wisc.edu/~craigm/idl/idl.html)
    */

/* Test routines for MPFit library
    $Id: TestMPFit.cs,v 1.1 2010/05/04 dcuccia Exp $
*/
namespace Spectroscopy_Viewer
{ 
    public static class ForwardModels
    {
        /* 
            * linear fit function
            *
            * m - number of data points
            * n - number of parameters (2)
            * p - array of fit parameters 
            * dy - array of residuals to be returned
            * CustomUserVariable - private data (struct vars_struct *)
            *
            * RETURNS: error code (0 = success)
            */
        public static int LinFunc(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            int i;
            double[] x, y, ey;
            double f;

            CustomUserVariable v = (CustomUserVariable)vars;

            x = v.X;
            y = v.Y;
            ey = v.Ey;

            for (i = 0; i < dy.Length; i++)
            {
                f = p[0] + p[1] * x[i];     /* Linear fit function */
                dy[i] = (y[i] - f) / ey[i];
            }

            return 0;
        }

        /* 
        * quadratic fit function
        *
        * m - number of data points
        * n - number of parameters (2)
        * p - array of fit parameters 
        * dy - array of residuals to be returned
        * CustomUserVariable - private data (struct vars_struct *)
        *
        * RETURNS: error code (0 = success)
        */
        public static int QuadFunc(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            int i;
            double[] x, y, ey;

            CustomUserVariable v = (CustomUserVariable)vars;
            x = v.X;
            y = v.Y;
            ey = v.Ey;

            /* Console.Write ("QuadFunc %f %f %f\n", p[0], p[1], p[2]); */

            for (i = 0; i < dy.Length; i++)
            {
                dy[i] = (y[i] - p[0] - p[1] * x[i] - p[2] * x[i] * x[i]) / ey[i];
            }

            return 0;
        }


        /* 
            * gaussian fit function
            *
            * m - number of data points
            * n - number of parameters (4)
            * p - array of fit parameters 
            * dy - array of residuals to be returned
            * CustomUserVariable - private data (struct vars_struct *)
            *
            * RETURNS: error code (0 = success)
            */
        public static int GaussFunc(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            int i;
            CustomUserVariable v = (CustomUserVariable)vars;
            double[] x, y, ey;
            double xc, sig2;

            x = v.X;
            y = v.Y;
            ey = v.Ey;

            sig2 = p[3] * p[3];

            for (i = 0; i < dy.Length; i++)
            {
                xc = x[i] - p[2];
                dy[i] = (y[i] - p[1] * Math.Exp(-0.5 * xc * xc / sig2) - p[0]) / ey[i];
            }

            return 0;
        }
        
        public static double factEval(double val)
        {
            //Console.Write("Value:" + val);
            int myval = Convert.ToInt32(val);
            double result = 1.0;
            while (myval > 1.0)
            {
                //Console.Write("IN while loop");
                result = result * myval;
                myval -= 1;
            }
            return result;
        }

        public static double[] movingAverage(double[] vals)
        {
            double[] retVal = new double[vals.Length];
            retVal[0] = vals[0];
            for (int i = 1; i < vals.Length; i++)
            {
                retVal[i] = (vals[i] + vals[i - 1]) * 0.5;
            }
            return retVal;
        }
    
        /* 
           * double poisson fit function
           *
           * m - number of data points
           * n - number of parameters (2)
           * p - array of fit parameters 
           * dy - array of residuals to be returned
           * CustomUserVariable - private data (struct vars_struct *)
           *
           * RETURNS: error code (0 = success)
           */
        public static int DoublePossFunc(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            int i;
            CustomUserVariable v = (CustomUserVariable)vars;
            double[] x, y, ey;

            double f;

            x = v.X;
            y = v.Y;
            ey = v.Ey;

            //First I want the tallest point and then 

            for (i = 0; i < dy.Length; i++)
            {
                //Value from first poisson Note that the fact will only work for integers

                double lam1 = p[0];
                double y1 = Math.Pow(lam1, x[i])*Math.Exp(-1.0 * lam1)/(factEval(x[i]));

                double lam2 = p[1];
                double y2 = Math.Pow(lam2, x[i]) * Math.Exp(-1.0 * lam2) / (factEval(x[i]));

                //Value from second poisson
                f = y1 + y2 + p[2];
                dy[i] = (y[i] - f) / ey[i];
            }

            return 0;
        }

        public static int GaussianFuncAndDerivs(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            CustomUserVariable v = (CustomUserVariable)vars;
            double[] x, y, ey;
            double sig2;

            x = v.X;
            y = v.Y;
            ey = v.Ey;

            sig2 = p[3] * p[3];
            //var dvec1 = new double[dy.Length];
            //var residuals = new double[dy.Length];

            for (int i = 0; i < dy.Length; i++)
            {
                double xc = x[i] - p[2];
                double exp = Math.Exp(-0.5 * xc * xc / sig2);
                //residuals[i] = (y[i] - p[1] * exp - p[0]) / ey[i];
                dy[i] = (y[i] - p[1] * exp - p[0]) / ey[i];

                // NOTE: it would make sense to store the first 2 derivatives in vars since they don't change.
                if (dvec != null)
                {
                    if (dvec[0] != null)
                    {
                        dvec[0][i] = -1.0 / ey[i];
                    }
                    if (dvec[1] != null)
                    {
                        //dvec1[i] = -exp / ey[i];
                        dvec[1][i] = -exp / ey[i];
                    }
                    if (dvec[2] != null)
                    {
                        dvec[2][i] = -p[1] * xc * exp / (ey[i] * sig2);
                    }
                    if (dvec[3] != null)
                    {
                        dvec[3][i] = -p[1] * xc * xc * exp / (ey[i] * p[3] * sig2);
                    }
                }
            }

            // Array assignment rather than element-wise causes failure.
            //dy = residuals;

            // Array mismatch exception
            //if (dvec != null)
            //{
            //  if (dvec[1] != null)
            //  {
            //      dvec[1] = dvec1;
            //  }
            //}
            return 0;
        }
    }
}

