using System;
using vafmpro.AbsCircuits;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;


namespace vafmpro.Circuits.Forces
{
	// 3D force field linear interpolator
	
	public class i3Dlin : ForceInterpolator3D
	{
			
        private double[,,,] TrilinMx; //coeffients for trilin interpolation along X direction in each position x,y,z,v,fc
		private double TrilinDiv;

		public i3Dlin(string[] words)
		{
			Init(words);
			Console.WriteLine("\n---INITIALIZING 3D FORCE FIELD LINEAR INTERPOLATOR---");
			
			//coordinates for force evaluation
			Input.Add(new Channel("x", null)); //0
			Input.Add(new Channel("y", null)); //1
            Input.Add(new Channel("z", null)); //2
			
			Output.Add(new Channel("error",this)); //0
			

			
		}
		public override void SetUp ()
		{
			string ForceFile = GetInitParameterString(InitWords,"file",true); //get the forcefield
			
			if(Initialize(ForceFile))
				Console.WriteLine("Circuit {0} (3D Field Linear Interpolator) created.", Name);
			else{
				//Console.WriteLine("ERROR! Circuit {0} (3D Field Linear Interpolator) not correctly initlized.", Name);
				throw new Exception(string.Format("ERROR! Circuit {0} (3D Field Linear Interpolator) not correctly initlized.", Name));
			}
			
			InputDefaultInit();
		}
		
		//read the input file...
		public bool Initialize(string ForceFile)
		{
			
			if(!ReadGrid(ForceFile))
				return false;
			if (!ReadForces_GridFormat(ForceFile))
				return false;
			
			Force =  new double[Dim];
			Trilin_Init();
			
			//create output channels, one for each component of the force
			for(int i=0;i<Dim;i++)
				Output.Add(new Channel("F"+i,this));	
			
			/*
			for(double zzz = 0.201; zzz<=0.5; zzz+=0.05)
				DumpField_zfix(zzz); //debug!
			*/
			
			return true;
		}
		
		#region "Read Input file"

        protected override void MakeGrid()
        {

            Grid = new List<double[]>(3); //make the grid

			Grid.Add(new double[GridSize[0] + 1]); //one more grid point if there is pacman geometry
			Grid[0][GridSize[0]] = GridSize[0] * GridStep[0];
			
			Grid.Add(new double[GridSize[1] + 1]);
			Grid[1][GridSize[1]] = GridSize[1] * GridStep[1];
            //----------------------------------------------------
            Grid.Add(new double[GridSize[2]]);


            for (int i = 0; i < 3; i++)
				for (int j = 0; j < GridSize[i]; j++)
					Grid[i][j] = j * GridStep[i];
			
			//now parse them all and multiply to get them in NANOMETERS
			for (int i = 0; i < 3; i++) {
				//GridMin[i] *= GridUnits;
				GridStep[i] *= GridUnits;
				for (int j = 0; j < Grid[i].Length; j++){
					Grid[i][j] *= GridUnits;
					//Console.WriteLine("grid[{0}][{1}] = {2}",i,j,Grid[i][j]);
				}
			}
			
			UnitSize[0] *=GridUnits;
			UnitSize[1] *=GridUnits;
			
			Console.WriteLine("  Grid created.");
        }
		private bool ReadForces_GridFormat(string ForceFile)
        {

            StreamReader reader = new StreamReader(ForceFile);
            string line;
            string[] words;
            char[] delimiterChars = { ' ', '\t' };

			Console.WriteLine("  Reading Forces in grid format...");
			
            if (!StringReader.FindString("<forces>", reader)) {
                Console.WriteLine("ERROR! The forcefield is not there!");
                return false;
            }

            int[] xi = new int[3];
			DataGrid=new double[GridSize[0],GridSize[1],GridSize[2],Dim];

            while ((line = reader.ReadLine()) != null) { //read a line
                words = StringReader.TrimWords(line.Split(delimiterChars));
				
                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0) //skip comment/empty lines
                    continue;
				
                //read the i j k coords from the first 3 fields
                for (int i = 0; i < 3; i++)
					if(!Int32.TryParse(words[i], out xi[i]))	{
						Console.WriteLine("ERROR! Grid xyz indeces have to be integer.");
						return false;
					}
				
				xi[0]--;xi[1]--;xi[2]--; //decrease them by 1 because index starts from 0 in C
				
                for (int i = 0; i < Dim; i++){ //read the forces (Dim components)
					if(!double.TryParse(words[i + 3], out DataGrid[xi[0], xi[1], xi[2], i])){
						Console.WriteLine("ERROR! Forces need to be in number form.");
						return false;
					}
					DataGrid[xi[0], xi[1], xi[2], i] *= ForceUnits; //convert them back to NANONEWTON!
                }

            }
            Console.WriteLine("  Forces values in the grid points loaded.");

