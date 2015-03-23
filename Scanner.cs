using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vafmpro.Circuits
{

    public class Instruction
    {
        public int Code;
        public double[] Param, Paramo;
        public string[] sParam, sParamo;
        
        public Instruction(int code, double[] vars, string[] svars)
        {
            Code = code;
            Param = vars;
            sParam = svars;

            Paramo = null;
            if (Param != null)
            {
                Paramo = new double[Param.Length];
                for (int i = 0; i < Param.Length; i++)
                    Paramo[i] = Param[i];
            }
            sParamo = null;
            if (sParam != null)
            {
                sParamo = new string[sParam.Length];
                for (int i = 0; i < sParam.Length; i++)
                    sParamo[i] = sParam[i];
            }

        }
        public void Reinit()
        {
            if (Param != null)
            {
                Param = new double[Paramo.Length];
                for (int i = 0; i < Paramo.Length; i++)
                    Param[i] = Paramo[i];
            }
            if (sParam != null)
            {
                sParam = new string[sParamo.Length];
                for (int i = 0; i < sParamo.Length; i++)
                    sParam[i] = sParamo[i];
            }

        }

    }
    public class Scanloop
    {
        public int Nlines;


    }

    public partial class Scanner : Circuit
    {

        public bool StopRecord = false;

        public Scanner()
        {
            Init("scanner");

            Input.Add(new Channel("in0",null)); //0  free input channels
            Input.Add(new Channel("in1", null)); //1
            Input.Add(new Channel("in2", null)); //2
            Input.Add(new Channel("in3", null)); //3
            Input.Add(new Channel("in4", null)); //4
            Input.Add(new Channel("amp", null)); //5  amplitude specific channel
            Input.Add(new Channel("df", null));  //6  df specific input

            Output.Add(new Channel("holderx", this)); //0
            Output.Add(new Channel("holdery", this)); //1
            Output.Add(new Channel("holderz", this)); //2

            Output.Add(new Channel("stop", this));  //3?
            Output.Add(new Channel("cmd", this));   //4 : the code of the currently executed command
            Output.Add(new Channel("icmd", this));  //5 : the index of the currently executed command

            Output.Add(new Channel("record", this));//6 : becomes 1 when scanline records
            Output.Add(new Channel("scan", this));  //7 : the scanline number
        }


       public override void Update(ref double dt)
        {
			
            if (StopRecord)  //listen for stoprecording events from the instruction
            {
                Output[6].Signal.Value = 0;
                StopRecord = false;
            }
        }
        

    }
}
