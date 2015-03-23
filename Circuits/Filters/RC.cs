using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace vafmpro.Circuits
{
	
    public class PLP : Circuit
    {
        private int Order;
        private double Fcutoff,a;

        private double[] filters;
        private double[] folds;

        public PLP(string[] words)
        {
            Init(words);

			Input.Add(new Channel("signal",null));
			Output.Add(new Channel("out", this));

            Console.WriteLine("Circuit {0} (RC) created.\n", Name);
        }
		public override void SetUp ()
		{
			Order = (int)GetInitParameter(InitWords,"order",true);
			Fcutoff = GetInitParameter(InitWords,"fc",true);
			Fcutoff = 1.0/(2.0*Math.PI*Fcutoff);
			a = Program.mytimer.dt / (Fcutoff + Program.mytimer.dt);
			
            filters = new double[Order+1];
            folds = new double[Order];
			
			//reset the values
			for (int i = 0; i < Order; i++){
				filters[i+1] = 0.0;
				folds[i] = 0.0;
			}
			
			InputDefaultInit();
		}


        
       public override void Update(ref double dt)
        {
            //a = dt / (Fcutoff + dt);

            filters[0] = Input[0].Value;

            for (int i = 0; i < Order; i++){
                filters[i + 1] = folds[i] + a * (filters[i] - folds[i]);
                folds[i] = filters[i + 1];
            }

            Output[0].Value = filters[Order];

        }
    }

    public class PHP : Circuit
    {
        private int Order;
        private double Fcutoff,a;

        private double[] filters;
        private double[] folds;

        public PHP(string[] words, int ord, double fcut)
        {
            Init(words);

			Input.Add(new Channel("signal",null));
            Output.Add(new Channel("out", this));

            Console.WriteLine("Circuit {0} (RC) created.\n", Name);
        }

		public override void SetUp ()
		{
			Order = (int)GetInitParameter(InitWords,"order",true);
			Fcutoff = GetInitParameter(InitWords,"fc",true);
			Fcutoff = 1.0/(2.0*Math.PI*Fcutoff);
			a = Fcutoff / (Fcutoff + Program.mytimer.dt);
			
            filters = new double[Order + 1];
            folds = new double[Order + 1];

            //reset the values
            for (int i = 0; i < Order; i++){
                filters[i + 1] = 0.0;
                folds[i] = 0.0;
            }
			
			InputDefaultInit();
		}

        public override void Update(ref double dt)
        {
            filters[0] = Input[0].Value; //filter0 is the signal
            //for i from 1 to n
            //y[i] := α * (y[i-1] + x[i] - x[i-1])

            for (int i = 1; i < Order+1; i++)
                filters[i] = a * (folds[i] + filters[i - 1] - folds[i - 1]);

            for (int i = 0; i < Order + 1; i++)
                folds[i] = filters[i];

            Output[0].Value = filters[Order];

        }
    }

}
