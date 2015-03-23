using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;


namespace vafmpro.Circuits
{

    public class waver : Circuit
    {
		
        private double phase = 0.0;
		private double w;
		
        public waver(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (waver) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("amp",null));
            Input.Add(new Channel("freq", null)); 
			Input.Add(new Channel("off",null));
			Input.Add(new Channel("phase",null));
			
            Output.Add(new Channel("sin",this));
            Output.Add(new Channel("cos",this));

			InputDefaultInit();
            
        }


        public override void Update(ref double dt)
        {

            w = 2.0 * Math.PI * Input[1].Value;
			phase += w*dt;
			if (phase > 2.0 * Math.PI)
				phase -= 2.0 * Math.PI;
			
			Output[0].Value = Input[0].Value*Math.Sin(phase + Input[3].Value) + Input[2].Value;
            Output[1].Value = Input[0].Value*Math.Cos(phase + Input[3].Value) + Input[2].Value;

        }

    }
    public class waversaw : Circuit
    {
		
        private double phase = 0.0;
		private double w = 0.0;
		//private int step = 0;
		
        public waversaw(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (waver saw form) created.\n", Name);
		}
		public override void SetUp ()
		{
			
            Input.Add(new Channel("amp",null));
            Input.Add(new Channel("freq", null)); 
			Input.Add(new Channel("off",null));
			Input.Add(new Channel("phase",null));
			
            Output.Add(new Channel("out",this));
            //Output.Add(new Channel("cos",this));

			InputDefaultInit();
			
			
        }

        public override void Update(ref double dt)
        {

			phase += Input[1].Value*dt;
			w = phase + Input[3].Value;
			
			if (phase >= 1.0)
				phase -= 1.0;
			
            //Output[0].Value = 2.0*Input[0].Value*(phase+Input[3].Value-0.5) + Input[2].Value;
            Output[0].Value = 2.0*Input[0].Value*(w-Math.Floor(w+0.5))+Input[2].Value;

            
        }

    }
	
	public class fourisaw : Circuit
    {
		
        private double phase = 0.0;
		private double w = 0.0;
		private int m1k = -1;
		private int Nwaves = 1;
		
        public fourisaw(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (Fourier saw form) created.\n", Name);
		}
		public override void SetUp ()
		{
			
            Input.Add(new Channel("amp",null));
            Input.Add(new Channel("freq", null)); 
			Input.Add(new Channel("off",null));
			Input.Add(new Channel("phase",null));
			
            Output.Add(new Channel("out",this));
            
			Nwaves = (int)GetInitParameter(InitWords,"nwaves",true);
			
			InputDefaultInit();
			
			
        }

        public override void Update(ref double dt)
        {

			phase += 2.0*Math.PI*Input[1].Value*dt;
			w=0.0; m1k=1;
			for(int i=1;i<=Nwaves;i++){
				w += Math.Sin(phase*i + Input[3].Value)*m1k/i;
				m1k *= -1;
			}
			if(phase >= 2.0 * Math.PI)
				phase -= 2.0 * Math.PI;
			
            Output[0].Value = Input[0].Value*w*2.0/Math.PI + Input[2].Value;

            
        }

    }
	
	
	public class springdamp : Circuit
	{
		
		private double w0, x0;
		private double x,xo,xoo,v,a;
			
        public springdamp(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (springdamper) created.\n", Name);
		}
		public override void SetUp ()
		{
            Input.Add(new Channel("x0",null)); //the reference point
            Input.Add(new Channel("f0", null)); 
			Input.Add(new Channel("eta",null));
			
            Output.Add(new Channel("x",this));

			InputDefaultInit();
			
			x=0; xo=0; xoo=0; v = 0; a = 0;
        }


        public override void Update(ref double dt)
        {
			
			w0 = 2.0 * Math.PI * Input[1].Value;
			//w0 = w0 * dt;
			
			x0 = Input[0].Value;
			
			x = x + v*dt + 0.5*a*dt*dt;
			v = v*(1.0-0.5*dt*Input[2].Value) + 0.5*a*dt;
			a = -w0*w0*(x-x0);
			v = v*(1.0-0.5*dt*Input[2].Value) + 0.5*a*dt;
			
			Output[0].Value = x;
			
			//x = 2*xo + xoo*(0.5*Input[2].Value*dt-1.0) + w0*w0*Input[0].Value*x0;
			//Output[0].Value = x / (1+w0*w0+0.5*Input[2].Value*dt);
			
			
			
			xoo = xo;
			xo = x;
        }

    }
	
	//series of spring/damper
	public class springdamps : Circuit
	{
		
		private double x0, tmp;
		private double[] x,v,a;
		private int n;
		
        public springdamps(string[] words)
        {
            Init(words);
			Console.WriteLine("Circuit {0} (coupled springdamper series) created.\n", Name);
		}
		public override void SetUp ()
		{
			
			Input.Add(new Channel("x0",null)); //the reference point
			
			n = (int)GetInitParameter(InitWords,"n",true);
			
			for(int i=0; i<n; i++) {
				Input.Add(new Channel("f"+i.ToString(), null)); 
			}
			for(int i=0; i<n; i++) {
				Input.Add(new Channel("eta"+i.ToString(), null)); 
			}
			for(int i=0; i<n; i++) {
				 Output.Add(new Channel("x"+(i+1).ToString(),this));
			}
           
			x = new double[n+1];v = new double[n+1];a = new double[n+1];
			
			
			InputDefaultInit();
			
			//x=0; xo=0; xoo=0; v = 0; a = 0;
        }


        public override void Update(ref double dt)
        {
			
			x[0] = Input[0].Value;
			
			//position & speed update
			for(int i=1; i<=n; i++) {
				x[i] = x[i] + v[i]*dt + 0.5*a[i]*dt*dt;
				Output[i-1].Value = x[i];
				
				tmp = Input[i+n].Value;
				if(i<n) tmp += Input[i+n+1].Value;
				
				v[i] = v[i]*(1.0-0.5*dt*tmp) + 0.5*a[i]*dt;
			}
			
			#region "accel"
			for(int i=1; i<=n; i++) {
				//spring with the previous
				tmp = 2.0 * Math.PI*Input[i].Value;
				tmp *= tmp;
				a[i] = -tmp*(x[i]-x[i-1]);
				
				//spring with the next
				if(i<n) {
					
					tmp = 2.0 * Math.PI*Input[i+1].Value;
					tmp *= tmp;
					a[i] += tmp*(x[i+1]-x[i]);
					
				}
				
			}
			#endregion
			
			for(int i=1; i<=n; i++) {
				tmp = Input[i+n].Value;
				if(i<n) tmp += Input[i+n+1].Value;
				v[i] = v[i]*(1.0-0.5*dt*tmp) + 0.5*a[i]*dt;
			}	
			
        }

    }

	
	#region "Random number generators
	
