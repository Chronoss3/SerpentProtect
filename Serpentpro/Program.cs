using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace Serpentpro
{
    class Program
    {
        public static void Main(string[] args)
        {
            ModuleDef mod = ModuleDefMD.Load(args[0]);
            ControlFlowPhase(mod);
            base64(mod);
            Rename(mod);
            mod.Write(args[0] + "-SerpentProtect.exe" , new ModuleWriterOptions(mod)
            {
                PEHeadersOptions = { NumberOfRvaAndSizes = 13 },
                Logger = DummyLogger.NoThrowInstance
            });
        }

        public static void ControlFlowPhase(ModuleDef module)
        {
            foreach (TypeDef type in module.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                if (method.Body.Instructions[i].IsLdcI4())
                {
                    int numorig = new Random(Guid.NewGuid().GetHashCode()).Next();
                    int div = new Random(Guid.NewGuid().GetHashCode()).Next();
                    int num = numorig ^ div;

                    Instruction nop = OpCodes.Nop.ToInstruction();

                    Local local = new Local(method.Module.ImportAsTypeSig(typeof(int)));
                    method.Body.Variables.Add(local);

                    method.Body.Instructions.Insert(i + 1, OpCodes.Stloc.ToInstruction(local));
                    method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Ldc_I4, method.Body.Instructions[i].GetLdcI4Value() - sizeof(float)));
                    method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Ldc_I4, num));
                    method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Ldc_I4, div));
                    method.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Xor));
                    method.Body.Instructions.Insert(i + 6, Instruction.Create(OpCodes.Ldc_I4, numorig));
                    method.Body.Instructions.Insert(i + 7, Instruction.Create(OpCodes.Bne_Un, nop));
                    method.Body.Instructions.Insert(i + 8, Instruction.Create(OpCodes.Ldc_I4, 2));
                    method.Body.Instructions.Insert(i + 9, OpCodes.Stloc.ToInstruction(local));
                    method.Body.Instructions.Insert(i + 10, Instruction.Create(OpCodes.Sizeof, method.Module.Import(typeof(float))));
                    method.Body.Instructions.Insert(i + 11, Instruction.Create(OpCodes.Add));
                    method.Body.Instructions.Insert(i + 12, nop);
                    i += 12;
                }
            }
                }
            }
        }
        public static void Rename(ModuleDef module)
        {
            foreach (TypeDef type in module.Types)
            {
                type.Name = RandomString(Random.Next(80, 120), Ascii);
                type.Namespace = RandomString(Random.Next(80, 120), Ascii);
                if (type.IsGlobalModuleType || type.IsRuntimeSpecialName || type.IsSpecialName || type.IsWindowsRuntime || type.IsInterface)
                {
                    continue;
                }
                foreach (MethodDef method in type.Methods)
                {
                    if (method.IsConstructor || method.IsRuntimeSpecialName || method.IsRuntime || method.IsStaticConstructor || method.IsVirtual) continue;
                    method.Name = RandomString(Random.Next(80, 120), Ascii);
                    foreach (var field in type.Fields)
                    {
                        field.Name = RandomString(Random.Next(80, 120), Ascii);
                        foreach (EventDef eventdef in type.Events)
                        {
                            eventdef.Name = RandomString(Random.Next(80, 120), Ascii);
                            foreach (PropertyDef property in type.Properties)
                            {
                                if (property.IsRuntimeSpecialName) continue;
                                property.Name = RandomString(Random.Next(80, 120), Ascii);
                            }
                        }
                    }
                }
            }
        }
        public static Random Random = new Random();
        public static string Ascii = "俺ム仮俺ム仮";
        private static string RandomString(int length, string chars)
        {
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
        public static void base64(ModuleDef mod)
       {
            foreach (TypeDef type in mod.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (method.Body == null) continue;
                    method.Body.SimplifyBranches();
                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                        {
                            string oldString = method.Body.Instructions[i].Operand.ToString();
                            string newString = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(oldString));
                            method.Body.Instructions[i].OpCode = OpCodes.Nop;
                            method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Call, mod.Import(typeof(System.Text.Encoding).GetMethod("get_UTF8", new Type[] { }))));
                            method.Body.Instructions.Insert(i + 2, new Instruction(OpCodes.Ldstr, newString));
                            method.Body.Instructions.Insert(i + 3, new Instruction(OpCodes.Call, mod.Import(typeof(System.Convert).GetMethod("FromBase64String", new Type[] { typeof(string) }))));
                            method.Body.Instructions.Insert(i + 4, new Instruction(OpCodes.Callvirt, mod.Import(typeof(System.Text.Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) }))));
                            i += 4;
                        }
                    }
                }
            }
        }
    }
}
