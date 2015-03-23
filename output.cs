using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;



namespace vafmpro.Circuits
{
    class Outputter : Circuit
    {

        private List<StreamWriter> outfiles;
        public List<string> filenames;

        public List<List<string[]>> outchannels; //list of channels (specified by names) to be printed for each output file
        private List<int> counter;
        public List<int> dumpFreq;
        public List<bool> Connected;
        public Outputter()
        {
            Init("output");

            dumpFreq = new List<int>();
            Connected = new List<bool>();

            outfiles = new List<StreamWriter>();
            filenames = new List<string>();

            outchannels = new List<List<string[]>>();

        }

        public void Init()
        {
            //int i = 0;
            int maxouts = filenames.Count;  //the number of output streams
            counter = new List<int>();

            for (int i = 0; i < maxouts; i++)
            {
                outfiles.Add(new StreamWriter(filenames[i])); //add the streamwriters
                counter.Add(0); //reset the frequency counter
                Connected.Add(false);
                Input.Add(new Channel(i.ToString(),null)); //create an input channel for each output streamer
                Input[Input.Count - 1].Signal.Value = 1.0; //by default they are active
                //the idea is to connect these to some sort of "enabler" channel

            }

        }


        public void Close()
        {
            foreach (StreamWriter s in outfiles)
                s.Close();

        }

        public override void  Update(ref double dt)
        {

            for (int i = 0; i < filenames.Count; i++)
            {
                counter[i]++; //increase the counter

				
                if ((counter[i] >= dumpFreq[i]) && (Input[i].Signal.Value > 0.0)) //check if it is time to dump (or later)!
                {
					
					counter[i] = 0;
                    for (int j = 0; j < outchannels[i].Count; j++) //loop over the string pairs for this output file
                    {
                        Circuit c; Channel ch;
                        Circuit.CheckCircuitChannel(outchannels[i][j], ChannelType.Any, out c, out ch); // get the right reference to the channel
                        outfiles[i].Write("{0} ", ch.Signal.Value.ToString());

                    }
                    outfiles[i].Write("\r\n");
                }
            }

            return;
        }

		public void WriteChannel(int channel, string str)
		{
		if(channel<outfiles.Count)
			{
				outfiles[channel].WriteLine(str);
				outfiles[channel].Flush();
			}
		else
			{
				Console.WriteLine("WARNING! Output channel #{0} does not exist!",channel.ToString());
			}
			
		}
		
        public void ForceAllWrite()
        {
            for (int i = 0; i < filenames.Count; i++)
            {
 
                if (Input[i].Signal.Value > 0.0) //check if it is time to dump (or later)!
                {
                    for (int j = 0; j < outchannels[i].Count; j++) //loop over the string pairs for this output file
                    {
                        Circuit c; Channel ch;
                        Circuit.CheckCircuitChannel(outchannels[i][j], ChannelType.Any, out c, out ch); // get the right reference to the channel
                        outfiles[i].Write("{0} ", ch.Signal.Value.ToString());
                    }
                    outfiles[i].Write("\r\n");
                }
            }


        }

    }
}
