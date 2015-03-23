using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;


namespace vafmpro
{
    public static class StringReader
    {
		
		public static char[] StdSeps = { ' ', ',', '\t' };
		
        public static bool FindString(string starter, StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
                if (line.Trim().StartsWith(starter))
                    return true;
            return false;
        }

        public static bool FindStringNoEnd(string starter, StreamReader reader, string reposition, ref string retline, string FileName)
        {
            bool result = false;

            reader.Dispose();
            reader = new StreamReader(FileName);
            FindString(reposition, reader);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim().StartsWith(starter))
                {
                    result = true;
                    retline = line;
                    break;
                }

                if (line.Trim().StartsWith("<end>"))
                {
                    result = false;
                    break;
                }

            }

            return result;
        }

        public static string[] TrimWords(string[] words)
        {
            string[] nwords;
            List<string> lst = new List<string>();

            for (int i = 0; i < words.Length; i++)
			{
				if(words[i].Contains("#")) // if a word contains comment marker, stop parsing
					break;
                if (words[i] != "") 
                    lst.Add(words[i]);
				
			}

            nwords = new string[lst.Count];
            for (int i = 0; i < lst.Count; i++)
                nwords[i] = lst[i];

            return nwords;
        }

		//find a necessary parameter in a words array
		public static double GetLineParameter_d(string[] words, string pName)
		{
			string[] keys;
			double val;
			
			for(int i=0;i<words.Length;i++){ //start from the 1st element
				
				if(!words[i].StartsWith(pName+"="))
					continue;
				
				keys = StringReader.TrimWords(words[i].Split('='));//read the type and the name
				
				if(!double.TryParse(keys[1], out val)){ //if not numeric, error
					throw new Exception("ERROR! Parameter "+pName+" does not have a numerical value.");					
				}
				
				return val;
			}
			
			//if the code arrives here, the param was not found.
			throw new Exception("ERROR! Parameter "+pName+" was not found.");		
		}
		public static string GetLineParameter_s(string[] words, string pName)
		{
			string[] keys;
			
			for(int i=0;i<words.Length;i++){ //start from the 1st element
				
				if(!words[i].StartsWith(pName+"="))
					continue;
				
				keys = StringReader.TrimWords(words[i].Split('='));//read the type and the name
				if(keys.Length < 2)
					throw new Exception("ERROR! Parameter "+pName+" does not have a value.");
				return keys[1];
			}
			
			//if the code arrives here, the param was not found.
			throw new Exception("ERROR! Parameter "+pName+" was not found.");		
		}
		
    }
}
