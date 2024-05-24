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
namespace ObjectWeb.Asm.Commons;

/// <summary>
///     A <see cref="MethodVisitor" /> to insert before, after and around advices in methods and constructors.
///     For constructors, the code keeps track of the elements on the stack in order to detect when the
///     super class constructor is called (note that there can be multiple such calls in different
///     branches). <c>onMethodEnter</c> is called after each super class constructor call, because the
///     object cannot be used before it is properly initialized.
///     @author Eugene Kuleshov
///     @author Eric Bruneton
/// </summary>
public abstract class AdviceAdapter : GeneratorAdapter
{
    /// <summary>
    ///     Prefix of the error message when invalid opcodes are found.
    /// </summary>
    private const string InvalidOpcode = "Invalid opcode ";

    /// <summary>
    ///     The "uninitialized this" value.
    /// </summary>
    private static readonly object UNINITIALIZED_THIS = new object();

    /// <summary>
    ///     Any value other than "uninitialized this".
    /// </summary>
    private static readonly object OTHER = new object();

    /// <summary>
    ///     Whether the visited method is a constructor.
    /// </summary>
    private readonly bool _isConstructor;

    /// <summary>
    ///     The stack map frames corresponding to the labels of the forward jumps made *before* the super
    ///     class constructor has been called (note that the Java Virtual Machine forbids backward jumps
    ///     before the super class constructor is called). Note that by definition (cf. the 'before'), when
    ///     we reach a label from this map, <see cref="_superClassConstructorCalled" /> must be reset to false.
    ///     This field is only maintained for constructors.
    /// </summary>
    private IDictionary<Label, List<object>> _forwardJumpStackFrames;

    /// <summary>
    ///     The Values on the current execution stack frame (long and double are represented by two
    ///     elements). Each value is either <see cref="UNINITIALIZED_THIS" /> (for the uninitialized this value),
    ///     or <see cref="OTHER" /> (for any other value). This field is only maintained for constructors, in
    ///     branches where the super class constructor has not been called yet.
    /// </summary>
    private List<object> _stackFrame;

    /// <summary>
    ///     Whether the super class constructor has been called (if the visited method is a constructor),
    ///     at the current instruction. There can be multiple call sites to the super constructor (e.g. for
    ///     Java code such as <c>super(expr ? value1 : value2);</c>), in different branches. When scanning
    ///     the bytecode linearly, we can move from one branch where the super constructor has been called
    ///     to another where it has not been called yet. Therefore, this value can change from false to
    ///     true, and vice-versa.
    /// </summary>
    private bool _superClassConstructorCalled;

    /// <summary>
    ///     The access flags of the visited method.
    /// </summary>
    protected internal int methodAccess;

    /// <summary>
    ///     The descriptor of the visited method.
    /// </summary>
    protected internal string methodDesc;

    /// <summary>
    ///     Constructs a new <see cref="AdviceAdapter" />.
    /// </summary>
    /// <param name="api">
    ///     the ASM API version implemented by this visitor. Must be one of the <c>ASM</c><i>x</i> Values in <see cref="Opcodes" />.
    /// </param>
    /// <param name="methodVisitor"> the method visitor to which this adapter delegates calls. </param>
    /// <param name="access"> the method's access flags (see <see cref="Opcodes" />). </param>
    /// <param name="name"> the method's name. </param>
    /// <param name="descriptor"> the method's descriptor (see <see cref="Type" />). </param>
    public AdviceAdapter(int api, MethodVisitor methodVisitor, int access, string name, string descriptor) : base(
        api, methodVisitor, access, name, descriptor)
    {
        methodAccess = access;
        methodDesc = descriptor;
        _isConstructor = "<init>".Equals(name);
    }

    public override void VisitCode()
    {
        base.VisitCode();
        if (_isConstructor)
        {
            _stackFrame = new List<object>();
            _forwardJumpStackFrames = new Dictionary<Label, List<object>>();
        }
        else
        {
            OnMethodEnter();
        }
    }

    public override void VisitLabel(Label label)
    {
        base.VisitLabel(label);
        if (_isConstructor && _forwardJumpStackFrames != null)
        {
            if (_forwardJumpStackFrames.TryGetValue(label, out List<object> labelStackFrame))
            {
                _stackFrame = labelStackFrame;
                _superClassConstructorCalled = false;
                _forwardJumpStackFrames.Remove(label);
            }
        }
    }

