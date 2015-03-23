using System;
using vafmpro.AbsCircuits;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using vafmpro.Circuits;

namespace vafmpro.Instructions
{
	
	public abstract class Function_Circuit : Circuit
	{
		protected string[] Parameters;
		public Scripter myScripter;
		
		public bool isInitialized = false;
		public bool isActive = false;
		public bool isDone = false;
		
		public Function_Circuit (string[] words, Scripter engine)
		{
			Parameters = words;
			myScripter = engine;
		}
		
		
		protected override double GetInitParameter(string[] words, string pName, bool Necessary)
		{
			string[] keys, circchan;
			double val = double.NaN;
			Circuit circ;
			Channel chan;
			List<Circuit> CList;
			
			for(int i=1;i<words.Length;i++){ //start from the 3rd element cos 0 is the type, and 1 is the name
				
				if(!words[i].Contains(pName+"="))
					continue;
				
				keys = StringReader.TrimWords(words[i].Split('='));//read the type and the name
				//Console.WriteLine("parser: splitting {0}",words[i]);
				#region "try parse double/local var/global var/circ.chan"
				
				if(!double.TryParse(keys[1], out val)){ //if not numeric
					//Console.WriteLine("parser: Not A DOUBLE! {0}",words[i]);
					
					#region "Circ.chan pair"
					circchan = StringReader.TrimWords(keys[1].Split('.')); //try to split it in circ.chan pair
					if(circchan.Length == 2){
						if(myScripter.Owner != null){ // (only for composites)
							//Console.WriteLine("scripter: checking in subcircs of {0}",Owner.Name);
							CList = ((composite)(myScripter.Owner)).SubCircuits;
						} else {
							CList = Program.Circuits;
						}
						if(!Circuit.CheckCircuitChannel(circchan,ChannelType.Any,CList,out circ, out chan))
							throw new Exception("ERROR! "+circchan[0]+"."+circchan[1]+" not found!\n");
						val = chan.Value;
						return val;
					}
					#endregion
					
					//try a local variable
					if(myScripter.Owner != null){ // (only for composites)
						//Console.WriteLine("parser: owner is not null {0}",Owner.Name);
						if( ((composite)(myScripter.Owner)).Locals.ContainsKey(keys[1])){
							val = ((composite)(myScripter.Owner)).Locals[keys[1]];
							//Console.WriteLine("parser: found in local vars {0}",val);
						}
					}else{
						//try a global variable
						//Console.WriteLine("parser: owner is null");
						if(Program.Globals.ContainsKey(keys[1])){
							val = Program.Globals[keys[1]];
							//Console.WriteLine("parser: found in global vars {0}",val);
						}
					}
				}
				#endregion
				
				if(double.IsNaN(val))
					throw new Exception("ERROR! The given value ("+keys[0]+"="+keys[1]+") is not a variable nor a number!");
				
				return val;
			}
			//if we arrive here, the parameter pName was not in the input line
			throw new Exception("ERROR! Script parameter "+pName+" was not specified!");
			//return double.NaN;
		}
		protected override double GetInitParameter(string[] words, string pName, bool Necessary, double defval){
			
			string[] keys;
			double val = double.NaN;
			
			for(int i=1;i<words.Length;i++){ //start from the 3rd element cos 0 is the type, and 1 is the name
				
				if(!words[i].Contains(pName+"="))
					continue;
				
				keys = StringReader.TrimWords(words[i].Split('='));//read the type and the name
				
				#region "try parse double/local var/global var/"
				
				if(!double.TryParse(keys[1], out val)){ //if not numeric
					
					//try a local variable
					if(Owner != null){ // (only for composites)
						if( ((composite)Owner).Locals.ContainsKey(keys[1]))
							val = ((composite)Owner).Locals[keys[1]];
					}else{
						//try a global variable
						if(Program.Globals.ContainsKey(keys[1])){
							val = Program.Globals[keys[1]];
						}
					}
				}
				#endregion
				
				if(double.IsNaN(val) && Necessary)
					throw new Exception("ERROR! The given value ("+keys[0]+"="+keys[1]+") is not a variable nor a number!");
				
				return val;
			}
			//if we arrive here, the parameter pName was not in the input line
			if(Necessary)
				throw new Exception("ERROR! Parameter "+pName+" was not specified!");
			else
				return defval;
			
		}
		protected override string GetInitParameterString(string[] words, string pName, bool Necessary)
		{
			string[] keys;
			
			for(int i=1;i<words.Length;i++){ //start from the 3rd element cos 0 is the type, and 1 is the name
				
				if(!words[i].Contains(pName+"="))
					continue;
				
				keys = StringReader.TrimWords(words[i].Split('=')); //read the type and the name
				if(keys.Length != 2)
					throw new Exception("ERROR! Invalid parameter string ("+words[i]+").");
				
				return keys[1];
			}
			if(Necessary)
				throw new Exception("ERROR! Parameter "+pName+" was not specified!");
			else
				return "notfound";
			
		}
		protected override bool   GetInitParameterCC(string[] words,string pName, out Circuit c, out Channel ch)
		{
			string[] keys;
			//double val = double.NaN;
			List<Circuit> CList = Program.Circuits;
			if(myScripter.Owner != null) //if this script is in a composite
				CList = ((composite)(myScripter.Owner)).SubCircuits;
			
			for(int i=1;i<words.Length;i++){ //start from the 2rd element cos 0 is the instruction name
				
				#region "check for bad strings"
				if(!words[i].Contains(pName+"="))
					continue;
				
				keys = StringReader.TrimWords(words[i].Split('='));//read the type and the name
				if(keys.Length != 2) //ignore if there is an invalid number of words
					continue;
				if(!keys[1].Contains(".")) //ignore invalid circ.chan words
					continue;
				 #endregion
				
				keys = StringReader.TrimWords(keys[1].Split('.')); //split at the .
				if(!Circuit.CheckCircuitChannel(keys,ChannelType.Any,CList,out c, out ch)){
					throw new Exception("ERROR! "+keys[0]+"."+keys[1]+" does not exist (fGetInitParameterCC).");
				} else
					return true;
			}
			//if we arrive here, the parameter pName was not in the input line
			throw new Exception("ERROR! Parameter "+pName+" was not specified!");
		}
		public virtual void FunctionInit() //this function converts parameters into doubles
		{
			FieldInfo[] info = this.GetType().GetFields();
			string[] words;
			
			
			isDone = false;
			isActive = true; //activate!
			
			for (int i =0 ; i < info.Length ; i++){
				//Console.WriteLine( "{0} is {1}", info[i].Name, info[i].FieldType);
				if(info[i].Name.Contains("scr_")){
					words = StringReader.TrimWords(info[i].Name.Split('_'));
					info[i].SetValue(this,GetInitParameter(Parameters,words[1],true));
					//Console.WriteLine( "{0} is set to {1}", info[i].Name, info[i].GetValue(this));
				}
			}
		}
		
	}
	
