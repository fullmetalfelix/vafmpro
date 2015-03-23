using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;

namespace vafmpro.Circuits
{
    public class PI : Circuit
    {

        private double integral = 0.0;
        private double oldIntIn = 0.0;
		private double delta;
		
        public PI(string[] words)
        {
            Init(words,4,1);
			
			Input.Add(new Channel("signal",null)); //0
            Input.Add(new Channel("set", null));    //1
            Input.Add(new Channel("KP", null));     //2
            Input.Add(new Channel("KI", null));     //3

            Output.Add(new Channel("out", this));   //0
			
			Console.WriteLine("Circuit {0} (PI) created.\n", Name);
		}
		public override void SetUp ()
		{
			InputDefaultInit();           
        }

       public override void Update(ref double dt)
        {

            delta = Input[1].Value - Input[0].Value;
            integral += 0.5*(oldIntIn + Input[3].Value * delta) * dt;
            Output[0].Value = delta * Input[2].Value + integral;

            oldIntIn = Input[3].Value * delta;
        }
    }

    public class PID : Circuit
    {

        private double integral = 0.0;
        private double oldIntIn = 0.0;
        private double oldDelta = 0.0;
        private double delta;
		
        public PID(string[] words)
        {
            Init(words,5,1);

            Input.Add(new Channel("signal",null)); //0
            Input.Add(new Channel("set", null));    //1
            Input.Add(new Channel("KP", null));     //2
            Input.Add(new Channel("KI", null));     //3
            Input.Add(new Channel("KD", null));     //4

            Output.Add(new Channel("out", this));   //0

            Console.WriteLine("Circuit {0} (PID) created.\n", Name);
        }
		public override void SetUp ()
		{
			InputDefaultInit();
		}

        public override void Update(ref double dt)
        {
			
            delta = Input[1].Value - Input[0].Value;
            integral += 0.5 * (oldIntIn + Input[3].Value * delta) * dt;
            Output[0].Value = delta * Input[2].Value + integral + Input[4].Value *(delta-oldDelta)/dt;

            oldIntIn = Input[3].Value * delta;
            oldDelta = delta;

        }
    }

}
