using System;
using vafmpro.AbsCircuits;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;

namespace vafmpro.Circuits.Forces
{
	// 1D force field linear interpolator

	
	public class i1Dlin : Circuit
	{
		protected int Dim, GridSize, xIndex, PBC; //PBC=0 no periodicity
		protected double ForceUnits, GridUnits, GridStep, GridMin;
		protected double[] Grid,Force;
		protected double[,] DataGrid;
		protected double x,xc;

		public i1Dlin(string[] words)
		{
			Init(words);
			Console.WriteLine("\n---INITIALIZING 1D FORCE FIELD LINEAR INTERPOLATOR---");
			
			//coordinate for force evaluation
			Input.Add(new Channel("x", null)); //0
			
			//Output.Add(new Channel("error",this)); //0


		}
		
		public override void SetUp ()
		{
			string ForceFile = GetInitParameterString(InitWords,"file",true); //get the forcefield file
			
			//count the force points
			if(!CountPoints(ForceFile, out GridSize))
				throw new Exception(string.Format("ERROR! Circuit {0} (1D Field Linear Interpolator) not correctly initlized.", Name));
			
			//read grid parameters
			if(!ReadGrid(ForceFile))
				throw new Exception(string.Format("ERROR! Circuit {0} (1D Field Linear Interpolator) not correctly initlized.", Name));
			
			//make the grid
			MakeGrid();
			
			//read the data in
			if(!ReadForces_GridFormat(ForceFile))
				throw new Exception(string.Format("ERROR! Circuit {0} (1D Field Linear Interpolator) not correctly initlized.", Name));
			
			//make output channels
			for(int i=0; i<Dim; i++)
				Output.Add(new Channel("F"+i.ToString(),this));
			
			InputDefaultInit();
			
			Console.WriteLine("Circuit {0} (1D Field Linear Interpolator) created.", Name);
		}
		
		#region "Setup functions"
		protected virtual bool CountPoints(string ForceFile, out int points) {
			
			StreamReader reader = new StreamReader(ForceFile);
            string line="";
            string[] words;
            char[] delimiterChars = { ' ', '\t' };
			
			int pts = 0;
			
			if (!StringReader.FindString("<forces>", reader)) {
				Console.WriteLine("ERROR! The given force file does not contain forces.");
				points = 0;
				return false;
			}
			
			while ((line = reader.ReadLine()) != null) { //read a line
                words = StringReader.TrimWords(line.Split(delimiterChars));
				
                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0) //skip comment/empty lines
                    continue;
				pts++;
            }
			
			if (pts==0) {
				Console.WriteLine("ERROR! The given force file does not contain forces.");
				points = 0;
				return false;
			}
			
