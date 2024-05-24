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

using System;

namespace ObjectWeb.Asm;

/// <summary>
///     A reference to a type appearing in a class, field or method declaration, or on an instruction.
///     Such a reference designates the part of the class where the referenced type is appearing (e.g. an
///     'extends', 'implements' or 'throws' clause, a 'new' instruction, a 'catch' clause, a type cast, a
///     local variable declaration, etc).
///     @author Eric Bruneton
/// </summary>
public class TypeReference
{
    /// <summary>
    ///     The sort of type references that target a type parameter of a generic class. See <see cref="Sort"/>.
    /// </summary>
    public const int Class_Type_Parameter = 0x00;

    /// <summary>
    ///     The sort of type references that target a type parameter of a generic method. See <see cref="Sort"/>.
    /// </summary>
    public const int Method_Type_Parameter = 0x01;

    /// <summary>
    ///     The sort of type references that target the super class of a class or one of the interfaces it
    ///     implements. See <see cref="Sort" />.
    /// </summary>
    public const int Class_Extends = 0x10;

    /// <summary>
    ///     The sort of type references that target a bound of a type parameter of a generic class. See
    ///     <see cref="Sort" />.
    /// </summary>
    public const int Class_Type_Parameter_Bound = 0x11;

    /// <summary>
    ///     The sort of type references that target a bound of a type parameter of a generic method. See
    ///     <see cref="Sort" />.
    /// </summary>
    public const int Method_Type_Parameter_Bound = 0x12;

    /// <summary>
    ///     The sort of type references that target the type of a field. See <see cref="Sort" />.
    /// </summary>
    public const int Field = 0x13;

    /// <summary>
    ///     The sort of type references that target the return type of a method. See <see cref="Sort" />.
    /// </summary>
    public const int Method_Return = 0x14;

    /// <summary>
    ///     The sort of type references that target the receiver type of a method. See <see cref="Sort" />.
    /// </summary>
    public const int Method_Receiver = 0x15;

    /// <summary>
    ///     The sort of type references that target the type of a formal parameter of a method. See <see cref="Sort"/>.
    /// </summary>
    public const int Method_Formal_Parameter = 0x16;

    /// <summary>
    ///     The sort of type references that target the type of an exception declared in the throws clause
    ///     of a method. See <see cref="Sort" />.
    /// </summary>
    public const int Throws = 0x17;

    /// <summary>
    ///     The sort of type references that target the type of a local variable in a method. See <see cref="Sort"/>.
    /// </summary>
    public const int Local_Variable = 0x40;

    /// <summary>
    ///     The sort of type references that target the type of a resource variable in a method. See <see cref="Sort"/>.
    /// </summary>
    public const int Resource_Variable = 0x41;

    /// <summary>
    ///     The sort of type references that target the type of the exception of a 'catch' clause in a
    ///     method. See <see cref="Sort" />.
    /// </summary>
    public const int Exception_Parameter = 0x42;

    /// <summary>
    ///     The sort of type references that target the type declared in an 'instanceof' instruction. See
    ///     <see cref="Sort" />.
    /// </summary>
    public const int Instanceof = 0x43;

    /// <summary>
    ///     The sort of type references that target the type of the object created by a 'new' instruction.
    ///     See <see cref="Sort" />.
    /// </summary>
    public const int New = 0x44;

    /// <summary>
    ///     The sort of type references that target the receiver type of a constructor reference. See
    ///     <see cref="Sort" />.
    /// </summary>
    public const int Constructor_Reference = 0x45;

    /// <summary>
    ///     The sort of type references that target the receiver type of a method reference. See <see cref="Sort"/>.
    /// </summary>
    public const int Method_Reference = 0x46;

    /// <summary>
    ///     The sort of type references that target the type declared in an explicit or implicit cast
    ///     instruction. See <see cref="Sort" />.
    /// </summary>
    public const int Cast = 0x47;

    /// <summary>
    ///     The sort of type references that target a type parameter of a generic constructor in a
    ///     constructor call. See <see cref="Sort" />.
    /// </summary>
    public const int Constructor_Invocation_Type_Argument = 0x48;

    /// <summary>
    ///     The sort of type references that target a type parameter of a generic method in a method call.
    ///     See <see cref="Sort" />.
    /// </summary>
    public const int Method_Invocation_Type_Argument = 0x49;

