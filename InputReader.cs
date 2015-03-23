using System; using vafmpro.AbsCircuits;
using System.Collections.Generic;
//////using System.Linq;
using System.Text;
using System.IO;
using vafmpro.Circuits;

namespace vafmpro
{
    public enum ChannelType
    {
        Input = 0,
        Output = 1,
        Any = 2
    }

    partial class Program
    {

        static string FileNameMain;
		
		

        static bool ReadInput()
        {

            if (!File.Exists(FileNameMain))
            {
                Console.WriteLine("FATAL! The input file was not found.");
                return false;
            }
            Console.WriteLine("Opening input file: {0}", FileNameMain);
 

			myScanner = new MainScanner(new string[]{"program","scanner"});
			Circuits.Add(myScanner);
			//Circuits.Insert(2, myScanner);
			
			if (!ReadVariables(FileNameMain, null))
				return false;
			if (!ReadComposAlias(FileNameMain))
				return false;
            if (!ReadCircuits(FileNameMain, null))   //read all the circuits
                return false;
            if (!ReadCantilever()) //read the cantilever (which is a circuit)
                return false;
			if (!ReadOutput())   //now read the output
                return false;
            if (!ReadMainScannerScript()) //read the scanner (which is a circuit)
                return false;
			
			//add all feeds to the global list
			foreach(Circuit listedcirc in Circuits)
				foreach(Channel outchan in listedcirc.Output)
					Program.AllFeeds.Add(outchan.Signal);
			
            //if (!ReadSecondary(FileNameMain, null))   //now read the output
            //    return false;
            if (!ReadConnections(FileNameMain, null)) //and finally the connections
                return false;

			/*
			//makes sure the scanned has the same holder position as the cantilever
			for(int i=0;i<3;i++)
			{
				myScanner.Output[i].Value = HolderPosition[i];
				myCantilever.Input[i].Value = HolderPosition[i];
			}
			 */

            Console.WriteLine("Input file read correctly!\n");

            return true;
        }

		static bool ReadComposAlias(string FileName)
		{
			string[] words;
			string line = "";
			char[] delimiterChars = { ' ', ',', '\t' };
			
			Console.WriteLine("\n   Reading Composite Types from {0}:", FileName);
			StreamReader reader = new StreamReader(FileName);
            if (!StringReader.FindString("<composites>", reader)){
                Console.WriteLine("INFO! Composite Type list is missing.");
				reader.Dispose();
                return true;
            }
			
			while ((line = reader.ReadLine()) != null){
				line = line.Trim();
                words = StringReader.TrimWords(line.Split(delimiterChars));//read the type and the name
				
				#region "check for empty/terminator lines"
                if (line.StartsWith("<end>")) //finish at the end of group
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0 ) //ignore empty lines/comments/timer
                    continue;
				#endregion
			
				if(words.Length != 2){
					Console.WriteLine("ERROR! Invalid composite type definition.");
					return false;
				}
				
				CompositeAlias.Add(words[0],words[1]);
				
				
			}
			
			
			
			return true;
		}
		public static bool ReadVariables(string FileName, Circuit owner)
		{
			Dictionary<string,double> CList;
			string[] words,keys;
			string line = "";
			char[] delimiterChars = { ' ', ',', '\t' };
			double val = 0;
			
			
			if(owner == null)
				CList = Program.Globals;
			else
				CList = ((composite)owner).Locals;
		
			Console.WriteLine("\n   Reading variables from {0}:", FileName);
			StreamReader reader = new StreamReader(FileName);
            if (!StringReader.FindString("<variables>", reader)){
                Console.WriteLine("WARNING! Variable list is missing.");
				reader.Dispose();
                return true;
            }
			
			while ((line = reader.ReadLine()) != null){
				line = line.Trim();
                words = StringReader.TrimWords(line.Split(delimiterChars));//read the type and the name
				
				#region "check for empty/terminator lines"
                if (line.StartsWith("<end>")) //finish at the end of group
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0 ) //ignore empty lines/comments/timer
                    continue;
				#endregion
			
				#region "parse the line"
				for(int i=0;i<words.Length;i++){
					keys = StringReader.TrimWords(words[i].Split('=')); //divide the string,
					if(keys.Length != 2){
						Console.WriteLine("ERROR! Expected varible=value pair.");
						return false;
					}
					//now we have a pair of strings... the second must be a number!
					if(!double.TryParse(keys[1], out val)){
						Console.WriteLine("ERROR! Value of variable {0} is not a number.",keys[0]);
						return false;
					}
					//now check if the variable was already declared
					if(CList.ContainsKey(keys[0])){
						Console.WriteLine("ERROR! Variable {0} was already declared.",keys[0]);
						return false;
					}
					
					CList.Add(keys[0],val);	
				}
				#endregion
			}
				
