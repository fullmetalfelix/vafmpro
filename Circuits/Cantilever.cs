using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace vafmpro.Circuits
{
    class Cantilever : Circuit
    {
		private double[] IniPos = new double[3];
		private int nV,nL;
		private bool ModeV,ModeL;
		private double[] Wz,Qz,Kz; //one of these for each v mode
		private double[] Wy,Qy,Ky; //one of these for each l mode
		private double[] z,zo,zoo; //one of these for each v mode
		private double[] y,yo,yoo; //one of these for each l mode
		
		private double Ztip, Ztipo, Ztipoo; //relative and absolute tip Z
		private double Ytip, Ytipo, Ytipoo; //relative and absolute tip Y
		private double Bias;
		
		private double[] Mz; private double az;
		private double[] My; private double ay;
		
		private double Az_exc, Az_TS;
		
		private double dt;
		
		public Circuit ForceModule;
		
		public Cantilever()
        {

            Init(new string[]{"program","cantilever"},8,8);
			
			
            //holder position (not priority)
            Input.Add(new Channel("holderx", null));        //0
            Input.Add(new Channel("holdery", null));        //1
            Input.Add(new Channel("holderz", null));        //2
			Input.Add(new Channel("bias",null));            //3

			Input.Add(new Channel("yex",null));             //4
			Input.Add(new Channel("zex",null));             //5
			
			Input.Add(new Channel("Fy",null));              //6
			Input.Add(new Channel("Fz",null));              //7
			
			//tip position relative to the holder
            Output.Add(new Channel("y", this));      //0
            Output.Add(new Channel("z", this));      //1
			//tip velocity
            Output.Add(new Channel("vy", this));     //2
            Output.Add(new Channel("vz", this));     //3

            Output.Add(new Channel("xabs", this));      //4
			Output.Add(new Channel("yabs", this));      //5
            Output.Add(new Channel("zabs", this));      //6
			

			/*
			//all cantilevers input are secondary by definition
			for(int i=0;i<Input.Count;i++)
				Input[i].Priority=false;
			*/
			
			dt = Program.mytimer.dt;
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
			//char[] delimiterCirc  = {'.'};
			string[] words;//,circh;
			//Circuit circ;
			//Channel ch;
            int nmodes = 0;
			
			#region "Vertical modes - Frequencies"
			if (!StringReader.FindStringNoEnd("flexmodes", reader, "<cantilever>", ref line, file)){
				Console.WriteLine("INFO! Cantilever has no flexural mode.");
				ModeV = false;
				nV = 0;
            }else{
				ModeV = true;
				words = StringReader.TrimWords(line.Split(delimiterChars));
				nmodes = words.Length - 1; //number of vertical modes found
				nV = nmodes;
				Wz = new double[nmodes];
				for(int i=0;i<nmodes;i++){
					
					if(!double.TryParse(words[1+i],out Wz[i])){ //if not numeric, try using variable
						Wz[i] = Program.GetVariable(null,words[1+i]);
						if(Wz[i] == 0.0){
							Console.WriteLine("FATAL! Cantilever's flexural frequency #{0} must be a number or variable.",i+1);
							return false;
						}
					}
					
					Console.WriteLine("Found flexural mode at frequency: {0}Hz",Wz[i]);
					Wz[i] *= 2.0*Math.PI*dt; //convert the frequency to angular pulse!
				}
				
				//create the position vectors
				z = new double[Wz.Length];
				zo = new double[Wz.Length];
				zoo = new double[Wz.Length];
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
					
					if(!double.TryParse(words[1+i],out Qz[i])){ //if not numeric, try using variable
						Qz[i] = Program.GetVariable(null,words[1+i]);
						if(Qz[i] == 0.0){
							Console.WriteLine("FATAL! Cantilever's fQ factor #{0} must be a number or variable.",i+1);
							return false;
						}
					}
										
					Console.WriteLine("Found flexural Q factor         : {0}",Qz[i]);
					Qz[i] = 0.5*Wz[i]/Qz[i]; //convert Q to gamma
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
					
					if(!double.TryParse(words[1+i],out Kz[i])){ //if not numeric, try using variable
						Kz[i] = Program.GetVariable(null,words[1+i]);
						if(Kz[i] == 0.0){
							Console.WriteLine("FATAL! Cantilever's spring constant k #{0} must be a number or variable.",i+1);
							return false;
						}
					}
					
					Console.WriteLine("Found flexural spring const. k : {0}",Kz[i]);
				}
			}
			#endregion			
			
			#region "Lateral modes - Frequencies"
			if (!StringReader.FindStringNoEnd("latmodes", reader, "<cantilever>", ref line, file)){
				Console.WriteLine("INFO! Cantilever has no torsional mode.");
				ModeL = false;
				nL = 0;
            }else{
				ModeL = true;
				words = StringReader.TrimWords(line.Split(delimiterChars));
				nmodes = words.Length - 1; //number of vertical modes found
				nL = nmodes;
				Wy = new double[nmodes];
				for(int i=0;i<nmodes;i++){
					
					if(!double.TryParse(words[1+i],out Wy[i])){ //if not numeric, try using variable
						Wy[i] = Program.GetVariable(null,words[1+i]);
						if(Wy[i] == 0.0){
							Console.WriteLine("FATAL! Cantilever's torsional frequency #{0} must be a number or variable.",i+1);
							return false;
						}
					}
					Console.WriteLine("Found torsional mode at frequency: {0}Hz",Wy[i]);
					Wy[i] *= 2.0*Math.PI*dt; //convert the frequency to angular pulse! times DT
				}
				
				//create the position vectors
				y = new double[Wy.Length];
				yo = new double[Wy.Length];
				yoo = new double[Wy.Length];
			}
			
			#endregion
			#region "Lateral modes - Qfactors"
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
					
					if(!double.TryParse(words[1+i],out Qy[i])){ //if not numeric, try using variable
						Qy[i] = Program.GetVariable(null,words[1+i]);
						if(Qy[i] == 0.0){
							Console.WriteLine("FATAL! Cantilever's Q factor #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
					}
					
					Console.WriteLine("Found torsional Q factor         : {0}",Qy[i]);
					Qy[i] = 0.5*Wy[i]/Qy[i]; //convert Q to gamma
				}
			}
			#endregion
			#region "Lateral modes - Springs"
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
					
					if(!double.TryParse(words[1+i],out Ky[i])){ //if not numeric, try using variable
						Ky[i] = Program.GetVariable(null,words[1+i]);
						if(Ky[i] == 0.0){
							Console.WriteLine("FATAL! Cantilever's spring constant k #{0} must be a number or a valid circuit.channel name!",i+1);
							return false;
						}
					}
					
					Console.WriteLine("Found torsional spring const. k : {0}",Ky[i]);
				}
			}
			#endregion			
			
			
			#region "Mass"
			
			if(ModeV){
				Mz = new double[Wz.Length];
				for(int i=0;i<Wz.Length;i++)
					Mz[i] = 1.0 / (Kz[i] / (Wz[i]*Wz[i]) ); // this is dt^2/M
			}
			
			if(ModeL){
				My = new double[Wy.Length];
				for(int i=0;i<Wy.Length;i++)
					My[i] = 1.0 / (Ky[i] / (Wy[i]*Wy[i]) ); //masses inverted!!
			}
				
			//Console.WriteLine("Cantilever's effective masses: {0} {1}",Mz,My);
			
			
            #endregion
			
			#region "Starting Z TIP position"
			if(ModeV){
				if (!StringReader.FindStringNoEnd("tipInitZ", reader, "<cantilever>", ref line, file)){
					Console.WriteLine("FATAL! Tip starting Z position not specified.");
					return false;
				}
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if (words.Length < 2){
					Console.WriteLine("FATAL! Tip starting Z position not specified correctly.");
					return false;
				}
				//read the lateral pos
				double zini = 0.0;
				if(!double.TryParse(words[1], out zini)){ //if not a number,
					zini = Program.GetVariable(null,words[1]);
				}
				
				if(words.Length>2){ //if there is a third keyword... which should be the number of the excited mode or 'all'
					
					if(!int.TryParse(words[2], out nmodes) ){//if not numerical value
						
						if(words[2].StartsWith("all")){
							Console.WriteLine("  All modes will be equally initialized @ zini = {0}",zini/(double)Wz.Length);
							for(int i=0;i<Wz.Length;i++)
								z[i] = zini/(double)Wz.Length;
							
						}else{ //if non numeric and is not "all"
							Console.WriteLine("WARNING! The mode specified is not a valid number! Assuming 1!");
							z[0] = zini;
						}
						
					}else{ //if it was numeric all along
						
						if((nmodes < 1) || (nmodes > Wz.Length)){ //check for invalid numbers
							Console.WriteLine("WARNING! The mode specified is not a valid number! Assuming 1!");
							z[0] = zini;
						}else{ //if valid
							Console.WriteLine("  Initial Z only for vertical mode #{0}.",nmodes);
							z[nmodes-1] = zini;
						}
					}
				} else {
					Console.WriteLine("  Initial Z only for vertical mode #1.");
					z[0] = zini;
				} //end of "if there was a third keyword"
			} //end of "if there are vertical modes"
            #endregion
			#region "Starting Y TIP position"
			if(ModeL){
				if (!StringReader.FindStringNoEnd("tipInitY", reader, "<cantilever>", ref line, file)){
					Console.WriteLine("FATAL! Tip starting Y position not specified.");
					return false;
				}
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if (words.Length < 2){
					Console.WriteLine("FATAL! Tip starting Y position not specified correctly.");
					return false;
				}
				//read the lateral pos
				double yini = 0.0;
				if(!double.TryParse(words[1], out yini)){ //if not a number,
					yini = Program.GetVariable(null,words[1]);
				}
				
				if(words.Length>2){ //if there is a third keyword...
					
					if(!int.TryParse(words[2], out nmodes) ){//if not numerical value
						
						if(words[2].StartsWith("all")){
							Console.WriteLine("  All modes will be equally initialized@ yini = {0}",yini/(double)Wy.Length);
							for(int i=0;i<Wy.Length;i++)
								y[i] = yini/(double)Wy.Length;
						}else{ //if non numeric and is not "all"
							Console.WriteLine("WARNING! The mode specified is not a valid number! Assuming 1!");
							y[0] = yini;
						}
					}else{ //if it was numeric
						
						if((nmodes < 1) || (nmodes > Wy.Length)){ //check for invalid numbers
							Console.WriteLine("WARNING! The mode specified is not a valid number! Assuming 1!");
							y[0] = yini;
						}else{ //if valid
							Console.WriteLine("  Initial Y only for lateral mode #{0}.",nmodes);
							y[nmodes-1] = yini;
						}
					}
				} else {
					Console.WriteLine("  Initial Y only for lateral mode #1.");
					y[0] = yini;
				} //end of "if there was a third keyword"
			} //end of "if there are lateral modes"
            #endregion
			
			#region "position offset"
			if (!StringReader.FindStringNoEnd("position", reader, "<cantilever>", ref line, file)){
				Console.WriteLine("ERROR! Cantilever has no initial position.");
				return false;
            }else{
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if(words.Length < 4){
					Console.WriteLine("ERROR! Cantilever initial position is not given correctly.");
					return false;
				}
				for(int i=0;i<3;i++){
					
					if(!double.TryParse(words[1+i],out IniPos[i])){
						IniPos[i] = Program.GetVariable(null,words[1+i]);
					}
					
				}
				Console.WriteLine("Cantilever initial position: {0} {1} {2}",IniPos[0],IniPos[1],IniPos[2]);
			}
			#endregion
			
			
			#region "force module"
			if (!StringReader.FindStringNoEnd("forcemodule", reader, "<cantilever>", ref line, file)){
				Console.WriteLine("WARNING! Cantilever has no force module explicitly attached.");
            }else{
				words = StringReader.TrimWords(line.Split(delimiterChars));
				if(words.Length < 2){
					Console.WriteLine("ERROR! Cantilever force module is not specified correctly.");
					return false;
				}
				if(!Circuit.FindCircuit(words[1],Program.Circuits, out ForceModule)){
					Console.WriteLine("ERROR! Cantilever force module {0} is not a valid circuit.",words[1]);
					return false;
				}
				
				//the force module wont be updated in the main program update cycle
				
				Console.WriteLine("Force module type: {0}",ForceModule.GetType().ToString());
			}
			
			#endregion
			
			Initialize();
			
			return true;
		}
		private void Initialize()
        {

			Console.WriteLine("CANTILEVER INITIALIZATION...");

			dt = Program.mytimer.dt;
			
			az = 0.0;
			ay = 0.0;
			
			#region "set initial old positions & multifreq outputs"
			
			//vertical modes:
			Ztip = 0.0;
			if(ModeV){
				
				for(int i=0;i<Wz.Length;i++){
					zo[i] = z[i];
					zoo[i] = z[i];
					Ztip += z[i];
				}
				for(int i=0;i<Wz.Length;i++)
					Output.Add(new Channel("z"+(i+1).ToString(),this)); //add channels for modes positions
				for(int i=0;i<Wz.Length;i++)
					Output.Add(new Channel("vz"+(i+1).ToString(),this));//add channels for modes velocities				
				
				Ztipo = Ztip;
				Ztipoo= Ztip;
			}
			
			//lateral modes:
			Ytip = 0.0;
			if(ModeL){
				
				for(int i=0;i<Wy.Length;i++){
					yo[i] = y[i];
					yoo[i] = y[i];
					Ytip += y[i];
				}
				for(int i=0;i<Wy.Length;i++)
					Output.Add(new Channel("y"+(i+1).ToString(),this)); //add channels for modes positions
				for(int i=0;i<Wy.Length;i++)
					Output.Add(new Channel("vy"+(i+1).ToString(),this));//add channels for modes velocities		
				
				Ytipo = Ytip;
				Ytipoo= Ytip;
			}		
			
			#endregion
			
			#region "check points/cycle"
			double maxf = 0;
			if(ModeV){
				for (int i=0; i<Wz.Length;i++){
					if(Wz[i]/dt > maxf)
						maxf = Wz[i]/dt;
				}
			}
			if(ModeL){
				for (int i=0; i<Wy.Length;i++){
					if(Wy[i]/dt > maxf)
						maxf = Wy[i]/dt;
				}
			}
			
            Console.WriteLine("   Maximum system's frequency is: {0}", (0.5*maxf/Math.PI).ToString());
            maxf = (1 / maxf) / Program.mytimer.dt;
            Console.WriteLine("   Number of points per cycle is: {0}", maxf.ToString());
            if (maxf < 10)
                Console.WriteLine("WARNING! There will be less than 10 points in a cycle at that frequency!\n   Consider decreasing the time step!");

			#endregion
			
			//for(int i=0; i<Output.Count; i++)
			//	Console.WriteLine("canti output {0}: {1}  ({2})",i,Output[i].Name,Wz.Length);
			Console.WriteLine("DONE!");
        }
		
		public override void Update (ref double dt)
		{
			//get the absolute tip position TipAbsPos
			
			#region "vertical modes - first update"
			if(ModeV){
				
				Ztip = 0.0; //the final tip position as sum of all the modes
				
				//since canti goes first, the input channels have just been pushed
				
				for(int i=0;i<Wz.Length;i++){ //loop over the modes update x and first half of v
					
					//use zoo as acceleration for each mode				
					//use zo as velocity for each mode			
					//Qz must be wdt/2Q
					
					z[i] += zo[i]*dt*(1.0-Qz[i]) + 0.5*zoo[i]*dt; //update x
					zo[i] = zo[i]*(1.0-Qz[i]) + 0.5*zoo[i]; //update v (half step)
					
					Output[7+i].Value = z[i];               //assign the mode output position
					
					Ztip += z[i];
					
				}//at this point we have the new tip position
				Output[1].Value = Ztip;
				Output[3].Value = 0.5*(Ztip-Ztipo)/dt; //and this is the 1st order velocity!
				Ztipo = Ztip;
				
			}
			
            #endregion
			#region "lateral modes - first update"
			if(ModeL){
				
				Ytip = 0.0; //the final tip position as sum of all the modes
				
				for(int i=0;i<Wy.Length;i++){ //loop over the modes update x and first half of v
					
					y[i] += yo[i]*dt*(1.0-Qy[i]) + 0.5*yoo[i]*dt; //update x
					yo[i] = yo[i]*(1.0-Qy[i]) + 0.5*yoo[i]; //update v (half step)
					
					Output[7+2*nV+i].Value = y[i];          //assign the mode output position
					
					Ytip += y[i];
					
				}//at this point we have the new tip position
				Output[0].Value = Ytip;
				Output[2].Value = 0.5*(Ytip-Ytipo)/dt;
				Ytipo = Ytip;
				
			}
			
            #endregion
			
			#region "set the output positions"			
			Output[4].Value = Input[0].Value + IniPos[0];   //tip absolute x
			Output[5].Value = Input[1].Value + Ytip + IniPos[1];   //tip absolute y
			Output[6].Value = Input[2].Value + Ztip + IniPos[2];   //tip absolute z
			Output[4].Signal.PushBuffer(); //push the buffer with the absolute positions!
			Output[5].Signal.PushBuffer(); //push the buffer!
			Output[6].Signal.PushBuffer(); //push the buffer!
			#endregion

			//update the force module
			if(ForceModule != null){
				ForceModule.Update(ref dt);
				for(int i=0;i<ForceModule.Output.Count;i++)
					ForceModule.Output[i].Signal.PushBuffer();
			}
			
			#region "vertical - final update"
			if(ModeV){
				
				az = (Input[7].Value + Input[5].Value); //sum Fts and excitation
				
				for(int i=0;i<Wz.Length;i++){ //now loop again, get the new accelerations and finish update v
					
					//       (exc+fts)   +    spring       :this is a*dt
					zoo[i] = az*Mz[i] - z[i]*Wz[i]*Wz[i];
					zoo[i] /= dt;
					zo[i] = zo[i]*(1.0-Qz[i]) + 0.5*zoo[i]; //update v (half step)
					
					Output[7+nV+i].Value = zo[i]; //assign the mode output velocity
				}
			}
			#endregion
			#region "lateral - final update"
			if(ModeL){
				
				ay = (Input[6].Value + Input[4].Value); //sum Fts and excitation
				
				for(int i=0;i<Wy.Length;i++){ //now loop again, get the new accelerations and finish update v
					
					//       (exc+fts)   +    spring       :this is a*dt
					yoo[i] = ay*My[i] - y[i]*Wy[i]*Wy[i];
					yoo[i] /= dt;
					yo[i] = yo[i]*(1.0-Qy[i]) + 0.5*yoo[i]; //update v (half step)
					
					Output[7+2*nV+nL+i].Value = yo[i]; //assign the mode output velocity
				}
			}
			#endregion
			
			//now push all single mode output channels
			for(int i=7;i<Output.Count;i++)
				Output[i].Signal.PushBuffer();
			
		}
		
		
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