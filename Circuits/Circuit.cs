using System;using vafmpro.Circuits;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;

namespace vafmpro.AbsCircuits
{

    public class Feed
    {
		private double buffered;
        private double v;
        public Circuit Owner = null;
		
		public double Value
		{
			get { return v; } //on get u get the real value at t
			set { buffered = value; } //on set, u write in the buffered value
		}
		public void PushBuffer()
		{
			v = buffered; //set the value readed by circuits to the buffered one
		}
		public double GetBufferedValue()
		{
			return buffered;
		}
    }

    public class Channel
    {
        public string Name;
        //public Circuit Owner;
        //public bool Priority;

        public Feed Signal;
		
        //the owner of the channel is the circuit that contains it.
        //when an input is connected to some output, the channel stays the
        //same but the feed becomes a reference to the source channel's feed.
		//the owner of the feed is always the original circuit and cannot change in time. EVAAAR!

        public Channel(string name, Circuit main)
        {
            Name = name;
            //Owner = main;
            //Priority = true;// by default channels are needed for the owner circuit to be updated

            Signal = new Feed();
            Signal.Owner = main; //input channels are created with null as owner!
            Value = 0.0;
        }
		
		//if the channel is secondary, it means itz involved in some feedback loop!
		//now the issue of the ticks becomes severe: if circuit B generates a tick depending on the output of A
		//and B should use this tick, B will not see the tick as it lasts only until the end of the cycle.
		
		
		public double Value	{
            get { return Signal.Value; } //on get just get the value
            set { Signal.Value = value; }
        }
		
		
    }

    
	public abstract class Circuit
    {
		public string Name;
        public bool Updated = false;
		public bool PushOut = false;
		
        public List<Channel> Input;
        public List<Channel> Output;
        public Circuit Owner; //ONLY FOR SUBCIRCUITS IN COMPOSITES
        
		protected string[] InitWords;
		
		
		#region "standard constructors"
		public virtual void SetUp() {
			InputDefaultInit();
		}
		
        public void Init(string[] words)
        {
			InitWords = words;
			Console.WriteLine("Creating circuit {0}...",this.GetType());
            Name = words[1];
            Updated = false; //by default the circuit is not yet updated
            Input=new List<Channel>();
            Output = new List<Channel>();
			
			
			//Console.WriteLine("dbg: my owner is: {0}",Owner.Name);
			
			/*
			if(words[0] == "program")
				Owner = null;
			else{
				Circuit.FindCircuit(words[0],Program.Circuits,out Owner);
				//Owner = Program.Circuits.Find(circ => circ.Name == words[0]);
				//Console.WriteLine("CIRC INIT: word0 not program: {0}",words[0]);
			}
			//Console.WriteLine("CIRC INIT: owner is null?{0}",Owner == null);
			*/
        }
		public void Init(string[] words, int chI,int chO)
		{
			InitWords = words;
			Console.WriteLine("Creating circuit {0}...",this.GetType());
			Name = words[1];
			Updated = false; //by default the circuit is not yet updated
			Input = new List<Channel>(chI);
			Output= new List<Channel>(chO);
			
			//Console.WriteLine("dbg: my owner is: {0}",Owner.Name);
			/*
			if(words[0] == "program")
				Owner = null;
			else
				Owner = Program.Circuits.Find(circ => circ.Name == words[0]);
			 */
		}
		

        #endregion
		
		
		

		#region "Initialization"
		
		 
		
