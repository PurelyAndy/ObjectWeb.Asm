using System;
using System.Collections.Generic;

// ASM: a very small and fast Java bytecode manipulation framework
// Copyright (c) 2000-2011 INRIA, France Telecom
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. Neither the name of the copyright holders nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
namespace ObjectWeb.Asm.Commons
{
    /// <summary>
    ///     A <seealso cref = "MethodVisitor"/> that keeps track of stack map frame changes between {@link
    ///     #visitFrame(int, int, Object[], int, Object[])} calls. This adapter must be used with the {@link
    ///     org.objectweb.asm.ClassReader#EXPAND_FRAMES} option. Each visit<i>X</i> instruction delegates to
    ///     the next visitor in the chain, if any, and then simulates the effect of this instruction on the
    ///     stack map frame, represented by <seealso cref = "locals"/> and <seealso cref = "stack"/>. The next visitor in the
    ///     chain
    ///     can get the state of the stack map frame <i>before</i> each instruction by reading the value of
    ///     these fields in its visit<i>X</i> methods (this requires a reference to the AnalyzerAdapter that
    ///     is before it in the chain). If this adapter is used with a class that does not contain stack map
    ///     table attributes (i.e., pre Java 6 classes) then this adapter may not be able to compute the
    ///     stack map frame for each instruction. In this case no exception is thrown but the <seealso cref = "locals"/>
    ///     and <seealso cref = "stack"/> fields will be null for these instructions.
    ///     @author Eric Bruneton
    /// </summary>
    public class AnalyzerAdapter : MethodVisitor
    {
        /// <summary>
        ///     The labels that designate the next instruction to be visited. May be {@literal null}.
        /// </summary>
        private List<Label> _labels;

        /// <summary>
        ///     The local variable slots for the current execution frame. Primitive types are represented by
        ///     <seealso cref = "IIOpcodes.top / > , <seealso cref = "IIOpcodes.integer / > , <seealso cref = "IIOpcodes. float  / > , 
        ///     <seealso cref = "IIOpcodes. long  / > , 
        ///     <seealso cref = "IIOpcodes. double  / > ,  <seealso cref = "IIOpcodes. null  / > or  <seealso cref = "IIOpcodes.uninitializedThis / > ///( long  and  ///double  are  represented  by  two  elements ,  the  second  one  being  TOP ) . Reference  types  are  ///represented  by  String  objects ( representing  internal  names ) ,  and  uninitialized  types  by  Label  ///objects ( this  label  designates  the  NEW  instruction  that  created  this  uninitialized  value ) . This  ///field  is  { @literal  null } for  unreachable  instructions .
        /// </summary>
        public List<object> Locals { get; set; }

        /// <summary>
        ///     The maximum number of local variables of this method.
        /// </summary>
        private int _maxLocals;

        /// <summary>
        ///     The maximum stack size of this method.
        /// </summary>
        private int _maxStack;

        /// <summary>
        ///     The owner's class name.
        /// </summary>
        private readonly string _owner;

        /// <summary>
        ///     The operand stack slots for the current execution frame. Primitive types are represented by
        ///     <seealso cref = "IIOpcodes.top / > , <seealso cref = "IIOpcodes.integer / > , <seealso cref = "IIOpcodes. float  / > , 
        ///     <seealso cref = "IIOpcodes. long  / > , 
        ///     <seealso cref = "IIOpcodes. double  / > ,  <seealso cref = "IIOpcodes. null  / > or  <seealso cref = "IIOpcodes.uninitializedThis / > ///( long  and  ///double  are  represented  by  two  elements ,  the  second  one  being  TOP ) . Reference  types  are  ///represented  by  String  objects ( representing  internal  names ) ,  and  uninitialized  types  by  Label  ///objects ( this  label  designates  the  NEW  instruction  that  created  this  uninitialized  value ) . This  ///field  is  { @literal  null } for  unreachable  instructions .
        /// </summary>
        public List<object> Stack { get; set; }

        /// <summary>
        ///     The uninitialized types in the current execution frame. This map associates internal names to
        ///     Label objects. Each label designates a NEW instruction that created the currently uninitialized
        ///     types, and the associated internal name represents the NEW operand, i.e. the final, initialized
        ///     type value.
        /// </summary>
        public IDictionary<object, object> UninitializedTypes { get; set; }