	public abstract class Function_Event : Function_Circuit
	{
		
		public Function_Event (string[] words, Scripter engine):base(words, engine){}

	}
	
	#region "waiting functions"
	public class Function_wait : Function_Event
	{
		public double scr_t;
		private double displ,speed,ptsdist;
		
		public Function_wait(string[] words, Scripter engine):base(words,engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			displ = scr_t;
			speed = scr_t*0.9;
			ptsdist = scr_t*0.1;
			
			myScripter.Write("Waiting for "+ displ.ToString() + "s: ");
		}

		
		
		public override void Update (ref double dt)
		{
			displ -= dt;
			if(displ <= speed){
				myScripter.Write("."); //print a point to let the user know that we are still alive
				speed -= ptsdist;
			}
			
            if (displ <= 0){
				myScripter.WriteLine("done");
				isActive = false; //deactivate instruction
				isDone = true;  //tell the scripter this action is finished and start the next one
			}
		}

		
	}
	public class Function_waitON : Function_Circuit
	{
		public double scr_tmax;
		private Channel Chn;
		
		private double displ,speed,ptsdist;
		
		public Function_waitON(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			displ = scr_tmax;
			speed = scr_tmax*0.9;
			ptsdist = scr_tmax*0.1;
			Circuit c;
			
			myScripter.Write("Waiting ON @ ");
			GetInitParameterCC(Parameters,"channel",out c,out Chn);
			myScripter.Write(c.Name+"."+Chn.Name+"(max "+displ.ToString()+"s): ");
		}

		
		
