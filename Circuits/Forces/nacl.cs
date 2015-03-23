using System;
using vafmpro.AbsCircuits;
using vafmpro.Potentials;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace vafmpro.Circuits.Forces
{
	public class nacl : Circuit
	{
		
		protected double[] TipPos = new double[3];
		protected double dPiOverLa, CutOff, zDecay, scale;
		protected double[] UnitSize = new double[2];
		
		public nacl (string[] words)
		{
			Init(words);
			Input.Add(new Channel("x", null)); //0
			Input.Add(new Channel("y", null)); //1
            Input.Add(new Channel("z", null)); //2
			
			Output.Add(new Channel("Fy",this)); //0
			Output.Add(new Channel("Fz",this)); //2
			
			
		}
		
		public override void SetUp ()
		{
			double La = GetInitParameter(InitWords,"La",true); 
			CutOff = GetInitParameter(InitWords,"cutoff",true); 
			zDecay = GetInitParameter(InitWords,"decay",false,1.0);
			scale = GetInitParameter(InitWords,"scale",false,1.0);
			
			UnitSize[0] = La; UnitSize[1] = La;
			
			dPiOverLa = 2.0*Math.PI/La;
			
			InputDefaultInit();
		}
		
		
		protected void SetZero()
		{
			for(int i=0;i<2;i++)
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
		
		public override void Update (ref double dt)
		{
			TipPos[2] = Input[2].Value;
			if(TipPos[2] >= CutOff){
				SetZero();
				return;
			}
			GetTip();
			
			Output[1].Value = zDecay*scale * Math.Cos(dPiOverLa*TipPos[0])*Math.Sin(dPiOverLa*TipPos[1])*Math.Exp(-TipPos[2]*zDecay);
			Output[0].Value = -scale * Math.Cos(dPiOverLa*TipPos[0])*Math.Cos(dPiOverLa*TipPos[1])*Math.Exp(-TipPos[2]*zDecay)*dPiOverLa;
			
			//Console.WriteLine("SURFACE: {0} {1} {2} - {3}",TipPos[0],TipPos[1],TipPos[2],Output[1].Signal.GetBufferedValue());
		}
		
	}
	

}

