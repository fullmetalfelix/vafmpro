using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;



namespace vafmpro
{
    //trilinear interpolator for forcefield(x,y,z,V)
    public partial class Forcer
    {
		
		private double[] xr = new double[4];
		private int[] xIndex=new int[4];

        private double[, , , ,] TrilinMx; //coeffients for trilin interpolation along X direction in each position x,y,z,v,fc

        private void Trilin_Init()
        {

            TrilinMx = new double[GridSize[0], GridSize[1], GridSize[2], GridSize[3], 3];

            for (int i = 0; i < GridSize[0]-1; i++) //loop on the x EXCEPT THE LAST ONE!
            {

                for (int j = 0; j < GridSize[1]; j++) // loop on Y
                {

                    for (int k = 0; k < GridSize[2]; k++) // loop on Z
                    {
                        for (int v = 0; v < GridSize[3]; v++) // loop on V
                        {
                            for (int c = 0; c < 3; c++)
                                TrilinMx[i, j, k, v, c] = (DataGrid[i + 1, j, k, v, c] - DataGrid[i, j, k, v, c]) / (Grid[0][i + 1] - Grid[0][i]);
                        }

                    }
                }
            }
            //now compute it for the last point depending on PBC type
            int ii = GridSize[0] - 1;

            if (PBC[0] == 0) //pacman on X
            {
                for (int j = 0; j < GridSize[1]; j++) // loop on Y
                    for (int k = 0; k < GridSize[2]; k++) // loop on Z
                        for (int v = 0; v < GridSize[3]; v++) // loop on V
                            for (int c = 0; c < 3; c++)
                                TrilinMx[ii, j, k, v, c] = (DataGrid[0, j, k, v, c] - DataGrid[ii, j, k, v, c]) / 
									(Grid[0][GridSize[0]] - Grid[0][ii]);
            }
			else //mirror on X
			{
				for (int j = 0; j < GridSize[1]; j++) // loop on Y
                    for (int k = 0; k < GridSize[2]; k++) // loop on Z
                        for (int v = 0; v < GridSize[3]; v++) // loop on V
                            for (int c = 0; c < 3; c++)
                                TrilinMx[ii, j, k, v, c] = (DataGrid[ii-1, j, k, v, c] - DataGrid[ii, j, k, v, c]) / 
									(Grid[0][GridSize[0]] - Grid[0][ii]);
			}


        }

		
		//get the forces through trilinear interpolation
		//store the result in Forces (public member)
        public void Trilin_Force(double[] x)
        {
            double intr000, intr010;
            double f000, f001, f,fv;

            int i,j,k,v;
            int jj,kk,vv,jji;
            int c;

            Force[0] = 0; Force[1] = 0; Force[2] = 0; //set it to zero

            //center the position in the unit cell
            CenterCursor(x, ref xr);

            //find the voxel where the point is
            FindVoxel(xr, ref xIndex);

            i=xIndex[0];
            j = xIndex[1]; jj = j + 1; jji = j + 1;
            k = xIndex[2]; kk = k + 1;
            v = xIndex[3]; vv = v + 1;

            if (jj == GridSize[1]) //if we are on the Y edge and PBC pacman...
            {
                if (PBC[1] == 0)
                { 
                    jj = 0;
                }

            } 
			

            //Console.WriteLine("point is between X {0} and {1}", xIndex[0], ffIndex[0]);
            //Console.WriteLine("point is between Y {0} and {1}", xIndex[1], ffIndex[1]);
            #region "Interpolation"
            for (c = 0; c < 3; c++) //for each component
            {
                intr000 = TrilinMx[i, j, k, v, c] * (xr[0] - Grid[0][i]) + DataGrid[i, j, k, v, c]; // interp along base line
                intr010 = TrilinMx[i, jj, k, v, c] * (xr[0] - Grid[0][i]) + DataGrid[i, jj, k, v, c]; // interp along base line
                f000 = ((intr010 - intr000)/(Grid[1][jji]-Grid[1][j])) * (xr[1]-Grid[1][j])+intr000; //inter along Y

                //redo the interp at z+1
                intr000 = TrilinMx[i, j, kk, v, c] * (xr[0] - Grid[0][i]) + DataGrid[i, j, kk, v, c]; // interp along base line
                intr010 = TrilinMx[i, jj, kk, v, c] * (xr[0] - Grid[0][i]) + DataGrid[i, jj, kk, v, c]; // interp along base line
                f001 = ((intr010 - intr000) / (Grid[1][jji] - Grid[1][j])) * (xr[1] - Grid[1][j]) + intr000; //inter along Y

                //interpolate along Z
                f = ((f001 - f000) / (Grid[2][kk] - Grid[2][k])) * (xr[2] - Grid[2][k]) + f000;



                //redo at V+1 if there is V
                if (Dims == 4)
                {
                    intr000 = TrilinMx[i, j, k, vv, c] * (xr[0] - Grid[0][i]) + DataGrid[i, j, k, vv, c]; // interp along base line
                    intr010 = TrilinMx[i, jj, k, vv, c] * (xr[0] - Grid[0][i]) + DataGrid[i, jj, k, vv, c]; // interp along base line
                    f000 = ((intr010 - intr000) / (Grid[1][jji] - Grid[1][j])) * (xr[1] - Grid[1][j]) + intr000; //inter along Y

                    //redo the interp at z+1
                    intr000 = TrilinMx[i, j, kk, vv, c] * (xr[0] - Grid[0][i]) + DataGrid[i, j, kk, vv, c]; // interp along base line
                    intr010 = TrilinMx[i, jj, kk, vv, c] * (xr[0] - Grid[0][i]) + DataGrid[i, jj, kk, vv, c]; // interp along base line
                    f001 = ((intr010 - intr000) / (Grid[1][jji] - Grid[1][j])) * (xr[1] - Grid[1][j]) + intr000; //inter along Y

                    //interpolate along Z
                    fv = ((f001 - f000) / (Grid[2][kk] - Grid[2][k])) * (xr[2] - Grid[2][k]) + f000;
                    //and along V
                    f = ((fv - f) / (Grid[3][vv] - Grid[3][v])) * (xr[3] - Grid[3][v]) + f;

                }
                Force[c] = f;
            }
            #endregion

            //Console.WriteLine("Force is {0} {1} {2}", force[0], force[1], force[2]);

        }
		
		
		
		
        

    }

}