    /// <summary>
    ///     The sort of type references that target a type parameter of a generic constructor in a
    ///     constructor reference. See <see cref="Sort" />.
    /// </summary>
    public const int Constructor_Reference_Type_Argument = 0x4A;

    /// <summary>
    ///     The sort of type references that target a type parameter of a generic method in a method
    ///     reference. See <see cref="Sort" />.
    /// </summary>
    public const int Method_Reference_Type_Argument = 0x4B;

    /// <summary>
    ///     The target_type and target_info structures - as defined in the Java Virtual Machine
    ///     Specification (JVMS) - corresponding to this type reference. target_type uses one byte, and all
    ///     the target_info union fields use up to 3 bytes (except localvar_target, handled with the
    ///     specific method <see cref="MethodVisitor.VisitLocalVariableAnnotation" />). Thus, both structures can
    ///     be stored in an int.
    ///     <para>
    ///         This int field stores target_type (called the TypeReference 'sort' in the public API of this
    ///         class) in its most significant byte, followed by the target_info fields. Depending on
    ///         target_type, 1, 2 or even 3 least significant bytes of this field are unused. target_info
    ///         fields which reference bytecode offsets are set to 0 (these offsets are ignored in ClassReader,
    ///         and recomputed in MethodWriter).
    ///     </para>
    /// </summary>
    /// <seealso cref=
    /// <a href="https://docs.oracle.com/javase/specs/jvms/se9/html/jvms-4.html#jvms-4.7.20">
    ///     JVMS
    ///     4.7.20
    /// </a>
    /// </seealso>
    /// <seealso cref=
    /// <a
    ///     href="https://docs.oracle.com/javase/specs/jvms/se9/html/jvms-4.html#jvms-4.7.20.1">
    ///     JVMS
    ///     4.7.20.1
    /// </a>
    /// </seealso>
    private readonly int _targetTypeAndInfo;

    /// <summary>
    ///     Constructs a new TypeReference.
    /// </summary>
    /// <param name="typeRef">
    ///     the int encoded value of the type reference, as received in a visit method
    ///     related to type annotations, such as <see cref="ClassVisitor.VisitTypeAnnotation" />.
    /// </param>
    public TypeReference(int typeRef)
    {
            _targetTypeAndInfo = typeRef;
        }

    /// <summary>
    ///     Returns the sort of this type reference.
    /// </summary>
    /// <returns>
    ///     one of <see cref="Class_Type_Parameter" />, <see cref="Method_Type_Parameter" />, <see cref="Class_Extends"/>, <see cref="Class_Type_Parameter_Bound" />, <see cref="Method_Type_Parameter_Bound" />,
    ///     <see cref="Field" />, <see cref="Method_Return" />, <see cref="Method_Receiver" />, <see cref="Method_Formal_Parameter"/>, <see cref="Throws" />, <see cref="Local_Variable" />, <see cref="Resource_Variable"/>, <see cref="Exception_Parameter" />, <see cref="Instanceof" />, <see cref="New" />,
    ///     <see cref="Constructor_Reference" />, <see cref="Method_Reference" />, <see cref="Cast" />, <see cref="Constructor_Invocation_Type_Argument"/>, <see cref="Method_Invocation_Type_Argument" />, <see cref="Constructor_Reference_Type_Argument"/>, or <see cref="Method_Reference_Type_Argument" />.
    /// </returns>
    public virtual int Sort => _targetTypeAndInfo >>> 24;

    /// <summary>
    ///     Returns the index of the type parameter referenced by this type reference. This method must
    ///     only be used for type references whose sort is <see cref="Class_Type_Parameter" />, <see cref="Method_Type_Parameter"/>, <see cref="Class_Type_Parameter_Bound" /> or <see cref="Method_Type_Parameter_Bound"/>.
    /// </summary>
    /// <returns> a type parameter index. </returns>
    public virtual int TypeParameterIndex => (_targetTypeAndInfo & 0x00FF0000) >> 16;

    /// <summary>
    ///     Returns the index of the type parameter bound, within the type parameter <see cref="getTypeParameterIndex"/>, referenced by this type reference. This method must only be used for
    ///     type references whose sort is <see cref="Class_Type_Parameter_Bound" /> or <see cref="METHOD_TYPE_PARAMETER_BOUND"/>.
    /// </summary>
    /// <returns> a type parameter bound index. </returns>
    public virtual int TypeParameterBoundIndex => (_targetTypeAndInfo & 0x0000FF00) >> 8;

