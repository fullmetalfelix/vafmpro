using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;


namespace vafmpro.Circuits.Forces
{
	
	public class i3DspXYlZ : ForceInterpolator3D
	{
		/* ---------------------------------------
		 * This circuit interpolates F(x,y,z) by 2D bicubic interpolation
		 * along x and y, and then does linear interpolation along z.
		 * The bicubic coefficients for the z planes are precalculated.
		 */ 
		
		private double[,,,,,] BiCubs;
		private double[,] Square = new double[4,4];
		private int[] xidx = new int[4];
		private int[] yidx = new int[4];
		
		private double x1,x2,x3,y1,y2,y3;
		private double[] f0, f1;
		
		
		public i3DspXYlZ(string[] words)
		{
			Init(words);
			Console.WriteLine("\n---INITIALIZING 3D FORCE FIELD BICUBIC(XY)LINEAR(Z) INTERPOLATOR---");
			
			//coordinates for force evaluation
			Input.Add(new Channel("x", null)); //0
			Input.Add(new Channel("y", null)); //1
            Input.Add(new Channel("z", null)); //2
			
			Output.Add(new Channel("error",this)); //0
			
			string ForceFile = GetInitParameterString(words,"file",true); //get the forcefield
			
			
			if(Initialize(ForceFile))
				Console.WriteLine("Circuit {0} (3D Field Bicubic(XY) Linear(Z) Interpolator) created.", Name);
			else
				Console.WriteLine("ERROR! Circuit {0} (3D Field Bicubic(XY) Linear(Z) Interpolator) not correctly initlized.", Name);
		}
		
		public override void SetUp ()
		{
			InputDefaultInit();
		}
		
		private bool Initialize(string ForceFile)
		{
			
			if(!ReadGrid(ForceFile))
				return false;
			if (!ReadForces_GridFormat(ForceFile))
				return false;
			
			Force =  new double[3];
			Bicubic_Init();
			
			f0 = new double[Dim];
			f1 = new double[Dim];
			
			//create output channels, one for each component of the force
			for(int i=0;i<Dim;i++)
				Output.Add(new Channel("F"+i,this));	
			
			//DumpField_zfix(0.55); //debug!
			
			return true;
		}
		
