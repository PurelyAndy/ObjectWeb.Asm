﻿using System;

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
/// A non standard class, field, method or Code attribute, as defined in the Java Virtual Machine
/// Specification (JVMS).
/// </summary>
/// <seealso cref="<a href= "https://docs.oracle.com/javase/specs/jvms/se9/html/jvms-4.html#jvms-4.7">JVMS
///     4.7</a> </seealso>
/// <seealso cref="<a href= "https://docs.oracle.com/javase/specs/jvms/se9/html/jvms-4.html#jvms-4.7.3">JVMS
///     4.7.3</a>
/// @author Eric Bruneton
/// @author Eugene Kuleshov </seealso>
public class Attribute
{
    /// <summary>
    /// The type of this attribute, also called its name in the JVMS. </summary>
    public readonly string type;

    /// <summary>
    /// The raw content of this attribute, as returned by see <see cref="Write"/>.
    /// The 6 header bytes of the attribute (attribute_name_index and attribute_length) are <i>not</i>
    /// included.
    /// </summary>
    private ByteVector _cachedContent;

    /// <summary>
    /// The next attribute in this attribute list (Attribute instances can be linked via this field to
    /// store a list of class, field, method or Code attributes). May be <c>null</c>.
    /// </summary>
    internal Attribute nextAttribute;

    /// <summary>
    /// Constructs a new empty attribute.
    /// </summary>
    /// <param name="type"> the type of the attribute. </param>
    public Attribute(string type)
    {
        this.type = type;
    }

    /// <summary>
    /// Returns <c>true</c> if this type of attribute is unknown. This means that the attribute
    /// content can't be parsed to extract constant pool references, labels, etc. Instead, the
    /// attribute content is read as an opaque byte array, and written back as is. This can lead to
    /// invalid attributes, if the content actually contains constant pool references, labels, or other
    /// symbolic references that need to be updated when there are changes to the constant pool, the
    /// method bytecode, etc. The default implementation of this method always returns <c>true</c>.
    /// </summary>
    /// <returns> <c>true</c> if this type of attribute is unknown. </returns>
    public virtual bool Unknown => true;

    /// <summary>
    /// Returns <c>true</c> if this type of attribute is a Code attribute.
    /// </summary>
    /// <returns> <c>true</c> if this type of attribute is a Code attribute. </returns>
    public virtual bool CodeAttribute => false;

    /// <summary>
    /// Returns the labels corresponding to this attribute.
    /// </summary>
    /// <returns> the labels corresponding to this attribute, or <c>null</c> if this attribute is not
    ///     a Code attribute that contains labels. </returns>
    [Obsolete("no longer used by ASM.")]
    public virtual Label[] Labels => new Label[0];

    /// <summary>
    /// Reads a <see cref="type"/> attribute. This method must return a <i>new</i> <see cref="Attribute"/> object,
    /// of type <see cref="type"/>, corresponding to the 'length' bytes starting at 'offset', in the given
    /// ClassReader.
    /// </summary>
    /// <param name="classReader"> the class that contains the attribute to be read. </param>
    /// <param name="offset"> index of the first byte of the attribute's content in <see cref="ClassReader"/>. The 6
    ///     attribute header bytes (attribute_name_index and attribute_length) are not taken into
    ///     account here. </param>
    /// <param name="length"> the length of the attribute's content (excluding the 6 attribute header bytes). </param>
    /// <param name="charBuffer"> the buffer to be used to call the ClassReader methods requiring a
    ///     'charBuffer' parameter. </param>
    /// <param name="codeAttributeOffset"> index of the first byte of content of the enclosing Code attribute
    ///     in <see cref="ClassReader"/>, or -1 if the attribute to be read is not a Code attribute. The 6
    ///     attribute header bytes (attribute_name_index and attribute_length) are not taken into
    ///     account here. </param>
    /// <param name="labels"> the labels of the method's code, or <c>null</c> if the attribute to be read
    ///     is not a Code attribute. Labels defined in the attribute must be created and added to this
    ///     array, if not already present, by calling the <see cref="ReadLabel"/> method. (do not create
    ///     <see cref="Label"/> instances directly). </param>
    /// <returns> a <i>new</i> <see cref="Attribute"/> object corresponding to the specified bytes. </returns>
    public virtual Attribute Read(ClassReader classReader, int offset, int length, char[] charBuffer,
        int codeAttributeOffset, Label[] labels)
    {
        Attribute attribute = new Attribute(type);
        attribute._cachedContent = new ByteVector(classReader.ReadBytes(offset, length));
        return attribute;
    }
    
