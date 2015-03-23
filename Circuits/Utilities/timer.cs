using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;


namespace vafmpro.Circuits
{

    public class timer : Circuit
    {

        public double dt;
        private long idt;

        public timer(string[] words)
        {
            Init(words);
						
            Console.WriteLine("Circuit {0} (timer) created.\n", Name);
        }
		public override void SetUp ()
		{
			dt = GetInitParameter(InitWords,"dt",true);
            idt = 0;
			Output.Add(new Channel("t",this));
		}

		public override void Update(ref double dt)
        {
            idt++;
            Output[0].Value = idt*dt;
        }
    }

}
