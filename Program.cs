using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using vafmpro.Circuits;
using vafmpro.AbsCircuits;

//using System.Windows.Forms;

namespace vafmpro
{
    partial class Program
    {
		public static List<Feed> AllFeeds = new List<Feed>();
		public static Dictionary<string,Type> myCircuitTypes, myPotentialTypes, myInstructionTypes;
		
        public static List<Circuit> Circuits;
        public static Dictionary<string,double> Globals; //global variables
		public static Dictionary<string,string> CompositeAlias;

        public static Outputter Out;
        public static timer mytimer;
        public static Cantilever myCantilever;
        public static MainScanner myScanner; //this is just a channel holder

		private static double dt;

        static void Main(string[] args)
        {
			
            if (args.Length == 0)
            {
                Console.WriteLine("FATAL! No input file specified!");
                return;
            }

			CreateCircuitList();
			
			Globals = new Dictionary<string, double>();
			CompositeAlias = new Dictionary<string, string>();
			
            Circuits = new List<Circuit>();    //init the circuit list
            //Script = new List<Instruction>();  //init the actions list

            CreateConsts();

	
            //read the input file
            FileNameMain = args[0];
            if (!ReadInput())
            {
                Console.WriteLine("FATAL! The input file contained errors, the program will stop!");
                Console.ReadKey();
                return;
            }
			
            //try a fake update to check for recursion
            //if (!CheckUpdateSequence(false)){
			//	Console.ReadKey();
			//	return;
			//}
	
			RelistCircuits(Circuits);
			
            //initialize the first function
			myScanner.ScriptInit();
            //FunctionsInit[Script[scriptIdx].Code - 1](Script[scriptIdx].Param, Script[scriptIdx].sParam);
			dt = mytimer.dt;
			
            while (Update()){ //do the update untill a stop is generated

            }

            Out.Close();
			
			//DebugOwners(Circuits);
			
            Console.WriteLine("Press any key to continue...");
            //Console.ReadKey();
        }
        
		private static void RelistCircuits(List<Circuit> CList)
		{
			Circuit cr;
			
			cr = CList.Find(circ => circ.GetType() == typeof(Scripter) || circ.GetType() == typeof(MainScanner));
			if(cr != null){
				CList.Remove(cr);
				CList.Add(cr);
			}
			
			cr = CList.Find(circ => circ.GetType() == typeof(Outputter));
			if(cr != null){
				CList.Remove(cr);
				CList.Insert(CList.Count-1, cr);
			}
			
			cr = CList.Find(circ => circ.GetType() == typeof(Cantilever));
			if(cr != null){
				CList.Remove(cr);
				CList.Insert(1, cr);
			}
			
			//Program.Circuits.Find(circ => circ.Name == words[0])
			
			//reorder the list of circuits so that any scripter goes last, output goes onetolast
			for(int i=0;i<CList.Count;i++){
				if(CList[i].GetType() == typeof(composite))
					RelistCircuits(((composite)CList[i]).SubCircuits);
			}
			
			
		}
		
