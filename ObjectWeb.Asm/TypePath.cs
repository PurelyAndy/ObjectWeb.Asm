using System;
using System.Text;

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

namespace ObjectWeb.Asm;

/// <summary>
///     The path to a type argument, wildcard bound, array element type, or static inner type within an
///     enclosing type.
///     @author Eric Bruneton
/// </summary>
public sealed class TypePath
{
    /// <summary>
    ///     A type path step that steps into the element type of an array type. See <see cref="GetStep" />.
    /// </summary>
    public const int Array_Element = 0;

    /// <summary>
    ///     A type path step that steps into the nested type of a class type. See <see cref="GetStep" />.
    /// </summary>
    public const int Inner_Type = 1;

    /// <summary>
    ///     A type path step that steps into the bound of a wildcard type. See <see cref="GetStep" />.
    /// </summary>
    public const int Wildcard_Bound = 2;

    /// <summary>
    ///     A type path step that steps into a type argument of a generic type. See <see cref="GetStep" />.
    /// </summary>
    public const int Type_Argument = 3;

    /// <summary>
    ///     The byte array where the 'type_path' structure - as defined in the Java Virtual Machine
    ///     Specification (JVMS) - corresponding to this TypePath is stored. The first byte of the
    ///     structure in this array is given by <see cref="_typePathOffset" />.
    /// </summary>
    /// <seealso cref=
    /// <a
    ///     href="https://docs.oracle.com/javase/specs/jvms/se9/html/jvms-4.html#jvms-4.7.20.2">
    ///     JVMS
    ///     4.7.20.2
    /// </a>
    /// </seealso>
    private readonly sbyte[] _typePathContainer;

    /// <summary>
    ///     The offset of the first byte of the type_path JVMS structure in <see cref="_typePathContainer" />.
    /// </summary>
    private readonly int _typePathOffset;

    /// <summary>
    ///     Constructs a new TypePath.
    /// </summary>
    /// <param name="typePathContainer"> a byte array containing a type_path JVMS structure. </param>
    /// <param name="typePathOffset">
    ///     the offset of the first byte of the type_path structure in
    ///     typePathContainer.
    /// </param>
    public TypePath(sbyte[] typePathContainer, int typePathOffset)
    {
        this._typePathContainer = typePathContainer;
        this._typePathOffset = typePathOffset;
    }

    /// <summary>
    ///     Returns the length of this path, i.e. its number of steps.
    /// </summary>
    /// <returns> the length of this path. </returns>
    public int Length =>
        // path_length is stored in the first byte of a type_path.
        _typePathContainer[_typePathOffset];

    /// <summary>
    ///     Returns the value of the given step of this path.
    /// </summary>
    /// <param name="index"> an index between 0 and <see cref="Length" />, exclusive. </param>
    /// <returns>
    ///     one of <see cref="Array_Element" />, <see cref="Inner_Type" />, <see cref="Wildcard_Bound" />, or
    ///     <see cref="TYPE_ARGUMENT"/>.
    /// </returns>
    public int GetStep(int index)
    {
        // Returns the type_path_kind of the path element of the given index.
        return _typePathContainer[_typePathOffset + 2 * index + 1];
    }

    /// <summary>
    ///     Returns the index of the type argument that the given step is stepping into. This method should
    ///     only be used for steps whose value is <see cref="Type_Argument" />.
    /// </summary>
    /// <param name="index"> an index between 0 and <see cref="Length" />, exclusive. </param>
    /// <returns> the index of the type argument that the given step is stepping into. </returns>
    public int GetStepArgument(int index)
    {
        // Returns the type_argument_index of the path element of the given index.
        return _typePathContainer[_typePathOffset + 2 * index + 2];
    }

    /// <summary>
    ///     Converts a type path in string form, in the format used by <see cref="toString()" />, into a TypePath
    ///     object.
    /// </summary>
    /// <param name="typePath">
    ///     a type path in string form, in the format used by <see cref="toString()" />. May be
    ///     <c>null</c> or empty.
    /// </param>
    /// <returns> the corresponding TypePath object, or <c>null</c> if the path is empty. </returns>
    public static TypePath FromString(string typePath)
    {
        if (ReferenceEquals(typePath, null) || typePath.Length == 0) return null;
        int typePathLength = typePath.Length;
        ByteVector output = new ByteVector(typePathLength);
        output.PutByte(0);
        int typePathIndex = 0;
        while (typePathIndex < typePathLength)
        {
            char c = typePath[typePathIndex++];
            if (c == '[')
            {
                output.Put11(Array_Element, 0);
            }
            else if (c == '.')
            {
                output.Put11(Inner_Type, 0);
            }
            else if (c == '*')
            {
                output.Put11(Wildcard_Bound, 0);
            }
            else if (c >= '0' && c <= '9')
            {
                int typeArg = c - '0';
                while (typePathIndex < typePathLength)
                {
                    c = typePath[typePathIndex++];
                    if (c >= '0' && c <= '9')
                        typeArg = typeArg * 10 + c - '0';
                    else if (c == ';')
                        break;
                    else
                        throw new ArgumentException();
                }

                output.Put11(Type_Argument, typeArg);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        output.data[0] = (sbyte)(output.length / 2);
        return new TypePath(output.data, 0);
    }

    /// <summary>
    ///     Returns a string representation of this type path. <see cref="Array_Element" /> steps are represented
    ///     with '[', <see cref="Inner_Type" /> steps with '.', <see cref="Wildcard_Bound" /> steps with '*' and <see cref="TYPE_ARGUMENT"/> steps with their type argument index in decimal form followed by ';'.
    /// </summary>
    public override string ToString()
    {
        int length = Length;
        StringBuilder result = new StringBuilder(length * 2);
        for (int i = 0; i < length; ++i)
            switch (GetStep(i))
            {
                case Array_Element:
                    result.Append('[');
                    break;
                case Inner_Type:
                    result.Append('.');
                    break;
                case Wildcard_Bound:
                    result.Append('*');
                    break;
                case Type_Argument:
                    result.Append(GetStepArgument(i)).Append(';');
                    break;
                default:
                    throw new Exception();
            }

        return result.ToString();
    }

    /// <summary>
    ///     Puts the type_path JVMS structure corresponding to the given TypePath into the given
    ///     ByteVector.
    /// </summary>
    /// <param name="typePath"> a TypePath instance, or <c>null</c> for empty paths. </param>
    /// <param name="output"> where the type path must be put. </param>
    internal static void Put(TypePath typePath, ByteVector output)
    {
        if (typePath == null)
        {
            output.PutByte(0);
        }
        else
        {
            int length = typePath._typePathContainer[typePath._typePathOffset] * 2 + 1;
            output.PutByteArray(typePath._typePathContainer, typePath._typePathOffset, length);
        }
    }
}