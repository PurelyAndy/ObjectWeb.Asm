using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
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
namespace ObjectWeb.Asm;

/// <summary>
/// A parser to make a <see cref="ClassVisitor"/> visit a ClassFile structure, as defined in the Java
/// Virtual Machine Specification (JVMS). This class parses the ClassFile content and calls the
/// appropriate visit methods of a given <see cref="ClassVisitor"/> for each field, method and bytecode
/// instruction encountered.
/// </summary>
/// <seealso cref="<a href="https://docs.oracle.com/javase/specs/jvms/se9/html/jvms-4.html">JVMS 4</a>
/// @author Eric Bruneton
/// @author Eugene Kuleshov </seealso>
public class ClassReader
{
    /// <summary>
    /// A flag to skip the Code attributes. If this flag is set the Code attributes are neither parsed
    /// nor visited.
    /// </summary>
    public const int Skip_Code = 1;

    /// <summary>
    /// A flag to skip the SourceFile, SourceDebugExtension, LocalVariableTable,
    /// LocalVariableTypeTable, LineNumberTable and MethodParameters attributes. If this flag is set
    /// these attributes are neither parsed nor visited (i.e. <see cref="ClassVisitor.VisitSource"/>, <see cref="MethodVisitor.visitLocalVariable"/>, <see cref="MethodVisitor.VisitLineNumber"/> and <see cref="MethodVisitor.visitParameter"/> are not called).
    /// </summary>
    public const int Skip_Debug = 2;

    /// <summary>
    /// A flag to skip the StackMap and StackMapTable attributes. If this flag is set these attributes
    /// are neither parsed nor visited (i.e. <see cref="MethodVisitor.VisitFrame"/> is not called). This flag
    /// is useful when the <see cref="ClassWriter.Compute_Frames"/> option is used: it avoids visiting frames
    /// that will be ignored and recomputed from scratch.
    /// </summary>
    public const int Skip_Frames = 4;

    /// <summary>
    /// A flag to expand the stack map frames. By default stack map frames are visited in their
    /// original format (i.e. "expanded" for classes whose version is less than V1_6, and "compressed"
    /// for the other classes). If this flag is set, stack map frames are always visited in expanded
    /// format (this option adds a decompression/compression step in ClassReader and ClassWriter which
    /// degrades performance quite a lot).
    /// </summary>
    public const int Expand_Frames = 8;

    /// <summary>
    /// A flag to expand the ASM specific instructions into an equivalent sequence of standard bytecode
    /// instructions. When resolving a forward jump it may happen that the signed 2 bytes offset
    /// reserved for it is not sufficient to store the bytecode offset. In this case the jump
    /// instruction is replaced with a temporary ASM specific instruction using an unsigned 2 bytes
    /// offset (see <see cref="Label.Resolve"/>). This internal flag is used to re-read classes containing
    /// such instructions, in order to replace them with standard instructions. In addition, when this
    /// flag is used, goto_w and jsr_w are <i>not</i> converted into goto and jsr, to make sure that
    /// infinite loops where a goto_w is replaced with a goto in ClassReader and converted back to a
    /// goto_w in ClassWriter cannot occur.
    /// </summary>
    internal const int Expand_Asm_Insns = 256;

    /// <summary>
    /// The maximum size of array to allocate. </summary>
    private const int MaxBufferSize = 1024 * 1024;

    /// <summary>
    /// The size of the temporary byte array used to read class input streams chunk by chunk. </summary>
    private const int InputStreamDataChunkSize = 4096;

    /// <summary>
    /// A byte array containing the JVMS ClassFile structure to be parsed.
    /// </summary>
    /// @deprecated Use <see cref="ReadByte"/> and the other read methods instead. This field will
    ///     eventually be deleted. 
    [Obsolete("Use <seealso cref=\"readByte(int)\"/> and the other read methods instead. This field will")]
    public readonly sbyte[] b;

    /// <summary>
    /// The offset in bytes of the ClassFile's access_flags field. </summary>
    public readonly int header;

    /// <summary>
    /// A byte array containing the JVMS ClassFile structure to be parsed. <i>The content of this array
    /// must not be modified. This field is intended for <see cref="Attribute"/> sub classes, and is normally
    /// not needed by class visitors.</i>
    /// 
    /// <para>NOTE: the ClassFile structure can start at any offset within this array, i.e. it does not
    /// necessarily start at offset 0. Use <see cref="GetItem"/> and <see cref="header"/> to get correct
    /// ClassFile element offsets within this byte array.
    /// </para>
    /// </summary>
    internal readonly sbyte[] classFileBuffer;

    /// <summary>
    /// The offset in bytes, in <see cref="classFileBuffer"/>, of each cp_info entry of the ClassFile's
    /// constant_pool array, <i>plus one</i>. In other words, the offset of constant pool entry i is
    /// given by cpInfoOffsets[i] - 1, i.e. its cp_info's tag field is given by b[cpInfoOffsets[i] -
    /// 1].
    /// </summary>
    private readonly int[] _cpInfoOffsets;

    /// <summary>
    /// The String objects corresponding to the CONSTANT_Utf8 constant pool items. This cache avoids
    /// multiple parsing of a given CONSTANT_Utf8 constant pool item.
    /// </summary>
    private readonly string[] _constantUtf8Values;

    /// <summary>
    /// The ConstantDynamic objects corresponding to the CONSTANT_Dynamic constant pool items. This
    /// cache avoids multiple parsing of a given CONSTANT_Dynamic constant pool item.
    /// </summary>
    private readonly ConstantDynamic[] _constantDynamicValues;

    /// <summary>
    /// The start offsets in <see cref="classFileBuffer"/> of each element of the bootstrap_methods array
    /// (in the BootstrapMethods attribute).
    /// </summary>
    /// <seealso cref="<a href="https://docs.oracle.com/javase/specs/jvms/se9/html/jvms-4.html#jvms-4.7.23">JVMS
    ///     4.7.23</a> </seealso>
    private readonly int[] _bootstrapMethodOffsets;

    /// <summary>
    /// A conservative estimate of the maximum length of the strings contained in the constant pool of
    /// the class.
    /// </summary>
    private readonly int _maxStringLength;

    // -----------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Constructs a new <see cref="ClassReader"/> object.
    /// </summary>
    /// <param name="classFile"> the JVMS ClassFile structure to be read. </param>
    public ClassReader(sbyte[] classFile) : this(classFile, 0, classFile.Length)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="ClassReader"/> object.
    /// </summary>
    /// <param name="classFileBuffer"> a byte array containing the JVMS ClassFile structure to be read. </param>
    /// <param name="classFileOffset"> the offset in byteBuffer of the first byte of the ClassFile to be read. </param>
    /// <param name="classFileLength"> the length in bytes of the ClassFile to be read. </param>
    public ClassReader(sbyte[] classFileBuffer, int classFileOffset, int classFileLength) : this(classFileBuffer,
        classFileOffset, true)
    {
        // NOPMD(UnusedFormalParameter) used for backward compatibility.
    }

    /// <summary>
    /// Constructs a new <see cref="ClassReader"/> object. <i>This internal constructor must not be exposed
    /// as a public API</i>.
    /// </summary>
    /// <param name="classFileBuffer"> a byte array containing the JVMS ClassFile structure to be read. </param>
    /// <param name="classFileOffset"> the offset in byteBuffer of the first byte of the ClassFile to be read. </param>
    /// <param name="checkClassVersion"> whether to check the class version or not. </param>
        
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ClassReader(sbyte[] classFileBuffer, int classFileOffset, bool checkClassVersion)
    {
        this.classFileBuffer = classFileBuffer;
        this.b = classFileBuffer;
        // Check the class' major_version. This field is after the magic and minor_version fields, which
        // use 4 and 2 bytes respectively.
        if (checkClassVersion && ReadShort(classFileOffset + 6) > Opcodes.V25)
        {
            throw new System.ArgumentException("Unsupported class file major version " +
                                               ReadShort(classFileOffset + 6));
        }

        // Create the constant pool arrays. The constant_pool_count field is after the magic,
        // minor_version and major_version fields, which use 4, 2 and 2 bytes respectively.
        int constantPoolCount = ReadUnsignedShort(classFileOffset + 8);
        _cpInfoOffsets = new int[constantPoolCount];
        _constantUtf8Values = new string[constantPoolCount];
        // Compute the offset of each constant pool entry, as well as a conservative estimate of the
        // maximum length of the constant pool strings. The first constant pool entry is after the
        // magic, minor_version, major_version and constant_pool_count fields, which use 4, 2, 2 and 2
        // bytes respectively.
        int currentCpInfoIndex = 1;
        int currentCpInfoOffset = classFileOffset + 10;
        int currentMaxStringLength = 0;
        bool hasBootstrapMethods = false;
        bool hasConstantDynamic = false;
        // The offset of the other entries depend on the total size of all the previous entries.
        while (currentCpInfoIndex < constantPoolCount)
        {
            _cpInfoOffsets[currentCpInfoIndex++] = currentCpInfoOffset + 1;
            int cpInfoSize;
            switch (classFileBuffer[currentCpInfoOffset])
            {
                case Symbol.Constant_Fieldref_Tag:
                case Symbol.Constant_Methodref_Tag:
                case Symbol.Constant_Interface_Methodref_Tag:
                case Symbol.Constant_Integer_Tag:
                case Symbol.Constant_Float_Tag:
                case Symbol.Constant_Name_And_Type_Tag:
                    cpInfoSize = 5;
                    break;
                case Symbol.Constant_Dynamic_Tag:
                    cpInfoSize = 5;
                    hasBootstrapMethods = true;
                    hasConstantDynamic = true;
                    break;
                case Symbol.Constant_Invoke_Dynamic_Tag:
                    cpInfoSize = 5;
                    hasBootstrapMethods = true;
                    break;
                case Symbol.Constant_Long_Tag:
                case Symbol.Constant_Double_Tag:
                    cpInfoSize = 9;
                    currentCpInfoIndex++;
                    break;
                case Symbol.Constant_Utf8_Tag:
                    cpInfoSize = 3 + ReadUnsignedShort(currentCpInfoOffset + 1);
                    if (cpInfoSize > currentMaxStringLength)
                    {
                        // The size in bytes of this CONSTANT_Utf8 structure provides a conservative estimate
                        // of the length in characters of the corresponding string, and is much cheaper to
                        // compute than this exact length.
                        currentMaxStringLength = cpInfoSize;
                    }

                    break;
                case Symbol.Constant_Method_Handle_Tag:
                    cpInfoSize = 4;
                    break;
                case Symbol.Constant_Class_Tag:
                case Symbol.Constant_String_Tag:
                case Symbol.Constant_Method_Type_Tag:
                case Symbol.Constant_Package_Tag:
                case Symbol.Constant_Module_Tag:
                    cpInfoSize = 3;
                    break;
                default:
                    throw new System.ArgumentException();
            }

            currentCpInfoOffset += cpInfoSize;
        }

        _maxStringLength = currentMaxStringLength;
        // The Classfile's access_flags field is just after the last constant pool entry.
        header = currentCpInfoOffset;

        // Allocate the cache of ConstantDynamic Values, if there is at least one.
        _constantDynamicValues = hasConstantDynamic ? new ConstantDynamic[constantPoolCount] : null;

        // Read the BootstrapMethods attribute, if any (only get the offset of each method).
        _bootstrapMethodOffsets =
            hasBootstrapMethods ? ReadBootstrapMethodsAttribute(currentMaxStringLength) : null;
    }

    /// <summary>
    /// Constructs a new <see cref="ClassReader"/> object.
    /// </summary>
    /// <param name="inputStream"> an input stream of the JVMS ClassFile structure to be read. This input
    ///     stream must contain nothing more than the ClassFile structure itself. It is read from its
    ///     current position to its end. </param>
    /// <exception cref="IOException"> if a problem occurs during reading. </exception>
    public ClassReader(Stream inputStream) : this(ReadStream(inputStream, false))
    {
    }

    /// <summary>
    /// Reads the given input stream and returns its content as a byte array.
    /// </summary>
    /// <param name="inputStream"> an input stream. </param>
    /// <param name="close"> true to close the input stream after reading. </param>
    /// <returns> the content of the given input stream. </returns>
    /// <exception cref="IOException"> if a problem occurs during reading. </exception>
    private static sbyte[] ReadStream(Stream inputStream, bool close)
    {
        if (inputStream == null)
        {
            throw new IOException("Class not found");
        }

        int bufferSize = CalculateBufferSize(inputStream);
        try
        {
            using (MemoryStream outputStream = new MemoryStream())
            {
                byte[] data = new byte[bufferSize];
                int bytesRead;
                int readCount = 0;
                while ((bytesRead = inputStream.Read(data, 0, bufferSize)) != 0)
                {
                    outputStream.Write(data, 0, bytesRead);
                    readCount++;
                }

                outputStream.Flush();
                if (readCount == 1)
                {
                    return Array.ConvertAll(data, b => unchecked((sbyte)b));
                }

                return Array.ConvertAll(outputStream.ToArray(), b => unchecked((sbyte)b));
            }
        }
        finally
        {
            if (close)
            {
                inputStream.Close();
            }
        }
    }

    private static int CalculateBufferSize(Stream inputStream)
    {
        long expectedLength = inputStream.Length;
        /*
         * Some implementations can return 0 while holding available data
         * (e.g. new FileInputStream("/proc/a_file"))
         * Also in some pathological cases a very small number might be returned,
         * and in this case we use default size
         */
        if (expectedLength < 256)
        {
            return InputStreamDataChunkSize;
        }

        return (int)Math.Min(expectedLength, MaxBufferSize);
    }

    // -----------------------------------------------------------------------------------------------
    // Accessors
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Returns the class's access flags (see <see cref="Opcodes"/>). This value may not reflect Deprecated
    /// and Synthetic flags when bytecode is before 1.5 and those flags are represented by attributes.
    /// </summary>
    /// <returns> the class access flags. </returns>
    /// <seealso cref="ClassVisitor#visit(int, int, String, String, String, String[]) </seealso>
    public virtual int Access => ReadUnsignedShort(header);

    /// <summary>
    /// Returns the internal name of the class (see <see cref="JType.InternalName"/>).
    /// </summary>
    /// <returns> the internal class name. </returns>
    /// <seealso cref="ClassVisitor#visit(int, int, String, String, String, String[]) </seealso>
    public virtual string ClassName =>
        // this_class is just after the access_flags field (using 2 bytes).
        ReadClass(header + 2, new char[_maxStringLength]);

    /// <summary>
    /// Returns the internal name of the super class (see <see cref="JType.InternalName"/>). For
    /// interfaces, the super class is <see cref="object"/>.
    /// </summary>
    /// <returns> the internal name of the super class, or null for <see cref="object"/> class. </returns>
    /// <seealso cref="ClassVisitor.Visit(int, int, String, String, String, String[])" />
    public virtual string SuperName =>
        // super_class is after the access_flags and this_class fields (2 bytes each).
        ReadClass(header + 4, new char[_maxStringLength]);

    /// <summary>
    /// Returns the internal names of the implemented interfaces (see <see cref="JType.InternalName"/>).
    /// </summary>
    /// <returns> the internal names of the directly implemented interfaces. Inherited implemented
    ///     interfaces are not returned. </returns>
    /// <seealso cref="ClassVisitor#visit(int, int, String, String, String, String[]) </seealso>
    public virtual string[] Interfaces
    {
        get
        {
            // interfaces_count is after the access_flags, this_class and super_class fields (2 bytes each).
            int currentOffset = header + 6;
            int interfacesCount = ReadUnsignedShort(currentOffset);
            string[] interfaces = new string[interfacesCount];
            if (interfacesCount > 0)
            {
                char[] charBuffer = new char[_maxStringLength];
                for (int i = 0; i < interfacesCount; ++i)
                {
                    currentOffset += 2;
                    interfaces[i] = ReadClass(currentOffset, charBuffer);
                }
            }

            return interfaces;
        }
    }

    // -----------------------------------------------------------------------------------------------
    // Public methods
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Makes the given visitor visit the JVMS ClassFile structure passed to the constructor of this
    /// <see cref="ClassReader"/>.
    /// </summary>
    /// <param name="classVisitor"> the visitor that must visit this class. </param>
    /// <param name="parsingOptions"> the options to use to parse this class. One or more of <see cref="Skip_Code"/>, <see cref="Skip_Debug"/>, <see cref="Skip_Frames"/> or <see cref="Expand_Frames"/>. </param>
    public virtual void Accept(ClassVisitor classVisitor, int parsingOptions)
    {
        Accept(classVisitor, new Attribute[0], parsingOptions);
    }