    public override void VisitInsn(int opcode)
    {
        if (_isConstructor && !_superClassConstructorCalled)
        {
            int stackSize;
            switch (opcode)
            {
                case Opcodes.Ireturn:
                case Opcodes.Freturn:
                case Opcodes.Areturn:
                case Opcodes.Lreturn:
                case Opcodes.Dreturn:
                    throw new ArgumentException("Invalid return in constructor");
                case Opcodes.Return: // empty stack
                    OnMethodExit(opcode);
                    EndConstructorBasicBlockWithoutSuccessor();
                    break;
                case Opcodes.Athrow: // 1 before n/a after
                    PopValue();
                    OnMethodExit(opcode);
                    EndConstructorBasicBlockWithoutSuccessor();
                    break;
                case Opcodes.Nop:
                case Opcodes.Laload: // remove 2 add 2
                case Opcodes.Daload: // remove 2 add 2
                case Opcodes.Lneg:
                case Opcodes.Dneg:
                case Opcodes.Fneg:
                case Opcodes.Ineg:
                case Opcodes.L2D:
                case Opcodes.D2L:
                case Opcodes.F2I:
                case Opcodes.I2B:
                case Opcodes.I2C:
                case Opcodes.I2S:
                case Opcodes.I2F:
                case Opcodes.Arraylength:
                    break;
                case Opcodes.Aconst_Null:
                case Opcodes.Iconst_M1:
                case Opcodes.Iconst_0:
                case Opcodes.Iconst_1:
                case Opcodes.Iconst_2:
                case Opcodes.Iconst_3:
                case Opcodes.Iconst_4:
                case Opcodes.Iconst_5:
                case Opcodes.Fconst_0:
                case Opcodes.Fconst_1:
                case Opcodes.Fconst_2:
                case Opcodes.F2L: // 1 before 2 after
                case Opcodes.F2D:
                case Opcodes.I2L:
                case Opcodes.I2D:
                    PushValue(OTHER);
                    break;
                case Opcodes.Lconst_0:
                case Opcodes.Lconst_1:
                case Opcodes.Dconst_0:
                case Opcodes.Dconst_1:
                    PushValue(OTHER);
                    PushValue(OTHER);
                    break;
                case Opcodes.Iaload: // remove 2 add 1
                case Opcodes.Faload: // remove 2 add 1
                case Opcodes.Aaload: // remove 2 add 1
                case Opcodes.Baload: // remove 2 add 1
                case Opcodes.Caload: // remove 2 add 1
                case Opcodes.Saload: // remove 2 add 1
                case Opcodes.Pop:
                case Opcodes.Iadd:
                case Opcodes.Fadd:
                case Opcodes.Isub:
                case Opcodes.Lshl: // 3 before 2 after
                case Opcodes.Lshr: // 3 before 2 after
                case Opcodes.Lushr: // 3 before 2 after
                case Opcodes.L2I: // 2 before 1 after
                case Opcodes.L2F: // 2 before 1 after
                case Opcodes.D2I: // 2 before 1 after
                case Opcodes.D2F: // 2 before 1 after
                case Opcodes.Fsub:
                case Opcodes.Fmul:
                case Opcodes.Fdiv:
                case Opcodes.Frem:
                case Opcodes.Fcmpl: // 2 before 1 after
                case Opcodes.Fcmpg: // 2 before 1 after
                case Opcodes.Imul:
                case Opcodes.Idiv:
                case Opcodes.Irem:
                case Opcodes.Ishl:
                case Opcodes.Ishr:
                case Opcodes.Iushr:
                case Opcodes.Iand:
                case Opcodes.Ior:
                case Opcodes.Ixor:
                case Opcodes.Monitorenter:
                case Opcodes.Monitorexit:
                    PopValue();
                    break;
                case Opcodes.Pop2:
                case Opcodes.Lsub:
                case Opcodes.Lmul:
                case Opcodes.Ldiv:
                case Opcodes.Lrem:
                case Opcodes.Ladd:
                case Opcodes.Land:
                case Opcodes.Lor:
                case Opcodes.Lxor:
                case Opcodes.Dadd:
                case Opcodes.Dmul:
                case Opcodes.Dsub:
                case Opcodes.Ddiv:
                case Opcodes.Drem:
                    PopValue();
                    PopValue();
                    break;
                case Opcodes.Iastore:
                case Opcodes.Fastore:
                case Opcodes.Aastore:
                case Opcodes.Bastore:
                case Opcodes.Castore:
                case Opcodes.Sastore:
                case Opcodes.Lcmp: // 4 before 1 after
                case Opcodes.Dcmpl:
                case Opcodes.Dcmpg:
                    PopValue();
                    PopValue();
                    PopValue();
                    break;
                case Opcodes.Lastore:
                case Opcodes.Dastore:
                    PopValue();
                    PopValue();
                    PopValue();
                    PopValue();
                    break;
                case Opcodes.Dup:
                    PushValue(PeekValue());
                    break;
                case Opcodes.Dup_X1:
                    stackSize = _stackFrame.Count;
                    _stackFrame.Insert(stackSize - 2, _stackFrame[stackSize - 1]);
                    break;
                case Opcodes.Dup_X2:
                    stackSize = _stackFrame.Count;
                    _stackFrame.Insert(stackSize - 3, _stackFrame[stackSize - 1]);
                    break;
                case Opcodes.Dup2:
                    stackSize = _stackFrame.Count;
                    _stackFrame.Insert(stackSize - 2, _stackFrame[stackSize - 1]);
                    _stackFrame.Insert(stackSize - 2, _stackFrame[stackSize - 1]);
                    break;
                case Opcodes.Dup2_X1:
                    stackSize = _stackFrame.Count;
                    _stackFrame.Insert(stackSize - 3, _stackFrame[stackSize - 1]);
                    _stackFrame.Insert(stackSize - 3, _stackFrame[stackSize - 1]);
                    break;
                case Opcodes.Dup2_X2:
                    stackSize = _stackFrame.Count;
                    _stackFrame.Insert(stackSize - 4, _stackFrame[stackSize - 1]);
                    _stackFrame.Insert(stackSize - 4, _stackFrame[stackSize - 1]);
                    break;
                case Opcodes.Swap:
                    stackSize = _stackFrame.Count;
                    _stackFrame.Insert(stackSize - 2, _stackFrame[stackSize - 1]);
                    _stackFrame.RemoveAt(stackSize);
                    break;
                default:
                    throw new ArgumentException(InvalidOpcode + opcode);
            }
        }
        else
        {
            switch (opcode)
            {
                case Opcodes.Return:
                case Opcodes.Ireturn:
                case Opcodes.Freturn:
                case Opcodes.Areturn:
                case Opcodes.Lreturn:
                case Opcodes.Dreturn:
                case Opcodes.Athrow:
                    OnMethodExit(opcode);
                    break;
            }
        }

        base.VisitInsn(opcode);
    }

