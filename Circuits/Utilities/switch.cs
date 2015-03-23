using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;

namespace vafmpro.Circuits
{
    public class Switch : Circuit
    {
	
		private int Factors = 2;
		private int q=0;
		
        //this filter requires that the timestep was defined before!
        public Switch(string[] words)
        {
            Init(words);
			Factors = q;
			Factors = (int)GetInitParameter(words,"factors",false,2.0);
			
			Input.Add(new Channel("switch", null)); //input channel 0 is the switch
			for(int i=1;i<=Factors;i++)
				Input.Add(new Channel("in"+i.ToString(),null));
			
            Output.Add(new Channel("out", this));
			
			Console.WriteLine("Circuit {0} (Switch) created.\n", Name);
		}
		public override void SetUp ()
		{
			InputDefaultInit();
		}


        public override void Update(ref double dt)
        {
			//port = Math.DivRem( Convert.ToInt32(Input[0].Value)+1, Factors, out q );
			q=(int)Math.Abs(Convert.ToInt32(Input[0].Value) % Factors)+1;
			//Console.WriteLine("inp={0} -> {1}  port={2}",Input[0].Value,Convert.ToInt32(Input[0].Value),q);
			Output[0].Value = Input[q].Value;
			
        }
    }
	
}