		public override void Update (ref double dt)
		{
			
			displ -= dt; //reduce the waiting time
			
			if(displ <= speed){
				myScripter.Write("."); //print a point to let the user know that we are still alive
				speed -= ptsdist;
			}
			
			//check if the channel is on
			if(Chn.Value > 0 ){
				myScripter.WriteLine("done");
				isActive = false; //deactivate instruction
				isDone = true;  //tell the scripter this action is finished and start the next one
				return;
			}
			
			
            if (displ <= 0){ //time is up!
				myScripter.WriteLine("done - time's up!");
				isActive = false; //deactivate instruction
				isDone = true;  //tell the scripter this action is finished and start the next one
			}
		}

		
	}
	public class Function_waitFlat : Function_Circuit
	{
		public double scr_t;
		private Channel Chn;
		public double scr_value;
		public double scr_tol;
		
		//private bool flat = true;
		private double displ,speed;
		
		public Function_waitFlat(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			displ = scr_t;
			speed = scr_t*0.9;
			speed = 0;
			
			Circuit c;
			
			//get the channel to listen
			myScripter.Write("Waiting flat ");
			GetInitParameterCC(Parameters,"channel",out c,out Chn);
			myScripter.Write("("+scr_value.ToString()+"±"+scr_tol.ToString()+"): ");
		}

		
		
		public override void Update (ref double dt)
		{
			speed += dt; //total time counter

            if (Math.Abs(Chn.Value-scr_value) < scr_tol) 
				displ -= dt; //if in range, decrease waiting
            else 
				displ = scr_t; //reset the relaxation time
			
            //check if the time elapsed with the signal flat itz done
            if (displ <= 0){
                myScripter.WriteLine("done in "+speed.ToString()+"s");
				isActive = false; //deactivate instruction
				isDone = true;  //tell the scripter this action is finished and start the next one
				return;
            }
			
		}

		
	}
	
	#endregion
	
	#region "move holder - only for main scanner"
	public class Function_place : Function_Circuit
	{
		public double scr_x;
		public double scr_y;
		public double scr_z;
		
		public Function_place(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();

			myScripter.WriteLine("Positioning holder @ ("+scr_x.ToString()+" "+scr_y.ToString()+" "+ scr_z.ToString()+")");
		}

		
		
		public override void Update (ref double dt)
		{
			Program.myScanner.Output[0].Value = scr_x;
			Program.myScanner.Output[1].Value = scr_y;
			Program.myScanner.Output[2].Value = scr_z;
			Program.myScanner.Output[0].Signal.PushBuffer();
			Program.myScanner.Output[1].Signal.PushBuffer();
			Program.myScanner.Output[2].Signal.PushBuffer();
			
			isActive = false; //deactivate instruction
			isDone = true;  //tell the scripter this action is finished and start the next one
			
		}

		
	}
	public class Function_move : Function_Circuit
	{
		private double x;
		private double y;
		private double z;
		public double scr_v;
		private double displ, tmp, ptsdist;
		private double[] FinalPosition = new double[3];
		
		public Function_move(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			
			x = GetInitParameter(Parameters,"x",false,0);
			y = GetInitParameter(Parameters,"y",false,0);
			z = GetInitParameter(Parameters,"z",false,0);
			
			displ = Math.Sqrt(x*x + y*y + z*z);
			tmp = displ*0.9;
			ptsdist = displ*0.1;
						
			myScripter.Write("Scanner: move by "+x.ToString()+" "+y.ToString()+" "+z.ToString()+"  at speed "+scr_v.ToString()+":");
			
			FinalPosition[0] = x + Program.myScanner.Output[0].Value;
			FinalPosition[1] = y + Program.myScanner.Output[1].Value;
			FinalPosition[2] = z + Program.myScanner.Output[2].Value;
			
			x *= scr_v / displ;
			y *= scr_v / displ;
			z *= scr_v / displ;

            //now we have a move direction normalized to the speed,
            //displ is the length to travel and finalpos the exact final position
            myScripter.Write("   - final position: "+FinalPosition[0].ToString()+" "+FinalPosition[1].ToString()+" "+
			                 FinalPosition[2].ToString()+":");
			
		}

		
		
