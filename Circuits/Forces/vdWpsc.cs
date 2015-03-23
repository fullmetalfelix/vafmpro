using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace vafmpro.Circuits.Forces
{
	
	//van der Waals calculator for a plane surface with a sphere-cone tip
	public class vdWpsc : Circuit
	{
		
		private double TipRadius, TipHamak, TipAngle, TipOffset;
		private double sing,tang,cosg,cos2g;
		private double TR2, TRS, TRC;
		
		private double vdw,ztip;
		
		public vdWpsc(string[] words)
		{
			Init(words,1,1);
						
			Input.Add(new Channel("ztip",null));
			
			Output.Add(new Channel("Fz",this));
			
			Console.WriteLine("Circuit {0} (vdW plane-sphere/cone) created.", Name);
		}
		public override void SetUp ()
		{
			TipAngle = GetInitParameter(InitWords,"alpha",true);
			TipHamak = GetInitParameter(InitWords,"hamaker",true);
			TipRadius= GetInitParameter(InitWords,"radius",true);
			TipOffset= GetInitParameter(InitWords,"offset",false,0.0);
			
			double g2r = Math.PI/180.0;
			sing = Math.Sin(TipAngle*g2r);
			tang = Math.Tan(TipAngle*g2r);
			cosg = Math.Cos(TipAngle*g2r);
			cos2g= Math.Cos(TipAngle*g2r*2.0);

			//TipRadius*=1.0e-9;
            TR2 = TipRadius*TipRadius;
			TRC = TipRadius*cos2g;
			TRS = TipRadius*sing;
			TipHamak = TipHamak* 1.0e18;//converted in NANONEWTON-NANOMETER
			
		}
		
		
		public override void Update(ref double dt)
		{	
			//TODO: step geometry
			ztip = Input[0].Value + TipOffset;
			if(ztip == 0.0)
				return;
			
			vdw = (TipHamak*TR2)*(1.0-sing)*(TRS-ztip*sing-TipRadius-ztip);
			vdw/= (6.0*(ztip*ztip)*(TipRadius+ztip-TRS)*(TipRadius+ztip-TRS));
			vdw-= (TipHamak*tang*(ztip*sing+TRS+TRC))/(6.0*cosg*(TipRadius+ztip-TRS)*(TipRadius+ztip-TRS));
			//vdw*= 1.0e18; //converted in NANONEWTON

			Output[0].Value = vdw;
			//Console.WriteLine("vdW force: {0}<-{1}",vdw,ztip);
		}
		
	}
	
}