            Console.WriteLine("  {0} force points were found", pts.ToString());
			points = pts;
			reader.Dispose();
			return true;
		}
		protected virtual bool ReadGrid(string ForceFile)
        {
            StreamReader reader = new StreamReader(ForceFile);
            string line="";
            string[] words;
            char[] delimiterChars = { ' ', '\t' };


			Console.WriteLine("  Reading Grid...");
			
			if (!StringReader.FindString("<grid>", reader)) {
				Console.WriteLine("ERROR! Parameters for the grid were not found.");
				return false;
			}

			#region "force dimension"
            if (!StringReader.FindStringNoEnd("components", reader, "<grid>", ref line,ForceFile)){
                Console.WriteLine("FATAL! Specify the amount of components for the field.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 2){
                Console.WriteLine("FATAL! Specify the amount of components for the field.");
                return false;
            }
			if(!int.TryParse(words[1], out Dim)){
				Console.WriteLine("FATAL! You have to specity a numerical dimension for the forcefield.");
				return false;
			}
			Console.WriteLine("  Forcefield components: {0}",Dim);
			
            #endregion
            #region "force units"
            ForceUnits = 1; //assume nN
            if (!StringReader.FindStringNoEnd("forceunits", reader, "<grid>", ref line,ForceFile))
                Console.WriteLine("  Forcefield units are not specified, assuming nN (1).");
            else {
                words = StringReader.TrimWords(line.Split(delimiterChars));
				
				if (words.Length < 2)
					Console.WriteLine("  Forcefield units not specified, assuming nN (1).");
				else{
					ForceUnits = Convert.ToDouble(words[1]);
					Console.WriteLine("  Forcefield units: {0}", ForceUnits);
				}
			}
			#endregion
			
			#region "gridunits"
            GridUnits = 1;
			if (!StringReader.FindStringNoEnd("gridunits", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("INFO! Grid units are not specified, assuming nm (1).");
            }
			else
			{   //IMPORTANT!!!!
                words = StringReader.TrimWords(line.Split(delimiterChars));
                if (words.Length > 1)
                {
					//try the known words
					if (words[1] == "ang")
						GridUnits = 0.1;
					else if (words[1] == "nm")
						GridUnits = 1.0;
                    else if (words[1] == "m")
                        GridUnits = 1.0e9;
					else if(words[1] == "bohr")
						GridUnits = 52.9177e-3;
                    else{
						if(!double.TryParse(words[1], out GridUnits))
							Console.WriteLine("INFO! Unit type not recognized, assuming nm (1).");
					}
				}
				else
					Console.WriteLine("INFO! Unit type not recognized, assuming nm (1).");
				Console.WriteLine("  Forcefield grid units: {0}",GridUnits);
				
            }
            #endregion

            #region "gridstp"
			if (!StringReader.FindStringNoEnd("gridstp", reader, "<grid>", ref line, ForceFile)) {
				Console.WriteLine("FATAL! The grid step has to be specified.");
				return false;
			}
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 2) {
                Console.WriteLine("FATAL! You have to specity the grid step along one direction.");
                return false;
            }
			if (!double.TryParse(words[1], out GridStep)) {
				Console.WriteLine("FATAL! The grid steps have to be float!");
				return false;
			}

            #endregion
			#region "gridmin"
			if (!StringReader.FindStringNoEnd("gridmin", reader, "<grid>", ref line, ForceFile)) {
				Console.WriteLine("INFO! No grid min specified, assuming 0.");
				GridMin = 0;
			} else {
				
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if (words.Length < 2) {
					Console.WriteLine("FATAL! You have to specity the grid min along one direction.");
					return false;
				}
				if (!double.TryParse(words[1], out GridMin)) {
					Console.WriteLine("FATAL! The grid min has to be float!");
					return false;
				}
			}
			
			
            #endregion
			
            #region "periodicity"
			PBC = 0;
            if (!StringReader.FindStringNoEnd("periodicity", reader, "<grid>", ref line, ForceFile)){
                Console.WriteLine("INFO! Periodicity is not specified, assuming no PBC.");
			} else {
				
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if (words.Length < 2){
					Console.WriteLine("INFO! Periodicity is not specified correctly.");
					return true;
				}
				if (words[1] == "pacman")
					PBC = 1;
				//else if (words[i+1] == "mirror")
				//	PBC = 1;
				else{
					Console.WriteLine("INFO! Periodicity is not specified correctly, assuming no PBC.");
					PBC = 0;
				}
			}
			
            #endregion
            
            reader.Dispose();
            return true;
        }
		protected virtual void MakeGrid()
        {
			Force = new double[Dim];
            Grid = new double[GridSize+1]; //one more grid point if there is pacman geometry
			
			GridStep *= GridUnits;
			GridMin *= GridUnits;
			
			for(int i=0; i<=GridSize; i++) {
				Grid[i] = i*GridStep;
			}
			
			Console.WriteLine("  Grid created.");
        }
		protected virtual bool ReadForces_GridFormat(string ForceFile)
        {
			
			#region "init"
            StreamReader reader = new StreamReader(ForceFile);
            string line;
            string[] words;
            char[] delimiterChars = { ' ', '\t' };

			Console.WriteLine("  Reading Forces in grid format...");
			
            if (!StringReader.FindString("<forces>", reader)) {
                Console.WriteLine("ERROR! The forcefield is not there!");
                return false;
            }
			
			#endregion
			
            int xi;
			DataGrid = new double[GridSize, Dim];

            while ((line = reader.ReadLine()) != null) { //read a line
                words = StringReader.TrimWords(line.Split(delimiterChars));
				
                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0) //skip comment/empty lines
                    continue;
				
                //read the coord from the first field
				if(!Int32.TryParse(words[0], out xi)) {
					Console.WriteLine("ERROR! Grid index has to be integer.");
					return false;
				}
				
				xi--; //decrease it by 1 because index starts from 0 in C
				
                for (int i = 0; i < Dim; i++){ //read the forces (Dim components)
					if(!double.TryParse(words[i + 1], out DataGrid[xi, i])){
						Console.WriteLine("ERROR! Forces need to be in number form.");
						return false;
					}
					DataGrid[xi, i] *= ForceUnits; //convert them back to NANONEWTON!
                }

            }
            Console.WriteLine("  Forces values in the grid points loaded.");
			
            reader.Dispose();
            return true;
        }
		
		
		#endregion
		
		
		protected virtual void CenterCursor() {
			
			x = Input[0].Value - GridMin ; xc = x;
			
			if(PBC == 1) {
				xc = x - Math.Floor(x/(GridSize*GridStep))*GridSize*GridStep;
			}
			
			//if(xc < 0) xc = 0;
			//if(xc > GridStep*GridSize) xc = Grid[GridSize-1];
			
		}
		
		protected virtual void Interpolate()
        {

            int i,ii,iii;

            //center the position in the unit cell
            CenterCursor(); //also takes the input
			
			if(PBC == 0) { //if no pbc
				if(xc < 0) xc = 0;
				if(xc > GridStep*GridSize) xc = Grid[GridSize-1];
			}
			
            //find the voxel where the point is
            xIndex = (int)Math.Floor(xc/GridStep);
			
			i = xIndex; ii = i+1; 
			iii = (i+1) % (GridSize);
			if(PBC == 0) iii = Math.Min(ii,GridSize-1);
			
			//Console.WriteLine("{0} {1} {2} -> {3} {4} {5}",x[0],x[1],x[2],xc[0],xc[1],xc[2]);
			
			
			for(int c = 0; c<Dim; c++)
			{
				Force[c] = DataGrid[i,c] * (Grid[ii]-xc) + DataGrid[iii,c] * (xc-Grid[i]);
				Force[c] /= GridStep;
			}
			
			
			//if(Input[2].Value <= 0.3)
			//	Console.WriteLine("Force is {0} {1} {2}", Force[0], Force[1], Force[2]);

        }
		
		
		
		
		public override void Update (ref double dt)
		{
			Interpolate();
						
			for(int c=0;c<Dim;c++)
				Output[c].Value=Force[c];
		}
		
		
	}
	
}