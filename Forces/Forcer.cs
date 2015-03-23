using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;

namespace vafmpro
{
    public enum FieldType
    {
        Morse = 0,
        Grid = 1,
        FCurves = 2
    };
    public enum InterpolationType
    {
        Trilinear = 0,
        NURBS3D = 1,
        Hyperboloidal = 2
    };

    public struct ForceElement
    {
        public double Z;
        public double[] Forces;

        public ForceElement(double z, double[] force)
        {
            Z = z;
            Forces = new double[3];
            for (int i = 0; i < 3; i++)
            {
                Forces[i] = force[i];
            }
        }

    }

    
    public partial class Forcer
    {

        private string ForceFile;
        private FieldType ForceType;
        private InterpolationType InterpType;
        //private double InterpParam;
		public int Dims=3; //dimensions of the force grid 3 for xyz, 4 for xyzv

		public double GridUnits = 1.0;
		public double ForceUnits = 1.0;
		
        //private List<double[]> FCurvesMap; //list of [X,Y,index of forcecurve]
        //private List<List<ForceElement>> FCurves; // list of lists of forceelements
        private double[,,,,] DataGrid; //<-- forces here! (xyzv)
        private int[] PBC;

		public double[] Force = new double[3];
		
		//ALL POSITIONS ARE IN NANOMETERS
		//ALL FORCES ARE IN NANONEWTONS
		
		
        public Forcer(string filename)
        {
            ForceFile = filename;
            //ForceType = ftype;
            //InterpType = interptype;
            //InterpParam = interpparam;


        }


        public bool ReadForceFile()
        {

            //StreamReader reader = new StreamReader(ForceFile);

            Console.WriteLine("Reading forcefield...");

            //read the parameters
            if (!ReadParameters())
                return false;
            if (!ReadGrid())
                return false;

            //if the forcefield is given on a grid...
            if (ForceType == FieldType.Grid)
                if (!ReadForces_GridFormat())
                    return false;

            /*
            if (ForceType == FieldType.FCurves)
            {
                if (!ReadForceCurvesMap())//read the force curve map
                    return false;
                if(!ReadForces_FCurvesFormat()) //read the actual forcecurves
                    return false;

                HyperInterpolate(); //do the interpolation
            }
            */
            if (InterpType == InterpolationType.Trilinear)
                Trilin_Init();


            Console.WriteLine("Forcefield read correctly.");
/*
            #region "debug test"
            
			double[] asd = new double[4];
			double[] lol = new double[3];
            asd[0] = 0.0; asd[1] = 0.0; asd[2] = 5.1;

            StreamWriter writer = new StreamWriter("asder.txt");
            for (int i = 0; i < 564*2; i++)
            {
                Trilin_Force(asd, ref lol);
                writer.WriteLine("{0} {1}",asd[1],lol[2]);
                asd[1] += 0.01;
            }
            writer.Dispose();
            writer = new StreamWriter("asder2.txt");
            for (int i = 0; i < GridSize[1]; i++)
            {
                writer.WriteLine("{0} {1}", Grid[1][i], DataGrid[0,i,51,0,2]);
            }
            writer.Dispose();
            
            #endregion
*/
            return true;
        }