    public override void VisitVarInsn(int opcode, int varIndex)
    {
        base.VisitVarInsn(opcode, varIndex);
        if (_isConstructor && !_superClassConstructorCalled)
            switch (opcode)
            {
                case Opcodes.Iload:
                case Opcodes.Fload:
                    PushValue(OTHER);
                    break;
                case Opcodes.Lload:
                case Opcodes.Dload:
                    PushValue(OTHER);
                    PushValue(OTHER);
                    break;
                case Opcodes.Aload:
                    PushValue(varIndex == 0 ? UNINITIALIZED_THIS : OTHER);
                    break;
                case Opcodes.Astore:
                case Opcodes.Istore:
                case Opcodes.Fstore:
                    PopValue();
                    break;
                case Opcodes.Lstore:
                case Opcodes.Dstore:
                    PopValue();
                    PopValue();
                    break;
                case Opcodes.Ret:
                    EndConstructorBasicBlockWithoutSuccessor();
                    break;
                default:
                    throw new ArgumentException(InvalidOpcode + opcode);
            }
    }

    public override void VisitFieldInsn(int opcode, string owner, string name, string descriptor)
    {
        base.VisitFieldInsn(opcode, owner, name, descriptor);
        if (_isConstructor && !_superClassConstructorCalled)
        {
            char firstDescriptorChar = descriptor[0];
            bool longOrDouble = firstDescriptorChar == 'J' || firstDescriptorChar == 'D';
            switch (opcode)
            {
                case Opcodes.Getstatic:
                    PushValue(OTHER);
                    if (longOrDouble) PushValue(OTHER);
                    break;
                case Opcodes.Putstatic:
                    PopValue();
                    if (longOrDouble) PopValue();
                    break;
                case Opcodes.Putfield:
                    PopValue();
                    PopValue();
                    if (longOrDouble) PopValue();
                    break;
                case Opcodes.Getfield:
                    if (longOrDouble) PushValue(OTHER);
                    break;
                default:
                    throw new ArgumentException(InvalidOpcode + opcode);
            }
        }
    }

    public override void VisitIntInsn(int opcode, int operand)
    {
        base.VisitIntInsn(opcode, operand);
        if (_isConstructor && !_superClassConstructorCalled && opcode != Opcodes.Newarray) PushValue(OTHER);
    }