		public override void Update (ref double dt)
		{
			Program.myScanner.Output[0].Value += dt*x;
			Program.myScanner.Output[1].Value += dt*y;
			Program.myScanner.Output[2].Value += dt*z;
			
            displ -= scr_v * dt;
			if(displ <= tmp){
				tmp -= ptsdist;
				myScripter.Write(".");
			}
			
            //check if the distance was travelled
            if (displ <= 1.0e-16)
			{
				Program.myScanner.Output[0].Value = FinalPosition[0];
				Program.myScanner.Output[1].Value = FinalPosition[1];
				Program.myScanner.Output[2].Value = FinalPosition[2];
				isActive = false; //deactivate instruction
				isDone = true;  //tell the scripter this action is finished and start the next one
				myScripter.WriteLine("");
            }			
		}

		
	}	
	public class Function_moveto : Function_Circuit
	{
		private double x;
		private double y;
		private double z;
		public double scr_v;
		private double displ, tmp, ptsdist;
		private double[] FinalPosition = new double[3];
		
		public Function_moveto(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			
			x = GetInitParameter(Parameters,"x",false,Program.myScanner.Output[0].Value);
			y = GetInitParameter(Parameters,"y",false,Program.myScanner.Output[1].Value);
			z = GetInitParameter(Parameters,"z",false,Program.myScanner.Output[2].Value);
			
			myScripter.Write("Scanner: move to "+x.ToString()+" "+y.ToString()+" "+
			                 z.ToString()+"  at speed "+scr_v.ToString()+":");
			
			FinalPosition[0] = x;
			FinalPosition[1] = y;
			FinalPosition[2] = z;
			
			x -= Program.myScanner.Output[0].Value;
			y -= Program.myScanner.Output[1].Value;
			z -= Program.myScanner.Output[2].Value;

			displ = Math.Sqrt(x*x + y*y + z*z);
			tmp = displ*0.9;
			ptsdist = displ*0.1;
			
			x *= scr_v / displ;
			y *= scr_v / displ;
			z *= scr_v / displ;
			
		}

		
		
		public override void Update (ref double dt)
		{
			Program.myScanner.Output[0].Value += dt*x;
			Program.myScanner.Output[1].Value += dt*y;
			Program.myScanner.Output[2].Value += dt*z;
			
            displ -= scr_v * dt;
			if(displ <= tmp){
				tmp -= ptsdist;
				myScripter.Write(".");
			}
			
            //check if the distance was travelled
            if (displ <= 1.0e-16)
			{
				Program.myScanner.Output[0].Value = FinalPosition[0];
				Program.myScanner.Output[1].Value = FinalPosition[1];
				Program.myScanner.Output[2].Value = FinalPosition[2];
				isActive = false; //deactivate instruction
				isDone = true;  //tell the scripter this action is finished and start the next one
				myScripter.WriteLine("");
            }			
		}

		
	}	
	public class Function_scan : Function_Circuit
	{
		private double x;
		private double y;
		private double z;
		public double scr_len;
		public double scr_v;
		public double scr_pts;
		private double displ, ptsdist;
		private double[] FinalPosition = new double[3];
		private int scanPts;
		
		public Function_scan(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			
			x = GetInitParameter(Parameters,"x",false,0);
			y = GetInitParameter(Parameters,"y",false,0);
			z = GetInitParameter(Parameters,"z",false,0);
			Console.WriteLine("Scanning dir: {0} {1} {2}",x,y,z);
			double norm = Math.Sqrt(x*x + y*y + z*z);
			x /= norm;			y /= norm;			z /= norm;
			
			FinalPosition[0] = x*scr_len + Program.myScanner.Output[0].Value;
			FinalPosition[1] = y*scr_len + Program.myScanner.Output[1].Value;
			FinalPosition[2] = z*scr_len + Program.myScanner.Output[2].Value;
			
			x *= scr_v;			y *= scr_v;			z *= scr_v;
			
			displ = scr_len / (scr_pts-1); //store here the distance between the points to record
			ptsdist = displ;
			
            scanPts = 0;
		
            //record this point!
            Program.myScanner.Output[6].Value = 1.0; //give the record signal

            myScripter.Write("Scanning:");
		}

		
		
