using System;
using System.Linq;
using System.Reflection;
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
///     A Java field or method type. This class can be used to make it easier to manipulate type and
///     method descriptors.
///     @author Eric Bruneton
///     @author Chris Nokleberg
/// </summary>
public sealed class JType
{
    /// <summary>
    ///     The sort of the <c>void</c> type. See <see cref="Sort" />.
    /// </summary>
    public const int Void = 0;

    /// <summary>
    ///     The sort of the <c>boolean</c> type. See <see cref="Sort" />.
    /// </summary>
    public const int Boolean = 1;

    /// <summary>
    ///     The sort of the <c>char</c> type. See <see cref="Sort" />.
    /// </summary>
    public const int Char = 2;

    /// <summary>
    ///     The sort of the <c>byte</c> type. See <see cref="Sort" />.
    /// </summary>
    public const int Byte = 3;

    /// <summary>
    ///     The sort of the <c>short</c> type. See <see cref="Sort" />.
    /// </summary>
    public const int Short = 4;

    /// <summary>
    ///     The sort of the <c>int</c> type. See <see cref="Sort" />.
    /// </summary>
    public const int Int = 5;

    /// <summary>
    ///     The sort of the <c>float</c> type. See <see cref="Sort" />.
    /// </summary>
    public const int Float = 6;

    /// <summary>
    ///     The sort of the <c>long</c> type. See <see cref="Sort" />.
    /// </summary>
    public const int Long = 7;

    /// <summary>
    ///     The sort of the <c>double</c> type. See <see cref="Sort" />.
    /// </summary>
    public const int Double = 8;

    /// <summary>
    ///     The sort of array reference types. See <see cref="Sort" />.
    /// </summary>
    public const int Array = 9;

    /// <summary>
    ///     The sort of object reference types. See <see cref="Sort" />.
    /// </summary>
    public const int Object = 10;

    /// <summary>
    ///     The sort of method types. See <see cref="Sort" />.
    /// </summary>
    public const int Method = 11;

    /// <summary>
    ///     The (private) sort of object reference types represented with an internal name.
    /// </summary>
    private const int Internal = 12;

    /// <summary>
    ///     The descriptors of the primitive types.
    /// </summary>
    private const string PrimitiveDescriptors = "VZCBSIFJD";

    /// <summary>
    ///     The <c>void</c> type.
    /// </summary>
    public static readonly JType VoidType = new JType(Void, PrimitiveDescriptors, Void, Void + 1);

    /// <summary>
    ///     The <c>boolean</c> type.
    /// </summary>
    public static readonly JType BooleanType = new JType(Boolean, PrimitiveDescriptors, Boolean, Boolean + 1);

    /// <summary>
    ///     The <c>char</c> type.
    /// </summary>
    public static readonly JType CharType = new JType(Char, PrimitiveDescriptors, Char, Char + 1);

    /// <summary>
    ///     The <c>byte</c> type.
    /// </summary>
    public static readonly JType ByteType = new JType(Byte, PrimitiveDescriptors, Byte, Byte + 1);

    /// <summary>
    ///     The <c>short</c> type.
    /// </summary>
    public static readonly JType ShortType = new JType(Short, PrimitiveDescriptors, Short, Short + 1);

    /// <summary>
    ///     The <c>int</c> type.
    /// </summary>
    public static readonly JType IntType = new JType(Int, PrimitiveDescriptors, Int, Int + 1);

    /// <summary>
    ///     The <c>float</c> type.
    /// </summary>
    public static readonly JType FloatType = new JType(Float, PrimitiveDescriptors, Float, Float + 1);

    /// <summary>
    ///     The <c>long</c> type.
    /// </summary>
    public static readonly JType LongType = new JType(Long, PrimitiveDescriptors, Long, Long + 1);

    /// <summary>
    ///     The <c>double</c> type.
    /// </summary>
    public static readonly JType DoubleType = new JType(Double, PrimitiveDescriptors, Double, Double + 1);