        /// <summary>
        ///     Constructs a new <seealso cref = "AnalyzerAdapter"/>. <i>Subclasses must not use this constructor</i>.
        ///     Instead, they must use the {@link #AnalyzerAdapter(int, String, int, String, String,
        ///     MethodVisitor)} version.
        /// </summary>
        /// <param name = "owner"> the owner's class name. </param>
        /// <param name = "access"> the method's access flags (see <seealso cref = "Opcodes"/>). </param>
        /// <param name = "name"> the method's name. </param>
        /// <param name = "descriptor"> the method's descriptor (see <seealso cref = "Type"/>). </param>
        /// <param name = "methodVisitor">
        ///     the method visitor to which this adapter delegates calls. May be {@literal
        ///     null}.
        /// </param>
        /// <exception cref = "IllegalStateException"> If a subclass calls this constructor. </exception>
        public AnalyzerAdapter(string owner, int access, string name, string descriptor, MethodVisitor methodVisitor) :
            this(Opcodes.Asm9, owner, access, name, descriptor, methodVisitor)
        {
            if (GetType() != typeof(AnalyzerAdapter))
                throw new InvalidOperationException();
        }

        /// <summary>
        ///     Constructs a new <seealso cref = "AnalyzerAdapter"/>.
        /// </summary>
        /// <param name = "api">
        ///     the ASM API version implemented by this visitor. Must be one of the {@code
        ///     ASM}<i>x</i> Values in <seealso cref = "Opcodes"/>.
        /// </param>
        /// <param name = "owner"> the owner's class name. </param>
        /// <param name = "access"> the method's access flags (see <seealso cref = "Opcodes"/>). </param>
        /// <param name = "name"> the method's name. </param>
        /// <param name = "descriptor"> the method's descriptor (see <seealso cref = "Type"/>). </param>
        /// <param name = "methodVisitor">
        ///     the method visitor to which this adapter delegates calls. May be {@literal
        ///     null}.
        /// </param>
        public AnalyzerAdapter(int api, string owner, int access, string name, string descriptor,
            MethodVisitor methodVisitor) : base(api, methodVisitor)
        {
            this._owner = owner;
            Locals = new List<object>();
            Stack = new List<object>();
            UninitializedTypes = new Dictionary<object, object>();
            if ((access & Opcodes.Acc_Static) == 0)
            {
                if ("<init>".Equals(name))
                    Locals.Add(Opcodes.uninitializedThis);
                else
                    Locals.Add(owner);
            }

            foreach (var argumentType in JType.GetArgumentTypes(descriptor))
                switch (argumentType.Sort)
                {
                    case JType.Boolean:
                    case JType.Char:
                    case JType.Byte:
                    case JType.Short:
                    case JType.Int:
                        Locals.Add(Opcodes.integer);
                        break;
                    case JType.Float:
                        Locals.Add(Opcodes.@float);
                        break;
                    case JType.Long:
                        Locals.Add(Opcodes.@long);
                        Locals.Add(Opcodes.top);
                        break;
                    case JType.Double:
                        Locals.Add(Opcodes.@double);
                        Locals.Add(Opcodes.top);
                        break;
                    case JType.Array:
                        Locals.Add(argumentType.Descriptor);
                        break;
                    case JType.Object:
                        Locals.Add(argumentType.InternalName);
                        break;
                    default:
                        throw new Exception();
                }

            _maxLocals = Locals.Count;
        }

        public override void VisitFrame(int type, int numLocal, object[] local, int numStack, object[] stack)
        {
            if (type != Opcodes.F_New)
                // Uncompressed frame.
                throw new ArgumentException(
                    "AnalyzerAdapter only accepts expanded frames (see ClassReader.EXPAND_FRAMES)");
            base.VisitFrame(type, numLocal, local, numStack, stack);
            if (Locals != null)
            {
                Locals.Clear();
                this.Stack.Clear();
            }
            else
            {
                Locals = new List<object>();
                this.Stack = new List<object>();
            }

            VisitFrameTypes(numLocal, local, Locals);
            VisitFrameTypes(numStack, stack, this.Stack);
            _maxLocals = Math.Max(_maxLocals, Locals.Count);
            _maxStack = Math.Max(_maxStack, this.Stack.Count);
        }