    /// <summary>
    /// Reads an attribute with the same <see cref="type"/> as the given attribute. This method returns a
    /// new <see cref="Attribute"/> object, corresponding to the 'length' bytes starting at 'offset', in the
    /// given <see cref="ClassReader"/>.
    /// </summary>
    /// <param name="attribute"> the attribute prototype that is used for reading. </param>
    /// <param name="classReader"> the class that contains the attribute to be read. </param>
    /// <param name="offset"> index of the first byte of the attribute's content in <see cref="ClassReader"/>. The 6
    ///     attribute header bytes (attribute_name_index and attribute_length) are not taken into
    ///     account here. </param>
    /// <param name="length"> the length of the attribute's content (excluding the 6 attribute header bytes). </param>
    /// <param name="charBuffer"> the buffer to be used to call the ClassReader methods requiring a
    ///     'charBuffer' parameter. </param>
    /// <param name="codeAttributeOffset"> index of the first byte of content of the enclosing Code attribute
    ///     in <see cref="ClassReader"/>, or -1 if the attribute to be read is not a Code attribute. The 6
    ///     attribute header bytes (attribute_name_index and attribute_length) are not taken into
    ///     account here. </param>
    /// <param name="labels"> the labels of the method's code, or <c>null</c> if the attribute to be read
    ///     is not a Code attribute. Labels defined in the attribute are added to this array, if not
    ///     already present. </param>
    /// <returns> a new <see cref="Attribute"/> object corresponding to the specified bytes. </returns>
    public static Attribute Read(Attribute attribute, ClassReader classReader, int offset, int length,
        char[] charBuffer, int codeAttributeOffset, Label[] labels)
    {
        return attribute.Read(classReader, offset, length, charBuffer, codeAttributeOffset, labels);
    }

    /// <summary>
    /// Returns the label corresponding to the given bytecode offset by calling
    /// <see cref="ClassReader.ReadLabel"/>. This creates and adds the label to the given array if it is not
    /// already present. Note that this created label may be a <see cref="Label"/> subclass instance, if
    /// the given <see cref="ClassReader"/> overrides <see cref="ClassReader.ReadLabel"/>. Hence
    /// <see cref="Read(ClassReader, int, int, char[], int, Label[])"/> must not manually create <see cref="Label"/>
    /// </summary>
    /// <param name="bytecodeOffset"> a bytecode offset in a method. </param>
    /// <param name="labels"> the already created labels, indexed by their offset. If a label already exists
    ///     for <see cref="bytecodeOffset"/> this method does not create a new one. Otherwise it stores the new label
    ///     in this array. </param>
    /// <returns> a label for the given bytecode offset. </returns>
    public static Label ReadLabel(ClassReader classReader, int bytecodeOffset, Label[] labels)
    {
        return classReader.ReadLabel(bytecodeOffset, labels);
    }
    
    /// <summary>
    /// Calls <see cref="Write(ClassWriter,byte[],int,int,int)"/> if it has not already been called and
    /// returns its result or its (cached) previous result.
    /// </summary>
    /// <param name="classWriter"> the class to which this attribute must be added. This parameter can be used
    ///     to add the items that corresponds to this attribute to the constant pool of this class. </param>
    /// <param name="code"> the bytecode of the method corresponding to this Code attribute, or <c>null</c>
    ///     if this attribute is not a Code attribute. Corresponds to the 'code' field of the Code
    ///     attribute. </param>
    /// <param name="codeLength"> the length of the bytecode of the method corresponding to this code
    ///     attribute, or 0 if this attribute is not a Code attribute. Corresponds to the 'code_length'
    ///     field of the Code attribute. </param>
    /// <param name="maxStack"> the maximum stack size of the method corresponding to this Code attribute, or
    ///     -1 if this attribute is not a Code attribute. </param>
    /// <param name="maxLocals"> the maximum number of local variables of the method corresponding to this code
    ///     attribute, or -1 if this attribute is not a Code attribute. </param>
    /// <returns> the byte array form of this attribute. </returns>
    private ByteVector MaybeWrite(ClassWriter classWriter, sbyte[] code, int codeLength, int maxStack, int maxLocals)
    {
        if (_cachedContent == null)
        {
            _cachedContent = Write(classWriter, code, codeLength, maxStack, maxLocals);
        }

        return _cachedContent;
    }

