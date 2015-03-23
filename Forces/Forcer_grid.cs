using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;

namespace vafmpro
{


    public partial class Forcer
    {

        private double[] UnitSize, GridMin,GridStep;
        private int[] GridSize;

        private List<double[]> Grid;

        //create forcecurve map

        //create the regular grid with the input parameters
        private void MakeGrid()
        {

            Grid = new List<double[]>(Dims); //make the grid

            //create X and Y grid depending on the periodicity....
            //if (PBC[0] == 0)
            //{
                Grid.Add(new double[GridSize[0] + 1]); //one more grid point if there is pacman geometry
                Grid[0][GridSize[0]] = GridMin[0] + (GridSize[0]) * GridStep[0];
            //}
            //else
            //    Grid.Add(new double[GridSize[0]]);

            //if (PBC[1] == 0)
            //{
                Grid.Add(new double[GridSize[1] + 1]);
                Grid[1][GridSize[1]] = GridMin[1] + (GridSize[1]) * GridStep[1];
            //}
            //else
            //    Grid.Add(new double[GridSize[1]]);
            //----------------------------------------------------
            Grid.Add(new double[GridSize[2]]);
            Grid.Add(new double[GridSize[3]]);

            for (int i = 0; i < Dims; i++)
            {
                for (int j = 0; j < GridSize[i]; j++)
                    Grid[i][j] = GridMin[i] + j * GridStep[i];
            }

			//now parse them all and multiply to get them in NANOMETERS
			for (int i = 0; i < 3; i++)
			{
				GridMin[i] *= GridUnits;
				GridStep[i] *= GridUnits;
				for (int j = 0; j < Grid[i].Length; j++)
					Grid[i][j] *= GridUnits;
			}
			UnitSize[0] *=GridUnits;
			UnitSize[1] *=GridUnits;
						
        }

		/*
        //read the map of force curves on the surface
        private bool ReadForceCurvesMap()
        {
            StreamReader reader = new StreamReader(ForceFile);
            string line;
            string[] words;
            char[] delimiterChars = { ' ', '\t' };

            if (!StringReader.FindString("<fcurves>", reader))
            {
                Console.WriteLine("ERROR! The forcefield is not there!");
                return false;
            }

            FCurvesMap = new List<double[]>();

            int maxfc = 0;

            while ((line = reader.ReadLine()) != null) //read a line
            {
                words = StringReader.TrimWords(line.Split(delimiterChars));

                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0) //skip comment/empty lines
                    continue;

                if (words.Length < 3)
                {
                    Console.WriteLine("WARNING! line:{0} was not valid and it will be skipped.",line);
                    continue;
                }

                
                FCurvesMap.Add(new double[3]);
                for (int i = 0; i < 3; i++)
                {
                    double.TryParse(words[i], out FCurvesMap[FCurvesMap.Count - 1][i]);                    
                }

                if (FCurvesMap[FCurvesMap.Count - 1][2] > maxfc)
                    maxfc = Convert.ToInt32(FCurvesMap[FCurvesMap.Count - 1][2]);

            }

            FCurves=new List<List<ForceElement>>();
            for (int i = 0; i < maxfc; i++)
                FCurves.Add(new List<ForceElement>());

            reader.Dispose();
            return true;
        }
*/
		/*
        private bool ReadForces_FCurvesFormat()
        {
            StreamReader reader = new StreamReader(ForceFile);
            string line;
            string[] words;
            char[] delimiterChars = { ' ', '\t' };

            if (!StringReader.FindString("<forces>", reader))
            {
                Console.WriteLine("ERROR! The forcefield is not there!");
                return false;
            }


            while ((line = reader.ReadLine()) != null) //read a line
            {
                words = StringReader.TrimWords(line.Split(delimiterChars));

                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0) //skip comment/empty lines
                    continue;

                if (words.Length < 5)
                {
                    Console.WriteLine("WARNING! line:{0} was not valid and it will be skipped.", line);
                    continue;
                }

                int idx;
                int.TryParse(words[0],out idx);
                double[] f = new double[3];
                double z;
                double.TryParse(words[1], out z);
                for (int i = 0; i < 3; i++)
                {
                    double.TryParse(words[i + 2], out f[i]);
                }


                FCurves[idx-1].Add(new ForceElement(z,f));


            }



            Console.WriteLine("\nForces values in the grid points loaded.\n");

            reader.Dispose();
            return true;
        }
*/
        private bool ReadForces_GridFormat()
        {

            StreamReader reader = new StreamReader(ForceFile);
            string line;
            string[] words;
            char[] delimiterChars = { ' ', '\t' };


			Console.WriteLine("   Reading Forces in grid format...");
			
			
			
            if (!StringReader.FindString("<forces>", reader))
            {
                Console.WriteLine("ERROR! The forcefield is not there!");
                return false;
            }

            int[] x=new int[4];

			DataGrid=new double[GridSize[0],GridSize[1],GridSize[2],GridSize[3],3];
            

            while ((line = reader.ReadLine()) != null) //read a line
            {
                words = StringReader.TrimWords(line.Split(delimiterChars));

                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0) //skip comment/empty lines
                    continue;

                //read the i j k coords from the first 3 fields
                for (int i = 0; i < Dims; i++)
				{
					if(!Int32.TryParse(words[i], out x[i]))
					{
						Console.WriteLine("ERROR! Grid xyz indeces have to be integer.");
						return false;
					}
				}
				
				if(Dims==3) x[3]=1;
				x[0]--;x[1]--;x[2]--;x[3]--; //decrease them by 1 because index starts from 0 in C
				
                for (int i = 0; i < 3; i++) //read the forces (3 components)
                {
                    if(!double.TryParse(words[i + Dims], out DataGrid[x[0], x[1], x[2],x[3], i]))
					{
						Console.WriteLine("ERROR! Forces needs to be in number form.");
						return false;
					}
					DataGrid[x[0], x[1], x[2],x[3], i] *= ForceUnits; //convert them back to NANONEWTON!
                }

            }
            Console.WriteLine("\nForces values in the grid points loaded.\n");

            reader.Dispose();
            return true;
        }



	} //end of class
	
	
}