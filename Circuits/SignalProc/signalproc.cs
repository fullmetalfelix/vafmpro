using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;

namespace vafmpro.Circuits
{
	
	public class gain : Circuit
    {
        private double mygain = 0.0;

        public gain(string[] words)
        {
            Init(words);

            Input.Add(new Channel("signal",null));
			Output.Add(new Channel("out", this));

            Console.WriteLine("Circuit {0} (gain) created.\n", Name);
        }
		public override void SetUp ()
		{
			mygain = GetInitParameter(InitWords,"gain",true);
			InputDefaultInit();
		}

       public override void Update(ref double dt)
        {
            Output[0].Value = Input[0].Value * mygain;
        }
    }
	
	
	public class peaker : Circuit
    {

        private double signalO = 0.0;
        private double signalOO = 0.0;
        private double delay = 0.0;
        private bool Up,peaked;

        public peaker(string[] words)
        {
            Init(words);
			
            Console.WriteLine("Circuit {0} (peaker) created.\n", Name);

        }
		public override void SetUp ()
		{
			Input.Add(new Channel("signal", null));

            Output.Add(new Channel("tick",this));  //0: gets to 1 if peak is detected
            Output.Add(new Channel("peak",this));  //1: the peak value
            Output.Add(new Channel("delay",this)); //2: time since last peak
			
			Up = false;
			if(GetInitParameter(InitWords,"up",true) == 1)
				Up = true;
			
			InputDefaultInit();
			
		}


       public override void Update(ref double dt)
        {
            delay += dt;
            peaked = false;

            #region "up peak"
            if (Up) //detect upward peak
            {
                if ((signalOO < signalO) && (signalO > Input[0].Value))
                {
                    Output[0].Value = 1;
                    Output[1].Value = signalO;
                    //delay -= dt; //remove the last dt
                    peaked = true;
					//Console.WriteLine("PEAK!" + Output[0].Value.ToString());
                }
                else
                {
                    Output[0].Value = 0;
                }
            }
            #endregion
            #region "down peak"
            else
            {
                if ((signalOO > signalO) && (signalO < Input[0].Value))
                {
                    Output[0].Value = 1;
                    Output[1].Value = signalO;
                    //delay -= dt; //remove the last dt
                    peaked = true;
                }
                else
                {
                    Output[0].Value = 0;
                }
            }
            #endregion

            

            if (peaked){
                Output[2].Value = delay;
                delay = 0.0;
				//Console.WriteLine("PEAK2!" + Output[0].Value.ToString());
            }

            signalOO = signalO;
            signalO = Input[0].Value;

        }

    }
	
	
    public class deriver : Circuit
    {

        private double yoo, yo, y;
        private bool P3 = false;
		
        public deriver(string[] words)
        {
            Init(words,1,1);
			
            Input.Add(new Channel("signal", null));
			Output.Add(new Channel("out", this));

            Console.WriteLine("Differentiator {0} created.\n", Name);
        }
		public override void SetUp ()
		{
			if(GetInitParameter(InitWords,"p3",false,0.0) == 1)
				P3 = true;
			
			InputDefaultInit();
		}

		public override void Update(ref double dt)
        {
            y = Input[0].Value;
            
            if (P3)
                Output[0].Value = 0.5*(y - yoo) / dt;
            else
                Output[0].Value = (y - yo) / dt;

            yoo = yo;
            yo = y;

        }

    }
	
	public class integrator : Circuit
	{
		private double I = 0.0;
		private double so = 0.0;
		
		public integrator(string[] words)
		{
			Init(words,1,1);
			
			Input.Add(new Channel("signal",null));
			Output.Add(new Channel("out",this));
			
			Console.WriteLine("Integrator {0} created.\n", Name);
		}
		
		public override void SetUp ()
		{
			InputDefaultInit();
		}
		
		public override void Update (ref double dt)
		{
			I += (so + Input[0].Value)*dt*0.5;
			Output[0].Value = I;
			
			so = Input[0].Value;
		}
		
		
	}
	
}
