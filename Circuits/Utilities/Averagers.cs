using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace vafmpro.Circuits
{
    public class runavg : Circuit
    {

        private double[] samples;
        private double sum;
        private int idx,N;
		private bool Ticked = false;
		
        public runavg(string[] words)
        {
            Init(words);

            Input.Add(new Channel("signal",null));
            Output.Add(new Channel("out", this));
			
            Console.WriteLine("Circuit {0} (Running Averager) created.\n", Name);
        }
		public override void SetUp ()
		{
			N = (int)GetInitParameter(InitWords,"samples",true); // get the amount of samples
			samples = new double[N];
			
			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}
			
            sum = 0;
            idx = 0; //index of the new element
			
			InputDefaultInit();
            
		}
		
        public override void Update(ref double dt)
        {
			
			if(Ticked){
				if(Input[1].Value <= 0) //if ticked and no tick is detected, do nothing
					return;
			}
			//if the code arrives here, the circuit is not ticked, or the tick was detected!
			
			Math.DivRem(idx, N, out idx);
			sum -= samples[idx]; //remove the idx element from the sum
			samples[idx] = Input[0].Value; //take in the new value
			sum += samples[idx]; //add the new to the sum
			Output[0].Value = sum/N;
			idx++;  

        }
    }
	
	
	/*
	public class runavgt : Circuit
    {

        private double[] samples;
        private double sum;
        private int idx,N;

        public runavgt(string[] words)
        {
            Init(words,1,1);

            Input.Add(new Channel("signal",null));
			Input.Add(new Channel("tick",null));
            Output.Add(new Channel("out", this));
			
			N = (int)GetInitParameter(words,"samples",true);
			
            sum = 0;
            idx = 0; //index of the new element

            samples = new double[N];
			
            Console.WriteLine("Circuit {0} (Ticked Running Averager) created.\n", Name);
        }

        public override void Update(ref double dt)
        {
			if(Input[1].Value <= 0)
				return;
			
			Math.DivRem(idx, N, out idx);
            sum -= samples[idx]; //remove the idx element from the sum
			samples[idx] = Input[0].Value; //take in the new value
			sum += samples[idx]; //add the new to the sum
			Output[0].Value = sum/N;
			idx++;           

        }
    }
    */

    public class sampler : Circuit
    {

        private double[] samples;
        private double sum;
        private int idx,N;

        public sampler(string[] words)
        {
            Init(words,1,1);

            Input.Add(new Channel("signal",null));
            Output.Add(new Channel("out", this));
			
			double rate = GetInitParameter(InitWords,"rate",true);
			N = (int)Math.Floor(1.0/(Program.mytimer.dt*rate));
			samples = new double[N];
			
            sum = 0;
            idx = 0; //index of the new element
			
            Console.WriteLine("Circuit {0} (Sampler ({1}p) created.\n", Name,N);
        }

        public override void Update(ref double dt)
        {
			Math.DivRem(idx, N, out idx);
            sum -= samples[idx]; //remove the idx element from the sum
			samples[idx] = Input[0].Value; //take in the new value
			sum += samples[idx]; //add the new to the sum
			
			if(idx == 0)
				Output[0].Value = sum/N;
			
			idx++;           

        }
    }
	

}
