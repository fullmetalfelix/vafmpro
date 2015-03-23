using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;

namespace vafmpro.Circuits
{
	//Resonance shear apparatus - modelled by me : )
	public class RSA : Circuit
	{
		private double M1,M2,MI;
		private double Ks, Ksz, K2;
		private double gammaCM,gammaRot, gamma2, eta, mu;
		
		private double springy; //initial y position of the springs with respect to CM
		private double springx; //initial x position of one spring with respect to CM, the other will be in -springx.
		private double fpoint;
		
		private double xcm, ycm, x2, theta;
		private double vxcm, vycm, vx2, vtheta;
		
		private double forcex, forcey, torque, force2;
		
		private double x,y, tmpfx, tmpfy;
		
		public RSA (string[] words)
		{
			Init(words);
			
			Input.Add(new Channel("exciter",null));
			Input.Add(new Channel("eta",null));
			Input.Add(new Channel("mu",null));
			Output.Add(new Channel("out", this));
			Output.Add(new Channel("xcm", this));
			Output.Add(new Channel("ycm", this));
			Output.Add(new Channel("x2", this));
			Output.Add(new Channel("theta", this));
			
		}
		
		public override void SetUp ()
		{
			M1 = GetInitParameter(InitWords,"M1",true,0.1);
			M2 = GetInitParameter(InitWords,"M2",true,0.1);
			MI = GetInitParameter(InitWords,"MI",true,0.01);

			Ks = GetInitParameter(InitWords,"Ks",true,250);
			Ksz = GetInitParameter(InitWords,"Ksz",true,1000);
			K2 = GetInitParameter(InitWords,"K2",true,500);
			
			gammaCM = GetInitParameter(InitWords,"gammaCM",true,0.001);
			gamma2 = GetInitParameter(InitWords,"gamma2",true,0.001);
			gammaRot = GetInitParameter(InitWords,"gammaRot",true,0.01);
			
			springx = GetInitParameter(InitWords,"springx",true,0.03);
			springy = GetInitParameter(InitWords,"springy",true,0.05);
			
			fpoint = GetInitParameter(InitWords,"fpoint",true,0.05);
			
			M1 = 1.0/M1; M2 = 1.0/M2; MI = 1.0/MI;
			
			//mygain = GetInitParameter(InitWords,"gain",true);
			InputDefaultInit();
			//theta = 0.01;
		}
		
		public override void Update (ref double dt)
		{
			forcex = 0; forcey = 0; torque = 0; force2 = 0;
			eta = Input[1].Value;
			mu = Input[2].Value;
			
			//update positions
			xcm += vxcm * dt;
			ycm += vycm * dt;
			theta += vtheta*dt;
			x2 += vx2 * dt;
			
			//get new forces
			torque = Input[0].Value; // get the torque from exciter signal
			//forcex = Input[0].Value; // get the shear excitation from exciter signal
			
			GetSpring();
			
			eta = -eta*(vxcm + (-fpoint)*vtheta*Math.Cos(theta)-vx2);
			mu = -mu*Math.Sign(vxcm + (-fpoint)*vtheta*Math.Cos(theta)-vx2);
			//this mu force must be capped somehow! it cannot trigger an inversion of velocity!
			//at most it will cause a halt
			
			//eta = -eta*Math.Sign(vxcm-vx2);
			//eta = -eta*vxcm;
			//eta = -K2*(xcm -fpoint*Math.Sin(theta));
			//mu = -gamma2*(vxcm + (-fpoint)*vtheta*Math.Cos(theta));
			
			forcex += eta; forcex += mu;
			force2 -= eta; force2 -= mu;
			
			torque += 0-(fpoint*Math.Cos(theta)*eta);
			torque += 0-(fpoint*Math.Cos(theta)*mu);
			
			//update speeds
			vxcm = (vxcm + forcex*dt*M1)/(1.0 + gammaCM*dt*M1);
			vycm = (vycm + forcey*dt*M1)/(1.0 + gammaCM*dt*M1);
			vtheta = (vtheta + torque*dt*MI)/(1.0 + gammaRot*dt*MI);
			vx2 = (vx2 + force2*dt*M2)/(1.0 + gamma2*dt*M2);
			
			Output[0].Value = springy*Math.Sin(theta)+xcm;
			Output[1].Value = xcm;
			Output[2].Value = ycm;
			Output[3].Value = x2;
			Output[4].Value = theta;
			
		}
		
