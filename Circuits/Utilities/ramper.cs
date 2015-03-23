using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace vafmpro.Circuits
{
	
    public class ramper : Circuit
    {

        private double speed, val;
        private double Min, Max;

        public ramper(string[] words)
        {
            Init(words,1,1);
			
			Input.Add(new Channel("speed", null));
			//Input.Add(new Channel("reset", null));
			
            Output.Add(new Channel("out", this));
			Console.WriteLine("Circuit {0} (Ramper) created.\n", Name);
		}
		public override void SetUp ()
		{
			Max = GetInitParameter(InitWords,"max",true);
            Min = GetInitParameter(InitWords,"min",true);
            val = Min;

			InputDefaultInit();
			
			
        }


       public override void Update(ref double dt)
        {
			speed = Input[0].Value;
			if(speed == 0){
				val = Min;
				Output[0].Value = val;
				return;
			}
			
			/*if(Input[1].Value >0) {
				val = (speed>0)? Min : Max;
				Output[0].Value = val;
				return;
			}*/
			
            if ((speed > 0 && val >= Max) || (speed < 0 && val <= Min) || (speed == 0))
                return;

            val += speed * dt;
            Output[0].Value = val;

        }
    }
}
