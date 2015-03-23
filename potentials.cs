using vafmpro.AbsCircuits;
using System;


namespace vafmpro.Potentials
{
	
	public class morse : Potential
	{
		
		private double A, r0, d;
		private double tmp;
		
		public morse(string[] words):base(words){}
		
		
		public override void Initialize ()
		{
			A = StringReader.GetLineParameter_d(Words, "A");
			r0 = StringReader.GetLineParameter_d(Words, "r0");
			d = 1.0/StringReader.GetLineParameter_d(Words, "d");
		}
		
		public override void Evaluate (double[] rv, double dist)
		{
			tmp = Math.Exp((r0-dist)*d);
			tmp = -2.0 * A * d * tmp * (1.0-tmp)/dist;
			for(int c=0;c<3;c++){
					F[c] = tmp*rv[c]; //*(Atoms[i].type*2-1);
			}
		}

		
	}
	public class exp : Potential
	{
		
		private double A, r0, d;
		private double tmp;
		
		public exp(string[] words):base(words){}
		
		
		public override void Initialize ()
		{
			A = StringReader.GetLineParameter_d(Words, "A");
			r0 = StringReader.GetLineParameter_d(Words, "r0");
			d = 1.0/StringReader.GetLineParameter_d(Words, "d");
		}
		
		//V(r) = A Exp(-(r-r0)/d)  --> F(r) = A/d Exp(...)
		public override void Evaluate (double[] rv, double dist)
		{
			//tmp = Math.Exp((dist-r0)*d);
			tmp = A * d * Math.Exp((r0-dist)*d) / dist;
			for(int c=0;c<3;c++){
					F[c] = tmp*rv[c]; //*(Atoms[i].type*2-1);
			}
		}

		
	}
	
	
	
	
}

	
	