		private void Bicubic_Init()
		{
			
			//precalculate the coefficients for bicubic interpolation
			BiCubs = new double[GridSize[0],GridSize[1],GridSize[2],Dim,4,4];

			
			for(int k=0;k<GridSize[2];k++)
				for(int i=0;i<GridSize[0];i++)
					for(int j=0;j<GridSize[1];j++){
					
					//build the index lists
					FetchIndex(i, j);
					
					for(int c=0;c<Dim;c++){
						FetchSquare(k, c);
						#region "Bicubs evaluation"
						BiCubs[i,j,k,c,0,0] = Square[1,1];
						BiCubs[i,j,k,c,0,1] = -.5*Square[1,0] + .5*Square[1,2];
						BiCubs[i,j,k,c,0,2] = Square[1,0] - 2.5*Square[1,1] + 2*Square[1,2] - .5*Square[1,3];
						BiCubs[i,j,k,c,0,3] = -.5*Square[1,0] + 1.5*Square[1,1] - 1.5*Square[1,2] + .5*Square[1,3];
						BiCubs[i,j,k,c,1,0] = -.5*Square[0,1] + .5*Square[2,1];
						BiCubs[i,j,k,c,1,1] = .25*Square[0,0] - .25*Square[0,2] - .25*Square[2,0] + .25*Square[2,2];
						BiCubs[i,j,k,c,1,2] = -.5*Square[0,0] + 1.25*Square[0,1] - Square[0,2] + .25*Square[0,3] + .5*Square[2,0] - 1.25*Square[2,1] + Square[2,2] - .25*Square[2,3];
						BiCubs[i,j,k,c,1,3] = .25*Square[0,0] - .75*Square[0,1] + .75*Square[0,2] - .25*Square[0,3] - .25*Square[2,0] + .75*Square[2,1] - .75*Square[2,2] + .25*Square[2,3];
						BiCubs[i,j,k,c,2,0] = Square[0,1] - 2.5*Square[1,1] + 2*Square[2,1] - .5*Square[3,1];
						BiCubs[i,j,k,c,2,1] = -.5*Square[0,0] + .5*Square[0,2] + 1.25*Square[1,0] - 1.25*Square[1,2] - Square[2,0] + Square[2,2] + .25*Square[3,0] - .25*Square[3,2];
						BiCubs[i,j,k,c,2,2] = Square[0,0] - 2.5*Square[0,1] + 2*Square[0,2] - .5*Square[0,3] - 2.5*Square[1,0] + 6.25*Square[1,1] - 5*Square[1,2] + 1.25*Square[1,3] + 2*Square[2,0] - 5*Square[2,1] + 4*Square[2,2] - Square[2,3] - .5*Square[3,0] + 1.25*Square[3,1] - Square[3,2] + .25*Square[3,3];
						BiCubs[i,j,k,c,2,3] = -.5*Square[0,0] + 1.5*Square[0,1] - 1.5*Square[0,2] + .5*Square[0,3] + 1.25*Square[1,0] - 3.75*Square[1,1] + 3.75*Square[1,2] - 1.25*Square[1,3] - Square[2,0] + 3*Square[2,1] - 3*Square[2,2] + Square[2,3] + .25*Square[3,0] - .75*Square[3,1] + .75*Square[3,2] - .25*Square[3,3];
						BiCubs[i,j,k,c,3,0] = -.5*Square[0,1] + 1.5*Square[1,1] - 1.5*Square[2,1] + .5*Square[3,1];
						BiCubs[i,j,k,c,3,1] = .25*Square[0,0] - .25*Square[0,2] - .75*Square[1,0] + .75*Square[1,2] + .75*Square[2,0] - .75*Square[2,2] - .25*Square[3,0] + .25*Square[3,2];
						BiCubs[i,j,k,c,3,2] = -.5*Square[0,0] + 1.25*Square[0,1] - Square[0,2] + .25*Square[0,3] + 1.5*Square[1,0] - 3.75*Square[1,1] + 3*Square[1,2] - .75*Square[1,3] - 1.5*Square[2,0] + 3.75*Square[2,1] - 3*Square[2,2] + .75*Square[2,3] + .5*Square[3,0] - 1.25*Square[3,1] + Square[3,2] - .25*Square[3,3];
						BiCubs[i,j,k,c,3,3] = .25*Square[0,0] - .75*Square[0,1] + .75*Square[0,2] - .25*Square[0,3] - .75*Square[1,0] + 2.25*Square[1,1] - 2.25*Square[1,2] + .75*Square[1,3] + .75*Square[2,0] - 2.25*Square[2,1] + 2.25*Square[2,2] - .75*Square[2,3] - .25*Square[3,0] + .75*Square[3,1] - .75*Square[3,2] + .25*Square[3,3];
						#endregion
					}
					
					
				}
			
		}
		
		
		#region "Read Input file"
        protected override void MakeGrid()
        {

            Grid = new List<double[]>(3); //make the grid as a list of arrays: one for each of the 3 spatial directions

			for(int i = 0;i<2;i++){
				Grid.Add(new double[GridSize[i] + 1]); //along x and y there should be 1 more grid point, just to be sure!
				Grid[i][GridSize[i]] = GridSize[i] * GridStep[i]; //set value of the last the last point
			}
            //----------------------------------------------------
            Grid.Add(new double[GridSize[2]]); //add the z list

			//populate the grid
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

            int[] x=new int[3];
			DataGrid=new double[GridSize[0],GridSize[1],GridSize[2],Dim];

            while ((line = reader.ReadLine()) != null) { //read a line
                words = StringReader.TrimWords(line.Split(delimiterChars));
				
                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0) //skip comment/empty lines
                    continue;
				
                //read the i j k coords from the first 3 fields
                for (int i = 0; i < 3; i++)
					if(!Int32.TryParse(words[i], out x[i]))	{
						Console.WriteLine("ERROR! Grid xyz indeces have to be integer.");
						return false;
					}
				
				x[0]--;x[1]--;x[2]--; //decrease them by 1 because index starts from 0 in C
				
                for (int i = 0; i < Dim; i++){ //read the forces (Dim components)
					if(!double.TryParse(words[i + 3], out DataGrid[x[0], x[1], x[2], i])){
						Console.WriteLine("ERROR! Forces needs to be in number form.");
						return false;
					}
					DataGrid[x[0], x[1], x[2], i] *= ForceUnits; //convert them back to NANONEWTON!
                }

            }
            Console.WriteLine("\nForces values in the grid points loaded.\n");

            reader.Dispose();
            return true;
        }
		#endregion
		