        private static void VisitFrameTypes(int numTypes, object[] frameTypes, List<object> result)
        {
            for (var i = 0; i < numTypes; ++i)
            {
                var frameType = frameTypes[i];
                result.Add(frameType);
                if (Equals(frameType, Opcodes.@long) || Equals(frameType, Opcodes.@double))
                    result.Add(Opcodes.top);
            }
        }

        public override void VisitInsn(int opcode)
        {
            base.VisitInsn(opcode);
            Execute(opcode, 0, null);
            if (opcode >= Opcodes.Ireturn && opcode <= Opcodes.Return || opcode == Opcodes.Athrow)
            {
                Locals = null;
                Stack = null;
            }
        }

        public override void VisitIntInsn(int opcode, int operand)
        {
            base.VisitIntInsn(opcode, operand);
            Execute(opcode, operand, null);
        }

        public override void VisitVarInsn(int opcode, int varIndex)
        {
            base.VisitVarInsn(opcode, varIndex);
            var isLongOrDouble = opcode == Opcodes.Lload || opcode == Opcodes.Dload || opcode == Opcodes.Lstore ||
                                 opcode == Opcodes.Dstore;
            _maxLocals = Math.Max(_maxLocals, varIndex + (isLongOrDouble ? 2 : 1));
            Execute(opcode, varIndex, null);
        }

        public override void VisitTypeInsn(int opcode, string type)
        {
            if (opcode == Opcodes.New)
            {
                if (_labels == null)
                {
                    var label = new Label();
                    _labels = new List<Label>(3);
                    _labels.Add(label);
                    if (mv != null)
                        mv.VisitLabel(label);
                }

                foreach (var label in _labels)
                    UninitializedTypes[label] = type;
            }

            base.VisitTypeInsn(opcode, type);
            Execute(opcode, 0, type);
        }

        public override void VisitFieldInsn(int opcode, string owner, string name, string descriptor)
        {
            base.VisitFieldInsn(opcode, owner, name, descriptor);
            Execute(opcode, 0, descriptor);
        }

        public override void VisitMethodInsn(int opcodeAndSource, string owner, string name, string descriptor,
            bool isInterface)
        {
            if (api < Opcodes.Asm5 && (opcodeAndSource & Opcodes.Source_Deprecated) == 0)
            {
                // Redirect the call to the deprecated version of this method.
                base.VisitMethodInsn(opcodeAndSource, owner, name, descriptor, isInterface);
                return;
            }

            base.VisitMethodInsn(opcodeAndSource, owner, name, descriptor, isInterface);
            var opcode = opcodeAndSource & ~Opcodes.Source_Mask;
            if (Locals == null)
            {
                _labels = null;
                return;
            }

            Pop(descriptor);
            if (opcode != Opcodes.Invokestatic)
            {
                var value = Pop();
                if (opcode == Opcodes.Invokespecial && name.Equals("<init>"))
                {
                    object initializedValue;
                    if (Equals(value, Opcodes.uninitializedThis))
                        initializedValue = this._owner;
                    else
                    {
                        UninitializedTypes.TryGetValue(value, out initializedValue);
                    }

                    for (var i = 0; i < Locals.Count; ++i)
                        if (Locals[i] == value)
                            Locals[i] = initializedValue;
                    for (var i = 0; i < Stack.Count; ++i)
                        if (Stack[i] == value)
                            Stack[i] = initializedValue;
                }
            }

            PushDescriptor(descriptor);
            _labels = null;
        }

        public override void VisitInvokeDynamicInsn(string name, string descriptor, Handle bootstrapMethodHandle,
            params object[] bootstrapMethodArguments)
        {
            base.VisitInvokeDynamicInsn(name, descriptor, bootstrapMethodHandle, bootstrapMethodArguments);
            if (Locals == null)
            {
                _labels = null;
                return;
            }

            Pop(descriptor);
            PushDescriptor(descriptor);
            _labels = null;
        }

        public override void VisitJumpInsn(int opcode, Label label)
        {
            base.VisitJumpInsn(opcode, label);
            Execute(opcode, 0, null);
            if (opcode == Opcodes.Goto)
            {
                Locals = null;
                Stack = null;
            }
        }