		//get a numerical value out of the input string
		protected virtual double GetInitParameter(string[] words, string pName, bool Necessary)
		{
			string[] keys;
			double val = double.NaN;
			
			for(int i=2;i<words.Length;i++){ //start from the 3rd element cos 0 is the type, and 1 is the name
				
				//Console.WriteLine("parser: parsing {0}",words[i]);
				
				if(!words[i].Contains(pName+"="))
					continue;
				
				keys = StringReader.TrimWords(words[i].Split('='));//read the type and the name
				//Console.WriteLine("parser: splitting {0}",words[i]);
				#region "try parse double/local var/global var/"
				
				if(!double.TryParse(keys[1], out val)){ //if not numeric
					//Console.WriteLine("parser: Not A DOUBLE! {0}",words[i]);
					
					//try a local variable
					val = Program.GetVariable(this,keys[1]);
					
					/*
					//if(this.GetType() == typeof(composite)){
					if(Owner != null){ // (only for composites)
						//Console.WriteLine("parser: owner is not null {0}",Owner.Name);
						if( ((composite)Owner).Locals.ContainsKey(keys[1])){
							val = ((composite)Owner).Locals[keys[1]];
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
					*/
					
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
				return double.NaN;
			
		}
		protected virtual double GetInitParameter(string[] words, string pName, bool Necessary, double defval)
		{
			string[] keys;
			double val = double.NaN;
			
			
			for(int i=2;i<words.Length;i++){ //start from the 3rd element cos 0 is the type, and 1 is the name
				
				if(!words[i].Contains(pName+"="))
					continue;
				
				keys = StringReader.TrimWords(words[i].Split('='));//read the type and the name
				
				#region "try parse double/local var/global var/"
				
				if(!double.TryParse(keys[1], out val)){ //if not numeric
					
					//try a local variable
					val = Program.GetVariable(this,keys[1]);
					/*
					if(Owner != null){ // (only for composites)
						if( ((composite)Owner).Locals.ContainsKey(keys[1]))
							val = ((composite)Owner).Locals[keys[1]];
					}else{
						//try a global variable
						if(Program.Globals.ContainsKey(keys[1])){
							val = Program.Globals[keys[1]];
						}
					}
					*/
					
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
		protected virtual string GetInitParameterString(string[] words, string pName, bool Necessary)
		{
			string[] keys;
			
			for(int i=2;i<words.Length;i++){ //start from the 3rd element cos 0 is the type, and 1 is the name
				
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
		protected virtual bool   GetInitParameterCC(string[] words,string pName, out Circuit c, out Channel ch)
		{
			string[] keys;
			//double val = double.NaN;
			List<Circuit> CList = Program.Circuits;
			if(Owner != null)
				CList = ((composite)Owner).SubCircuits;
			
			for(int i=2;i<words.Length;i++){ //start from the 3rd element cos 0 is the type, and 1 is the name
				
				if(!words[i].Contains(pName+"="))
					continue;
				
				keys = StringReader.TrimWords(words[i].Split('='));//read the type and the name
				if(keys.Length != 2) //ignore if there is an invalid number of words
					continue;
				if(!keys[1].Contains(".")) //ignore invalid circ.chan words
					continue;
				
				keys = StringReader.TrimWords(keys[1].Split('.')); //split at the .
				if(!Circuit.CheckCircuitChannel(keys,ChannelType.Any,CList,out c, out ch)){
					throw new Exception("ERROR! "+keys[0]+"."+keys[1]+" does not exist (GetInitParameterCC).");
				} else
					return true;
			}
			//if we arrive here, the parameter pName was not in the input line
			throw new Exception("ERROR! Parameter "+pName+" was not specified!");
		}
		
		
		
		/* initialize the input channels of the circuit using a list of input parameters
		 * the dictionary key is the channel to set to the corresponding value.
		 * return true if everything was awrite! false if epic fail! */
		protected void InputDefaultInit()
		{
			string[] keys;
			Channel ch;
			double val;
			
			Console.WriteLine("  {0}: Input channels default init:",Name);
			
			
			for(int i=2;i<InitWords.Length;i++){ //start from the 3rd element cos 0 is the type, and 1 is the name
				
				//look for words that contain '='
				if(!InitWords[i].Contains("="))
					continue;
				//if we are here, the word contains an =
				keys = StringReader.TrimWords(InitWords[i].Split('='));//read the type and the name
				if(keys.Length != 2)
					throw new Exception("ERROR! The initialization string is not right ("+InitWords[i]+").");
				
				#region "check for pushed buffers"
				if(keys[0] == "pushed" && keys[1] == "true"){
					PushOut = true;
					continue;
				}
                #endregion
				
				//now check if the first key is the name of an input channel
				if(!GetChannel(keys[0], ChannelType.Input, out ch, true)){
					Console.WriteLine("    WARNING! Input channel {0} not found, ignoring initialization.",keys[0]);
					continue;
				}
				//Console.WriteLine("    found input channel {0}...converting value:",keys[0]);
				
				//if we are here the channel was found! try parse key2 to get a number out of it
				#region "try parse double/local var/global var/"
				
				if(double.TryParse(keys[1], out val)){ //if numeric
					ch.Value = val;
					ch.Signal.PushBuffer();
					Console.WriteLine("    input {0} <- {1} (numeric)",ch.Name,val);
					continue;
				}
				//if the code arrives here, the value was symbolic
				val = Program.GetVariable(this,keys[1]);
				ch.Value = val;
				ch.Signal.PushBuffer();
				
				
				//at this point we tried all possibilities. print a warning
				if(double.IsNaN(val))
					Console.WriteLine("    WARNING! The given value for input channel {0} is not a number/variable and will be ignored.",ch.Name);
				#endregion
				
			}
		}
		
		
		#endregion
		
		#region "Updaters"
		
		//public virtual void PushBuffers
        public abstract void Update(ref double dt); //this has to be implemented in the derived class
		public virtual void PostUpdate()
		{
			if(PushOut)
				foreach (Channel ch in Output)
					ch.Signal.PushBuffer();			
		}
		
        public virtual void ResetUpdate()
        { Updated = false; }
		
		/*
        public bool FullUpdate(double dt)
        {

            if (Updated)
                return true;

            Console.WriteLine("trying to update circuit {0}", Name);

            //if it can be updated do it!
            if (IsUpdatable())
            {
                Update(ref dt);
                if (this.GetType() != typeof(Const)) //constants are always updated already!!
                    Updated = true;
                Console.WriteLine("CIRCUIT {0} UPDATED!",Name);
                return true;
            }
            Console.WriteLine("mhmm... the inputs are not updated tho!");
            //otherwise try to fullupdate the owners of input signals
            for (int i = 0; i < Input.Count; i++)
            {
                Circuit c = Input[i].Signal.Owner; //get the owner
                if (c.GetType() == typeof(Const)) //avoid the constants
                    continue;
                if (/*Input[i].Priority && !c.Updated) //fullupdate it if is required and it is not updated yet
                {
                    if (c.Name != this.Name)
                        c.FullUpdate(dt);
                }

            }

            return true;
        }

        public bool FakeUpdate()
        {

            if (Updated)
                return false;

            //if it can be updated do it!
            if (IsUpdatable())
            {
                Updated = true;
                return true;
            }

            return false;
        }
		*/
		/*
        public bool IsUpdatable()
        {

            //if already update return false
            if (Updated)
                return false;

            //check the inputs
            for (int i = 0; i < Input.Count; i++)
            {
                //if the owner of the signal is not updated AND that signal is needed
                //                                    this condition is here just to make it work for subcircs
                if (Input[i].Signal.Owner == null || Input[i].Signal.Owner == this.Owner) //if itz null means that this input is disconnected
                    continue;
                if (!Input[i].Signal.Owner.Updated && Input[i].Priority)
                {
                    return false;
                }

            }

            return true;
        }
		 */
		

		#endregion
		
        /// <summary>
        /// Connect the signal (feed) of source to the signal of channel dest
        /// </summary>
        /// <param name="source">Output channel of some circuit.</param>
        /// <param name="dest">Name of the receiving input channel.</param>
        /// <returns>True if the connection was successful, false otherwise.</returns>
        public bool Connect(Channel source, string dest)
        {
            //connect a source channel to a dest channel in the input
            //list of this circuit.
            if (source == null)
                return false;

            foreach (Channel ch in Input)
                if (ch.Name == dest){
                    Console.WriteLine("Connecting {0}.{1} -> {2}.{3}.",source.Signal.Owner.Name, source.Name, Name,dest);
                    ch.Signal = source.Signal;
                    return true;
                }
            
            return false;
        }
				
		#region "Methods to find circuits and/or channels"
		
		/// <summary>
        /// Find a channel in this circuit in the main program's circuit list, and set it as out.
        /// </summary>
        /// <param name="name">Name of the channel to find.</param>
        /// <param name="type">Channel type: In, Out or any.</param>
        /// <param name="channel">Reference to the channel if it was found.</param>
        /// <returns>Returns true if the channel exists, false otherwise</returns>
		public bool GetChannel(string name, ChannelType type, out Channel channel, bool Quiet)
		{
			channel = null;
			
			#region "Input only"
			if(type == ChannelType.Input){ //look only in the input list
				
				foreach(Channel ch in Input){
					if(ch.Name == name){
						channel = ch;
						return true;
					}
				}
				if(!Quiet)
					Console.WriteLine("ERROR! Channel {0} not found in the inputs of circuit {1}.", name, this.Name);
				return false; //if we are here the channel was not found
				
			}
			#endregion
			#region "Output only"
			if(type == ChannelType.Output){ //look only in the input list
				
				foreach(Channel ch in Output){
					if(ch.Name == name){
						channel = ch;
						return true;
					}
				}
				if(!Quiet)
					Console.WriteLine("ERROR! Channel {0} not found in the outputs of circuit {1}.", name, this.Name);
				return false; //if we are here the channel was not found
				
			}
			#endregion
			#region "All channels"
			if(type == ChannelType.Any){ //look only in the input list
				
				foreach(Channel ch in Input){
					if(ch.Name == name){
						channel = ch;
						return true;
					}
				}
				foreach(Channel ch in Output){
					if(ch.Name == name){
						channel = ch;
						return true;
					}
				}
				if(!Quiet)
					Console.WriteLine("ERROR! Channel {0} not found in the channels of circuit {1}.", name, this.Name);
				return false; //if we are here the channel was not found
				
			}
			#endregion
			
			return true;
		}
		
        /// <summary>
        /// Find a circuit in the main program's circuit list, and set it as out.
        /// </summary>
        /// <param name="name">Name of the circuit to find.</param>
        /// <param name="CList">List of circuits to scan.</param>
        /// <param name="circuit">Reference to the circuit if it was found.</param>
        /// <returns>Returns true if the circuit exists, false otherwise</returns>
		public static bool FindCircuit(string name, List<Circuit> CList, out Circuit circuit)
        {
			circuit = null;
			foreach (Circuit ch in CList){ // look in circuits
				if (ch.Name == name){
					circuit = ch;
					return true;
				}
			}
            Console.WriteLine("ERROR! Circuit {0} was not found in the list.",name);
            return false;
        }

        /// <summary>
        /// Find a pair circuit/channel.
        /// </summary>
        /// <param name="names">Array of two strings containing the name of the circuit and its channel.</param>
        /// <param name="type">Specifies wether to look in the input, output or any channel.</param>
        /// <param name="circ">Output variable: reference to the circuit if it was found, null otherwise.</param>
        /// <param name="ch">Output variable: reference to the channel if it was found, null otherwise.</param>
        /// <returns>true if the pair was found</returns>
        public static bool CheckCircuitChannel(string[] names, ChannelType type, out Circuit circ, out Channel ch)
        {
			ch = null;
            circ = null;
			if(!Circuit.FindCircuit(names[0], Program.Circuits, out circ)) //use the find circuit
				return false;
			
			if(!circ.GetChannel(names[1],type, out ch, false)){ //if the channel was not found return false
				return false;
			}
			
			return true;
		}

        /// <summary>
        /// Find a pair circuit/channel in a specific list
        /// </summary>
        /// <param name="names">Array of two strings containing the name of the circuit and its channel.</param>
        /// <param name="type">Specifies wether to look in the input, output or any channel.</param>
        /// <param name="CList">Specifies wether to look in the input, output or any channel.</param>
        /// <param name="circ">Output variable: reference to the circuit if it was found, null otherwise.</param>
        /// <param name="ch">Output variable: reference to the channel if it was found, null otherwise.</param>
        /// <returns></returns>
        public static bool CheckCircuitChannel(string[] names, ChannelType type, List<Circuit> CList, out Circuit circ, out Channel ch)
        {
			ch = null;
            circ = null;
			if(!Circuit.FindCircuit(names[0], CList, out circ)) //use the find circuit
				return false;
			
			if(!circ.GetChannel(names[1],type, out ch, false)){ //if the channel was not found return false
				return false;
			}

            return true;
        }

		#endregion
		
		
		//convert a string to a number, if not a number try the values in the parameters dictionary
		public static bool ValueTryParseasd(string word, out double Value, Dictionary<string,double> parameters)
		{
			if(double.TryParse(word, out Value))
				return true;
			
			//if the code is here, word is not a number
			if(parameters != null){
				if(parameters.ContainsKey(word)){
					Value = parameters[word];
					return true;
				}
			}
			
			//if the code arrives here, there was an error converting word to double
			Console.WriteLine("ERROR! Coverting '{0}' to numerical was not possible.",word);
			Value = 0.0;
			return false;
			
		}
		
		

        /// <summary>
        /// Check if this circuit depends on c for updating.
        /// </summary>
        /// <param name="c">Circuit to check.</param>
        /// <returns>Returns true if this circuit depends directly on c.</returns>
        public bool DependsOnCircuit(Circuit c)
        {
            for (int i = 0; i < Input.Count; i++)
                if (Input[i].Name == c.Name)
                    return true;

            return false;
        }



    }
	
	public abstract class FlipFlop : Circuit
	{
		protected bool Fronted = false;
		
		private int myState = 0;
		protected int State {
			get { return myState; }
			set { myState = value; SetOutput(); return; }
		}
		
		protected bool Clock;
		private bool Clocki, ClockO = false;
		private bool tmp;
		
		public override void SetUp ()
		{
			Input.Add(new Channel("clock",null)); //the clock input
			Output.Add(new Channel("Q",this));
			Output.Add(new Channel("nQ",this));
			
			if(GetInitParameter(InitWords,"front",false,0.0) == 1)
				Fronted = true;
		}
		
		protected virtual bool GetFront(bool signal, ref bool old)
		{
			tmp = signal && !old;
			old = signal;
			return tmp;
		}
		
		protected virtual void GetClockFront()
		{
			Clocki = Input[0].Value > 0;
			Clock = Clocki && !ClockO;
			ClockO = Clocki;
		}
		
		private void SetOutput()
		{
			Output[0].Value = myState;
			Output[1].Value = (myState == 1)? 0.0:1.0;
		}
		protected void Switch()
		{
			State = (State==0)?1:0;
		}
	}
	
	public abstract class ForceInterpolator3D : Circuit
	{
		
		protected int Dim;
		protected double ForceUnits = 1; //assume nN
		protected double[,,,] DataGrid; //<-- forces here! (xyzc)
		protected double[] Force;
		
		protected double[] x = new double[3];
		protected double[] xc = new double[3];
		protected int[] xIndex=new int[3];
		
		
		//grid stuff
		protected double GridUnits = 1.0;
		protected int[] GridSize; //number of points along each direction
		protected double[] UnitSize, GridStep;
		protected int[] PBC;
		protected List<double[]> Grid;
		protected double div;
		
		protected bool ReadGrid(string ForceFile)
        {
            StreamReader reader = new StreamReader(ForceFile);
            string line="";
            string[] words;
            char[] delimiterChars = { ' ', '\t' };


			Console.WriteLine("  Reading Grid...");
			
			if (!StringReader.FindString("<grid>", reader)) {
				Console.WriteLine("ERROR! Parameters for the grid were not found.");
				return false;
			}

			#region "force dimension"
            if (!StringReader.FindStringNoEnd("components", reader, "<grid>", ref line,ForceFile)){
                Console.WriteLine("FATAL! Specify the amount of components for the field.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 2){
                Console.WriteLine("FATAL! Specify the amount of components for the field.");
                return false;
            }
			if(!int.TryParse(words[1], out Dim)){
				Console.WriteLine("FATAL! You have to specity a numerical dimension for the forcefield.");
				return false;
			}
			Console.WriteLine("  Forcefield components: {0}",Dim);
			
            #endregion
            #region "force units"
            ForceUnits = 1; //assume nN
            if (!StringReader.FindStringNoEnd("forceunits", reader, "<grid>", ref line,ForceFile))
                Console.WriteLine("  Forcefield units are not specified, assuming nN (1).");
            else {
                words = StringReader.TrimWords(line.Split(delimiterChars));
				
				if (words.Length < 2)
					Console.WriteLine("  Forcefield units not specified, assuming nN (1).");
				else{
					ForceUnits = Convert.ToDouble(words[1]);
					Console.WriteLine("  Forcefield units: {0}", ForceUnits);
				}
			}
			#endregion
			
            #region "gridPoints"
			GridSize = new int[3];for(int i=0;i<3;i++)GridSize[i]=1;
			
            if (!StringReader.FindStringNoEnd("points", reader, "<grid>", ref line, ForceFile)) {
				Console.WriteLine("FATAL! The grid size have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 4) {
                Console.WriteLine("FATAL! You have to specity the amount of points along each direction X Y Z.");
                return false;
            }
			
			for (int i = 0; i < 3; i++){ //read 3 quantities
				if (!Int32.TryParse(words[i + 1], out GridSize[i])) {
					Console.WriteLine("FATAL! The amount of grid points has to be integer!");
					return false;
				}
            }
			#endregion
            #region "UnitSize"
			UnitSize = new double[2];
            if (!StringReader.FindStringNoEnd("unitcell", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("FATAL! The grid size have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 3)
            {
                Console.WriteLine("FATAL! You have to specity the unit cell size along each direction X Y.");
                return false;
            }
            for (int i = 0; i < 2; i++){
                if (!double.TryParse(words[i + 1], out UnitSize[i])) {
                    Console.WriteLine("FATAL! The unit cell size has to be float!");
                    return false;
                }
            }
            #endregion
			#region "gridunits"
            GridUnits = 1;
			if (!StringReader.FindStringNoEnd("gridunits", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("INFO! Grid units are not specified, assuming nm (1).");
            }
			else
			{   //IMPORTANT!!!!
                words = StringReader.TrimWords(line.Split(delimiterChars));
                if (words.Length > 1)
                {
					//try the known words
					if (words[1] == "ang")
						GridUnits = 0.1;
					else if (words[1] == "nm")
						GridUnits = 1.0;
                    else if (words[1] == "m")
                        GridUnits = 1.0e9;
					else if(words[1] == "bohr")
						GridUnits = 52.9177e-3;
                    else{
						if(!double.TryParse(words[1], out GridUnits))
							Console.WriteLine("INFO! Unit type not recognized, assuming nm (1).");
					}
				}
				else
					Console.WriteLine("INFO! Unit type not recognized, assuming nm (1).");
				Console.WriteLine("  Forcefield grid units: {0}",GridUnits);
				
            }
            #endregion
			/* GRID MIN HAS BEEN DEPRECATED! GRIDS START FROM 0!
            #region "gridmin"
			GridMin = new double[3];
            if (!StringReader.FindStringNoEnd("gridmin", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("FATAL! The grid starting points have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 4)
            {
                Console.WriteLine("FATAL! You have to specity the grid starting points along each direction X Y Z.");
                return false;
            }
            for (int i = 0; i < 3; i++)
            {
                if (!double.TryParse(words[i + 1], out GridMin[i]))
                {
                    Console.WriteLine("FATAL! The grid starting points have to be float!");
                    return false;
                }

            }
            #endregion
            */
            #region "gridstp"
			GridStep = new double[3];
            if (!StringReader.FindStringNoEnd("gridstp", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("FATAL! The grid steps  have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 4)
            {
                Console.WriteLine("FATAL! You have to specity the grid steps along each direction X Y Z.");
                return false;
            }
            for (int i = 0; i < 3; i++)
            {
                if (!double.TryParse(words[i + 1], out GridStep[i]))
                {
                    Console.WriteLine("FATAL! The grid steps have to be float!");
                    return false;
                }

            }
            #endregion
            #region "periodicity"
			PBC = new int[2]; PBC[0] = 0; PBC[1] = 0;
            if (!StringReader.FindStringNoEnd("periodicity", reader, "<grid>", ref line, ForceFile)){
                Console.WriteLine("INFO! Periodicity is not specified, assuming PBC in both X and Y.");
                return true;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 3){
                Console.WriteLine("INFO! Periodicity is not specified correctly, assuming PBC in both X and Y.");
                return true;
            }for (int i = 0; i < 2; i++){
                if (words[i+1] == "pacman")
                    PBC[i] = 0;
                else if (words[i+1] == "mirror")
                    PBC[i] = 1;
                else{
                    Console.WriteLine("INFO! Periodicity is not specified correctly, assuming PBC.");
                    PBC[i] = 1;
                }
            }
            #endregion
            
			MakeGrid();
            reader.Dispose();
            return true;
        }
		abstract protected void MakeGrid();
		
		
		abstract protected void Interpolate();
		
		protected int Evaluate()
		{
			//Console.WriteLine("gts {0} {1} {2}",x[0],x[1],x[2]);
			for(int i=0;i<Dim;i++)// reset the force
				Force[i] = 0;
			
			if (x[2] >= Grid[2][Grid[2].Length - 1]){ //if tip too high do not extrapolate
				//Console.WriteLine("too high! {0}",Grid[2].Length);
				return 1;
			}
			if(x[2]<Grid[2][0]){
				Console.WriteLine("ERROR! The tip height ({0}) is lower then the minimum gridpoint ({1}).",x[2],Grid[2][0]);
				return 0; //no error on tip crash
				//throw new Exception("Tip crashed?");
			}
			//Console.WriteLine("ok!");
			Interpolate();
			return 1;
		}
	
		//find the grid voxel where the tip is
        protected void FindVoxel()
        {
			/*
            int i;
            for (int c = 0; c < 3; c++)
            {
                for (i = 0; i < Grid[c].Length; i++)
                    if (Grid[c][i] > xc[c])
                        break;
                xIndex[c] = i-1;
				if((xIndex[c]<0) || (xIndex[c] > GridSize[c] -1)){
					Console.WriteLine("WTF?! {0} {1} ->  x:{2} {3} {4} !!!",c,xIndex[c],x[0],x[1],x[2]);
					Console.WriteLine("WTF?! {0} {1} ->  x:{2} {3} {4} !!!",c,xIndex[c],xc[0],xc[1],xc[2]);
				}
            }
			*/
			
			for (int c = 0; c < 3; c++)
				xIndex[c] = (int)Math.Floor(xc[c]/GridStep[c]);
			
			//Console.WriteLine("VOXEL is {0} {1} {2} ({3} {4} {5})",xIndex[0],xIndex[1],xIndex[2],xc[0],xc[1],xc[2]);
			
        }
        protected void FindVoxel(out int ii, out int jj, out int kk)
        {
            int i;
			
			for (i = 0; i < Grid[0].Length; i++)
				if (Grid[0][i] > xc[0])
					break;
			ii = i-1; xIndex[0] = ii;
			for (i = 0; i < Grid[1].Length; i++)
				if (Grid[1][i] > xc[1])
					break;
			jj = i-1; xIndex[1] = jj;
			for (i = 0; i < Grid[2].Length; i++)
				if (Grid[2][i] > xc[2])
					break;
			kk = i-1; xIndex[2] = kk;
			
			for (int c = 0; c < 3; c++)		
			if((xIndex[c]<0) || (xIndex[c] > Grid[c].Length -1)){
				Console.WriteLine("WTF?! {0} {1} ->  x:{2} {3} {4} !!!",c,xIndex[c],x[0],x[1],x[2]);
				Console.WriteLine("WTF?! {0} {1} ->  x:{2} {3} {4} !!!",c,xIndex[c],xc[0],xc[1],xc[2]);
			}
			
			//Console.WriteLine("VOXEL is {0} {1} {2} ({3} {4} {5})",xIndex[0],xIndex[1],xIndex[2],xc[0],xc[1],xc[2]);
			
        }
		//center the cursor in the unit cell
        protected void CenterCursor()
		{
			#region "X direction"
            if (PBC[0] == 0){ //pacman symmetry
				div = Math.Floor(x[0] / UnitSize[0]);
				//Console.WriteLine("ceiling: {0} = {1}",x[0],div);
				xc[0] = x[0] - div * UnitSize[0];
			}
            else{ //mirror
				xc[0]=x[0];
				if(x[0]<0) xc[0]=-x[0];      //if the point is in the negative side, flip it!
				
				do{
					if(xc[0] >= 2.0*UnitSize[0]){
						xc[0]=xc[0]-(2.0* Grid[0][GridSize[0]]);      //this centers the point in the range [0,2L]
					}
					else{
						if( xc[0] > Grid[0][GridSize[0]] ){
							xc[0]=2.0d*Grid[0][GridSize[0]]-xc[0];
						}
						break;
					}
				}while(true);
				
            }
			#endregion
			
			#region "Y direction"
			if(PBC[1] == 0) //pacman symmetry
			{
				div = Math.Floor(x[1] / UnitSize[1]);
				xc[1] = x[1] - div*UnitSize[1];
				//Console.WriteLine("ceiling: {0}   {1}   {2} ... gstep {3}",x[1],div,xc[1],Math.Ceiling(x[1]/GridStep[1]));
			}
			else{
				xc[1]=x[1];
				if(x[1]<0) xc[1]=-x[1];      //if the point is in the negative side, flip it!
				
				do{
					if(xc[1] >= 2.0*UnitSize[1]){
						xc[1]=xc[1]-(2.0* Grid[1][GridSize[1]]);      //this centers the point in the range [0,2L]
					}
					else{
						if( xc[1] > Grid[1][GridSize[1]] ){
							xc[1]=2.0d*Grid[1][GridSize[1]]-xc[1];
						}
						break;
					}
				}while(true);
			}
			#endregion
			
			xc[2] = x[2];

			/*
			Console.WriteLine("point {0} {1} {2} was centered in {3} {4} {5}",
			                  x[0].ToString(),x[1].ToString(),x[2].ToString(),
			                  xc[0].ToString(),xc[1].ToString(),xc[2].ToString());
			*/
			
		}
		
	
		/// <summary>
		/// Dump the interpolated field at constant height z
		/// </summary>
		public void DumpField_zfix(double z)
		{
			string field = Name+".z"+z.ToString()+".field.out";
			StreamWriter o = new StreamWriter(field);
			
			
			x[2] = z;
			double xx = -UnitSize[0];
			double yy = -UnitSize[1];
			
			for(xx = 0; xx<=2*UnitSize[0]; xx+=GridStep[0]/2){
				for(yy = 0; yy<=2*UnitSize[1]; yy+=GridStep[1]/2){
					x[0] = xx; x[1] = yy;
					Evaluate();
					o.Write("{0} {1} ", xx.ToString(), yy.ToString());
					for(int c=0;c<Dim;c++)
						o.Write("{0} ",Force[c]);
					o.WriteLine("");
				}
				o.WriteLine("");
			}
			
			
			o.Dispose();
			
			field = Name+".source.out";
			o = new StreamWriter(field);
			
			
			for(int i=0;i<GridSize[0];i++){
				for(int j=0;j<GridSize[1];j++){

					o.Write("{0} {1} ",Grid[0][i].ToString(),Grid[1][j].ToString());
					for(int c=0;c<Dim;c++)
						o.Write("{0} ",DataGrid[i,j,xIndex[2],c]);
					o.WriteLine("");
				}
				o.WriteLine("");
			}
			
			
			o.Dispose();
			
			
		}
		
	}
	public abstract class ForceInterpolator4D : Circuit
	{
		
		protected int Dim;
		protected double ForceUnits = 1; //assume nN
		protected double[,,,,] DataGrid; //<-- forces here! (xyzvc)
		protected double[] Force;
		
		protected double[] x = new double[4];
		protected double[] xc = new double[4];
		protected int[] xIndex=new int[4];
		
		
		//grid stuff
		protected double GridUnits = 1.0;
		protected int[] GridSize; //number of points along each direction
		protected double[] UnitSize, GridMin, GridStep;
		protected int[] PBC;
		protected List<double[]> Grid;
		
		
		
		protected bool ReadGrid(string ForceFile)
        {
            StreamReader reader = new StreamReader(ForceFile);
            string line="";
            string[] words;
            char[] delimiterChars = { ' ', '\t' };


			Console.WriteLine("  Reading Grid...");
			
			if (!StringReader.FindString("<grid>", reader)) {
				Console.WriteLine("ERROR! Parameters for the grid were not found.");
				return false;
			}

			#region "force dimension"
            if (!StringReader.FindStringNoEnd("forcedim", reader, "<grid>", ref line,ForceFile)){
                Console.WriteLine("FATAL! The forcefield does not have a dimension.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 2){
                Console.WriteLine("FATAL! You have to specity a dimension for the forcefield.");
                return false;
            }
			if(!int.TryParse(words[1], out Dim)){
				Console.WriteLine("FATAL! You have to specity a numerical dimension for the forcefield.");
				return false;
			}
			Console.WriteLine("  Forcefield dimension: {0}",Dim);
			
            #endregion
            #region "force units"
            ForceUnits = 1; //assume nN
            if (!StringReader.FindStringNoEnd("forceunits", reader, "<grid>", ref line,ForceFile))
                Console.WriteLine("  Forcefield units are not specified, assuming nN (1).");
            else {
                words = StringReader.TrimWords(line.Split(delimiterChars));
				
				if (words.Length < 2)
					Console.WriteLine("  Forcefield units not specified, assuming nN (1).");
				else{
					ForceUnits = Convert.ToDouble(words[1]);
					Console.WriteLine("  Forcefield units: {0}", ForceUnits);
				}
			}
			#endregion
			
            #region "gridPoints"
			GridSize = new int[4];for(int i=0;i<4;i++)GridSize[i]=1;
			
            if (!StringReader.FindStringNoEnd("points", reader, "<grid>", ref line, ForceFile)) {
				Console.WriteLine("FATAL! The grid size have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 5) {
                Console.WriteLine("FATAL! You have to specity the amount of points along each direction X Y Z and V.");
                return false;
            }
			
			for (int i = 0; i < 4; i++){ //read 4 quantities
				if (!Int32.TryParse(words[i + 1], out GridSize[i])) {
					Console.WriteLine("FATAL! The amount of grid points has to be integer!");
					return false;
				}
            }
			#endregion
            #region "UnitSize"
			UnitSize = new double[2];
            if (!StringReader.FindStringNoEnd("unitcell", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("FATAL! The grid size have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 3)
            {
                Console.WriteLine("FATAL! You have to specity the unit cell size along each direction X Y.");
                return false;
            }
            for (int i = 0; i < 2; i++){
                if (!double.TryParse(words[i + 1], out UnitSize[i])) {
                    Console.WriteLine("FATAL! The unit cell size has to be float!");
                    return false;
                }
            }
            #endregion
            #region "gridunits"
            GridUnits = 1;
            if (!StringReader.FindStringNoEnd("gridunits", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("INFO! Grid units are not specified, assuming nm.");
            }
			else
			{   //IMPORTANT!!!!
                words = StringReader.TrimWords(line.Split(delimiterChars));
                if (words.Length > 1)
                {
                    if (words[1] == "ang")
                        GridUnits = 0.1;
                    else if (words[1] == "m")
                        GridUnits = 1.0e9;
					else if(words[1] == "bohr")
						GridUnits = 52.9177e-3;
                    else
                        Console.WriteLine("INFO! Unit type not recognized, assuming nm.");
					
                }

            }
            #endregion
            #region "gridmin"
			GridMin = new double[4];
            if (!StringReader.FindStringNoEnd("gridmin", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("FATAL! The grid starting points have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 5)
            {
                Console.WriteLine("FATAL! You have to specity the grid starting points along each direction X Y Z V.");
                return false;
            }
            for (int i = 0; i < 4; i++){
				if (!double.TryParse(words[i + 1], out GridMin[i])){
					Console.WriteLine("FATAL! The grid starting points have to be float!");
					return false;
                }

            }
            #endregion
            #region "gridstp"
			GridStep = new double[4];
            if (!StringReader.FindStringNoEnd("gridstp", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("FATAL! The grid steps  have to be specified.");
                return false;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 5){
                Console.WriteLine("FATAL! You have to specity the grid steps along each direction X Y Z V.");
                return false;
            }
            for (int i = 0; i < 4; i++){
                if (!double.TryParse(words[i + 1], out GridStep[i])){
                    Console.WriteLine("FATAL! The grid steps have to be float!");
                    return false;
                }

            }
            #endregion
            #region "periodicity"
			PBC = new int[2]; PBC[0] = 0; PBC[1] = 0;
            if (!StringReader.FindStringNoEnd("periodicity", reader, "<grid>", ref line, ForceFile))
            {
                Console.WriteLine("INFO! Periodicity is not specified, assuming PBC in both X and Y.");
                return true;
            }
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 3)
            {
                Console.WriteLine("INFO! Periodicity is not specified correctly, assuming PBC in both X and Y.");
                return true;
            }
            for (int i = 0; i < 2; i++)
            {
                if (words[i+1] == "pacman")
                    PBC[i] = 0;
                else if (words[i+1] == "mirror")
                    PBC[i] = 1;
                else
                {
                    Console.WriteLine("INFO! Periodicity is not specified correctly, assuming PBC.");
                    PBC[i] = 1;
                }
            }
            #endregion
            
			MakeGrid();
            reader.Dispose();
            return true;
        }
		abstract protected void MakeGrid();
		
		
		abstract protected void Interpolate();
		
		protected int Evaluate()
		{
			//Console.WriteLine("gts {0} {1} {2}",x[0],x[1],x[2]);
			for(int i=0;i<Dim;i++)
				Force[i] = 0;
            if (x[2] >= Grid[2][Grid[2].Length - 1]) //if tip too high do not extrapolate
                return 1;
			if(x[2]<Grid[2][0]){ //if tip too close
				Console.WriteLine("ERROR! The tip height ({0}) is lower then the minimum gridpoint.",x[2]);
				throw new Exception("Tip crashed?");
			}
			
			
			Interpolate();
			return 1;
		}
	
		//find the grid voxel where the tip is
        protected void FindVoxel()
        {
            int i;
            for (int c = 0; c < 4; c++){
				for (i = 0; i < Grid[c].Length; i++)
                    if (Grid[c][i] > xc[c])
                        break;
                xIndex[c] = i-1;
				if((xIndex[c]<0) || (xIndex[c] > Grid[c].Length -1)){
					Console.WriteLine("WTF?! {0} {1} ->  x:{2} {3} {4} v({5}) !!!",c,xIndex[c],x[0],x[1],x[2],x[3]);
					Console.WriteLine("WTF?! {0} {1} ->  x:{2} {3} {4} v({5}) !!!",c,xIndex[c],xc[0],xc[1],xc[2],xc[3]);
				}
            }
			
			//Console.WriteLine("VOXEL is {0} {1} {2} ({3} {4} {5})",xIndex[0],xIndex[1],xIndex[2],xc[0],xc[1],xc[2]);
			
        }
        protected void FindVoxel(out int ii, out int jj, out int kk, out int vv)
        {
            int i;
			
			for (i = 0; i < Grid[0].Length; i++)
				if (Grid[0][i] > xc[0])
					break;
			ii = i-1; xIndex[0] = ii;
			for (i = 0; i < Grid[1].Length; i++)
				if (Grid[1][i] > xc[1])
					break;
			jj = i-1; xIndex[1] = jj;
			for (i = 0; i < Grid[2].Length; i++)
				if (Grid[2][i] > xc[2])
					break;
			kk = i-1; xIndex[2] = kk;
			for (i = 0; i < Grid[3].Length; i++)
				if (Grid[3][i] > xc[3])
					break;
			vv = i-1; xIndex[3] = vv;
			
			for (int c = 0; c < 3; c++)		
			if((xIndex[c]<0) || (xIndex[c] > Grid[c].Length -1)){
				Console.WriteLine("WTF?! {0} {1} ->  x:{2} {3} {4} v({5}) !!!",c,xIndex[c],x[0],x[1],x[2],x[3]);
				Console.WriteLine("WTF?! {0} {1} ->  x:{2} {3} {4} v({5}) !!!",c,xIndex[c],xc[0],xc[1],xc[2],xc[3]);
			}
			
			//Console.WriteLine("VOXEL is {0} {1} {2} ({3} {4} {5})",xIndex[0],xIndex[1],xIndex[2],xc[0],xc[1],xc[2]);
			
        }
		//center the cursor in the unit cell
        protected void CenterCursor()
		{
			#region "X direction"
            if (PBC[0] == 0){ //pacman symmetry
				double div = Math.Floor(x[0] / UnitSize[0]);
				//Console.WriteLine("ceiling: {0} = {1}",x[0],div);
				xc[0] = x[0] - div * UnitSize[0];
			}
            else{ //mirror
				xc[0]=x[0];
				if(x[0]<0) xc[0]=-x[0];      //if the point is in the negative side, flip it!
				
				do{
					if(xc[0] >= 2.0*UnitSize[0]){
						xc[0]=xc[0]-(2.0* Grid[0][GridSize[0]]);      //this centers the point in the range [0,2L]
					}
					else{
						if( xc[0] > Grid[0][GridSize[0]] ){
							xc[0]=2.0d*Grid[0][GridSize[0]]-xc[0];
						}
						break;
					}
				}while(true);
				
            }
			#endregion
			
			#region "Y direction"
			if(PBC[1] == 0) //pacman symmetry
			{
				double div = Math.Floor(x[1] / UnitSize[1]);
				xc[1] = x[1] - div*UnitSize[1];
				//Console.WriteLine("ceiling: {0}   {1}   {2} ... gstep {3}",x[1],div,xc[1],Math.Ceiling(x[1]/GridStep[1]));
			}
			else{
				xc[1]=x[1];
				if(x[1]<0) xc[1]=-x[1];      //if the point is in the negative side, flip it!
				
				do{
					if(xc[1] >= 2.0*UnitSize[1]){
						xc[1]=xc[1]-(2.0* Grid[1][GridSize[1]]);      //this centers the point in the range [0,2L]
					}
					else{
						if( xc[1] > Grid[1][GridSize[1]] ){
							xc[1]=2.0d*Grid[1][GridSize[1]]-xc[1];
						}
						break;
					}
				}while(true);
			}
			#endregion
			
			xc[2] = x[2]; //z
			xc[3] = x[3]; //v

			/*
			Console.WriteLine("point {0} {1} {2} was centered in {3} {4} {5}",
			                  x[0].ToString(),x[1].ToString(),x[2].ToString(),
			                  xc[0].ToString(),xc[1].ToString(),xc[2].ToString());
			*/
			
		}
		
		/*
		/// <summary>
		/// Dump the interpolated field at constant height z
		/// </summary>
		public void DumpField_zfix(double z)
		{
			string field = Name+".field.out";
			StreamWriter o = new StreamWriter(field);
			
			x[2] = z;
			double xx = -UnitSize[0];
			double yy = -UnitSize[1];
			
			for(xx = -UnitSize[0]; xx<=UnitSize[0]; xx+=GridStep[0]/3){
				for(yy = -UnitSize[1]; yy<=UnitSize[1]; yy+=GridStep[1]/3){
					x[0] = xx; x[1] = yy;
					Evaluate();
					o.Write("{0} {1} ",xx.ToString(),yy.ToString());
					for(int c=0;c<Dim;c++)
						o.Write("{0} ",Force[c]);
					o.WriteLine("");
				}
				o.WriteLine("");
			}
			
			
			o.Dispose();
			
			field = Name+".source.out";
			o = new StreamWriter(field);
			
			
			for(int i=0;i<GridSize[0];i++){
				for(int j=0;j<GridSize[1];j++){

					o.Write("{0} {1} ",Grid[0][i].ToString(),Grid[1][j].ToString());
					for(int c=0;c<Dim;c++)
						o.Write("{0} ",DataGrid[i,j,xIndex[2],c]);
					o.WriteLine("");
				}
				o.WriteLine("");
			}
			
			
			o.Dispose();
			
			
		}
		*/
	}
	
	
	public abstract class Potential
	{
		protected string[] Words;
		public double[] F;
		
		public Potential(string[] words)
		{
			Words = words;
			F = new double[3];
		}
		public abstract void Initialize();
		public abstract void Evaluate(double[] rv, double dist);
		
	}
	
	//uniform random numbers generator
	public abstract class absrand : Circuit
	{
		protected double[] Buffer;
		protected int Index = 0;
		protected int N = 0, seed;
		protected Random rnd;
		protected bool Ticked = false;
		
		
		protected void RandomSetup()
		{
			N = (int)GetInitParameter(InitWords, "buffer",false,10.0);
			if(N%2 == 1)
				N++;
			Buffer = new double[N];
			seed = (int)GetInitParameter(InitWords, "seed",false,0.0);
			rnd = new Random(seed);
			
			if(GetInitParameter(InitWords, "ticked", false, 0.0) == 1.0){
				Ticked = true;
				Input.Add(new Channel("tick",null));
			}
			
		}
		
		//fills the buffer with 
		protected abstract void Seed();
		
		public override void Update (ref double dt)
		{
			if(Ticked)
				if(Input[0].Value <= 0.0)
					return;
			
			Output[0].Value = Buffer[Index];
			Index++;
			
			if(Index==N)
			{
				Index = 0;
				Seed();
			}
			
		}
		
		
		
	}	
	
	public class MersenneTwister : System.Random
	{
		/* Period parameters */
		private const int N = 624;
		private const int M = 397;
		private const uint MATRIX_A   = 0x9908b0df; /* constant vector a */
		private const uint UPPER_MASK = 0x80000000; /* most significant w-r bits */
		private const uint LOWER_MASK = 0x7fffffff; /* least significant r bits */

		/* Tempering parameters */
		private const uint TEMPERING_MASK_B = 0x9d2c5680;
		private const uint TEMPERING_MASK_C = 0xefc60000;

		private static uint TEMPERING_SHIFT_U(uint y) { return (y >> 11); }
		private static uint TEMPERING_SHIFT_S(uint y) { return (y <<  7); }
		private static uint TEMPERING_SHIFT_T(uint y) { return (y << 15); }
		private static uint TEMPERING_SHIFT_L(uint y) { return (y >> 18); }

		private uint[] mt = new uint[N]; /* the array for the state vector  */

		private short mti;

		private static uint[] mag01 = { 0x0, MATRIX_A };

		/* initializing the array with a NONZERO seed */
		public MersenneTwister(uint seed)
		{
			/* setting initial seeds to mt[N] using         */
			/* the generator Line 25 of Table 1 in          */
			/* [KNUTH 1981, The Art of Computer Programming */
			/*    Vol. 2 (2nd Ed.), pp102]                  */
			mt[0] = seed & 0xffffffffU;
			for (mti = 1; mti < N; ++mti)
			{
				mt[mti] = (69069 * mt[mti - 1]) & 0xffffffffU;
			}
		}
		public MersenneTwister() : this((uint) (DateTime.Now.Second + DateTime.Now.Minute*100 + DateTime.Now.Hour*10000 + DateTime.Now.Millisecond)) /* a default initial seed is used   */
		{
		}

		protected uint GenerateUInt()
		{
			uint y;

			/* mag01[x] = x * MATRIX_A  for x=0,1 */
			if (mti >= N) /* generate N words at one time */
			{
				short kk = 0;

				for (; kk < N - M; ++kk)
				{
					y = (mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK);
					mt[kk] = mt[kk + M] ^ (y >> 1) ^ mag01[y & 0x1];
				}

				for(;kk < N - 1; ++kk)
				{
					y = (mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK);
					mt[kk] = mt[kk+(M - N)] ^ (y >> 1) ^ mag01[y & 0x1];
				}

				y = (mt[N - 1] & UPPER_MASK) | (mt[0] & LOWER_MASK);
				mt[N - 1] = mt[M - 1] ^ (y >> 1) ^ mag01[y & 0x1];

				mti = 0;
			}

			y = mt[mti++];
			y ^= TEMPERING_SHIFT_U(y);
			y ^= TEMPERING_SHIFT_S(y) & TEMPERING_MASK_B;
			y ^= TEMPERING_SHIFT_T(y) & TEMPERING_MASK_C;
			y ^= TEMPERING_SHIFT_L(y);

			return y;
		}

		public virtual uint NextUInt()
		{
			return this.GenerateUInt();
		}

		public virtual uint NextUInt(uint maxValue)
		{
			return (uint)(this.GenerateUInt() / ((double)uint.MaxValue / maxValue));
		}

		public virtual uint NextUInt(uint minValue, uint maxValue) /* throws ArgumentOutOfRangeException */
		{
			if (minValue >= maxValue)
			{
				throw new ArgumentOutOfRangeException();
			}

			return (uint)(this.GenerateUInt() / ((double)uint.MaxValue / (maxValue - minValue)) + minValue);
		}

		public override int Next()
		{
			return this.Next(int.MaxValue);
		}

		public override int Next(int maxValue) /* throws ArgumentOutOfRangeException */
		{
			if (maxValue <= 1)
			{
				if (maxValue < 0)
				{
					throw new ArgumentOutOfRangeException();
				}

				return 0;
			}

			return (int)(this.NextDouble() * maxValue);
		}

		public override int Next(int minValue, int maxValue)
		{
			if (maxValue < minValue)
			{
				throw new ArgumentOutOfRangeException();
			}
			else if(maxValue == minValue)
			{
				return minValue;
			}
			else
			{
				return this.Next(maxValue - minValue) + minValue;
			}
		}

		public override void NextBytes(byte[] buffer) /* throws ArgumentNullException*/
		{
			int bufLen = buffer.Length;

			if (buffer == null)
			{
				throw new ArgumentNullException();
			}

			for (int idx = 0; idx < bufLen; ++idx)
			{
				buffer[idx] = (byte)this.Next(256);
			}
		}

		public override double NextDouble()
		{
			return (double)this.GenerateUInt() / ((ulong)uint.MaxValue + 1);
		}
	}


}
