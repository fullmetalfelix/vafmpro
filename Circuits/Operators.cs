using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;

namespace vafmpro.Circuits
{
	
    public class Const : Circuit
    {
        public Const(string[] words)
        {
            Init(words);
            Updated = true; //constants are always updated!
            
			Output.Add(new Channel("value", this));
			
            Output[0].Value = GetInitParameter(words,"value",true);

            Console.WriteLine("Const {0} created ({1}).\n", Name,Output[0].Value);

        }

		
       public override void Update(ref double dt)
        {

        }
    }

	
    #region "Binary Operators - Arithmetics"
	
	public class opPro : Circuit
	{
		
		
		public opPro(string[] words)
		{
			Init(words);
			
		}
		public override void SetUp ()
		{
			base.SetUp ();
			
			InputDefaultInit();
		}
		
		public override void Update (ref double dt)
		{
			throw new NotImplementedException ();
		}
		
	}
	
    public class opAdd : Circuit
    {
		private int Factors = 2;
		private bool Ticked = false;
		private double result;
		
        public opAdd(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (Adder {1} factors) created.\n", Name,Factors);
		}
		
		public override void SetUp ()
		{
			Factors = (int)Math.Floor(GetInitParameter(InitWords,"factors",false,2.0));
			
			for(int i=1;i<=Factors;i++)
				Input.Add(new Channel("in"+i.ToString(),null));
			Output.Add(new Channel("out", this));
			
			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}
			
			
			InputDefaultInit();
			
			
        }

		
        public override void Update(ref double dt)
        {
			if(Ticked){
				if(Input[Factors].Value <= 0.0)
					return;
			}
			//if the code is here, the circuit is not ticked, or itz ticked and there was a tick
			result = 0.0;
			for(int i=0;i<Factors;i++)
				result += Input[i].Value;			
			Output[0].Value = result;
        }
    }
	public class opMulAdd : Circuit
    {
		private int Factors = 2;
		private bool Ticked = false;
		private double result;
		
        public opMulAdd(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (MulAdder {1} factors) created.\n", Name,Factors);
		}
		
		public override void SetUp ()
		{
			Factors = (int)Math.Floor(GetInitParameter(InitWords,"factors",false,2.0));
			
			for(int i=1;i<=Factors;i++) {
				Input.Add(new Channel("ina"+i.ToString(),null));
				Input.Add(new Channel("inb"+i.ToString(),null));
			}
			Output.Add(new Channel("out", this));
			
			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}
			
			
			InputDefaultInit();
			
			
        }

		
        public override void Update(ref double dt)
        {
			if(Ticked){
				if(Input[Factors].Value <= 0.0)
					return;
			}
			//if the code is here, the circuit is not ticked, or itz ticked and there was a tick
			result = 0.0;
			for(int i=0;i<Factors*2;i+=2) {
				result += Input[i].Value*Input[i+1].Value;
				//Console.WriteLine("muladd {0}",Input[i].Value);
			}
			//Console.WriteLine("muladd {0} {1}  {2}",Input[6].Value,Input[7].Value,result);
			Output[0].Value = result;
			
        }
    }
    
	public class opSub : Circuit
    {
		private bool Ticked = false;
		
        public opSub(string[] words)
        {
			Init(words);
			Console.WriteLine("Circuit {0} (Subtractor) created.\n", Name);
            
        }
		public override void SetUp ()
		{
			Input.Add(new Channel("in1", null));
			Input.Add(new Channel("in2", null));

			Output.Add(new Channel("out", this));

			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}

			InputDefaultInit();
		}
		
        public override void Update(ref double dt)
        {
			if(Ticked){
				if(Input[2].Value <= 0.0)
					return;
			}
			Output[0].Value = Input[0].Value - Input[1].Value;
        }
    }
    public class opMul : Circuit
    {
		private int Factors = 2;
		private bool Ticked = false;
		private double result;
		
        public opMul(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (Multiplier {1} factors) created.\n", Name,Factors);
		}
		public override void SetUp ()
		{
			Factors = (int)Math.Floor(GetInitParameter(InitWords,"factors",false,2.0));
			
			for(int i=1;i<=Factors;i++)
				Input.Add(new Channel("in"+i.ToString(),null));
            Output.Add(new Channel("out", this));

			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}

			InputDefaultInit();
			
            
        }

		
        public override void Update(ref double dt)
        {
			if(Ticked) {
				if(Input[Factors].Value <= 0.0)
					return;
			}
			
			result = 1;
			for(int i=0;i<Factors;i++)
				result *= Input[i].Value;
			Output[0].Value = result;
			
        }
    }
    public class opDiv : Circuit
    {
		private double result;
		private bool Ticked = false;
		
        public opDiv(string[] words)
        {
			Init(words);
			Console.WriteLine("Circuit {0} (Divider) created.\n", Name);
		}
		
		public override void SetUp ()
		{
			Input.Add(new Channel("in1", null));
			Input.Add(new Channel("in2", null));

            Output.Add(new Channel("out", this));

			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}

			InputDefaultInit();
			
            
        }

		
        public override void Update(ref double dt)
        {
			if(Ticked){
				if(Input[2].Value <= 0.0)
					return;
            }
            
			result = Input[0].Value / Input[1].Value;
			if(double.IsInfinity(result) || double.IsNaN(result))
				result = 0;
			Output[0].Value = result;
            
        }
    }
	public class opPow : Circuit
    {
		private bool Ticked = false;
        public opPow(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (Pow) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in1", null));
            Input.Add(new Channel("in2", null));

            Output.Add(new Channel("out", this));

			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}

			InputDefaultInit();
			
            
        }

		
        public override void Update(ref double dt)
        {
			if(Ticked){
				if(Input[2].Value <= 0.0)
					return;
			}
			Output[0].Value = Math.Pow(Input[0].Value,Input[1].Value);
        }
    }
    #endregion
    #region "Unary Operators - Arithmetics"
    public class opAbs : Circuit
    {
		private bool Ticked = false;
		
        public opAbs(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (Abs) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in", null));
            Output.Add(new Channel("out", this));

			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}

            
        }

		
        public override void Update(ref double dt)
        {
			if(Ticked){
				if(Input[1].Value <= 0.0)
					return;
			}
			Output[0].Value = Math.Abs(Input[0].Value);
        }
    }
	public class opSqr : Circuit
    {
		private bool Ticked = false;
		 
        public opSqr(string[] words)
        {
			Init(words);
			Console.WriteLine("Circuit {0} (Sqr) created.\n", Name);
		}
		public override void SetUp ()
		{
			Input.Add(new Channel("in", null));
            Output.Add(new Channel("out", this));

			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}

            
        }

		
        public override void Update(ref double dt)
        {
			if(Ticked){
				if(Input[1].Value <= 0.0)
					return;
			}
			Output[0].Value = Math.Sqrt(Input[0].Value);
        }
    }
	public class opExp : Circuit
    {
		private bool Ticked = false;
		
        public opExp(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (Exp) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in", null));
            Output.Add(new Channel("out", this));

			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}

            
        }

		
        public override void Update(ref double dt)
        {
			if(Ticked){
				if(Input[1].Value <= 0.0)
					return;
			}
			Output[0].Value = Math.Exp(Input[0].Value);
        }
    }
	public class opLog : Circuit
    {
		private bool Ticked = false;
		
        public opLog(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (Log) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in", null));
            Output.Add(new Channel("out", this));

			if(GetInitParameter(InitWords,"ticked",false,0.0) != 0.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}

            
        }

		
        public override void Update(ref double dt)
        {
			if(Ticked){
				if(Input[1].Value <= 0.0)
					return;
			}
			Output[0].Value = Math.Log(Input[0].Value);
        }
    }
    public class opLLim : Circuit
    {

        public opLLim(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (LowLimiter) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in", null));
            Input.Add(new Channel("min", null));

            Output.Add(new Channel("out", this));

			InputDefaultInit();
			
            
        } 


        public override void Update(ref double dt)
        {
            if (Input[0].Value < Input[1].Value)
                Output[0].Value = Input[1].Value;
            else
                Output[0].Value = Input[0].Value;
        }
    }
    public class opULim : Circuit
    {

        public opULim(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (HiLimiter) created.\n", Name);
		}
		public override void SetUp ()
		{
			Input.Add(new Channel("in", null));
            Input.Add(new Channel("max", null));

            Output.Add(new Channel("out", this));

			InputDefaultInit();
			
        }

		
        public override void Update(ref double dt)
        {
            if (Input[0].Value > Input[1].Value)
                Output[0].Value = Input[1].Value;
            else
                Output[0].Value = Input[0].Value;
        }
    }
    public class opBLim : Circuit
    {

        public opBLim(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (BandLimiter) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in", null));
            Input.Add(new Channel("max", null));
            Input.Add(new Channel("min", null));

            Output.Add(new Channel("out", this));

			InputDefaultInit();
			
        }

		
        public override void Update(ref double dt)
        {
            if (Input[0].Value > Input[1].Value)
                Output[0].Value = Input[1].Value;
            else if (Input[0].Value < Input[2].Value)
                Output[0].Value = Input[2].Value;
            else
                Output[0].Value = Input[0].Value;
        }
    }
    #endregion
	
	
    #region "Logical Operators - Comparison"
    public class opGE : Circuit
    {
        //greater or equal
        public opGE(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (BOOL >=) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in1", null));
            Input.Add(new Channel("in2", null));

            Output.Add(new Channel("out", this));

			InputDefaultInit();
			
            
        }

		
        public override void Update(ref double dt)
        {
			Output[0].Value = (Input[0].Value >= Input[1].Value)? 1.0:0.0;
        }
    }
    public class opLE : Circuit
    {
        //less or equal
        public opLE(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (BOOL <=) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in1", null));
            Input.Add(new Channel("in2", null));

            Output.Add(new Channel("out", this));
			InputDefaultInit();
            
        }


        public override void Update(ref double dt)
        {
            Output[0].Value = (Input[0].Value <= Input[1].Value)? 1.0:0.0;
        }
    }
    public class opEQ : Circuit
    {
        //equal
        public opEQ(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (BOOL ==) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in1", null));
            Input.Add(new Channel("in2", null));

            Output.Add(new Channel("out", this));
			InputDefaultInit();
            
        }


        public override void Update(ref double dt)
        {
            Output[0].Value =  (Input[0].Value == Input[1].Value)? 1.0:0.0;
        }
    }
    #endregion

    #region "Logical Operators"
    public class opNOT : Circuit
    {

        public opNOT(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (NOT) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in", null));

            Output.Add(new Channel("out", this));

            
        }


        public override void Update(ref double dt)
        {
			Output[0].Value = (Input[0].Value <= 0)? 1:0;
        }
    }
    public class opAND : Circuit
    {
		private int Factors = 2;
		//private double result;
		
        public opAND(string[] words)
        {
            Init(words);
			Factors = (int)Math.Floor( GetInitParameter(words,"factors",false,2.0) );
			
			Console.WriteLine("Circuit {0} (AND {1} channels) created\n.", Name, Factors);
		}
		public override void SetUp ()
		{
			for(int i=1;i<=Factors;i++)
				Input.Add(new Channel("in"+i.ToString(),null));

            Output.Add(new Channel("out", this));

			InputDefaultInit();
			
            
        }


        public override void Update(ref double dt)
        {
			Output[0].Value = 1.0;
			for(int i=0;i<Factors;i++){
				if(Input[i].Value <= 0){
					//result = 0;
					Output[0].Value = 0.0;
					break;
				}
			}
        }
    }
    public class opOR : Circuit
    {
		private int Factors = 2;
		
        public opOR(string[] words)
        {
            Init(words);
			Factors = (int)Math.Floor( GetInitParameter(words,"factors",false,2.0) );
			
			Console.WriteLine("Circuit {0} (OR {1} channels) created\n.", Name,Factors);
		}
		public override void SetUp ()
		{
			for(int i=1;i<=Factors;i++)
				Input.Add(new Channel("in"+i.ToString(),null));

            Output.Add(new Channel("out", this));

			InputDefaultInit();
			
            
        }


        public override void Update(ref double dt)
        {
			bool o = true;
			
			for(int i=0;i<Factors;i++)
				o = o || (Input[i].Value > 0);
			
            Output[0].Value = (o)? 1:0;
			
        }
    }
    public class opXOR : Circuit
    {

        public opXOR(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (XOR) created\n.", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in1", null));
            Input.Add(new Channel("in2", null));

            Output.Add(new Channel("out", this));
			InputDefaultInit();
            
        }


        public override void Update(ref double dt)
        {
			Output[0].Value = (Input[0].Value > 0 ^ Input[1].Value > 0)? 1:0;

        }
    }
	public class opNOR : Circuit
    {

        public opNOR(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (NOR) created\n.", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("in1", null));
            Input.Add(new Channel("in2", null));

            Output.Add(new Channel("out", this));
			InputDefaultInit();
            
        }


        public override void Update(ref double dt)
        {
			Output[0].Value = (Input[0].Value > 0 || Input[1].Value > 0)? 0:1;

        }
    }
    #endregion


}