        public override void VisitLabel(Label label)
        {
            base.VisitLabel(label);
            if (_labels == null)
                _labels = new List<Label>(3);
            _labels.Add(label);
        }

        public override void VisitLdcInsn(object value)
        {
            base.VisitLdcInsn(value);
            if (Locals == null)
            {
                _labels = null;
                return;
            }

            if (value is int)
            {
                Push(Opcodes.integer);
            }
            else if (value is long)
            {
                Push(Opcodes.@long);
                Push(Opcodes.top);
            }
            else if (value is float)
            {
                Push(Opcodes.@float);
            }
            else if (value is double)
            {
                Push(Opcodes.@double);
                Push(Opcodes.top);
            }
            else if (value is string)
            {
                Push("java/lang/String");
            }
            else if (value is JType)
            {
                var sort = ((JType)value).Sort;
                if (sort == JType.Object || sort == JType.Array)
                    Push("java/lang/Class");
                else if (sort == JType.Method)
                    Push("java/lang/invoke/MethodType");
                else
                    throw new ArgumentException();
            }
            else if (value is Handle)
            {
                Push("java/lang/invoke/MethodHandle");
            }
            else if (value is ConstantDynamic)
            {
                PushDescriptor(((ConstantDynamic)value).Descriptor);
            }
            else
            {
                throw new ArgumentException();
            }

            _labels = null;
        }

        public override void VisitIincInsn(int varIndex, int increment)
        {
            base.VisitIincInsn(varIndex, increment);
            _maxLocals = Math.Max(_maxLocals, varIndex + 1);
            Execute(Opcodes.Iinc, varIndex, null);
        }

        public override void VisitTableSwitchInsn(int min, int max, Label dflt, params Label[] labels)
        {
            base.VisitTableSwitchInsn(min, max, dflt, labels);
            Execute(Opcodes.Tableswitch, 0, null);
            Locals = null;
            Stack = null;
        }

        public override void VisitLookupSwitchInsn(Label dflt, int[] keys, Label[] labels)
        {
            base.VisitLookupSwitchInsn(dflt, keys, labels);
            Execute(Opcodes.Lookupswitch, 0, null);
            Locals = null;
            Stack = null;
        }

        public override void VisitMultiANewArrayInsn(string descriptor, int numDimensions)
        {
            base.VisitMultiANewArrayInsn(descriptor, numDimensions);
            Execute(Opcodes.Multianewarray, numDimensions, descriptor);
        }

        public override void VisitLocalVariable(string name, string descriptor, string signature, Label start,
            Label end, int index)
        {
            var firstDescriptorChar = descriptor[0];
            _maxLocals = Math.Max(_maxLocals,
                index + (firstDescriptorChar == 'J' || firstDescriptorChar == 'D' ? 2 : 1));
            base.VisitLocalVariable(name, descriptor, signature, start, end, index);
        }

        public override void VisitMaxs(int maxStack, int maxLocals)
        {
            if (mv != null)
            {
                this._maxStack = Math.Max(this._maxStack, maxStack);
                this._maxLocals = Math.Max(this._maxLocals, maxLocals);
                mv.VisitMaxs(this._maxStack, this._maxLocals);
            }
        }

        // -----------------------------------------------------------------------------------------------
        private object Get(int local)
        {
            _maxLocals = Math.Max(_maxLocals, local + 1);
            return local < Locals.Count ? Locals[local] : Opcodes.top;
        }

        private void Set(int local, object type)
        {
            _maxLocals = Math.Max(_maxLocals, local + 1);
            while (local >= Locals.Count)
                Locals.Add(Opcodes.top);
            Locals[local] = type;
        }

        private void Push(object type)
        {
            Stack.Add(type);
            _maxStack = Math.Max(_maxStack, Stack.Count);
        }