/*
	public class random : Circuit
    {
		
        private MersenneTwister rnd;
        private double Min,Max;
		
		
        public random(string name, double min, double max)
        {
            Init(words);
			rnd = new MersenneTwister();
			Min=min;Max=max;
			
			Input.Add(new Channel("tick",null));
			
            Output.Add(new Channel("out", this));

            Console.WriteLine("Circuit {0} (Random Number) created\n.", Name);
        }

       public override void Update(ref double dt)
        {
			if(Input[0].Value>0)
				Output[0].Value = rnd.NextDouble()*(Max-Min)+Min;
        }
    }
	public class irandom : Circuit
    {
		
        private MersenneTwister rnd;
        private int Min,Max;

		
        public irandom(string name, int min, int max)
        {
            Init(words);
			rnd = new MersenneTwister();
			Min=min;Max=max;
			
			Input.Add(new Channel("tick",null));
			
            Output.Add(new Channel("out", this));

            Console.WriteLine("Circuit {0} (Integer Random Number) created\n.", Name);
        }

       public override void Update(ref double dt)
        {
			if(Input[0].Value>0)
				Output[0].Value = rnd.Next(Min,Max);
        }
    }	
	public class grandom : Circuit
    {
		
        private MersenneTwister rnd1;
		private double w, x, y;
		private double Mean, Stdv;
		private double cache,result;
		private bool cached = false;
		
        public grandom(string name, double mean, double stdv)
        {
            Init(words);

			rnd1 = new MersenneTwister( );
			Mean = mean;
			Stdv = stdv;
			
			Input.Add(new Channel("tick",null));
			
            Output.Add(new Channel("norm",this));
			Output.Add(new Channel("unif",this));

            Console.WriteLine("Circuit {0} (Gaussian Random Number) created\n.", Name);
        }

	
		public override void Update(ref double dt)
		{
			if(Input[0].Value>0){
				
				if(!cached){
					
					w = 0;
					while( (w == 0) || (w >= 1) ) {
						x = rnd1.NextDouble()*2-1;
						y = rnd1.NextDouble()*2-1;
						w = x*x + y*y;
					}
					w = Math.Sqrt(-2*Math.Log(w)/w);
					cache = x*w*Stdv + Mean;
					result = y*w*Stdv + Mean;
					Output[1].Value = x;
					cached = true;
				}
				else{
					result = cache;
					cached = false;
					Output[1].Value = y;
				}
				
				Output[0].Value = result;
				
				
			}
			
        }
		
	}	
	
	*/
	
	#endregion
	
}
