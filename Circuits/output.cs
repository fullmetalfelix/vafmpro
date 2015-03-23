using System;using vafmpro.AbsCircuits;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;



namespace vafmpro.Circuits
{
    class Outputter : Circuit
    {

        private List<StreamWriter> outfiles;
        public List<string> filenames;

        public List<List<string[]>> outchannels; //list of channels (specified by names) to be printed for each output file
		public List<List<Channel>> OutChannels;
        private List<int> counter;
		private List<List<bool>> Getbuffered;
		
        public List<int> dumpFreq;
        public List<bool> Connected;
		
        public Outputter()
        {
            Init(new string[]{"program","output"});

            dumpFreq = new List<int>();
            Connected = new List<bool>();

            outfiles = new List<StreamWriter>();
            filenames = new List<string>();

            outchannels = new List<List<string[]>>();
			OutChannels = new List<List<Channel>>();
			Getbuffered = new List<List<bool>>();
			
        }

        public void InputInit()
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
                Input[Input.Count - 1].Value = 1.0; //by default they are active
				Input[Input.Count - 1].Signal.PushBuffer();
                //the idea is to connect these to some sort of "enabler" channel
				
				OutChannels.Add(new List<Channel>());
				Getbuffered.Add(new List<bool>());
				
				for(int j=0;j<outchannels[i].Count; j++){
					Circuit c; Channel ch;
					Circuit.CheckCircuitChannel(outchannels[i][j], ChannelType.Any, out c, out ch); // get the right reference to the channel
					OutChannels[i].Add(ch);
					
					if(Circuit.CheckCircuitChannel(outchannels[i][j], ChannelType.Output, out c, out ch))
						Getbuffered[i].Add(true);
					else
						Getbuffered[i].Add(false);
				}
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

				
                if ((counter[i] >= dumpFreq[i]) && (Input[i].Value > 0.0)) //check if it is time to dump (or later)!
                {
					
					counter[i] = 0;
                    for (int j = 0; j < OutChannels[i].Count; j++) //loop over the string pairs for this output file
                    {
                        //Circuit c; Channel ch;
                        //Circuit.CheckCircuitChannel(outchannels[i][j], ChannelType.Any, out c, out ch); // get the right reference to the channel
                        //outfiles[i].Write("{0}({1}) ", ch.Value.ToString(), ch.Signal.GetBufferedValue().ToString());
						outfiles[i].Write("{0} ", (Getbuffered[i][j])? OutChannels[i][j].Signal.GetBufferedValue().ToString() :
							OutChannels[i][j].Value.ToString() );

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
 
                if (Input[i].Value > 0.0) //check if it is time to dump (or later)!
                {
                    for (int j = 0; j < outchannels[i].Count; j++) //loop over the string pairs for this output file
                    {
                        Circuit c; Channel ch;
                        Circuit.CheckCircuitChannel(outchannels[i][j], ChannelType.Any, out c, out ch); // get the right reference to the channel
                        outfiles[i].Write("{0} ", ch.Value.ToString());
                    }
                    outfiles[i].Write("\r\n");
                }
            }


        }

		public void RenameChannel(int channel, string newname)
		{
			
			outfiles[channel].Flush();
			outfiles[channel].Close();
			outfiles[channel].Dispose();
			outfiles[channel] = new StreamWriter(newname);
			
			
		}
		
    }
}
