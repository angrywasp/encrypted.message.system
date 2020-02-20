using System;

namespace EMS
{
    public static class Helpers
    {
        public static string PopWord(ref string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
                
            int index = input.IndexOf(' ');
            if (index == -1)
            {
                string ret = input;
                input = string.Empty;
                return ret;
            }
            else
            {
                string ret = input.Substring(0, index);
                input = input.Remove(0, ret.Length).TrimStart();
                return ret;
            }
        }
    }
}