		public override void Update (ref double dt)
		{
			//check if we are in position...
            if (displ <= 1.0e-16){ //the dist between 2 points has been travelled
				displ = ptsdist - displ; //reset the distance to travel to go to the next point
                scanPts++;
                Program.myScanner.Output[6].Value = 1.0; //give the record signal
                myScripter.Write(".");
            } else {
                Program.myScanner.Output[6].Value = 0.0; //clear the record signal
            }

            if (scanPts == ((int)scr_pts) - 1){ //if all points were recorded (except the last one but we are on it!)
                myScripter.WriteLine(" done");
				for(int i=0;i<3;i++) //put the cantilever(scanner) where it is supposed to be
					Program.myScanner.Output[i].Value = FinalPosition[i];
				
				isActive = false; //deactivate instruction
				isDone = true;  //tell the scripter this action is finished and start the next one
				
                Program.myScanner.StopRecord = true; //tell the scanner to stop recording on the next timestep
                return;
            }

            //move along the line...
            displ -= dt * scr_v;
			Program.myScanner.Output[0].Value += dt*x; //set the position in the scanner channels
			Program.myScanner.Output[1].Value += dt*y;
			Program.myScanner.Output[2].Value += dt*z;	
		}

		
	}		
	public class Function_scanimg : Function_Circuit
	{
		public double scr_lenfast; //fastscan is y
		public double scr_lenslow; //slowscan is x
		public double scr_v;
		protected double vRepo;
		public double scr_pts;
		public double scr_lines;
		private double displfast, ptsdist, displslow, linedist;
		protected double StartFast, FinalFast;
		protected double StartSlow, FinalSlow;
		private int scanPts, scanLine = 0;
		private int Phase = 0; //0->fastscan, 1->goback, 2-> goup
		
		public Function_scanimg(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			
			vRepo = GetInitParameter(Parameters,"vback",false,10*scr_v); //reposition speed is 10v by default
			
			displfast = scr_lenfast / (scr_pts-1); //store here the distance between the points to record
			ptsdist = displfast;
			
			displslow = scr_lenslow / (scr_lines-1); //store here the distance between the points to record
			linedist = displslow;
			
			StartFast = Program.myScanner.Output[1].Value;
			FinalFast = scr_lenfast + Program.myScanner.Output[1].Value;
			
			StartSlow = Program.myScanner.Output[0].Value;
			FinalSlow = scr_lenslow + Program.myScanner.Output[0].Value;
			
			
            scanPts = 0;
		
            //record this point!
            Program.myScanner.Output[6].Value = 1.0; //give the record signal

            myScripter.Write("Scanning:");
		}
		protected virtual void DoSlowScan(ref double dt)
		{
			//this is just a moving instruction towards the beginning of the line
			//and upwards on x: total displacement (dx,dy) = (-scr_lenfast, + scr_lenslow)
			
			if(Phase == 1){
				
				displfast -= dt * vRepo;
				Program.myScanner.Output[1].Value -= dt*vRepo;
				
				if(displfast <= 1.0e-16){ //if the tip is gone back the right length,
					displfast = ptsdist;
					Phase = 2; //goto phase 2 = goup
					Program.myScanner.Output[1].Value = StartFast;
					Console.WriteLine("going up...");
				}
			}
			
			if(Phase == 2){ //move up
				
				displslow -= dt * vRepo;
				Program.myScanner.Output[0].Value += dt*vRepo;
				
				if(displslow <= 1.0e-16){
					Phase = 0;
					displslow = linedist;
					StartSlow += linedist;
					Program.myScanner.Output[0].Value = StartSlow;
					scanLine++;
					displfast = ptsdist;
					scanPts = 0;
					Console.WriteLine("going up done\nNewscan:");
					Program.myScanner.Output[6].Value = 1.0; //give the record signal
				}
				
			}
						
		}
		protected virtual void DoFastScan(ref double dt)
		{
			//check if we are in position... record the point
            if (displfast <= 1.0e-16){ //the dist between 2 points has been travelled
				displfast = ptsdist - displfast; //reset the distance to travel to go to the next point
                scanPts++;
                Program.myScanner.Output[6].Value = 1.0; //give the record signal
                myScripter.Write(".");
            } else {
                Program.myScanner.Output[6].Value = 0.0; //clear the record signal
            }

            if (scanPts == ((int)scr_pts) - 1){ //if all points were recorded (except the last one but we are on it!)
                myScripter.WriteLine(" done");
				Program.myScanner.Output[1].Value = FinalFast;
				
                Program.myScanner.StopRecord = true; //tell the scanner to stop recording on the next timestep
				Phase = 1; //switch to phase 1 = move the tip back
				
				displfast = scr_lenfast;
				
                return;
            }

            //move along the line...
            displfast -= dt * scr_v;
			Program.myScanner.Output[1].Value += dt*scr_v; //set the position in the scanner channels
		}
		
		
		public override void Update (ref double dt)
		{
			
			if(Phase == 0)
				DoFastScan(ref dt);
			else
				DoSlowScan(ref dt);
			
			
			if(scanLine > scr_lines){
				isActive = false; //deactivate instruction
				isDone = true;  //tell the scripter this action is finished and start the next one
				myScripter.WriteLine(" done");
				Program.myScanner.StopRecord = true; //tell the scanner to stop recording on the next timestep
			}
			

		}

		
	}		
	public class Function_goto : Function_Circuit
	{
		public double scr_times;
		public double scr_func;
		private int counter;
		public Function_goto(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			counter = (int)scr_times;
		}
		