    public override void VisitLdcInsn(object value)
    {
        base.VisitLdcInsn(value);
        if (_isConstructor && !_superClassConstructorCalled)
        {
            PushValue(OTHER);
            if (value is double? || value is long? ||
                value is ConstantDynamic && ((ConstantDynamic)value).Size == 2) PushValue(OTHER);
        }
    }

    public override void VisitMultiANewArrayInsn(string descriptor, int numDimensions)
    {
        base.VisitMultiANewArrayInsn(descriptor, numDimensions);
        if (_isConstructor && !_superClassConstructorCalled)
        {
            for (int i = 0; i < numDimensions; i++) PopValue();
            PushValue(OTHER);
        }
    }

    public override void VisitTypeInsn(int opcode, string type)
    {
        base.VisitTypeInsn(opcode, type);
        // ANEWARRAY, CHECKCAST or INSTANCEOF don't change stack.
        if (_isConstructor && !_superClassConstructorCalled && opcode == Opcodes.New) PushValue(OTHER);
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
        int opcode = opcodeAndSource & ~Opcodes.Source_Mask;

        DoVisitMethodInsn(opcode, name, descriptor);
    }

    private void DoVisitMethodInsn(int opcode, string name, string descriptor)
    {
        if (_isConstructor && !_superClassConstructorCalled)
        {
            foreach (JType argumentType in JType.GetArgumentTypes(descriptor))
            {
                PopValue();
                if (argumentType.Size == 2) PopValue();
            }

            switch (opcode)
            {
                case Opcodes.Invokeinterface:
                case Opcodes.Invokevirtual:
                    PopValue();
                    break;
                case Opcodes.Invokespecial:
                    object value = PopValue();
                    if (value == UNINITIALIZED_THIS && !_superClassConstructorCalled && name.Equals("<init>"))
                    {
                        _superClassConstructorCalled = true;
                        OnMethodEnter();
                    }

                    break;
            }

            JType returnType = JType.GetReturnType(descriptor);
            if (returnType != JType.VoidType)
            {
                PushValue(OTHER);
                if (returnType.Size == 2) PushValue(OTHER);
            }
        }
    }

    public override void VisitInvokeDynamicInsn(string name, string descriptor, Handle bootstrapMethodHandle,
        params object[] bootstrapMethodArguments)
    {
        base.VisitInvokeDynamicInsn(name, descriptor, bootstrapMethodHandle, bootstrapMethodArguments);
        DoVisitMethodInsn(Opcodes.Invokedynamic, name, descriptor);
    }

    public override void VisitJumpInsn(int opcode, Label label)
    {
        base.VisitJumpInsn(opcode, label);
        if (_isConstructor && !_superClassConstructorCalled)
        {
            switch (opcode)
            {
                case Opcodes.Ifeq:
                case Opcodes.Ifne:
                case Opcodes.Iflt:
                case Opcodes.Ifge:
                case Opcodes.Ifgt:
                case Opcodes.Ifle:
                case Opcodes.Ifnull:
                case Opcodes.Ifnonnull:
                    PopValue();
                    break;
                case Opcodes.If_Icmpeq:
                case Opcodes.If_Icmpne:
                case Opcodes.If_Icmplt:
                case Opcodes.If_Icmpge:
                case Opcodes.If_Icmpgt:
                case Opcodes.If_Icmple:
                case Opcodes.If_Acmpeq:
                case Opcodes.If_Acmpne:
                    PopValue();
                    PopValue();
                    break;
                case Opcodes.Jsr:
                    PushValue(OTHER);
                    break;
                case Opcodes.Goto:
                    EndConstructorBasicBlockWithoutSuccessor();
                    break;
            }

            AddForwardJump(label);
        }
    }

    public override void VisitLookupSwitchInsn(Label dflt, int[] keys, Label[] labels)
    {
        base.VisitLookupSwitchInsn(dflt, keys, labels);
        if (_isConstructor && !_superClassConstructorCalled)
        {
            PopValue();
            AddForwardJumps(dflt, labels);
            EndConstructorBasicBlockWithoutSuccessor();
        }
    }

    public override void VisitTableSwitchInsn(int min, int max, Label dflt, params Label[] labels)
    {
        base.VisitTableSwitchInsn(min, max, dflt, labels);
        if (_isConstructor && !_superClassConstructorCalled)
        {
            PopValue();
            AddForwardJumps(dflt, labels);
            EndConstructorBasicBlockWithoutSuccessor();
        }
    }