    // -----------------------------------------------------------------------------------------------
    // Fields
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    ///     The sort of this type. Either <see cref="Void" />, <see cref="Boolean" />, <see cref="Char" />,
    ///     <see cref="Byte" />,
    ///     <see cref="short" />, <see cref="int" />, <see cref="float" />, <see cref="long" />,
    ///     <see cref="double" />, <see cref="Array" />,
    ///     <see cref="Object" />, <see cref="Method" /> or <see cref="internal" />.
    /// </summary>
    private readonly int _sort;

    /// <summary>
    ///     The beginning index, inclusive, of the value of this Java field or method type in <see cref="valueBuffer"/>. This value is an internal name for <see cref="Object" /> and <see cref="internal" /> types,
    ///     and a field or method descriptor in the other cases.
    /// </summary>
    private readonly int _valueBegin;

    /// <summary>
    ///     A buffer containing the value of this field or method type. This value is an internal name for
    ///     <see cref="Object" /> and <see cref="internal" /> types, and a field or method descriptor in the other
    ///     cases.
    ///     <para>
    ///         For <see cref="Object" /> types, this field also contains the descriptor: the characters in
    ///         [<see cref="_valueBegin" />,<see cref="_valueEnd" />) contain the internal name, and those in [<see cref="valueBegin"/> - 1, <see cref="_valueEnd" /> + 1) contain the descriptor.
    ///     </para>
    /// </summary>
    private readonly string _valueBuffer;

    /// <summary>
    ///     The end index, exclusive, of the value of this Java field or method type in <see cref="valueBuffer"/>. This value is an internal name for <see cref="Object" /> and <see cref="internal" /> types,
    ///     and a field or method descriptor in the other cases.
    /// </summary>
    private readonly int _valueEnd;

    /// <summary>
    ///     Constructs a reference type.
    /// </summary>
    /// <param name="sort"> the sort of this type, see <see cref="_sort" />. </param>
    /// <param name="valueBuffer"> a buffer containing the value of this field or method type. </param>
    /// <param name="valueBegin">
    ///     the beginning index, inclusive, of the value of this field or method type in
    ///     valueBuffer.
    /// </param>
    /// <param name="valueEnd">
    ///     the end index, exclusive, of the value of this field or method type in
    ///     valueBuffer.
    /// </param>
    private JType(int sort, string valueBuffer, int valueBegin, int valueEnd)
    {
        this._sort = sort;
        this._valueBuffer = valueBuffer;
        this._valueBegin = valueBegin;
        this._valueEnd = valueEnd;
    }

    /// <summary>
    ///     Returns the type of the elements of this array type. This method should only be used for an
    ///     array type.
    /// </summary>
    /// <returns> Returns the type of the elements of this array type. </returns>
    public JType ElementType
    {
        get
        {
            int numDimensions = Dimensions;
            return GetTypeInternal(_valueBuffer, _valueBegin + numDimensions, _valueEnd);
        }
    }

    /// <summary>
    ///     Returns the argument types of methods of this type. This method should only be used for method
    ///     types.
    /// </summary>
    /// <returns> the argument types of methods of this type. </returns>
    public JType[] ArgumentTypes => GetArgumentTypes(Descriptor);

    /// <summary>
    ///     Returns the return type of methods of this type. This method should only be used for method
    ///     types.
    /// </summary>
    /// <returns> the return type of methods of this type. </returns>
    public JType ReturnType => GetReturnType(Descriptor);

    // -----------------------------------------------------------------------------------------------
    // Methods to get class names, internal names or descriptors.
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    ///     Returns the binary name of the class corresponding to this type. This method must not be used
    ///     on method types.
    /// </summary>
    /// <returns> the binary name of the class corresponding to this type. </returns>
    public string ClassName
    {
        get
        {
            switch (_sort)
            {
                case Void:
                    return "void";
                case Boolean:
                    return "boolean";
                case Char:
                    return "char";
                case Byte:
                    return "byte";
                case Short:
                    return "short";
                case Int:
                    return "int";
                case Float:
                    return "float";
                case Long:
                    return "long";
                case Double:
                    return "double";
                case Array:
                    StringBuilder stringBuilder = new StringBuilder(ElementType.ClassName);
                    for (int i = Dimensions; i > 0; --i) stringBuilder.Append("[]");
                    return stringBuilder.ToString();
                case Object:
                case Internal:
                    return _valueBuffer.Substring(_valueBegin, _valueEnd - _valueBegin).Replace('/', '.');
                default:
                    throw new Exception("Unknown type sort");
            }
        }
    }