		//compute spring forces and torques
		protected void GetSpring()
		{
			//compute the correct position
			x = springx*Math.Cos(theta)-springy*Math.Sin(theta);
			y = springx*Math.Sin(theta)+springy*Math.Cos(theta);
			x += xcm; y += ycm; //absolute position of the spring attachement point
			
			tmpfx = -Ks * (x-springx);
			tmpfy = -Ks * (y-springy);
			torque += (x-xcm)*tmpfy - (y-ycm)*tmpfx;
			forcex += tmpfx; forcey += tmpfy;
			
			//and for the second spring
			x = -springx*Math.Cos(theta)-springy*Math.Sin(theta);
			y = -springx*Math.Sin(theta)+springy*Math.Cos(theta);
			x += xcm; y += ycm; //absolute position of the spring attachement point
			
			tmpfx = -Ks * (x+springx);
			tmpfy = -Ks * (y-springy);
			torque += (x-xcm)*tmpfy - (y-ycm)*tmpfx;
			forcex += tmpfx; forcey += tmpfy;
			
			force2 = -K2 * x2; //lower spring
			
		}
		
		
	}

	public class RSA2 : Circuit
	{
		private double M1,M2,MI;
		private double Ks, Ksz, K2;
		private double gammaCM,gammaRot, gamma2, eta, mu;
		
		private double springy; //initial y position of the springs with respect to CM
		private double springx; //initial x position of one spring with respect to CM, the other will be in -springx.
		private double fpoint;
		
		private double xcm, ycm, x2, theta;
		private double vxcm, vycm, vx2, vtheta;
		
		private double forcex, forcey, torque, force2;
		
		private double x,y, tmpfx, tmpfy;
		
		public RSA2 (string[] words)
		{
			Init(words);
			
			Input.Add(new Channel("exciter",null));
			Input.Add(new Channel("eta",null));
			Input.Add(new Channel("mu",null));
			Output.Add(new Channel("out", this));
			Output.Add(new Channel("xcm", this));
			Output.Add(new Channel("ycm", this));
			Output.Add(new Channel("x2", this));
			Output.Add(new Channel("theta", this));
			
		}
		
		public override void SetUp ()
		{
			M1 = GetInitParameter(InitWords,"M1",true,0.1);
			M2 = GetInitParameter(InitWords,"M2",true,0.1);
			MI = GetInitParameter(InitWords,"MI",true,0.01);

			Ks = GetInitParameter(InitWords,"Ks",true,250);
			Ksz = GetInitParameter(InitWords,"Ksz",true,1000);
			K2 = GetInitParameter(InitWords,"K2",true,500);
			
			gammaCM = GetInitParameter(InitWords,"gammaCM",true,0.001);
			gamma2 = GetInitParameter(InitWords,"gamma2",true,0.001);
			gammaRot = GetInitParameter(InitWords,"gammaRot",true,0.01);
			
			springx = GetInitParameter(InitWords,"springx",true,0.03);
			springy = GetInitParameter(InitWords,"springy",true,0.05);
			
			fpoint = GetInitParameter(InitWords,"fpoint",true,0.05);
			
			M1 = 1.0/M1; M2 = 1.0/M2; MI = 1.0/MI;
			
			//mygain = GetInitParameter(InitWords,"gain",true);
			InputDefaultInit();
			//theta = 0.01;
		}
		
		public override void Update (ref double dt)
		{
			forcex = 0; forcey = 0; torque = 0; force2 = 0;
			eta = Input[1].Value;
			mu = Input[2].Value;
			
			//update positions
			xcm += vxcm * dt;
			ycm += vycm * dt;
			theta += vtheta*dt;
			x2 += vx2 * dt;
			
			//get new forces
			torque = Input[0].Value; // get the torque from exciter signal
			//forcex = Input[0].Value; // get the shear excitation from exciter signal
			
			GetSpring();
			
			eta = -eta*(vxcm + (-fpoint)*vtheta*Math.Cos(theta)-vx2);
			mu = -mu*Math.Sign(vxcm + (-fpoint)*vtheta*Math.Cos(theta)-vx2);
			//this mu force must be capped somehow! it cannot trigger an inversion of velocity!
			//at most it will cause a halt
			
			//eta = -eta*Math.Sign(vxcm-vx2);
			//eta = -eta*vxcm;
			//eta = -K2*(xcm -fpoint*Math.Sin(theta));
			//mu = -gamma2*(vxcm + (-fpoint)*vtheta*Math.Cos(theta));
			
			forcex += eta; forcex += mu;
			force2 -= eta; force2 -= mu;
			
			torque += 0-(fpoint*Math.Cos(theta)*eta);
			torque += 0-(fpoint*Math.Cos(theta)*mu);
			
			//update speeds
			vxcm = (vxcm + forcex*dt*M1)/(1.0 + gammaCM*dt*M1);
			vycm = (vycm + forcey*dt*M1)/(1.0 + gammaCM*dt*M1);
			vtheta = (vtheta + torque*dt*MI)/(1.0 + gammaRot*dt*MI);
			vx2 = (vx2 + force2*dt*M2)/(1.0 + gamma2*dt*M2);
			
			Output[0].Value = springy*Math.Sin(theta)+xcm;
			Output[1].Value = xcm;
			Output[2].Value = ycm;
			Output[3].Value = x2;
			Output[4].Value = theta;
			
		}
		
