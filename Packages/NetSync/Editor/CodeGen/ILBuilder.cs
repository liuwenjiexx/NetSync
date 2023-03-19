using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using OpCode = Mono.Cecil.Cil.OpCode;

namespace Yanmonet.NetSync.Editor.CodeGen
{
    /// <summary>
    /// OpCode: https://docs.microsoft.com/zh-cn/dotnet/api/system.reflection.emit.opcodes?view=net-5.0
    /// </summary>
    public sealed class ILBuilder
    {
        private MethodDefinition method;
        private ILProcessor il;
        private Instruction insertPoint;
        private Stack<Instruction> instructions = new Stack<Instruction>();

        public ILBuilder(ILProcessor il, MethodDefinition method)
        {
            if (il == null)
                throw new ArgumentNullException(nameof(il));
            this.il = il;
            this.method = method;
        }

        public ModuleDefinition Module { get => method.Module; }

        public AssemblyDefinition Assembly { get => method.Module.Assembly; }

        public MethodDefinition Method { get => method; }

        public ILProcessor ILProcessor { get => il; }

        public MethodBody Body { get => method.Body; }

        public Collection<VariableDefinition> Variables { get => method.Body.Variables; }

        public TypeSystem TypeSystem
        {
            get => Module.TypeSystem;
        }

        public Instruction InsertPoint { get => insertPoint; set => insertPoint = value; }

        public ILBuilder SetInsertPoint(Instruction insertPoint)
        {
            InsertPoint = insertPoint;
            return this;
        }

        public TypeReference ImportReference(Type type)
        {
            TypeReference tr = null;
            if (Module.Assembly.Name.Name == type.Assembly.GetName().Name)
            {
                tr = Module.GetType(type.FullName);
            }
            if (tr == null)
            {
                tr = Module.ImportReference(type);
            }
            return tr;
        }
        public MethodReference ImportReference(ConstructorInfo constructor)
        {
            return Module.ImportReference(constructor);
        }
        public MethodReference ImportReference(MethodBase method)
        {
            return Module.ImportReference(method);
        }
        public FieldReference ImportReference(FieldInfo field)
        {
            return Module.ImportReference(field);
        }

        public Instruction FirstOrCreate()
        {
            Instruction first;
            if (il.Body.Instructions.Count == 0)
            {
                first = il.Create(OpCodes.Nop);
                il.Append(first);
            }
            else
            {
                first = il.Body.Instructions[0];
            }
            return first;
        }

        public Instruction Last()
        {
            return il.Body.Instructions.Last();
        }
        public ILBuilder CreateInsertPoint()
        {
            return CreateInsertPoint(insertPoint);
        }
        public ILBuilder CreateInsertPoint(Instruction target)
        {
            Instruction insertPoint;

            if (target.OpCode == OpCodes.Ret)
            {
                target.OpCode = OpCodes.Nop;
                insertPoint = il.Create(OpCodes.Ret);
                il.InsertAfter(target, insertPoint);
            }
            else
            {
                insertPoint = il.Create(OpCodes.Nop);
                il.InsertAfter(target, insertPoint);
            }
            InsertPoint = insertPoint;
            return this;
        }

        public ILBuilder Emit(Instruction instruction)
        {
            il.InsertBefore(insertPoint, instruction);
            return this;
        }

        public ILBuilder Emit(OpCode opCode)
        {
            return Emit(il.Create(opCode));
        }
        public ILBuilder Emit(OpCode opCode, Instruction instruction)
        {
            return Emit(il.Create(opCode, instruction));
        }

        public ILBuilder Emit(OpCode opCode, VariableDefinition variable)
        {
            return Emit(il.Create(opCode, variable));
        }

        public ILBuilder Emit(OpCode opCode, TypeReference type)
        {
            return Emit(il.Create(opCode, type));
        }
        public ILBuilder Emit(OpCode opCode, MethodReference method)
        {
            return Emit(il.Create(opCode, method));
        }
        public ILBuilder Emit(OpCode opCode, FieldReference field)
        {
            return Emit(il.Create(opCode, field));
        }
        public ILBuilder Emit(OpCode opCode, string value)
        {
            return Emit(il.Create(opCode, value));
        }
        public ILBuilder Emit(OpCode opCode, int value)
        {
            return Emit(il.Create(opCode, value));
        }
        public ILBuilder Emit(OpCode opCode, long value)
        {
            return Emit(il.Create(opCode, value));
        }
        public ILBuilder Emit(OpCode opCode, byte value)
        {
            return Emit(il.Create(opCode, value));
        }
        public ILBuilder Emit(OpCode opCode, float value)
        {
            return Emit(il.Create(opCode, value));
        }
        public ILBuilder Emit(OpCode opCode, double value)
        {
            return Emit(il.Create(opCode, value));
        }