		/*
        public static bool CheckUpdateSequence(bool Quiet)
        {
			//this routine plays with .Updated property of circuits,
			//so it has to be called in the beginning or the end of the
			//update cycle.
			//mhmmm not really because the Updated property is not really used
			//outside of this function!!!
			Console.WriteLine("checking update sequence!");
			//set all to not updated
			for(int i=0;i<Circuits.Count;i++)
				Circuits[i].Updated=false;

            #region "Self-need check"

            int j, LastChecked, Size;

            //this masterpiece of coding checks if there are circuits that need themself for update (even indirectly!)
            for (int i = 0; i < Circuits.Count; i++)
            {
				//Console.WriteLine("typeof circ {0} is {1}",i, Circuits[i].GetType().ToString() );
				
                if (Circuits[i].GetType() == typeof(vafmpro.Circuits.Const)) //consts do not depend on anything
                    continue;

				//if (Circuits[i].GetType() == typeof(Scripter)) //scripters do not depend on anything
                //   continue;

                List<Circuit> reflist = new List<Circuit>();
                reflist.Add(Circuits[i]);
                LastChecked = -1;

                do
                {
                    //LastChecked = reflist.Count - 1; //j is the index of the last element in the reflist, jp is 0 at first
                    Size = reflist.Count; //get the size of the reflist!

                    for (j = LastChecked+1; j < Size; j++)
                        for (int k = 0; k < reflist[j].Input.Count; k++) //loop over this circuit's inputs
                        {
						if(reflist[j].Input[k].Signal.Owner == mytimer )
							Console.WriteLine("ASD!");
						
                            if (/*reflist[j].Input[k].Priority && reflist[j].Input[k].Signal.Owner != null) //add to the list the priority&connected ones
                                reflist.Add(reflist[j].Input[k].Signal.Owner);
                        }
                    LastChecked += (Size-LastChecked-1); //update the index of the last checked item

                    if(reflist.FindAll(delegate(Circuit c) { return c.Name == reflist[0].Name; }).Count>1)
                    {
                        Console.WriteLine("FATAL! There seems to be an infinitely recursive update cycle!");
                        Console.WriteLine("Some circuits depend somehow on themselves so they cannot be updated.");
                        Console.WriteLine("Try to set one of its input channels to secondary.");
                        return false;
                    }
                    if (Size == reflist.Count) //if no other circuit was added...
                        break;
                } while (true);
            }
            #endregion

			Console.WriteLine("list checked!");

            #region "Reorder circuit list"
            //try to establish a correct updating order
            j = 2;
            LastChecked = 2;
            bool isPresent;
            int[] index = new int[Circuits.Count];
            index[0] = 0; //the timer is always put first
            index[1] = Circuits.IndexOf(myCantilever); // cantilever goes second
            mytimer.Updated = true; //timer is always updated
            myCantilever.Updated = true; //cantilever is always updated
			myScanner.Updated = true; //scanner is always updated
            do
            {
                //Console.WriteLine("Checking circ {0}={1}", LastChecked, Circuits[LastChecked].Name);

                if ((Circuits[LastChecked].IsUpdatable() || typeof(vafmpro.Circuits.Const)==Circuits[LastChecked].GetType()) && (Circuits[LastChecked].Name != "output" &&
                    Circuits[LastChecked].Name != "time" && Circuits[LastChecked].Name != "cantilever" && Circuits[LastChecked].Name != "scanner"))
                {

                    //check if the index is already there
                    isPresent = false;
                    for (int ii = 0; ii <= j; ii++){
                        if (index[ii] == LastChecked){
                            isPresent = true;
                            break;
                        }
                    }

                    if (!isPresent) //if not present put it
                    {
                        index[j] = LastChecked;
                        Circuits[LastChecked].Updated = true;
                        //Console.WriteLine("putting circ {0}={1} at list {2}", LastChecked, Circuits[LastChecked].Name, j);
                        j++;
                    }
                }



                LastChecked++;
                if (LastChecked == Circuits.Count)
                    LastChecked = 0;

            } while (j < Circuits.Count-2); //timer,canti?,scanner?,output are excluded because they have fixed position

			//Console.WriteLine("part 2 done!");
			
            index[Circuits.Count - 2] = Circuits.IndexOf(myScanner);//scanner is always one to last
            index[Circuits.Count - 1] = Circuits.Count - 1;//output is always last

            List<Circuit> newlist = new List<Circuit>(Circuits.Count);
            for (j = 0; j < Circuits.Count; j++)
            {
                //Console.WriteLine("Update order: {0}", Circuits[index[j]].Name);
                newlist.Add(Circuits[index[j]]);
                Circuits[index[j]].Updated = false; //reset the updated status to false
				//Console.WriteLine("circuit {0}!",Circuits[j].Name.ToString());
				
            }
            Circuits = newlist; //use the ordered list
            #endregion

			#region "print update order"
			if(!Quiet){
				Console.WriteLine("Update order:");
				for(int i=0; i<Circuits.Count;i++)
					Console.WriteLine("Circuit {0}: {1}",i,Circuits[i].Name);
			}
			#endregion
			
            return true;

        }
		 */

        /// <summary>
        /// Main Execution Loop: updates all the circuits and the output.
        /// </summary>
        /// <returns>Returns false of the scanner finished its tasklist</returns>
        static bool Update()
        {
            bool result = true;

			//update all the circuits in the order given in the list - MAIN SCANNER TOO
            for (int i = 0; i < Circuits.Count; i++){ //the outputter will NOT be updated now
				if (typeof(vafmpro.Circuits.Const) != Circuits[i].GetType() &&
					Circuits[i] != myCantilever.ForceModule){
					Circuits[i].Update(ref dt);
					Circuits[i].PostUpdate(); //this pushes the output of a circuit if it was set as pushed=true
				} 
			}
			
			//now push all feeds
			for(int i=0;i<AllFeeds.Count;i++)
				AllFeeds[i].PushBuffer();
			
			
            //now all the circuits were updated... perform the script action
            if (myScanner.Output[3].Value > 0.0) //if scripter gives error/endfunctions or the scanner orders to stop, halt!
            {
                //Out.ForceAllWrite();//force the writing on all channels
				Console.WriteLine("Execution stopped.");
                return false;
            }
            
			//NOW update the output
            //Out.Update(ref dt);
            

            return result;
        }


        
        /// <summary>
        /// Creates the common constants channels.
        /// </summary>
        static void CreateConsts()
        {
			/*
			string[] words = new string[]{"program","ZERO","value=0.0"};
			Circuits.Add(new vafmpro.Circuits.Const(words));
			words[1] = "ONE"; words[2] = "value=1.0";
            Circuits.Add(new vafmpro.Circuits.Const(words));
			words[1] = "PI"; words[2] = "value="+Math.PI.ToString();
            Circuits.Add(new vafmpro.Circuits.Const(words));
			words[1] = "2PI"; words[2] = "value="+(2 * Math.PI).ToString();
            Circuits.Add(new vafmpro.Circuits.Const(words));
            */
        }
		


