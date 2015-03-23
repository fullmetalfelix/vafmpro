using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace vafmpro.Circuits
{



    public class MainScanner : Scripter
    {

        public bool StopRecord = false;

		public int CurrentInstruction;
		
		
        public MainScanner(string[] words)
        {
            Init(words);
			Console.WriteLine("executing main scanner constructor");
            /*Input.Add(new Channel("in0",null)); //0  free input channels
			Input.Add(new Channel("in1", null)); //1
            Input.Add(new Channel("in2", null)); //2
            Input.Add(new Channel("in3", null)); //3
            Input.Add(new Channel("in4", null)); //4
            Input.Add(new Channel("amp", null)); //5  amplitude specific channel
            Input.Add(new Channel("df", null));  //6  df specific input*/

            Output.Add(new Channel("holderx", this)); //0
            Output.Add(new Channel("holdery", this)); //1
            Output.Add(new Channel("holderz", this)); //2

            Output.Add(new Channel("stop", this));  //3?
            Output.Add(new Channel("cmd", this));   //4 : the code of the currently executed command
            Output.Add(new Channel("icmd", this));  //5 : the index of the currently executed command

            Output.Add(new Channel("record", this));//6 : becomes 1 when scanline records
            Output.Add(new Channel("scan", this));  //7 : the scanline number
			
			//all scanner input are secondary by definition
			/*for(int i=0;i<Input.Count;i++)
				Input[i].Priority=false;*/
        }

		#region "Outputs"
		public override void Write (string message)
		{
			Console.Write (message);
		}
		public override void WriteLine (string message)
		{
			Console.WriteLine (message);
		}

		
		#endregion
		
		
       public override void Update(ref double dt)
        {
			
            if (StopRecord){  //listen for stoprecording events from the instruction
				Output[6].Value = 0;
				StopRecord = false;
			}
						
			//update the current instruction
			//Console.WriteLine("Scanner idx {0}",scriptIdx);
			Functions[scriptIdx].Update(ref dt);
			
			if(Functions[scriptIdx].isDone){      //if the function had finished its task...
				scriptIdx++;
				//Console.WriteLine("action done!");
				if(scriptIdx >= Functions.Count){ //stop if last instruction
					ScriptStop = 1;
					Output[3].Value = ScriptStop;
					return;
				}
				//initialize the next function - only if itz not active
				if(Functions[scriptIdx].isActive == false)
					Functions[scriptIdx].FunctionInit();
			}
			
        }
        

    }
}