		//read the parameres box from the forcefile
        private bool ReadParameters()
        {

            StreamReader reader = new StreamReader(ForceFile);
            string line="";
            string[] words;
            char[] delimiterChars = { ' ', '\t' };


            if (!StringReader.FindString("<parameters>", reader))
            {
                Console.WriteLine("ERROR! Parameters for the forcefield were not found.");
                return false;
            }

            #region "field type"
            if (!StringReader.FindStringNoEnd("type", reader, "<parameters>", ref line,ForceFile))
            {
                Console.WriteLine("WARNING! The forcefield type was not found, assuming grid.");
				ForceType = FieldType.Grid;
				
            }
			else
			{
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if (words.Length < 2)
				{
					Console.WriteLine("FATAL! You have to specity a type: morse, grid or fcurves");
					return false;
				}
				if (words[1] == "morse")
					ForceType = FieldType.Morse;
				else if (words[1] == "grid")
					ForceType = FieldType.Grid;
				//else if (words[1] == "fcurves")
				//    ForceType = FieldType.FCurves;
				else
				{
					Console.WriteLine("FATAL! The forcefield type is not valid, chose: morse or grid.");
					return false;
				}
			}
			#endregion
			#region "dimensions"
            if (!StringReader.FindStringNoEnd("dimension", reader, "<parameters>", ref line,ForceFile))
            {
                Console.WriteLine("FATAL! The forcefield does not have a dimension.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 2)
            {
                Console.WriteLine("FATAL! You have to specity a dimension: xyz or xyzv");
                return false;
            }
            if (words[1] == "xyz")
				Dims=3;
            else if (words[1] == "xyzv")
				Dims=4;
            else
            {
                Console.WriteLine("FATAL! The forcefield dimension is not valid, chose: xyz or xyzv.");
                return false;
            }
            #endregion
			#region "interp type"
            InterpType = InterpolationType.Trilinear;
            /*
            if (!StringReader.FindStringNoEnd("interpolation", reader, "<parameters>", out line))
            {
                Console.WriteLine("INFO! Interpolation method was not specified, assuming trilinear.");
            }
            else
            {
                words = StringReader.TrimWords(line.Split(delimiterChars));
                if (words.Length < 2)
                {
                    Console.WriteLine("INFO! Interpolation method was not specified, assuming trilinear.");
                    return false;
                }
                else
                {
                    if (words[1] == "trilinear")
                        InterpType = InterpolationType.Trilinear;
                    else if (words[1] == "nurbs")
                        InterpType = InterpolationType.NURBS3D;
                    else if (words[1] == "hyperboloid")
                        InterpType = InterpolationType.Hyperboloidal;
                    else
                    {
                        Console.WriteLine("INFO! The interpolation type is not valid, assuming trilinear.");
                        InterpType = InterpolationType.Trilinear;
                    }

                    if (InterpType != InterpolationType.Trilinear)
                    {
                        if (words.Length < 3)
                        {
                            Console.WriteLine("FATAL! NURBS and Hyperboloiday interpolation requires a parameter (order or curvature).");
                            return false;
                        }
                        else
                            InterpParam = Convert.ToDouble(words[2]);
                    }

                }
            }*/
            #endregion
            #region "units"
            ForceUnits = 1;
            if (!StringReader.FindStringNoEnd("forceunits", reader, "<parameters>", ref line,ForceFile))
            {
                Console.WriteLine("INFO! Forces units are not specified, assuming nN.");
            }
            else
            {
                words = StringReader.TrimWords(line.Split(delimiterChars));
                if (words.Length > 1)
                {
                    if (words[1] == "eV/ang" || words[1] == "ev/ang")
                        ForceUnits = 1.60217646;
                    else if (words[1] == "N")
                        ForceUnits = 1.0e9;
                    else
                        Console.WriteLine("INFO! Unit type not recognized, assuming nN.");
                }

            }
            #endregion

            reader.Dispose();
            return true;
        }
        private bool ReadGrid()
        {
            StreamReader reader = new StreamReader(ForceFile);
            string line="";
            string[] words;
            char[] delimiterChars = { ' ', '\t' };


			Console.WriteLine("   Reading Grid...");
			
            if (!StringReader.FindString("<grid>", reader))
            {
                Console.WriteLine("ERROR! Parameters for the grid were not found.");
                return false;
            }

            GridSize = new int[4];for(int i=0;i<4;i++)GridSize[i]=1;
            UnitSize = new double[2];
            GridMin = new double[4];
            GridStep = new double[4];

            PBC = new int[2]; PBC[0] = 0; PBC[1] = 0;

            #region "gridPoints"
            if (!StringReader.FindStringNoEnd("points", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("FATAL! The grid size have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 1+Dims)
            {
                Console.WriteLine("FATAL! You have to specity the amount of points along each direction X Y Z (V).");
                return false;
            }
            for (int i = 0; i < Dims; i++) //read 3 or 4 quantities
            {
                if (!Int32.TryParse(words[i + 1], out GridSize[i]))
                {
                    Console.WriteLine("FATAL! The amount of grid points has to be integer!");
                    return false;
                }

            }
            #endregion
            #region "UnitSize"
            if (!StringReader.FindStringNoEnd("unitcell", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("FATAL! The grid size have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 3)
            {
                Console.WriteLine("FATAL! You have to specity the unit cell size along each direction X Y.");
                return false;
            }
            for (int i = 0; i < 2; i++)
            {
                if (!double.TryParse(words[i + 1], out UnitSize[i]))
                {
                    Console.WriteLine("FATAL! The unit cell size has to be float!");
                    return false;
                }

            }
            #endregion
            #region "gridunits"
            GridUnits = 1;
            if (!StringReader.FindStringNoEnd("gridunits", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("INFO! Grid units are not specified, assuming nm.");
            }
			else
			{   //IMPORTANT!!!!
                words = StringReader.TrimWords(line.Split(delimiterChars));
                if (words.Length > 1)
                {
                    if (words[1] == "ang")
                        GridUnits = 0.1;
                    else if (words[1] == "m")
                        GridUnits = 1.0e9;
					else if(words[1] == "bohr")
						GridUnits = 52.9177e-3;
                    else
                        Console.WriteLine("INFO! Unit type not recognized, assuming nm.");
					
                }

            }
            #endregion

            #region "gridmin"
            if (!StringReader.FindStringNoEnd("gridmin", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("FATAL! The grid starting points have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 1+Dims)
            {
                Console.WriteLine("FATAL! You have to specity the grid starting points along each direction X Y Z.");
                return false;
            }
            for (int i = 0; i < Dims; i++)
            {
                if (!double.TryParse(words[i + 1], out GridMin[i]))
                {
                    Console.WriteLine("FATAL! The grid starting points have to be float!");
                    return false;
                }

            }
            #endregion
            #region "gridstp"
            if (!StringReader.FindStringNoEnd("gridstp", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("FATAL! The grid steps  have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 1+Dims)
            {
                Console.WriteLine("FATAL! You have to specity the grid steps along each direction X Y Z.");
                return false;
            }
            for (int i = 0; i < Dims; i++)
            {
                if (!double.TryParse(words[i + 1], out GridStep[i]))
                {
                    Console.WriteLine("FATAL! The grid steps have to be float!");
                    return false;
                }

            }
            #endregion

            #region "periodicity"
            if (!StringReader.FindStringNoEnd("periodicity", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("INFO! Periodicity is not specified, assuming PBC in both X and Y.");
                return true;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 3)
            {
                Console.WriteLine("INFO! Periodicity is not specified correctly, assuming PBC in both X and Y.");
                return true;
            }
            for (int i = 0; i < 2; i++)
            {
                if (words[i+1] == "pacman")
                    PBC[i] = 0;
                else if (words[i+1] == "mirror")
                    PBC[i] = 1;
                else
                {
                    Console.WriteLine("INFO! Periodicity is not specified correctly, assuming PBC.");
                    PBC[i] = 1;
                }
            }
            #endregion


            MakeGrid();

            reader.Dispose();
            return true;
        }


		//get the tip-sample force (from the grid)
        public bool GetTSForce(double[] x)
        {
			//Console.WriteLine("gts {0} {1} {2}",x[0],x[1],x[2]);
			
            if (x[2] >= Grid[2][Grid[2].Length - 1]) //if tip too high do not extrapolate
                return true;
            if (Dims == 4 && (x[3] < GridMin[3] || x[3] >= Grid[3][Grid[3].Length - 1])) //if the voltage is out of bounds return an error
			{
				Console.WriteLine("ERROR! The tip-sample bias ({0}) is outside the given grid.",x[3]);
                return false;
			}
			if(x[2]<Grid[2][0])
			{
				Console.WriteLine("ERROR! The tip height ({0}) is lower then the minimum gridpoint.",x[2]);
				return false;
			}

			
			//if we arrive here everything was fine!
			Trilin_Force(x);


            return true;
        }

		
		//find the grid voxel where the tip is
        private void FindVoxel(double[] xc, ref int[] indexes)
        {
            int i;
            for (int c = 0; c < Dims; c++)
            {
                for (i = 0; i < Grid[c].Length; i++)
                {
                    if (Grid[c][i] > xc[c])
                    {
                        break;
                    }
                }
                indexes[c] = i-1;
            }

			//Console.WriteLine("VOXEL is {0} {1} {2}",indexes[0],indexes[1],indexes[2]);

        }

		//center the cursor in the unit cell
        public void CenterCursor(double[] x, ref double[] xc)
		{
			//on X direction
            if (PBC[0] == 0) //pacman symmetry
            {
                double div = Math.Truncate(x[0] / UnitSize[0]);
                xc[0] = Math.Abs(x[0]) - div * UnitSize[0];
            }
            else //mirror
            {
				xc[0]=x[0];
				if(x[0]<0) xc[0]=-x[0];      //if the point is in the negative side, flip it!
				
				do{
					if(xc[0] >= 2.0*UnitSize[0])
					{
						xc[0]=xc[0]-(2.0* Grid[0][GridSize[0]]);      //this centers the point in the range [0,2L]
							
					}
					else
					{
						if( xc[0] > Grid[0][GridSize[0]] )
						{
							xc[0]=2.0d*Grid[0][GridSize[0]]-xc[0];
						}
						break;
					}
				}while(true);

            }
			
			//on Y direction
			if(PBC[1] == 0) //pacman symmetry
			{
				double div=Math.Truncate(x[1]/UnitSize[1]);
				xc[1]= Math.Abs(x[1])-div*UnitSize[1];
			}
			else
			{
				xc[1]=x[1];
				if(x[1]<0) xc[1]=-x[1];      //if the point is in the negative side, flip it!
				
				do{
					if(xc[1] >= 2.0*UnitSize[1])
					{
						xc[1]=xc[1]-(2.0* Grid[1][GridSize[1]]);      //this centers the point in the range [0,2L]
							
					}
					else
					{
						if( xc[1] > Grid[1][GridSize[1]] )
						{
							xc[1]=2.0d*Grid[1][GridSize[1]]-xc[1];
						}
						break;
					}
				}while(true);
			}
            
			xc[2] = x[2];
            xc[3] = x[3];

			//Console.WriteLine("point {0} {1} {2} was centered in {3} {4} {5}",x[0].ToString(),x[1].ToString(),x[2].ToString(),
			//                  xc[0].ToString(),xc[1].ToString(),xc[2].ToString());
			
		}
		
		
		
		/*
subroutine CenterCursor(x,xc)
  implicit none
  double precision, intent(in) :: x(3)
  double precision, intent(out) :: xc(3)
  
  
  !center the cursor in the x direction
  xc(1)=x(1)
  if(xsymmetry==1)then
    !the pacman symmetry
    do
      if((xc(1)>xMax).or.(xc(1)<0.0d0)) then 
				xc(1)=xc(1)-(x(1)/Abs(x(1)))*xMax
      else
				exit
      endif
    enddo
    
!   else
!     !mirror geometry
!     
!     if(xc(1)<0) xc(1)=-xc(1)      !if the point is in the negative side, flip it!
!     
!     do
!       if((xc(1)>=2.0d0*GridX(pX))) then
!         xc(1)=xc(1)-(2.0d0*GridX(pX))      !this centers the point in the range [0,2L]
!       else
!         if( xc(1)>GridX(pX) ) then
!           xc(1)=2.0d0*GridX(pX)-xc(1)
! 				endif
!         exit
!       endif
!     enddo
  endif
  
  !center the cursor in the y direction
  xc(2)=x(2)
  if(ysymmetry==1)then    !use the right symmetry fcs
    !the pacman symmetry
    do
      if((xc(2)>yMax).or.(xc(2)<0.0d0)) then 
	xc(2)=xc(2)-(x(2)/abs(x(2)))*yMax
      else
	exit
      endif
    enddo
    
  else
    !the mirror symmetry
    
    if(xc(2)<0) xc(2)=-xc(2)      !if the point is in the negative side, flip it!
    
    do
      if((xc(2)>=2.0d0*GridY(pY))) then
        xc(2)=xc(2)-(2.0d0*GridY(pY))      !this centers the point in the range [0,2L]
      else
        if( xc(2)>GridY(pY) ) then
          xc(2)=2.0d0*GridY(pY)-xc(2)
	endif
        exit
      endif
    enddo
    
  endif
  
  xc(3)=x(3)
  
  return

end subroutine CenterCursor
		 * */





    }
}
