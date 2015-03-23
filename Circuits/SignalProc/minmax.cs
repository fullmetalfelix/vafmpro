using System;
using vafmpro.AbsCircuits;


namespace vafmpro.Circuits
{
	
	public class minmax : FlipFlop
	{
		protected double min = 0.0, max = 0.0;
		
		
		public minmax (string[] words)
		{
			Init(words,2,4);
			
			Console.WriteLine("Circuit {0} (max/min) created.\n", Name);
		}
		public override void SetUp ()
		{
			Input.Add(new Channel("clock",null)); //the clock input
			Input.Add(new Channel("signal",null));
			
			Output.Add(new Channel("min",this));
			Output.Add(new Channel("max",this));
			Output.Add(new Channel("amp",this));
			Output.Add(new Channel("off",this));
			
			InputDefaultInit();
		}
		public override void Update (ref double dt)
		{
			
			if(Input[1].Value > max)
				max = Input[1].Value;
			if(Input[1].Value < min)
				min = Input[1].Value;
			
			GetClockFront(); //get the clock front
			if(!Clock)       //do nothing if the clock has no front
				return;
			
			Output[0].Value = min;
			Output[1].Value = max;
			Output[2].Value = 0.5*(max-min);
			Output[3].Value = 0.5*(max+min);
			max = double.MinValue;
			min = double.MaxValue;
		}
	}
	
	
}