    /// <summary>
    ///     Returns the index of the "super type" of a class that is referenced by this type reference.
    ///     This method must only be used for type references whose sort is <see cref="Class_Extends" />.
    /// </summary>
    /// <returns>
    ///     the index of an interface in the 'implements' clause of a class, or -1 if this type
    ///     reference references the type of the super class.
    /// </returns>
    public virtual int SuperTypeIndex => (short)((_targetTypeAndInfo & 0x00FFFF00) >> 8);

    /// <summary>
    ///     Returns the index of the formal parameter whose type is referenced by this type reference. This
    ///     method must only be used for type references whose sort is <see cref="Method_Formal_Parameter" />.
    /// </summary>
    /// <returns> a formal parameter index. </returns>
    public virtual int FormalParameterIndex => (_targetTypeAndInfo & 0x00FF0000) >> 16;

    /// <summary>
    ///     Returns the index of the exception, in a 'throws' clause of a method, whose type is referenced
    ///     by this type reference. This method must only be used for type references whose sort is <see cref="THROWS"/>.
    /// </summary>
    /// <returns> the index of an exception in the 'throws' clause of a method. </returns>
    public virtual int ExceptionIndex => (_targetTypeAndInfo & 0x00FFFF00) >> 8;

    /// <summary>
    ///     Returns the index of the try catch block (using the order in which they are visited with
    ///     visitTryCatchBlock), whose 'catch' type is referenced by this type reference. This method must
    ///     only be used for type references whose sort is <see cref="Exception_Parameter" /> .
    /// </summary>
    /// <returns> the index of an exception in the 'throws' clause of a method. </returns>
    public virtual int TryCatchBlockIndex => (_targetTypeAndInfo & 0x00FFFF00) >> 8;

    /// <summary>
    ///     Returns the index of the type argument referenced by this type reference. This method must only
    ///     be used for type references whose sort is <see cref="Cast" />, <see cref="Constructor_Invocation_Type_Argument"/>, <see cref="Method_Invocation_Type_Argument" />, <see cref="Constructor_Reference_Type_Argument"/>, or <see cref="Method_Reference_Type_Argument" />.
    /// </summary>
    /// <returns> a type parameter index. </returns>
    public virtual int TypeArgumentIndex => _targetTypeAndInfo & 0xFF;

    /// <summary>
    ///     Returns the int encoded value of this type reference, suitable for use in visit methods related
    ///     to type annotations, like visitTypeAnnotation.
    /// </summary>
    /// <returns> the int encoded value of this type reference. </returns>
    public virtual int Value => _targetTypeAndInfo;

    /// <summary>
    ///     Returns a type reference of the given sort.
    /// </summary>
    /// <param name="sort">
    ///     one of <see cref="Field" />, <see cref="Method_Return" />, <see cref="Method_Receiver" />, <see cref="Local_Variable"/>, <see cref="Resource_Variable" />, <see cref="Instanceof" />, <see cref="New" />,
    ///     <see cref="Constructor_Reference"/>, or <see cref="Method_Reference" />.
    /// </param>
    /// <returns> a type reference of the given sort. </returns>
    public static TypeReference NewTypeReference(int sort)
    {
            return new TypeReference(sort << 24);
        }

    /// <summary>
    ///     Returns a reference to a type parameter of a generic class or method.
    /// </summary>
    /// <param name="sort"> one of <see cref="Class_Type_Parameter" /> or <see cref="Method_Type_Parameter" />. </param>
    /// <param name="paramIndex"> the type parameter index. </param>
    /// <returns> a reference to the given generic class or method type parameter. </returns>
    public static TypeReference NewTypeParameterReference(int sort, int paramIndex)
    {
            return new TypeReference((sort << 24) | (paramIndex << 16));
        }

    /// <summary>
    ///     Returns a reference to a type parameter bound of a generic class or method.
    /// </summary>
    /// <param name="sort"> one of <see cref="Class_Type_Parameter" /> or <see cref="Method_Type_Parameter" />. </param>
    /// <param name="paramIndex"> the type parameter index. </param>
    /// <param name="boundIndex"> the type bound index within the above type parameters. </param>
    /// <returns> a reference to the given generic class or method type parameter bound. </returns>
    public static TypeReference NewTypeParameterBoundReference(int sort, int paramIndex, int boundIndex)
    {
            return new TypeReference((sort << 24) | (paramIndex << 16) | (boundIndex << 8));
        }

