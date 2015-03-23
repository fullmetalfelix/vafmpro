using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace vafmpro.Circuits
{
    class Cantilever : Circuit
    {

        public bool[] Modes = { false, false, false }; //modes: flexural, 2nd flexural, torsional
        private int[] ModesI = { 0, 0, 0 }; //the multipliers associated to the modes

		private bool ModeV,ModeL;
		private double[] Wz,Qz,Kz; double Mz, Gz = 0;
		private double[] Wy,Qy,Ky; double My, Gy = 0;
		
		
        public double[] Freq = new double[3]; //and their frequencies
        private double[] W = new double[3];

        public double[] Q = new double[3]; //Q factors
        public double[] k = new double[3]; //spring consts
        public double[] m = new double[3]; //effective masses
        private double[] gamma = new double[3]; //the gamma to put in the diff equation
		
        //private current and older positions of the tip
		public double[] x;
        private double[] xo, xoo; //position referred to the holder!
		private double[] xc;      //absolute tip position
        private double[] v,a;
		private double[] force = new double[3]; //forces
		
        public Cantilever()
        {

            Init("cantilever");

            //holder position (not priority)
            Input.Add(new Channel("holderx", null));        //0
            Input.Add(new Channel("holdery", null));        //1
            Input.Add(new Channel("holderz", null));        //2
			Input.Add(new Channel("bias",null));            //3

			Input.Add(new Channel("zex",null));             //4
			Input.Add(new Channel("yex",null));             //5
			
			Input.Add(new Channel("Fx",null));              //6
			Input.Add(new Channel("Fy",null));              //7
			Input.Add(new Channel("Fz",null));              //8
			
			//tip position relative to the holder
            Output.Add(new Channel("x",this));//0
            Output.Add(new Channel("y", this));//1
            Output.Add(new Channel("z", this));//2
			//tip velocity
            Output.Add(new Channel("vx", this));//3
            Output.Add(new Channel("vy", this));//4
            Output.Add(new Channel("vz", this));//5

            Output.Add(new Channel("xtick", this));//6
            Output.Add(new Channel("ytick", this));//7
            Output.Add(new Channel("ztick", this));//8
			
			//tip absolute position
			Output.Add(new Channel("xabs",this));//9
            Output.Add(new Channel("yabs", this));//10
            Output.Add(new Channel("zabs", this));//11

			
			//x[3] will always be 0!
			xc = new double[4];
            x = new double[4]; v = new double[3]; a=new double[3];
            xo = new double[4];
            xoo = new double[4];

			//all cantilevers input are secondary by definition
			for(int i=0;i<Input.Count;i++)
				Input[i].Priority=false;
			
        }

		public bool ReadCantilever(string file)
		{
			StreamReader reader = new StreamReader(file); //reopen the file
            if (!StringReader.FindString("<cantilever>", reader)){
                Console.WriteLine("FATAL! No cantilever descrption was found.");
                return false;
            }
            Console.WriteLine("\n   Reading cantilever descrption:");

            string line = "";
            char[] delimiterChars = { ' ', '\t' };
			char[] delimiterCirc  = {'.'};
			string[] words,circh;
			Circuit circ;
			Channel ch;
            int nmodes = 0;
			
			#region "Vertical modes - Frequencies"
			if (!StringReader.FindStringNoEnd("flexmodes", reader, "<cantilever>", ref line, file)){
				Console.WriteLine("INFO! Cantilever has no flexural mode.");
				ModeV = false;
            }else{
				ModeV = true;
				words = StringReader.TrimWords(line.Split(delimiterChars));
				nmodes = words.Length - 1; //number of vertical modes found
				Wz = new double[nmodes];
				for(int i=0;i<nmodes;i++){
					if(!double.TryParse(words[1+i],out Wz[i])){ //if not numeric, try using circ.chan syntax
						
						circh = StringReader.TrimWords(words[1+i].Split(delimiterCirc)); //split the string
						if(circh.Length<2){
							Console.WriteLine("FATAL! Cantilever's flexural frequency #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						if(!Circuit.CheckCircuitChannel(circh,ChannelType.Any,out circ,out ch)){
							Console.WriteLine("FATAL! Cantilever's flexural frequency #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						Wz[i] = ch.Signal.Value;
					}
					Console.WriteLine("Found flexural mode at frequency: {0}Hz",Wz[i]);
					Wz[i] *= 2.0*Math.PI; //convert the frequency to angular pulse!
				}
			}
			
			#endregion
			#region "Vertical modes - Qfactors"
			if(ModeV){
				if (!StringReader.FindStringNoEnd("flexQ", reader, "<cantilever>", ref line, file) && ModeV){
					Console.WriteLine("FATAL! Cantilever has flexural modes but no Q factor was given.");
					return false;
				}
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if(words.Length - 1 < nmodes){
					Console.WriteLine("FATAL! The amount of Q factors ({0}) does not match the amount of flexural modes ({0}).",words.Length-1,nmodes);
					return false;
				}
				Qz = new double[nmodes];
				for(int i=0;i<nmodes;i++){
					if(!double.TryParse(words[1+i],out Qz[i])){ //if not numeric, try using circ.chan syntax
						
						circh = StringReader.TrimWords(words[1+i].Split(delimiterCirc)); //split the string
						if(circh.Length<2){
							Console.WriteLine("FATAL! Cantilever's Q factor #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						if(!Circuit.CheckCircuitChannel(circh,ChannelType.Any,out circ,out ch)){
							Console.WriteLine("FATAL! Cantilever's Q factor #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						Qz[i] = ch.Signal.Value;
					}
					Console.WriteLine("Found flexural Q factor         : {0}",Qz[i]);
					Qz[i] = Wz[i]/Qz[i]; //convert Q to gamma
				}
			}
			#endregion
			#region "Vertical modes - Springs"
			if(ModeV){
				if (!StringReader.FindStringNoEnd("flexk", reader, "<cantilever>", ref line, file) && ModeV){
					Console.WriteLine("FATAL! Cantilever has flexural modes but no spring constant k was given.");
					return false;
				}
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if(words.Length - 1 < nmodes){
					Console.WriteLine("FATAL! The amount of spring constants k ({0}) does not match the amount of flexural modes ({0}).",words.Length-1,nmodes);
					return false;
				}
				Kz = new double[nmodes];
				for(int i=0;i<nmodes;i++){
					if(!double.TryParse(words[1+i],out Kz[i])){ //if not numeric, try using circ.chan syntax
						
						circh = StringReader.TrimWords(words[1+i].Split(delimiterCirc)); //split the string
						if(circh.Length<2){
							Console.WriteLine("FATAL! Cantilever's spring constant k #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						if(!Circuit.CheckCircuitChannel(circh,ChannelType.Any,out circ,out ch)){
							Console.WriteLine("FATAL! Cantilever's spring constant k #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						Kz[i] = ch.Signal.Value;
					}
					Console.WriteLine("Found flexural spring const. k : {0}",Kz[i]);
				}
			}
			#endregion			
			
			#region "Lateral modes - Frequencies"
			if (!StringReader.FindStringNoEnd("latmodes", reader, "<cantilever>", ref line, file)){
				Console.WriteLine("INFO! Cantilever has no torsional mode.");
				ModeL = false;
            }else{
				ModeL = true;
				words = StringReader.TrimWords(line.Split(delimiterChars));
				nmodes = words.Length - 1; //number of vertical modes found
				Wy = new double[nmodes];
				for(int i=0;i<nmodes;i++){
					if(!double.TryParse(words[1+i],out Wy[i])){ //if not numeric, try using circ.chan syntax
						
						circh = StringReader.TrimWords(words[1+i].Split(delimiterCirc)); //split the string
						if(circh.Length<2){
							Console.WriteLine("FATAL! Cantilever's torsional frequency #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						if(!Circuit.CheckCircuitChannel(circh,ChannelType.Any,out circ,out ch)){
							Console.WriteLine("FATAL! Cantilever's torsional frequency #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						Wy[i] = ch.Signal.Value;
					}
					Console.WriteLine("Found torsional mode at frequency: {0}Hz",Wy[i]);
					Wy[i] *= 2.0*Math.PI; //convert the frequency to angular pulse!
				}
			}
			
			#endregion
			#region "Vertical modes - Qfactors"
			if(ModeL){
				if (!StringReader.FindStringNoEnd("latQ", reader, "<cantilever>", ref line, file) && ModeL){
					Console.WriteLine("FATAL! Cantilever has torsional modes but no Q factor was given.");
					return false;
				}
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if(words.Length - 1 < nmodes){
					Console.WriteLine("FATAL! The amount of Q factors ({0}) does not match the amount of torsional modes ({0}).",words.Length-1,nmodes);
					return false;
				}
				Qy = new double[nmodes];
				for(int i=0;i<nmodes;i++){
					if(!double.TryParse(words[1+i],out Qy[i])){ //if not numeric, try using circ.chan syntax
						
						circh = StringReader.TrimWords(words[1+i].Split(delimiterCirc)); //split the string
						if(circh.Length<2){
							Console.WriteLine("FATAL! Cantilever's Q factor #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						if(!Circuit.CheckCircuitChannel(circh,ChannelType.Any,out circ,out ch)){
							Console.WriteLine("FATAL! Cantilever's Q factor #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						Qy[i] = ch.Signal.Value;
					}
					Console.WriteLine("Found torsional Q factor         : {0}",Qy[i]);
					Qy[i] = Wy[i]/Qy[i]; //convert Q to gamma
				}
			}
			#endregion
			#region "Vertical modes - Springs"
			if(ModeL){
				if (!StringReader.FindStringNoEnd("latk", reader, "<cantilever>", ref line, file) && ModeL){
					Console.WriteLine("FATAL! Cantilever has torsional modes but no spring constant k was given.");
					return false;
				}
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if(words.Length - 1 < nmodes){
					Console.WriteLine("FATAL! The amount of spring constants k ({0}) does not match the amount of torsional modes ({0}).",words.Length-1,nmodes);
					return false;
				}
				Ky = new double[nmodes];
				for(int i=0;i<nmodes;i++){
					if(!double.TryParse(words[1+i],out Ky[i])){ //if not numeric, try using circ.chan syntax
						
						circh = StringReader.TrimWords(words[1+i].Split(delimiterCirc)); //split the string
						if(circh.Length<2){
							Console.WriteLine("FATAL! Cantilever's spring constant k #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						if(!Circuit.CheckCircuitChannel(circh,ChannelType.Any,out circ,out ch)){
							Console.WriteLine("FATAL! Cantilever's spring constant k #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
						Ky[i] = ch.Signal.Value;
					}
					Console.WriteLine("Found torsional spring const. k : {0}",Ky[i]);
				}
			}
			#endregion			
			
			#region "Mass"
			
            if (!StringReader.FindStringNoEnd("mass", reader, "<cantilever>", ref line, file)){
                Console.WriteLine("INFO! Cantilever's effective mass will be calculated from k and w.");
			
				Mz = 0; My = 0;
				if(ModeV){
					for(int i=0;i<Wz.Length;i++)
						Mz += Kz[i]/(Wz[i]*Wz[i]);
					Mz /= Wz.Length;
				}else
					Mz = 1;
				
				if(ModeL){
					for(int i=0;i<Wy.Length;i++)
						My += Ky[i]/(Wy[i]*Wy[i]);
					My /= Wy.Length;
				}else
					My = 1;
				
			}
			else{ //read them if present
				words = StringReader.TrimWords(line.Split(delimiterChars));
                if (words.Length - 1 < 2){
                    Console.WriteLine("FATAL! Cantilever's effective masses are not specified for both flexural and torsional modes.");
                    return false;
                }
				if(!double.TryParse(words[1], out Mz)){
					Console.WriteLine("FATAL! Cantilever's effective masses have to be numbers.");
					return false;
				}
				if(!double.TryParse(words[2], out My)){
					Console.WriteLine("FATAL! Cantilever's effective masses have to be numbers.");
					return false;
				}

            }
			Console.WriteLine("Cantilever's effective masses: {0} {1}",Mz,My);
			
            #endregion

			#region "Starting TIP position"
            if (!StringReader.FindStringNoEnd("tipInit", reader, "<cantilever>", ref line, file)){
				Console.WriteLine("FATAL! Tip starting position not specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 3){
                Console.WriteLine("FATAL! Some tip's initial coordinates are missing.");
                return false;
            }
			for(int i=0;i<2;i++){
				if(!double.TryParse(words[i+1],out x[i+1])){
					Console.WriteLine("FATAL! Tip's initial coordinate should be a number.");
					return false;
				}
			}
			
            #endregion
			return true;
		}
		
		public void InitCanti()
        {

			Console.WriteLine("CANTILEVER INITIALIZATION...");

			for (int i = 0; i < 3; i++){ //0 starting force/speed
				a[i] = 0;
				v[i] = 0;
			}
			
			double maxf = 0;
			if(ModeV){
				for (int i=0; i<Wz.Length;i++){
					if(Wz[i] > maxf)
						maxf = Wz[i];
					Gz += Qz[i]; //sum up all the gammaz
				}
			}
			if(ModeL){
				for (int i=0; i<Wy.Length;i++){
					if(Wy[i] > maxf)
						maxf = Wy[i];
					Gy += Qy[i]; //sum up all the gammay
				}
			}
			
            Console.WriteLine("   Maximum system's frequency is: {0}", (0.5*maxf/Math.PI).ToString());
            maxf = (1 / maxf) / Program.myTimer.dt;
            Console.WriteLine("   Number of points per cycle is: {0}", maxf.ToString());
            if (maxf < 10)
                Console.WriteLine("WARNING! There will be less than 10 points in a cycle at that frequency!\n   Consider decreasing the time step!");

			Console.WriteLine("DONE!");
        }
				
		public override void Update(ref double dt)
        {
			//the forces are supposed to be updated already
			
			//sets the tip absolute position (bias included)
			for(int i = 0; i < 4; i++){
				xc[i] = x[i]+Input[i].Signal.Value;	
			}
			//Console.WriteLine("Cantilever: 1 abspos {0} {1} {2}",xc[0],xc[1],xc[2]);
			//Console.WriteLine("Cantilever: 1 acc {0} {1} {2}",a[0],a[1],a[2]);

			//verlet position update
            x[2] = x[2] + v[2] * dt + 0.5 * a[2] * dt * dt;
           // x[1] = x[1] + v[1] * dt + 0.5 * a[1] * dt * dt;

			//Console.WriteLine("Cantilever: 1.1 tippos {0} {1} {2}",x[0],x[1],x[2]);
			//TODO: a real multimode stuff
            //halfstep v update			
            v[2] = 0.5 * dt * a[2] + v[2] * (1.0 - 0.5 * dt * Gz);
            v[1] = 0.5 * dt * a[1] + v[1] * (1.0 - 0.5 * dt * Gy);

			//now we would need to get the forces again... and there is no way we can get them!
			//so we end the update of the cantilever here, output position and speed (at half timestep)
			//and we are happy with it, then all the other circuits will be updated, so the forces
			//will be updated with the current position. Then, we run the PostUpdate for the cantilever
			//where the speed is updated by the other dt/2.
			
			//update the outputs
			for (int i = 0; i < 3; i++){
				Output[i].Signal.Value = x[i];      //tip position
                Output[i + 3].Signal.Value = v[i];  //speed
				Output[i + 9].Signal.Value = x[i]+Input[i].Signal.Value; //absolute tip position
            }
			
		}
		
		//update tip speed (only by dt/2)
		public void PostUpdate(ref double dt)
		{
			//take forces from input channels
			for (int i = 0; i < 3; i++)
				force[i] = Input[i+6].Signal.Value; //tip-sample force
			//Console.WriteLine("Cantilever: 2 force {0} {1} {2}",force[0],force[1],force[2]);
			//     tipsample force                   W²                                        Z-piezoZ
            a[2] = force[2] / m[0] -(W[0] * W[0] * ModesI[0] + W[1] * W[1] * ModesI[1]) * (x[2] - Input[4].Signal.Value);
            //a[1] = force[1] / m[2] - (W[2] * W[2] * x[1] * ModesI[2]);
			//Console.WriteLine("\nALLCHECK!");
			//Console.WriteLine("W0 {0} W1 {1} W2 {2}",W[0],W[1],W[2]);
			//Console.WriteLine("M0 {0} M1 {1} M2 {2}",ModesI[0],ModesI[1],ModesI[2]);
			//Console.WriteLine("x1 {0} x2 {1} in4 {2}",x[1],x[2],Input[4].Signal.Value);
			
			//Console.WriteLine("Cantilever: 2 acc {0} {1} {2}",a[0],a[1],a[2]);
			
			//update velocity by half dt again with the new forces
			v[2] = 0.5 * dt * a[2] + v[2] * (1.0 - 0.5 * dt * (gamma[0] + gamma[1]));
			//v[1] = 0.5 * dt * a[1] + v[1] * (1.0 - 0.5 * dt * gamma[2]);
			
			//update the absolute tip position
			for(int i = 0; i < 4; i++)
				xc[i] = x[i] + Input[i].Signal.Value;	
			
			//Console.WriteLine("Cantilever: 2 abspos  {0} {1} {2}",xc[0],xc[1],xc[2]);
			if(double.IsNaN(xc[1])){
				throw new Exception("nan?");
			}
			
            #region "Ticks"
			for(int i=0;i<3;i++){
				if ((xoo[i] < xo[i]) && (x[i] < x[i]))
					Output[i+6].Signal.Value = 1;
				else if ((xoo[i] > xo[i]) && (x[i] > x[i]))
					Output[i+6].Signal.Value = -1;
				else
					Output[i+6].Signal.Value = 0;
			}
            #endregion
			#region "back collect"
            for (int i = 0; i < 4; i++){
                xoo[i] = xo[i];
                xo[i] = x[i];
            }
			#endregion
			
		}

			//TMP like in lev code
			//a[2] -= (gamma[0] + gamma[1])*v[2];
			//a[1] -= gamma[1]*v[1];
						
			//Console.WriteLine("{0}",a[2].ToString());

    }
}


/*            FORTRAN VERSION
!*** Customized Verlet Velocity *********************************************************************
X=X + V*dt + 0.50d0*A*dt**2                       !Calc X in the next time
!V=V + 0.50d0*A*dt                                 !Calc V(T+Dt/2) USING A(T)
V(3)= 0.50d0*A(3)*dt + V(3)*(1.0d0-0.50d0*dt*Gamma1)
V(2)= 0.50d0*A(2)*dt + V(2)*(1.0d0-0.50d0*dt*Gamma2)

!x(2)=0.0d0                           !lock X,Y
!x(1)=0.0d0!dble(2.820e-10)
Call GetForce(A)                                  !Calc A(T+Dt)

V(3)= 0.50d0*A(3)*dt + V(3)*(1.0d0-0.50d0*dt*Gamma1)
V(2)= 0.50d0*A(2)*dt + V(2)*(1.0d0-0.50d0*dt*Gamma2)

!V(3)=(V(3) + 0.50d0*A(3)*dt)/(1.0d0+0.50d0*dt*Gamma1) !Calc V(T+Dt)   USING A(T+Dt/2) (Z Component)
!V(2)=(V(2) + 0.50d0*A(2)*dt)/(1.0d0+0.50d0*dt*Gamma2) !Calc V(T+Dt)   USING A(T+Dt/2) (Y Component)
!V(1)=(V(1) + 0.50d0*A(1)*dt)                      !Calc V(T+Dt)   USING A(T+Dt/2) (X Component)
!****************************************************************************************************/