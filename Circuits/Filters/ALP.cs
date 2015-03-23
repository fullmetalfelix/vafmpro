using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;

namespace vafmpro.Circuits
{
	//active lowpass circuit
	public class SKLP : Circuit
	{
		
		private double yoo, yo, y,x;
		private double alpha;
		private double wc, gamma, gain;
		
		
		//this filter requires that the timestep was defined before!
		public SKLP(string[] words)
		{
			Init(words,1,1);
			Console.WriteLine("Circuit {0} (Sallen-Key Low Pass) created.\n", Name);
		}
		
		public override void SetUp ()
		{
			Input.Add(new Channel("signal", null));
			Output.Add(new Channel("out", this));
			
			//get the init params
			wc = 2*Math.PI*GetInitParameter(InitWords,"fc",true) * Program.mytimer.dt; //wc = w*dt
			gamma = GetInitParameter(InitWords,"Q",true);
			gamma = wc/(2.0*gamma); //gamma = w*dt/(2Q)!
			gain = GetInitParameter(InitWords,"gain",false,1.0);
			wc = wc*wc;  //wc = w²dt²
			alpha = 1.0/(1.0 + gamma + wc);
      
	}
     
    public override void Update(ref double dt)
    {
      
      x = Input[0].Value;
      y = gain*wc*x + (2.0*yo-yoo) + gamma*yoo;
      y *= alpha;
      
      Output[0].Value = y;
      
      yoo = yo;
      yo = y;
      
    }
  }
  
	//active highpass circuit
	public class SKHP : Circuit
	{
		
		private double yoo, yo, y,x,xo,xoo;
		private double alpha,gamma,wc, gain;
		
		//this filter requires that the timestep was defined before!
		public SKHP(string[] words)
		{
			Init(words,1,1);
			Console.WriteLine("Circuit {0} (Sallen-Key High Pass) created.\n", Name);
		}
		public override void SetUp ()
		{
			Input.Add(new Channel("signal", null));
			Output.Add(new Channel("out", this));
			
			//get the init params
			gain = GetInitParameter(InitWords,"gain",false,1.0);
			wc = 2*Math.PI*GetInitParameter(InitWords,"fc",true)*Program.mytimer.dt; //wc = w*dt
			gamma = GetInitParameter(InitWords,"Q",true);
			gamma = wc/(2.0*gamma); //gamma = w*dt/(2Q)
			alpha = 1.0/(1.0 + gamma + wc*wc);

		}
		


        public override void Update(ref double dt)
        {
			x = Input[0].Value;
			y = (2*yo-yoo) + gamma*yoo + gain*(xoo-2.0*xo+x);
			y*= alpha;

            Output[0].Value = y;
            yoo = yo; xoo = xo;
            yo = y; xo = x;
			
        }
    }

	//active Bandpass circuit
	public class SKBP : Circuit
    {

        private double yoo, yo, y,x,xo,xoo;
		private double alpha, gamma, wc, gain;
		
        //this filter requires that the timestep was defined before!
        public SKBP(string[] words)
        {
            Init(words,1,1);
			Console.WriteLine("Circuit {0} (Sallen-Key Band Pass) created.", Name);
		}
		
		public override void SetUp ()
		{
            Input.Add(new Channel("signal", null));
            Output.Add(new Channel("out", this));

			//get the init params
			gain = GetInitParameter(InitWords,"gain",false,1.0);
			wc = GetInitParameter(InitWords,"fc",true); //wc = fc
			gamma = GetInitParameter(InitWords,"band",true); //gamma = bw
			gamma = wc / gamma; //now gamma is Q

			wc = 2*Math.PI* wc * Program.mytimer.dt; //wc = w*dt
			gamma = wc/(2.0*gamma); //gamma = w*dt/(2Q)!

			alpha = 1.0/(1.0 + gamma + wc*wc);

            
        }


        public override void Update(ref double dt)
        {
			x = Input[0].Value;
			
			y = gain*gamma*(x-xoo) + gamma*yoo + (2.0*yo-yoo);
			y*= alpha;

            Output[0].Value = y;
            yoo = yo; xoo = xo;
            yo = y; xo = x;
			
        }
    }

}