    /// <summary>
    ///     Returns a reference to the super class or to an interface of the 'implements' clause of a
    ///     class.
    /// </summary>
    /// <param name="itfIndex">
    ///     the index of an interface in the 'implements' clause of a class, or -1 to
    ///     reference the super class of the class.
    /// </param>
    /// <returns> a reference to the given super type of a class. </returns>
    public static TypeReference NewSuperTypeReference(int itfIndex)
    {
            return new TypeReference((Class_Extends << 24) | ((itfIndex & 0xFFFF) << 8));
        }

    /// <summary>
    ///     Returns a reference to the type of a formal parameter of a method.
    /// </summary>
    /// <param name="paramIndex"> the formal parameter index. </param>
    /// <returns> a reference to the type of the given method formal parameter. </returns>
    public static TypeReference NewFormalParameterReference(int paramIndex)
    {
            return new TypeReference((Method_Formal_Parameter << 24) | (paramIndex << 16));
        }

    /// <summary>
    ///     Returns a reference to the type of an exception, in a 'throws' clause of a method.
    /// </summary>
    /// <param name="exceptionIndex"> the index of an exception in a 'throws' clause of a method. </param>
    /// <returns> a reference to the type of the given exception. </returns>
    public static TypeReference NewExceptionReference(int exceptionIndex)
    {
            return new TypeReference((Throws << 24) | (exceptionIndex << 8));
        }

    /// <summary>
    ///     Returns a reference to the type of the exception declared in a 'catch' clause of a method.
    /// </summary>
    /// <param name="tryCatchBlockIndex">
    ///     the index of a try catch block (using the order in which they are
    ///     visited with visitTryCatchBlock).
    /// </param>
    /// <returns> a reference to the type of the given exception. </returns>
    public static TypeReference NewTryCatchReference(int tryCatchBlockIndex)
    {
            return new TypeReference((Exception_Parameter << 24) | (tryCatchBlockIndex << 8));
        }

    /// <summary>
    ///     Returns a reference to the type of a type argument in a constructor or method call or
    ///     reference.
    /// </summary>
    /// <param name="sort">
    ///     one of <see cref="Cast" />, <see cref="Constructor_Invocation_Type_Argument" />, <see cref="Method_Invocation_Type_Argument"/>, <see cref="Constructor_Reference_Type_Argument" />, or <see cref="Method_Reference_Type_Argument"/>.
    /// </param>
    /// <param name="argIndex"> the type argument index. </param>
    /// <returns> a reference to the type of the given type argument. </returns>
    public static TypeReference NewTypeArgumentReference(int sort, int argIndex)
    {
            return new TypeReference((sort << 24) | argIndex);
        }

    /// <summary>
    ///     Puts the given target_type and target_info JVMS structures into the given ByteVector.
    /// </summary>
    /// <param name="targetTypeAndInfo">
    ///     a target_type and a target_info structures encoded as in <see cref="targetTypeAndInfo"/>. LOCAL_VARIABLE and Resource_Variable target types are not supported.
    /// </param>
    /// <param name="output"> where the type reference must be put. </param>
    internal static void PutTarget(int targetTypeAndInfo, ByteVector output)
    {
            switch (targetTypeAndInfo >>> 24)
            {
                case Class_Type_Parameter:
                case Method_Type_Parameter:
                case Method_Formal_Parameter:
                    output.PutShort(targetTypeAndInfo >>> 16);
                    break;
                case Field:
                case Method_Return:
                case Method_Receiver:
                    output.PutByte(targetTypeAndInfo >>> 24);
                    break;
                case Cast:
                case Constructor_Invocation_Type_Argument:
                case Method_Invocation_Type_Argument:
                case Constructor_Reference_Type_Argument:
                case Method_Reference_Type_Argument:
                    output.PutInt(targetTypeAndInfo);
                    break;
                case Class_Extends:
                case Class_Type_Parameter_Bound:
                case Method_Type_Parameter_Bound:
                case Throws:
                case Exception_Parameter:
                case Instanceof:
                case New:
                case Constructor_Reference:
                case Method_Reference:
                    output.Put12(targetTypeAndInfo >>> 24, (targetTypeAndInfo & 0xFFFF00) >> 8);
                    break;
                default:
                    throw new ArgumentException();
            }
        }
}