            reader.Dispose();
            return true;
        }
		#endregion
		
		/*
		//find the grid voxel where the tip is
        private void FindVoxel()
        {
            int i;
            for (int c = 0; c < 3; c++)
            {
                for (i = 0; i < Grid[c].Length; i++)
                    if (Grid[c][i] > xc[c])
                        break;
                xIndex[c] = i-1;
				if((xIndex[c]<0) || (xIndex[c] > GridSize[c] -1)){
					Console.WriteLine("WTF?! {0} {1} ->  x:{2} {3} {4} !!!",c,xIndex[c],x[0],x[1],x[2]);
					Console.WriteLine("WTF?! {0} {1} ->  x:{2} {3} {4} !!!",c,xIndex[c],xc[0],xc[1],xc[2]);
				}
            }
			
			//Console.WriteLine("VOXEL is {0} {1} {2} ({3} {4} {5})",xIndex[0],xIndex[1],xIndex[2],xc[0],xc[1],xc[2]);
			
        }

		//center the cursor in the unit cell
        private void CenterCursor()
		{
			#region "X direction"
            if (PBC[0] == 0){ //pacman symmetry
				double div = Math.Floor(x[0] / UnitSize[0]);
				//Console.WriteLine("ceiling: {0} = {1}",x[0],div);
				xc[0] = x[0] - div * UnitSize[0];
			}
            else{ //mirror
				xc[0]=x[0];
				if(x[0]<0) xc[0]=-x[0];      //if the point is in the negative side, flip it!
				
				do{
					if(xc[0] >= 2.0*UnitSize[0]){
						xc[0]=xc[0]-(2.0* Grid[0][GridSize[0]]);      //this centers the point in the range [0,2L]
					}
					else{
						if( xc[0] > Grid[0][GridSize[0]] ){
							xc[0]=2.0d*Grid[0][GridSize[0]]-xc[0];
						}
						break;
					}
				}while(true);
				
            }
			#endregion
			
			#region "Y direction"
			if(PBC[1] == 0) //pacman symmetry
			{
				double div = Math.Floor(x[1] / UnitSize[1]);
				xc[1] = x[1] - div*UnitSize[1];
				//Console.WriteLine("ceiling: {0}   {1}   {2} ... gstep {3}",x[1],div,xc[1],Math.Ceiling(x[1]/GridStep[1]));
			}
			else{
				xc[1]=x[1];
				if(x[1]<0) xc[1]=-x[1];      //if the point is in the negative side, flip it!
				
				do{
					if(xc[1] >= 2.0*UnitSize[1]){
						xc[1]=xc[1]-(2.0* Grid[1][GridSize[1]]);      //this centers the point in the range [0,2L]
					}
					else{
						if( xc[1] > Grid[1][GridSize[1]] ){
							xc[1]=2.0d*Grid[1][GridSize[1]]-xc[1];
						}
						break;
					}
				}while(true);
			}
			#endregion
			
			xc[2] = x[2];

			
			Console.WriteLine("point {0} {1} {2} was centered in {3} {4} {5}",
			                  x[0].ToString(),x[1].ToString(),x[2].ToString(),
			                  xc[0].ToString(),xc[1].ToString(),xc[2].ToString());
			
		}
		*/
		