    /// <summary>
    ///     Returns the internal name of the class corresponding to this object or array type. The internal
    ///     name of a class is its fully qualified name (as returned by Class.getName(), where '.' are
    ///     replaced by '/'). This method should only be used for an object or array type.
    /// </summary>
    /// <returns> the internal name of the class corresponding to this object type. </returns>
    public string InternalName => _valueBuffer.Substring(_valueBegin, _valueEnd - _valueBegin);

    /// <summary>
    ///     Returns the descriptor corresponding to this type.
    /// </summary>
    /// <returns> the descriptor corresponding to this type. </returns>
    public string Descriptor
    {
        get
        {
            if (_sort == Object)
                return _valueBuffer.Substring(_valueBegin - 1, _valueEnd + 1 - (_valueBegin - 1));
            if (_sort == Internal)
                return 'L' + _valueBuffer.Substring(_valueBegin, _valueEnd - _valueBegin) + ';';
            return _valueBuffer.Substring(_valueBegin, _valueEnd - _valueBegin);
        }
    }

    // -----------------------------------------------------------------------------------------------
    // Methods to get the sort, dimension, size, and opcodes corresponding to a Type or descriptor.
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    ///     Returns the sort of this type.
    /// </summary>
    /// <returns>
    ///     <see cref="Void" />, <see cref="Boolean" />, <see cref="Char" />, <see cref="Byte" />,
    ///     <see cref="short" />, <see cref="INT"/>, <see cref="float" />, <see cref="long" />, <see cref="double" />, <see cref="Array" />,
    ///     <see cref="Object" /> or
    ///     <see cref="Method" />.
    /// </returns>
    public int Sort => _sort == Internal ? Object : _sort;

    /// <summary>
    ///     Returns the number of dimensions of this array type. This method should only be used for an
    ///     array type.
    /// </summary>
    /// <returns> the number of dimensions of this array type. </returns>
    public int Dimensions
    {
        get
        {
            int numDimensions = 1;
            while (_valueBuffer[_valueBegin + numDimensions] == '[') numDimensions++;
            return numDimensions;
        }
    }

    /// <summary>
    ///     Returns the size of values of this type. This method must not be used for method types.
    /// </summary>
    /// <returns>
    ///     the size of values of this type, i.e., 2 for <c>long</c> and <c>double</c>, 0 for
    ///     <c>void</c> and 1 otherwise.
    /// </returns>
    public int Size
    {
        get
        {
            switch (_sort)
            {
                case Void:
                    return 0;
                case Boolean:
                case Char:
                case Byte:
                case Short:
                case Int:
                case Float:
                case Array:
                case Object:
                case Internal:
                    return 1;
                case Long:
                case Double:
                    return 2;
                default:
                    throw new Exception("AssertionError");
            }
        }
    }
        
    /// <summary>
    /// The number of arguments of this method type. This property should only be used for method types.
    /// Each argument counts for 1, even long and double ones. The implicit <c>this</c> argument is not counted.
    /// </summary>
    public int ArgumentCount => GetArgumentCount(Descriptor);
        
    /// <summary>
    /// Returns the number of arguments in the given method descriptor.
    /// </summary>
    /// <param name="methodDescriptor"> a method descriptor. </param>
    /// <returns> the number of arguments in the given method descriptor. Each argument counts for 1,
    /// even long and double ones. The implicit <c>this</c> argument is not counted. </returns>
    public static int GetArgumentCount(string methodDescriptor) {
        int argumentCount = 0;
        // Skip the first character, which is always a '('.
        int currentOffset = 1;
        // Parse the argument types, one at a each loop iteration.
        while (methodDescriptor[currentOffset] != ')') {
            while (methodDescriptor[currentOffset] == '[') {
                currentOffset++;
            }
            if (methodDescriptor[currentOffset++] == 'L') {
                // Skip the argument descriptor content.
                int semiColumnOffset = methodDescriptor.IndexOf(';', currentOffset);
                currentOffset = Math.Max(currentOffset, semiColumnOffset + 1);
            }
            ++argumentCount;
        }
        return argumentCount;
    }