		public override void Update (ref double dt)
		{
			counter--;
			isDone = true;  //tell the scripter this action is finished and start the next one
			
			if(counter == -1) //if itz the last one
				isActive = false; //deactivate instruction
			else{
				myScripter.scriptIdx = ((int)scr_func) - 1;
				myScripter.WriteLine("GOTO "+((int)scr_func).ToString()+"  ("+counter.ToString()+")");
			}
		}
	}

	
	#endregion
	
	#region "channel operations"
	public class Function_connect : Function_Circuit
	{
		
		public Function_connect(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			string[] words,ccpair;
			string[] delims = {"->"};
			Circuit a,b;
			Channel x,y;
				
			//parse the input line and find entries containing "->"
			for(int i=1; i<Parameters.Length; i++){
				if(!Parameters[i].Contains("->")) //skip wrong inputs
					continue;
				
				#region "separate circ.out -> circ.in"
				words = StringReader.TrimWords(Parameters[i].Split(delims,StringSplitOptions.None));
				if(words.Length != 2) //skip wrong inputs
					continue;
				#endregion
				
				#region "separate circ.channel"
				ccpair= StringReader.TrimWords(words[0].Split('.'));
				if(!Circuit.CheckCircuitChannel(ccpair,ChannelType.Output,out a,out x))
					throw new Exception("ERROR! Unable to make connection: "+words[0]+" not found.");
				
				ccpair= StringReader.TrimWords(words[1].Split('.'));
				if(!Circuit.CheckCircuitChannel(ccpair,ChannelType.Input,out b,out y))
					throw new Exception("ERROR! Unable to make connection: "+words[1]+" not found.");
				
				#endregion
			
				b.Connect(x,ccpair[1]); //make the connection
				
			}
			
			/*
			if(Owner != null){ //if this scripter is in a composite...
				
			}else{//if this is the main scripter...
				if(!Program.CheckUpdateSequence(true))   //check the if the update order is screwd!
					throw new Exception("ERROR! After connecting stuff, the update order cannot be consistent any more!");
			}
			*/		
		}

		
		
		public override void Update (ref double dt)
		{

			
			isActive = false; //deactivate instruction
			isDone = true;  //tell the scripter this action is finished and start the next one
			
		}

		
	}
	public class Function_disconnect : Function_Circuit
	{
		
		public Function_disconnect(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			string[] words;
			Circuit a;
			Channel x;
			double val;
			
			myScripter.Write("Disconnecting: ");

			//parse the input line and find entries containing "->"
			for(int i=1; i<Parameters.Length; i++){
				
				#region "separate circ.in"
				words = StringReader.TrimWords(Parameters[i].Split('.'));
				if(words.Length != 2) //skip wrong inputs
					continue;
				if(!Circuit.CheckCircuitChannel(words,ChannelType.Input,out a,out x))
					throw new Exception("ERROR! Unable to disconnect: "+Parameters[i]+" not found.");
				
				#endregion
				val = x.Value; //save the value
				x.Signal = new Feed(); //renew the feed
				x.Value = val; //load the last value
			
				myScripter.Write(a.Name+"."+x.Name+" ");				
			}
			myScripter.WriteLine("");
			
			/*
			if(Owner != null){ //if this scripter is in a composite...
				
			}else{//if this is the main scripter...
				if(!Program.CheckUpdateSequence(true))   //check the if the update order is screwd!
					throw new Exception("ERROR! After disconnecting stuff, the update order cannot be consistent any more!");
			}
			*/	
		}

		
		
		public override void Update (ref double dt)
		{
			isActive = false; //deactivate instruction
			isDone = true;  //tell the scripter this action is finished and start the next one
		}

		
	}	
	public class Function_setinput : Function_Circuit
	{
		
