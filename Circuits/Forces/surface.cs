using System;
using vafmpro.AbsCircuits;
using vafmpro.Potentials;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace vafmpro.Circuits.Forces
{
	
	
	public class AtomType
	{
		public string Name;
		public Potential Pot;
		
		public AtomType(string InputLine)
		{
			string[] words = StringReader.TrimWords(InputLine.Split(StringReader.StdSeps));
			Name = words[0];
			
			//get the name of the potential
			string potname = StringReader.GetLineParameter_s(words,"potential");
			if(!Program.myPotentialTypes.ContainsKey(potname))
				throw new Exception("ERROR! Potential type "+potname+" is not coded.");
			Type pottype = Program.myPotentialTypes[potname];
			Pot = (Potential) Activator.CreateInstance(pottype, new object[]{words});
			Pot.Initialize();
		}
		
	}
	
	
	public struct Atom{
		public AtomType type;
		public double[] x;
		
		public Atom(AtomType t, double px, double py, double pz)
		{
			type = t;
			x = new double[3];
			x[0] = px;
			x[1] = py;
			x[2] = pz;
		}
	}
	public class Dimer{
		public Atom Atom1, Atom2;
		public double[] p1up,p2up,p1dw,p2dw;
		
	}
	
	public class surface : Circuit
	{
		protected int Dim = 1, Replica = 1;
		protected double ForceUnits = 1.0, GridUnits = 1.0, CutOff = 1.0;
		protected double[] UnitSize = new double[3];
		
		protected List<AtomType> ATypes;
		protected List<Atom> Atoms;
		
		protected string[] AtomForces;
		
		public double[] TipPos = new double[3];
		public double[] Force = new double[3];
		protected double[] r = new double[3];
		protected double dist,dist1;
		
		
		public surface(string[] words)
		{
			Init(words);
			Console.WriteLine("\n---INITIALIZING Surface Forcefield---");
			
			//coordinates for force evaluation
			Input.Add(new Channel("x", null)); //0
			Input.Add(new Channel("y", null)); //1
            Input.Add(new Channel("z", null)); //2
			
			Output.Add(new Channel("Fx",this)); //0
			Output.Add(new Channel("Fy",this)); //1
			Output.Add(new Channel("Fz",this)); //2
		}
		
		public override void SetUp ()
		{
			string ForceFile = GetInitParameterString(InitWords,"file",true); //get the forcefield
			
			if(Initialize(ForceFile))
				Console.WriteLine("Circuit {0} (Surface field) created.", Name);
			else{
				//Console.WriteLine("ERROR! Circuit {0} (3D Field Linear Interpolator) not correctly initlized.", Name);
				throw new Exception(string.Format("ERROR! Circuit {0} (Surface field) not correctly initlized.", Name));
			}
			
			InputDefaultInit();
		}
		

		
		//read the input file...
		protected bool Initialize(string ForceFile)
		{
		
			//read unit cell and FF parameters
			if(!ReadParams(ForceFile))
				return false;
			if(!ReadAtomTypes(ForceFile))
				return false;
			if(!ReadAtomPositions(ForceFile))
				return false;
			
			//Debug();
			
			return true;
		}
		protected bool ReadParams(string ForceFile)
		{
			
			StreamReader reader = new StreamReader(ForceFile);
            string line="";
            string[] words;
            char[] delimiterChars = { ' ', '\t' };

			Console.WriteLine("  Reading surface parameters...");
			
            if (!StringReader.FindString("<grid>", reader)) {
                Console.WriteLine("ERROR! The surface parameters <grid> was not found!");
                return false;
            }
			
            #region "force units"
            ForceUnits = 1.0; //assume nN
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
                    else if (words[1] == "m")
                        GridUnits = 1.0e9;
					else if (words[1] == "nm")
                        GridUnits = 1.0;
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
			#region "UnitSize"
			UnitSize[2] = 9999999.0; //so bogus
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
            for (int i = 0; i < 2; i++){
                if (!double.TryParse(words[i + 1], out UnitSize[i])) {
                    Console.WriteLine("FATAL! The unit cell size has to be a floating point number!");
                    return false;
                }
				UnitSize[i] *= GridUnits;
            }
			Console.WriteLine("  Basic unit cell size: {0}nm {1}nm",UnitSize[0],UnitSize[1]);
            #endregion
			#region "replica"
            if (!StringReader.FindStringNoEnd("replicate", reader, "<grid>", ref line,ForceFile)){
                Console.WriteLine("INFO! The unit cell will not be replicated.");
            }
			else
			{
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if (words.Length < 2){
					Console.WriteLine("FATAL! Specify the amount of cunit cell replica.");
					return false;
				}
				if(!int.TryParse(words[1], out Replica)){
					Console.WriteLine("FATAL! Specify the amount of cunit cell replica numerically.");
					return false;
				}
				Console.WriteLine("  Unit cell replica: {0}", Replica);
				UnitSize[0] *= Replica;
				UnitSize[1] *= Replica;
				Console.WriteLine("  Extended unit cell size: {0}nm {1}nm",UnitSize[0],UnitSize[1]);
			}
            #endregion
			#region "Cutoff"
            if (!StringReader.FindStringNoEnd("cutoff", reader, "<grid>", ref line,ForceFile)){
                Console.WriteLine("ERROR! Force cutoff not specified.");
				return false;
            }
			
			words = StringReader.TrimWords(line.Split(delimiterChars));
			if (words.Length < 2){
				Console.WriteLine("ERROR! Force cutoff not specified.");
				return false;
			}
			if(!double.TryParse(words[1], out CutOff)){
				Console.WriteLine("ERROR! Force cutoff not specified numerically.");
				return false;
			}
			CutOff *= GridUnits;
			Console.WriteLine("  Force cutoff: {0} nm", CutOff);
            #endregion
			
			
			reader.Dispose();
			return true;
		}
		
		//TODO: interaction parameters?
		protected bool ReadAtomTypes(string ForceFile)
		{
			
			StreamReader reader = new StreamReader(ForceFile);
            string line="";
            string[] words;
            char[] delimiterChars = { ' ', '\t' };

			Console.WriteLine("  Reading atomic species...");
			#region "read atomic types"
            if (!StringReader.FindString("<types>", reader)) {
                Console.WriteLine("ERROR! The surface atom types <types> was not found!");
                return false;
            }
			
			ATypes = new List<AtomType>();
			while ((line = reader.ReadLine()) != null) { //read a line
                words = StringReader.TrimWords(line.Split(delimiterChars));
				
				#region "check for bad lines"
                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0) //skip comment/empty lines
                    continue;
				if (words[0].StartsWith("#"))
					continue;
				#endregion
				
				ATypes.Add(new AtomType(line));
				
			}
			Console.WriteLine(" types read.");
			reader.Dispose();
			#endregion
			
			
			return true;
		}
		
		protected bool ReadAtomPositions(string ForceFile)
		{
			
			StreamReader reader = new StreamReader(ForceFile);
            string line="";
            string[] words;
            char[] delimiterChars = { ' ', '\t' };

			Console.WriteLine("  Reading atoms...");
			#region "read atoms"
            if (!StringReader.FindString("<atoms>", reader)) {
                Console.WriteLine("ERROR! The surface atoms list <atoms> was not found!");
                return false;
            }
			
			Atoms = new List<Atom>();
			
			while ((line = reader.ReadLine()) != null) { //read a line
                words = StringReader.TrimWords(line.Split(delimiterChars));
				
				#region "check for bad lines"
                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0) //skip comment/empty lines
                    continue;
				if (words[0].StartsWith("#"))
					continue;
				if(words.Length<4){
					Console.WriteLine("ERROR! Bad atom! ({0})",line);
					return false;
				}
				
				if(ATypes.Find(c => c.Name == words[0]) == null)
					throw new Exception("ERROR! Atom type "+words[0]+" not declared.");
				
				#endregion
				
				Atom p = new Atom();
				p.type = ATypes.Find(c => c.Name == words[0]);
				p.x = new double[3];
				for(int i=0;i<3;i++){
					if(	!double.TryParse(words[i+1],out p.x[i])){
						Console.WriteLine("ERROR! Atom coordinates should be numbers!");
						return false;
					}
					p.x[i] *= GridUnits;
				}
				Atoms.Add(p);
			}
			
			if(Atoms.Count == 0){
				Console.WriteLine("ERROR! No atoms were found!");
				return false;
			}
			
			Console.WriteLine(" atoms read.");
			reader.Dispose();
			#endregion
			
			#region "replicate atoms"
			List<Atom> newlist = new List<Atom>();
			foreach(Atom p in Atoms){
				
				for(int i=0;i<Replica;i++){
					for(int j=0;j<Replica;j++){
						newlist.Add( new Atom(p.type, p.x[0]+i*UnitSize[0]/Replica, p.x[1]+j*UnitSize[1]/Replica, p.x[2]) );
					}
				}
			}
			Atoms = newlist;
			
			#endregion
			
			return true;
		}
		
		
		protected void SetZero()
		{
			for(int i=0;i<Dim;i++)
				Output[i].Value = 0.0;
		}
		protected void GetTip()
		{
			//center in the replicated unit cell
			for(int i=0;i<2;i++){
				TipPos[i] = Input[i].Value;
				TipPos[i] -= Math.Floor(TipPos[i]/UnitSize[i])*UnitSize[i];
			}
			
		}
		
		protected void Debug()
		{
			StreamWriter o = new StreamWriter("debugger.out");
			Input[2].Value = 0.5;
			Input[2].Signal.PushBuffer();
			double x=0.0,y;
			
			
			for(x=0;x<2*UnitSize[0];x+= UnitSize[0]/50.0){
				for(y=0;y<2*UnitSize[1];y+= UnitSize[1]/50.0){
					Input[0].Value = x; Input[0].Signal.PushBuffer();
					Input[1].Value = y; Input[1].Signal.PushBuffer();
					Update(ref x);
					o.WriteLine("{0} {1} {2}",x,y,Force[2]);
				}
				o.WriteLine("");
			}
			
			Input[0].Value = -0.01; Input[0].Signal.PushBuffer();
			Input[1].Value = 0; Input[1].Signal.PushBuffer();
			Update(ref x);
			Console.WriteLine("test {0}",Force[2]);
			
			Input[0].Value = 0; Input[0].Signal.PushBuffer();
			Input[1].Value = 0; Input[1].Signal.PushBuffer();
			Update(ref x);
			Console.WriteLine("test {0}",Force[2]);
			
			Input[0].Value = 0.01; Input[0].Signal.PushBuffer();
			Input[1].Value = 0; Input[1].Signal.PushBuffer();
			Update(ref x);
			Console.WriteLine("test {0}",Force[2]);
			
			o.Dispose();
		}
		
		public override void Update (ref double dt)
		{
			TipPos[2] = Input[2].Value;
			if(TipPos[2] >= CutOff){
				SetZero();
				return;
			}
			GetTip();
			
			//interact with the atoms within cutoff
			for(int i=0;i<3;i++)
				Force[i] = 0.0;
			
			for(int i=0;i<Atoms.Count;i++){
				dist = 0.0;
				for(int c=0;c<3;c++){
					r[c] = TipPos[c]-Atoms[i].x[c]; //r points from the atom towards the tip
					//Console.WriteLine("asd {0}",r[c] / UnitSize[c]);
					r[c] -= Math.Round(r[c] / UnitSize[c])*UnitSize[c];
					dist += r[c]*r[c];
				}
				//if(dist >= CutOff*CutOff)
				//	continue;
				//Console.WriteLine("interacts with {0}",i);
				
				dist1 = Math.Sqrt(dist);
				
				Atoms[i].type.Pot.Evaluate(r,dist1); //calculate interaction
				
				for(int c=0;c<3;c++)
					Force[c] += Atoms[i].type.Pot.F[c];
				
			}
			//Console.WriteLine("-------------------");
			for(int c=0;c<3;c++){
				if(double.IsNaN(Force[c]) || double.IsInfinity(Force[c]))
					Force[c] = 0.0;
				Output[c].Value = Force[c];
			}
			
			
			
			//Console.WriteLine("SURFACE: {0}  force {1}",TipPos[2],Force[2]);
		}
		
	}
	
	
}