    /// <summary>
    ///     Returns the size of the arguments and of the return value of methods of this type. This method
    ///     should only be used for method types.
    /// </summary>
    /// <returns>
    ///     the size of the arguments of the method (plus one for the implicit this argument),
    ///     argumentsSize, and the size of its return value, returnSize, packed into a single int i =
    ///     <c>(argumentsSize &lt;&lt; 2) | returnSize</c> (argumentsSize is therefore equal to <c>i &gt;&gt; 2</c>,
    ///     and returnSize to <c>i &amp; 0x03</c>). Long and double values have size 2, the others have size 1.
    /// </returns>
    public int ArgumentsAndReturnSizes => GetArgumentsAndReturnSizes(Descriptor);

    // -----------------------------------------------------------------------------------------------
    // Methods to get Type(s) from a descriptor, a reflected Method or Constructor, other types, etc.
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    ///     Returns the <see cref="Type" /> corresponding to the given type descriptor.
    /// </summary>
    /// <param name="typeDescriptor"> a field or method type descriptor. </param>
    /// <returns> the <see cref="Type" /> corresponding to the given type descriptor. </returns>
    public static JType GetType(string typeDescriptor)
    {
        return GetTypeInternal(typeDescriptor, 0, typeDescriptor.Length);
    }

    /// <summary>
    ///     Returns the <see cref="Type" /> corresponding to the given class.
    /// </summary>
    /// <param name="clazz"> a class. </param>
    /// <returns> the <see cref="Type" /> corresponding to the given class. </returns>
    public static JType GetType(Type clazz)
    {
        if (clazz.IsPrimitive)
        {
            if (clazz == typeof(int))
                return IntType;
            if (clazz == typeof(void))
                return VoidType;
            if (clazz == typeof(bool))
                return BooleanType;
            if (clazz == typeof(byte) || clazz == typeof(sbyte))
                return ByteType;
            if (clazz == typeof(char))
                return CharType;
            if (clazz == typeof(short))
                return ShortType;
            if (clazz == typeof(double))
                return DoubleType;
            if (clazz == typeof(float))
                return FloatType;
            if (clazz == typeof(long))
                return LongType;
            throw new Exception("Unknown primitive type");
        }

        return GetType(GetDescriptor(clazz));
    }

    /// <summary>
    ///     Returns the method <see cref="Type" /> corresponding to the given constructor.
    /// </summary>
    /// <param name="constructor"> a <see cref="System.Reflection.ConstructorInfo" /> object. </param>
    /// <returns> the method <see cref="Type" /> corresponding to the given constructor. </returns>
    public static JType GetType(ConstructorInfo constructor)
    {
        return GetType(GetConstructorDescriptor(constructor));
    }

    /// <summary>
    ///     Returns the method <see cref="Type" /> corresponding to the given method.
    /// </summary>
    /// <param name="method"> a <see cref="System.Reflection.MethodInfo" /> object. </param>
    /// <returns> the method <see cref="Type" /> corresponding to the given method. </returns>
    public static JType GetType(MethodInfo method)
    {
        return GetType(GetMethodDescriptor(method));
    }

    /// <summary>
    ///     Returns the <see cref="Type" /> corresponding to the given internal name.
    /// </summary>
    /// <param name="internalName"> an internal name. (see <see cref="JType.InternalName"/>) </param>
    /// <returns> the <see cref="Type" /> corresponding to the given internal name. </returns>
    public static JType GetObjectType(string internalName)
    {
        return new JType(internalName[0] == '[' ? Array : Internal, internalName, 0, internalName.Length);
    }

    /// <summary>
    ///     Returns the <see cref="Type" /> corresponding to the given method descriptor. Equivalent to
    ///     <code>
    /// Type.getType(methodDescriptor)</code>
    ///     .
    /// </summary>
    /// <param name="methodDescriptor"> a method descriptor. </param>
    /// <returns> the <see cref="Type" /> corresponding to the given method descriptor. </returns>
    public static JType GetMethodType(string methodDescriptor)
    {
        return new JType(Method, methodDescriptor, 0, methodDescriptor.Length);
    }

