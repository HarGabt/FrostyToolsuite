using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Ebx
{
    public class DebugInformation
    {
        internal Dictionary<string, long> offsetsMap = new Dictionary<string, long>();

        // temp variables
        internal string offsetKey = string.Empty;
        internal readonly Stack<string> offsetKeyStack = new Stack<string>();

        internal void ResetWorkingVariables()
        {
            offsetKey = string.Empty;
            offsetKeyStack.Clear();
        }

        internal void PushDebugOffset(string suffix, long position, bool clear = false)
        {
            if (clear)
            {
                offsetKeyStack.Clear();
                offsetKey = suffix;
            }
            else
            {
                offsetKey += suffix;
            }
            offsetKeyStack.Push(suffix);
            if (!offsetsMap.ContainsKey(offsetKey))
            {
                offsetsMap.Add(offsetKey, position);
            }
        }

        internal void PopDebugOffset()
        {
            if (offsetKeyStack.Count > 0)
            {
                string suffix = offsetKeyStack.Pop();
                if (offsetKey.EndsWith(suffix))
                {
                    offsetKey = offsetKey.Substring(0, offsetKey.Length - suffix.Length);
                }
            }
        }

        internal long GetDebugOffset(string suffix)
        {
            offsetKeyStack.Push(suffix);
            offsetKey += suffix;
            return offsetsMap.TryGetValue(offsetKey, out long value) ? value : 0;
        }


    }
}