    public override void VisitTryCatchBlock(Label start, Label end, Label handler, string type)
    {
        base.VisitTryCatchBlock(start, end, handler, type);
        // By definition of 'forwardJumpStackFrames', 'handler' should be pushed only if there is an
        // instruction between 'start' and 'end' at which the super class constructor is not yet
        // called. Unfortunately, try catch blocks must be visited before their labels, so we have no
        // way to know this at this point. Instead, we suppose that the super class constructor has not
        // been called at the start of *any* exception handler. If this is wrong, normally there should
        // not be a second super class constructor call in the exception handler (an object can't be
        // initialized twice), so this is not issue (in the sense that there is no risk to emit a wrong
        // 'onMethodEnter').
        if (_isConstructor && !_forwardJumpStackFrames.ContainsKey(handler))
        {
            List<object> handlerStackFrame = new List<object>();
            handlerStackFrame.Add(OTHER);
            _forwardJumpStackFrames[handler] = handlerStackFrame;
        }
    }

    private void AddForwardJumps(Label dflt, Label[] labels)
    {
        AddForwardJump(dflt);
        foreach (Label label in labels) AddForwardJump(label);
    }

    private void AddForwardJump(Label label)
    {
        if (_forwardJumpStackFrames.ContainsKey(label)) return;
        _forwardJumpStackFrames[label] = new List<object>(_stackFrame);
    }

    private void EndConstructorBasicBlockWithoutSuccessor()
    {
        // The next instruction is not reachable from this instruction. If it is dead code, we
        // should not try to simulate stack operations, and there is no need to insert advices
        // here. If it is reachable with a backward jump, the only possible case Opcodes.is that the super
        // class constructor has already been called (backward jumps are forbidden before it is
        // called). If it is reachable with a forward jump, there are two sub-cases. Either the
        // super class constructor has already been called when reaching the next instruction, or
        // it has not been called. But in this case Opcodes.there must be a forwardJumpStackFrames entry
        // for a Label designating the next instruction, and superClassConstructorCalled will be
        // reset to false there. We can therefore always reset this field to true here.
        _superClassConstructorCalled = true;
    }

    private object PopValue()
    {
        int index = _stackFrame.Count - 1;
        object oldValue = _stackFrame[index];
        _stackFrame.RemoveAt(index);
        return oldValue;
    }

    private object PeekValue()
    {
        return _stackFrame[_stackFrame.Count - 1];
    }

    private void PushValue(object value)
    {
        _stackFrame.Add(value);
    }

    /// <summary>
    ///     Generates the "before" advice for the visited method. The default implementation of this method
    ///     does nothing. Subclasses can use or change all the local variables, but should not change state
    ///     of the stack. This method is called at the beginning of the method or after super class
    ///     constructor has been called (in constructors).
    /// </summary>
    public virtual void OnMethodEnter()
    {
    }

    /// <summary>
    ///     Generates the "after" advice for the visited method. The default implementation of this method
    ///     does nothing. Subclasses can use or change all the local variables, but should not change state
    ///     of the stack. This method is called at the end of the method, just before return and athrow
    ///     instructions. The top element on the stack contains the return value or the exception instance.
    ///     For example:
    ///     <pre>
    ///         public void onMethodExit(final int opcode) {
    ///         if (opcode == RETURN) {
    ///         visitInsn(ACONST_NULL);
    ///         } else if (opcode == ARETURN || opcode == ATHROW) {
    ///         dup();
    ///         } else {
    ///         if (opcode == LRETURN || opcode == DRETURN) {
    ///         dup2();
    ///         } else {
    ///         dup();
    ///         }
    ///         box(Type.getReturnType(this.methodDesc));
    ///         }
    ///         visitIntInsn(SIPUSH, opcode);
    ///         visitMethodInsn(INVOKESTATIC, owner, "onExit", "(Ljava/lang/Object;I)V");
    ///         }
    ///         // An actual call back method.
    ///         public static void onExit(final Object exitValue, final int opcode) {
    ///         ...
    ///         }
    ///     </pre>
    /// </summary>
    /// <param name="opcode">
    ///     one of <see cref="Opcodes.Return />, <see cref="Opcodes.Ireturn />, 
    ///     <see cref="Opcodes.Freturn />,
    ///     <see cref="Opcodes.Areturn />, 
    ///     <see cref="Opcodes.Lreturn />, <see cref="Opcodes.Dreturn /> or
    ///     <see cref="Opcodes.ATHROW"/>.
    /// 
    /// </param>
    public virtual void OnMethodExit(int opcode)
    {
    }
}