    /// <summary>
    ///     Returns the method <see cref="Type" /> corresponding to the given argument and return types.
    /// </summary>
    /// <param name="returnType"> the return type of the method. </param>
    /// <param name="argumentTypes"> the argument types of the method. </param>
    /// <returns> the method <see cref="Type" /> corresponding to the given argument and return types. </returns>
    public static JType GetMethodType(JType returnType, params JType[] argumentTypes)
    {
        return GetType(GetMethodDescriptor(returnType, argumentTypes));
    }

    /// <summary>
    ///     Returns the <see cref="Type" /> values corresponding to the argument types of the given method
    ///     descriptor.
    /// </summary>
    /// <param name="methodDescriptor"> a method descriptor. </param>
    /// <returns>
    ///     the <see cref="Type" /> values corresponding to the argument types of the given method
    ///     descriptor.
    /// </returns>
    public static JType[] GetArgumentTypes(string methodDescriptor)
    {
        // First step: compute the number of argument types in methodDescriptor.
        int numArgumentTypes = GetArgumentCount(methodDescriptor);

        // Second step: create a Type instance for each argument type.
        JType[] argumentTypes = new JType[numArgumentTypes];
        // Skip the first character, which is always a '('.
        int currentOffset = 1;
        // Parse and create the argument types, one at each loop iteration.
        int currentArgumentTypeIndex = 0;
        while (methodDescriptor[currentOffset] != ')')
        {
            int currentArgumentTypeOffset = currentOffset;
            while (methodDescriptor[currentOffset] == '[') currentOffset++;
            if (methodDescriptor[currentOffset++] == 'L')
            {
                // Skip the argument descriptor content.
                int semiColumnOffset = methodDescriptor.IndexOf(';', currentOffset);
                currentOffset = Math.Max(currentOffset, semiColumnOffset + 1);
            }

            argumentTypes[currentArgumentTypeIndex++] =
                GetTypeInternal(methodDescriptor, currentArgumentTypeOffset, currentOffset);
        }

        return argumentTypes;
    }

    /// <summary>
    ///     Returns the <see cref="Type" /> values corresponding to the argument types of the given method.
    /// </summary>
    /// <param name="method"> a method. </param>
    /// <returns> the <see cref="Type" /> values corresponding to the argument types of the given method. </returns>
    public static JType[] GetArgumentTypes(MethodInfo method)
    {
        Type[] classes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        JType[] types = new JType[classes.Length];
        for (int i = classes.Length - 1; i >= 0; --i) types[i] = GetType(classes[i]);
        return types;
    }

    /// <summary>
    ///     Returns the <see cref="Type" /> corresponding to the return type of the given method descriptor.
    /// </summary>
    /// <param name="methodDescriptor"> a method descriptor. </param>
    /// <returns> the <see cref="Type" /> corresponding to the return type of the given method descriptor. </returns>
    public static JType GetReturnType(string methodDescriptor)
    {
        return GetTypeInternal(methodDescriptor, GetReturnTypeOffset(methodDescriptor), methodDescriptor.Length);
    }

    /// <summary>
    ///     Returns the <see cref="Type" /> corresponding to the return type of the given method.
    /// </summary>
    /// <param name="method"> a method. </param>
    /// <returns> the <see cref="Type" /> corresponding to the return type of the given method. </returns>
    public static JType GetReturnType(MethodInfo method)
    {
        return GetType(method.ReturnType);
    }

    /// <summary>
    ///     Returns the start index of the return type of the given method descriptor.
    /// </summary>
    /// <param name="methodDescriptor"> a method descriptor. </param>
    /// <returns> the start index of the return type of the given method descriptor. </returns>
    internal static int GetReturnTypeOffset(string methodDescriptor)
    {
        // Skip the first character, which is always a '('.
        int currentOffset = 1;
        // Skip the argument types, one at a each loop iteration.
        while (methodDescriptor[currentOffset] != ')')
        {
            while (methodDescriptor[currentOffset] == '[') currentOffset++;
            if (methodDescriptor[currentOffset++] == 'L')
            {
                // Skip the argument descriptor content.
                int semiColumnOffset = methodDescriptor.IndexOf(';', currentOffset);
                currentOffset = Math.Max(currentOffset, semiColumnOffset + 1);
            }
        }

        return currentOffset + 1;
    }

