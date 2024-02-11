using System;
using ObjectWeb.Asm.Util;

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
    /// A <seealso cref="MethodVisitor"/> providing a more detailed API to generate and transform instructions.
    /// 
    /// @author Eric Bruneton
    /// </summary>
    public class InstructionAdapter : MethodVisitor
    {
        /// <summary>
        /// The type of the java.lang.Object class. </summary>
        public static readonly JType ObjectType = JType.GetType("Ljava/lang/Object;");

        /// <summary>
        /// Constructs a new <seealso cref="InstructionAdapter"/>. <i>Subclasses must not use this constructor</i>.
        /// Instead, they must use the <seealso cref="InstructionAdapter(int, MethodVisitor)"/> version.
        /// </summary>
        /// <param name="methodVisitor"> the method visitor to which this adapter delegates calls. </param>
        /// <exception cref="IllegalStateException"> If a subclass calls this constructor. </exception>
        public InstructionAdapter(MethodVisitor methodVisitor) : this(Opcodes.Asm9, methodVisitor)
        {
            if (this.GetType() != typeof(InstructionAdapter))
            {
                throw new System.InvalidOperationException();
            }
        }

        /// <summary>
        /// Constructs a new <seealso cref="InstructionAdapter"/>.
        /// </summary>
        /// <param name="api"> the ASM API version implemented by this visitor. Must be one of the {@code
        ///     ASM}<i>x</i> Values in <seealso cref="Opcodes"/>. </param>
        /// <param name="methodVisitor"> the method visitor to which this adapter delegates calls. </param>
        public InstructionAdapter(int api, MethodVisitor methodVisitor) : base(api, methodVisitor)
        {
        }

        public override void VisitInsn(int opcode)
        {
            switch (opcode)
            {
                case Opcodes.Nop:
                    Nop();
                    break;
                case Opcodes.Aconst_Null:
                    Aconst(null);
                    break;
                case Opcodes.Iconst_M1:
                case Opcodes.Iconst_0:
                case Opcodes.Iconst_1:
                case Opcodes.Iconst_2:
                case Opcodes.Iconst_3:
                case Opcodes.Iconst_4:
                case Opcodes.Iconst_5:
                    Iconst(opcode - Opcodes.Iconst_0);
                    break;
                case Opcodes.Lconst_0:
                case Opcodes.Lconst_1:
                    Lconst((long)(opcode - Opcodes.Lconst_0));
                    break;
                case Opcodes.Fconst_0:
                case Opcodes.Fconst_1:
                case Opcodes.Fconst_2:
                    Fconst((float)(opcode - Opcodes.Fconst_0));
                    break;
                case Opcodes.Dconst_0:
                case Opcodes.Dconst_1:
                    Dconst((double)(opcode - Opcodes.Dconst_0));
                    break;
                case Opcodes.Iaload:
                    Aload(JType.IntType);
                    break;
                case Opcodes.Laload:
                    Aload(JType.LongType);
                    break;
                case Opcodes.Faload:
                    Aload(JType.FloatType);
                    break;
                case Opcodes.Daload:
                    Aload(JType.DoubleType);
                    break;
                case Opcodes.Aaload:
                    Aload(ObjectType);
                    break;
                case Opcodes.Baload:
                    Aload(JType.ByteType);
                    break;
                case Opcodes.Caload:
                    Aload(JType.CharType);
                    break;
                case Opcodes.Saload:
                    Aload(JType.ShortType);
                    break;
                case Opcodes.Iastore:
                    Astore(JType.IntType);
                    break;
                case Opcodes.Lastore:
                    Astore(JType.LongType);
                    break;
                case Opcodes.Fastore:
                    Astore(JType.FloatType);
                    break;
                case Opcodes.Dastore:
                    Astore(JType.DoubleType);
                    break;
                case Opcodes.Aastore:
                    Astore(ObjectType);
                    break;
                case Opcodes.Bastore:
                    Astore(JType.ByteType);
                    break;
                case Opcodes.Castore:
                    Astore(JType.CharType);
                    break;
                case Opcodes.Sastore:
                    Astore(JType.ShortType);
                    break;
                case Opcodes.Pop:
                    Pop();
                    break;
                case Opcodes.Pop2:
                    Pop2();
                    break;
                case Opcodes.Dup:
                    Dup();
                    break;
                case Opcodes.Dup_X1:
                    DupX1();
                    break;
                case Opcodes.Dup_X2:
                    DupX2();
                    break;
                case Opcodes.Dup2:
                    Dup2();
                    break;
                case Opcodes.Dup2_X1:
                    Dup2X1();
                    break;
                case Opcodes.Dup2_X2:
                    Dup2X2();
                    break;
                case Opcodes.Swap:
                    Swap();
                    break;
                case Opcodes.Iadd:
                    Add(JType.IntType);
                    break;
                case Opcodes.Ladd:
                    Add(JType.LongType);
                    break;
                case Opcodes.Fadd:
                    Add(JType.FloatType);
                    break;
                case Opcodes.Dadd:
                    Add(JType.DoubleType);
                    break;
                case Opcodes.Isub:
                    Sub(JType.IntType);
                    break;
                case Opcodes.Lsub:
                    Sub(JType.LongType);
                    break;
                case Opcodes.Fsub:
                    Sub(JType.FloatType);
                    break;
                case Opcodes.Dsub:
                    Sub(JType.DoubleType);
                    break;
                case Opcodes.Imul:
                    Mul(JType.IntType);
                    break;
                case Opcodes.Lmul:
                    Mul(JType.LongType);
                    break;
                case Opcodes.Fmul:
                    Mul(JType.FloatType);
                    break;
                case Opcodes.Dmul:
                    Mul(JType.DoubleType);
                    break;
                case Opcodes.Idiv:
                    Div(JType.IntType);
                    break;
                case Opcodes.Ldiv:
                    Div(JType.LongType);
                    break;
                case Opcodes.Fdiv:
                    Div(JType.FloatType);
                    break;
                case Opcodes.Ddiv:
                    Div(JType.DoubleType);
                    break;
                case Opcodes.Irem:
                    Rem(JType.IntType);
                    break;
                case Opcodes.Lrem:
                    Rem(JType.LongType);
                    break;
                case Opcodes.Frem:
                    Rem(JType.FloatType);
                    break;
                case Opcodes.Drem:
                    Rem(JType.DoubleType);
                    break;
                case Opcodes.Ineg:
                    Neg(JType.IntType);
                    break;
                case Opcodes.Lneg:
                    Neg(JType.LongType);
                    break;
                case Opcodes.Fneg:
                    Neg(JType.FloatType);
                    break;
                case Opcodes.Dneg:
                    Neg(JType.DoubleType);
                    break;
                case Opcodes.Ishl:
                    Shl(JType.IntType);
                    break;
                case Opcodes.Lshl:
                    Shl(JType.LongType);
                    break;
                case Opcodes.Ishr:
                    Shr(JType.IntType);
                    break;
                case Opcodes.Lshr:
                    Shr(JType.LongType);
                    break;
                case Opcodes.Iushr:
                    Ushr(JType.IntType);
                    break;
                case Opcodes.Lushr:
                    Ushr(JType.LongType);
                    break;
                case Opcodes.Iand:
                    And(JType.IntType);
                    break;
                case Opcodes.Land:
                    And(JType.LongType);
                    break;
                case Opcodes.Ior:
                    Or(JType.IntType);
                    break;
                case Opcodes.Lor:
                    Or(JType.LongType);
                    break;
                case Opcodes.Ixor:
                    Xor(JType.IntType);
                    break;
                case Opcodes.Lxor:
                    Xor(JType.LongType);
                    break;
                case Opcodes.I2L:
                    Cast(JType.IntType, JType.LongType);
                    break;
                case Opcodes.I2F:
                    Cast(JType.IntType, JType.FloatType);
                    break;
                case Opcodes.I2D:
                    Cast(JType.IntType, JType.DoubleType);
                    break;
                case Opcodes.L2I:
                    Cast(JType.LongType, JType.IntType);
                    break;
                case Opcodes.L2F:
                    Cast(JType.LongType, JType.FloatType);
                    break;
                case Opcodes.L2D:
                    Cast(JType.LongType, JType.DoubleType);
                    break;
                case Opcodes.F2I:
                    Cast(JType.FloatType, JType.IntType);
                    break;
                case Opcodes.F2L:
                    Cast(JType.FloatType, JType.LongType);
                    break;
                case Opcodes.F2D:
                    Cast(JType.FloatType, JType.DoubleType);
                    break;
                case Opcodes.D2I:
                    Cast(JType.DoubleType, JType.IntType);
                    break;
                case Opcodes.D2L:
                    Cast(JType.DoubleType, JType.LongType);
                    break;
                case Opcodes.D2F:
                    Cast(JType.DoubleType, JType.FloatType);
                    break;
                case Opcodes.I2B:
                    Cast(JType.IntType, JType.ByteType);
                    break;
                case Opcodes.I2C:
                    Cast(JType.IntType, JType.CharType);
                    break;
                case Opcodes.I2S:
                    Cast(JType.IntType, JType.ShortType);
                    break;
                case Opcodes.Lcmp:
                    Lcmp();
                    break;
                case Opcodes.Fcmpl:
                    Cmpl(JType.FloatType);
                    break;
                case Opcodes.Fcmpg:
                    Cmpg(JType.FloatType);
                    break;
                case Opcodes.Dcmpl:
                    Cmpl(JType.DoubleType);
                    break;
                case Opcodes.Dcmpg:
                    Cmpg(JType.DoubleType);
                    break;
                case Opcodes.Ireturn:
                    Areturn(JType.IntType);
                    break;
                case Opcodes.Lreturn:
                    Areturn(JType.LongType);
                    break;
                case Opcodes.Freturn:
                    Areturn(JType.FloatType);
                    break;
                case Opcodes.Dreturn:
                    Areturn(JType.DoubleType);
                    break;
                case Opcodes.Areturn:
                    Areturn(ObjectType);
                    break;
                case Opcodes.Return:
                    Areturn(JType.VoidType);
                    break;
                case Opcodes.Arraylength:
                    Arraylength();
                    break;
                case Opcodes.Athrow:
                    Athrow();
                    break;
                case Opcodes.Monitorenter:
                    Monitorenter();
                    break;
                case Opcodes.Monitorexit:
                    Monitorexit();
                    break;
                default:
                    throw new System.ArgumentException();
            }
        }

        public override void VisitIntInsn(int opcode, int operand)
        {
            switch (opcode)
            {
                case Opcodes.Bipush:
                    Iconst(operand);
                    break;
                case Opcodes.Sipush:
                    Iconst(operand);
                    break;
                case Opcodes.Newarray:
                    switch (operand)
                    {
                        case Opcodes.Boolean:
                            Newarray(JType.BooleanType);
                            break;
                        case Opcodes.Char:
                            Newarray(JType.CharType);
                            break;
                        case Opcodes.Byte:
                            Newarray(JType.ByteType);
                            break;
                        case Opcodes.Short:
                            Newarray(JType.ShortType);
                            break;
                        case Opcodes.Int:
                            Newarray(JType.IntType);
                            break;
                        case Opcodes.Float:
                            Newarray(JType.FloatType);
                            break;
                        case Opcodes.Long:
                            Newarray(JType.LongType);
                            break;
                        case Opcodes.Double:
                            Newarray(JType.DoubleType);
                            break;
                        default:
                            throw new System.ArgumentException();
                    }

                    break;
                default:
                    throw new System.ArgumentException();
            }
        }

        public override void VisitVarInsn(int opcode, int varIndex)
        {
            switch (opcode)
            {
                case Opcodes.Iload:
                    Load(varIndex, JType.IntType);
                    break;
                case Opcodes.Lload:
                    Load(varIndex, JType.LongType);
                    break;
                case Opcodes.Fload:
                    Load(varIndex, JType.FloatType);
                    break;
                case Opcodes.Dload:
                    Load(varIndex, JType.DoubleType);
                    break;
                case Opcodes.Aload:
                    Load(varIndex, ObjectType);
                    break;
                case Opcodes.Istore:
                    Store(varIndex, JType.IntType);
                    break;
                case Opcodes.Lstore:
                    Store(varIndex, JType.LongType);
                    break;
                case Opcodes.Fstore:
                    Store(varIndex, JType.FloatType);
                    break;
                case Opcodes.Dstore:
                    Store(varIndex, JType.DoubleType);
                    break;
                case Opcodes.Astore:
                    Store(varIndex, ObjectType);
                    break;
                case Opcodes.Ret:
                    Ret(varIndex);
                    break;
                default:
                    throw new System.ArgumentException();
            }
        }

        public override void VisitTypeInsn(int opcode, string type)
        {
            var objectType = JType.GetObjectType(type);
            switch (opcode)
            {
                case Opcodes.New:
                    Anew(objectType);
                    break;
                case Opcodes.Anewarray:
                    Newarray(objectType);
                    break;
                case Opcodes.Checkcast:
                    Checkcast(objectType);
                    break;
                case Opcodes.Instanceof:
                    InstanceOf(objectType);
                    break;
                default:
                    throw new System.ArgumentException();
            }
        }

        public override void VisitFieldInsn(int opcode, string owner, string name, string descriptor)
        {
            switch (opcode)
            {
                case Opcodes.Getstatic:
                    Getstatic(owner, name, descriptor);
                    break;
                case Opcodes.Putstatic:
                    Putstatic(owner, name, descriptor);
                    break;
                case Opcodes.Getfield:
                    Getfield(owner, name, descriptor);
                    break;
                case Opcodes.Putfield:
                    Putfield(owner, name, descriptor);
                    break;
                default:
                    throw new System.ArgumentException();
            }
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

            var opcode = opcodeAndSource & ~Opcodes.Source_Mask;

            switch (opcode)
            {
                case Opcodes.Invokespecial:
                    Invokespecial(owner, name, descriptor, isInterface);
                    break;
                case Opcodes.Invokevirtual:
                    Invokevirtual(owner, name, descriptor, isInterface);
                    break;
                case Opcodes.Invokestatic:
                    Invokestatic(owner, name, descriptor, isInterface);
                    break;
                case Opcodes.Invokeinterface:
                    Invokeinterface(owner, name, descriptor);
                    break;
                default:
                    throw new System.ArgumentException();
            }
        }

        public override void VisitInvokeDynamicInsn(string name, string descriptor, Handle bootstrapMethodHandle,
            params object[] bootstrapMethodArguments)
        {
            Invokedynamic(name, descriptor, bootstrapMethodHandle, bootstrapMethodArguments);
        }

        public override void VisitJumpInsn(int opcode, Label label)
        {
            switch (opcode)
            {
                case Opcodes.Ifeq:
                    Ifeq(label);
                    break;
                case Opcodes.Ifne:
                    Ifne(label);
                    break;
                case Opcodes.Iflt:
                    Iflt(label);
                    break;
                case Opcodes.Ifge:
                    Ifge(label);
                    break;
                case Opcodes.Ifgt:
                    Ifgt(label);
                    break;
                case Opcodes.Ifle:
                    Ifle(label);
                    break;
                case Opcodes.If_Icmpeq:
                    Ificmpeq(label);
                    break;
                case Opcodes.If_Icmpne:
                    Ificmpne(label);
                    break;
                case Opcodes.If_Icmplt:
                    Ificmplt(label);
                    break;
                case Opcodes.If_Icmpge:
                    Ificmpge(label);
                    break;
                case Opcodes.If_Icmpgt:
                    Ificmpgt(label);
                    break;
                case Opcodes.If_Icmple:
                    Ificmple(label);
                    break;
                case Opcodes.If_Acmpeq:
                    Ifacmpeq(label);
                    break;
                case Opcodes.If_Acmpne:
                    Ifacmpne(label);
                    break;
                case Opcodes.Goto:
                    GoTo(label);
                    break;
                case Opcodes.Jsr:
                    Jsr(label);
                    break;
                case Opcodes.Ifnull:
                    Ifnull(label);
                    break;
                case Opcodes.Ifnonnull:
                    Ifnonnull(label);
                    break;
                default:
                    throw new System.ArgumentException();
            }
        }

        public override void VisitLabel(Label label)
        {
            Mark(label);
        }

        public override void VisitLdcInsn(object value)
        {
            if (api < Opcodes.Asm5 && (value is Handle || (value is JType && ((JType)value).Sort == JType.Method)))
            {
                throw new System.NotSupportedException("This feature requires ASM5");
            }

            if (api < Opcodes.Asm7 && value is ConstantDynamic)
            {
                throw new System.NotSupportedException("This feature requires ASM7");
            }

            if (value is int)
            {
                Iconst(((int?)value).Value);
            }
            else if (value is Byte)
            {
                Iconst(((sbyte?)value).Value);
            }
            else if (value is char)
            {
                Iconst(((char?)value).Value);
            }
            else if (value is short)
            {
                Iconst(((short?)value).Value);
            }
            else if (value is Boolean)
            {
                Iconst(((bool?)value).Value ? 1 : 0);
            }
            else if (value is float)
            {
                Fconst(((float?)value).Value);
            }
            else if (value is long)
            {
                Lconst(((long?)value).Value);
            }
            else if (value is Double)
            {
                Dconst(((double?)value).Value);
            }
            else if (value is string)
            {
                Aconst(value);
            }
            else if (value is Type)
            {
                Tconst((JType)value);
            }
            else if (value is Handle)
            {
                Hconst((Handle)value);
            }
            else if (value is ConstantDynamic)
            {
                Cconst((ConstantDynamic)value);
            }
            else
            {
                throw new System.ArgumentException();
            }
        }

        public override void VisitIincInsn(int varIndex, int increment)
        {
            Iinc(varIndex, increment);
        }

        public override void VisitTableSwitchInsn(int min, int max, Label dflt, params Label[] labels)
        {
            Tableswitch(min, max, dflt, labels);
        }

        public override void VisitLookupSwitchInsn(Label dflt, int[] keys, Label[] labels)
        {
            Lookupswitch(dflt, keys, labels);
        }

        public override void VisitMultiANewArrayInsn(string descriptor, int numDimensions)
        {
            Multianewarray(descriptor, numDimensions);
        }

        // -----------------------------------------------------------------------------------------------

        /// <summary>
        /// Generates a nop instruction. </summary>
        public virtual void Nop()
        {
            mv.VisitInsn(Opcodes.Nop);
        }

        /// <summary>
        /// Generates the instruction to push the given value on the stack.
        /// </summary>
        /// <param name="value"> the constant to be pushed on the stack. This parameter must be an <seealso cref="Integer"/>,
        ///     a <seealso cref="Float"/>, a <seealso cref="Long"/>, a <seealso cref="Double"/>, a <seealso cref="string"/>, a <seealso cref="Type"/> of
        ///     OBJECT or ARRAY sort for {@code .class} constants, for classes whose version is 49, a
        ///     <seealso cref="Type"/> of METHOD sort for MethodType, a <seealso cref="Handle"/> for MethodHandle constants,
        ///     for classes whose version is 51 or a <seealso cref="ConstantDynamic"/> for a constant dynamic for
        ///     classes whose version is 55. </param>
        public virtual void Aconst(object value)
        {
            if (value == null)
            {
                mv.VisitInsn(Opcodes.Aconst_Null);
            }
            else
            {
                mv.VisitLdcInsn(value);
            }
        }

        /// <summary>
        /// Generates the instruction to push the given value on the stack.
        /// </summary>
        /// <param name="intValue"> the constant to be pushed on the stack. </param>
        public virtual void Iconst(int intValue)
        {
            if (intValue >= -1 && intValue <= 5)
            {
                mv.VisitInsn(Opcodes.Iconst_0 + intValue);
            }
            else if (intValue >= sbyte.MinValue && intValue <= sbyte.MaxValue)
            {
                mv.VisitIntInsn(Opcodes.Bipush, intValue);
            }
            else if (intValue >= short.MinValue && intValue <= short.MaxValue)
            {
                mv.VisitIntInsn(Opcodes.Sipush, intValue);
            }
            else
            {
                mv.VisitLdcInsn(intValue);
            }
        }

        /// <summary>
        /// Generates the instruction to push the given value on the stack.
        /// </summary>
        /// <param name="longValue"> the constant to be pushed on the stack. </param>
        public virtual void Lconst(long longValue)
        {
            if (longValue == 0L || longValue == 1L)
            {
                mv.VisitInsn(Opcodes.Lconst_0 + (int)longValue);
            }
            else
            {
                mv.VisitLdcInsn(longValue);
            }
        }

        /// <summary>
        /// Generates the instruction to push the given value on the stack.
        /// </summary>
        /// <param name="floatValue"> the constant to be pushed on the stack. </param>
        public virtual void Fconst(float floatValue)
        {
            var bits = Int32AndSingleConverter.Convert(floatValue);
            if (bits == 0L || bits == 0x3F800000 || bits == 0x40000000)
            {
                // 0..2
                mv.VisitInsn(Opcodes.Fconst_0 + (int)floatValue);
            }
            else
            {
                mv.VisitLdcInsn(floatValue);
            }
        }

        /// <summary>
        /// Generates the instruction to push the given value on the stack.
        /// </summary>
        /// <param name="doubleValue"> the constant to be pushed on the stack. </param>
        public virtual void Dconst(double doubleValue)
        {
            var bits = System.BitConverter.DoubleToInt64Bits(doubleValue);
            if (bits == 0L || bits == 0x3FF0000000000000L)
            {
                // +0.0d and 1.0d
                mv.VisitInsn(Opcodes.Dconst_0 + (int)doubleValue);
            }
            else
            {
                mv.VisitLdcInsn(doubleValue);
            }
        }

        /// <summary>
        /// Generates the instruction to push the given type on the stack.
        /// </summary>
        /// <param name="type"> the type to be pushed on the stack. </param>
        public virtual void Tconst(JType type)
        {
            mv.VisitLdcInsn(type);
        }

        /// <summary>
        /// Generates the instruction to push the given handle on the stack.
        /// </summary>
        /// <param name="handle"> the handle to be pushed on the stack. </param>
        public virtual void Hconst(Handle handle)
        {
            mv.VisitLdcInsn(handle);
        }

        /// <summary>
        /// Generates the instruction to push the given constant dynamic on the stack.
        /// </summary>
        /// <param name="constantDynamic"> the constant dynamic to be pushed on the stack. </param>
        public virtual void Cconst(ConstantDynamic constantDynamic)
        {
            mv.VisitLdcInsn(constantDynamic);
        }

        public virtual void Load(int var, JType type)
        {
            mv.VisitVarInsn(type.GetOpcode(Opcodes.Iload), var);
        }

        public virtual void Aload(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Iaload));
        }

        public virtual void Store(int var, JType type)
        {
            mv.VisitVarInsn(type.GetOpcode(Opcodes.Istore), var);
        }

        public virtual void Astore(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Iastore));
        }

        public virtual void Pop()
        {
            mv.VisitInsn(Opcodes.Pop);
        }

        public virtual void Pop2()
        {
            mv.VisitInsn(Opcodes.Pop2);
        }

        public virtual void Dup()
        {
            mv.VisitInsn(Opcodes.Dup);
        }

        public virtual void Dup2()
        {
            mv.VisitInsn(Opcodes.Dup2);
        }

        public virtual void DupX1()
        {
            mv.VisitInsn(Opcodes.Dup_X1);
        }

        public virtual void DupX2()
        {
            mv.VisitInsn(Opcodes.Dup_X2);
        }

        public virtual void Dup2X1()
        {
            mv.VisitInsn(Opcodes.Dup2_X1);
        }

        public virtual void Dup2X2()
        {
            mv.VisitInsn(Opcodes.Dup2_X2);
        }

        public virtual void Swap()
        {
            mv.VisitInsn(Opcodes.Swap);
        }

        public virtual void Add(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Iadd));
        }

        public virtual void Sub(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Isub));
        }

        public virtual void Mul(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Imul));
        }

        public virtual void Div(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Idiv));
        }

        public virtual void Rem(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Irem));
        }

        public virtual void Neg(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Ineg));
        }

        public virtual void Shl(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Ishl));
        }

        public virtual void Shr(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Ishr));
        }

        public virtual void Ushr(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Iushr));
        }

        public virtual void And(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Iand));
        }

        public virtual void Or(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Ior));
        }

        public virtual void Xor(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Ixor));
        }

        public virtual void Iinc(int var, int increment)
        {
            mv.VisitIincInsn(var, increment);
        }

        /// <summary>
        /// Generates the instruction to cast from the first given type to the other.
        /// </summary>
        /// <param name="from"> a Type. </param>
        /// <param name="to"> a Type. </param>
        public virtual void Cast(JType from, JType to)
        {
            Cast(mv, from, to);
        }

        /// <summary>
        /// Generates the instruction to cast from the first given type to the other.
        /// </summary>
        /// <param name="methodVisitor"> the method visitor to use to generate the instruction. </param>
        /// <param name="from"> a Type. </param>
        /// <param name="to"> a Type. </param>
        internal static void Cast(MethodVisitor methodVisitor, JType from, JType to)
        {
            if (from != to)
            {
                if (from == JType.DoubleType)
                {
                    if (to == JType.FloatType)
                    {
                        methodVisitor.VisitInsn(Opcodes.D2F);
                    }
                    else if (to == JType.LongType)
                    {
                        methodVisitor.VisitInsn(Opcodes.D2L);
                    }
                    else
                    {
                        methodVisitor.VisitInsn(Opcodes.D2I);
                        Cast(methodVisitor, JType.IntType, to);
                    }
                }
                else if (from == JType.FloatType)
                {
                    if (to == JType.DoubleType)
                    {
                        methodVisitor.VisitInsn(Opcodes.F2D);
                    }
                    else if (to == JType.LongType)
                    {
                        methodVisitor.VisitInsn(Opcodes.F2L);
                    }
                    else
                    {
                        methodVisitor.VisitInsn(Opcodes.F2I);
                        Cast(methodVisitor, JType.IntType, to);
                    }
                }
                else if (from == JType.LongType)
                {
                    if (to == JType.DoubleType)
                    {
                        methodVisitor.VisitInsn(Opcodes.L2D);
                    }
                    else if (to == JType.FloatType)
                    {
                        methodVisitor.VisitInsn(Opcodes.L2F);
                    }
                    else
                    {
                        methodVisitor.VisitInsn(Opcodes.L2I);
                        Cast(methodVisitor, JType.IntType, to);
                    }
                }
                else
                {
                    if (to == JType.ByteType)
                    {
                        methodVisitor.VisitInsn(Opcodes.I2B);
                    }
                    else if (to == JType.CharType)
                    {
                        methodVisitor.VisitInsn(Opcodes.I2C);
                    }
                    else if (to == JType.DoubleType)
                    {
                        methodVisitor.VisitInsn(Opcodes.I2D);
                    }
                    else if (to == JType.FloatType)
                    {
                        methodVisitor.VisitInsn(Opcodes.I2F);
                    }
                    else if (to == JType.LongType)
                    {
                        methodVisitor.VisitInsn(Opcodes.I2L);
                    }
                    else if (to == JType.ShortType)
                    {
                        methodVisitor.VisitInsn(Opcodes.I2S);
                    }
                }
            }
        }

        public virtual void Lcmp()
        {
            mv.VisitInsn(Opcodes.Lcmp);
        }

        public virtual void Cmpl(JType type)
        {
            mv.VisitInsn(type == JType.FloatType ? Opcodes.Fcmpl : Opcodes.Dcmpl);
        }

        public virtual void Cmpg(JType type)
        {
            mv.VisitInsn(type == JType.FloatType ? Opcodes.Fcmpg : Opcodes.Dcmpg);
        }

        public virtual void Ifeq(Label label)
        {
            mv.VisitJumpInsn(Opcodes.Ifeq, label);
        }

        public virtual void Ifne(Label label)
        {
            mv.VisitJumpInsn(Opcodes.Ifne, label);
        }

        public virtual void Iflt(Label label)
        {
            mv.VisitJumpInsn(Opcodes.Iflt, label);
        }

        public virtual void Ifge(Label label)
        {
            mv.VisitJumpInsn(Opcodes.Ifge, label);
        }

        public virtual void Ifgt(Label label)
        {
            mv.VisitJumpInsn(Opcodes.Ifgt, label);
        }

        public virtual void Ifle(Label label)
        {
            mv.VisitJumpInsn(Opcodes.Ifle, label);
        }

        public virtual void Ificmpeq(Label label)
        {
            mv.VisitJumpInsn(Opcodes.If_Icmpeq, label);
        }

        public virtual void Ificmpne(Label label)
        {
            mv.VisitJumpInsn(Opcodes.If_Icmpne, label);
        }

        public virtual void Ificmplt(Label label)
        {
            mv.VisitJumpInsn(Opcodes.If_Icmplt, label);
        }

        public virtual void Ificmpge(Label label)
        {
            mv.VisitJumpInsn(Opcodes.If_Icmpge, label);
        }

        public virtual void Ificmpgt(Label label)
        {
            mv.VisitJumpInsn(Opcodes.If_Icmpgt, label);
        }

        public virtual void Ificmple(Label label)
        {
            mv.VisitJumpInsn(Opcodes.If_Icmple, label);
        }

        public virtual void Ifacmpeq(Label label)
        {
            mv.VisitJumpInsn(Opcodes.If_Acmpeq, label);
        }

        public virtual void Ifacmpne(Label label)
        {
            mv.VisitJumpInsn(Opcodes.If_Acmpne, label);
        }

        public virtual void GoTo(Label label)
        {
            mv.VisitJumpInsn(Opcodes.Goto, label);
        }

        public virtual void Jsr(Label label)
        {
            mv.VisitJumpInsn(Opcodes.Jsr, label);
        }

        public virtual void Ret(int var)
        {
            mv.VisitVarInsn(Opcodes.Ret, var);
        }

        public virtual void Tableswitch(int min, int max, Label dflt, params Label[] labels)
        {
            mv.VisitTableSwitchInsn(min, max, dflt, labels);
        }

        public virtual void Lookupswitch(Label dflt, int[] keys, Label[] labels)
        {
            mv.VisitLookupSwitchInsn(dflt, keys, labels);
        }

        public virtual void Areturn(JType type)
        {
            mv.VisitInsn(type.GetOpcode(Opcodes.Ireturn));
        }

        public virtual void Getstatic(string owner, string name, string descriptor)
        {
            mv.VisitFieldInsn(Opcodes.Getstatic, owner, name, descriptor);
        }

        public virtual void Putstatic(string owner, string name, string descriptor)
        {
            mv.VisitFieldInsn(Opcodes.Putstatic, owner, name, descriptor);
        }

        public virtual void Getfield(string owner, string name, string descriptor)
        {
            mv.VisitFieldInsn(Opcodes.Getfield, owner, name, descriptor);
        }

        public virtual void Putfield(string owner, string name, string descriptor)
        {
            mv.VisitFieldInsn(Opcodes.Putfield, owner, name, descriptor);
        }

        /// <summary>
        /// Deprecated.
        /// </summary>
        /// <param name="owner"> the internal name of the method's owner class. </param>
        /// <param name="name"> the method's name. </param>
        /// <param name="descriptor"> the method's descriptor (see <seealso cref="Type"/>). </param>
        /// @deprecated use <seealso cref="Invokevirtual(string,string,string,bool)"/> instead. 
        [Obsolete("use <seealso cref=\"invokevirtual(String, String, String, bool)\"/> instead.")]
        public virtual void Invokevirtual(string owner, string name, string descriptor)
        {
            if (api >= Opcodes.Asm5)
            {
                Invokevirtual(owner, name, descriptor, false);
                return;
            }

            mv.VisitMethodInsn(Opcodes.Invokevirtual, owner, name, descriptor);
        }

        /// <summary>
        /// Generates the instruction to call the given virtual method.
        /// </summary>
        /// <param name="owner"> the internal name of the method's owner class (see {@link
        ///     Type#getInternalName()}). </param>
        /// <param name="name"> the method's name. </param>
        /// <param name="descriptor"> the method's descriptor (see <seealso cref="Type"/>). </param>
        /// <param name="isInterface"> if the method's owner class is an interface. </param>
        public virtual void Invokevirtual(string owner, string name, string descriptor, bool isInterface)
        {
            if (api < Opcodes.Asm5)
            {
                if (isInterface)
                {
                    throw new System.NotSupportedException("INVOKEVIRTUAL on interfaces require ASM 5");
                }

                Invokevirtual(owner, name, descriptor);
                return;
            }

            mv.VisitMethodInsn(Opcodes.Invokevirtual, owner, name, descriptor, isInterface);
        }

        /// <summary>
        /// Deprecated.
        /// </summary>
        /// <param name="owner"> the internal name of the method's owner class. </param>
        /// <param name="name"> the method's name. </param>
        /// <param name="descriptor"> the method's descriptor (see <seealso cref="Type"/>). </param>
        /// @deprecated use <seealso cref="Invokespecial(string,string,string,bool)"/> instead. 
        [Obsolete("use <seealso cref=\"invokespecial(String, String, String, bool)\"/> instead.")]
        public virtual void Invokespecial(string owner, string name, string descriptor)
        {
            if (api >= Opcodes.Asm5)
            {
                Invokespecial(owner, name, descriptor, false);
                return;
            }

            mv.VisitMethodInsn(Opcodes.Invokespecial, owner, name, descriptor, false);
        }

        /// <summary>
        /// Generates the instruction to call the given special method.
        /// </summary>
        /// <param name="owner"> the internal name of the method's owner class (see {@link
        ///     Type#getInternalName()}). </param>
        /// <param name="name"> the method's name. </param>
        /// <param name="descriptor"> the method's descriptor (see <seealso cref="Type"/>). </param>
        /// <param name="isInterface"> if the method's owner class is an interface. </param>
        public virtual void Invokespecial(string owner, string name, string descriptor, bool isInterface)
        {
            if (api < Opcodes.Asm5)
            {
                if (isInterface)
                {
                    throw new System.NotSupportedException("INVOKESPECIAL on interfaces require ASM 5");
                }

                Invokespecial(owner, name, descriptor);
                return;
            }

            mv.VisitMethodInsn(Opcodes.Invokespecial, owner, name, descriptor, isInterface);
        }

        /// <summary>
        /// Deprecated.
        /// </summary>
        /// <param name="owner"> the internal name of the method's owner class. </param>
        /// <param name="name"> the method's name. </param>
        /// <param name="descriptor"> the method's descriptor (see <seealso cref="Type"/>). </param>
        /// @deprecated use <seealso cref="Invokestatic(string,string,string,bool)"/> instead. 
        [Obsolete("use <seealso cref=\"invokestatic(String, String, String, bool)\"/> instead.")]
        public virtual void Invokestatic(string owner, string name, string descriptor)
        {
            if (api >= Opcodes.Asm5)
            {
                Invokestatic(owner, name, descriptor, false);
                return;
            }

            mv.VisitMethodInsn(Opcodes.Invokestatic, owner, name, descriptor, false);
        }

        /// <summary>
        /// Generates the instruction to call the given static method.
        /// </summary>
        /// <param name="owner"> the internal name of the method's owner class (see {@link
        ///     Type#getInternalName()}). </param>
        /// <param name="name"> the method's name. </param>
        /// <param name="descriptor"> the method's descriptor (see <seealso cref="Type"/>). </param>
        /// <param name="isInterface"> if the method's owner class is an interface. </param>
        public virtual void Invokestatic(string owner, string name, string descriptor, bool isInterface)
        {
            if (api < Opcodes.Asm5)
            {
                if (isInterface)
                {
                    throw new System.NotSupportedException("INVOKESTATIC on interfaces require ASM 5");
                }

                Invokestatic(owner, name, descriptor);
                return;
            }

            mv.VisitMethodInsn(Opcodes.Invokestatic, owner, name, descriptor, isInterface);
        }

        /// <summary>
        /// Generates the instruction to call the given interface method.
        /// </summary>
        /// <param name="owner"> the internal name of the method's owner class (see {@link
        ///     Type#getInternalName()}). </param>
        /// <param name="name"> the method's name. </param>
        /// <param name="descriptor"> the method's descriptor (see <seealso cref="Type"/>). </param>
        public virtual void Invokeinterface(string owner, string name, string descriptor)
        {
            mv.VisitMethodInsn(Opcodes.Invokeinterface, owner, name, descriptor, true);
        }

        /// <summary>
        /// Generates the instruction to call the given dynamic method.
        /// </summary>
        /// <param name="name"> the method's name. </param>
        /// <param name="descriptor"> the method's descriptor (see <seealso cref="Type"/>). </param>
        /// <param name="bootstrapMethodHandle"> the bootstrap method. </param>
        /// <param name="bootstrapMethodArguments"> the bootstrap method constant arguments. Each argument must be
        ///     an <seealso cref="Integer"/>, <seealso cref="Float"/>, <seealso cref="Long"/>, <seealso cref="Double"/>, <seealso cref="string"/>, {@link
        ///     Type}, <seealso cref="Handle"/> or <seealso cref="ConstantDynamic"/> value. This method is allowed to modify
        ///     the content of the array so a caller should expect that this array may change. </param>
        public virtual void Invokedynamic(string name, string descriptor, Handle bootstrapMethodHandle,
            object[] bootstrapMethodArguments)
        {
            mv.VisitInvokeDynamicInsn(name, descriptor, bootstrapMethodHandle, bootstrapMethodArguments);
        }

        public virtual void Anew(JType type)
        {
            mv.VisitTypeInsn(Opcodes.New, type.InternalName);
        }

        /// <summary>
        /// Generates the instruction to create and push on the stack an array of the given type.
        /// </summary>
        /// <param name="type"> an array Type. </param>
        public virtual void Newarray(JType type)
        {
            Newarray(mv, type);
        }

        /// <summary>
        /// Generates the instruction to create and push on the stack an array of the given type.
        /// </summary>
        /// <param name="methodVisitor"> the method visitor to use to generate the instruction. </param>
        /// <param name="type"> an array Type. </param>
        internal static void Newarray(MethodVisitor methodVisitor, JType type)
        {
            int arrayType;
            switch (type.Sort)
            {
                case JType.Boolean:
                    arrayType = Opcodes.Boolean;
                    break;
                case JType.Char:
                    arrayType = Opcodes.Char;
                    break;
                case JType.Byte:
                    arrayType = Opcodes.Byte;
                    break;
                case JType.Short:
                    arrayType = Opcodes.Short;
                    break;
                case JType.Int:
                    arrayType = Opcodes.Int;
                    break;
                case JType.Float:
                    arrayType = Opcodes.Float;
                    break;
                case JType.Long:
                    arrayType = Opcodes.Long;
                    break;
                case JType.Double:
                    arrayType = Opcodes.Double;
                    break;
                default:
                    methodVisitor.VisitTypeInsn(Opcodes.Anewarray, type.InternalName);
                    return;
            }

            methodVisitor.VisitIntInsn(Opcodes.Newarray, arrayType);
        }

        public virtual void Arraylength()
        {
            mv.VisitInsn(Opcodes.Arraylength);
        }

        public virtual void Athrow()
        {
            mv.VisitInsn(Opcodes.Athrow);
        }

        public virtual void Checkcast(JType type)
        {
            mv.VisitTypeInsn(Opcodes.Checkcast, type.InternalName);
        }

        public virtual void InstanceOf(JType type)
        {
            mv.VisitTypeInsn(Opcodes.Instanceof, type.InternalName);
        }

        public virtual void Monitorenter()
        {
            mv.VisitInsn(Opcodes.Monitorenter);
        }

        public virtual void Monitorexit()
        {
            mv.VisitInsn(Opcodes.Monitorexit);
        }

        public virtual void Multianewarray(string descriptor, int numDimensions)
        {
            mv.VisitMultiANewArrayInsn(descriptor, numDimensions);
        }

        public virtual void Ifnull(Label label)
        {
            mv.VisitJumpInsn(Opcodes.Ifnull, label);
        }

        public virtual void Ifnonnull(Label label)
        {
            mv.VisitJumpInsn(Opcodes.Ifnonnull, label);
        }

        public virtual void Mark(Label label)
        {
            mv.VisitLabel(label);
        }
    }
}