        public ILBuilder Load(string value)
        {
            return Emit(il.Create(OpCodes.Ldstr, value));
        }
        public ILBuilder Load(int value)
        {
            return Emit(il.Create(OpCodes.Ldc_I4, value));
        }
        public ILBuilder Load(long value)
        {
            return Emit(il.Create(OpCodes.Ldc_I8, value));
        }
        public ILBuilder Load(float value)
        {
            return Emit(il.Create(OpCodes.Ldc_R4, value));
        }
        public ILBuilder Load(double value)
        {
            return Emit(il.Create(OpCodes.Ldc_R8, value));
        }

        public ILBuilder Load(object value, TypeReference targetType)
        {
            if (value == null)
            {
                LoadNull();
                return this;
            }

            var variable = value as VariableDefinition;
            if (variable != null)
            {
                Load(variable, targetType);
                return this;
            }
            if (value is string)
            {
                Load((string)value);
                return this;
            }
            Type type = value.GetType();
            if (type.IsPrimitive)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int32:
                        Load((int)value);
                        break;
                    case TypeCode.Int64:
                        Load((long)value);
                        break;
                    case TypeCode.Single:
                        Load((float)value);
                        break;
                    case TypeCode.Double:
                        Load((double)value);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Box(type, targetType);
            }
            else
            {
                throw new NotImplementedException();
            }
            return this;
        }

        public ILBuilder Load(VariableDefinition variable)
        {
            return Emit(OpCodes.Ldloc, variable);
        }
        public ILBuilder Load(MemberReference member)
        {
            var field = member as FieldReference;

            if (field != null)
                Emit(OpCodes.Ldsfld, field);
            return this;
        }

        public ILBuilder LoadNull()
        {
            return Emit(OpCodes.Ldnull);
        }

        public ILBuilder Box(TypeReference fromType, TypeReference toType)
        {
            if (fromType.IsValueType != toType.IsValueType)
            {
                if (fromType.IsValueType)
                {
                    Emit(OpCodes.Box, fromType);
                }
                else
                {
                    Emit(OpCodes.Unbox_Any, toType);
                }
            }
            return this;
        }
        public ILBuilder Box(Type fromType, TypeReference toType)
        {
            if (fromType.IsValueType != toType.IsValueType)
            {
                if (fromType.IsValueType)
                {
                    Emit(OpCodes.Box, ImportReference(fromType));
                }
                else
                {
                    Emit(OpCodes.Unbox_Any, toType);
                }
            }
            return this;
        }

        public ILBuilder Load(VariableDefinition variable, TypeReference targetType)
        {
            Emit(OpCodes.Ldloc, variable);
            Box(variable.VariableType, targetType);
            return this;
        }


        public ILBuilder Set(VariableDefinition variable)
        {
            return Emit(OpCodes.Stloc, variable);
        }
        public ILBuilder Set(VariableDefinition variable, TypeReference valueType)
        {
            Box(valueType, variable.VariableType);
            return Emit(OpCodes.Stloc, variable);
        }
        public ILBuilder SetNull(VariableDefinition variable)
        {
            Emit(OpCodes.Ldnull);
            Set(variable);
            return this;
        }
        public ILBuilder New(Type type)
        {
            return New(type, Type.EmptyTypes);
        }
        public ILBuilder New(Type type, params Type[] argTypes)
        {
            var constructor = type.GetConstructor(argTypes);
            if (constructor == null)
                throw new ArgumentException($"Not found constructor, Type: {type.Name}", nameof(type));
            return New(ImportReference(constructor));
        }
        public ILBuilder New(MethodReference constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException(nameof(constructor));
            Emit(OpCodes.Newobj, constructor);
            return this;
        }

        //public ILBuilder Load(MethodDefinition method)
        //{
        //    ILProcessor.LoadMethod(insertPoint, method);

        //    return this;
        //}

        public ILBuilder LoadThis()
        {
            if (method.IsStatic)
                Emit(OpCodes.Ldnull);
            else
                Emit(OpCodes.Ldarg_0);
            return this;
        }

        public ILBuilder Call(MethodInfo method, object obj, params object[] args)
        {
            return Call(Module.ImportReference(method), obj, method.IsVirtual, args);
        }