    /// <summary>
    ///     Returns the <see cref="Type" /> corresponding to the given field or method descriptor.
    /// </summary>
    /// <param name="descriptorBuffer"> a buffer containing the field or method descriptor. </param>
    /// <param name="descriptorBegin">
    ///     the beginning index, inclusive, of the field or method descriptor in
    ///     descriptorBuffer.
    /// </param>
    /// <param name="descriptorEnd">
    ///     the end index, exclusive, of the field or method descriptor in
    ///     descriptorBuffer.
    /// </param>
    /// <returns> the <see cref="Type" /> corresponding to the given type descriptor. </returns>
    private static JType GetTypeInternal(string descriptorBuffer, int descriptorBegin, int descriptorEnd)
    {
        switch (descriptorBuffer[descriptorBegin])
        {
            case 'V':
                return VoidType;
            case 'Z':
                return BooleanType;
            case 'C':
                return CharType;
            case 'B':
                return ByteType;
            case 'S':
                return ShortType;
            case 'I':
                return IntType;
            case 'F':
                return FloatType;
            case 'J':
                return LongType;
            case 'D':
                return DoubleType;
            case '[':
                return new JType(Array, descriptorBuffer, descriptorBegin, descriptorEnd);
            case 'L':
                return new JType(Object, descriptorBuffer, descriptorBegin + 1, descriptorEnd - 1);
            case '(':
                return new JType(Method, descriptorBuffer, descriptorBegin, descriptorEnd);
            default:
                throw new ArgumentException("Invalid descriptor: " + descriptorBuffer);
        }
    }

    /// <summary>
    ///     Returns the internal name of the given class. The internal name of a class is its fully
    ///     qualified name, as returned by Class.getName(), where '.' are replaced by '/'.
    /// </summary>
    /// <param name="clazz"> an object or array class. </param>
    /// <returns> the internal name of the given class. </returns>
    public static string GetInternalName(Type clazz)
    {
        return clazz.FullName.Replace('.', '/');
    }

    /// <summary>
    ///     Returns the descriptor corresponding to the given class.
    /// </summary>
    /// <param name="clazz"> an object class, a primitive class or an array class. </param>
    /// <returns> the descriptor corresponding to the given class. </returns>
    public static string GetDescriptor(Type clazz)
    {
        StringBuilder stringBuilder = new StringBuilder();
        AppendDescriptor(clazz, stringBuilder);
        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Returns the descriptor corresponding to the given constructor.
    /// </summary>
    /// <param name="constructor"> a <see cref="System.Reflection.ConstructorInfo" /> object. </param>
    /// <returns> the descriptor of the given constructor. </returns>
    public static string GetConstructorDescriptor(ConstructorInfo constructor)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append('(');
        Type[] parameters = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
        foreach (Type parameter in parameters) AppendDescriptor(parameter, stringBuilder);
        return stringBuilder.Append(")V").ToString();
    }

    /// <summary>
    ///     Returns the descriptor corresponding to the given argument and return types.
    /// </summary>
    /// <param name="returnType"> the return type of the method. </param>
    /// <param name="argumentTypes"> the argument types of the method. </param>
    /// <returns> the descriptor corresponding to the given argument and return types. </returns>
    public static string GetMethodDescriptor(JType returnType, params JType[] argumentTypes)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append('(');
        foreach (JType argumentType in argumentTypes) argumentType.AppendDescriptor(stringBuilder);
        stringBuilder.Append(')');
        returnType.AppendDescriptor(stringBuilder);
        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Returns the descriptor corresponding to the given method.
    /// </summary>
    /// <param name="method"> a <see cref="System.Reflection.MethodInfo" /> object. </param>
    /// <returns> the descriptor of the given method. </returns>
    public static string GetMethodDescriptor(MethodInfo method)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append('(');
        Type[] parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
        foreach (Type parameter in parameters) AppendDescriptor(parameter, stringBuilder);
        stringBuilder.Append(')');
        AppendDescriptor(method.ReturnType, stringBuilder);
        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Appends the descriptor corresponding to this type to the given string buffer.
    /// </summary>
    /// <param name="stringBuilder"> the string builder to which the descriptor must be appended. </param>
    private void AppendDescriptor(StringBuilder stringBuilder)
    {
        if (_sort == Object)
            stringBuilder.Append(_valueBuffer, _valueBegin - 1, _valueEnd + 1);
        else if (_sort == Internal)
            stringBuilder.Append('L').Append(_valueBuffer, _valueBegin, _valueEnd).Append(';');
        else
            stringBuilder.Append(_valueBuffer, _valueBegin, _valueEnd);
    }

