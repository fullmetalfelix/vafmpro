using System;
using vafmpro.AbsCircuits;


namespace vafmpro.Circuits
{
    public class vco : Circuit
    {
        private double phase = 0.0;

        public vco(string[] words)
        {
            Init(words);

            Input.Add(new Channel("df", null));
            Input.Add(new Channel("f0", null));

            Output.Add(new Channel("sin", this));
            Output.Add(new Channel("cos", this));
			
            Console.WriteLine("Circuit {0} (VCO) created.\n", Name);
        }

		public override void SetUp ()
		{
			InputDefaultInit();
		}

        public override void Update(ref double dt)
        {

            phase += 2.0*Math.PI*(Input[0].Value + Input[1].Value) * dt;
            Output[0].Value = -Math.Sin(phase);
            Output[1].Value = Math.Cos(phase);
            if (phase > 2.0 * Math.PI)
                phase -= 2.0 * Math.PI;

        }


    }
}