		public Function_setinput(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			string[] words,ccpair;
			Circuit a;
			Channel x;
			
			myScripter.Write("Setting: ");
			//parse the input line and find entries containing "->"
			for(int i=1; i<Parameters.Length; i++){
				if(!Parameters[i].Contains("=")) //skip wrong inputs
					continue;
				
				#region "separate circ.in = somevalue"
				words = StringReader.TrimWords(Parameters[i].Split('='));
				if(words.Length != 2) //skip wrong inputs
					continue;
				#endregion
				
				#region "separate circ.in"
				ccpair= StringReader.TrimWords(words[0].Split('.'));
				if(!Circuit.CheckCircuitChannel(ccpair,ChannelType.Input,out a,out x))
					throw new Exception("ERROR! Unable to set input: "+words[0]+" not found.");				
				#endregion
				
				x.Value = GetInitParameter(Parameters,words[0],true);
				x.Signal.PushBuffer();
				myScripter.Write(words[0]+" <~ "+x.Value.ToString()+"  ");
				
			}
			myScripter.WriteLine("");
					
		}

		
		
		public override void Update (ref double dt)
		{
			isActive = false; //deactivate instruction
			isDone = true;  //tell the scripter this action is finished and start the next one
			
		}

		
	}
	/*
	public class Function_priority : Function_Circuit
	{
		
		public Function_priority(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			string[] words;
			Circuit a;
			Channel x;
			
			GetInitParameterCC(Parameters,"channel",out a,out x);
			bool flag = GetInitParameter(Parameters,"value",true)==1;
			
			myScripter.Write("set priority channel "+a.Name+"."+x.Name+": "+flag.ToString());
			x.Priority = flag;
			
			myScripter.WriteLine("");
					
		}

		
		
		public override void Update (ref double dt)
		{
			isActive = false; //deactivate instruction
			isDone = true;  //tell the scripter this action is finished and start the next one
			
		}

		
	}
	*/
	#endregion
	
	#region "output system"
	public class Function_write : Function_Circuit
	{
		
		public Function_write(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			string[] words;
			Circuit a;
			Channel x;
			
			if(!Parameters[1].Contains("channel=")) //skip wrong inputs
				throw new Exception("ERROR! An output channel for writing has to be specified!");
			
			int ch = (int)GetInitParameter(Parameters,"channel",true);
			myScripter.Write("write on channel "+ch.ToString()+": ");
			words = new string[]{"output",ch.ToString()};
			
			StringBuilder sb = new StringBuilder();
			for(int i=2;i<Parameters.Length;i++){
				sb.Append(Parameters[i]);
				sb.Append(" ");
			}
			
			if(ch<0)
				myScripter.WriteLine(sb.ToString());
			else{
				if(!CheckCircuitChannel(words,ChannelType.Input,out a,out x))
					throw new Exception("ERROR! Output channel not found!");
				Program.Out.WriteChannel(ch,sb.ToString());
				myScripter.WriteLine("");
			}
					
		}

		
		
		public override void Update (ref double dt)
		{
			isActive = false; //deactivate instruction
			isDone = true;  //tell the scripter this action is finished and start the next one
			
		}

		
	}
	public class Function_rmout : Function_Circuit
	{
		
		public Function_rmout(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
			string[] words;
			Circuit a;
			Channel x;
					
			int ch = (int)GetInitParameter(Parameters,"channel",true); //channel number to rename
			string basename = GetInitParameterString(Parameters,"base",true); //base file name
			double id = GetInitParameter(Parameters,"n",true); //secondary part of the name
			
			//filename= basename_id.txt
			#region "create the new filenamebasename_id.txt"
			myScripter.Write("renaming output channel "+ch.ToString()+" -> ");
			words = new string[]{"output",ch.ToString()};
			
			StringBuilder sb = new StringBuilder();
			sb.Append(basename);
			sb.Append("_");
			sb.Append(id.ToString());
			sb.Append(".txt");
			myScripter.WriteLine(sb.ToString());
			#endregion
			
			words = new string[2];
			words[0] = "output";
			words[1] = ch.ToString();
			
			if(ch<0)
				throw new Exception("ERROR! The output channel you are trying to rename does not exist.");
			else{
				if(!CheckCircuitChannel(words,ChannelType.Input,out a,out x))
					throw new Exception("ERROR! Output channel not found!");
				
				Program.Out.RenameChannel(ch,sb.ToString());
				myScripter.WriteLine("");
			}
					
		}

		
		