    /// <summary>
    ///     Appends the descriptor of the given class to the given string builder.
    /// </summary>
    /// <param name="clazz"> the class whose descriptor must be computed. </param>
    /// <param name="stringBuilder"> the string builder to which the descriptor must be appended. </param>
    private static void AppendDescriptor(Type clazz, StringBuilder stringBuilder)
    {
        Type currentClass = clazz;
        while (currentClass.IsArray)
        {
            stringBuilder.Append('[');
            currentClass = currentClass.GetElementType();
        }

        if (currentClass.IsPrimitive)
        {
            char descriptor;
            if (currentClass == typeof(int) || currentClass == typeof(byte))
                descriptor = 'I';
            else if (currentClass == typeof(void))
                descriptor = 'V';
            else if (currentClass == typeof(bool))
                descriptor = 'Z';
            else if (currentClass == typeof(sbyte))
                descriptor = 'B';
            else if (currentClass == typeof(char))
                descriptor = 'C';
            else if (currentClass == typeof(short))
                descriptor = 'S';
            else if (currentClass == typeof(double))
                descriptor = 'D';
            else if (currentClass == typeof(float))
                descriptor = 'F';
            else if (currentClass == typeof(long))
                descriptor = 'J';
            else
                throw new Exception("Unknown primitive descriptor type");
            stringBuilder.Append(descriptor);
        }
        else
        {
            stringBuilder.Append('L').Append(GetInternalName(currentClass)).Append(';');
        }
    }

    /// <summary>
    ///     Computes the size of the arguments and of the return value of a method.
    /// </summary>
    /// <param name="methodDescriptor"> a method descriptor. </param>
    /// <returns>
    ///     the size of the arguments of the method (plus one for the implicit this argument),
    ///     argumentsSize, and the size of its return value, returnSize, packed into a single int i =
    ///     <c>(argumentsSize &lt;&lt; 2) | returnSize</c> (argumentsSize is therefore equal to <c>i &gt;&gt; 2</c>,
    ///     and returnSize to <c>i &amp; 0x03</c>). Long and double values have size 2, the others have size 1.
    /// </returns>
    public static int GetArgumentsAndReturnSizes(string methodDescriptor)
    {
        int argumentsSize = 1;
        // Skip the first character, which is always a '('.
        int currentOffset = 1;
        int currentChar = methodDescriptor[currentOffset];
        // Parse the argument types and compute their size, one at a each loop iteration.
        while (currentChar != ')')
        {
            if (currentChar == 'J' || currentChar == 'D')
            {
                currentOffset++;
                argumentsSize += 2;
            }
            else
            {
                while (methodDescriptor[currentOffset] == '[') currentOffset++;
                if (methodDescriptor[currentOffset++] == 'L')
                {
                    // Skip the argument descriptor content.
                    int semiColumnOffset = methodDescriptor.IndexOf(';', currentOffset);
                    currentOffset = Math.Max(currentOffset, semiColumnOffset + 1);
                }

                argumentsSize += 1;
            }

            currentChar = methodDescriptor[currentOffset];
        }

        currentChar = methodDescriptor[currentOffset + 1];
        if (currentChar == 'V') return argumentsSize << 2;

        int returnSize = currentChar == 'J' || currentChar == 'D' ? 2 : 1;
        return (argumentsSize << 2) | returnSize;
    }