		//compute spring forces and torques
		protected void GetSpring()
		{
			//compute the correct position
			x = springx*Math.Cos(theta)-springy*Math.Sin(theta);
			y = springx*Math.Sin(theta)+springy*Math.Cos(theta);
			x += xcm; y += ycm; //absolute position of the spring attachement point
			
			tmpfx = -Ks * (x-springx);
			tmpfy = -Ksz * (y-springy);
			torque += (x-xcm)*tmpfy - (y-ycm)*tmpfx;
			forcex += tmpfx; forcey += tmpfy;
			
			//and for the second spring
			x = -springx*Math.Cos(theta)-springy*Math.Sin(theta);
			y = -springx*Math.Sin(theta)+springy*Math.Cos(theta);
			x += xcm; y += ycm; //absolute position of the spring attachement point
			
			tmpfx = -Ks * (x+springx);
			tmpfy = -Ksz * (y-springy);
			torque += (x-xcm)*tmpfy - (y-ycm)*tmpfx;
			forcex += tmpfx; forcey += tmpfy;
			
			force2 = -K2 * x2; //lower spring
			
		}
		
		
	}

	
	//simple RSA
	public class RSAs : Circuit
	{
		private double M1,M2;
		private double K1, K2, Kf;
		private double gamma1, gamma2, eta, mu, dcrit;
			
		private double x1, x2;
		private double v1, v2;
		
		private double f1, f2;
		private double slidedist = 0;
		
		private double x,y, tmpfx, tmpfy;
		
		public RSAs (string[] words)
		{
			Init(words);
			
			Input.Add(new Channel("exciter",null));
			Input.Add(new Channel("eta",null));
			Input.Add(new Channel("mu",null));
			
			Output.Add(new Channel("x1", this));
			Output.Add(new Channel("x2", this));
			Output.Add(new Channel("f1", this));
			Output.Add(new Channel("f2", this));
		}
		
		public override void SetUp ()
		{
			M1 = GetInitParameter(InitWords,"M1",true,0.1);
			M2 = GetInitParameter(InitWords,"M2",true,0.1);

			K1 = GetInitParameter(InitWords,"K1",true,250);
			K2 = GetInitParameter(InitWords,"K2",true,500);
			
			gamma1 = GetInitParameter(InitWords,"gamma1",true,0.01);
			gamma2 = GetInitParameter(InitWords,"gamma2",true,0.01);
			
			dcrit = GetInitParameter(InitWords,"roughd",true,0.001);
			
			M1 = 1.0/M1; M2 = 1.0/M2;
			
			//mygain = GetInitParameter(InitWords,"gain",true);
			InputDefaultInit();
			//theta = 0.01;
		}
		
		public override void Update (ref double dt)
		{
			f1 = 0; f2 = 0;
			eta = Input[1].Value;
			mu = Input[2].Value;
			
			//update positions
			x1 += v1 * dt;
			x2 += v2 * dt;
			slidedist += (v1-v2)*dt; //stick
			if(slidedist>0)
				slidedist -= Math.Floor(slidedist/dcrit)*dcrit; //slip
			else
				slidedist -= Math.Ceiling(slidedist/dcrit)*dcrit; //slip
				
			
			//get new forces
			f1 = -K1*x1 -gamma1*v1 -eta*(v1-v2) + Input[0].Value;
			f2 = -K2*x2 -gamma2*v2 -eta*(v2-v1);
			
			f1 -= mu*slidedist;
			f2 += mu*slidedist;
			
			
			//update speeds
			v1 = v1 + f1*dt*M1;
			v2 = v2 + f2*dt*M2;
			
			Output[0].Value = x1;
			Output[1].Value = x2;
			Output[2].Value = -mu*slidedist;
			Output[3].Value = slidedist;
			
		}
		
		
	}

}