    /// <summary>
    /// Returns the byte array form of the content of this attribute. The 6 header bytes
    /// (attribute_name_index and attribute_length) must <i>not</i> be added in the returned
    /// ByteVector.
    ///
    /// This method is only invoked once to compute the binary form of this attribute. Subsequent
    /// changes to this attribute after it was written for the first time will not be considered.
    /// </summary>
    /// <param name="classWriter"> the class to which this attribute must be added. This parameter can be used
    ///     to add the items that corresponds to this attribute to the constant pool of this class. </param>
    /// <param name="code"> the bytecode of the method corresponding to this Code attribute, or <c>null</c>
    ///     if this attribute is not a Code attribute. Corresponds to the 'code' field of the Code
    ///     attribute. </param>
    /// <param name="codeLength"> the length of the bytecode of the method corresponding to this code
    ///     attribute, or 0 if this attribute is not a Code attribute. Corresponds to the 'code_length'
    ///     field of the Code attribute. </param>
    /// <param name="maxStack"> the maximum stack size of the method corresponding to this Code attribute, or
    ///     -1 if this attribute is not a Code attribute. </param>
    /// <param name="maxLocals"> the maximum number of local variables of the method corresponding to this code
    ///     attribute, or -1 if this attribute is not a Code attribute. </param>
    /// <returns> the byte array form of this attribute. </returns>
    public virtual ByteVector Write(ClassWriter classWriter, sbyte[] code, int codeLength, int maxStack,
        int maxLocals)
    {
        return _cachedContent;
    }
    
    /// <summary>
    /// Returns the byte array form of the content of the given attribute. The 6 header bytes
    /// (attribute_name_index and attribute_length) are <i>not</i> added in the returned byte array.
    /// </summary>
    /// <param name="attribute"> the attribute that should be written. </param>
    /// <param name="classWriter"> the class to which this attribute must be added. This parameter can be used
    ///     to add the items that corresponds to this attribute to the constant pool of this class. </param>
    /// <param name="code"> the bytecode of the method corresponding to this Code attribute, or <c>null</c>
    ///     if this attribute is not a Code attribute. Corresponds to the 'code' field of the Code
    ///     attribute. </param>
    /// <param name="codeLength"> the length of the bytecode of the method corresponding to this code
    ///     attribute, or 0 if this attribute is not a Code attribute. Corresponds to the 'code_length'
    ///     field of the Code attribute. </param>
    /// <param name="maxStack"> the maximum stack size of the method corresponding to this Code attribute, or
    ///     -1 if this attribute is not a Code attribute. </param>
    /// <param name="maxLocals"> the maximum number of local variables of the method corresponding to this code
    ///     attribute, or -1 if this attribute is not a Code attribute. </param>
    /// <returns> the byte array form of this attribute. </returns>
    public static sbyte[] Write(Attribute attribute, ClassWriter classWriter, sbyte[] code, int codeLength,
        int maxStack, int maxLocals)
    {
        ByteVector content = attribute.MaybeWrite(classWriter, code, codeLength, maxStack, maxLocals);
        sbyte[] result = new sbyte[content.length];
        Array.Copy(content.data, 0, result, 0, content.length);
        return result;
    }

    /// <summary>
    /// Returns the number of attributes of the attribute list that begins with this attribute.
    /// </summary>
    /// <returns> the number of attributes of the attribute list that begins with this attribute. </returns>
    public int AttributeCount
    {
        get
        {
            int count = 0;
            Attribute attribute = this;
            while (attribute != null)
            {
                count += 1;
                attribute = attribute.nextAttribute;
            }

            return count;
        }
    }

    /// <summary>
    /// Returns the total size in bytes of all the attributes in the attribute list that begins with
    /// this attribute. This size includes the 6 header bytes (attribute_name_index and
    /// attribute_length) per attribute. Also adds the attribute type names to the constant pool.
    /// </summary>
    /// <param name="symbolTable"> where the constants used in the attributes must be stored. </param>
    /// <returns> the size of all the attributes in this attribute list. This size includes the size of
    ///     the attribute headers. </returns>
    public int ComputeAttributesSize(SymbolTable symbolTable)
    {
        const sbyte[] code = null;
        const int codeLength = 0;
        const int maxStack = -1;
        const int maxLocals = -1;
        return ComputeAttributesSize(symbolTable, code, codeLength, maxStack, maxLocals);
    }