        private void PushDescriptor(string fieldOrMethodDescriptor)
        {
            var descriptor = fieldOrMethodDescriptor[0] == '('
                ? JType.GetReturnType(fieldOrMethodDescriptor).Descriptor
                : fieldOrMethodDescriptor;
            switch (descriptor[0])
            {
                case 'V':
                    return;
                case 'Z':
                case 'C':
                case 'B':
                case 'S':
                case 'I':
                    Push(Opcodes.integer);
                    return;
                case 'F':
                    Push(Opcodes.@float);
                    return;
                case 'J':
                    Push(Opcodes.@long);
                    Push(Opcodes.top);
                    return;
                case 'D':
                    Push(Opcodes.@double);
                    Push(Opcodes.top);
                    return;
                case '[':
                    Push(descriptor);
                    break;
                case 'L':
                    Push(descriptor.Substring(1, descriptor.Length - 1 - 1));
                    break;
                default:
                    throw new Exception();
            }
        }

        private object Pop()
        {
            var stackCount = Stack.Count - 1;
            var current = Stack[stackCount];
            Stack.RemoveAt(stackCount);
            return current;
        }

        private void Pop(int numSlots)
        {
            var size = Stack.Count;
            var end = size - numSlots;
            for (var i = size - 1; i >= end; --i)
                Stack.RemoveAt(i);
        }

        private void Pop(string descriptor)
        {
            var firstDescriptorChar = descriptor[0];
            if (firstDescriptorChar == '(')
            {
                var numSlots = 0;
                var types = JType.GetArgumentTypes(descriptor);
                foreach (var type in types)
                    numSlots += type.Size;
                Pop(numSlots);
            }
            else if (firstDescriptorChar == 'J' || firstDescriptorChar == 'D')
            {
                Pop(2);
            }
            else
            {
                Pop(1);
            }
        }