        private void Trilin_Init()
        {
			
			TrilinDiv = GridStep[0]*GridStep[1]*GridStep[2];
			
            TrilinMx = new double[GridSize[0], GridSize[1], GridSize[2], Dim];

            for (int i = 0; i < GridSize[0]-1; i++){ //loop on the x EXCEPT THE LAST ONE!
				for (int j = 0; j < GridSize[1]; j++){ // loop on Y
					for (int k = 0; k < GridSize[2]; k++){ // loop on Z
						for (int c = 0; c < Dim; c++){
							TrilinMx[i, j, k, c] = (DataGrid[i + 1, j, k, c] - DataGrid[i, j, k, c]) / (Grid[0][i + 1] - Grid[0][i]);
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
						for (int c = 0; c < Dim; c++)
							TrilinMx[ii, j, k, c] = (DataGrid[0, j, k, c] - DataGrid[ii, j, k, c]) / (Grid[0][GridSize[0]] - Grid[0][ii]);
            }
			else //mirror on X
			{
				for (int j = 0; j < GridSize[1]; j++) // loop on Y
                    for (int k = 0; k < GridSize[2]; k++) // loop on Z
						for (int c = 0; c < Dim; c++)
							TrilinMx[ii, j, k, c] = (DataGrid[ii-1, j, k, c] - DataGrid[ii, j, k, c]) / (Grid[0][GridSize[0]] - Grid[0][ii]);
			}

        }
		
		protected override void Interpolate()
        {

            int i,j,k;
            int ii,jj,kk,jji,iii;


            //center the position in the unit cell
            CenterCursor();

            //find the voxel where the point is
            FindVoxel();
			
			i = xIndex[0]; ii = i+1; iii = (i+1) % (GridSize[0]);
            j = xIndex[1]; jj = j + 1; jji = (j + 1) % (GridSize[1]);
            k = xIndex[2]; kk = k + 1;
			
			//Console.WriteLine("{0} {1} {2} -> {3} {4} {5}",x[0],x[1],x[2],xc[0],xc[1],xc[2]);
			
			/*
			if (ii == GridSize[0]){ //if we are on the X edge and PBC pacman...
				if (PBC[0] == 0){
					ii = 0;
				}
			} 
            if (jj == GridSize[1]){ //if we are on the Y edge and PBC pacman...
				if (PBC[1] == 0){
					jj = 0;
				}
			}*/
			
			for(int c = 0; c<Dim; c++)
			{
				Force[c] = DataGrid[i, j,k,c] * (Grid[0][ii]-xc[0]) * (Grid[1][jj]-xc[1]) * (Grid[2][kk]-xc[2]);
				
				Force[c]+= DataGrid[iii,j,k,c] * (xc[0]-Grid[0][i] ) * (Grid[1][jj]-xc[1]) * (Grid[2][kk]-xc[2]);
				Force[c]+= DataGrid[i,jji,k,c] * (Grid[0][ii]-xc[0]) * (xc[1]-Grid[1][j] ) * (Grid[2][kk]-xc[2]);
				Force[c]+= DataGrid[i,j,kk,c] * (Grid[0][ii]-xc[0]) * (Grid[1][jj]-xc[1]) * (xc[2]-Grid[2][k] );
				
				Force[c]+= DataGrid[iii,j,kk,c] * (xc[0]-Grid[0][i] ) * (Grid[1][jj]-xc[1]) * (xc[2]-Grid[2][k] );
				Force[c]+= DataGrid[i,jji,kk,c] * (Grid[0][ii]-xc[0]) * (xc[1]-Grid[1][j] ) * (xc[2]-Grid[2][k] );
				Force[c]+= DataGrid[iii,jji,k,c] * (xc[0]-Grid[0][i] ) * (xc[1]-Grid[1][j] ) * (Grid[2][kk]-xc[2]);
				
				Force[c]+= DataGrid[iii,jji,kk,c] * (xc[0]-Grid[0][i] ) * (xc[1]-Grid[1][j] ) * (xc[2]-Grid[2][k] );
				Force[c] /= TrilinDiv;
			}
			
			
			
			
			/*
            #region "Interpolation - old style"
            for (c = 0; c < Dim; c++) //for each component
            {
                intr000 = TrilinMx[i, j, k, c] * (xc[0] - Grid[0][i]) + DataGrid[i, j, k, c]; // interp along base line
                intr010 = TrilinMx[i, jj, k, c] * (xc[0] - Grid[0][i]) + DataGrid[i, jj, k, c]; // interp along base line
                f000 = ((intr010 - intr000)/(Grid[1][jji]-Grid[1][j])) * (xc[1]-Grid[1][j])+intr000; //inter along Y

                //redo the interp at z+1
                intr000 = TrilinMx[i, j, kk, c] * (xc[0] - Grid[0][i]) + DataGrid[i, j, kk, c]; // interp along base line
                intr010 = TrilinMx[i, jj, kk, c] * (xc[0] - Grid[0][i]) + DataGrid[i, jj, kk, c]; // interp along base line
                f001 = ((intr010 - intr000) / (Grid[1][jji] - Grid[1][j])) * (xc[1] - Grid[1][j]) + intr000; //inter along Y

                //interpolate along Z
                f = ((f001 - f000) / (Grid[2][kk] - Grid[2][k])) * (xc[2] - Grid[2][k]) + f000;

                Force[c] = f;
            }
            #endregion
            */
			
			//if(Input[2].Value <= 0.3)
			//	Console.WriteLine("Force is {0} {1} {2}", Force[0], Force[1], Force[2]);

        }
		
		public override void Update(ref double dt)
		{
			
			for(int c=0;c<3;c++)
				x[c] = Input[c].Value;
			
						
			Output[0].Value = Evaluate();
			for(int c=0;c<Dim;c++)
				Output[c+1].Value=Force[c];
			
			//if(Input[2].Value <= 0.2)
			//	Console.WriteLine("iforce: {0}",Force[Dim-1].ToString());
		}
		

	}
}
