using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace vafmpro.Circuits
{
	
	//TODO: documentation
    //exact phase shifter: if u know sine and cosine, then u can phase shift it exactly
    public class phi:Circuit
    {

		double ph,cosphi,sinphi;
		double sina,cosa;
		
        public phi(string[] words)
        {
            Init(words);

            Input.Add(new Channel("ssin", null));   //0
            Input.Add(new Channel("scos", null));   //1
            Input.Add(new Channel("phi", null));    //2

            Output.Add(new Channel("sin", this));   //0
            Output.Add(new Channel("cos", this));   //1
			
            Console.WriteLine("Circuit {0} (Phase shifter) created.\n", Name);
        }
		
		public override void SetUp ()
		{
			InputDefaultInit();
		}


       public override void Update(ref double dt)
        {
            ph = Input[2].Value;
            cosphi = Math.Cos(ph);
            sinphi = Math.Sin(ph);

            sina=Input[0].Value;
            cosa=Input[1].Value;

            Output[0].Value = sina * cosphi + cosa * sinphi; //sin a+b = sina cosb + cosa sinb
            Output[1].Value = cosa * cosphi - sina * sinphi; //cosa cosb - sina sinb

        }

    }

	
	public class delay : Circuit
    {
		private int steps, idx = 0;
		private double[] buffer;
		
        public delay(string[] words)
        {
            Init(words);

            Input.Add(new Channel("signal", null));   //0
			Output.Add(new Channel("out", this));     //0
			
            Console.WriteLine("Circuit {0} (Delay) created.\n", Name);
        }
		
		public override void SetUp ()
		{
			steps = (int)Math.Floor(GetInitParameter(InitWords, "time", true)/Program.mytimer.dt);
			buffer = new double[steps];
			
			InputDefaultInit();
		}


       public override void Update(ref double dt)
        {
			Math.DivRem(idx, steps, out idx);
			Output[0].Value = buffer[idx];
			buffer[idx] = Input[0].Value;
			idx++;
        }

    }


	
}
