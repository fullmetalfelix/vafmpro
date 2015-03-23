using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;



namespace vafmpro.Circuits
{

	
	public class flip : Circuit
    {

		private bool signal = false;
        private bool signalO = false;
        
        public flip(string[] words)
        {
            Init(words);
			
            Console.WriteLine("Circuit {0} (flip) created.\n", Name);

        }
		public override void SetUp ()
		{
			Input.Add(new Channel("signal", null));
            Output.Add(new Channel("tick",this));  //0: gets to 1 if the digital signal raises
			
			InputDefaultInit();
		}


       public override void Update(ref double dt)
        {
			signal = Input[0].Value > 0;
			
			Output[0].Value = (signal && !signalO)? 1.0 : 0.0;
						
			signalO = signal;
		}
		
	}
	
	
	public class flipflopSR : FlipFlop
	{
		private bool Si, Ri;
		private bool S, R;
		private bool SO = false, RO = false;
		
		public flipflopSR(string[] words){
			
			Init(words);
			
			Console.WriteLine("Circuit {0} (SR flipflop) created.\n", Name);
		}
		public override void SetUp ()
		{
			base.SetUp();
			
			//input 0 is the clock
			Input.Add(new Channel("S",null));
			Input.Add(new Channel("R",null));
			
			InputDefaultInit();
		}

		public override void Update (ref double dt)
		{
			//get the input values
			S = Si = Input[1].Value > 0.0;
			R = Ri = Input[2].Value > 0.0;
			
			if(Fronted){
				S = GetFront(Si,ref SO);
				R = GetFront(Ri,ref RO);
			}
			
			//check for reset
			if(R)
				State = 0;
			
			GetClockFront(); //get the clock front
			if(!Clock)       //do nothing if the clock has no front
				return;
			
			//activate if the clock has a front AND S=1 AND R=0
			if(S && !R)
				State = 1;
			
		}
		
		
	}
	
	public class flipflopJK : FlipFlop
	{
		
		private bool Ji, Ki;
		private bool J, K;
		private bool JO = false, KO = false;
		
		public flipflopJK(string[] words){
			
			Init(words);
			
			Console.WriteLine("Circuit {0} (JK flipflop) created.\n", Name);
		}
		public override void SetUp ()
		{
			base.SetUp();
			
			Input.Add(new Channel("J",null));
			Input.Add(new Channel("K",null));
			
			InputDefaultInit();
		}

		public override void Update (ref double dt)
		{
			GetClockFront(); //get the clock front
			if(!Clock)       //do nothing if the clock has no front
				return;
			
			J = Ji = Input[1].Value > 0.0;
			K = Ki = Input[2].Value > 0.0;
			
			if(Fronted){
				J = GetFront(Ji,ref JO);
				K = GetFront(Ki,ref KO);
			}
			
			if(J && State==0){
				State=1;
				return;
			}
			if(K && State==1){
				State=0;
				return;
			}
			
		}
		
		
	}
	
	public class flipflopD : FlipFlop
	{
		
		private bool Di;
		private bool D;
		private bool DO = false;
		
		public flipflopD(string[] words){
			
			Init(words);
			
			Console.WriteLine("Circuit {0} (D flipflop) created.\n", Name);
		}
		public override void SetUp ()
		{
			base.SetUp();
			
			Input.Add(new Channel("D",null));
			
			InputDefaultInit();
		}

		public override void Update (ref double dt)
		{
			GetClockFront(); //get the clock front
			if(!Clock)       //do nothing if the clock has no front
				return;
			
			D = Di = Input[1].Value > 0.0;
			
			if(Fronted){
				D = GetFront(Di,ref DO);
			}
			
			if(D){
				Switch();
				return;
			}
			
		}
		
	}
	
	public class flipflopDR : FlipFlop
	{
		
		private bool Di, Ri;
		private bool D, R;
		private bool DO = false, RO = false;
		
		public flipflopDR(string[] words){
			
			Init(words);
			
			Console.WriteLine("Circuit {0} (DR flipflop) created.\n", Name);
		}
		public override void SetUp ()
		{
			base.SetUp();
			
			Input.Add(new Channel("D",null));
			Input.Add(new Channel("R",null));
			
			InputDefaultInit();
		}

		public override void Update (ref double dt)
		{
			
			D = Di = Input[1].Value > 0.0;
			R = Ri = Input[2].Value > 0.0;
			
			if(Fronted){
				D = GetFront(Di,ref DO);
				R = GetFront(Ri,ref RO);
			}
			if(R){ //if the reset is high, reset the state and quit
				State = 0;
				return;
			}
			
			GetClockFront(); //get the clock front
			if(!Clock)       //do nothing if the clock has no front
				return;
			
			if(D)
				Switch();
			
		}
		
		
	}
	

	
	
	
}