		private void FetchIndex(int i, int j)
		{
			//build the index lists
			for(int ii=0;ii<4;ii++){
				xidx[ii] = (i+ii)%(GridSize[0]);
				yidx[ii] = (j+ii)%(GridSize[1]);
			}
		}
		private void FetchIndex(int[] ix)
		{
			//build the index lists
			for(int ii=0;ii<4;ii++){
				xidx[ii] = (ix[0]+ii)%(GridSize[0]);
				yidx[ii] = (ix[1]+ii)%(GridSize[1]);
			}
		}
		private void FetchSquare(int k, int c)
		{
			for(int ii=0;ii<4;ii++)
				for(int jj=0;jj<4;jj++)
					Square[ii,jj] = DataGrid[ xidx[ii], yidx[jj], k, c];
		}
		
		protected override void Interpolate ()
		{
			int i,j,k;
			
			//this offset is necessary because if the point is in  voxel ij
			//the bicubic interpolation should be done using a square starting from i-1 j-1
			//since FindVoxel is implemented the same way for everyone, we trick it to return i-1 j-1 instead!
			x[0]-=GridStep[0];
			x[1]-=GridStep[1];
			
			CenterCursor();
            FindVoxel(out i, out j, out k);
			
			//Console.WriteLine("voxel: {0} {1} {2}",i,j,k);
			//Console.WriteLine("XICUB xc: {0} {1} {2}",x[0],x[1],x[2]);
			//Console.WriteLine("AICUB xc: {0} {1} {2}",xc[0],xc[1],xc[2]);
			
			x1 = (xc[0] - Grid[0][xIndex[0]])/GridStep[0];
			x2 = x1 * x1;
			x3 = x2 * x1;
			y1 = (xc[1] - Grid[1][xIndex[1]])/GridStep[1];
			y2 = y1 * y1;
			y3 = y2 * y1;
			
			//Console.WriteLine("BICUB xc: {0} {1} {2}",x1,y1,xc[2]);
			
			//interpolate bicubic on the k plane
			FetchIndex(xIndex);
			for(int c=0;c<Dim;c++){
				FetchSquare(xIndex[2], c);
				
				f0[c]= (BiCubs[i,j,k,c,0,0] + BiCubs[i,j,k,c,0,1] * y1 + BiCubs[i,j,k,c,0,2] * y2 + BiCubs[i,j,k,c,0,3] * y3) +
					   (BiCubs[i,j,k,c,1,0] + BiCubs[i,j,k,c,1,1] * y1 + BiCubs[i,j,k,c,1,2] * y2 + BiCubs[i,j,k,c,1,3] * y3) * x1 +
					   (BiCubs[i,j,k,c,2,0] + BiCubs[i,j,k,c,2,1] * y1 + BiCubs[i,j,k,c,2,2] * y2 + BiCubs[i,j,k,c,2,3] * y3) * x2 +
					   (BiCubs[i,j,k,c,3,0] + BiCubs[i,j,k,c,3,1] * y1 + BiCubs[i,j,k,c,3,2] * y2 + BiCubs[i,j,k,c,3,3] * y3) * x3;
				f1[c]= (BiCubs[i,j,k+1,c,0,0] + BiCubs[i,j,k+1,c,0,1] * y1 + BiCubs[i,j,k+1,c,0,2] * y2 + BiCubs[i,j,k+1,c,0,3] * y3) +
					   (BiCubs[i,j,k+1,c,1,0] + BiCubs[i,j,k+1,c,1,1] * y1 + BiCubs[i,j,k+1,c,1,2] * y2 + BiCubs[i,j,k+1,c,1,3] * y3) * x1 +
					   (BiCubs[i,j,k+1,c,2,0] + BiCubs[i,j,k+1,c,2,1] * y1 + BiCubs[i,j,k+1,c,2,2] * y2 + BiCubs[i,j,k+1,c,2,3] * y3) * x2 +
					   (BiCubs[i,j,k+1,c,3,0] + BiCubs[i,j,k+1,c,3,1] * y1 + BiCubs[i,j,k+1,c,3,2] * y2 + BiCubs[i,j,k+1,c,3,3] * y3) * x3;
				
				Force[c] = (xc[2]-Grid[2][xIndex[2]])*(f1[c]-f0[c])/GridStep[2]+f0[c];
				
			}
			
			//y = m(x-x0) + y0
			
		}

		
		
		public override void Update (ref double dt)
		{
			for(int c=0;c<3;c++)
				x[c] = Input[c].Value;
			
			Output[0].Value = Evaluate();
			for(int c=0;c<Dim;c++)
				Output[c+1].Value=Force[c];
			
		}

		
	}
}
