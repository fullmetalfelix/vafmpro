using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace vafmpro.Circuits
{
    public class composite : Circuit
    {

        public List<Circuit> SubCircuits;
        public List<Channel> MetaInput,MetaOutput;
		
		
		public Dictionary<string,double> Locals; //list of parameters given from outside
		
        public composite(string[] words)
        {
			Console.WriteLine("--- CREATING COMPOSITE {0} ---\n",words[1]);
            Init(words);

            SubCircuits = new List<Circuit>();
			
            MetaInput = new List<Channel>();
            MetaOutput = new List<Channel>();

			Locals = new Dictionary<string, double>();
		}
		
		public void composite_Init()
		{
			//words: [program|circuit], name, type=asd, var1=val1, ..."
			
			string typename = GetInitParameterString(InitWords,"type",true);
			if(!Program.CompositeAlias.ContainsKey(typename))
				throw new Exception("ERROR! The composite type "+typename+" was not declared.");
			
			typename = Program.CompositeAlias[typename]; //get the filename
			
			#region "take care of local variables"
			string[] keys;
			Program.ReadVariables(typename,this); //read the variables list
			
			//override local variables with input parameters
			for(int i=2;i<InitWords.Length;i++){
				if(InitWords[i].StartsWith("type"))continue;
				keys = StringReader.TrimWords(InitWords[i].Split('='));//read the type and the name
				if(keys.Length != 2) throw new Exception("ERROR! Invalid input string (" + InitWords[i] + ").");
				
				if(Locals.ContainsKey(keys[0])) //if the first  key is the name of a local variable...
					Locals[keys[0]] = GetInitParameter(InitWords,keys[0],true); //overwrite the value with the input line one
			}
			foreach(KeyValuePair<string,double> p in Locals)
				Console.WriteLine(" local var {0}: {1}",p.Key,p.Value);
			#endregion
			
			#region "Read input stuff"
			if(!Program.ReadCircuits(typename,this))
				throw new Exception("ERROR! Something was wrong in the composite circuits.");
			if(!Program.ReadExternals(typename,this))
				throw new Exception("ERROR! Something was wrong in the composite externals.");
			if (!Program.ReadConnections(typename, this))
                throw new Exception("ERROR! Something was wrong in the composite connections.");
			//if (!Program.ReadSecondary(typename, this))
            //    throw new Exception("ERROR! Something was wrong in the composite secondaries.");
			#endregion
			
			InputDefaultInit();
			
			//if there is a scripter, initialize it
			for(int i=0;i<SubCircuits.Count;i++)
				if(typeof(Scripter) == SubCircuits[i].GetType())
					((Scripter)SubCircuits[i]).ScriptInit();
			
			
			
            //now check if the inner mechanism is self-dependent
            //if (!CheckUpdateSequence_Composite())
            //    throw new Exception("ERROR! Something was wrong in the composite update sequence.");
			
			Console.WriteLine("---------------DONE----------------\n");
        }
		
		/* initialize the input channels of the circuit using a list of input parameters
		 * the dictionary key is the channel to set to the corresponding value.
		 * return true if everything was awrite! false if epic fail! */
		private void asdMetaInputDefaultInit(string[] words)
		{
			string[] keys;
			Channel ch;
			double val;
			
			Console.WriteLine("  {0}: MetaInput channels default init: ",Name);
			
			for(int i=2;i<words.Length;i++){ //start from the 3rd element cos 0 is the type, and 1 is the name
				
				//look for words that contain '='
				if(!words[i].Contains("=") || words[i].Contains("type"))
					continue;
				//if we are here, the word contains an =
				Console.WriteLine("ASDASD! word {0}",words[i]);
				keys = StringReader.TrimWords(words[i].Split('='));//read the type and the name
				if(keys.Length != 2)
					throw new Exception("ERROR! The initialization string is not right ("+words[i]+").");
				
				//now check if the first key is the name of an input channel
				if(!GetMetaChannel(keys[0], ChannelType.Input, out ch, true)){
					Console.WriteLine("    WARNING! Input channel {0} not found, ignoring initialization.",keys[0]);
					continue;
				}
				//if we are here the channel was found! try parse key2 to get a number out of it
				#region "try parse double/local var/global var/"
				
				if(double.TryParse(keys[1], out val)){ //if numeric
					ch.Value = val;
				}else{
					//if not numeric
					val = double.NaN;
					
					//try a local variable
					if(Owner != null){  //for composites
						if( ((composite)Owner).Locals.ContainsKey(keys[1]))
							val = ((composite)Owner).Locals[keys[1]];
					}else{
						//try a global variable
						if(Program.Globals.ContainsKey(keys[1])){
							val = Program.Globals[keys[1]];
						}
					}
					//at this point we tried all possibilities. if value is still none, print a warning
					if(double.IsNaN(val))
						Console.WriteLine("    WARNING! The given value for channel {0} is not a number and will be ignored.",ch.Name);
					else{
						Console.WriteLine("    input {0} <- {1}",ch.Name,val);
						ch.Value = val;
					}
				}
				#endregion
				
			}
		}
		
		
		public override void Update(ref double dt)
        {
            //hardcopy the values from input channels to metainput
            for (int i = 0; i < Input.Count; i++)
            {
                MetaInput[i].Value = Input[i].Value; //this takes input[i].v (the signal at t) and put it in the buffer of metainput[i]
				MetaInput[i].Signal.PushBuffer(); //this forces the signal to get the buffered value
            }


            //update all the circuits
            for (int i = 0; i < SubCircuits.Count; i++)
            {
                if (typeof(Const) != SubCircuits[i].GetType())
                    SubCircuits[i].Update(ref dt);
            }




            //hardcopy the values from metaoutput to output
            for (int i = 0; i < Output.Count; i++)
            {
                MetaOutput[i].Signal.PushBuffer(); //forces the buffered metaoutput to be the real value
                Output[i].Value = MetaOutput[i].Value; //this is ok as it writes  the real metaoutput in the buffer of output
            }

        }


		/// <summary>
        /// Find a channel in this circuit in the main program's circuit list, and set it as out.
        /// </summary>
        /// <param name="name">Name of the channel to find.</param>
        /// <param name="type">Channel type: In, Out or any.</param>
        /// <param name="channel">Reference to the channel if it was found.</param>
        /// <returns>Returns true if the channel exists, false otherwise</returns>
		public bool GetMetaChannel(string name, ChannelType type, out Channel channel, bool Quiet)
		{
			channel = null;
			
			#region "Input only"
			if(type == ChannelType.Input){ //look only in the input list
				
				foreach(Channel ch in MetaInput){
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
				
				foreach(Channel ch in MetaOutput){
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
				
				foreach(Channel ch in MetaInput){
					if(ch.Name == name){
						channel = ch;
						return true;
					}
				}
				foreach(Channel ch in MetaOutput){
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
			
			return false;
		}
		
		/*
        public Channel FindMetaInputasd(string name)
        {
            foreach (Channel ch in MetaInput) // look in circuits
                if (ch.Name == name)
                    return ch;

			
			
            return null;
        }
        public Channel FindMetaOutputasd(string name)
        {
            foreach (Channel ch in MetaOutput) // look in circuits
                if (ch.Name == name)
                    return ch;

            return null;
        }
		 */
		/*
        /// <summary>
        /// Run the self-dependence test on any given circuit list and reorder it.
        /// </summary>
        /// <param name="SubCircuits">List of circuits that will be checked.</param>
        /// <returns>True if everything is ok, false otherwhise.</returns>
        public bool CheckUpdateSequence_Composite()
        {

            Console.WriteLine("Checking self-referencing loops...");

            #region "Self-need check"

            int j, LastChecked, Size;

            //this masterpiece of coding checks if there are circuits that need themself for update (even indirectly!)
            for (int i = 0; i < SubCircuits.Count; i++)
            {
                if (SubCircuits[i].GetType() == typeof(Const))
                    continue;

                List<Circuit> reflist = new List<Circuit>();
                reflist.Add(SubCircuits[i]);
                LastChecked = -1;

                do
                {
                    Size = reflist.Count; //get the size of the reflist!

                    for (j = LastChecked + 1; j < Size; j++)
                        for (int k = 0; k < reflist[j].Input.Count; k++) //loop over this circuit's inputs
                        {
                            if (reflist[j].Input[k].Priority && reflist[j].Input[k].Signal.Owner != null) //add to the list the priority&connected ones
                            {
                                if (reflist[j].Input[k].Signal.Owner != this) //exclude also if the channel is connected to an input port"
                                    reflist.Add(reflist[j].Input[k].Signal.Owner);

                            }
                        }
                    LastChecked += (Size - LastChecked - 1); //update the index of the last checked item

                    if (reflist.FindAll(delegate(Circuit c) { return c.Name == reflist[0].Name; }).Count > 1)
                    {
                        Console.WriteLine("FATAL! There seems to be an infinitely recursive update cycle!");
                        Console.WriteLine("Some circuits depend somehow on themselves so they cannot be updated.");
                        Console.WriteLine("Try to set an input channel to secondary.");
                        return false;
                    }
                    if (Size == reflist.Count) //if no other circuit was added...
                        break;
                } while (true);
            }
            #endregion

            #region "Reorder circuit list"
            //try to establish a correct updating order
            j = 0;
            LastChecked = 0;
            bool isPresent;
            int[] index = new int[SubCircuits.Count];
            do
            {
                //Console.WriteLine("Checking circ {0}={1}", LastChecked, CList[LastChecked].Name);

                if ((SubCircuits[LastChecked].IsUpdatable() || typeof(Const) == SubCircuits[LastChecked].GetType())){

                    //check if the index is already there
                    isPresent = false;
                    for (int ii = 0; ii < j; ii++)
                    {
                        if (index[ii] == LastChecked)
                        {
                            isPresent = true;
                            break;
                        }
                    }

                    if (!isPresent) //if not present put it
                    {
                        index[j] = LastChecked;
                        SubCircuits[LastChecked].Updated = true;
                        //Console.WriteLine("putting circ {0}={1} at list {2}", LastChecked, CList[LastChecked].Name, j);
                        j++;
                    }
                }


                LastChecked++;
                if (LastChecked == SubCircuits.Count)
                    LastChecked = 0;

            } while (j < SubCircuits.Count); //timer,canti?,scanner?,output are excluded because they have fixed position

            List<Circuit> newlist = new List<Circuit>(SubCircuits.Count);
            for (j = 0; j < SubCircuits.Count; j++)
            {
                //Console.WriteLine("Update order: {0}", Circuits[index[j]].Name);
                newlist.Add(SubCircuits[index[j]]);
                SubCircuits[index[j]].Updated = false; //reset the updated status to false
            }
            SubCircuits = newlist; //use the ordered list
            #endregion

            return true;

        }
		 */
    }
}