    /// <summary>
    /// Returns the total size in bytes of all the attributes in the attribute list that begins with
    /// this attribute. This size includes the 6 header bytes (attribute_name_index and
    /// attribute_length) per attribute. Also adds the attribute type names to the constant pool.
    /// </summary>
    /// <param name="symbolTable"> where the constants used in the attributes must be stored. </param>
    /// <param name="code"> the bytecode of the method corresponding to these Code attributes, or <c>null</c> if they are not Code attributes. Corresponds to the 'code' field of the Code
    ///     attribute. </param>
    /// <param name="codeLength"> the length of the bytecode of the method corresponding to these code
    ///     attributes, or 0 if they are not Code attributes. Corresponds to the 'code_length' field of
    ///     the Code attribute. </param>
    /// <param name="maxStack"> the maximum stack size of the method corresponding to these Code attributes, or
    ///     -1 if they are not Code attributes. </param>
    /// <param name="maxLocals"> the maximum number of local variables of the method corresponding to these
    ///     Code attributes, or -1 if they are not Code attribute. </param>
    /// <returns> the size of all the attributes in this attribute list. This size includes the size of
    ///     the attribute headers. </returns>
    public int ComputeAttributesSize(SymbolTable symbolTable, sbyte[] code, int codeLength, int maxStack,
        int maxLocals)
    {
        ClassWriter classWriter = symbolTable.classWriter;
        int size = 0;
        Attribute attribute = this;
        while (attribute != null)
        {
            symbolTable.AddConstantUtf8(attribute.type);
            size += 6 + attribute.MaybeWrite(classWriter, code, codeLength, maxStack, maxLocals).length;
            attribute = attribute.nextAttribute;
        }

        return size;
    }

    /// <summary>
    /// Returns the total size in bytes of all the attributes that correspond to the given field,
    /// method or class access flags and signature. This size includes the 6 header bytes
    /// (attribute_name_index and attribute_length) per attribute. Also adds the attribute type names
    /// to the constant pool.
    /// </summary>
    /// <param name="symbolTable"> where the constants used in the attributes must be stored. </param>
    /// <param name="accessFlags"> some field, method or class access flags. </param>
    /// <param name="signatureIndex"> the constant pool index of a field, method of class signature. </param>
    /// <returns> the size of all the attributes in bytes. This size includes the size of the attribute
    ///     headers. </returns>
    internal static int ComputeAttributesSize(SymbolTable symbolTable, int accessFlags, int signatureIndex)
    {
        int size = 0;
        // Before Java 1.5, synthetic fields are represented with a Synthetic attribute.
        if ((accessFlags & Opcodes.Acc_Synthetic) != 0 && symbolTable.MajorVersion < Opcodes.V1_5)
        {
            // Synthetic attributes always use 6 bytes.
            symbolTable.AddConstantUtf8(Constants.Synthetic);
            size += 6;
        }

        if (signatureIndex != 0)
        {
            // Signature attributes always use 8 bytes.
            symbolTable.AddConstantUtf8(Constants.Signature);
            size += 8;
        }

        // ACC_DEPRECATED is ASM specific, the ClassFile format uses a Deprecated attribute instead.
        if ((accessFlags & Opcodes.Acc_Deprecated) != 0)
        {
            // Deprecated attributes always use 6 bytes.
            symbolTable.AddConstantUtf8(Constants.Deprecated);
            size += 6;
        }

        return size;
    }

    /// <summary>
    /// Puts all the attributes of the attribute list that begins with this attribute, in the given
    /// byte vector. This includes the 6 header bytes (attribute_name_index and attribute_length) per
    /// attribute.
    /// </summary>
    /// <param name="symbolTable"> where the constants used in the attributes must be stored. </param>
    /// <param name="output"> where the attributes must be written. </param>
    public void PutAttributes(SymbolTable symbolTable, ByteVector output)
    {
        const sbyte[] code = null;
        const int codeLength = 0;
        const int maxStack = -1;
        const int maxLocals = -1;
        PutAttributes(symbolTable, code, codeLength, maxStack, maxLocals, output);
    }