		public override void Update (ref double dt)
		{
			isActive = false; //deactivate instruction
			isDone = true;  //tell the scripter this action is finished and start the next one
			
		}

		
	}
	#endregion
	
	#region "variable control"
	public class Function_varset : Function_Circuit
	{
		
		public Function_varset(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
						
			string varname = GetInitParameterString(Parameters,"var",true); //base file name
			double adder = GetInitParameter(Parameters,"value",true); //channel number to rename
			
			#region "find variable in dictionaries"
			if(myScripter.Owner != null){ //if the scripter is in a composite
				//try to parse the local variables first
				if( ((composite)myScripter.Owner).Locals.ContainsKey(varname) ){
					myScripter.WriteLine("variable "+varname+" (local) set to "+adder.ToString());
					((composite)myScripter.Owner).Locals[varname] = adder;
					return;
				}
			}
			//if the code arrives here, the variable was not a local one or the scripter was not in a composite
			if(Program.Globals.ContainsKey(varname)){
				myScripter.WriteLine("variable "+varname+" (global) set to "+adder.ToString());
				Program.Globals[varname] = adder;
			}else{
				//the variable was not found
				myScripter.WriteLine("ERROR! Variable "+varname+" was not found. Ignoring instruction!");
			}
			
			#endregion
								
		}

		
		
		public override void Update (ref double dt)
		{
			isActive = false; //deactivate instruction
			isDone = true;  //tell the scripter this action is finished and start the next one
			
		}

		
	}
	public class Function_varadd : Function_Circuit
	{
		
		public Function_varadd(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
						
			string varname = GetInitParameterString(Parameters,"var",true); //base file name
			double adder = GetInitParameter(Parameters,"value",true); //channel number to rename
			
			#region "find variable in dictionaries"
			if(myScripter.Owner != null){ //if the scripter is in a composite
				//try to parse the local variables first
				if( ((composite)myScripter.Owner).Locals.ContainsKey(varname) ){
					adder = adder + ((composite)myScripter.Owner).Locals[varname];
					myScripter.WriteLine("variable "+varname+" (local) set to "+adder.ToString());
					((composite)myScripter.Owner).Locals[varname] = adder;
					return;
				}
			}
			//if the code arrives here, the variable was not a local one or the scripter was not in a composite
			if(Program.Globals.ContainsKey(varname)){
				adder = adder + Program.Globals[varname];
				myScripter.WriteLine("variable "+varname+" (global) set to "+adder.ToString());
				Program.Globals[varname] = adder;
			}else{
				//the variable was not found
				myScripter.WriteLine("ERROR! Variable "+varname+" was not found. Ignoring instruction!");
			}
			
			#endregion
								
		}

		
		
		public override void Update (ref double dt)
		{
			isActive = false; //deactivate instruction
			isDone = true;  //tell the scripter this action is finished and start the next one
			
		}

		
	}
	public class Function_varmul : Function_Circuit
	{
		
		public Function_varmul(string[] words, Scripter engine):base(words, engine)
		{}
		
		public override void FunctionInit ()
		{
			base.FunctionInit ();
						
			string varname = GetInitParameterString(Parameters,"var",true); //base file name
			double adder = GetInitParameter(Parameters,"value",true); //channel number to rename
			
			#region "find variable in dictionaries"
			if(myScripter.Owner != null){ //if the scripter is in a composite
				//try to parse the local variables first
				if( ((composite)myScripter.Owner).Locals.ContainsKey(varname) ){
					adder = adder * ((composite)myScripter.Owner).Locals[varname];
					myScripter.WriteLine("variable "+varname+" (local) set to "+adder.ToString());
					((composite)myScripter.Owner).Locals[varname] = adder;
					return;
				}
			}
			//if the code arrives here, the variable was not a local one or the scripter was not in a composite
			if(Program.Globals.ContainsKey(varname)){
				adder = adder * Program.Globals[varname];
				myScripter.WriteLine("variable "+varname+" (global) set to "+adder.ToString());
				Program.Globals[varname] = adder;
			}else{
				//the variable was not found
				myScripter.WriteLine("ERROR! Variable "+varname+" was not found. Ignoring instruction!");
			}
			
			#endregion
								
		}

		
		
		public override void Update (ref double dt)
		{
			isActive = false; //deactivate instruction
			isDone = true;  //tell the scripter this action is finished and start the next one
			
		}

		
	}
	#endregion
	
}