    /// <summary>
    ///     Returns a JVM instruction opcode adapted to this <see cref="Type" />. This method must not be used for
    ///     method types.
    /// </summary>
    /// <param name="opcode">
    ///     a JVM instruction opcode. This opcode must be one of ILOAD, ISTORE, IALOAD,
    ///     IASTORE, IADD, ISUB, IMUL, IDIV, IREM, INEG, ISHL, ISHR, IUSHR, IAND, IOR, IXOR and
    ///     IRETURN.
    /// </param>
    /// <returns>
    ///     an opcode that is similar to the given opcode, but adapted to this <see cref="Type" />. For
    ///     example, if this type is <c>float</c> and <c>opcode</c> is IRETURN, this method returns
    ///     FRETURN.
    /// </returns>
    public int GetOpcode(int opcode)
    {
        if (opcode == Opcodes.Iaload || opcode == Opcodes.Iastore)
            switch (_sort)
            {
                case Boolean:
                case Byte:
                    return opcode + (Opcodes.Baload - Opcodes.Iaload);
                case Char:
                    return opcode + (Opcodes.Caload - Opcodes.Iaload);
                case Short:
                    return opcode + (Opcodes.Saload - Opcodes.Iaload);
                case Int:
                    return opcode;
                case Float:
                    return opcode + (Opcodes.Faload - Opcodes.Iaload);
                case Long:
                    return opcode + (Opcodes.Laload - Opcodes.Iaload);
                case Double:
                    return opcode + (Opcodes.Daload - Opcodes.Iaload);
                case Array:
                case Object:
                case Internal:
                    return opcode + (Opcodes.Aaload - Opcodes.Iaload);
                case Method:
                case Void:
                    throw new NotSupportedException();
                default:
                    throw new Exception("AssertionError");
            }

        switch (_sort)
        {
            case Void:
                if (opcode != Opcodes.Ireturn) throw new NotSupportedException();
                return Opcodes.Return;
            case Boolean:
            case Byte:
            case Char:
            case Short:
            case Int:
                return opcode;
            case Float:
                return opcode + (Opcodes.Freturn - Opcodes.Ireturn);
            case Long:
                return opcode + (Opcodes.Lreturn - Opcodes.Ireturn);
            case Double:
                return opcode + (Opcodes.Dreturn - Opcodes.Ireturn);
            case Array:
            case Object:
            case Internal:
                if (opcode != Opcodes.Iload && opcode != Opcodes.Istore && opcode != Opcodes.Ireturn)
                    throw new NotSupportedException();
                return opcode + (Opcodes.Areturn - Opcodes.Ireturn);
            case Method:
                throw new NotSupportedException();
            default:
                throw new Exception("AssertionError");
        }
    }

    // -----------------------------------------------------------------------------------------------
    // Equals, hashCode and toString.
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    ///     Tests if the given object is equal to this type.
    /// </summary>
    /// <param name="object"> the object to be compared to this type. </param>
    /// <returns> <c>true</c> if the given object is equal to this type. </returns>
    public override bool Equals(object @object)
    {
        if (this == @object) return true;
        if (!(@object is Type)) return false;
        JType other = (JType)@object;
        if ((_sort == Internal ? Object : _sort) != (other._sort == Internal ? Object : other._sort)) return false;
        int begin = _valueBegin;
        int end = _valueEnd;
        int otherBegin = other._valueBegin;
        int otherEnd = other._valueEnd;
        // Compare the values.
        if (end - begin != otherEnd - otherBegin) return false;
        for (int i = begin, j = otherBegin; i < end; i++, j++)
            if (_valueBuffer[i] != other._valueBuffer[j])
                return false;
        return true;
    }

    /// <summary>
    ///     Returns a hash code value for this type.
    /// </summary>
    /// <returns> a hash code value for this type. </returns>
    public override int GetHashCode()
    {
        int hashCode = 13 * (_sort == Internal ? Object : _sort);
        if (_sort >= Array)
            for (int i = _valueBegin, end = _valueEnd; i < end; i++)
                hashCode = 17 * (hashCode + _valueBuffer[i]);
        return hashCode;
    }

    /// <summary>
    ///     Returns a string representation of this type.
    /// </summary>
    /// <returns> the descriptor of this type. </returns>
    public override string ToString()
    {
        return Descriptor;
    }
}