        public ILBuilder Call(MethodReference method, object obj, params object[] args)
        {
            return Call(method, obj, method.HasThis, args);
        }
        private ILBuilder Call(MethodReference method, object obj, bool virt, params object[] args)
        {
            if (method.HasThis)
            {
                if (obj == null)
                    throw new ArgumentNullException(nameof(obj));
                if (obj is VariableDefinition)
                {
                    Load((VariableDefinition)obj);
                }
                else if (obj is MemberReference)
                {
                    Load((MemberReference)obj);
                }
                else
                {
                    Load(obj, method.DeclaringType);
                }
            }
            Call(method, method.HasThis, args);
            return this;
        }
        public ILBuilder Call(MethodReference method)
        {
            if (method.HasThis)
                return Emit(OpCodes.Callvirt, method);
            return Emit(OpCodes.Call, method);
        }
        public ILBuilder Call(MethodBase method)
        {
            if (method.IsVirtual)
                return Emit(OpCodes.Callvirt, ImportReference(method));
            return Emit(OpCodes.Call, ImportReference(method));
        }

        void Call(MethodReference method, bool virt, object[] args)
        {
            var ps = method.Parameters;
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                Load(arg, ps[i].ParameterType);
            }
            if (method.HasThis)
                Emit(OpCodes.Callvirt, method);
            else
                Emit(OpCodes.Call, method);
        }

        public ILBuilder Get(PropertyInfo property, VariableDefinition obj)
        {
            var getter = property.GetGetMethod();
            if (getter == null)
                throw new ArgumentException($"Property {property.Name} not getter", nameof(property));

            return Call(getter, obj);
        }

        public ILBuilder IfNull()
        {
            return IfNull(instructions.Peek(), true);
        }

        public ILBuilder IfNull(Instruction falsePoint)
        {
            return IfNull(falsePoint, true);
        }
        public ILBuilder IfNotNull()
        {
            return IfNull(instructions.Peek(), false);
        }
        public ILBuilder IfNotNull(Instruction falseGoto)
        {
            return IfNull(falseGoto, false);
        }
        private ILBuilder IfNull(Instruction gotoPoint, bool isTrue)
        {
            if (isTrue)
                Emit(OpCodes.Brtrue, gotoPoint);
            else
                Emit(OpCodes.Brfalse, gotoPoint);
            return this;
        }
        public ILBuilder IfTrue()
        {
            return Emit(OpCodes.Brfalse, instructions.Peek());
        }
        public ILBuilder IfTrueBreakBlock()
        {
            return Emit(OpCodes.Brfalse, instructions.Peek());
        }

        public ILBuilder IfFalse()
        {
            return Emit(OpCodes.Brtrue, instructions.Peek());
        }
        public ILBuilder IfFalseBreakBlock()
        {
            return Emit(OpCodes.Brtrue, instructions.Peek());
        }
        public ILBuilder BeginBlock()
        {
            var begin = il.Create(OpCodes.Nop);
            var end = il.Create(OpCodes.Nop);
            Emit(begin);
            instructions.Push(end);
            return this;
        }
        public ILBuilder EndBlock()
        {
            var end = instructions.Pop();
            return Emit(end);
        }

        public ILBuilder Nop()
        {
            return Emit(OpCodes.Nop);
        }
        public ILBuilder Nop(out Instruction nop)
        {
            nop = il.Create(OpCodes.Nop);
            return Emit(nop);
        }

        public ILBuilder Pop()
        {
            return Emit(OpCodes.Pop);
        }
        public ILBuilder Throw()
        {
            return Emit(OpCodes.Throw);
        }
        public ILBuilder Throw(VariableDefinition ex)
        {
            Emit(OpCodes.Ldloc, ex);
            return Emit(OpCodes.Throw);
        }

        public VariableDefinition NewVariable(TypeReference type)
        {
            var var = new VariableDefinition(type);
            Body.Variables.Add(var);
            return var;
        }

        public VariableDefinition NewArrayVariable(  TypeReference elementType, int length)
        {
            var arrayVar = NewVariable(new ArrayType(elementType));

            Load(length);
            Emit(OpCodes.Newarr, elementType);
            Emit(OpCodes.Stloc, arrayVar);
            return arrayVar;
        }

        public Instruction NopBefore(   )
        {
            var nop = il.Create(OpCodes.Nop);
            il.InsertBefore(InsertPoint, nop);
            return nop;
        }
        public Instruction NopAfter(   )
        {
            var nop = il.Create(OpCodes.Nop);
            il.InsertAfter(InsertPoint, nop);
            return nop;
        }
    }
}