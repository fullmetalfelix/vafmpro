using System;
using System.Collections.Generic;
using vafmpro.AbsCircuits;

//TODO: manual
namespace vafmpro.Circuits
{
	//uniform random numbers generator
	public class random : absrand
	{
		protected double min,max;
		
		public random(string[] words)
		{
			Init(words,1,1);
			
			Output.Add(new Channel("out",this));
			Console.WriteLine("Random number generator {0} created.\n", Name);
		}
		
		public override void SetUp ()
		{
			RandomSetup();
			
			min = GetInitParameter(InitWords, "min",false,0.0);
			max = GetInitParameter(InitWords, "max",false,1.0);
			
			Seed();
		}
		
		//fills the buffer with 
		protected override void Seed ()
		{
			for(int i=0;i<N;i++)
				Buffer[i] = rnd.NextDouble()*(max-min)+min;
		}
		
	}
	
	public class randomg : absrand
	{
		protected double A,sigma,mean, tmp, tmp2;
		
		public randomg(string[] words)
		{
			Init(words,1,1);
			
			Output.Add(new Channel("out",this));
			Console.WriteLine("Gaussian Random number generator {0} created.\n", Name);
		}
		
		public override void SetUp ()
		{
			RandomSetup();
			
			A = GetInitParameter(InitWords, "A",false,1.0);
			sigma = GetInitParameter(InitWords, "sigma",true);
			mean = GetInitParameter(InitWords, "mean",false,0.0);
			
			Seed();
		}
		
		//fills the buffer with 
		protected override void Seed()
		{
			for(int i=0;i<N;i+=2)
			{
				tmp = rnd.NextDouble();
				tmp2= rnd.NextDouble();
				
				Buffer[i] = Math.Sqrt(-2.0 * Math.Log(tmp)) * Math.Sin(2.0 * Math.PI * tmp2);
				Buffer[i+1] = Math.Sqrt(-2.0 * Math.Log(tmp2)) * Math.Sin(2.0 * Math.PI * tmp);
				Buffer[i] = (mean + Buffer[i]*sigma)*A;
				Buffer[i+1] = (mean + Buffer[i+1]*sigma)*A;
			}

		}
		
				
		
	}
	
	
	public class perlin1D : Circuit
	{
		
		protected int Octaves, SeedVal;
		protected double Amplitude, Persist; //perlin parameters
		
		protected double t, tnorm, toct, TimeLength; //how long (in time) the buffer is
		
		protected Random rnd;
		protected List<double[]> NoiseMatrix;
		protected double noise, pest;
		
		
		public perlin1D(string[] words)
		{
			Init(words);
			t = 0.0;
			
			Output.Add(new Channel("out",this));
			
			InputDefaultInit();
		}
		
		public override void SetUp ()
		{
			Octaves = (int)GetInitParameter(InitWords,"oct",true);
			Amplitude = GetInitParameter(InitWords,"amp",false,1.0);
			Persist = GetInitParameter(InitWords,"persist",false,0.5);
			TimeLength = GetInitParameter(InitWords,"timelen",true);
			
			rnd = new Random();
			SeedVal = (int)GetInitParameter(InitWords,"seed",false,-1.0);
			if(SeedVal>=0)
				rnd = new Random(SeedVal);
			
			//generate the matrix
			NoiseMatrix = new List<double[]>(Octaves);
			for(int i=0;i<Octaves;i++){
				NoiseMatrix.Add(new double[(1<<i) + 1]);
				for(int k=0;k<(1<<i) + 1; k++)
					NoiseMatrix[i][k] = (2*rnd.NextDouble()-1)*Amplitude;
				
			}
			
		}
		protected void ReFill()
		{
			
			for(int i=0;i<Octaves;i++){
				NoiseMatrix[i][0] = NoiseMatrix[i][(1<<i)];
				for(int k=1;k<(1<<i) + 1; k++)
					NoiseMatrix[i][k] = (2*rnd.NextDouble()-1)*Amplitude;
				
			}
			
		}
		
		
		public override void Update (ref double dt)
		{
			tnorm = t/TimeLength;
			noise = 0.0;
			pest = 1.0;
			
			for(int i=0;i<Octaves;i++)
			{
				toct = Math.Floor(tnorm * (1<<i));
				int oIdx = (int)Math.Floor(tnorm * (1<<i));
				toct = tnorm - (double)oIdx / (1<<i);
				toct *= (1<<i);
				//Console.WriteLine("  oct {0}: index {1}, toct={2}  (dt={3})",i,oIdx,toct,(1<<i));
				
				noise += (NoiseMatrix[i][oIdx]*(1.0-toct) + toct*NoiseMatrix[i][oIdx+1])*pest;
				pest*=Persist;
			}
			
			Output[0].Value = noise;
			
			t+=dt;
			if(t>=TimeLength){
				t-=TimeLength;	
				ReFill();
			}
		}
		
		
	}
	
	
}

