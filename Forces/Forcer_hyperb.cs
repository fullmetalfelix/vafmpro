using System;
using System.Collections.Generic;
////using System.Linq;
using System.Text;
using System.IO;
//using DotNumerics;
//using DotNumerics.LinearAlgebra;

/*
namespace vafmpro
{
    //multihyperboloidal interpolator for grid refinement
    public partial class Forcer
    {

        private Matrix[] C;
        private List<double[]> PointList;

        private void HyperInterpolate()
        {
            PointList = new List<double[]>(); // X Y Z Fx Fy Fz


            //build a linear array of all the data points
            #region "point list"
            for (int i = 0; i < FCurvesMap.Count; i++) //loop on the map
            {
                int fcIdx = Convert.ToInt32(FCurvesMap[i][2]) - 1;
                for (int j = 0; j < FCurves[fcIdx].Count; j++) //loop on the specific forcecurve points
                {

                    PointList.Add(new double[6]);
                    PointList[PointList.Count - 1][0] = FCurvesMap[i][0]; // X
                    PointList[PointList.Count - 1][1] = FCurvesMap[i][1]; // Y
                    PointList[PointList.Count - 1][2] = FCurves[fcIdx][j].Z; // Z
                    PointList[PointList.Count - 1][3] = FCurves[fcIdx][j].Forces[0];
                    PointList[PointList.Count - 1][4] = FCurves[fcIdx][j].Forces[1];
                    PointList[PointList.Count - 1][5] = FCurves[fcIdx][j].Forces[2];

                }


            }
            #endregion

            //create the matrix
            Matrix A = new Matrix(PointList.Count, PointList.Count);
            double norms;

            for (int i = 0; i < PointList.Count; i++)
                for (int j = 0; j < PointList.Count; j++)
                {
                    norms = 0;
                    for (int k = 0; k < 3; k++)
                        norms += (PointList[i][k] - PointList[j][k]) * (PointList[i][k] - PointList[j][k]);

                    A[i, j] = Math.Sqrt(norms + InterpParam);

                }

            Matrix B = new Matrix(PointList.Count, 1);
            C = new Matrix[3]; // solution vectors for each component
            LinearEquations leq = new LinearEquations();

            for (int k = 3; k < 6; k++) // solve for each component
            {
                for (int i = 0; i < PointList.Count; i++)
                    B[i, 0] = PointList[i][k];

                C[k - 3] = leq.Solve(A, B);
            }



        }





    }

}


*/