			return true;
		}
				
		
        //reads from an arbitrary file, and adds the circuits either to the program list or to the composites subcircuits
        public static bool ReadCircuits(string FileName, Circuit owner)
        {
			List<Circuit> CList;
            string[] words;
			string line = "";
            char[] delimiterChars = { ' ', ',', '\t' };
			Type CircType;
			Circuit circ;
			
			
			if(owner == null)
				CList = Program.Circuits; //if we are reading the main input file
			else
				CList = ((composite)owner).SubCircuits; //if we are reading a composite setup
			
            Console.WriteLine("\n   Reading circuits from {0} (composite?{1}):", FileName,owner!=null);

            StreamReader reader = new StreamReader(FileName);
            if (!StringReader.FindString("<circuits>", reader)){
                Console.WriteLine("FATAL! Circuit list is missing.");
                return false;
            }
			
            Console.WriteLine("   Reading circuit list:\n");

			//check if there is a main timer (but not for composites)
			#region "get main timer"
			if(owner == null){
				if(!StringReader.FindStringNoEnd("timer", reader, "<circuits>", ref line, FileName) ){
					Console.WriteLine("FATAL! The main timer circuit is not present.");
					return false;
				}
				line = line.Trim();
				words = StringReader.TrimWords(line.Split(delimiterChars));//read the type and the name
				words[1]="time"; //force it to be called time
				mytimer = (timer)Activator.CreateInstance(myCircuitTypes[words[0]], new object[]{words});
				mytimer.SetUp();
				CList.Insert(0, mytimer ); //ACHTUNG! timer goes first!
				reader.Dispose();
				reader = new StreamReader(FileName);
				StringReader.FindString("<circuits>", reader);
			}
			#endregion
			
            while ((line = reader.ReadLine()) != null){
				line = line.Trim();
                words = StringReader.TrimWords(line.Split(delimiterChars));//read the type and the name
                
				#region "check for empty/terminator lines"
                if (line.StartsWith("<end>")) //finish at the end of group
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0 || line.StartsWith("timer") ) //ignore empty lines/comments/timer
                    continue;
                if (words[0].StartsWith("#"))
                    continue;
				if (words.Length < 2){
					Console.WriteLine("FATAL! Two fields are always required!");
					return false;
				}
				#endregion
				
                #region "check restricted names"
                if ( (words[1] == "time") && (words[0] != "timer") ){
                    Console.WriteLine("FATAL! Only a timer circuit can be named 'timer'.");
                    return false;
                }
                if (words[1] == "scanner"){
                    Console.WriteLine("FATAL! Name 'scanner' is reserved for the scanner controller.");
                    return false;
                }
                if (words[1] == "cantilever"){
                    Console.WriteLine("FATAL! Name 'cantilever' is reserved for the cantilever.");
                    return false;
                }
                #endregion
                
				#region "check if name/type are good"
				//check if present in the circuit types list
				if(!myCircuitTypes.ContainsKey(words[0])){
					Console.WriteLine("ERROR! Circuit type {0} does not exist.",words[0]);
					return false;
				}
				
				//check if the name is already used
				if(CList.Find(c => c.Name == words[1]) != null){
					Console.WriteLine("ERROR! A circuit named {0} was already initialized.",words[1]);
					return false;
				}
				#endregion
								
				CircType = myCircuitTypes[words[0]]; //get the circuit type
				if(owner == null){
					words[0] = "program";
				}else{
					words[0] = owner.Name;
				}
				
				#region "check that there is only one scripter"
				if(CircType == typeof(Scripter)){
					for(int i=0; i<CList.Count; i++){
						if((CList[i].GetType() == typeof(Scripter)) || (CList[i].GetType() == typeof(MainScanner))){
							Console.WriteLine("ERROR! Only one scripter is allowed!");
							return false;
						}
					}
				}
				#endregion
				
				//Console.WriteLine("INPUT READER: w0 {0}",words[0]);
				circ = (Circuit)Activator.CreateInstance(CircType, new object[]{words});
				circ.Owner = owner; // set the owner
				circ.SetUp(); //do the setup routine
				CList.Add( circ ); //add the circuit to the list
				
				if(CircType == typeof(composite))
					((composite)circ).composite_Init();
				
				if(CircType == typeof(Scripter)){
					if(!(((Scripter)circ).ReadScript(FileName)))
						return false;
				}
				
            }
			
			//add all the output feeds of the circuit to a global list of feeds. ONLY FOR COMPOSITES
			if(owner != null)
				foreach(Circuit listedcirc in CList)
					foreach(Channel outchan in listedcirc.Output)
						Program.AllFeeds.Add(outchan.Signal);
			
            Console.WriteLine("---Circuits Read.\n");
            reader.Dispose();
            return true;
        }

        public static bool ReadConnections(string FileName, composite myComp)
        {
            string[] words;
			string[] keys;
            string line;
            char[] delimiterChars = { ' ', '\t' };
			bool MetaIn, MetaOut;
			
            List<Circuit> CList = Program.Circuits;
            if (myComp != null)
                CList = myComp.SubCircuits;


            StreamReader reader = new StreamReader(FileName); //reopen the file
            Console.WriteLine("\n   Reading connection list:");
            if (!StringReader.FindString("<connections>", reader)){
                Console.WriteLine("FATAL! Connection list is missing.");
                return false;
            }

			//parse all the lines
            while ((line = reader.ReadLine()) != null)
            {
				line = line.Trim();
                words = StringReader.TrimWords(line.Split(delimiterChars)); //split with spaces and tabs
                
				#region "check for bad characters..."
                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0)
                    continue;
                if (words[0].StartsWith("#"))
                    continue;
				
                if (words.Length < 2){
                    Console.WriteLine("FATAL! The format for connections is: circA.outX  circB.inY ...");
                    return false;
                }
				#endregion
				
                Circuit c1, c2;
                Channel v1, v2;
				
				#region "get the first circuit.channel"
				
				keys = StringReader.TrimWords(words[0].Split('.'));
				if (keys.Length != 2){
                    Console.WriteLine("FATAL! The format for connections is: circA.outX  circB.inY ...");
                    return false;
                }
				
                //SPECIAL CASE FOR COMPOSITE CIRCUIT CONNECTION TO EXTERNAL PORTS - INPUT
                if (keys[0] == "me" && myComp != null) {
					MetaIn = true;
                    if (!myComp.GetMetaChannel(keys[1], ChannelType.Input, out v1, true)){
                        Console.WriteLine("FATAL! Unable to find input port {0} in the composite circuit.", keys[1]);
                        return false;
                    }
					c1 = myComp;
					/*
                    v2.Signal = v1.Signal;
                    c2.Owner = myComp; //set the ownership so that this will be considered updatable according to this channel
                    Console.WriteLine("Input port {0} is connected to {1}.{2}.", words[1], words[2], words[3]);
                    continue;
                    */
                }
				else{ //get the first circuit.channel normally from the list
					MetaIn = false;
					if (!Circuit.CheckCircuitChannel(new string[] { keys[0], keys[1] },ChannelType.Output,CList, out c1, out v1))
						return false;
				}
				#endregion
				Console.WriteLine("Connecting {0}.{1} to: ",c1.Name,v1.Name);
				
				for(int i=1;i<words.Length;i++){ //parse the other words
					
					keys = StringReader.TrimWords(words[i].Split('.'));
					
					if (keys[0] == "me" && myComp != null){ //SPECIAL CASE FOR COMPOSITE CIRCUIT CONNECTION TO EXTERNAL PORTS - OUTPUT
						MetaOut = true;
						if (!myComp.GetMetaChannel(keys[1], ChannelType.Output, out v2, true)){
							Console.WriteLine("FATAL! Unable to find input port {0} in the composite circuit.", keys[1]);
							return false;
						}
						c2 = myComp;
					}
					else { //get the other circuit.channel normally from the list
						MetaOut = false;
					    if (!Circuit.CheckCircuitChannel(new string[] { keys[0], keys[1] },ChannelType.Input,CList, out c2, out v2))
							return false;
					}
					
					//perform the connection
					v2.Signal = v1.Signal; //put the signal of c1.v1 (the out) in the signal of c2.v2 (the input)
					if(MetaIn) //if the input was a metainput from composite, the receiver's channel becomes owned by the composite
						c2.Owner = myComp;
					Console.WriteLine("   {0}.{1}",c2.Name,v2.Name);
				}
				
				
				
				/*
				#region "SPECIAL CASES COMPOSITES - outputs"
                //SPECIAL CASE FOR COMPOSITE CIRCUIT CONNECTION TO EXTERNAL PORTS - OUTPUT
                if (words[2] == "me" && myComp != null)
                {
                    v2 = myComp.FindMetaOutput(words[3]);
                    if (v2 == null)
                    {
                        Console.WriteLine("FATAL! Unable to find output port {0} in the composite circuit.", words[3]);
                        return false;
                    }
                    if (!Circuit.CheckCircuitChannel(new string[] { words[0], words[1] }, ChannelType.Output, CList, out c1, out v1)) //find the first circuit
                        return false;

                    v2.Signal = v1.Signal; //take the subcircuit's channel.feed and give the ref to the metaoutput

                    Console.WriteLine("Output port {0} is connected to {1}.{2}.", words[3], words[0], words[1]);
                    continue;
                }
                #endregion

                if (!Circuit.CheckCircuitChannel(new string[] { words[0], words[1] }, ChannelType.Output, CList, out c1, out v1)) //find the first circuit
                    return false;
                if (!Circuit.CheckCircuitChannel(new string[] { words[2], words[3] }, ChannelType.Input, CList, out c2, out v2)) //find the second circuit
                    return false;


                if (!c2.Connect(v1, words[3]))
                {
                    Console.WriteLine("FATAL! Something went wrong during the connection: {0}", line);
                    return false;
                }
                */

            }

            Console.WriteLine("---Connections Read.");
            reader.Dispose();
            return true;
        }

        static bool ReadOutput()
        {
            string[] words;
            StreamReader reader = new StreamReader(FileNameMain); //reopen the file
            if (!StringReader.FindString("<output>", reader))
            {
                Console.WriteLine("FATAL! Output list is missing.");
                return false;
            }
            Console.WriteLine("\n   Reading output configuration:");

            string line; string[] fields;
            char[] delimiterChars = { ' ', '\t' };

            Out = new Outputter();

            while ((line = reader.ReadLine()) != null) //read each line
            {
                words = StringReader.TrimWords(line.Split(delimiterChars));
                line = line.Trim();
                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0 || words.Length == 0)
                    continue;

                //read the type and the name

                if (words[0].StartsWith("#"))
                    continue;
                if (words.Length < 3)
                {
                    Console.WriteLine("FATAL! The output format is: filename frequency obj1.cha obj2.chb ...");
                    return false;
                }

                Out.filenames.Add(words[0]);                  //add a filename to the list
                Out.dumpFreq.Add(Convert.ToInt32(words[1]));  //add its dump frequency

                Out.outchannels.Add(new List<string[]>());
                Console.WriteLine("Generatin output file {0}.", words[0]);

                char[] subdels = { '.' };
                for (int i = 0; i < words.Length - 2; i++)
                {
                    fields = StringReader.TrimWords(words[i + 2].Split(subdels));
                    if (fields.Length != 2)
                    {
                        Console.WriteLine("FATAL! The output format is: filename circ1.chA circ2.chB ...");
                        return false;
                    }

                    Circuit c1; Channel v1;
                    if (!Circuit.CheckCircuitChannel(fields, ChannelType.Any, out c1, out v1)) //find the first circuit
                        return false;

                    Out.outchannels[Out.outchannels.Count - 1].Add(fields);
                    Console.WriteLine("  adding channel {0}.{1}", c1.Name, v1.Name);
                }

            }

            Out.InputInit(); //initialize the output
            Circuits.Add(Out);
			
            Console.WriteLine("---Output Read.");
            reader.Dispose();
            return true;
        }

        static bool ReadCantilever()
        {

            myCantilever = new Cantilever(); //create a cantilever
            Circuits.Insert(1, myCantilever); //insert it in the second place (after timer)

			return myCantilever.ReadCantilever(FileNameMain);
			
		}

		
		static bool ReadMainScannerScript()
		{
			return myScanner.ReadScript(FileNameMain);
		}
		
		/*
		public static bool ReadSecondary(string FileName, Circuit owner)
        {
			List<Circuit> CList;
			if(owner == null)
				CList = Program.Circuits;
			else
				CList = ((composite)owner).SubCircuits;
			
			
            StreamReader reader = new StreamReader(FileName); //reopen the file
            
            if (!StringReader.FindString("<secondary>", reader))
            {
                Console.WriteLine("INFO! No secondary channels were specified.");
                return true;
            }
			Console.WriteLine("\n   Reading secondary channels:");
			
            string line = "";
            string[] words, subwords;
            char[] delimiterChars = { ' ', '\t' };
            char[] subdel = { '.' };
            Channel ch; Circuit c;

            while ((line = reader.ReadLine()) != null) //parse to the end
            {
                line = line.Trim();
                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0)
                    continue;

                words = StringReader.TrimWords(line.Split(delimiterChars));

                if (words[0].StartsWith("#"))
                    continue;

                for (int i = 0; i < words.Length; i++)
                {
                    subwords = StringReader.TrimWords(words[i].Split(subdel));
                    if (subwords.Length != 2)
                    {
                        Console.WriteLine("FATAL! The format to describe secondary channel is: circuit1.channelA");
                        return false;
                    }

                    if (!Circuit.CheckCircuitChannel(subwords, ChannelType.Input, CList, out c, out ch))
                        return false;
                    ch.Priority = false;
                    Console.WriteLine("Channel {0} will not be needed to update circuit {1}.", words[i], subwords[1]);
                }

            }
			
			Console.WriteLine("---Secondary channels Read.");

            reader.Dispose();
            return true;
        }
		*/
		
		/*
        static bool ReadCustom(string TypeName, string CustomName)
        {
            //read a composite circuit descriptor

            StreamReader reader = new StreamReader(FileNameMain);
            Console.WriteLine("\n---CUSTOM CIRCUIT DESCRIPTION---");
			
            if (!StringReader.FindString("<customs>", reader)){
                Console.WriteLine("FATAL! Custom files block is missing.");
                return false;
            }
			
            string line = "";
            if (!StringReader.FindStringNoEnd(TypeName, reader, "<customs>", ref line, FileNameMain))
            {
                Console.WriteLine("FATAL! Input file for custom circuit type {0} is not specified.", TypeName);
                return false;
            }
            string[] words;
            char[] delimiterChars = { ' ', '\t' };

            words = StringReader.TrimWords(line.Split(delimiterChars)); //get the file where the description is stored
            if (words.Length < 2)
            {
                Console.WriteLine("FATAL! Input file for custom circuit {0} is not specified.", TypeName);
                return false;
            }

            if (!File.Exists(words[1])) // check if file exists
            {
                Console.WriteLine("FATAL! The input file for custom circuit ({0}) was not found.", words[1]);
                return false;
            }

            //create the custom
            SPICER.Network myCustom = new SPICER.Network(words[1], CustomName, mytimer.dt);
            Circuits.Add(myCustom);

            //call the input parser in the network
            if (!myCustom.Initialize())
                return false;


            Console.WriteLine("---------------DONE----------------\n");

            reader.Dispose();
            return true;
        }
*/
        //composites circuits reader

        public static bool ReadExternals(string FileName, composite myComp)
        {
            //read a composite circuit descriptor for externals ports

            StreamReader reader = new StreamReader(FileName);
            Console.WriteLine("\n   Reading external ports:");
            if (!StringReader.FindString("<externals>", reader))
            {
                Console.WriteLine("WARNING! Composite circuit does not have external connections?!.");
                return false;
            }
            string line = "";

            string[] words;
            char[] delimiterChars = { ' ', '\t' };
            int ip = 0, op = 0;

            while ((line = reader.ReadLine()) != null) //parse to the end
            {
                line = line.Trim();
				
				#region "check for comments/empty lines..."
                if (line.StartsWith("<end>"))
                    break;
                if (line.StartsWith("#") || line.Length == 0)
                    continue;

                words = StringReader.TrimWords(line.Split(delimiterChars));

                if (words[0].StartsWith("#"))
                    continue;
				#endregion
				
                if (words.Length < 2)
                {
                    Console.WriteLine("FATAL! External ports format is: input|output  name (true|false)");
                    return false;
                }

	
				if (words[0] == "input"){ //create an input port
                    Console.WriteLine("Input port named {0} was added", words[1]);
                    myComp.Input.Add(new Channel(words[1], null));
                    myComp.MetaInput.Add(new Channel(words[1], myComp)); //metainputs are owned by the container circuit: they are effectively outputs
					/*if (words.Length > 2){
						if (words[2] == "false"){
                            Console.WriteLine(" as secondary.");
                            myComp.Input[ip].Priority = false;
                            myComp.MetaInput[ip].Priority = false;
                        }
                    }
					else //if(words[2] == "true")
                        Console.WriteLine(" as primary.");
                    */

                    ip++;
                }
                if (words[0] == "output") //create an input port
                {
                    Console.WriteLine("Output port named {0} was added.", words[1]);
                    myComp.Output.Add(new Channel(words[1], myComp));
                    myComp.MetaOutput.Add(new Channel(words[1], myComp));
                    op++;
                }

            }
			Console.WriteLine("---External ports Read.");
            reader.Dispose();
            return true;
        }
		
		/*
		//this will be called by the composite initializer
        static bool ReadComposite(string TypeName, string CompositeName, Dictionary<string,double> Parameters)
        {
            //read a composite circuit descriptor
            string[] words;
            char[] delimiterChars = { ' ', '\t' };
			
            StreamReader reader = new StreamReader(FileNameMain);
            Console.WriteLine("\n---COMPOSITE CIRCUIT DESCRIPTION---");
			
			#region "check if ok"
            if (!StringReader.FindString("<composites>", reader))
            {
                Console.WriteLine("FATAL! Composites file block is missing.");
                return false;
            }
            string line = "";
            if (!StringReader.FindStringNoEnd(TypeName, reader, "<composites>", ref line, FileNameMain))
            {
                Console.WriteLine("FATAL! Input file for composite circuit type {0} is not specified.", TypeName);
                return false;
            }
			
            words = StringReader.TrimWords(line.Split(delimiterChars));
            if (words.Length < 2)
            {
                Console.WriteLine("FATAL! Input file for composite circuit {0} is not specified.", TypeName);
                return false;
            }

			if (!File.Exists(words[1])) // check if file exists
            {
                Console.WriteLine("FATAL! The input file for composite circuit ({0}) was not found.",words[1]);
                return false;
            }
			#endregion
			
            //create the composite
            composite myCompos = new composite(words);
			myCompos.Locals = Parameters;
            Circuits.Add(myCompos); //add it to the list
			
			
			//read the
			
			
            //parse the input file - circuits
            if (!ReadCircuits(words[1], myCompos))
                return false;

            //parse the input file - ports
            if (!ReadExternals(words[1], myCompos))
                return false;

            //parse the input file - connections
            if (!ReadConnections(words[1], myCompos))
                return false;

            //parse the input file - secondaries
            if (!ReadSecondary(words[1], myCompos.SubCircuits))
                return false;

            //now check if the inner mechanism is self-dependent
            if (!myCompos.CheckUpdateSequence_Composite())
                return false;


            Console.WriteLine("---------------DONE----------------\n");

            reader.Dispose();
            return true;
        }

*/

    }

}