        private void Execute(int opcode, int intArg, string stringArg)
        {
            if (opcode == Opcodes.Jsr || opcode == Opcodes.Ret)
                throw new ArgumentException("JSR/RET are not supported");
            if (Locals == null)
            {
                _labels = null;
                return;
            }

            object value1;
            object value2;
            object value3;
            object t4;
            switch (opcode)
            {
                case Opcodes.Nop:
                case Opcodes.Ineg:
                case Opcodes.Lneg:
                case Opcodes.Fneg:
                case Opcodes.Dneg:
                case Opcodes.I2B:
                case Opcodes.I2C:
                case Opcodes.I2S:
                case Opcodes.Goto:
                case Opcodes.Return:
                    break;
                case Opcodes.Aconst_Null:
                    Push(Opcodes.@null);
                    break;
                case Opcodes.Iconst_M1:
                case Opcodes.Iconst_0:
                case Opcodes.Iconst_1:
                case Opcodes.Iconst_2:
                case Opcodes.Iconst_3:
                case Opcodes.Iconst_4:
                case Opcodes.Iconst_5:
                case Opcodes.Bipush:
                case Opcodes.Sipush:
                    Push(Opcodes.integer);
                    break;
                case Opcodes.Lconst_0:
                case Opcodes.Lconst_1:
                    Push(Opcodes.@long);
                    Push(Opcodes.top);
                    break;
                case Opcodes.Fconst_0:
                case Opcodes.Fconst_1:
                case Opcodes.Fconst_2:
                    Push(Opcodes.@float);
                    break;
                case Opcodes.Dconst_0:
                case Opcodes.Dconst_1:
                    Push(Opcodes.@double);
                    Push(Opcodes.top);
                    break;
                case Opcodes.Iload:
                case Opcodes.Fload:
                case Opcodes.Aload:
                    Push(Get(intArg));
                    break;
                case Opcodes.Lload:
                case Opcodes.Dload:
                    Push(Get(intArg));
                    Push(Opcodes.top);
                    break;
                case Opcodes.Laload:
                case Opcodes.D2L:
                    Pop(2);
                    Push(Opcodes.@long);
                    Push(Opcodes.top);
                    break;
                case Opcodes.Daload:
                case Opcodes.L2D:
                    Pop(2);
                    Push(Opcodes.@double);
                    Push(Opcodes.top);
                    break;
                case Opcodes.Aaload:
                    Pop(1);
                    value1 = Pop();
                    if (value1 is string)
                        PushDescriptor(((string)value1).Substring(1));
                    else if (Equals(value1, Opcodes.@null))
                        Push(value1);
                    else
                        Push("java/lang/Object");
                    break;
                case Opcodes.Istore:
                case Opcodes.Fstore:
                case Opcodes.Astore:
                    value1 = Pop();
                    Set(intArg, value1);
                    if (intArg > 0)
                    {
                        value2 = Get(intArg - 1);
                        if (Equals(value2, Opcodes.@long) || Equals(value2, Opcodes.@double))
                            Set(intArg - 1, Opcodes.top);
                    }

                    break;
                case Opcodes.Lstore:
                case Opcodes.Dstore:
                    Pop(1);
                    value1 = Pop();
                    Set(intArg, value1);
                    Set(intArg + 1, Opcodes.top);
                    if (intArg > 0)
                    {
                        value2 = Get(intArg - 1);
                        if (Equals(value2, Opcodes.@long) || Equals(value2, Opcodes.@double))
                            Set(intArg - 1, Opcodes.top);
                    }

                    break;
                case Opcodes.Iastore:
                case Opcodes.Bastore:
                case Opcodes.Castore:
                case Opcodes.Sastore:
                case Opcodes.Fastore:
                case Opcodes.Aastore:
                    Pop(3);
                    break;
                case Opcodes.Lastore:
                case Opcodes.Dastore:
                    Pop(4);
                    break;
                case Opcodes.Pop:
                case Opcodes.Ifeq:
                case Opcodes.Ifne:
                case Opcodes.Iflt:
                case Opcodes.Ifge:
                case Opcodes.Ifgt:
                case Opcodes.Ifle:
                case Opcodes.Ireturn:
                case Opcodes.Freturn:
                case Opcodes.Areturn:
                case Opcodes.Tableswitch:
                case Opcodes.Lookupswitch:
                case Opcodes.Athrow:
                case Opcodes.Monitorenter:
                case Opcodes.Monitorexit:
                case Opcodes.Ifnull:
                case Opcodes.Ifnonnull:
                    Pop(1);
                    break;
                case Opcodes.Pop2:
                case Opcodes.If_Icmpeq:
                case Opcodes.If_Icmpne:
                case Opcodes.If_Icmplt:
                case Opcodes.If_Icmpge:
                case Opcodes.If_Icmpgt:
                case Opcodes.If_Icmple:
                case Opcodes.If_Acmpeq:
                case Opcodes.If_Acmpne:
                case Opcodes.Lreturn:
                case Opcodes.Dreturn:
                    Pop(2);
                    break;
                case Opcodes.Dup:
                    value1 = Pop();
                    Push(value1);
                    Push(value1);
                    break;
                case Opcodes.Dup_X1:
                    value1 = Pop();
                    value2 = Pop();
                    Push(value1);
                    Push(value2);
                    Push(value1);
                    break;
                case Opcodes.Dup_X2:
                    value1 = Pop();
                    value2 = Pop();
                    value3 = Pop();
                    Push(value1);
                    Push(value3);
                    Push(value2);
                    Push(value1);
                    break;
                case Opcodes.Dup2:
                    value1 = Pop();
                    value2 = Pop();
                    Push(value2);
                    Push(value1);
                    Push(value2);
                    Push(value1);
                    break;
                case Opcodes.Dup2_X1:
                    value1 = Pop();
                    value2 = Pop();
                    value3 = Pop();
                    Push(value2);
                    Push(value1);
                    Push(value3);
                    Push(value2);
                    Push(value1);
                    break;
                case Opcodes.Dup2_X2:
                    value1 = Pop();
                    value2 = Pop();
                    value3 = Pop();
                    t4 = Pop();
                    Push(value2);
                    Push(value1);
                    Push(t4);
                    Push(value3);
                    Push(value2);
                    Push(value1);
                    break;
                case Opcodes.Swap:
                    value1 = Pop();
                    value2 = Pop();
                    Push(value1);
                    Push(value2);
                    break;
                case Opcodes.Iaload:
                case Opcodes.Baload:
                case Opcodes.Caload:
                case Opcodes.Saload:
                case Opcodes.Iadd:
                case Opcodes.Isub:
                case Opcodes.Imul:
                case Opcodes.Idiv:
                case Opcodes.Irem:
                case Opcodes.Iand:
                case Opcodes.Ior:
                case Opcodes.Ixor:
                case Opcodes.Ishl:
                case Opcodes.Ishr:
                case Opcodes.Iushr:
                case Opcodes.L2I:
                case Opcodes.D2I:
                case Opcodes.Fcmpl:
                case Opcodes.Fcmpg:
                    Pop(2);
                    Push(Opcodes.integer);
                    break;
                case Opcodes.Ladd:
                case Opcodes.Lsub:
                case Opcodes.Lmul:
                case Opcodes.Ldiv:
                case Opcodes.Lrem:
                case Opcodes.Land:
                case Opcodes.Lor:
                case Opcodes.Lxor:
                    Pop(4);
                    Push(Opcodes.@long);
                    Push(Opcodes.top);
                    break;
                case Opcodes.Faload:
                case Opcodes.Fadd:
                case Opcodes.Fsub:
                case Opcodes.Fmul:
                case Opcodes.Fdiv:
                case Opcodes.Frem:
                case Opcodes.L2F:
                case Opcodes.D2F:
                    Pop(2);
                    Push(Opcodes.@float);
                    break;
                case Opcodes.Dadd:
                case Opcodes.Dsub:
                case Opcodes.Dmul:
                case Opcodes.Ddiv:
                case Opcodes.Drem:
                    Pop(4);
                    Push(Opcodes.@double);
                    Push(Opcodes.top);
                    break;
                case Opcodes.Lshl:
                case Opcodes.Lshr:
                case Opcodes.Lushr:
                    Pop(3);
                    Push(Opcodes.@long);
                    Push(Opcodes.top);
                    break;
                case Opcodes.Iinc:
                    Set(intArg, Opcodes.integer);
                    break;
                case Opcodes.I2L:
                case Opcodes.F2L:
                    Pop(1);
                    Push(Opcodes.@long);
                    Push(Opcodes.top);
                    break;
                case Opcodes.I2F:
                    Pop(1);
                    Push(Opcodes.@float);
                    break;
                case Opcodes.I2D:
                case Opcodes.F2D:
                    Pop(1);
                    Push(Opcodes.@double);
                    Push(Opcodes.top);
                    break;
                case Opcodes.F2I:
                case Opcodes.Arraylength:
                case Opcodes.Instanceof:
                    Pop(1);
                    Push(Opcodes.integer);
                    break;
                case Opcodes.Lcmp:
                case Opcodes.Dcmpl:
                case Opcodes.Dcmpg:
                    Pop(4);
                    Push(Opcodes.integer);
                    break;
                case Opcodes.Getstatic:
                    PushDescriptor(stringArg);
                    break;
                case Opcodes.Putstatic:
                    Pop(stringArg);
                    break;
                case Opcodes.Getfield:
                    Pop(1);
                    PushDescriptor(stringArg);
                    break;
                case Opcodes.Putfield:
                    Pop(stringArg);
                    Pop();
                    break;
                case Opcodes.New:
                    Push(_labels[0]);
                    break;
                case Opcodes.Newarray:
                    Pop();
                    switch (intArg)
                    {
                        case Opcodes.Boolean:
                            PushDescriptor("[Z");
                            break;
                        case Opcodes.Char:
                            PushDescriptor("[C");
                            break;
                        case Opcodes.Byte:
                            PushDescriptor("[B");
                            break;
                        case Opcodes.Short:
                            PushDescriptor("[S");
                            break;
                        case Opcodes.Int:
                            PushDescriptor("[I");
                            break;
                        case Opcodes.Float:
                            PushDescriptor("[F");
                            break;
                        case Opcodes.Double:
                            PushDescriptor("[D");
                            break;
                        case Opcodes.Long:
                            PushDescriptor("[J");
                            break;
                        default:
                            throw new ArgumentException("Invalid array type " + intArg);
                    }

                    break;
                case Opcodes.Anewarray:
                    Pop();
                    PushDescriptor("[" + JType.GetObjectType(stringArg));
                    break;
                case Opcodes.Checkcast:
                    Pop();
                    PushDescriptor(JType.GetObjectType(stringArg).Descriptor);
                    break;
                case Opcodes.Multianewarray:
                    Pop(intArg);
                    PushDescriptor(stringArg);
                    break;
                default:
                    throw new ArgumentException("Invalid opcode " + opcode);
            }

            _labels = null;
        }
    }
}