		static void CreateCircuitList()
		{
			//This gets all the types in the current assembly and prints them out
            Assembly assembly = Assembly.GetExecutingAssembly();
			
			#region "circuits"
			
			myCircuitTypes = new Dictionary<string, Type>();
			
            foreach (Type t in assembly.GetTypes())
			{
				if(t.Namespace == null)
					continue;
				if(t.Name.Contains(">"))
					continue;
				if(t.Namespace.Contains(".Circuits") && !t.Name.Contains(">")){
					myCircuitTypes.Add(t.Name,t);
					//Console.WriteLine(t.Name);
				}
				
			}
			#endregion
			#region "potentials"
			
			myPotentialTypes = new Dictionary<string, Type>();
			
            foreach (Type t in assembly.GetTypes())
			{
				if(t.Namespace == null)
					continue;
				if(t.Name.Contains(">"))
					continue;
				if(t.Namespace.Contains(".Potentials") && !t.Name.Contains(">")){
					myPotentialTypes.Add(t.Name,t);
					//Console.WriteLine(t.Name);
				}
				
			}
			#endregion
			#region "create instruction dictionary"
			string[] words;
			myInstructionTypes = new Dictionary<string, Type>();
			//This gets all the types in the current assembly and prints them out
            foreach (Type t in assembly.GetTypes()){
				if(t.Namespace == null)
					continue;
				
				if(t.Namespace.Contains(".Instructions") && !t.Name.Contains(">") 
					&& !t.Name.Contains("_Circuit") && !t.Name.Contains("_Event") ){
					words = StringReader.TrimWords(t.Name.Split('_'));
					myInstructionTypes.Add(words[1],t);
					//Console.WriteLine(words[1]);
				}
			}
			//Console.WriteLine("---\n");
			#endregion

		}
		
		
		static void DebugOwners(List<Circuit> clist){
			
			for(int i=0;i<clist.Count;i++){
				if(clist[i].Owner == null)
				{
					Console.WriteLine("circuit {0} owned by nobody",clist[i].Name);
					
				}else
					Console.WriteLine("circuit {0} owned by {1}",clist[i].Name,clist[i].Owner.Name);
				
				if(clist[i].GetType() == typeof(composite))
				{
					Console.WriteLine("-- COMPOSITE! ---");
					DebugOwners(((composite)clist[i]).SubCircuits);
					Console.WriteLine("-- ENDCOMPOSITE! ---");
				}
				
				
			}
			
			
		}
		
		static public double GetVariable(Circuit c, string varname){
			
			double val = 0;
			
			//Console.WriteLine("attempting variable read...");
			
			if(c == null){ //if we sweep up to the main level...
				//Console.WriteLine("getvar: arrived at the bottom");
				if(Program.Globals.ContainsKey(varname)){
					val = Program.Globals[varname];
					return val;
				}
				else{
					Console.WriteLine("ERROR! Variable {0} was never declared.",varname);
					throw new Exception("ERROR!");
				}
			}
			
			if(c.GetType() == typeof(composite) ){ //if itz a composite...
				//Console.WriteLine("getva: looking in composite {0}",c.Name);
				if( ((composite)c).Locals.ContainsKey(varname)){  //if it contains the variable as local...
					val = ((composite)c).Locals[varname]; //return its value
					return val;
				}
				
			}
			
			//if the code arrives here it means that c is not null, and c is not a composite with a valid variable in it!
			// or not a composite at all
			//Console.WriteLine("getva: looking in circuit {0} - going up one level!",c.Name);
			//go up one level, to the owner of the circuit c
			val = GetVariable(c.Owner, varname);
						
			return val;
		}
		
    }

}