    /// <summary>
    /// Puts all the attributes of the attribute list that begins with this attribute, in the given
    /// byte vector. This includes the 6 header bytes (attribute_name_index and attribute_length) per
    /// attribute.
    /// </summary>
    /// <param name="symbolTable"> where the constants used in the attributes must be stored. </param>
    /// <param name="code"> the bytecode of the method corresponding to these Code attributes, or <c>null</c> if they are not Code attributes. Corresponds to the 'code' field of the Code
    ///     attribute. </param>
    /// <param name="codeLength"> the length of the bytecode of the method corresponding to these code
    ///     attributes, or 0 if they are not Code attributes. Corresponds to the 'code_length' field of
    ///     the Code attribute. </param>
    /// <param name="maxStack"> the maximum stack size of the method corresponding to these Code attributes, or
    ///     -1 if they are not Code attributes. </param>
    /// <param name="maxLocals"> the maximum number of local variables of the method corresponding to these
    ///     Code attributes, or -1 if they are not Code attribute. </param>
    /// <param name="output"> where the attributes must be written. </param>
    public void PutAttributes(SymbolTable symbolTable, sbyte[] code, int codeLength, int maxStack, int maxLocals,
        ByteVector output)
    {
        ClassWriter classWriter = symbolTable.classWriter;
        Attribute attribute = this;
        while (attribute != null)
        {
            ByteVector attributeContent = attribute.MaybeWrite(classWriter, code, codeLength, maxStack, maxLocals);
            // Put attribute_name_index and attribute_length.
            output.PutShort(symbolTable.AddConstantUtf8(attribute.type)).PutInt(attributeContent.length);
            output.PutByteArray(attributeContent.data, 0, attributeContent.length);
            attribute = attribute.nextAttribute;
        }
    }

    /// <summary>
    /// Puts all the attributes that correspond to the given field, method or class access flags and
    /// signature, in the given byte vector. This includes the 6 header bytes (attribute_name_index and
    /// attribute_length) per attribute.
    /// </summary>
    /// <param name="symbolTable"> where the constants used in the attributes must be stored. </param>
    /// <param name="accessFlags"> some field, method or class access flags. </param>
    /// <param name="signatureIndex"> the constant pool index of a field, method of class signature. </param>
    /// <param name="output"> where the attributes must be written. </param>
    internal static void PutAttributes(SymbolTable symbolTable, int accessFlags, int signatureIndex,
        ByteVector output)
    {
        // Before Java 1.5, synthetic fields are represented with a Synthetic attribute.
        if ((accessFlags & Opcodes.Acc_Synthetic) != 0 && symbolTable.MajorVersion < Opcodes.V1_5)
        {
            output.PutShort(symbolTable.AddConstantUtf8(Constants.Synthetic)).PutInt(0);
        }

        if (signatureIndex != 0)
        {
            output.PutShort(symbolTable.AddConstantUtf8(Constants.Signature)).PutInt(2).PutShort(signatureIndex);
        }

        if ((accessFlags & Opcodes.Acc_Deprecated) != 0)
        {
            output.PutShort(symbolTable.AddConstantUtf8(Constants.Deprecated)).PutInt(0);
        }
    }

    /// <summary>
    /// A set of attribute prototypes (attributes with the same type are considered equal). </summary>
    internal sealed class Set
    {
        internal const int Size_Increment = 6;

        internal int size;
        internal Attribute[] data = new Attribute[Size_Increment];

        public void AddAttributes(Attribute attributeList)
        {
            Attribute attribute = attributeList;
            while (attribute != null)
            {
                if (!Contains(attribute))
                {
                    Add(attribute);
                }

                attribute = attribute.nextAttribute;
            }
        }

        public Attribute[] ToArray()
        {
            Attribute[] result = new Attribute[size];
            Array.Copy(data, 0, result, 0, size);
            return result;
        }

        public bool Contains(Attribute attribute)
        {
            for (int i = 0; i < size; ++i)
            {
                if (data[i].type.Equals(attribute.type))
                {
                    return true;
                }
            }

            return false;
        }

        public void Add(Attribute attribute)
        {
            if (size >= data.Length)
            {
                Attribute[] newData = new Attribute[data.Length + Size_Increment];
                Array.Copy(data, 0, newData, 0, size);
                data = newData;
            }

            data[size++] = attribute;
        }
    }
}