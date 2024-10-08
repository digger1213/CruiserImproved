﻿using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;

namespace CruiserImproved.Utils;

public class NullMemberException : Exception
{
    private NullMemberException(string message) : base(message) { }

    public static NullMemberException Method(Type type, string methodName, Type[] parameters = null, Type[] generics = null)
    {
        var paramStr = "";
        var genericStr = "";
        if(parameters != null && parameters.Length > 0) paramStr = string.Join(", ", parameters.Select(param => param.ToString()));
        if(generics != null && generics.Length > 0) genericStr = "<" + string.Join(", ", generics.Select(param => param.ToString())) + ">";

        return new NullMemberException($"Could not find method {type.Name}:{genericStr}{methodName}({paramStr})");
    }

    public static NullMemberException Field(Type type, string fieldName)
    {
        return new NullMemberException($"Could not find field {type.Name}.{fieldName}");
    }
}

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
            operandOrNull = operand;
        }
    }

    public static bool OperandCompare(object inputOperand, object codeInstructionOperand)
    {
        if (inputOperand.Equals(codeInstructionOperand)) return true;

        Type type = codeInstructionOperand.GetType();
        if (type == typeof(LocalBuilder))
        {
            return inputOperand.Equals(((LocalBuilder)codeInstructionOperand).LocalIndex);
        }

        object converted = Convert.ChangeType(inputOperand, codeInstructionOperand.GetType());
        if (converted == null) return false;

        return converted.Equals(codeInstructionOperand);
    }

    public static int LocateCodeSegment(int startIndex, List<CodeInstruction> searchSpace, List<OpcodeMatch> searchFor)
    {
        if (startIndex < 0 || startIndex >= searchSpace.Count) return -1;

        int searchForIndex = 0;
        for (int searchSpaceIndex = startIndex; searchSpaceIndex < searchSpace.Count; searchSpaceIndex++)
        {
            CodeInstruction check = searchSpace[searchSpaceIndex];
            OpcodeMatch currentMatch = searchFor[searchForIndex];
            bool match = check.opcode == currentMatch.opcode;

            //try comparing operands if we have one
            if (match && currentMatch.operandOrNull != null)
            {
                match = OperandCompare(currentMatch.operandOrNull, check.operand);
            }
            if (match)
            {
                searchForIndex++;
                if (searchForIndex == searchFor.Count)
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

    public static string GetAllMaskLayers(LayerMask _mask)
    {
        var bitmask = _mask.value;
        string text = "";
        for (int i = 0; i < 32; i++)
        {
            if ((1 << i & bitmask) != 0)
            {
                text = $"{text} {LayerMask.LayerToName(i)}";
            }
        }
        return text;
    }

    public static bool TryMethod(Type type, string name, out MethodInfo methodInfo)
    {
        return TryMethod(type, name, null, null, out methodInfo);
    }
    public static bool TryMethod(Type type, string name, Type[] parameters, out MethodInfo methodInfo)
    {
        return TryMethod(type, name, parameters, null, out methodInfo);
    }

    public static bool TryMethod(Type type, string name, Type[] parameters, Type[] generics, out MethodInfo methodInfo)
    {
        if (parameters == null && generics == null) methodInfo = type.GetMethod(name, AccessTools.all);
        else
        {
            if (generics == null) generics = Type.EmptyTypes;
            if (parameters == null) parameters = Type.EmptyTypes;
            methodInfo = type.GetMethod(name, generics.Length, AccessTools.all, null, parameters, null);

            if(generics.Length > 0)
            {
                methodInfo = methodInfo.MakeGenericMethod(generics);
            }
        }

        return methodInfo != null;
    }

    public static MethodInfo Method(Type type, string name, Type[] parameters = null, Type[] generics = null)
    {
        if (!TryMethod(type, name, parameters, generics, out MethodInfo info))
        {
            throw NullMemberException.Method(type, name, parameters, generics);
        }
        return info;
    }

    public static bool TryField(Type type, string name, out FieldInfo fieldInfo)
    {
        fieldInfo = AccessTools.Field(type, name);
        return fieldInfo != null;
    }

    public static FieldInfo Field(Type type, string name)
    {
        if(!TryField(type, name, out FieldInfo info))
        {
            throw NullMemberException.Field(type, name);
        }
        return info;
    }
}