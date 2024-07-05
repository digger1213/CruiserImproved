using DiggCruiserImproved;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;

namespace DiggCruiserImproved
{
    static internal class PatchUtils
    {
        public struct OpcodeMatch
        {
            public OpCode opcode;
            public object operandOrNull = null;

            public OpcodeMatch(OpCode opcode)
            {
                this.opcode = opcode;
            }
            public OpcodeMatch(OpCode opcode, object operand)
            {
                this.opcode = opcode;
                this.operandOrNull = operand;
            }
        }

        public static int LocateCodeSegment(int startIndex, List<CodeInstruction> searchSpace, List<OpcodeMatch> searchFor)
        {
            if (startIndex < 0 || startIndex >= searchSpace.Count) return -1;

            int searchForIndex = 0;
            for(int searchSpaceIndex = startIndex; searchSpaceIndex < searchSpace.Count; searchSpaceIndex++)
            {
                CodeInstruction check = searchSpace[searchSpaceIndex];
                OpcodeMatch currentMatch = searchFor[searchForIndex];
                if(check.opcode == currentMatch.opcode)
                {
                    searchForIndex++;
                    if(searchForIndex == searchFor.Count)
                    {
                        //we found the sequence, return the index at the start of the sequence
                        return searchSpaceIndex - searchForIndex + 1;
                    }
                }
                else
                {
                    searchForIndex = 0;
                }
            }

            //we got to the end and didnt find the sequence
            return -1;
        }
    }
}