    /// <summary>
    /// Makes the given visitor visit the JVMS ClassFile structure passed to the constructor of this
    /// <see cref="ClassReader"/>.
    /// </summary>
    /// <param name="classVisitor"> the visitor that must visit this class. </param>
    /// <param name="attributePrototypes"> prototypes of the attributes that must be parsed during the visit of
    ///     the class. Any attribute whose type is not equal to the type of one the prototypes will not
    ///     be parsed: its byte array value will be passed unchanged to the ClassWriter. <i>This may
    ///     corrupt it if this value contains references to the constant pool, or has syntactic or
    ///     semantic links with a class element that has been transformed by a class adapter between
    ///     the reader and the writer</i>. </param>
    /// <param name="parsingOptions"> the options to use to parse this class. One or more of <see cref="Skip_Code"/>, <see cref="Skip_Debug"/>, <see cref="Skip_Frames"/> or <see cref="Expand_Frames"/>. </param>
    public virtual void Accept(ClassVisitor classVisitor, Attribute[] attributePrototypes, int parsingOptions)
    {
        Context context = new Context();
        context.attributePrototypes = attributePrototypes;
        context.parsingOptions = parsingOptions;
        context.charBuffer = new char[_maxStringLength];

        // Read the access_flags, this_class, super_class, interface_count and interfaces fields.
        char[] charBuffer = context.charBuffer;
        int currentOffset = header;
        int accessFlags = ReadUnsignedShort(currentOffset);
        string thisClass = ReadClass(currentOffset + 2, charBuffer);
        string superClass = ReadClass(currentOffset + 4, charBuffer);
        string[] interfaces = new string[ReadUnsignedShort(currentOffset + 6)];
        currentOffset += 8;
        for (int i = 0; i < interfaces.Length; ++i)
        {
            interfaces[i] = ReadClass(currentOffset, charBuffer);
            currentOffset += 2;
        }

        // Read the class attributes (the variables are ordered as in Section 4.7 of the JVMS).
        // Attribute offsets exclude the attribute_name_index and attribute_length fields.
        // - The offset of the InnerClasses attribute, or 0.
        int innerClassesOffset = 0;
        // - The offset of the EnclosingMethod attribute, or 0.
        int enclosingMethodOffset = 0;
        // - The string corresponding to the Signature attribute, or null.
        string signature = null;
        // - The string corresponding to the SourceFile attribute, or null.
        string sourceFile = null;
        // - The string corresponding to the SourceDebugExtension attribute, or null.
        string sourceDebugExtension = null;
        // - The offset of the RuntimeVisibleAnnotations attribute, or 0.
        int runtimeVisibleAnnotationsOffset = 0;
        // - The offset of the RuntimeInvisibleAnnotations attribute, or 0.
        int runtimeInvisibleAnnotationsOffset = 0;
        // - The offset of the RuntimeVisibleTypeAnnotations attribute, or 0.
        int runtimeVisibleTypeAnnotationsOffset = 0;
        // - The offset of the RuntimeInvisibleTypeAnnotations attribute, or 0.
        int runtimeInvisibleTypeAnnotationsOffset = 0;
        // - The offset of the Module attribute, or 0.
        int moduleOffset = 0;
        // - The offset of the ModulePackages attribute, or 0.
        int modulePackagesOffset = 0;
        // - The string corresponding to the ModuleMainClass attribute, or null.
        string moduleMainClass = null;
        // - The string corresponding to the NestHost attribute, or null.
        string nestHostClass = null;
        // - The offset of the NestMembers attribute, or 0.
        int nestMembersOffset = 0;
        // - The offset of the PermittedSubclasses attribute, or 0
        int permittedSubclassesOffset = 0;
        // - The offset of the Record attribute, or 0.
        int recordOffset = 0;
        // - The non standard attributes (linked with their <see>Attribute#nextAttribute</see> field).
        //   This list in the <i>reverse order</i> or their order in the ClassFile structure.
        Attribute attributes = null;

        int currentAttributeOffset = FirstAttributeOffset;
        for (int i = ReadUnsignedShort(currentAttributeOffset - 2); i > 0; --i)
        {
            // Read the attribute_info's attribute_name and attribute_length fields.
            string attributeName = ReadUtf8(currentAttributeOffset, charBuffer);
            int attributeLength = ReadInt(currentAttributeOffset + 2);
            currentAttributeOffset += 6;
            // The tests are sorted in decreasing frequency order (based on frequencies observed on
            // typical classes).
            if (Constants.Source_File.Equals(attributeName))
            {
                sourceFile = ReadUtf8(currentAttributeOffset, charBuffer);
            }
            else if (Constants.Inner_Classes.Equals(attributeName))
            {
                innerClassesOffset = currentAttributeOffset;
            }
            else if (Constants.Enclosing_Method.Equals(attributeName))
            {
                enclosingMethodOffset = currentAttributeOffset;
            }
            else if (Constants.Nest_Host.Equals(attributeName))
            {
                nestHostClass = ReadClass(currentAttributeOffset, charBuffer);
            }
            else if (Constants.Nest_Members.Equals(attributeName))
            {
                nestMembersOffset = currentAttributeOffset;
            }
            else if (Constants.Permitted_Subclasses.Equals(attributeName))
            {
                permittedSubclassesOffset = currentAttributeOffset;
            }
            else if (Constants.Signature.Equals(attributeName))
            {
                signature = ReadUtf8(currentAttributeOffset, charBuffer);
            }
            else if (Constants.Runtime_Visible_Annotations.Equals(attributeName))
            {
                runtimeVisibleAnnotationsOffset = currentAttributeOffset;
            }
            else if (Constants.Runtime_Visible_Type_Annotations.Equals(attributeName))
            {
                runtimeVisibleTypeAnnotationsOffset = currentAttributeOffset;
            }
            else if (Constants.Deprecated.Equals(attributeName))
            {
                accessFlags |= Opcodes.Acc_Deprecated;
            }
            else if (Constants.Synthetic.Equals(attributeName))
            {
                accessFlags |= Opcodes.Acc_Synthetic;
            }
            else if (Constants.Source_Debug_Extension.Equals(attributeName))
            {
                if (attributeLength > classFileBuffer.Length - currentAttributeOffset)
                {
                    throw new System.ArgumentException();
                }

                sourceDebugExtension = ReadUtf(currentAttributeOffset, attributeLength, new char[attributeLength]);
            }
            else if (Constants.Runtime_Invisible_Annotations.Equals(attributeName))
            {
                runtimeInvisibleAnnotationsOffset = currentAttributeOffset;
            }
            else if (Constants.Runtime_Invisible_Type_Annotations.Equals(attributeName))
            {
                runtimeInvisibleTypeAnnotationsOffset = currentAttributeOffset;
            }
            else if (Constants.Record.Equals(attributeName))
            {
                recordOffset = currentAttributeOffset;
                accessFlags |= Opcodes.Acc_Record;
            }
            else if (Constants.Module.Equals(attributeName))
            {
                moduleOffset = currentAttributeOffset;
            }
            else if (Constants.Module_Main_Class.Equals(attributeName))
            {
                moduleMainClass = ReadClass(currentAttributeOffset, charBuffer);
            }
            else if (Constants.Module_Packages.Equals(attributeName))
            {
                modulePackagesOffset = currentAttributeOffset;
            }
            else if (!Constants.Bootstrap_Methods.Equals(attributeName))
            {
                // The BootstrapMethods attribute is read in the constructor.
                Attribute attribute = ReadAttribute(attributePrototypes, attributeName, currentAttributeOffset,
                    attributeLength, charBuffer, -1, null);
                attribute.nextAttribute = attributes;
                attributes = attribute;
            }

            currentAttributeOffset += attributeLength;
        }

        // Visit the class declaration. The minor_version and major_version fields start 6 bytes before
        // the first constant pool entry, which itself starts at cpInfoOffsets[1] - 1 (by definition).
        classVisitor.Visit(ReadInt(_cpInfoOffsets[1] - 7), accessFlags, thisClass, signature, superClass,
            interfaces);

        // Visit the SourceFile and SourceDebugExtenstion attributes.
        if ((parsingOptions & Skip_Debug) == 0 && (!string.ReferenceEquals(sourceFile, null) ||
                                                   !string.ReferenceEquals(sourceDebugExtension, null)))
        {
            classVisitor.VisitSource(sourceFile, sourceDebugExtension);
        }

        // Visit the Module, ModulePackages and ModuleMainClass attributes.
        if (moduleOffset != 0)
        {
            ReadModuleAttributes(classVisitor, context, moduleOffset, modulePackagesOffset, moduleMainClass);
        }

        // Visit the NestHost attribute.
        if (!string.ReferenceEquals(nestHostClass, null))
        {
            classVisitor.VisitNestHost(nestHostClass);
        }

        // Visit the EnclosingMethod attribute.
        if (enclosingMethodOffset != 0)
        {
            string className = ReadClass(enclosingMethodOffset, charBuffer);
            int methodIndex = ReadUnsignedShort(enclosingMethodOffset + 2);
            string name = methodIndex == 0 ? null : ReadUtf8(_cpInfoOffsets[methodIndex], charBuffer);
            string type = methodIndex == 0 ? null : ReadUtf8(_cpInfoOffsets[methodIndex] + 2, charBuffer);
            classVisitor.VisitOuterClass(className, name, type);
        }

        // Visit the RuntimeVisibleAnnotations attribute.
        if (runtimeVisibleAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeVisibleAnnotationsOffset);
            int currentAnnotationOffset = runtimeVisibleAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset =
                    ReadElementValues(classVisitor.VisitAnnotation(annotationDescriptor, true),
                        currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeInvisibleAnnotations attribute.
        if (runtimeInvisibleAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeInvisibleAnnotationsOffset);
            int currentAnnotationOffset = runtimeInvisibleAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset =
                    ReadElementValues(classVisitor.VisitAnnotation(annotationDescriptor, false),
                        currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeVisibleTypeAnnotations attribute.
        if (runtimeVisibleTypeAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeVisibleTypeAnnotationsOffset);
            int currentAnnotationOffset = runtimeVisibleTypeAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the target_type, target_info and target_path fields.
                currentAnnotationOffset = ReadTypeAnnotationTarget(context, currentAnnotationOffset);
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset = ReadElementValues(
                    classVisitor.VisitTypeAnnotation(context.currentTypeAnnotationTarget,
                        context.currentTypeAnnotationTargetPath, annotationDescriptor, true),
                    currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeInvisibleTypeAnnotations attribute.
        if (runtimeInvisibleTypeAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeInvisibleTypeAnnotationsOffset);
            int currentAnnotationOffset = runtimeInvisibleTypeAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the target_type, target_info and target_path fields.
                currentAnnotationOffset = ReadTypeAnnotationTarget(context, currentAnnotationOffset);
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset = ReadElementValues(
                    classVisitor.VisitTypeAnnotation(context.currentTypeAnnotationTarget,
                        context.currentTypeAnnotationTargetPath, annotationDescriptor, false),
                    currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the non standard attributes.
        while (attributes != null)
        {
            // Copy and reset the nextAttribute field so that it can also be used in ClassWriter.
            Attribute nextAttribute = attributes.nextAttribute;
            attributes.nextAttribute = null;
            classVisitor.VisitAttribute(attributes);
            attributes = nextAttribute;
        }

        // Visit the NestedMembers attribute.
        if (nestMembersOffset != 0)
        {
            int numberOfNestMembers = ReadUnsignedShort(nestMembersOffset);
            int currentNestMemberOffset = nestMembersOffset + 2;
            while (numberOfNestMembers-- > 0)
            {
                classVisitor.VisitNestMember(ReadClass(currentNestMemberOffset, charBuffer));
                currentNestMemberOffset += 2;
            }
        }

        // Visit the PermittedSubclasses attribute.
        if (permittedSubclassesOffset != 0)
        {
            int numberOfPermittedSubclasses = ReadUnsignedShort(permittedSubclassesOffset);
            int currentPermittedSubclassesOffset = permittedSubclassesOffset + 2;
            while (numberOfPermittedSubclasses-- > 0)
            {
                classVisitor.VisitPermittedSubclass(ReadClass(currentPermittedSubclassesOffset, charBuffer));
                currentPermittedSubclassesOffset += 2;
            }
        }

        // Visit the InnerClasses attribute.
        if (innerClassesOffset != 0)
        {
            int numberOfClasses = ReadUnsignedShort(innerClassesOffset);
            int currentClassesOffset = innerClassesOffset + 2;
            while (numberOfClasses-- > 0)
            {
                classVisitor.VisitInnerClass(ReadClass(currentClassesOffset, charBuffer),
                    ReadClass(currentClassesOffset + 2, charBuffer), ReadUtf8(currentClassesOffset + 4, charBuffer),
                    ReadUnsignedShort(currentClassesOffset + 6));
                currentClassesOffset += 8;
            }
        }

        // Visit Record components.
        if (recordOffset != 0)
        {
            int recordComponentsCount = ReadUnsignedShort(recordOffset);
            recordOffset += 2;
            while (recordComponentsCount-- > 0)
            {
                recordOffset = ReadRecordComponent(classVisitor, context, recordOffset);
            }
        }

        // Visit the fields and methods.
        int fieldsCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (fieldsCount-- > 0)
        {
            currentOffset = ReadField(classVisitor, context, currentOffset);
        }

        int methodsCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (methodsCount-- > 0)
        {
            currentOffset = ReadMethod(classVisitor, context, currentOffset);
        }

        // Visit the end of the class.
        classVisitor.VisitEnd();
    }

    // ----------------------------------------------------------------------------------------------
    // Methods to parse modules, fields and methods
    // ----------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads the Module, ModulePackages and ModuleMainClass attributes and visit them.
    /// </summary>
    /// <param name="classVisitor"> the current class visitor </param>
    /// <param name="context"> information about the class being parsed. </param>
    /// <param name="moduleOffset"> the offset of the Module attribute (excluding the attribute_info's
    ///     attribute_name_index and attribute_length fields). </param>
    /// <param name="modulePackagesOffset"> the offset of the ModulePackages attribute (excluding the
    ///     attribute_info's attribute_name_index and attribute_length fields), or 0. </param>
    /// <param name="moduleMainClass"> the string corresponding to the ModuleMainClass attribute, or <c>null</c>. </param>
    private void ReadModuleAttributes(ClassVisitor classVisitor, Context context, int moduleOffset,
        int modulePackagesOffset, string moduleMainClass)
    {
        char[] buffer = context.charBuffer;

        // Read the module_name_index, module_flags and module_version_index fields and visit them.
        int currentOffset = moduleOffset;
        string moduleName = ReadModule(currentOffset, buffer);
        int moduleFlags = ReadUnsignedShort(currentOffset + 2);
        string moduleVersion = ReadUtf8(currentOffset + 4, buffer);
        currentOffset += 6;
        ModuleVisitor moduleVisitor = classVisitor.VisitModule(moduleName, moduleFlags, moduleVersion);
        if (moduleVisitor == null)
        {
            return;
        }

        // Visit the ModuleMainClass attribute.
        if (!string.ReferenceEquals(moduleMainClass, null))
        {
            moduleVisitor.VisitMainClass(moduleMainClass);
        }

        // Visit the ModulePackages attribute.
        if (modulePackagesOffset != 0)
        {
            int packageCount = ReadUnsignedShort(modulePackagesOffset);
            int currentPackageOffset = modulePackagesOffset + 2;
            while (packageCount-- > 0)
            {
                moduleVisitor.VisitPackage(ReadPackage(currentPackageOffset, buffer));
                currentPackageOffset += 2;
            }
        }

        // Read the 'requires_count' and 'requires' fields.
        int requiresCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (requiresCount-- > 0)
        {
            // Read the requires_index, requires_flags and requires_version fields and visit them.
            string requires = ReadModule(currentOffset, buffer);
            int requiresFlags = ReadUnsignedShort(currentOffset + 2);
            string requiresVersion = ReadUtf8(currentOffset + 4, buffer);
            currentOffset += 6;
            moduleVisitor.VisitRequire(requires, requiresFlags, requiresVersion);
        }

        // Read the 'exports_count' and 'exports' fields.
        int exportsCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (exportsCount-- > 0)
        {
            // Read the exports_index, exports_flags, exports_to_count and exports_to_index fields
            // and visit them.
            string exports = ReadPackage(currentOffset, buffer);
            int exportsFlags = ReadUnsignedShort(currentOffset + 2);
            int exportsToCount = ReadUnsignedShort(currentOffset + 4);
            currentOffset += 6;
            string[] exportsTo = null;
            if (exportsToCount != 0)
            {
                exportsTo = new string[exportsToCount];
                for (int i = 0; i < exportsToCount; ++i)
                {
                    exportsTo[i] = ReadModule(currentOffset, buffer);
                    currentOffset += 2;
                }
            }

            moduleVisitor.VisitExport(exports, exportsFlags, exportsTo);
        }

        // Reads the 'opens_count' and 'opens' fields.
        int opensCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (opensCount-- > 0)
        {
            // Read the opens_index, opens_flags, opens_to_count and opens_to_index fields and visit them.
            string opens = ReadPackage(currentOffset, buffer);
            int opensFlags = ReadUnsignedShort(currentOffset + 2);
            int opensToCount = ReadUnsignedShort(currentOffset + 4);
            currentOffset += 6;
            string[] opensTo = null;
            if (opensToCount != 0)
            {
                opensTo = new string[opensToCount];
                for (int i = 0; i < opensToCount; ++i)
                {
                    opensTo[i] = ReadModule(currentOffset, buffer);
                    currentOffset += 2;
                }
            }

            moduleVisitor.VisitOpen(opens, opensFlags, opensTo);
        }

        // Read the 'uses_count' and 'uses' fields.
        int usesCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (usesCount-- > 0)
        {
            moduleVisitor.VisitUse(ReadClass(currentOffset, buffer));
            currentOffset += 2;
        }

        // Read the  'provides_count' and 'provides' fields.
        int providesCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (providesCount-- > 0)
        {
            // Read the provides_index, provides_with_count and provides_with_index fields and visit them.
            string provides = ReadClass(currentOffset, buffer);
            int providesWithCount = ReadUnsignedShort(currentOffset + 2);
            currentOffset += 4;
            string[] providesWith = new string[providesWithCount];
            for (int i = 0; i < providesWithCount; ++i)
            {
                providesWith[i] = ReadClass(currentOffset, buffer);
                currentOffset += 2;
            }

            moduleVisitor.VisitProvide(provides, providesWith);
        }

        // Visit the end of the module attributes.
        moduleVisitor.VisitEnd();
    }

    /// <summary>
    /// Reads a record component and visit it.
    /// </summary>
    /// <param name="classVisitor"> the current class visitor </param>
    /// <param name="context"> information about the class being parsed. </param>
    /// <param name="recordComponentOffset"> the offset of the current record component. </param>
    /// <returns> the offset of the first byte following the record component. </returns>
    private int ReadRecordComponent(ClassVisitor classVisitor, Context context, int recordComponentOffset)
    {
        char[] charBuffer = context.charBuffer;

        int currentOffset = recordComponentOffset;
        string name = ReadUtf8(currentOffset, charBuffer);
        string descriptor = ReadUtf8(currentOffset + 2, charBuffer);
        currentOffset += 4;

        // Read the record component attributes (the variables are ordered as in Section 4.7 of the
        // JVMS).

        // Attribute offsets exclude the attribute_name_index and attribute_length fields.
        // - The string corresponding to the Signature attribute, or null.
        string signature = null;
        // - The offset of the RuntimeVisibleAnnotations attribute, or 0.
        int runtimeVisibleAnnotationsOffset = 0;
        // - The offset of the RuntimeInvisibleAnnotations attribute, or 0.
        int runtimeInvisibleAnnotationsOffset = 0;
        // - The offset of the RuntimeVisibleTypeAnnotations attribute, or 0.
        int runtimeVisibleTypeAnnotationsOffset = 0;
        // - The offset of the RuntimeInvisibleTypeAnnotations attribute, or 0.
        int runtimeInvisibleTypeAnnotationsOffset = 0;
        // - The non standard attributes (linked with their <see>Attribute#nextAttribute</see> field).
        //   This list in the <i>reverse order</i> or their order in the ClassFile structure.
        Attribute attributes = null;

        int attributesCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (attributesCount-- > 0)
        {
            // Read the attribute_info's attribute_name and attribute_length fields.
            string attributeName = ReadUtf8(currentOffset, charBuffer);
            int attributeLength = ReadInt(currentOffset + 2);
            currentOffset += 6;
            // The tests are sorted in decreasing frequency order (based on frequencies observed on
            // typical classes).
            if (Constants.Signature.Equals(attributeName))
            {
                signature = ReadUtf8(currentOffset, charBuffer);
            }
            else if (Constants.Runtime_Visible_Annotations.Equals(attributeName))
            {
                runtimeVisibleAnnotationsOffset = currentOffset;
            }
            else if (Constants.Runtime_Visible_Type_Annotations.Equals(attributeName))
            {
                runtimeVisibleTypeAnnotationsOffset = currentOffset;
            }
            else if (Constants.Runtime_Invisible_Annotations.Equals(attributeName))
            {
                runtimeInvisibleAnnotationsOffset = currentOffset;
            }
            else if (Constants.Runtime_Invisible_Type_Annotations.Equals(attributeName))
            {
                runtimeInvisibleTypeAnnotationsOffset = currentOffset;
            }
            else
            {
                Attribute attribute = ReadAttribute(context.attributePrototypes, attributeName, currentOffset,
                    attributeLength, charBuffer, -1, null);
                attribute.nextAttribute = attributes;
                attributes = attribute;
            }

            currentOffset += attributeLength;
        }

        RecordComponentVisitor recordComponentVisitor = classVisitor.VisitRecordComponent(name, descriptor, signature);
        if (recordComponentVisitor == null)
        {
            return currentOffset;
        }

        // Visit the RuntimeVisibleAnnotations attribute.
        if (runtimeVisibleAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeVisibleAnnotationsOffset);
            int currentAnnotationOffset = runtimeVisibleAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset =
                    ReadElementValues(recordComponentVisitor.VisitAnnotation(annotationDescriptor, true),
                        currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeInvisibleAnnotations attribute.
        if (runtimeInvisibleAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeInvisibleAnnotationsOffset);
            int currentAnnotationOffset = runtimeInvisibleAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset =
                    ReadElementValues(recordComponentVisitor.VisitAnnotation(annotationDescriptor, false),
                        currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeVisibleTypeAnnotations attribute.
        if (runtimeVisibleTypeAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeVisibleTypeAnnotationsOffset);
            int currentAnnotationOffset = runtimeVisibleTypeAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the target_type, target_info and target_path fields.
                currentAnnotationOffset = ReadTypeAnnotationTarget(context, currentAnnotationOffset);
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset = ReadElementValues(
                    recordComponentVisitor.VisitTypeAnnotation(context.currentTypeAnnotationTarget,
                        context.currentTypeAnnotationTargetPath, annotationDescriptor, true),
                    currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeInvisibleTypeAnnotations attribute.
        if (runtimeInvisibleTypeAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeInvisibleTypeAnnotationsOffset);
            int currentAnnotationOffset = runtimeInvisibleTypeAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the target_type, target_info and target_path fields.
                currentAnnotationOffset = ReadTypeAnnotationTarget(context, currentAnnotationOffset);
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset = ReadElementValues(
                    recordComponentVisitor.VisitTypeAnnotation(context.currentTypeAnnotationTarget,
                        context.currentTypeAnnotationTargetPath, annotationDescriptor, false),
                    currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the non standard attributes.
        while (attributes != null)
        {
            // Copy and reset the nextAttribute field so that it can also be used in FieldWriter.
            Attribute nextAttribute = attributes.nextAttribute;
            attributes.nextAttribute = null;
            recordComponentVisitor.VisitAttribute(attributes);
            attributes = nextAttribute;
        }

        // Visit the end of the field.
        recordComponentVisitor.VisitEnd();
        return currentOffset;
    }

    /// <summary>
    /// Reads a JVMS field_info structure and makes the given visitor visit it.
    /// </summary>
    /// <param name="classVisitor"> the visitor that must visit the field. </param>
    /// <param name="context"> information about the class being parsed. </param>
    /// <param name="fieldInfoOffset"> the start offset of the field_info structure. </param>
    /// <returns> the offset of the first byte following the field_info structure. </returns>
    private int ReadField(ClassVisitor classVisitor, Context context, int fieldInfoOffset)
    {
        char[] charBuffer = context.charBuffer;

        // Read the access_flags, name_index and descriptor_index fields.
        int currentOffset = fieldInfoOffset;
        int accessFlags = ReadUnsignedShort(currentOffset);
        string name = ReadUtf8(currentOffset + 2, charBuffer);
        string descriptor = ReadUtf8(currentOffset + 4, charBuffer);
        currentOffset += 6;

        // Read the field attributes (the variables are ordered as in Section 4.7 of the JVMS).
        // Attribute offsets exclude the attribute_name_index and attribute_length fields.
        // - The value corresponding to the ConstantValue attribute, or null.
        object constantValue = null;
        // - The string corresponding to the Signature attribute, or null.
        string signature = null;
        // - The offset of the RuntimeVisibleAnnotations attribute, or 0.
        int runtimeVisibleAnnotationsOffset = 0;
        // - The offset of the RuntimeInvisibleAnnotations attribute, or 0.
        int runtimeInvisibleAnnotationsOffset = 0;
        // - The offset of the RuntimeVisibleTypeAnnotations attribute, or 0.
        int runtimeVisibleTypeAnnotationsOffset = 0;
        // - The offset of the RuntimeInvisibleTypeAnnotations attribute, or 0.
        int runtimeInvisibleTypeAnnotationsOffset = 0;
        // - The non standard attributes (linked with their <see>Attribute#nextAttribute</see> field).
        //   This list in the <i>reverse order</i> or their order in the ClassFile structure.
        Attribute attributes = null;

        int attributesCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (attributesCount-- > 0)
        {
            // Read the attribute_info's attribute_name and attribute_length fields.
            string attributeName = ReadUtf8(currentOffset, charBuffer);
            int attributeLength = ReadInt(currentOffset + 2);
            currentOffset += 6;
            // The tests are sorted in decreasing frequency order (based on frequencies observed on
            // typical classes).
            if (Constants.Constant_Value.Equals(attributeName))
            {
                int constantvalueIndex = ReadUnsignedShort(currentOffset);
                constantValue = constantvalueIndex == 0 ? null : ReadConst(constantvalueIndex, charBuffer);
            }
            else if (Constants.Signature.Equals(attributeName))
            {
                signature = ReadUtf8(currentOffset, charBuffer);
            }
            else if (Constants.Deprecated.Equals(attributeName))
            {
                accessFlags |= Opcodes.Acc_Deprecated;
            }
            else if (Constants.Synthetic.Equals(attributeName))
            {
                accessFlags |= Opcodes.Acc_Synthetic;
            }
            else if (Constants.Runtime_Visible_Annotations.Equals(attributeName))
            {
                runtimeVisibleAnnotationsOffset = currentOffset;
            }
            else if (Constants.Runtime_Visible_Type_Annotations.Equals(attributeName))
            {
                runtimeVisibleTypeAnnotationsOffset = currentOffset;
            }
            else if (Constants.Runtime_Invisible_Annotations.Equals(attributeName))
            {
                runtimeInvisibleAnnotationsOffset = currentOffset;
            }
            else if (Constants.Runtime_Invisible_Type_Annotations.Equals(attributeName))
            {
                runtimeInvisibleTypeAnnotationsOffset = currentOffset;
            }
            else
            {
                Attribute attribute = ReadAttribute(context.attributePrototypes, attributeName, currentOffset,
                    attributeLength, charBuffer, -1, null);
                attribute.nextAttribute = attributes;
                attributes = attribute;
            }

            currentOffset += attributeLength;
        }

        // Visit the field declaration.
        FieldVisitor fieldVisitor = classVisitor.VisitField(accessFlags, name, descriptor, signature, constantValue);
        if (fieldVisitor == null)
        {
            return currentOffset;
        }

        // Visit the RuntimeVisibleAnnotations attribute.
        if (runtimeVisibleAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeVisibleAnnotationsOffset);
            int currentAnnotationOffset = runtimeVisibleAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset =
                    ReadElementValues(fieldVisitor.VisitAnnotation(annotationDescriptor, true),
                        currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeInvisibleAnnotations attribute.
        if (runtimeInvisibleAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeInvisibleAnnotationsOffset);
            int currentAnnotationOffset = runtimeInvisibleAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset =
                    ReadElementValues(fieldVisitor.VisitAnnotation(annotationDescriptor, false),
                        currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeVisibleTypeAnnotations attribute.
        if (runtimeVisibleTypeAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeVisibleTypeAnnotationsOffset);
            int currentAnnotationOffset = runtimeVisibleTypeAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the target_type, target_info and target_path fields.
                currentAnnotationOffset = ReadTypeAnnotationTarget(context, currentAnnotationOffset);
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset = ReadElementValues(
                    fieldVisitor.VisitTypeAnnotation(context.currentTypeAnnotationTarget,
                        context.currentTypeAnnotationTargetPath, annotationDescriptor, true),
                    currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeInvisibleTypeAnnotations attribute.
        if (runtimeInvisibleTypeAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeInvisibleTypeAnnotationsOffset);
            int currentAnnotationOffset = runtimeInvisibleTypeAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the target_type, target_info and target_path fields.
                currentAnnotationOffset = ReadTypeAnnotationTarget(context, currentAnnotationOffset);
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset = ReadElementValues(
                    fieldVisitor.VisitTypeAnnotation(context.currentTypeAnnotationTarget,
                        context.currentTypeAnnotationTargetPath, annotationDescriptor, false),
                    currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the non standard attributes.
        while (attributes != null)
        {
            // Copy and reset the nextAttribute field so that it can also be used in FieldWriter.
            Attribute nextAttribute = attributes.nextAttribute;
            attributes.nextAttribute = null;
            fieldVisitor.VisitAttribute(attributes);
            attributes = nextAttribute;
        }

        // Visit the end of the field.
        fieldVisitor.VisitEnd();
        return currentOffset;
    }

    /// <summary>
    /// Reads a JVMS method_info structure and makes the given visitor visit it.
    /// </summary>
    /// <param name="classVisitor"> the visitor that must visit the method. </param>
    /// <param name="context"> information about the class being parsed. </param>
    /// <param name="methodInfoOffset"> the start offset of the method_info structure. </param>
    /// <returns> the offset of the first byte following the method_info structure. </returns>
    private int ReadMethod(ClassVisitor classVisitor, Context context, int methodInfoOffset)
    {
        char[] charBuffer = context.charBuffer;

        // Read the access_flags, name_index and descriptor_index fields.
        int currentOffset = methodInfoOffset;
        context.currentMethodAccessFlags = ReadUnsignedShort(currentOffset);
        context.currentMethodName = ReadUtf8(currentOffset + 2, charBuffer);
        context.currentMethodDescriptor = ReadUtf8(currentOffset + 4, charBuffer);
        currentOffset += 6;

        // Read the method attributes (the variables are ordered as in Section 4.7 of the JVMS).
        // Attribute offsets exclude the attribute_name_index and attribute_length fields.
        // - The offset of the Code attribute, or 0.
        int codeOffset = 0;
        // - The offset of the Exceptions attribute, or 0.
        int exceptionsOffset = 0;
        // - The strings corresponding to the Exceptions attribute, or null.
        string[] exceptions = null;
        // - Whether the method has a Synthetic attribute.
        bool synthetic = false;
        // - The constant pool index contained in the Signature attribute, or 0.
        int signatureIndex = 0;
        // - The offset of the RuntimeVisibleAnnotations attribute, or 0.
        int runtimeVisibleAnnotationsOffset = 0;
        // - The offset of the RuntimeInvisibleAnnotations attribute, or 0.
        int runtimeInvisibleAnnotationsOffset = 0;
        // - The offset of the RuntimeVisibleParameterAnnotations attribute, or 0.
        int runtimeVisibleParameterAnnotationsOffset = 0;
        // - The offset of the RuntimeInvisibleParameterAnnotations attribute, or 0.
        int runtimeInvisibleParameterAnnotationsOffset = 0;
        // - The offset of the RuntimeVisibleTypeAnnotations attribute, or 0.
        int runtimeVisibleTypeAnnotationsOffset = 0;
        // - The offset of the RuntimeInvisibleTypeAnnotations attribute, or 0.
        int runtimeInvisibleTypeAnnotationsOffset = 0;
        // - The offset of the AnnotationDefault attribute, or 0.
        int annotationDefaultOffset = 0;
        // - The offset of the MethodParameters attribute, or 0.
        int methodParametersOffset = 0;
        // - The non standard attributes (linked with their <see>Attribute#nextAttribute</see> field).
        //   This list in the <i>reverse order</i> or their order in the ClassFile structure.
        Attribute attributes = null;

        int attributesCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (attributesCount-- > 0)
        {
            // Read the attribute_info's attribute_name and attribute_length fields.
            string attributeName = ReadUtf8(currentOffset, charBuffer);
            int attributeLength = ReadInt(currentOffset + 2);
            currentOffset += 6;
            // The tests are sorted in decreasing frequency order (based on frequencies observed on
            // typical classes).
            if (Constants.Code.Equals(attributeName))
            {
                if ((context.parsingOptions & Skip_Code) == 0)
                {
                    codeOffset = currentOffset;
                }
            }
            else if (Constants.Exceptions.Equals(attributeName))
            {
                exceptionsOffset = currentOffset;
                exceptions = new string[ReadUnsignedShort(exceptionsOffset)];
                int currentExceptionOffset = exceptionsOffset + 2;
                for (int i = 0; i < exceptions.Length; ++i)
                {
                    exceptions[i] = ReadClass(currentExceptionOffset, charBuffer);
                    currentExceptionOffset += 2;
                }
            }
            else if (Constants.Signature.Equals(attributeName))
            {
                signatureIndex = ReadUnsignedShort(currentOffset);
            }
            else if (Constants.Deprecated.Equals(attributeName))
            {
                context.currentMethodAccessFlags |= Opcodes.Acc_Deprecated;
            }
            else if (Constants.Runtime_Visible_Annotations.Equals(attributeName))
            {
                runtimeVisibleAnnotationsOffset = currentOffset;
            }
            else if (Constants.Runtime_Visible_Type_Annotations.Equals(attributeName))
            {
                runtimeVisibleTypeAnnotationsOffset = currentOffset;
            }
            else if (Constants.Annotation_Default.Equals(attributeName))
            {
                annotationDefaultOffset = currentOffset;
            }
            else if (Constants.Synthetic.Equals(attributeName))
            {
                synthetic = true;
                context.currentMethodAccessFlags |= Opcodes.Acc_Synthetic;
            }
            else if (Constants.Runtime_Invisible_Annotations.Equals(attributeName))
            {
                runtimeInvisibleAnnotationsOffset = currentOffset;
            }
            else if (Constants.Runtime_Invisible_Type_Annotations.Equals(attributeName))
            {
                runtimeInvisibleTypeAnnotationsOffset = currentOffset;
            }
            else if (Constants.Runtime_Visible_Parameter_Annotations.Equals(attributeName))
            {
                runtimeVisibleParameterAnnotationsOffset = currentOffset;
            }
            else if (Constants.Runtime_Invisible_Parameter_Annotations.Equals(attributeName))
            {
                runtimeInvisibleParameterAnnotationsOffset = currentOffset;
            }
            else if (Constants.Method_Parameters.Equals(attributeName))
            {
                methodParametersOffset = currentOffset;
            }
            else
            {
                Attribute attribute = ReadAttribute(context.attributePrototypes, attributeName, currentOffset,
                    attributeLength, charBuffer, -1, null);
                attribute.nextAttribute = attributes;
                attributes = attribute;
            }

            currentOffset += attributeLength;
        }

        // Visit the method declaration.
        MethodVisitor methodVisitor = classVisitor.VisitMethod(context.currentMethodAccessFlags, context.currentMethodName,
            context.currentMethodDescriptor, signatureIndex == 0 ? null : ReadUtf(signatureIndex, charBuffer),
            exceptions);
        if (methodVisitor == null)
        {
            return currentOffset;
        }

        // If the returned MethodVisitor is in fact a MethodWriter, it means there is no method
        // adapter between the reader and the writer. In this case, it might be possible to copy
        // the method attributes directly into the writer. If so, return early without visiting
        // the content of these attributes.
        if (methodVisitor is MethodWriter)
        {
            MethodWriter methodWriter = (MethodWriter)methodVisitor;
            if (methodWriter.CanCopyMethodAttributes(this, synthetic,
                    (context.currentMethodAccessFlags & Opcodes.Acc_Deprecated) != 0,
                    ReadUnsignedShort(methodInfoOffset + 4), signatureIndex, exceptionsOffset))
            {
                methodWriter.SetMethodAttributesSource(methodInfoOffset, currentOffset - methodInfoOffset);
                return currentOffset;
            }
        }

        // Visit the MethodParameters attribute.
        if (methodParametersOffset != 0 && (context.parsingOptions & Skip_Debug) == 0)
        {
            int parametersCount = ReadByte(methodParametersOffset);
            int currentParameterOffset = methodParametersOffset + 1;
            while (parametersCount-- > 0)
            {
                // Read the name_index and access_flags fields and visit them.
                methodVisitor.VisitParameter(ReadUtf8(currentParameterOffset, charBuffer),
                    ReadUnsignedShort(currentParameterOffset + 2));
                currentParameterOffset += 4;
            }
        }

        // Visit the AnnotationDefault attribute.
        if (annotationDefaultOffset != 0)
        {
            AnnotationVisitor annotationVisitor = methodVisitor.VisitAnnotationDefault();
            ReadElementValue(annotationVisitor, annotationDefaultOffset, null, charBuffer);
            if (annotationVisitor != null)
            {
                annotationVisitor.VisitEnd();
            }
        }

        // Visit the RuntimeVisibleAnnotations attribute.
        if (runtimeVisibleAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeVisibleAnnotationsOffset);
            int currentAnnotationOffset = runtimeVisibleAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset =
                    ReadElementValues(methodVisitor.VisitAnnotation(annotationDescriptor, true),
                        currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeInvisibleAnnotations attribute.
        if (runtimeInvisibleAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeInvisibleAnnotationsOffset);
            int currentAnnotationOffset = runtimeInvisibleAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset =
                    ReadElementValues(methodVisitor.VisitAnnotation(annotationDescriptor, false),
                        currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeVisibleTypeAnnotations attribute.
        if (runtimeVisibleTypeAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeVisibleTypeAnnotationsOffset);
            int currentAnnotationOffset = runtimeVisibleTypeAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the target_type, target_info and target_path fields.
                currentAnnotationOffset = ReadTypeAnnotationTarget(context, currentAnnotationOffset);
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset = ReadElementValues(
                    methodVisitor.VisitTypeAnnotation(context.currentTypeAnnotationTarget,
                        context.currentTypeAnnotationTargetPath, annotationDescriptor, true),
                    currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeInvisibleTypeAnnotations attribute.
        if (runtimeInvisibleTypeAnnotationsOffset != 0)
        {
            int numAnnotations = ReadUnsignedShort(runtimeInvisibleTypeAnnotationsOffset);
            int currentAnnotationOffset = runtimeInvisibleTypeAnnotationsOffset + 2;
            while (numAnnotations-- > 0)
            {
                // Parse the target_type, target_info and target_path fields.
                currentAnnotationOffset = ReadTypeAnnotationTarget(context, currentAnnotationOffset);
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                currentAnnotationOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentAnnotationOffset = ReadElementValues(
                    methodVisitor.VisitTypeAnnotation(context.currentTypeAnnotationTarget,
                        context.currentTypeAnnotationTargetPath, annotationDescriptor, false),
                    currentAnnotationOffset, true, charBuffer);
            }
        }

        // Visit the RuntimeVisibleParameterAnnotations attribute.
        if (runtimeVisibleParameterAnnotationsOffset != 0)
        {
            ReadParameterAnnotations(methodVisitor, context, runtimeVisibleParameterAnnotationsOffset, true);
        }

        // Visit the RuntimeInvisibleParameterAnnotations attribute.
        if (runtimeInvisibleParameterAnnotationsOffset != 0)
        {
            ReadParameterAnnotations(methodVisitor, context, runtimeInvisibleParameterAnnotationsOffset, false);
        }

        // Visit the non standard attributes.
        while (attributes != null)
        {
            // Copy and reset the nextAttribute field so that it can also be used in MethodWriter.
            Attribute nextAttribute = attributes.nextAttribute;
            attributes.nextAttribute = null;
            methodVisitor.VisitAttribute(attributes);
            attributes = nextAttribute;
        }

        // Visit the Code attribute.
        if (codeOffset != 0)
        {
            methodVisitor.VisitCode();
            ReadCode(methodVisitor, context, codeOffset);
        }

        // Visit the end of the method.
        methodVisitor.VisitEnd();
        return currentOffset;
    }

    // ----------------------------------------------------------------------------------------------
    // Methods to parse a Code attribute
    // ----------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads a JVMS 'Code' attribute and makes the given visitor visit it.
    /// </summary>
    /// <param name="methodVisitor"> the visitor that must visit the Code attribute. </param>
    /// <param name="context"> information about the class being parsed. </param>
    /// <param name="codeOffset"> the start offset in <see cref="classFileBuffer"/> of the Code attribute, excluding
    ///     its attribute_name_index and attribute_length fields. </param>
    private void ReadCode(MethodVisitor methodVisitor, Context context, int codeOffset)
    {
        int currentOffset = codeOffset;

        // Read the max_stack, max_locals and code_length fields.
        sbyte[] classBuffer = classFileBuffer;
        char[] charBuffer = context.charBuffer;
        int maxStack = ReadUnsignedShort(currentOffset);
        int maxLocals = ReadUnsignedShort(currentOffset + 2);
        int codeLength = ReadInt(currentOffset + 4);
        currentOffset += 8;
        if (codeLength > classFileBuffer.Length - currentOffset)
        {
            throw new System.ArgumentException();
        }

        // Read the bytecode 'code' array to create a label for each referenced instruction.
        int bytecodeStartOffset = currentOffset;
        int bytecodeEndOffset = currentOffset + codeLength;
        Label[] labels = context.currentMethodLabels = new Label[codeLength + 1];
        while (currentOffset < bytecodeEndOffset)
        {
            int bytecodeOffset = currentOffset - bytecodeStartOffset;
            int opcode = classBuffer[currentOffset] & 0xFF;
            switch (opcode)
            {
                case Opcodes.Nop:
                case Opcodes.Aconst_Null:
                case Opcodes.Iconst_M1:
                case Opcodes.Iconst_0:
                case Opcodes.Iconst_1:
                case Opcodes.Iconst_2:
                case Opcodes.Iconst_3:
                case Opcodes.Iconst_4:
                case Opcodes.Iconst_5:
                case Opcodes.Lconst_0:
                case Opcodes.Lconst_1:
                case Opcodes.Fconst_0:
                case Opcodes.Fconst_1:
                case Opcodes.Fconst_2:
                case Opcodes.Dconst_0:
                case Opcodes.Dconst_1:
                case Opcodes.Iaload:
                case Opcodes.Laload:
                case Opcodes.Faload:
                case Opcodes.Daload:
                case Opcodes.Aaload:
                case Opcodes.Baload:
                case Opcodes.Caload:
                case Opcodes.Saload:
                case Opcodes.Iastore:
                case Opcodes.Lastore:
                case Opcodes.Fastore:
                case Opcodes.Dastore:
                case Opcodes.Aastore:
                case Opcodes.Bastore:
                case Opcodes.Castore:
                case Opcodes.Sastore:
                case Opcodes.Pop:
                case Opcodes.Pop2:
                case Opcodes.Dup:
                case Opcodes.Dup_X1:
                case Opcodes.Dup_X2:
                case Opcodes.Dup2:
                case Opcodes.Dup2_X1:
                case Opcodes.Dup2_X2:
                case Opcodes.Swap:
                case Opcodes.Iadd:
                case Opcodes.Ladd:
                case Opcodes.Fadd:
                case Opcodes.Dadd:
                case Opcodes.Isub:
                case Opcodes.Lsub:
                case Opcodes.Fsub:
                case Opcodes.Dsub:
                case Opcodes.Imul:
                case Opcodes.Lmul:
                case Opcodes.Fmul:
                case Opcodes.Dmul:
                case Opcodes.Idiv:
                case Opcodes.Ldiv:
                case Opcodes.Fdiv:
                case Opcodes.Ddiv:
                case Opcodes.Irem:
                case Opcodes.Lrem:
                case Opcodes.Frem:
                case Opcodes.Drem:
                case Opcodes.Ineg:
                case Opcodes.Lneg:
                case Opcodes.Fneg:
                case Opcodes.Dneg:
                case Opcodes.Ishl:
                case Opcodes.Lshl:
                case Opcodes.Ishr:
                case Opcodes.Lshr:
                case Opcodes.Iushr:
                case Opcodes.Lushr:
                case Opcodes.Iand:
                case Opcodes.Land:
                case Opcodes.Ior:
                case Opcodes.Lor:
                case Opcodes.Ixor:
                case Opcodes.Lxor:
                case Opcodes.I2L:
                case Opcodes.I2F:
                case Opcodes.I2D:
                case Opcodes.L2I:
                case Opcodes.L2F:
                case Opcodes.L2D:
                case Opcodes.F2I:
                case Opcodes.F2L:
                case Opcodes.F2D:
                case Opcodes.D2I:
                case Opcodes.D2L:
                case Opcodes.D2F:
                case Opcodes.I2B:
                case Opcodes.I2C:
                case Opcodes.I2S:
                case Opcodes.Lcmp:
                case Opcodes.Fcmpl:
                case Opcodes.Fcmpg:
                case Opcodes.Dcmpl:
                case Opcodes.Dcmpg:
                case Opcodes.Ireturn:
                case Opcodes.Lreturn:
                case Opcodes.Freturn:
                case Opcodes.Dreturn:
                case Opcodes.Areturn:
                case Opcodes.Return:
                case Opcodes.Arraylength:
                case Opcodes.Athrow:
                case Opcodes.Monitorenter:
                case Opcodes.Monitorexit:
                case Constants.Iload_0:
                case Constants.Iload_1:
                case Constants.Iload_2:
                case Constants.Iload_3:
                case Constants.Lload_0:
                case Constants.Lload_1:
                case Constants.Lload_2:
                case Constants.Lload_3:
                case Constants.Fload_0:
                case Constants.Fload_1:
                case Constants.Fload_2:
                case Constants.Fload_3:
                case Constants.Dload_0:
                case Constants.Dload_1:
                case Constants.Dload_2:
                case Constants.Dload_3:
                case Constants.Aload_0:
                case Constants.Aload_1:
                case Constants.Aload_2:
                case Constants.Aload_3:
                case Constants.Istore_0:
                case Constants.Istore_1:
                case Constants.Istore_2:
                case Constants.Istore_3:
                case Constants.Lstore_0:
                case Constants.Lstore_1:
                case Constants.Lstore_2:
                case Constants.Lstore_3:
                case Constants.Fstore_0:
                case Constants.Fstore_1:
                case Constants.Fstore_2:
                case Constants.Fstore_3:
                case Constants.Dstore_0:
                case Constants.Dstore_1:
                case Constants.Dstore_2:
                case Constants.Dstore_3:
                case Constants.Astore_0:
                case Constants.Astore_1:
                case Constants.Astore_2:
                case Constants.Astore_3:
                    currentOffset += 1;
                    break;
                case Opcodes.Ifeq:
                case Opcodes.Ifne:
                case Opcodes.Iflt:
                case Opcodes.Ifge:
                case Opcodes.Ifgt:
                case Opcodes.Ifle:
                case Opcodes.If_Icmpeq:
                case Opcodes.If_Icmpne:
                case Opcodes.If_Icmplt:
                case Opcodes.If_Icmpge:
                case Opcodes.If_Icmpgt:
                case Opcodes.If_Icmple:
                case Opcodes.If_Acmpeq:
                case Opcodes.If_Acmpne:
                case Opcodes.Goto:
                case Opcodes.Jsr:
                case Opcodes.Ifnull:
                case Opcodes.Ifnonnull:
                    CreateLabel(bytecodeOffset + ReadShort(currentOffset + 1), labels);
                    currentOffset += 3;
                    break;
                case Constants.Asm_Ifeq:
                case Constants.Asm_Ifne:
                case Constants.Asm_Iflt:
                case Constants.Asm_Ifge:
                case Constants.Asm_Ifgt:
                case Constants.Asm_Ifle:
                case Constants.Asm_If_Icmpeq:
                case Constants.Asm_If_Icmpne:
                case Constants.Asm_If_Icmplt:
                case Constants.Asm_If_Icmpge:
                case Constants.Asm_If_Icmpgt:
                case Constants.Asm_If_Icmple:
                case Constants.Asm_If_Acmpeq:
                case Constants.Asm_If_Acmpne:
                case Constants.Asm_Goto:
                case Constants.Asm_Jsr:
                case Constants.Asm_Ifnull:
                case Constants.Asm_Ifnonnull:
                    CreateLabel(bytecodeOffset + ReadUnsignedShort(currentOffset + 1), labels);
                    currentOffset += 3;
                    break;
                case Constants.Goto_W:
                case Constants.Jsr_W:
                case Constants.Asm_Goto_W:
                    CreateLabel(bytecodeOffset + ReadInt(currentOffset + 1), labels);
                    currentOffset += 5;
                    break;
                case Constants.Wide:
                    switch (classBuffer[currentOffset + 1] & 0xFF)
                    {
                        case Opcodes.Iload:
                        case Opcodes.Fload:
                        case Opcodes.Aload:
                        case Opcodes.Lload:
                        case Opcodes.Dload:
                        case Opcodes.Istore:
                        case Opcodes.Fstore:
                        case Opcodes.Astore:
                        case Opcodes.Lstore:
                        case Opcodes.Dstore:
                        case Opcodes.Ret:
                            currentOffset += 4;
                            break;
                        case Opcodes.Iinc:
                            currentOffset += 6;
                            break;
                        default:
                            throw new System.ArgumentException();
                    }

                    break;
                case Opcodes.Tableswitch:
                    // Skip 0 to 3 padding bytes.
                    currentOffset += 4 - (bytecodeOffset & 3);
                    // Read the default label and the number of table entries.
                    CreateLabel(bytecodeOffset + ReadInt(currentOffset), labels);
                    int numTableEntries = ReadInt(currentOffset + 8) - ReadInt(currentOffset + 4) + 1;
                    currentOffset += 12;
                    // Read the table labels.
                    while (numTableEntries-- > 0)
                    {
                        CreateLabel(bytecodeOffset + ReadInt(currentOffset), labels);
                        currentOffset += 4;
                    }

                    break;
                case Opcodes.Lookupswitch:
                    // Skip 0 to 3 padding bytes.
                    currentOffset += 4 - (bytecodeOffset & 3);
                    // Read the default label and the number of switch cases.
                    CreateLabel(bytecodeOffset + ReadInt(currentOffset), labels);
                    int numSwitchCases = ReadInt(currentOffset + 4);
                    currentOffset += 8;
                    // Read the switch labels.
                    while (numSwitchCases-- > 0)
                    {
                        CreateLabel(bytecodeOffset + ReadInt(currentOffset + 4), labels);
                        currentOffset += 8;
                    }

                    break;
                case Opcodes.Iload:
                case Opcodes.Lload:
                case Opcodes.Fload:
                case Opcodes.Dload:
                case Opcodes.Aload:
                case Opcodes.Istore:
                case Opcodes.Lstore:
                case Opcodes.Fstore:
                case Opcodes.Dstore:
                case Opcodes.Astore:
                case Opcodes.Ret:
                case Opcodes.Bipush:
                case Opcodes.Newarray:
                case Opcodes.Ldc:
                    currentOffset += 2;
                    break;
                case Opcodes.Sipush:
                case Constants.Ldc_W:
                case Constants.Ldc2_W:
                case Opcodes.Getstatic:
                case Opcodes.Putstatic:
                case Opcodes.Getfield:
                case Opcodes.Putfield:
                case Opcodes.Invokevirtual:
                case Opcodes.Invokespecial:
                case Opcodes.Invokestatic:
                case Opcodes.New:
                case Opcodes.Anewarray:
                case Opcodes.Checkcast:
                case Opcodes.Instanceof:
                case Opcodes.Iinc:
                    currentOffset += 3;
                    break;
                case Opcodes.Invokeinterface:
                case Opcodes.Invokedynamic:
                    currentOffset += 5;
                    break;
                case Opcodes.Multianewarray:
                    currentOffset += 4;
                    break;
                default:
                    throw new System.ArgumentException();
            }
        }

        // Read the 'exception_table_length' and 'exception_table' field to create a label for each
        // referenced instruction, and to make methodVisitor visit the corresponding try catch blocks.
        int exceptionTableLength = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (exceptionTableLength-- > 0)
        {
            Label start = CreateLabel(ReadUnsignedShort(currentOffset), labels);
            Label end = CreateLabel(ReadUnsignedShort(currentOffset + 2), labels);
            Label handler = CreateLabel(ReadUnsignedShort(currentOffset + 4), labels);
            string catchType = ReadUtf8(_cpInfoOffsets[ReadUnsignedShort(currentOffset + 6)], charBuffer);
            currentOffset += 8;
            methodVisitor.VisitTryCatchBlock(start, end, handler, catchType);
        }

        // Read the Code attributes to create a label for each referenced instruction (the variables
        // are ordered as in Section 4.7 of the JVMS). Attribute offsets exclude the
        // attribute_name_index and attribute_length fields.
        // - The offset of the current 'stack_map_frame' in the StackMap[Table] attribute, or 0.
        // Initially, this is the offset of the first 'stack_map_frame' entry. Then this offset is
        // updated after each stack_map_frame is read.
        int stackMapFrameOffset = 0;
        // - The end offset of the StackMap[Table] attribute, or 0.
        int stackMapTableEndOffset = 0;
        // - Whether the stack map frames are compressed (i.e. in a StackMapTable) or not.
        bool compressedFrames = true;
        // - The offset of the LocalVariableTable attribute, or 0.
        int localVariableTableOffset = 0;
        // - The offset of the LocalVariableTypeTable attribute, or 0.
        int localVariableTypeTableOffset = 0;
        // - The offset of each 'type_annotation' entry in the RuntimeVisibleTypeAnnotations
        // attribute, or null.
        int[] visibleTypeAnnotationOffsets = null;
        // - The offset of each 'type_annotation' entry in the RuntimeInvisibleTypeAnnotations
        // attribute, or null.
        int[] invisibleTypeAnnotationOffsets = null;
        // - The non standard attributes (linked with their <see>Attribute#nextAttribute</see> field).
        //   This list in the <i>reverse order</i> or their order in the ClassFile structure.
        Attribute attributes = null;

        int attributesCount = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        while (attributesCount-- > 0)
        {
            // Read the attribute_info's attribute_name and attribute_length fields.
            string attributeName = ReadUtf8(currentOffset, charBuffer);
            int attributeLength = ReadInt(currentOffset + 2);
            currentOffset += 6;
            if (Constants.Local_Variable_Table.Equals(attributeName))
            {
                if ((context.parsingOptions & Skip_Debug) == 0)
                {
                    localVariableTableOffset = currentOffset;
                    // Parse the attribute to find the corresponding (debug only) labels.
                    int currentLocalVariableTableOffset = currentOffset;
                    int localVariableTableLength = ReadUnsignedShort(currentLocalVariableTableOffset);
                    currentLocalVariableTableOffset += 2;
                    while (localVariableTableLength-- > 0)
                    {
                        int startPc = ReadUnsignedShort(currentLocalVariableTableOffset);
                        CreateDebugLabel(startPc, labels);
                        int length = ReadUnsignedShort(currentLocalVariableTableOffset + 2);
                        CreateDebugLabel(startPc + length, labels);
                        // Skip the name_index, descriptor_index and index fields (2 bytes each).
                        currentLocalVariableTableOffset += 10;
                    }
                }
            }
            else if (Constants.Local_Variable_Type_Table.Equals(attributeName))
            {
                localVariableTypeTableOffset = currentOffset;
                // Here we do not extract the labels corresponding to the attribute content. We assume they
                // are the same or a subset of those of the LocalVariableTable attribute.
            }
            else if (Constants.Line_Number_Table.Equals(attributeName))
            {
                if ((context.parsingOptions & Skip_Debug) == 0)
                {
                    // Parse the attribute to find the corresponding (debug only) labels.
                    int currentLineNumberTableOffset = currentOffset;
                    int lineNumberTableLength = ReadUnsignedShort(currentLineNumberTableOffset);
                    currentLineNumberTableOffset += 2;
                    while (lineNumberTableLength-- > 0)
                    {
                        int startPc = ReadUnsignedShort(currentLineNumberTableOffset);
                        int lineNumber = ReadUnsignedShort(currentLineNumberTableOffset + 2);
                        currentLineNumberTableOffset += 4;
                        CreateDebugLabel(startPc, labels);
                        labels[startPc].AddLineNumber(lineNumber);
                    }
                }
            }
            else if (Constants.Runtime_Visible_Type_Annotations.Equals(attributeName))
            {
                visibleTypeAnnotationOffsets = ReadTypeAnnotations(methodVisitor, context, currentOffset, true);
                // Here we do not extract the labels corresponding to the attribute content. This would
                // require a full parsing of the attribute, which would need to be repeated when parsing
                // the bytecode instructions (see below). Instead, the content of the attribute is read one
                // type annotation at a time (i.e. after a type annotation has been visited, the next type
                // annotation is read), and the labels it contains are also extracted one annotation at a
                // time. This assumes that type annotations are ordered by increasing bytecode offset.
            }
            else if (Constants.Runtime_Invisible_Type_Annotations.Equals(attributeName))
            {
                invisibleTypeAnnotationOffsets = ReadTypeAnnotations(methodVisitor, context, currentOffset, false);
                // Same comment as above for the RuntimeVisibleTypeAnnotations attribute.
            }
            else if (Constants.Stack_Map_Table.Equals(attributeName))
            {
                if ((context.parsingOptions & Skip_Frames) == 0)
                {
                    stackMapFrameOffset = currentOffset + 2;
                    stackMapTableEndOffset = currentOffset + attributeLength;
                }
                // Here we do not extract the labels corresponding to the attribute content. This would
                // require a full parsing of the attribute, which would need to be repeated when parsing
                // the bytecode instructions (see below). Instead, the content of the attribute is read one
                // frame at a time (i.e. after a frame has been visited, the next frame is read), and the
                // labels it contains are also extracted one frame at a time. Thanks to the ordering of
                // frames, having only a "one frame lookahead" is not a problem, i.e. it is not possible to
                // see an offset smaller than the offset of the current instruction and for which no Label
                // exist. Except for UNINITIALIZED type offsets. We solve this by parsing the stack map
                // table without a full decoding (see below).
            }
            else if ("StackMap".Equals(attributeName))
            {
                if ((context.parsingOptions & Skip_Frames) == 0)
                {
                    stackMapFrameOffset = currentOffset + 2;
                    stackMapTableEndOffset = currentOffset + attributeLength;
                    compressedFrames = false;
                }
                // IMPORTANT! Here we assume that the frames are ordered, as in the StackMapTable attribute,
                // although this is not guaranteed by the attribute format. This allows an incremental
                // extraction of the labels corresponding to this attribute (see the comment above for the
                // StackMapTable attribute).
            }
            else
            {
                Attribute attribute = ReadAttribute(context.attributePrototypes, attributeName, currentOffset,
                    attributeLength, charBuffer, codeOffset, labels);
                attribute.nextAttribute = attributes;
                attributes = attribute;
            }

            currentOffset += attributeLength;
        }

        // Initialize the context fields related to stack map frames, and generate the first
        // (implicit) stack map frame, if needed.
        bool expandFrames = (context.parsingOptions & Expand_Frames) != 0;
        if (stackMapFrameOffset != 0)
        {
            // The bytecode offset of the first explicit frame is not offset_delta + 1 but only
            // offset_delta. Setting the implicit frame offset to -1 allows us to use of the
            // "offset_delta + 1" rule in all cases.
            context.currentFrameOffset = -1;
            context.currentFrameType = 0;
            context.currentFrameLocalCount = 0;
            context.currentFrameLocalCountDelta = 0;
            context.currentFrameLocalTypes = new object[maxLocals];
            context.currentFrameStackCount = 0;
            context.currentFrameStackTypes = new object[maxStack];
            if (expandFrames)
            {
                ComputeImplicitFrame(context);
            }

            // Find the labels for UNINITIALIZED frame types. Instead of decoding each element of the
            // stack map table, we look for 3 consecutive bytes that "look like" an UNINITIALIZED type
            // (tag ITEM_Uninitialized, offset within bytecode bounds, NEW instruction at this offset).
            // We may find false positives (i.e. not real UNINITIALIZED types), but this should be rare,
            // and the only consequence will be the creation of an unneeded label. This is better than
            // creating a label for each NEW instruction, and faster than fully decoding the whole stack
            // map table.
            for (int offset = stackMapFrameOffset; offset < stackMapTableEndOffset - 2; ++offset)
            {
                if (classBuffer[offset] == Frame.Item_Uninitialized)
                {
                    int potentialBytecodeOffset = ReadUnsignedShort(offset + 1);
                    if (potentialBytecodeOffset >= 0 && potentialBytecodeOffset < codeLength &&
                        (classBuffer[bytecodeStartOffset + potentialBytecodeOffset] & 0xFF) == Opcodes.New)
                    {
                        CreateLabel(potentialBytecodeOffset, labels);
                    }
                }
            }
        }

        if (expandFrames && (context.parsingOptions & Expand_Asm_Insns) != 0)
        {
            // Expanding the ASM specific instructions can introduce F_INSERT frames, even if the method
            // does not currently have any frame. These inserted frames must be computed by simulating the
            // effect of the bytecode instructions, one by one, starting from the implicit first frame.
            // For this, MethodWriter needs to know maxLocals before the first instruction is visited. To
            // ensure this, we visit the implicit first frame here (passing only maxLocals - the rest is
            // computed in MethodWriter).
            methodVisitor.VisitFrame(Opcodes.F_New, maxLocals, null, 0, null);
        }

        // Visit the bytecode instructions. First, introduce state variables for the incremental parsing
        // of the type annotations.

        // Index of the next runtime visible type annotation to read (in the
        // visibleTypeAnnotationOffsets array).
        int currentVisibleTypeAnnotationIndex = 0;
        // The bytecode offset of the next runtime visible type annotation to read, or -1.
        int currentVisibleTypeAnnotationBytecodeOffset =
            GetTypeAnnotationBytecodeOffset(visibleTypeAnnotationOffsets, 0);
        // Index of the next runtime invisible type annotation to read (in the
        // invisibleTypeAnnotationOffsets array).
        int currentInvisibleTypeAnnotationIndex = 0;
        // The bytecode offset of the next runtime invisible type annotation to read, or -1.
        int currentInvisibleTypeAnnotationBytecodeOffset =
            GetTypeAnnotationBytecodeOffset(invisibleTypeAnnotationOffsets, 0);

        // Whether a F_INSERT stack map frame must be inserted before the current instruction.
        bool insertFrame = false;

        // The delta to subtract from a goto_w or jsr_w opcode to get the corresponding goto or jsr
        // opcode, or 0 if goto_w and jsr_w must be left unchanged (i.e. when expanding ASM specific
        // instructions).
        int wideJumpOpcodeDelta =
            (context.parsingOptions & Expand_Asm_Insns) == 0 ? Constants.WideJumpOpcodeDelta : 0;

        currentOffset = bytecodeStartOffset;
        while (currentOffset < bytecodeEndOffset)
        {
            int currentBytecodeOffset = currentOffset - bytecodeStartOffset;
            ReadBytecodeInstructionOffset(currentBytecodeOffset);

            // Visit the label and the line number(s) for this bytecode offset, if any.
            Label currentLabel = labels[currentBytecodeOffset];
            if (currentLabel != null)
            {
                currentLabel.Accept(methodVisitor, (context.parsingOptions & Skip_Debug) == 0);
            }

            // Visit the stack map frame for this bytecode offset, if any.
            while (stackMapFrameOffset != 0 && (context.currentFrameOffset == currentBytecodeOffset ||
                                                context.currentFrameOffset == -1))
            {
                // If there is a stack map frame for this offset, make methodVisitor visit it, and read the
                // next stack map frame if there is one.
                if (context.currentFrameOffset != -1)
                {
                    if (!compressedFrames || expandFrames)
                    {
                        methodVisitor.VisitFrame(Opcodes.F_New, context.currentFrameLocalCount,
                            context.currentFrameLocalTypes, context.currentFrameStackCount,
                            context.currentFrameStackTypes);
                    }
                    else
                    {
                        methodVisitor.VisitFrame(context.currentFrameType, context.currentFrameLocalCountDelta,
                            context.currentFrameLocalTypes, context.currentFrameStackCount,
                            context.currentFrameStackTypes);
                    }

                    // Since there is already a stack map frame for this bytecode offset, there is no need to
                    // insert a new one.
                    insertFrame = false;
                }

                if (stackMapFrameOffset < stackMapTableEndOffset)
                {
                    stackMapFrameOffset =
                        ReadStackMapFrame(stackMapFrameOffset, compressedFrames, expandFrames, context);
                }
                else
                {
                    stackMapFrameOffset = 0;
                }
            }

            // Insert a stack map frame for this bytecode offset, if requested by setting insertFrame to
            // true during the previous iteration. The actual frame content is computed in MethodWriter.
            if (insertFrame)
            {
                if ((context.parsingOptions & Expand_Frames) != 0)
                {
                    methodVisitor.VisitFrame(Constants.F_Insert, 0, null, 0, null);
                }

                insertFrame = false;
            }

            // Visit the instruction at this bytecode offset.
            int opcode = classBuffer[currentOffset] & 0xFF;
            switch (opcode)
            {
                case Opcodes.Nop:
                case Opcodes.Aconst_Null:
                case Opcodes.Iconst_M1:
                case Opcodes.Iconst_0:
                case Opcodes.Iconst_1:
                case Opcodes.Iconst_2:
                case Opcodes.Iconst_3:
                case Opcodes.Iconst_4:
                case Opcodes.Iconst_5:
                case Opcodes.Lconst_0:
                case Opcodes.Lconst_1:
                case Opcodes.Fconst_0:
                case Opcodes.Fconst_1:
                case Opcodes.Fconst_2:
                case Opcodes.Dconst_0:
                case Opcodes.Dconst_1:
                case Opcodes.Iaload:
                case Opcodes.Laload:
                case Opcodes.Faload:
                case Opcodes.Daload:
                case Opcodes.Aaload:
                case Opcodes.Baload:
                case Opcodes.Caload:
                case Opcodes.Saload:
                case Opcodes.Iastore:
                case Opcodes.Lastore:
                case Opcodes.Fastore:
                case Opcodes.Dastore:
                case Opcodes.Aastore:
                case Opcodes.Bastore:
                case Opcodes.Castore:
                case Opcodes.Sastore:
                case Opcodes.Pop:
                case Opcodes.Pop2:
                case Opcodes.Dup:
                case Opcodes.Dup_X1:
                case Opcodes.Dup_X2:
                case Opcodes.Dup2:
                case Opcodes.Dup2_X1:
                case Opcodes.Dup2_X2:
                case Opcodes.Swap:
                case Opcodes.Iadd:
                case Opcodes.Ladd:
                case Opcodes.Fadd:
                case Opcodes.Dadd:
                case Opcodes.Isub:
                case Opcodes.Lsub:
                case Opcodes.Fsub:
                case Opcodes.Dsub:
                case Opcodes.Imul:
                case Opcodes.Lmul:
                case Opcodes.Fmul:
                case Opcodes.Dmul:
                case Opcodes.Idiv:
                case Opcodes.Ldiv:
                case Opcodes.Fdiv:
                case Opcodes.Ddiv:
                case Opcodes.Irem:
                case Opcodes.Lrem:
                case Opcodes.Frem:
                case Opcodes.Drem:
                case Opcodes.Ineg:
                case Opcodes.Lneg:
                case Opcodes.Fneg:
                case Opcodes.Dneg:
                case Opcodes.Ishl:
                case Opcodes.Lshl:
                case Opcodes.Ishr:
                case Opcodes.Lshr:
                case Opcodes.Iushr:
                case Opcodes.Lushr:
                case Opcodes.Iand:
                case Opcodes.Land:
                case Opcodes.Ior:
                case Opcodes.Lor:
                case Opcodes.Ixor:
                case Opcodes.Lxor:
                case Opcodes.I2L:
                case Opcodes.I2F:
                case Opcodes.I2D:
                case Opcodes.L2I:
                case Opcodes.L2F:
                case Opcodes.L2D:
                case Opcodes.F2I:
                case Opcodes.F2L:
                case Opcodes.F2D:
                case Opcodes.D2I:
                case Opcodes.D2L:
                case Opcodes.D2F:
                case Opcodes.I2B:
                case Opcodes.I2C:
                case Opcodes.I2S:
                case Opcodes.Lcmp:
                case Opcodes.Fcmpl:
                case Opcodes.Fcmpg:
                case Opcodes.Dcmpl:
                case Opcodes.Dcmpg:
                case Opcodes.Ireturn:
                case Opcodes.Lreturn:
                case Opcodes.Freturn:
                case Opcodes.Dreturn:
                case Opcodes.Areturn:
                case Opcodes.Return:
                case Opcodes.Arraylength:
                case Opcodes.Athrow:
                case Opcodes.Monitorenter:
                case Opcodes.Monitorexit:
                    methodVisitor.VisitInsn(opcode);
                    currentOffset += 1;
                    break;
                case Constants.Iload_0:
                case Constants.Iload_1:
                case Constants.Iload_2:
                case Constants.Iload_3:
                case Constants.Lload_0:
                case Constants.Lload_1:
                case Constants.Lload_2:
                case Constants.Lload_3:
                case Constants.Fload_0:
                case Constants.Fload_1:
                case Constants.Fload_2:
                case Constants.Fload_3:
                case Constants.Dload_0:
                case Constants.Dload_1:
                case Constants.Dload_2:
                case Constants.Dload_3:
                case Constants.Aload_0:
                case Constants.Aload_1:
                case Constants.Aload_2:
                case Constants.Aload_3:
                    opcode -= Constants.Iload_0;
                    methodVisitor.VisitVarInsn(Opcodes.Iload + (opcode >> 2), opcode & 0x3);
                    currentOffset += 1;
                    break;
                case Constants.Istore_0:
                case Constants.Istore_1:
                case Constants.Istore_2:
                case Constants.Istore_3:
                case Constants.Lstore_0:
                case Constants.Lstore_1:
                case Constants.Lstore_2:
                case Constants.Lstore_3:
                case Constants.Fstore_0:
                case Constants.Fstore_1:
                case Constants.Fstore_2:
                case Constants.Fstore_3:
                case Constants.Dstore_0:
                case Constants.Dstore_1:
                case Constants.Dstore_2:
                case Constants.Dstore_3:
                case Constants.Astore_0:
                case Constants.Astore_1:
                case Constants.Astore_2:
                case Constants.Astore_3:
                    opcode -= Constants.Istore_0;
                    methodVisitor.VisitVarInsn(Opcodes.Istore + (opcode >> 2), opcode & 0x3);
                    currentOffset += 1;
                    break;
                case Opcodes.Ifeq:
                case Opcodes.Ifne:
                case Opcodes.Iflt:
                case Opcodes.Ifge:
                case Opcodes.Ifgt:
                case Opcodes.Ifle:
                case Opcodes.If_Icmpeq:
                case Opcodes.If_Icmpne:
                case Opcodes.If_Icmplt:
                case Opcodes.If_Icmpge:
                case Opcodes.If_Icmpgt:
                case Opcodes.If_Icmple:
                case Opcodes.If_Acmpeq:
                case Opcodes.If_Acmpne:
                case Opcodes.Goto:
                case Opcodes.Jsr:
                case Opcodes.Ifnull:
                case Opcodes.Ifnonnull:
                    methodVisitor.VisitJumpInsn(opcode,
                        labels[currentBytecodeOffset + ReadShort(currentOffset + 1)]);
                    currentOffset += 3;
                    break;
                case Constants.Goto_W:
                case Constants.Jsr_W:
                    methodVisitor.VisitJumpInsn(opcode - wideJumpOpcodeDelta,
                        labels[currentBytecodeOffset + ReadInt(currentOffset + 1)]);
                    currentOffset += 5;
                    break;
                case Constants.Asm_Ifeq:
                case Constants.Asm_Ifne:
                case Constants.Asm_Iflt:
                case Constants.Asm_Ifge:
                case Constants.Asm_Ifgt:
                case Constants.Asm_Ifle:
                case Constants.Asm_If_Icmpeq:
                case Constants.Asm_If_Icmpne:
                case Constants.Asm_If_Icmplt:
                case Constants.Asm_If_Icmpge:
                case Constants.Asm_If_Icmpgt:
                case Constants.Asm_If_Icmple:
                case Constants.Asm_If_Acmpeq:
                case Constants.Asm_If_Acmpne:
                case Constants.Asm_Goto:
                case Constants.Asm_Jsr:
                case Constants.Asm_Ifnull:
                case Constants.Asm_Ifnonnull:
                {
                    // A forward jump with an offset > 32767. In this case we automatically replace ASM_GOTO
                    // with GOTO_W, ASM_JSR with JSR_W and ASM_IFxxx <l> with IFNOTxxx <L> GOTO_W <l> L:...,
                    // where IFNOTxxx is the "opposite" opcode of ASMS_IFxxx (e.g. IFNE for ASM_IFEQ) and
                    // where <L> designates the instruction just after the GOTO_W.
                    // First, change the ASM specific opcodes ASM_IFEQ ... ASM_JSR, ASM_IFNULL and
                    // ASM_IFNONNULL to IFEQ ... JSR, IFNULL and IFNONNULL.
                    opcode = opcode < Constants.Asm_Ifnull
                        ? opcode - Constants.Asm_Opcode_Delta
                        : opcode - Constants.Asm_Ifnull_Opcode_Delta;
                    Label target = labels[currentBytecodeOffset + ReadUnsignedShort(currentOffset + 1)];
                    if (opcode == Opcodes.Goto || opcode == Opcodes.Jsr)
                    {
                        // Replace GOTO with GOTO_W and JSR with JSR_W.
                        methodVisitor.VisitJumpInsn(opcode + Constants.WideJumpOpcodeDelta, target);
                    }
                    else
                    {
                        // Compute the "opposite" of opcode. This can be done by flipping the least
                        // significant bit for IFNULL and IFNONNULL, and similarly for IFEQ ... IF_ACMPEQ
                        // (with a pre and post offset by 1).
                        opcode = opcode < Opcodes.Goto ? ((opcode + 1) ^ 1) - 1 : opcode ^ 1;
                        Label endif = CreateLabel(currentBytecodeOffset + 3, labels);
                        methodVisitor.VisitJumpInsn(opcode, endif);
                        methodVisitor.VisitJumpInsn(Constants.Goto_W, target);
                        // endif designates the instruction just after GOTO_W, and is visited as part of the
                        // next instruction. Since it is a jump target, we need to insert a frame here.
                        insertFrame = true;
                    }

                    currentOffset += 3;
                    break;
                }
                case Constants.Asm_Goto_W:
                    // Replace ASM_GOTO_W with GOTO_W.
                    methodVisitor.VisitJumpInsn(Constants.Goto_W,
                        labels[currentBytecodeOffset + ReadInt(currentOffset + 1)]);
                    // The instruction just after is a jump target (because ASM_GOTO_W is used in patterns
                    // IFNOTxxx <L> ASM_GOTO_W <l> L:..., see MethodWriter), so we need to insert a frame
                    // here.
                    insertFrame = true;
                    currentOffset += 5;
                    break;
                case Constants.Wide:
                    opcode = classBuffer[currentOffset + 1] & 0xFF;
                    if (opcode == Opcodes.Iinc)
                    {
                        methodVisitor.VisitIincInsn(ReadUnsignedShort(currentOffset + 2),
                            ReadShort(currentOffset + 4));
                        currentOffset += 6;
                    }
                    else
                    {
                        methodVisitor.VisitVarInsn(opcode, ReadUnsignedShort(currentOffset + 2));
                        currentOffset += 4;
                    }

                    break;
                case Opcodes.Tableswitch:
                {
                    // Skip 0 to 3 padding bytes.
                    currentOffset += 4 - (currentBytecodeOffset & 3);
                    // Read the instruction.
                    Label defaultLabel = labels[currentBytecodeOffset + ReadInt(currentOffset)];
                    int low = ReadInt(currentOffset + 4);
                    int high = ReadInt(currentOffset + 8);
                    currentOffset += 12;
                    Label[] table = new Label[high - low + 1];
                    for (int i = 0; i < table.Length; ++i)
                    {
                        table[i] = labels[currentBytecodeOffset + ReadInt(currentOffset)];
                        currentOffset += 4;
                    }

                    methodVisitor.VisitTableSwitchInsn(low, high, defaultLabel, table);
                    break;
                }
                case Opcodes.Lookupswitch:
                {
                    // Skip 0 to 3 padding bytes.
                    currentOffset += 4 - (currentBytecodeOffset & 3);
                    // Read the instruction.
                    Label defaultLabel = labels[currentBytecodeOffset + ReadInt(currentOffset)];
                    int numPairs = ReadInt(currentOffset + 4);
                    currentOffset += 8;
                    int[] keys = new int[numPairs];
                    Label[] Values = new Label[numPairs];
                    for (int i = 0; i < numPairs; ++i)
                    {
                        keys[i] = ReadInt(currentOffset);
                        Values[i] = labels[currentBytecodeOffset + ReadInt(currentOffset + 4)];
                        currentOffset += 8;
                    }

                    methodVisitor.VisitLookupSwitchInsn(defaultLabel, keys, Values);
                    break;
                }
                case Opcodes.Iload:
                case Opcodes.Lload:
                case Opcodes.Fload:
                case Opcodes.Dload:
                case Opcodes.Aload:
                case Opcodes.Istore:
                case Opcodes.Lstore:
                case Opcodes.Fstore:
                case Opcodes.Dstore:
                case Opcodes.Astore:
                case Opcodes.Ret:
                    methodVisitor.VisitVarInsn(opcode, classBuffer[currentOffset + 1] & 0xFF);
                    currentOffset += 2;
                    break;
                case Opcodes.Bipush:
                case Opcodes.Newarray:
                    methodVisitor.VisitIntInsn(opcode, classBuffer[currentOffset + 1]);
                    currentOffset += 2;
                    break;
                case Opcodes.Sipush:
                    methodVisitor.VisitIntInsn(opcode, ReadShort(currentOffset + 1));
                    currentOffset += 3;
                    break;
                case Opcodes.Ldc:
                    methodVisitor.VisitLdcInsn(ReadConst(classBuffer[currentOffset + 1] & 0xFF, charBuffer));
                    currentOffset += 2;
                    break;
                case Constants.Ldc_W:
                case Constants.Ldc2_W:
                    methodVisitor.VisitLdcInsn(ReadConst(ReadUnsignedShort(currentOffset + 1), charBuffer));
                    currentOffset += 3;
                    break;
                case Opcodes.Getstatic:
                case Opcodes.Putstatic:
                case Opcodes.Getfield:
                case Opcodes.Putfield:
                case Opcodes.Invokevirtual:
                case Opcodes.Invokespecial:
                case Opcodes.Invokestatic:
                case Opcodes.Invokeinterface:
                {
                    int cpInfoOffset = _cpInfoOffsets[ReadUnsignedShort(currentOffset + 1)];
                    int nameAndTypeCpInfoOffset = _cpInfoOffsets[ReadUnsignedShort(cpInfoOffset + 2)];
                    string owner = ReadClass(cpInfoOffset, charBuffer);
                    string name = ReadUtf8(nameAndTypeCpInfoOffset, charBuffer);
                    string descriptor = ReadUtf8(nameAndTypeCpInfoOffset + 2, charBuffer);
                    if (opcode < Opcodes.Invokevirtual)
                    {
                        methodVisitor.VisitFieldInsn(opcode, owner, name, descriptor);
                    }
                    else
                    {
                        bool isInterface = classBuffer[cpInfoOffset - 1] == Symbol.Constant_Interface_Methodref_Tag;
                        methodVisitor.VisitMethodInsn(opcode, owner, name, descriptor, isInterface);
                    }

                    if (opcode == Opcodes.Invokeinterface)
                    {
                        currentOffset += 5;
                    }
                    else
                    {
                        currentOffset += 3;
                    }

                    break;
                }
                case Opcodes.Invokedynamic:
                {
                    int cpInfoOffset = _cpInfoOffsets[ReadUnsignedShort(currentOffset + 1)];
                    int nameAndTypeCpInfoOffset = _cpInfoOffsets[ReadUnsignedShort(cpInfoOffset + 2)];
                    string name = ReadUtf8(nameAndTypeCpInfoOffset, charBuffer);
                    string descriptor = ReadUtf8(nameAndTypeCpInfoOffset + 2, charBuffer);
                    int bootstrapMethodOffset = _bootstrapMethodOffsets[ReadUnsignedShort(cpInfoOffset)];
                    Handle handle = (Handle)ReadConst(ReadUnsignedShort(bootstrapMethodOffset), charBuffer);
                    object[] bootstrapMethodArguments = new object[ReadUnsignedShort(bootstrapMethodOffset + 2)];
                    bootstrapMethodOffset += 4;
                    for (int i = 0; i < bootstrapMethodArguments.Length; i++)
                    {
                        bootstrapMethodArguments[i] =
                            ReadConst(ReadUnsignedShort(bootstrapMethodOffset), charBuffer);
                        bootstrapMethodOffset += 2;
                    }

                    methodVisitor.VisitInvokeDynamicInsn(name, descriptor, handle, bootstrapMethodArguments);
                    currentOffset += 5;
                    break;
                }
                case Opcodes.New:
                case Opcodes.Anewarray:
                case Opcodes.Checkcast:
                case Opcodes.Instanceof:
                    methodVisitor.VisitTypeInsn(opcode, ReadClass(currentOffset + 1, charBuffer));
                    currentOffset += 3;
                    break;
                case Opcodes.Iinc:
                    methodVisitor.VisitIincInsn(classBuffer[currentOffset + 1] & 0xFF,
                        classBuffer[currentOffset + 2]);
                    currentOffset += 3;
                    break;
                case Opcodes.Multianewarray:
                    methodVisitor.VisitMultiANewArrayInsn(ReadClass(currentOffset + 1, charBuffer),
                        classBuffer[currentOffset + 3] & 0xFF);
                    currentOffset += 4;
                    break;
                default:
                    throw new Exception("AssertionError");
            }

            // Visit the runtime visible instruction annotations, if any.
            while (visibleTypeAnnotationOffsets != null &&
                   currentVisibleTypeAnnotationIndex < visibleTypeAnnotationOffsets.Length &&
                   currentVisibleTypeAnnotationBytecodeOffset <= currentBytecodeOffset)
            {
                if (currentVisibleTypeAnnotationBytecodeOffset == currentBytecodeOffset)
                {
                    // Parse the target_type, target_info and target_path fields.
                    int currentAnnotationOffset = ReadTypeAnnotationTarget(context,
                        visibleTypeAnnotationOffsets[currentVisibleTypeAnnotationIndex]);
                    // Parse the type_index field.
                    string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                    currentAnnotationOffset += 2;
                    // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                    ReadElementValues(
                        methodVisitor.VisitInsnAnnotation(context.currentTypeAnnotationTarget,
                            context.currentTypeAnnotationTargetPath, annotationDescriptor, true),
                        currentAnnotationOffset, true, charBuffer);
                }

                currentVisibleTypeAnnotationBytecodeOffset =
                    GetTypeAnnotationBytecodeOffset(visibleTypeAnnotationOffsets,
                        ++currentVisibleTypeAnnotationIndex);
            }

            // Visit the runtime invisible instruction annotations, if any.
            while (invisibleTypeAnnotationOffsets != null &&
                   currentInvisibleTypeAnnotationIndex < invisibleTypeAnnotationOffsets.Length &&
                   currentInvisibleTypeAnnotationBytecodeOffset <= currentBytecodeOffset)
            {
                if (currentInvisibleTypeAnnotationBytecodeOffset == currentBytecodeOffset)
                {
                    // Parse the target_type, target_info and target_path fields.
                    int currentAnnotationOffset = ReadTypeAnnotationTarget(context,
                        invisibleTypeAnnotationOffsets[currentInvisibleTypeAnnotationIndex]);
                    // Parse the type_index field.
                    string annotationDescriptor = ReadUtf8(currentAnnotationOffset, charBuffer);
                    currentAnnotationOffset += 2;
                    // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                    ReadElementValues(
                        methodVisitor.VisitInsnAnnotation(context.currentTypeAnnotationTarget,
                            context.currentTypeAnnotationTargetPath, annotationDescriptor, false),
                        currentAnnotationOffset, true, charBuffer);
                }

                currentInvisibleTypeAnnotationBytecodeOffset =
                    GetTypeAnnotationBytecodeOffset(invisibleTypeAnnotationOffsets,
                        ++currentInvisibleTypeAnnotationIndex);
            }
        }

        if (labels[codeLength] != null)
        {
            methodVisitor.VisitLabel(labels[codeLength]);
        }

        // Visit LocalVariableTable and LocalVariableTypeTable attributes.
        if (localVariableTableOffset != 0 && (context.parsingOptions & Skip_Debug) == 0)
        {
            // The (start_pc, index, signature_index) fields of each entry of the LocalVariableTypeTable.
            int[] typeTable = null;
            if (localVariableTypeTableOffset != 0)
            {
                typeTable = new int[ReadUnsignedShort(localVariableTypeTableOffset) * 3];
                currentOffset = localVariableTypeTableOffset + 2;
                int typeTableIndex = typeTable.Length;
                while (typeTableIndex > 0)
                {
                    // Store the offset of 'signature_index', and the value of 'index' and 'start_pc'.
                    typeTable[--typeTableIndex] = currentOffset + 6;
                    typeTable[--typeTableIndex] = ReadUnsignedShort(currentOffset + 8);
                    typeTable[--typeTableIndex] = ReadUnsignedShort(currentOffset);
                    currentOffset += 10;
                }
            }

            int localVariableTableLength = ReadUnsignedShort(localVariableTableOffset);
            currentOffset = localVariableTableOffset + 2;
            while (localVariableTableLength-- > 0)
            {
                int startPc = ReadUnsignedShort(currentOffset);
                int length = ReadUnsignedShort(currentOffset + 2);
                string name = ReadUtf8(currentOffset + 4, charBuffer);
                string descriptor = ReadUtf8(currentOffset + 6, charBuffer);
                int index = ReadUnsignedShort(currentOffset + 8);
                currentOffset += 10;
                string signature = null;
                if (typeTable != null)
                {
                    for (int i = 0; i < typeTable.Length; i += 3)
                    {
                        if (typeTable[i] == startPc && typeTable[i + 1] == index)
                        {
                            signature = ReadUtf8(typeTable[i + 2], charBuffer);
                            break;
                        }
                    }
                }

                methodVisitor.VisitLocalVariable(name, descriptor, signature, labels[startPc],
                    labels[startPc + length], index);
            }
        }

        // Visit the local variable type annotations of the RuntimeVisibleTypeAnnotations attribute.
        if (visibleTypeAnnotationOffsets != null)
        {
            foreach (int typeAnnotationOffset in visibleTypeAnnotationOffsets)
            {
                int targetType = ReadByte(typeAnnotationOffset);
                if (targetType == TypeReference.Local_Variable || targetType == TypeReference.Resource_Variable)
                {
                    // Parse the target_type, target_info and target_path fields.
                    currentOffset = ReadTypeAnnotationTarget(context, typeAnnotationOffset);
                    // Parse the type_index field.
                    string annotationDescriptor = ReadUtf8(currentOffset, charBuffer);
                    currentOffset += 2;
                    // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                    ReadElementValues(
                        methodVisitor.VisitLocalVariableAnnotation(context.currentTypeAnnotationTarget,
                            context.currentTypeAnnotationTargetPath,
                            context.currentLocalVariableAnnotationRangeStarts,
                            context.currentLocalVariableAnnotationRangeEnds,
                            context.currentLocalVariableAnnotationRangeIndices, annotationDescriptor, true),
                        currentOffset, true, charBuffer);
                }
            }
        }

        // Visit the local variable type annotations of the RuntimeInvisibleTypeAnnotations attribute.
        if (invisibleTypeAnnotationOffsets != null)
        {
            foreach (int typeAnnotationOffset in invisibleTypeAnnotationOffsets)
            {
                int targetType = ReadByte(typeAnnotationOffset);
                if (targetType == TypeReference.Local_Variable || targetType == TypeReference.Resource_Variable)
                {
                    // Parse the target_type, target_info and target_path fields.
                    currentOffset = ReadTypeAnnotationTarget(context, typeAnnotationOffset);
                    // Parse the type_index field.
                    string annotationDescriptor = ReadUtf8(currentOffset, charBuffer);
                    currentOffset += 2;
                    // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                    ReadElementValues(
                        methodVisitor.VisitLocalVariableAnnotation(context.currentTypeAnnotationTarget,
                            context.currentTypeAnnotationTargetPath,
                            context.currentLocalVariableAnnotationRangeStarts,
                            context.currentLocalVariableAnnotationRangeEnds,
                            context.currentLocalVariableAnnotationRangeIndices, annotationDescriptor, false),
                        currentOffset, true, charBuffer);
                }
            }
        }

        // Visit the non standard attributes.
        while (attributes != null)
        {
            // Copy and reset the nextAttribute field so that it can also be used in MethodWriter.
            Attribute nextAttribute = attributes.nextAttribute;
            attributes.nextAttribute = null;
            methodVisitor.VisitAttribute(attributes);
            attributes = nextAttribute;
        }

        // Visit the max stack and max locals Values.
        methodVisitor.VisitMaxs(maxStack, maxLocals);
    }

    /// <summary>
    /// Handles the bytecode offset of the next instruction to be visited in <see cref="Accept(ClassVisitor, int)"/>.
    /// This method is called just before the instruction and before its
    /// associated label and stack map frame, if any. The default implementation of this method does
    /// nothing. Subclasses can override this method to store the argument in a mutable field, for
    /// instance, so that <see cref="MethodVisitor"/> instances can get the bytecode offset of each visited
    /// instruction (if so, the usual concurrency issues related to mutable data should be addressed).
    /// </summary>
    /// <param name="bytecodeOffset">the bytecode offset of the next instruction to be visited.</param>
    protected void ReadBytecodeInstructionOffset(int bytecodeOffset)
    {
        // Do nothing by default.
    }

    /// <summary>
    /// Returns the label corresponding to the given bytecode offset. The default implementation of
    /// this method creates a label for the given offset if it has not been already created.
    /// </summary>
    /// <param name="bytecodeOffset"> a bytecode offset in a method. </param>
    /// <param name="labels"> the already created labels, indexed by their offset. If a label already exists
    ///     for bytecodeOffset this method must not create a new one. Otherwise it must store the new
    ///     label in this array. </param>
    /// <returns> a non null Label, which must be equal to labels[bytecodeOffset]. </returns>
    public virtual Label ReadLabel(int bytecodeOffset, Label[] labels)
    {
        if (labels[bytecodeOffset] == null)
        {
            labels[bytecodeOffset] = new Label();
        }

        return labels[bytecodeOffset];
    }

    /// <summary>
    /// Creates a label without the <see cref="Label.Flag_Debug_Only"/> flag set, for the given bytecode
    /// offset. The label is created with a call to <see cref="ReadLabel"/> and its <see cref="Label.FLAG_DEBUG_ONLY"/> flag is cleared.
    /// </summary>
    /// <param name="bytecodeOffset"> a bytecode offset in a method. </param>
    /// <param name="labels"> the already created labels, indexed by their offset. </param>
    /// <returns> a Label without the <see cref="Label.Flag_Debug_Only"/> flag set. </returns>
    private Label CreateLabel(int bytecodeOffset, Label[] labels)
    {
        Label label = ReadLabel(bytecodeOffset, labels);
        label.flags &= (short)(~Label.Flag_Debug_Only);
        return label;
    }

    /// <summary>
    /// Creates a label with the <see cref="Label.Flag_Debug_Only"/> flag set, if there is no already
    /// existing label for the given bytecode offset (otherwise does nothing). The label is created
    /// with a call to <see cref="ReadLabel"/>.
    /// </summary>
    /// <param name="bytecodeOffset"> a bytecode offset in a method. </param>
    /// <param name="labels"> the already created labels, indexed by their offset. </param>
    private void CreateDebugLabel(int bytecodeOffset, Label[] labels)
    {
        if (labels[bytecodeOffset] == null)
        {
            ReadLabel(bytecodeOffset, labels).flags |= (short)Label.Flag_Debug_Only;
        }
    }

    // ----------------------------------------------------------------------------------------------
    // Methods to parse annotations, type annotations and parameter annotations
    // ----------------------------------------------------------------------------------------------

    /// <summary>
    /// Parses a Runtime[In]VisibleTypeAnnotations attribute to find the offset of each type_annotation
    /// entry it contains, to find the corresponding labels, and to visit the try catch block
    /// annotations.
    /// </summary>
    /// <param name="methodVisitor"> the method visitor to be used to visit the try catch block annotations. </param>
    /// <param name="context"> information about the class being parsed. </param>
    /// <param name="runtimeTypeAnnotationsOffset"> the start offset of a Runtime[In]VisibleTypeAnnotations
    ///     attribute, excluding the attribute_info's attribute_name_index and attribute_length fields. </param>
    /// <param name="visible"> true if the attribute to parse is a RuntimeVisibleTypeAnnotations attribute,
    ///     false it is a RuntimeInvisibleTypeAnnotations attribute. </param>
    /// <returns> the start offset of each entry of the Runtime[In]VisibleTypeAnnotations_attribute's
    ///     'annotations' array field. </returns>
    private int[] ReadTypeAnnotations(MethodVisitor methodVisitor, Context context,
        int runtimeTypeAnnotationsOffset, bool visible)
    {
        char[] charBuffer = context.charBuffer;
        int currentOffset = runtimeTypeAnnotationsOffset;
        // Read the num_annotations field and create an array to store the type_annotation offsets.
        int[] typeAnnotationsOffsets = new int[ReadUnsignedShort(currentOffset)];
        currentOffset += 2;
        // Parse the 'annotations' array field.
        for (int i = 0; i < typeAnnotationsOffsets.Length; ++i)
        {
            typeAnnotationsOffsets[i] = currentOffset;
            // Parse the type_annotation's target_type and the target_info fields. The size of the
            // target_info field depends on the value of target_type.
            int targetType = ReadInt(currentOffset);
            switch (targetType >>> 24)
            {
                case TypeReference.Local_Variable:
                case TypeReference.Resource_Variable:
                    // A localvar_target has a variable size, which depends on the value of their table_length
                    // field. It also references bytecode offsets, for which we need labels.
                    int tableLength = ReadUnsignedShort(currentOffset + 1);
                    currentOffset += 3;
                    while (tableLength-- > 0)
                    {
                        int startPc = ReadUnsignedShort(currentOffset);
                        int length = ReadUnsignedShort(currentOffset + 2);
                        // Skip the index field (2 bytes).
                        currentOffset += 6;
                        CreateLabel(startPc, context.currentMethodLabels);
                        CreateLabel(startPc + length, context.currentMethodLabels);
                    }

                    break;
                case TypeReference.Cast:
                case TypeReference.Constructor_Invocation_Type_Argument:
                case TypeReference.Method_Invocation_Type_Argument:
                case TypeReference.Constructor_Reference_Type_Argument:
                case TypeReference.Method_Reference_Type_Argument:
                    currentOffset += 4;
                    break;
                case TypeReference.Class_Extends:
                case TypeReference.Class_Type_Parameter_Bound:
                case TypeReference.Method_Type_Parameter_Bound:
                case TypeReference.Throws:
                case TypeReference.Exception_Parameter:
                case TypeReference.Instanceof:
                case TypeReference.New:
                case TypeReference.Constructor_Reference:
                case TypeReference.Method_Reference:
                    currentOffset += 3;
                    break;
                case TypeReference.Class_Type_Parameter:
                case TypeReference.Method_Type_Parameter:
                case TypeReference.Method_Formal_Parameter:
                case TypeReference.Field:
                case TypeReference.Method_Return:
                case TypeReference.Method_Receiver:
                default:
                    // TypeReference type which can't be used in Code attribute, or which is unknown.
                    throw new System.ArgumentException();
            }

            // Parse the rest of the type_annotation structure, starting with the target_path structure
            // (whose size depends on its path_length field).
            int pathLength = ReadByte(currentOffset);
            if ((targetType >>> 24) == TypeReference.Exception_Parameter)
            {
                // Parse the target_path structure and create a corresponding TypePath.
                TypePath path = pathLength == 0 ? null : new TypePath(classFileBuffer, currentOffset);
                currentOffset += 1 + 2 * pathLength;
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentOffset, charBuffer);
                currentOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentOffset =
                    ReadElementValues(
                        methodVisitor.VisitTryCatchAnnotation(targetType & unchecked((int)0xFFFFFF00), path,
                            annotationDescriptor, visible), currentOffset, true, charBuffer);
            }
            else
            {
                // We don't want to visit the other target_type annotations, so we just skip them (which
                // requires some parsing because the element_value_pairs array has a variable size). First,
                // skip the target_path structure:
                currentOffset += 3 + 2 * pathLength;
                // Then skip the num_element_value_pairs and element_value_pairs fields (by reading them
                // with a null AnnotationVisitor).
                currentOffset = ReadElementValues(null, currentOffset, true, charBuffer);
            }
        }

        return typeAnnotationsOffsets;
    }

    /// <summary>
    /// Returns the bytecode offset corresponding to the specified JVMS 'type_annotation' structure, or
    /// -1 if there is no such type_annotation of if it does not have a bytecode offset.
    /// </summary>
    /// <param name="typeAnnotationOffsets"> the offset of each 'type_annotation' entry in a
    ///     Runtime[In]VisibleTypeAnnotations attribute, or null. </param>
    /// <param name="typeAnnotationIndex"> the index a 'type_annotation' entry in typeAnnotationOffsets. </param>
    /// <returns> bytecode offset corresponding to the specified JVMS 'type_annotation' structure, or -1
    ///     if there is no such type_annotation of if it does not have a bytecode offset. </returns>
    private int GetTypeAnnotationBytecodeOffset(int[] typeAnnotationOffsets, int typeAnnotationIndex)
    {
        if (typeAnnotationOffsets == null || typeAnnotationIndex >= typeAnnotationOffsets.Length ||
            ReadByte(typeAnnotationOffsets[typeAnnotationIndex]) < TypeReference.Instanceof)
        {
            return -1;
        }

        return ReadUnsignedShort(typeAnnotationOffsets[typeAnnotationIndex] + 1);
    }

    /// <summary>
    /// Parses the header of a JVMS type_annotation structure to extract its target_type, target_info
    /// and target_path (the result is stored in the given context), and returns the start offset of
    /// the rest of the type_annotation structure.
    /// </summary>
    /// <param name="context"> information about the class being parsed. This is where the extracted
    ///     target_type and target_path must be stored. </param>
    /// <param name="typeAnnotationOffset"> the start offset of a type_annotation structure. </param>
    /// <returns> the start offset of the rest of the type_annotation structure. </returns>
    private int ReadTypeAnnotationTarget(Context context, int typeAnnotationOffset)
    {
        int currentOffset = typeAnnotationOffset;
        // Parse and store the target_type structure.
        int targetType = ReadInt(typeAnnotationOffset);
        switch (targetType >>> 24)
        {
            case TypeReference.Class_Type_Parameter:
            case TypeReference.Method_Type_Parameter:
            case TypeReference.Method_Formal_Parameter:
                targetType &= unchecked((int)0xFFFF0000);
                currentOffset += 2;
                break;
            case TypeReference.Field:
            case TypeReference.Method_Return:
            case TypeReference.Method_Receiver:
                targetType &= unchecked((int)0xFF000000);
                currentOffset += 1;
                break;
            case TypeReference.Local_Variable:
            case TypeReference.Resource_Variable:
                targetType &= unchecked((int)0xFF000000);
                int tableLength = ReadUnsignedShort(currentOffset + 1);
                currentOffset += 3;
                context.currentLocalVariableAnnotationRangeStarts = new Label[tableLength];
                context.currentLocalVariableAnnotationRangeEnds = new Label[tableLength];
                context.currentLocalVariableAnnotationRangeIndices = new int[tableLength];
                for (int i = 0; i < tableLength; ++i)
                {
                    int startPc = ReadUnsignedShort(currentOffset);
                    int length = ReadUnsignedShort(currentOffset + 2);
                    int index = ReadUnsignedShort(currentOffset + 4);
                    currentOffset += 6;
                    context.currentLocalVariableAnnotationRangeStarts[i] =
                        CreateLabel(startPc, context.currentMethodLabels);
                    context.currentLocalVariableAnnotationRangeEnds[i] =
                        CreateLabel(startPc + length, context.currentMethodLabels);
                    context.currentLocalVariableAnnotationRangeIndices[i] = index;
                }

                break;
            case TypeReference.Cast:
            case TypeReference.Constructor_Invocation_Type_Argument:
            case TypeReference.Method_Invocation_Type_Argument:
            case TypeReference.Constructor_Reference_Type_Argument:
            case TypeReference.Method_Reference_Type_Argument:
                targetType &= unchecked((int)0xFF0000FF);
                currentOffset += 4;
                break;
            case TypeReference.Class_Extends:
            case TypeReference.Class_Type_Parameter_Bound:
            case TypeReference.Method_Type_Parameter_Bound:
            case TypeReference.Throws:
            case TypeReference.Exception_Parameter:
                targetType &= unchecked((int)0xFFFFFF00);
                currentOffset += 3;
                break;
            case TypeReference.Instanceof:
            case TypeReference.New:
            case TypeReference.Constructor_Reference:
            case TypeReference.Method_Reference:
                targetType &= unchecked((int)0xFF000000);
                currentOffset += 3;
                break;
            default:
                throw new System.ArgumentException();
        }

        context.currentTypeAnnotationTarget = targetType;
        // Parse and store the target_path structure.
        int pathLength = ReadByte(currentOffset);
        context.currentTypeAnnotationTargetPath =
            pathLength == 0 ? null : new TypePath(classFileBuffer, currentOffset);
        // Return the start offset of the rest of the type_annotation structure.
        return currentOffset + 1 + 2 * pathLength;
    }

    /// <summary>
    /// Reads a Runtime[In]VisibleParameterAnnotations attribute and makes the given visitor visit it.
    /// </summary>
    /// <param name="methodVisitor"> the visitor that must visit the parameter annotations. </param>
    /// <param name="context"> information about the class being parsed. </param>
    /// <param name="runtimeParameterAnnotationsOffset"> the start offset of a
    ///     Runtime[In]VisibleParameterAnnotations attribute, excluding the attribute_info's
    ///     attribute_name_index and attribute_length fields. </param>
    /// <param name="visible"> true if the attribute to parse is a RuntimeVisibleParameterAnnotations
    ///     attribute, false it is a RuntimeInvisibleParameterAnnotations attribute. </param>
    private void ReadParameterAnnotations(MethodVisitor methodVisitor, Context context,
        int runtimeParameterAnnotationsOffset, bool visible)
    {
        int currentOffset = runtimeParameterAnnotationsOffset;
        int numParameters = classFileBuffer[currentOffset++] & 0xFF;
        methodVisitor.VisitAnnotableParameterCount(numParameters, visible);
        char[] charBuffer = context.charBuffer;
        for (int i = 0; i < numParameters; ++i)
        {
            int numAnnotations = ReadUnsignedShort(currentOffset);
            currentOffset += 2;
            while (numAnnotations-- > 0)
            {
                // Parse the type_index field.
                string annotationDescriptor = ReadUtf8(currentOffset, charBuffer);
                currentOffset += 2;
                // Parse num_element_value_pairs and element_value_pairs and visit these Values.
                currentOffset =
                    ReadElementValues(methodVisitor.VisitParameterAnnotation(i, annotationDescriptor, visible),
                        currentOffset, true, charBuffer);
            }
        }
    }

    /// <summary>
    /// Reads the element Values of a JVMS 'annotation' structure and makes the given visitor visit
    /// them. This method can also be used to read the Values of the JVMS 'array_value' field of an
    /// annotation's 'element_value'.
    /// </summary>
    /// <param name="annotationVisitor"> the visitor that must visit the Values. </param>
    /// <param name="annotationOffset"> the start offset of an 'annotation' structure (excluding its type_index
    ///     field) or of an 'array_value' structure. </param>
    /// <param name="named"> if the annotation Values are named or not. This should be true to parse the Values
    ///     of a JVMS 'annotation' structure, and false to parse the JVMS 'array_value' of an
    ///     annotation's element_value. </param>
    /// <param name="charBuffer"> the buffer used to read strings in the constant pool. </param>
    /// <returns> the end offset of the JVMS 'annotation' or 'array_value' structure. </returns>
    private int ReadElementValues(AnnotationVisitor annotationVisitor, int annotationOffset, bool named,
        char[] charBuffer)
    {
        int currentOffset = annotationOffset;
        // Read the num_element_value_pairs field (or num_values field for an array_value).
        int numElementValuePairs = ReadUnsignedShort(currentOffset);
        currentOffset += 2;
        if (named)
        {
            // Parse the element_value_pairs array.
            while (numElementValuePairs-- > 0)
            {
                string elementName = ReadUtf8(currentOffset, charBuffer);
                currentOffset = ReadElementValue(annotationVisitor, currentOffset + 2, elementName, charBuffer);
            }
        }
        else
        {
            // Parse the array_value array.
            while (numElementValuePairs-- > 0)
            {
                currentOffset = ReadElementValue(annotationVisitor, currentOffset, null, charBuffer);
            }
        }

        if (annotationVisitor != null)
        {
            annotationVisitor.VisitEnd();
        }

        return currentOffset;
    }

    /// <summary>
    /// Reads a JVMS 'element_value' structure and makes the given visitor visit it.
    /// </summary>
    /// <param name="annotationVisitor"> the visitor that must visit the element_value structure. </param>
    /// <param name="elementValueOffset"> the start offset in <see cref="classFileBuffer"/> of the element_value
    ///     structure to be read. </param>
    /// <param name="elementName"> the name of the element_value structure to be read, or null. </param>
    /// <param name="charBuffer"> the buffer used to read strings in the constant pool. </param>
    /// <returns> the end offset of the JVMS 'element_value' structure. </returns>
    private int ReadElementValue(AnnotationVisitor annotationVisitor, int elementValueOffset, string elementName,
        char[] charBuffer)
    {
        int currentOffset = elementValueOffset;
        if (annotationVisitor == null)
        {
            switch (classFileBuffer[currentOffset] & 0xFF)
            {
                case 'e': // enum_const_value
                    return currentOffset + 5;
                case '@': // annotation_value
                    return ReadElementValues(null, currentOffset + 3, true, charBuffer);
                case '[': // array_value
                    return ReadElementValues(null, currentOffset + 1, false, charBuffer);
                default:
                    return currentOffset + 3;
            }
        }

        switch (classFileBuffer[currentOffset++] & 0xFF)
        {
            case 'B': // const_value_index, CONSTANT_Integer
                annotationVisitor.Visit(elementName,
                    (sbyte)ReadInt(_cpInfoOffsets[ReadUnsignedShort(currentOffset)]));
                currentOffset += 2;
                break;
            case 'C': // const_value_index, CONSTANT_Integer
                annotationVisitor.Visit(elementName,
                    (char)ReadInt(_cpInfoOffsets[ReadUnsignedShort(currentOffset)]));
                currentOffset += 2;
                break;
            case 'D': // const_value_index, CONSTANT_Double
            case 'F': // const_value_index, CONSTANT_Float
            case 'I': // const_value_index, CONSTANT_Integer
            case 'J': // const_value_index, CONSTANT_Long
                annotationVisitor.Visit(elementName, ReadConst(ReadUnsignedShort(currentOffset), charBuffer));
                currentOffset += 2;
                break;
            case 'S': // const_value_index, CONSTANT_Integer
                annotationVisitor.Visit(elementName,
                    (short)ReadInt(_cpInfoOffsets[ReadUnsignedShort(currentOffset)]));
                currentOffset += 2;
                break;

            case 'Z': // const_value_index, CONSTANT_Integer
                annotationVisitor.Visit(elementName,
                    ReadInt(_cpInfoOffsets[ReadUnsignedShort(currentOffset)]) == 0 ? false : true);
                currentOffset += 2;
                break;
            case 's': // const_value_index, CONSTANT_Utf8
                annotationVisitor.Visit(elementName, ReadUtf8(currentOffset, charBuffer));
                currentOffset += 2;
                break;
            case 'e': // enum_const_value
                annotationVisitor.VisitEnum(elementName, ReadUtf8(currentOffset, charBuffer),
                    ReadUtf8(currentOffset + 2, charBuffer));
                currentOffset += 4;
                break;
            case 'c': // class_info
                annotationVisitor.Visit(elementName, JType.GetType(ReadUtf8(currentOffset, charBuffer)));
                currentOffset += 2;
                break;
            case '@': // annotation_value
                currentOffset =
                    ReadElementValues(
                        annotationVisitor.VisitAnnotation(elementName, ReadUtf8(currentOffset, charBuffer)),
                        currentOffset + 2, true, charBuffer);
                break;
            case '[': // array_value
                int numValues = ReadUnsignedShort(currentOffset);
                currentOffset += 2;
                if (numValues == 0)
                {
                    return ReadElementValues(annotationVisitor.VisitArray(elementName), currentOffset - 2, false,
                        charBuffer);
                }

                switch (classFileBuffer[currentOffset] & 0xFF)
                {
                    case 'B':
                        sbyte[] byteValues = new sbyte[numValues];
                        for (int i = 0; i < numValues; i++)
                        {
                            byteValues[i] = (sbyte)ReadInt(_cpInfoOffsets[ReadUnsignedShort(currentOffset + 1)]);
                            currentOffset += 3;
                        }

                        annotationVisitor.Visit(elementName, byteValues);
                        break;
                    case 'Z':
                        bool[] booleanValues = new bool[numValues];
                        for (int i = 0; i < numValues; i++)
                        {
                            booleanValues[i] = ReadInt(_cpInfoOffsets[ReadUnsignedShort(currentOffset + 1)]) != 0;
                            currentOffset += 3;
                        }

                        annotationVisitor.Visit(elementName, booleanValues);
                        break;
                    case 'S':
                        short[] shortValues = new short[numValues];
                        for (int i = 0; i < numValues; i++)
                        {
                            shortValues[i] = (short)ReadInt(_cpInfoOffsets[ReadUnsignedShort(currentOffset + 1)]);
                            currentOffset += 3;
                        }

                        annotationVisitor.Visit(elementName, shortValues);
                        break;
                    case 'C':
                        char[] charValues = new char[numValues];
                        for (int i = 0; i < numValues; i++)
                        {
                            charValues[i] = (char)ReadInt(_cpInfoOffsets[ReadUnsignedShort(currentOffset + 1)]);
                            currentOffset += 3;
                        }

                        annotationVisitor.Visit(elementName, charValues);
                        break;
                    case 'I':
                        int[] intValues = new int[numValues];
                        for (int i = 0; i < numValues; i++)
                        {
                            intValues[i] = ReadInt(_cpInfoOffsets[ReadUnsignedShort(currentOffset + 1)]);
                            currentOffset += 3;
                        }

                        annotationVisitor.Visit(elementName, intValues);
                        break;
                    case 'J':
                        long[] longValues = new long[numValues];
                        for (int i = 0; i < numValues; i++)
                        {
                            longValues[i] = ReadLong(_cpInfoOffsets[ReadUnsignedShort(currentOffset + 1)]);
                            currentOffset += 3;
                        }

                        annotationVisitor.Visit(elementName, longValues);
                        break;
                    case 'F':
                        float[] floatValues = new float[numValues];
                        for (int i = 0; i < numValues; i++)
                        {
                            floatValues[i] =
                                Int32AndSingleConverter.Convert(
                                    ReadInt(_cpInfoOffsets[ReadUnsignedShort(currentOffset + 1)]));
                            currentOffset += 3;
                        }

                        annotationVisitor.Visit(elementName, floatValues);
                        break;
                    case 'D':
                        double[] doubleValues = new double[numValues];
                        for (int i = 0; i < numValues; i++)
                        {
                            doubleValues[i] =
                                BitConverter.Int64BitsToDouble(
                                    ReadLong(_cpInfoOffsets[ReadUnsignedShort(currentOffset + 1)]));
                            currentOffset += 3;
                        }

                        annotationVisitor.Visit(elementName, doubleValues);
                        break;
                    default:
                        currentOffset = ReadElementValues(annotationVisitor.VisitArray(elementName),
                            currentOffset - 2, false, charBuffer);
                        break;
                }

                break;
            default:
                throw new System.ArgumentException();
        }

        return currentOffset;
    }

    // ----------------------------------------------------------------------------------------------
    // Methods to parse stack map frames
    // ----------------------------------------------------------------------------------------------

    /// <summary>
    /// Computes the implicit frame of the method currently being parsed (as defined in the given
    /// <see cref="Context"/>) and stores it in the given context.
    /// </summary>
    /// <param name="context"> information about the class being parsed. </param>
    private void ComputeImplicitFrame(Context context)
    {
        string methodDescriptor = context.currentMethodDescriptor;
        object[] locals = context.currentFrameLocalTypes;
        int numLocal = 0;
        if ((context.currentMethodAccessFlags & Opcodes.Acc_Static) == 0)
        {
            if ("<init>".Equals(context.currentMethodName))
            {
                locals[numLocal++] = Opcodes.uninitializedThis;
            }
            else
            {
                locals[numLocal++] = ReadClass(header + 2, context.charBuffer);
            }
        }

        // Parse the method descriptor, one argument type descriptor at each iteration. Start by
        // skipping the first method descriptor character, which is always '('.
        int currentMethodDescritorOffset = 1;
        while (true)
        {
            int currentArgumentDescriptorStartOffset = currentMethodDescritorOffset;
            switch (methodDescriptor[currentMethodDescritorOffset++])
            {
                case 'Z':
                case 'C':
                case 'B':
                case 'S':
                case 'I':
                    locals[numLocal++] = Opcodes.integer;
                    break;
                case 'F':
                    locals[numLocal++] = Opcodes.@float;
                    break;
                case 'J':
                    locals[numLocal++] = Opcodes.@long;
                    break;
                case 'D':
                    locals[numLocal++] = Opcodes.@double;
                    break;
                case '[':
                    while (methodDescriptor[currentMethodDescritorOffset] == '[')
                    {
                        ++currentMethodDescritorOffset;
                    }

                    if (methodDescriptor[currentMethodDescritorOffset] == 'L')
                    {
                        ++currentMethodDescritorOffset;
                        while (methodDescriptor[currentMethodDescritorOffset] != ';')
                        {
                            ++currentMethodDescritorOffset;
                        }
                    }

                    locals[numLocal++] = methodDescriptor.Substring(currentArgumentDescriptorStartOffset,
                        (++currentMethodDescritorOffset) - currentArgumentDescriptorStartOffset);
                    break;
                case 'L':
                    while (methodDescriptor[currentMethodDescritorOffset] != ';')
                    {
                        ++currentMethodDescritorOffset;
                    }

                    locals[numLocal++] = methodDescriptor.Substring(currentArgumentDescriptorStartOffset + 1,
                        (currentMethodDescritorOffset++) - (currentArgumentDescriptorStartOffset + 1));
                    break;
                default:
                    context.currentFrameLocalCount = numLocal;
                    return;
            }
        }
    }

    /// <summary>
    /// Reads a JVMS 'stack_map_frame' structure and stores the result in the given <see cref="Context"/>
    /// object. This method can also be used to read a full_frame structure, excluding its frame_type
    /// field (this is used to parse the legacy StackMap attributes).
    /// </summary>
    /// <param name="stackMapFrameOffset"> the start offset in <see cref="classFileBuffer"/> of the
    ///     stack_map_frame_value structure to be read, or the start offset of a full_frame structure
    ///     (excluding its frame_type field). </param>
    /// <param name="compressed"> true to read a 'stack_map_frame' structure, false to read a 'full_frame'
    ///     structure without its frame_type field. </param>
    /// <param name="expand"> if the stack map frame must be expanded. See <see cref="Expand_Frames"/>. </param>
    /// <param name="context"> where the parsed stack map frame must be stored. </param>
    /// <returns> the end offset of the JVMS 'stack_map_frame' or 'full_frame' structure. </returns>
    private int ReadStackMapFrame(int stackMapFrameOffset, bool compressed, bool expand, Context context)
    {
        int currentOffset = stackMapFrameOffset;
        char[] charBuffer = context.charBuffer;
        Label[] labels = context.currentMethodLabels;
        int frameType;
        if (compressed)
        {
            // Read the frame_type field.
            frameType = classFileBuffer[currentOffset++] & 0xFF;
        }
        else
        {
            frameType = Frame.Full_Frame;
            context.currentFrameOffset = -1;
        }

        int offsetDelta;
        context.currentFrameLocalCountDelta = 0;
        if (frameType < Frame.Same_Locals_1_Stack_Item_Frame)
        {
            offsetDelta = frameType;
            context.currentFrameType = Opcodes.F_Same;
            context.currentFrameStackCount = 0;
        }
        else if (frameType < Frame.Reserved)
        {
            offsetDelta = frameType - Frame.Same_Locals_1_Stack_Item_Frame;
            currentOffset = ReadVerificationTypeInfo(currentOffset, context.currentFrameStackTypes, 0, charBuffer,
                labels);
            context.currentFrameType = Opcodes.F_Same1;
            context.currentFrameStackCount = 1;
        }
        else if (frameType >= Frame.Same_Locals_1_Stack_Item_Frame_Extended)
        {
            offsetDelta = ReadUnsignedShort(currentOffset);
            currentOffset += 2;
            if (frameType == Frame.Same_Locals_1_Stack_Item_Frame_Extended)
            {
                currentOffset = ReadVerificationTypeInfo(currentOffset, context.currentFrameStackTypes, 0,
                    charBuffer, labels);
                context.currentFrameType = Opcodes.F_Same1;
                context.currentFrameStackCount = 1;
            }
            else if (frameType >= Frame.Chop_Frame && frameType < Frame.Same_Frame_Extended)
            {
                context.currentFrameType = Opcodes.F_Chop;
                context.currentFrameLocalCountDelta = Frame.Same_Frame_Extended - frameType;
                context.currentFrameLocalCount -= context.currentFrameLocalCountDelta;
                context.currentFrameStackCount = 0;
            }
            else if (frameType == Frame.Same_Frame_Extended)
            {
                context.currentFrameType = Opcodes.F_Same;
                context.currentFrameStackCount = 0;
            }
            else if (frameType < Frame.Full_Frame)
            {
                int local = expand ? context.currentFrameLocalCount : 0;
                for (int k = frameType - Frame.Same_Frame_Extended; k > 0; k--)
                {
                    currentOffset = ReadVerificationTypeInfo(currentOffset, context.currentFrameLocalTypes, local++,
                        charBuffer, labels);
                }

                context.currentFrameType = Opcodes.F_Append;
                context.currentFrameLocalCountDelta = frameType - Frame.Same_Frame_Extended;
                context.currentFrameLocalCount += context.currentFrameLocalCountDelta;
                context.currentFrameStackCount = 0;
            }
            else
            {
                int numberOfLocals = ReadUnsignedShort(currentOffset);
                currentOffset += 2;
                context.currentFrameType = Opcodes.F_Full;
                context.currentFrameLocalCountDelta = numberOfLocals;
                context.currentFrameLocalCount = numberOfLocals;
                for (int local = 0; local < numberOfLocals; ++local)
                {
                    currentOffset = ReadVerificationTypeInfo(currentOffset, context.currentFrameLocalTypes, local,
                        charBuffer, labels);
                }

                int numberOfStackItems = ReadUnsignedShort(currentOffset);
                currentOffset += 2;
                context.currentFrameStackCount = numberOfStackItems;
                for (int stack = 0; stack < numberOfStackItems; ++stack)
                {
                    currentOffset = ReadVerificationTypeInfo(currentOffset, context.currentFrameStackTypes, stack,
                        charBuffer, labels);
                }
            }
        }
        else
        {
            throw new System.ArgumentException();
        }

        context.currentFrameOffset += offsetDelta + 1;
        CreateLabel(context.currentFrameOffset, labels);
        return currentOffset;
    }

    /// <summary>
    /// Reads a JVMS 'verification_type_info' structure and stores it at the given index in the given
    /// array.
    /// </summary>
    /// <param name="verificationTypeInfoOffset"> the start offset of the 'verification_type_info' structure to
    ///     read. </param>
    /// <param name="frame"> the array where the parsed type must be stored. </param>
    /// <param name="index"> the index in 'frame' where the parsed type must be stored. </param>
    /// <param name="charBuffer"> the buffer used to read strings in the constant pool. </param>
    /// <param name="labels"> the labels of the method currently being parsed, indexed by their offset. If the
    ///     parsed type is an ITEM_Uninitialized, a new label for the corresponding NEW instruction is
    ///     stored in this array if it does not already exist. </param>
    /// <returns> the end offset of the JVMS 'verification_type_info' structure. </returns>
    private int ReadVerificationTypeInfo(int verificationTypeInfoOffset, object[] frame, int index,
        char[] charBuffer, Label[] labels)
    {
        int currentOffset = verificationTypeInfoOffset;
        int tag = classFileBuffer[currentOffset++] & 0xFF;
        switch (tag)
        {
            case Frame.Item_Top:
                frame[index] = Opcodes.top;
                break;
            case Frame.Item_Integer:
                frame[index] = Opcodes.integer;
                break;
            case Frame.Item_Float:
                frame[index] = Opcodes.@float;
                break;
            case Frame.Item_Double:
                frame[index] = Opcodes.@double;
                break;
            case Frame.Item_Long:
                frame[index] = Opcodes.@long;
                break;
            case Frame.Item_Null:
                frame[index] = Opcodes.@null;
                break;
            case Frame.Item_Uninitialized_This:
                frame[index] = Opcodes.uninitializedThis;
                break;
            case Frame.Item_Object:
                frame[index] = ReadClass(currentOffset, charBuffer);
                currentOffset += 2;
                break;
            case Frame.Item_Uninitialized:
                frame[index] = CreateLabel(ReadUnsignedShort(currentOffset), labels);
                currentOffset += 2;
                break;
            default:
                throw new System.ArgumentException();
        }

        return currentOffset;
    }

    // ----------------------------------------------------------------------------------------------
    // Methods to parse attributes
    // ----------------------------------------------------------------------------------------------

    /// <summary>
    /// Returns the offset in <see cref="classFileBuffer"/> of the first ClassFile's 'attributes' array
    /// field entry.
    /// </summary>
    /// <returns> the offset in <see cref="classFileBuffer"/> of the first ClassFile's 'attributes' array
    ///     field entry. </returns>
    public int FirstAttributeOffset
    {
        get
        {
            // Skip the access_flags, this_class, super_class, and interfaces_count fields (using 2 bytes
            // each), as well as the interfaces array field (2 bytes per interface).
            int currentOffset = header + 8 + ReadUnsignedShort(header + 6) * 2;

            // Read the fields_count field.
            int fieldsCount = ReadUnsignedShort(currentOffset);
            currentOffset += 2;
            // Skip the 'fields' array field.
            while (fieldsCount-- > 0)
            {
                // Invariant: currentOffset is the offset of a field_info structure.
                // Skip the access_flags, name_index and descriptor_index fields (2 bytes each), and read the
                // attributes_count field.
                int attributesCount = ReadUnsignedShort(currentOffset + 6);
                currentOffset += 8;
                // Skip the 'attributes' array field.
                while (attributesCount-- > 0)
                {
                    // Invariant: currentOffset is the offset of an attribute_info structure.
                    // Read the attribute_length field (2 bytes after the start of the attribute_info) and skip
                    // this many bytes, plus 6 for the attribute_name_index and attribute_length fields
                    // (yielding the total size of the attribute_info structure).
                    currentOffset += 6 + ReadInt(currentOffset + 2);
                }
            }

            // Skip the methods_count and 'methods' fields, using the same method as above.
            int methodsCount = ReadUnsignedShort(currentOffset);
            currentOffset += 2;
            while (methodsCount-- > 0)
            {
                int attributesCount = ReadUnsignedShort(currentOffset + 6);
                currentOffset += 8;
                while (attributesCount-- > 0)
                {
                    currentOffset += 6 + ReadInt(currentOffset + 2);
                }
            }

            // Skip the ClassFile's attributes_count field.
            return currentOffset + 2;
        }
    }

    /// <summary>
    /// Reads the BootstrapMethods attribute to compute the offset of each bootstrap method.
    /// </summary>
    /// <param name="maxStringLength"> a conservative estimate of the maximum length of the strings contained
    ///     in the constant pool of the class. </param>
    /// <returns> the offsets of the bootstrap methods. </returns>
    private int[] ReadBootstrapMethodsAttribute(int maxStringLength)
    {
        char[] charBuffer = new char[maxStringLength];
        int currentAttributeOffset = FirstAttributeOffset;
        for (int i = ReadUnsignedShort(currentAttributeOffset - 2); i > 0; --i)
        {
            // Read the attribute_info's attribute_name and attribute_length fields.
            string attributeName = ReadUtf8(currentAttributeOffset, charBuffer);
            int attributeLength = ReadInt(currentAttributeOffset + 2);
            currentAttributeOffset += 6;
            if (Constants.Bootstrap_Methods.Equals(attributeName))
            {
                // Read the num_bootstrap_methods field and create an array of this size.
                int[] result = new int[ReadUnsignedShort(currentAttributeOffset)];
                // Compute and store the offset of each 'bootstrap_methods' array field entry.
                int currentBootstrapMethodOffset = currentAttributeOffset + 2;
                for (int j = 0; j < result.Length; ++j)
                {
                    result[j] = currentBootstrapMethodOffset;
                    // Skip the bootstrap_method_ref and num_bootstrap_arguments fields (2 bytes each),
                    // as well as the bootstrap_arguments array field (of size num_bootstrap_arguments * 2).
                    currentBootstrapMethodOffset += 4 + ReadUnsignedShort(currentBootstrapMethodOffset + 2) * 2;
                }

                return result;
            }

            currentAttributeOffset += attributeLength;
        }

        throw new System.ArgumentException();
    }

    /// <summary>
    /// Reads a non standard JVMS 'attribute' structure in <see cref="classFileBuffer"/>.
    /// </summary>
    /// <param name="attributePrototypes"> prototypes of the attributes that must be parsed during the visit of
    ///     the class. Any attribute whose type is not equal to the type of one the prototypes will not
    ///     be parsed: its byte array value will be passed unchanged to the ClassWriter. </param>
    /// <param name="type"> the type of the attribute. </param>
    /// <param name="offset"> the start offset of the JVMS 'attribute' structure in <see cref="classFileBuffer"/>.
    ///     The 6 attribute header bytes (attribute_name_index and attribute_length) are not taken into
    ///     account here. </param>
    /// <param name="length"> the length of the attribute's content (excluding the 6 attribute header bytes). </param>
    /// <param name="charBuffer"> the buffer to be used to read strings in the constant pool. </param>
    /// <param name="codeAttributeOffset"> the start offset of the enclosing Code attribute in <see cref="classFileBuffer"/>, or -1 if the attribute to be read is not a code attribute. The 6
    ///     attribute header bytes (attribute_name_index and attribute_length) are not taken into
    ///     account here. </param>
    /// <param name="labels"> the labels of the method's code, or <c>null</c> if the attribute to be read
    ///     is not a code attribute. </param>
    /// <returns> the attribute that has been read. </returns>
    private Attribute ReadAttribute(Attribute[] attributePrototypes, string type, int offset, int length,
        char[] charBuffer, int codeAttributeOffset, Label[] labels)
    {
        foreach (Attribute attributePrototype in attributePrototypes)
        {
            if (attributePrototype.type.Equals(type))
            {
                return attributePrototype.Read(this, offset, length, charBuffer, codeAttributeOffset, labels);
            }
        }

        return (new Attribute(type)).Read(this, offset, length, null, -1, null);
    }

    // -----------------------------------------------------------------------------------------------
    // Utility methods: low level parsing
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Returns the number of entries in the class's constant pool table.
    /// </summary>
    /// <returns> the number of entries in the class's constant pool table. </returns>
    public virtual int ItemCount => _cpInfoOffsets.Length;

    /// <summary>
    /// Returns the start offset in this <see cref="ClassReader"/> of a JVMS 'cp_info' structure (i.e. a
    /// constant pool entry), plus one. <i>This method is intended for <see cref="Attribute"/> sub classes,
    /// and is normally not needed by class generators or adapters.</i>
    /// </summary>
    /// <param name="constantPoolEntryIndex"> the index a constant pool entry in the class's constant pool
    ///     table. </param>
    /// <returns> the start offset in this <see cref="ClassReader"/> of the corresponding JVMS 'cp_info'
    ///     structure, plus one. </returns>
    public virtual int GetItem(int constantPoolEntryIndex)
    {
        return _cpInfoOffsets[constantPoolEntryIndex];
    }

    /// <summary>
    /// Returns a conservative estimate of the maximum length of the strings contained in the class's
    /// constant pool table.
    /// </summary>
    /// <returns> a conservative estimate of the maximum length of the strings contained in the class's
    ///     constant pool table. </returns>
    public virtual int MaxStringLength => _maxStringLength;

    /// <summary>
    /// Reads a byte value in this <see cref="ClassReader"/>. <i>This method is intended for <see cref="Attribute"/> sub classes, and is normally not needed by class generators or adapters.</i>
    /// </summary>
    /// <param name="offset"> the start offset of the value to be read in this <see cref="ClassReader"/>. </param>
    /// <returns> the read value. </returns>
    public virtual int ReadByte(int offset)
    {
        return classFileBuffer[offset] & 0xFF;
    }
    
    /// <summary>
    /// Reads several bytes in this <see cref="ClassReader"/>. <i>This method is intended for
    /// <see cref="Attribute"/> sub classes, and is normally not needed by class generators or adapters.</i>
    /// </summary>
    /// <param name="offset"> the start offset of the bytes to be read in this <see cref="ClassReader"/>. </param>
    /// <param name="length"> the number of bytes to read. </param>
    /// <returns> the read bytes. </returns>
    public virtual sbyte[] ReadBytes(int offset, int length)
    {
        sbyte[] result = new sbyte[length];
        Array.Copy(classFileBuffer, offset, result, 0, length);
        return result;
    }

    /// <summary>
    /// Reads an unsigned short value in this <see cref="ClassReader"/>. <i>This method is intended for
    /// <see cref="Attribute"/> sub classes, and is normally not needed by class generators or adapters.</i>
    /// </summary>
    /// <param name="offset"> the start index of the value to be read in this <see cref="ClassReader"/>. </param>
    /// <returns> the read value. </returns>
    public virtual int ReadUnsignedShort(int offset)
    {
        sbyte[] classBuffer = classFileBuffer;
        return ((classBuffer[offset] & 0xFF) << 8) | (classBuffer[offset + 1] & 0xFF);
    }

    /// <summary>
    /// Reads a signed short value in this <see cref="ClassReader"/>. <i>This method is intended for <see cref="Attribute"/> sub classes, and is normally not needed by class generators or adapters.</i>
    /// </summary>
    /// <param name="offset"> the start offset of the value to be read in this <see cref="ClassReader"/>. </param>
    /// <returns> the read value. </returns>
    public virtual short ReadShort(int offset)
    {
        sbyte[] classBuffer = classFileBuffer;
        return (short)(((classBuffer[offset] & 0xFF) << 8) | (classBuffer[offset + 1] & 0xFF));
    }

    /// <summary>
    /// Reads a signed int value in this <see cref="ClassReader"/>. <i>This method is intended for <see cref="Attribute"/> sub classes, and is normally not needed by class generators or adapters.</i>
    /// </summary>
    /// <param name="offset"> the start offset of the value to be read in this <see cref="ClassReader"/>. </param>
    /// <returns> the read value. </returns>
    public virtual int ReadInt(int offset)
    {
        sbyte[] classBuffer = classFileBuffer;
        return ((classBuffer[offset] & 0xFF) << 24) | ((classBuffer[offset + 1] & 0xFF) << 16) |
               ((classBuffer[offset + 2] & 0xFF) << 8) | (classBuffer[offset + 3] & 0xFF);
    }

    /// <summary>
    /// Reads a signed long value in this <see cref="ClassReader"/>. <i>This method is intended for <see cref="Attribute"/> sub classes, and is normally not needed by class generators or adapters.</i>
    /// </summary>
    /// <param name="offset"> the start offset of the value to be read in this <see cref="ClassReader"/>. </param>
    /// <returns> the read value. </returns>
    public virtual long ReadLong(int offset)
    {
        long l1 = ReadInt(offset);
        long l0 = ReadInt(offset + 4) & 0xFFFFFFFFL;
        return (l1 << 32) | l0;
    }

    /// <summary>
    /// Reads a CONSTANT_Utf8 constant pool entry in this <see cref="ClassReader"/>. <i>This method is
    /// intended for <see cref="Attribute"/> sub classes, and is normally not needed by class generators or
    /// adapters.</i>
    /// </summary>
    /// <param name="offset"> the start offset of an unsigned short value in this <see cref="ClassReader"/>, whose
    ///     value is the index of a CONSTANT_Utf8 entry in the class's constant pool table. </param>
    /// <param name="charBuffer"> the buffer to be used to read the string. This buffer must be sufficiently
    ///     large. It is not automatically resized. </param>
    /// <returns> the String corresponding to the specified CONSTANT_Utf8 entry. </returns>
    // DontCheck(AbbreviationAsWordInName): can't be renamed (for backward binary compatibility).
    public virtual string ReadUtf8(int offset, char[] charBuffer)
    {
        int constantPoolEntryIndex = ReadUnsignedShort(offset);
        if (offset == 0 || constantPoolEntryIndex == 0)
        {
            return null;
        }

        return ReadUtf(constantPoolEntryIndex, charBuffer);
    }

    /// <summary>
    /// Reads a CONSTANT_Utf8 constant pool entry in <see cref="classFileBuffer"/>.
    /// </summary>
    /// <param name="constantPoolEntryIndex"> the index of a CONSTANT_Utf8 entry in the class's constant pool
    ///     table. </param>
    /// <param name="charBuffer"> the buffer to be used to read the string. This buffer must be sufficiently
    ///     large. It is not automatically resized. </param>
    /// <returns> the String corresponding to the specified CONSTANT_Utf8 entry. </returns>
    public string ReadUtf(int constantPoolEntryIndex, char[] charBuffer)
    {
        string value = _constantUtf8Values[constantPoolEntryIndex];
        if (!string.ReferenceEquals(value, null))
        {
            return value;
        }

        int cpInfoOffset = _cpInfoOffsets[constantPoolEntryIndex];
        return _constantUtf8Values[constantPoolEntryIndex] =
            ReadUtf(cpInfoOffset + 2, ReadUnsignedShort(cpInfoOffset), charBuffer);
    }

    /// <summary>
    /// Reads an UTF8 string in <see cref="classFileBuffer"/>.
    /// </summary>
    /// <param name="utfOffset"> the start offset of the UTF8 string to be read. </param>
    /// <param name="utfLength"> the length of the UTF8 string to be read. </param>
    /// <param name="charBuffer"> the buffer to be used to read the string. This buffer must be sufficiently
    ///     large. It is not automatically resized. </param>
    /// <returns> the String corresponding to the specified UTF8 string. </returns>
    private string ReadUtf(int utfOffset, int utfLength, char[] charBuffer)
    {
        int currentOffset = utfOffset;
        int endOffset = currentOffset + utfLength;
        int strLength = 0;
        sbyte[] classBuffer = classFileBuffer;
        while (currentOffset < endOffset)
        {
            int currentByte = classBuffer[currentOffset++];
            if ((currentByte & 0x80) == 0)
            {
                charBuffer[strLength++] = (char)(currentByte & 0x7F);
            }
            else if ((currentByte & 0xE0) == 0xC0)
            {
                charBuffer[strLength++] =
                    (char)(((currentByte & 0x1F) << 6) + (classBuffer[currentOffset++] & 0x3F));
            }
            else
            {
                charBuffer[strLength++] = (char)(((currentByte & 0xF) << 12) +
                                                 ((classBuffer[currentOffset++] & 0x3F) << 6) +
                                                 (classBuffer[currentOffset++] & 0x3F));
            }
        }

        return new string(charBuffer, 0, strLength);
    }

    /// <summary>
    /// Reads a CONSTANT_Class, CONSTANT_String, CONSTANT_MethodType, CONSTANT_Module or
    /// CONSTANT_Package constant pool entry in <see cref="classFileBuffer"/>. <i>This method is intended
    /// for <see cref="Attribute"/> sub classes, and is normally not needed by class generators or
    /// adapters.</i>
    /// </summary>
    /// <param name="offset"> the start offset of an unsigned short value in <see cref="classFileBuffer"/>, whose
    ///     value is the index of a CONSTANT_Class, CONSTANT_String, CONSTANT_MethodType,
    ///     CONSTANT_Module or CONSTANT_Package entry in class's constant pool table. </param>
    /// <param name="charBuffer"> the buffer to be used to read the item. This buffer must be sufficiently
    ///     large. It is not automatically resized. </param>
    /// <returns> the String corresponding to the specified constant pool entry. </returns>
    private string ReadStringish(int offset, char[] charBuffer)
    {
        // Get the start offset of the cp_info structure (plus one), and read the CONSTANT_Utf8 entry
        // designated by the first two bytes of this cp_info.
        return ReadUtf8(_cpInfoOffsets[ReadUnsignedShort(offset)], charBuffer);
    }

    /// <summary>
    /// Reads a CONSTANT_Class constant pool entry in this <see cref="ClassReader"/>. <i>This method is
    /// intended for <see cref="Attribute"/> sub classes, and is normally not needed by class generators or
    /// adapters.</i>
    /// </summary>
    /// <param name="offset"> the start offset of an unsigned short value in this <see cref="ClassReader"/>, whose
    ///     value is the index of a CONSTANT_Class entry in class's constant pool table. </param>
    /// <param name="charBuffer"> the buffer to be used to read the item. This buffer must be sufficiently
    ///     large. It is not automatically resized. </param>
    /// <returns> the String corresponding to the specified CONSTANT_Class entry. </returns>
    public virtual string ReadClass(int offset, char[] charBuffer)
    {
        return ReadStringish(offset, charBuffer);
    }

    /// <summary>
    /// Reads a CONSTANT_Module constant pool entry in this <see cref="ClassReader"/>. <i>This method is
    /// intended for <see cref="Attribute"/> sub classes, and is normally not needed by class generators or
    /// adapters.</i>
    /// </summary>
    /// <param name="offset"> the start offset of an unsigned short value in this <see cref="ClassReader"/>, whose
    ///     value is the index of a CONSTANT_Module entry in class's constant pool table. </param>
    /// <param name="charBuffer"> the buffer to be used to read the item. This buffer must be sufficiently
    ///     large. It is not automatically resized. </param>
    /// <returns> the String corresponding to the specified CONSTANT_Module entry. </returns>
    public virtual string ReadModule(int offset, char[] charBuffer)
    {
        return ReadStringish(offset, charBuffer);
    }

    /// <summary>
    /// Reads a CONSTANT_Package constant pool entry in this <see cref="ClassReader"/>. <i>This method is
    /// intended for <see cref="Attribute"/> sub classes, and is normally not needed by class generators or
    /// adapters.</i>
    /// </summary>
    /// <param name="offset"> the start offset of an unsigned short value in this <see cref="ClassReader"/>, whose
    ///     value is the index of a CONSTANT_Package entry in class's constant pool table. </param>
    /// <param name="charBuffer"> the buffer to be used to read the item. This buffer must be sufficiently
    ///     large. It is not automatically resized. </param>
    /// <returns> the String corresponding to the specified CONSTANT_Package entry. </returns>
    public virtual string ReadPackage(int offset, char[] charBuffer)
    {
        return ReadStringish(offset, charBuffer);
    }

    /// <summary>
    /// Reads a CONSTANT_Dynamic constant pool entry in <see cref="classFileBuffer"/>.
    /// </summary>
    /// <param name="constantPoolEntryIndex"> the index of a CONSTANT_Dynamic entry in the class's constant
    ///     pool table. </param>
    /// <param name="charBuffer"> the buffer to be used to read the string. This buffer must be sufficiently
    ///     large. It is not automatically resized. </param>
    /// <returns> the ConstantDynamic corresponding to the specified CONSTANT_Dynamic entry. </returns>
    private ConstantDynamic ReadConstantDynamic(int constantPoolEntryIndex, char[] charBuffer)
    {
        ConstantDynamic constantDynamic = _constantDynamicValues[constantPoolEntryIndex];
        if (constantDynamic != null)
        {
            return constantDynamic;
        }

        int cpInfoOffset = _cpInfoOffsets[constantPoolEntryIndex];
        int nameAndTypeCpInfoOffset = _cpInfoOffsets[ReadUnsignedShort(cpInfoOffset + 2)];
        string name = ReadUtf8(nameAndTypeCpInfoOffset, charBuffer);
        string descriptor = ReadUtf8(nameAndTypeCpInfoOffset + 2, charBuffer);
        int bootstrapMethodOffset = _bootstrapMethodOffsets[ReadUnsignedShort(cpInfoOffset)];
        Handle handle = (Handle)ReadConst(ReadUnsignedShort(bootstrapMethodOffset), charBuffer);
        object[] bootstrapMethodArguments = new object[ReadUnsignedShort(bootstrapMethodOffset + 2)];
        bootstrapMethodOffset += 4;
        for (int i = 0; i < bootstrapMethodArguments.Length; i++)
        {
            bootstrapMethodArguments[i] = ReadConst(ReadUnsignedShort(bootstrapMethodOffset), charBuffer);
            bootstrapMethodOffset += 2;
        }

        return _constantDynamicValues[constantPoolEntryIndex] =
            new ConstantDynamic(name, descriptor, handle, bootstrapMethodArguments);
    }

    /// <summary>
    /// Reads a numeric or string constant pool entry in this <see cref="ClassReader"/>. <i>This method is
    /// intended for <see cref="Attribute"/> sub classes, and is normally not needed by class generators or
    /// adapters.</i>
    /// </summary>
    /// <param name="constantPoolEntryIndex"> the index of a CONSTANT_Integer, CONSTANT_Float, CONSTANT_Long,
    ///     CONSTANT_Double, CONSTANT_Class, CONSTANT_String, CONSTANT_MethodType,
    ///     CONSTANT_MethodHandle or CONSTANT_Dynamic entry in the class's constant pool. </param>
    /// <param name="charBuffer"> the buffer to be used to read strings. This buffer must be sufficiently
    ///     large. It is not automatically resized. </param>
    /// <returns> the <see cref="int"/>, <see cref="float"/>, <see cref="long"/>, <see cref="double"/>, <see cref="string"/>,
    ///     <see cref="Type"/>, <see cref="Handle"/> or <see cref="ConstantDynamic"/> corresponding to the specified
    ///     constant pool entry. </returns>
    public virtual object ReadConst(int constantPoolEntryIndex, char[] charBuffer)
    {
        int cpInfoOffset = _cpInfoOffsets[constantPoolEntryIndex];
        switch (classFileBuffer[cpInfoOffset - 1])
        {
            case Symbol.Constant_Integer_Tag:
                return ReadInt(cpInfoOffset);
            case Symbol.Constant_Float_Tag:
                return Int32AndSingleConverter.Convert(ReadInt(cpInfoOffset));
            case Symbol.Constant_Long_Tag:
                return ReadLong(cpInfoOffset);
            case Symbol.Constant_Double_Tag:
                return BitConverter.Int64BitsToDouble(ReadLong(cpInfoOffset));
            case Symbol.Constant_Class_Tag:
                return JType.GetObjectType(ReadUtf8(cpInfoOffset, charBuffer));
            case Symbol.Constant_String_Tag:
                return ReadUtf8(cpInfoOffset, charBuffer);
            case Symbol.Constant_Method_Type_Tag:
                return JType.GetMethodType(ReadUtf8(cpInfoOffset, charBuffer));
            case Symbol.Constant_Method_Handle_Tag:
                int referenceKind = ReadByte(cpInfoOffset);
                int referenceCpInfoOffset = _cpInfoOffsets[ReadUnsignedShort(cpInfoOffset + 1)];
                int nameAndTypeCpInfoOffset = _cpInfoOffsets[ReadUnsignedShort(referenceCpInfoOffset + 2)];
                string owner = ReadClass(referenceCpInfoOffset, charBuffer);
                string name = ReadUtf8(nameAndTypeCpInfoOffset, charBuffer);
                string descriptor = ReadUtf8(nameAndTypeCpInfoOffset + 2, charBuffer);
                bool isInterface = classFileBuffer[referenceCpInfoOffset - 1] ==
                                   Symbol.Constant_Interface_Methodref_Tag;
                return new Handle(referenceKind, owner, name, descriptor, isInterface);
            case Symbol.Constant_Dynamic_Tag:
                return ReadConstantDynamic(constantPoolEntryIndex, charBuffer);
            default:
                throw new System.ArgumentException();
        }
    }
}