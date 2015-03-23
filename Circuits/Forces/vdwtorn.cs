using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace vafmpro.Circuits.Forces
{
	
	//van der Waals calculator for a plane surface with a sphere-cone tip
	public class vdwTh : Circuit
	{
		
		private double A1,A2,A3,A4,A5, TipOffset;
		
		private double vdw,ztip;
		
		public vdwTh(string[] words)
		{
			Init(words,1,1);
						
			Input.Add(new Channel("ztip",null));
			
			Output.Add(new Channel("Fz",this));
			
			Console.WriteLine("Circuit {0} (vdW plane-sphere/cone) created.", Name);
		}
		public override void SetUp ()
		{
			A1 = GetInitParameter(InitWords,"A1",true);
			A2 = GetInitParameter(InitWords,"A2",true);
			A3 = GetInitParameter(InitWords,"A3",true);
			A4 = GetInitParameter(InitWords,"A4",true);
			A5 = GetInitParameter(InitWords,"A5",true);
			TipOffset= GetInitParameter(InitWords,"offset",false,0.0);
			
		}
		
		
		public override void Update(ref double dt)
		{	
			//TODO: step geometry
			ztip = 10*Input[0].Value + 10*TipOffset; //convert z to angstrom and add offset
			if(ztip == 0.0)
				return;
			
			vdw = A1*A2*Math.Exp(-A2*ztip);
			
			double r3=ztip*ztip*ztip; //r^3
			double r7=r3*r3*ztip;
			double r11=(10*A5)/(r7*r3*ztip);
			
			r7 = (6*A3)/r7;
			double r9 = (8*A4)/(r3*r3*r3);
			
			vdw += r11+r9+r7;
			vdw*= 1.60217656; //converted in NANONEWTON

			Output[0].Value = vdw;
			//Console.WriteLine("vdW force: {0}<-{1}",vdw,ztip);
		}
		
	}
	
}
