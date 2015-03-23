using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace vafmpro.Circuits.Forces
{
	
	//temp formula force = (a (x - x0)^2 - q)/(Exp[k (x - x1)] + 1);
	public class FFormula : Circuit
	{
		
		private double WellAmp, WellZ, WellOffset;
		private double FDSteep,FDZ;
				
		private double force,ztip;
		
		public FFormula(string[] words)
		{
			Init(words,1,1);
						
			Input.Add(new Channel("ztip",null));
			
			Output.Add(new Channel("Fz",this));
			
			Console.WriteLine("Circuit {0} (formula force) created.", Name);
		}
		public override void SetUp ()
		{
			WellAmp = GetInitParameter(InitWords,"a",true);
			WellZ = GetInitParameter(InitWords,"x0",true);
			WellOffset= GetInitParameter(InitWords,"q",true);
			FDSteep= GetInitParameter(InitWords,"k",true);
			FDZ= GetInitParameter(InitWords,"x1",true);

		}
		
		
		public override void Update(ref double dt)
		{	
			ztip = Input[0].Value*10; //convert it in Å
			
			force = (WellAmp*(ztip - WellZ)*(ztip - WellZ) - WellOffset);
			force /= Math.Exp(FDSteep*(ztip-FDZ))+1.0; //force in eV/Å
			
			force *= 1.60217646; //and in nanoNewton

			Output[0].Value = force;
			//Console.WriteLine("vdW force: {0}<-{1}",vdw,ztip);
		}
		
	}

	public class FFormula2 : Circuit
	{
						
		private double force,ztip;
		private double H1,H2;
		
		public FFormula2(string[] words)
		{
			Init(words,1,1);
						
			Input.Add(new Channel("ztip",null));
			
			Output.Add(new Channel("Fz",this));
			
			Console.WriteLine("Circuit {0} (formula force) created.", Name);
		}
		
		public override void SetUp ()
		{
			H1 = GetInitParameter(InitWords,"H1",true);
			H2 = GetInitParameter(InitWords,"H2",true);
		}
		
		public override void Update(ref double dt)
		{	
			ztip = 1.0/Input[0].Value;
			ztip *= ztip*ztip*ztip; //1/z^4
			
			force = H1*ztip + H2*ztip*ztip*ztip*ztip;
			

			Output[0].Value = force;
			
		}
		
	}

	
}
