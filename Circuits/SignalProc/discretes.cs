using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;

namespace vafmpro.Circuits
{



    public class phasor : Circuit
    {
		private double S1o,S2o;
		private bool S1,S2;
        private double delay = 0;
        private int state = 0;
        //state 0: waiting for signal1 to get true
        //state 1: signal1 arrived, waiting for signal2 to get true

        public phasor(string[] words)
        {
            Init(words);
            state = 0;

            Console.WriteLine(" Circuit {0} (Phase Detector) created.\n", Name);
        }
		
		public override void SetUp ()
		{
            Input.Add(new Channel("signal1",null));
            Input.Add(new Channel("signal2", this));

            Output.Add(new Channel("tick",this));
            Output.Add(new Channel("delay",this));
			
			InputDefaultInit();
		}


        public override void Update(ref double dt)
        {
    
            delay += dt;
            Output[0].Value = 0;
            //Output[1].Value = 0;
			
			S1 = (Input[0].Value > 0.0 && S1o <= 0.0);
			S2 = (Input[1].Value > 0.0 && S2o <= 0.0);
			
            //if the trigger arrived...
            if ((state == 0) && S1)
            {
                delay = 0; //reset the delay
                state = 1; //goto state 1
            }
            //if the countercheck arrived...
            if ((state == 1) && S2)
            {
                Output[0].Value = 1; //generate the tick
                Output[1].Value = delay;//-2*dt; //put the delay in the output
                delay = 0;
                state = 0;
            }

			S1o=Input[0].Value;
			S2o=Input[1].Value;

        }


    }
	
    public class digiter : Circuit
    {

        public digiter(string[] words)
        {
            Init(words);
	            
			Console.WriteLine(" Circuit {0} (Digitizer) created.\n", Name);
        }
		public override void SetUp ()
		{
			Input.Add(new Channel("signal",null));
            Output.Add(new Channel("out",this));
			InputDefaultInit();
		}

        public override void Update(ref double dt)
        {
			Output[0].Value = (Input[0].Value > 0.0)? 1.0:0.0;
        }


    }

	

	
	
	
		
}