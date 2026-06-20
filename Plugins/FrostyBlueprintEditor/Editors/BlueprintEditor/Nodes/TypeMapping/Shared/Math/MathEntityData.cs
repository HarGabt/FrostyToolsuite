using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;
using FrostyEditor;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Math
{
    public class MathNode : EntityNode
    {
        public override string ObjectType => "MathEntityData";
        public override string ToolTip => "This performs math operations on inputted values and outputs a result";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("In", ConnectionType.Event, Realm);
            AddOutput("Out", ConnectionType.Property, Realm);
            AddOutput("OnCalculate", ConnectionType.Event, Realm);

            UpdateInputs();
            
            ParseInstructions();
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);

            if (args.Item.Name == "Instructions")
            {
                switch (args.ModifiedArgs.Type)
                {
                    case ItemModifiedTypes.Remove:
                    {
                        dynamic instruction = (dynamic)args.OldValue;
                        string code = instruction.Code.ToString();
                        if (code.StartsWith("MathOpCode_Input"))
                        {
                            EntityInput input = GetInput((int)instruction.Param1, ConnectionType.Property);
                            RemoveInput(input);
                        }
                    } break;
                    case ItemModifiedTypes.Clear:
                    {
                        List<IPort> inputs = Inputs.ToList();

                        for (var i = 1; i < inputs.Count; i++)
                        {
                            IPort input = inputs[i];
                            RemoveInput((EntityInput)input);
                        }
                    } break;
                }
            }
            else if (args.Item.Name == "Param1")
            {
                dynamic instruction = args.Item.Parent.Value;
                
                string code = instruction.Code.ToString();
                if (code.StartsWith("MathOpCode_Input"))
                {
                    EntityInput input = GetInput((int)args.OldValue, ConnectionType.Property);
                    input.Name = Utils.GetString((int)args.NewValue);
                    RefreshCache();
                }
            }
            
            UpdateInputs();
        }

        public override void BuildFooter()
        {
            ParseInstructions();
        }

        private void UpdateInputs()
        {
            dynamic assembly = TryGetProperty("Assembly");
            foreach (dynamic instruction in assembly.Instructions)
            {
                switch (instruction.Code.ToString())
                {
                    case "MathOpCode_InputV4":
                    case "MathOpCode_InputV3":
                    case "MathOpCode_InputV2":
                    case "MathOpCode_InputT":
                    case "MathOpCode_InputF":
                    case "MathOpCode_InputI":
                    case "MathOpCode_InputB":
                    {
                        if (GetInput(Utils.GetString(instruction.Param1), ConnectionType.Property) != null)
                            continue;
                        
                        AddInput(Utils.GetString(instruction.Param1), ConnectionType.Property, Realm);
                    } break;
                }
            }
        }

        private void ParseInstructions()
        {
            ClearFooter();
            
            // each type is assigned 32 registers (as of NFS15, may differ for other games)
            // since nothing is being calculated here, all the registers will just contain strings to make it easier to build the final expression
            string[] transformReg = new string[32];
            string[] vec4Reg = new string[32];
            string[] vec3Reg = new string[32];
            string[] vec2Reg = new string[32];
            string[] intReg = new string[32];
            string[] floatReg = new string[32];
            string[] boolReg = new string[32];
            
            dynamic assembly = TryGetProperty("Assembly");
            foreach (dynamic instruction in assembly.Instructions)
            {
                switch (instruction.Code.ToString())
                {
                    // constants
                    case "MathOpCode_ConstB": boolReg[instruction.Result] = $"{GetConstant<bool>(instruction.Param1)}"; break;
                    case "MathOpCode_ConstI": intReg[instruction.Result] = $"{GetConstant<int>(instruction.Param1)}"; break;
                    case "MathOpCode_ConstF": floatReg[instruction.Result] = $"{GetConstant<float>(instruction.Param1)}"; break;

                    // params
                    case "MathOpCode_InputB": boolReg[instruction.Result] = Utils.GetString(instruction.Param1); break;
                    case "MathOpCode_InputI": intReg[instruction.Result] = Utils.GetString(instruction.Param1); break;
                    case "MathOpCode_InputF": floatReg[instruction.Result] = Utils.GetString(instruction.Param1); break;
                    case "MathOpCode_InputV2": vec2Reg[instruction.Result] = Utils.GetString(instruction.Param1); break;
                    case "MathOpCode_InputV3": vec3Reg[instruction.Result] = Utils.GetString(instruction.Param1); break;
                    case "MathOpCode_InputV4": vec4Reg[instruction.Result] = Utils.GetString(instruction.Param1); break;
                    case "MathOpCode_InputT": transformReg[instruction.Result] = Utils.GetString(instruction.Param1); break;

                    // logical operators
                    case "MathOpCode_OrB": boolReg[instruction.Result] = $"{boolReg[instruction.Param1]} || {boolReg[instruction.Param2]}"; break;
                    case "MathOpCode_AndB": boolReg[instruction.Result] = $"{boolReg[instruction.Param1]} && {boolReg[instruction.Param2]}"; break;

                    // comparisons
                    case "MathOpCode_GreaterI": boolReg[instruction.Result] = $"{intReg[instruction.Param1]} > {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_GreaterF": boolReg[instruction.Result] = $"{floatReg[instruction.Param1]} > {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_GreaterEqI": boolReg[instruction.Result] = $"{intReg[instruction.Param1]} >= {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_GreaterEqF": boolReg[instruction.Result] = $"{floatReg[instruction.Param1]} >= {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_LessI": boolReg[instruction.Result] = $"{intReg[instruction.Param1]} < {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_LessF": boolReg[instruction.Result] = $"{floatReg[instruction.Param1]} < {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_LessEqI": boolReg[instruction.Result] = $"{intReg[instruction.Param1]} <= {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_LessEqF": boolReg[instruction.Result] = $"{floatReg[instruction.Param1]} <= {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_NotEqI": boolReg[instruction.Result] = $"{intReg[instruction.Param1]} != {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_NotEqF": boolReg[instruction.Result] = $"{floatReg[instruction.Param1]} != {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_NotEqB": boolReg[instruction.Result] = $"{boolReg[instruction.Param1]} + {boolReg[instruction.Param2]}"; break;
                    case "MathOpCode_EqI": boolReg[instruction.Result] = $"{intReg[instruction.Param1]} == {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_EqF": boolReg[instruction.Result] = $"{floatReg[instruction.Param1]} == {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_EqB": boolReg[instruction.Result] = $"{boolReg[instruction.Param1]} == {boolReg[instruction.Param2]}"; break;

                    // addition
                    case "MathOpCode_AddI": intReg[instruction.Result] = $"{intReg[instruction.Param1]} + {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_AddF": floatReg[instruction.Result] = $"{floatReg[instruction.Param1]} + {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_AddV2": vec2Reg[instruction.Result] = $"{vec2Reg[instruction.Param1]} + {vec2Reg[instruction.Param2]}"; break;
                    case "MathOpCode_AddV3": vec3Reg[instruction.Result] = $"{vec3Reg[instruction.Param1]} + {vec3Reg[instruction.Param2]}"; break;
                    case "MathOpCode_AddV4": vec4Reg[instruction.Result] = $"{vec4Reg[instruction.Param1]} + {vec4Reg[instruction.Param2]}"; break;

                    // subtraction
                    case "MathOpCode_SubI": intReg[instruction.Result] = $"{intReg[instruction.Param1]} - {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_SubF": floatReg[instruction.Result] = $"{floatReg[instruction.Param1]} - {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_SubV2": vec2Reg[instruction.Result] = $"{vec2Reg[instruction.Param1]} - {vec2Reg[instruction.Param2]}"; break;
                    case "MathOpCode_SubV3": vec3Reg[instruction.Result] = $"{vec3Reg[instruction.Param1]} - {vec3Reg[instruction.Param2]}"; break;
                    case "MathOpCode_SubV4": vec4Reg[instruction.Result] = $"{vec4Reg[instruction.Param1]} - {vec4Reg[instruction.Param2]}"; break;

                    // multiplication
                    case "MathOpCode_MulF": floatReg[instruction.Result] = $"{floatReg[instruction.Param1]} * {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_MulI": intReg[instruction.Result] = $"{intReg[instruction.Param1]} * {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_MulV2F": vec2Reg[instruction.Result] = $"{vec2Reg[instruction.Param1]} * {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_MulV3F": vec3Reg[instruction.Result] = $"{vec3Reg[instruction.Param1]} * {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_MulV4F": vec4Reg[instruction.Result] = $"{vec4Reg[instruction.Param1]} * {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_MulV2I": vec2Reg[instruction.Result] = $"{vec2Reg[instruction.Param1]} * {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_MulV3I": vec3Reg[instruction.Result] = $"{vec3Reg[instruction.Param1]} * {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_MulV4I": vec4Reg[instruction.Result] = $"{vec4Reg[instruction.Param1]} * {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_MulT": transformReg[instruction.Result] = $"{transformReg[instruction.Param1]} * {transformReg[instruction.Param2]}"; break;

                    // division
                    case "MathOpCode_DivI": intReg[instruction.Result] = $"{intReg[instruction.Param1]} / {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_DivF": floatReg[instruction.Result] = $"{floatReg[instruction.Param1]} / {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_DivV2F": vec2Reg[instruction.Result] = $"{vec2Reg[instruction.Param1]} / {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_DivV3F": vec3Reg[instruction.Result] = $"{vec3Reg[instruction.Param1]} / {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_DivV4F": vec4Reg[instruction.Result] = $"{vec4Reg[instruction.Param1]} / {floatReg[instruction.Param2]}"; break;
                    case "MathOpCode_DivV2I": vec2Reg[instruction.Result] = $"{vec2Reg[instruction.Param1]} / {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_DivV3I": vec3Reg[instruction.Result] = $"{vec3Reg[instruction.Param1]} / {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_DivV4I": vec4Reg[instruction.Result] = $"{vec4Reg[instruction.Param1]} / {intReg[instruction.Param2]}"; break;

                    // modulo
                    case "MathOpCode_ModI": intReg[instruction.Result] = $"{intReg[instruction.Param1]} % {intReg[instruction.Param2]}"; break;

                    // negation
                    case "MathOpCode_NegI": intReg[instruction.Result] = $"-{intReg[instruction.Param1]}"; break;
                    case "MathOpCode_NegF": floatReg[instruction.Result] = $"-{floatReg[instruction.Param1]}"; break;
                    case "MathOpCode_NegV2": vec2Reg[instruction.Result] = $"-{vec2Reg[instruction.Param1]}"; break;
                    case "MathOpCode_NegV3": vec3Reg[instruction.Result] = $"-{vec3Reg[instruction.Param1]}"; break;
                    case "MathOpCode_NegV4": vec4Reg[instruction.Result] = $"-{vec4Reg[instruction.Param1]}"; break;
                    case "MathOpCode_NotB": boolReg[instruction.Result] = $"!{boolReg[instruction.Param1]}"; break;

                    // exponent
                    case "MathOpCode_PowI": intReg[instruction.Result] = $"{intReg[instruction.Param1]} ^ {intReg[instruction.Param2]}"; break;
                    case "MathOpCode_PowF": floatReg[instruction.Result] = $"{floatReg[instruction.Param1]} ^ {floatReg[instruction.Param2]}"; break;

                    // field accessors
                    case "MathOpCode_FieldV2":
                        {
                            switch (instruction.Param2)
                            {
                                case 0: floatReg[instruction.Result] = $"{vec2Reg[instruction.Param1]}.x"; break;
                                case 1: floatReg[instruction.Result] = $"{vec2Reg[instruction.Param1]}.y"; break;
                            }
                            break;
                        }
                    case "MathOpCode_FieldV3":
                        {
                            switch (instruction.Param2)
                            {
                                case 0: floatReg[instruction.Result] = $"{vec3Reg[instruction.Param1]}.x"; break;
                                case 1: floatReg[instruction.Result] = $"{vec3Reg[instruction.Param1]}.y"; break;
                                case 2: floatReg[instruction.Result] = $"{vec3Reg[instruction.Param1]}.z"; break;
                            }
                            break;
                        }
                    case "MathOpCode_FieldV4":
                        {
                            switch (instruction.Param2)
                            {
                                case 0: floatReg[instruction.Result] = $"{vec4Reg[instruction.Param1]}.x"; break;
                                case 1: floatReg[instruction.Result] = $"{vec4Reg[instruction.Param1]}.y"; break;
                                case 2: floatReg[instruction.Result] = $"{vec4Reg[instruction.Param1]}.z"; break;
                                case 3: floatReg[instruction.Result] = $"{vec4Reg[instruction.Param1]}.w"; break;
                            }
                            break;
                        }
                    case "MathOpCode_FieldT":
                        {
                            switch (instruction.Param2)
                            {
                                case 0: vec3Reg[instruction.Result] = $"{transformReg[instruction.Param1]}.left"; break;
                                case 1: vec3Reg[instruction.Result] = $"{transformReg[instruction.Param1]}.up"; break;
                                case 2: vec3Reg[instruction.Result] = $"{transformReg[instruction.Param1]}.forward"; break;
                                case 3: vec3Reg[instruction.Result] = $"{transformReg[instruction.Param1]}.trans"; break;
                            }
                            break;
                        }
                    
                    // functions
                    // these are all the functions available as of NFS15, later games probably have more
                    case "MathOpCode_Func":
                        {
                            List<uint> callParams = ((dynamic)Object).Assembly.FunctionCalls[instruction.Param2].Parameters;
                            switch ((uint)instruction.Param1)
                            {
                                case 0x7C6D8553: /* absf */ floatReg[instruction.Result] = $"absf({floatReg[callParams[0]]})"; break;
                                case 0x7C6D855C: /* absi */ intReg[instruction.Result] = $"absi({intReg[callParams[0]]})"; break;
                                case 0x7C6F98E6: /* mini */ intReg[instruction.Result] = $"mini({intReg[callParams[0]]}, {intReg[callParams[1]]})"; break;
                                case 0x7C6F98E9: /* minf */ floatReg[instruction.Result] = $"minf({floatReg[callParams[0]]}, {floatReg[callParams[1]]})"; break;
                                case 0x7C6FB9B8: /* maxi */ intReg[instruction.Result] = $"maxi({intReg[callParams[0]]}, {intReg[callParams[1]]})"; break;
                                case 0x7C6FB9B7: /* maxf */ floatReg[instruction.Result] = $"maxf({floatReg[callParams[0]]}, {floatReg[callParams[1]]})"; break;
                                case 0x0B874C7A: /* cos */ floatReg[instruction.Result] = $"cos({floatReg[callParams[0]]})"; break;
                                case 0x0B8790B1: /* sin */ floatReg[instruction.Result] = $"sin({floatReg[callParams[0]]})"; break;
                                case 0x0B8761FE: /* tan */ floatReg[instruction.Result] = $"tan({floatReg[callParams[0]]})"; break;
                                case 0x7C6DA01B: /* acos */ floatReg[instruction.Result] = $"acos({floatReg[callParams[0]]})"; break;
                                case 0x7C6DE550: /* asin */ floatReg[instruction.Result] = $"asin({floatReg[callParams[0]]})"; break;
                                case 0x7C6DBE1F: /* atan */ floatReg[instruction.Result] = $"atan({floatReg[callParams[0]]})"; break;
                                case 0x0BA1C827: /* sqrtf */ floatReg[instruction.Result] = $"sqrtf({floatReg[callParams[0]]})"; break;
                                case 0x0BA1C828: /* sqrti */ intReg[instruction.Result] = $"sqrti({intReg[callParams[0]]})"; break;
                                case 0x0A595A68: /* lerpf */ floatReg[instruction.Result] = $"lerpf({floatReg[callParams[0]]}, {floatReg[callParams[1]]}, {floatReg[callParams[2]]})"; break;
                                case 0x5BFD6DD0: /* clampf */ floatReg[instruction.Result] = $"clampf({floatReg[callParams[0]]}, {floatReg[callParams[1]]}, {floatReg[callParams[2]]})"; break;
                                case 0x5BFD6DDF: /* clampi */ intReg[instruction.Result] = $"clampi({intReg[callParams[0]]}, {intReg[callParams[1]]}, {intReg[callParams[2]]})"; break;
                                case 0x50FECABC: /* floati */ floatReg[instruction.Result] = $"float({intReg[callParams[0]]})"; break;
                                case 0x50FECAB7: /* floatb */ floatReg[instruction.Result] = $"float({boolReg[callParams[0]]})"; break;
                                case 0x7C71B5B0: /* intf */ intReg[instruction.Result] = $"int({floatReg[callParams[0]]})"; break;
                                case 0x7C71B5B4: /* intb */ intReg[instruction.Result] = $"int({boolReg[callParams[0]]})"; break;
                                case 0x0B9925E7: /* round */ floatReg[instruction.Result] = $"round({floatReg[callParams[0]]})"; break;
                                case 0x7C70B006: /* ceil */ floatReg[instruction.Result] = $"ceil({floatReg[callParams[0]]})"; break;
                                case 0x0A36467D: /* floor */ floatReg[instruction.Result] = $"floor({floatReg[callParams[0]]})"; break;
                                case 0x7C76EA27: /* vec2 */ vec2Reg[instruction.Result] = $"vec2({floatReg[callParams[0]]}, {floatReg[callParams[1]]})"; break;
                                case 0x7C76EA26: /* vec3 */ vec3Reg[instruction.Result] = $"vec3({floatReg[callParams[0]]}, {floatReg[callParams[1]]}, {floatReg[callParams[2]]})"; break;
                                case 0x7C76EA21: /* vec4 */ vec4Reg[instruction.Result] = $"vec4({floatReg[callParams[0]]}, {floatReg[callParams[1]]}, {floatReg[callParams[2]]}, {floatReg[callParams[3]]})"; break;
                                case 0x09CBC7BE: /* dotv2 */ floatReg[instruction.Result] = $"dotv2({vec2Reg[callParams[0]]}, {vec2Reg[callParams[1]]})"; break;
                                case 0x09CBC7BF: /* dotv3 */ floatReg[instruction.Result] = $"dotv3({vec3Reg[callParams[0]]}, {vec3Reg[callParams[1]]})"; break;
                                case 0x09CBC7B8: /* dotv4 */ floatReg[instruction.Result] = $"dotv4({vec4Reg[callParams[0]]}, {vec4Reg[callParams[1]]})"; break;
                                case 0x0A8EF17B: /* cross */ vec3Reg[instruction.Result] = $"cross({vec3Reg[callParams[0]]}, {vec3Reg[callParams[1]]})"; break;
                                case 0x623E329F: /* normv2 */ floatReg[instruction.Result] = $"normv2({vec2Reg[callParams[0]]})"; break;
                                case 0x623E329E: /* normv3 */ floatReg[instruction.Result] = $"normv3({vec3Reg[callParams[0]]})"; break;
                                case 0x623E3299: /* normv4 */ floatReg[instruction.Result] = $"normv4({vec4Reg[callParams[0]]})"; break;
                                case 0x5584A94A: /* lerpv2 */ vec2Reg[instruction.Result] = $"lerpv2({vec2Reg[callParams[0]]}, {vec2Reg[callParams[1]]}, {floatReg[callParams[2]]})"; break;
                                case 0x5584A94B: /* lerpv3 */ vec3Reg[instruction.Result] = $"lerpv3({vec3Reg[callParams[0]]}, {vec3Reg[callParams[1]]}, {floatReg[callParams[2]]})"; break;
                                case 0x5584A94C: /* lerpv4 */ vec4Reg[instruction.Result] = $"lerpv4({vec4Reg[callParams[0]]}, {vec4Reg[callParams[1]]}, {floatReg[callParams[2]]})"; break;
                                case 0x0BAD259D: /* slerp */ vec4Reg[instruction.Result] = $"slerp({vec3Reg[callParams[0]]}, {vec3Reg[callParams[1]]}, {floatReg[callParams[2]]})"; break;
                                case 0xECF435E2: /* distancev2 */ floatReg[instruction.Result] = $"distancev2({vec2Reg[callParams[0]]}, {vec2Reg[callParams[1]]})"; break;
                                case 0xECF435E3: /* distancev3 */ floatReg[instruction.Result] = $"distancev3({vec3Reg[callParams[0]]}, {vec3Reg[callParams[1]]})"; break;
                                case 0xECF435E4: /* distancev4 */ floatReg[instruction.Result] = $"distancev4({vec4Reg[callParams[0]]}, {vec4Reg[callParams[1]]})"; break;
                                case 0xEA9C0AB2: /* normalv2 */ vec2Reg[instruction.Result] = $"normalv2({vec2Reg[callParams[0]]})"; break;
                                case 0xEA9C0AB3: /* normalv3 */ vec3Reg[instruction.Result] = $"normalv3({vec3Reg[callParams[0]]})"; break;
                                case 0xEA9C0AB4: /* normalv4 */ vec4Reg[instruction.Result] = $"normalv4({vec4Reg[callParams[0]]})"; break;
                                case 0x30E0752E: /* translation */ transformReg[instruction.Result] = $"translation({vec3Reg[callParams[0]]})"; break;
                                case 0x32FB2E69: /* rotationx */ transformReg[instruction.Result] = $"rotationx({floatReg[callParams[0]]})"; break;
                                case 0x32FB2E68: /* rotationy */ transformReg[instruction.Result] = $"rotationy({floatReg[callParams[0]]})"; break;
                                case 0x32FB2E6B: /* rotationz */ transformReg[instruction.Result] = $"rotationz({floatReg[callParams[0]]})"; break;
                                case 0x0BAB517D: /* scale */
                                    // going off of NFS15, it can only use either 1 param or 3, there's no in between
                                    // could be different for other games
                                    if (callParams.Count <= 1) transformReg[instruction.Result] = $"scale({floatReg[callParams[0]]})";
                                    else transformReg[instruction.Result] = $"scale({floatReg[callParams[0]]}, {floatReg[callParams[1]]}, {floatReg[callParams[2]]})";
                                    break;
                                case 0xFEF06531: /* rotationAndTranslation */ transformReg[instruction.Result] = $"rotationAndTranslation({transformReg[callParams[0]]}, {vec3Reg[callParams[1]]})"; break;
                                case 0x6FF5791B: /* lookAtTransform */
                                    // requires at least 2 params
                                    if (callParams.Count <= 2) transformReg[instruction.Result] = $"lookAtTransform({vec3Reg[callParams[0]]}, {vec3Reg[callParams[1]]})";
                                    else transformReg[instruction.Result] = $"lookAtTransform({vec3Reg[callParams[0]]}, {vec3Reg[callParams[1]]}, {vec3Reg[callParams[2]]})";
                                    break;
                                case 0x569716D5: /* inverse */ transformReg[instruction.Result] = $"inverse({transformReg[callParams[0]]})"; break;
                                case 0xC7D3B8C6: /* fullInverse */ transformReg[instruction.Result] = $"fullInverse({transformReg[callParams[0]]})"; break;
                                case 0x7EBE7DDC: /* rotate */ vec3Reg[instruction.Result] = $"rotate({vec3Reg[callParams[0]]}, {transformReg[callParams[1]]})"; break;
                                case 0xF5E8DCED: /* invRotate */ vec3Reg[instruction.Result] = $"invRotate({vec3Reg[callParams[0]]}, {transformReg[callParams[1]]})"; break;
                                case 0x18BB7349: /* transform */ vec3Reg[instruction.Result] = $"transform({vec3Reg[callParams[0]]}, {transformReg[callParams[1]]})"; break;
                                case 0x72FCF358: /* invTransform */ vec3Reg[instruction.Result] = $"invTransform({vec3Reg[callParams[0]]}, {transformReg[callParams[1]]})"; break;
                                case 0x2AC63C95: /* isWorldSpaceTransform */ boolReg[instruction.Result] = $"isWorldSpaceTransform({transformReg[callParams[0]]})"; break;
                                case 0xE5C0729D: /* asWorldSpaceTransform */ transformReg[instruction.Result] = $"asWorldSpaceTransform({transformReg[callParams[0]]})"; break;
                                case 0x394FBBF2: /* asLocalSpaceTransform */ transformReg[instruction.Result] = $"asLocalSpaceTransform({transformReg[callParams[0]]})"; break;
                                case 0x0B875408: /* ifb */ boolReg[instruction.Result] = $"ifb({boolReg[callParams[0]]}, {boolReg[callParams[1]]}, {boolReg[callParams[2]]})"; break;
                                case 0x0B875403: /* ifi */ intReg[instruction.Result] = $"ifi({boolReg[callParams[0]]}, {intReg[callParams[1]]}, {intReg[callParams[2]]})"; break;
                                case 0x0B87540C: /* iff */ floatReg[instruction.Result] = $"iff({boolReg[callParams[0]]}, {floatReg[callParams[1]]}, {floatReg[callParams[2]]})"; break;
                                case 0x7C71D7AE: /* ifv2 */ vec2Reg[instruction.Result] = $"ifv2({boolReg[callParams[0]]}, {vec2Reg[callParams[1]]}, {vec2Reg[callParams[2]]})"; break;
                                case 0x7C71D7AF: /* ifv3 */ vec3Reg[instruction.Result] = $"ifv3({boolReg[callParams[0]]}, {vec3Reg[callParams[1]]}, {vec3Reg[callParams[2]]})"; break;
                                case 0x7C71D7A8: /* ifv4 */ vec4Reg[instruction.Result] = $"ifv4({boolReg[callParams[0]]}, {vec4Reg[callParams[1]]}, {vec4Reg[callParams[2]]})"; break;
                                case 0x0B87541E: /* ift */ transformReg[instruction.Result] = $"ift({boolReg[callParams[0]]}, {transformReg[callParams[1]]}, {transformReg[callParams[2]]})"; break;
                                case 0x7C79F742: /* xorb */ boolReg[instruction.Result] = $"xorb({boolReg[callParams[0]]}, {boolReg[callParams[1]]})"; break;

                                default: App.Logger.LogError($"Function {instruction.Param1} has not been implemented yet."); return;
                            }
                            break;
                        }
                    
                    // return
                    case "MathOpCode_Return":
                    {
                        switch ((int)instruction.Param1)
                        {
                            case 1: AddFooter(boolReg[instruction.Result]); return;
                            case 2: AddFooter(intReg[instruction.Result]); return;
                            case 4: AddFooter(floatReg[instruction.Result]); return;
                            case 8: AddFooter(vec2Reg[instruction.Result]); return;
                            case 16: AddFooter(vec3Reg[instruction.Result]); return;
                            case 32: AddFooter(vec4Reg[instruction.Result]); return;
                            case 64: AddFooter(transformReg[instruction.Result]); return;
                        }
                    } break;

                    default: App.Logger.LogError($"OpCode {instruction.Code} has not been implemented yet."); return;
                }
            }
        }
        
        private T GetConstant<T>(int constBuffer)
        {
            if (typeof(T) == typeof(bool)) return (T)(object)(constBuffer != 0);
            else if (typeof(T) == typeof(int)) return (T)(object)constBuffer;
            else if (typeof(T) == typeof(float)) return (T)(object)BitConverter.ToSingle(BitConverter.GetBytes(constBuffer), 0);
            return default(T);
        }
    }
}