using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using vafmpro.Circuits;
using vafmpro.AbsCircuits;
using vafmpro.Instructions;
using System.Reflection;


namespace vafmpro.Circuits
{


    //functions for scripted instructions
    public class Scripter : Circuit
    {
		protected List<Function_Circuit> Functions;
		protected StreamWriter OutputSystem;
		
		public int scriptIdx = 0;        //index of the currently executing instruction
        public int ScriptStop = 0;

		public Scripter(){
			
			Console.WriteLine("executing scripter empty constructor");
		}
		
		public Scripter(string[] words)
		{
			Init(words);
			Console.WriteLine("executing scripter constructor");
			
		}
		
        public void ScriptInit()
        {
			
			Console.WriteLine("SCRIPTER INIT! TYPE is {0}",this.GetType());
			if(this.GetType() == typeof(Scripter)){
				StringBuilder sb = new StringBuilder("scripter_");
				sb.Append(Name);
				sb.Append("_");
				if(Owner!=null)
					sb.Append(Owner.Name);
				sb.Append(".log");
				OutputSystem = new StreamWriter(sb.ToString());
				Console.WriteLine("Scripter output will go to: {0}",sb.ToString());
			}
			
			Functions[0].FunctionInit();
			
        }

        /*
        #region "scriptable functions"


        private static void SetConstInit(double[] vars, string[] svars)
        {
			//bool stringed = false;
            Circuit c; Channel ch = null;
			//double newval;
			
			Console.Write("Scanner: changing constant ({0}) ",vars.Length.ToString());
			//loop on the passed parameters:
			for(int i=0;i<vars.Length;i++)
			{
				//stringed = false;
				if(double.IsNaN(vars[i]))//if one value is nan use the circuit.channel names
				{
					//stringed = true;
					Circuit.CheckCircuitChannel(new string[] { svars[4*i+2], svars[4*i+3] }, ChannelType.Any, out c, out ch);
					vars[i] = ch.Value;
				}
				
				Circuit.CheckCircuitChannel(new string[] { svars[4*i], svars[4*i+1] }, ChannelType.Output, out c, out ch); //get the constant
				ch.Value = vars[i];
				Console.Write(" {0} to {1} | ", svars[4*i], vars[i]);
			}
	
            
        }

        private static void SetPriorityInit(double[] vars, string[] svars)
        {
            //make a connection on the fly!
            Circuit c1;
            Channel v1;
            Circuit.CheckCircuitChannel(new string[] { svars[0], svars[1] }, ChannelType.Input, out c1, out v1);
            if (vars[0] == 1)
            {
                v1.Priority = false;
                Console.Write("Channel {0}.{1} is now secondary.", svars[0], svars[1]);
            }
            else
            {
                v1.Priority = true;
                Console.Write("Channel {0}.{1} is now primary.", svars[0], svars[1]);
            }
            
            //check the if the update order is screwd!
            ScriptError = !CheckUpdateSequence();
            
        }
		
        #endregion
*/

		public override void Update (ref double dt)
		{
			//important shit here!
			
			//update the current instruction
			Functions[scriptIdx].Update(ref dt);
			
			if(Functions[scriptIdx].isDone){      //if the function had finished its task...
				scriptIdx++;
				OutputSystem.Flush();
				//Console.WriteLine("action done!");
				if(scriptIdx >= Functions.Count){ //stop if last instruction
					//remove this scripter from the composite subcircuits
					((composite)Owner).SubCircuits.Remove(this);
					
					return;
				}
				//initialize the next function - only if itz not active
				if(Functions[scriptIdx].isActive == false)
					Functions[scriptIdx].FunctionInit();
				
			}
			
			
		}

		
		public virtual void Write(string message)
		{
			OutputSystem.Write(message);
		}
		public virtual void WriteLine(string message)
		{
			OutputSystem.WriteLine(message);
			
		}
		
		//parse a file for a script definition
		//if this method is called, the <script> has to be present in the file, otherwise GTFO
		public bool ReadScript(string FileName)
		{
			
			#region "init stuff - check if script is present..."
			string[] words;
			string line = "";
			char[] delimiterChars = { ' ', ',', '\t' };
			System.Type FunctionType;
			Function_Circuit func;
				
			Console.WriteLine("\n   Reading Script from {0}:", FileName);
			StreamReader reader = new StreamReader(FileName);
            if (!StringReader.FindString("<script>", reader)){
                Console.WriteLine("INFO! Script definition is missing.");
				reader.Dispose();
                return false;
            }
			#endregion
			
			
			Functions = new List<Function_Circuit>(); //initialize the list of functions
			
			while ((line = reader.ReadLine()) != null){ //parse
				line = line.Trim();
                words = StringReader.TrimWords(line.Split(delimiterChars));//read the type and the name
				
				#region "check for empty/terminator lines"
                if (line.StartsWith("<end>")) //finish at the end of group
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0 ) //ignore empty lines/comments/timer
                    continue;
				#endregion
				
				#region "check if function type is good"
				//check if present in the circuit types list
				if(!Program.myInstructionTypes.ContainsKey(words[0])){
					Console.WriteLine("ERROR! Script function {0} is not coded.",words[0]);
					return false;
				}
				#endregion
							
				//the owner of the instruction is the owner of this scripter circuit (program or composite)
				FunctionType = Program.myInstructionTypes[words[0]]; //get the circuit type
				if(Owner == null){
					words[0] = "program";
				}else{
					words[0] = Owner.Name;
				}
				
				func = (Function_Circuit)Activator.CreateInstance(FunctionType, new object[]{words,this});
				Functions.Add( func );
				
			}
			
			Console.WriteLine("   Script read.\n");
			return true;
		}
		
		

		
/*
        public static bool ScriptUpdate(double dt)
        {
            int funCode = Script[scriptIdx].Code; //this is the code of the currently active function

            if (ScriptError)
            {
                Console.WriteLine("Halting execution because an error occurred!");
                return false;
            }

            bool finished = Functions[funCode - 1](dt);  //call the right function
            if (finished) //if the action was finished...
            {
				Console.WriteLine();
                Script[scriptIdx].Reinit(); //restore the original value parameters for later use (in case of a goto)

                //check if it was the last action
                if (scriptIdx + 1 >= Script.Count)
                {
                    myScanner.Output[3].Value = 1.0; //activate the stopper
                    return false; //get out of here
                }

                scriptIdx++;  //increase the action counter
                funCode = Script[scriptIdx].Code;

                //check if the instruction is a goto:  //as a result of this gotos are never initialized and never "executed"
                if (funCode == 12)
                {
                    if (Script[scriptIdx].Param[1] == 0) //if this was the last rebound...
                    {
                        Script[scriptIdx].Reinit(); //reinit the goto
                        scriptIdx++;  //increase the action counter to surpass the goto
						Console.WriteLine("initliazing instruction {0}.",scriptIdx);
                        funCode = Script[scriptIdx].Code;
                    }
                    else //if it wasnt the last one
                    {
                        Script[scriptIdx].Param[1]--; //decrese the number of repetition
                        scriptIdx = Convert.ToInt32(Script[scriptIdx].Param[0]); //set the pointer to the pointed instruction
                        funCode = Script[scriptIdx].Code;
                    }
                }

                FunctionsInit[funCode - 1](Script[scriptIdx].Param, Script[scriptIdx].sParam);  //initialize the new function
                myScanner.Output[4].Value = funCode - 1; //set the output channels for the command information
                myScanner.Output[5].Value = scriptIdx;

            }

            return true;
        }

*/

    }

}