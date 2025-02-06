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
namespace ObjectWeb.Asm;

/// <summary>
///     The constant pool entries, the BootstrapMethods attribute entries and the (ASM specific) type
///     table entries of a class.
///     @author Eric Bruneton
/// </summary>
/// <seealso cref=
/// <a href="https://docs.oracle.com/javase/specs/jvms/se9/html/jvms-4.html#jvms-4.4">
///     JVMS
///     4.4
/// </a>
/// </seealso>
/// <seealso cref=
/// <a href="https://docs.oracle.com/javase/specs/jvms/se9/html/jvms-4.html#jvms-4.7.23">
///     JVMS
///     4.7.23
/// </a>
/// </seealso>
public sealed class SymbolTable
{
    /// <summary>
    ///     The ClassWriter to which this SymbolTable belongs. This is only used to get access to <see cref="ClassWriter.getCommonSuperClass"/> and to serialize custom attributes with <see cref="Attribute.write"/>.
    /// </summary>
    internal readonly ClassWriter classWriter;

    /// <summary>
    ///     The ClassReader from which this SymbolTable was constructed, or null if it was
    ///     constructed from scratch.
    /// </summary>
    private readonly ClassReader _sourceClassReader;

    /// <summary>
    ///     The number of bootstrap methods in <see cref="_bootstrapMethods" />. Corresponds to the
    ///     BootstrapMethods_attribute's num_bootstrap_methods field value.
    /// </summary>
    private int _bootstrapMethodCount;

    /// <summary>
    ///     The content of the BootstrapMethods attribute 'bootstrap_methods' array corresponding to this
    ///     SymbolTable. Note that the first 6 bytes of the BootstrapMethods_attribute, and its
    ///     num_bootstrap_methods field, are <i>not</i> included.
    /// </summary>
    private ByteVector _bootstrapMethods;

    /// <summary>
    ///     The internal name of the class to which this symbol table belongs.
    /// </summary>
    private string _className;

    /// <summary>
    ///     The content of the ClassFile's constant_pool JVMS structure corresponding to this SymbolTable.
    ///     The ClassFile's constant_pool_count field is <i>not</i> included.
    /// </summary>
    private readonly ByteVector _constantPool;

    /// <summary>
    ///     The number of constant pool items in <see cref="_constantPool" />, plus 1. The first constant pool
    ///     item has index 1, and long and double items count for two items.
    /// </summary>
    private int _constantPoolCount;

    /// <summary>
    ///     A hash set of all the entries in this SymbolTable (this includes the constant pool entries, the
    ///     bootstrap method entries and the type table entries). Each <see cref="Entry" /> instance is stored at
    ///     the array index given by its hash code modulo the array size. If several entries must be stored
    ///     at the same array index, they are linked together via their <see cref="Entry.next" /> field. The
    ///     factory methods of this class make sure that this table does not contain duplicated entries.
    /// </summary>
    private Entry[] _entries;

    /// <summary>
    ///     The total number of <see cref="Entry" /> instances in <see cref="_entries" />. This includes entries that
    ///     are
    ///     accessible (recursively) via <see cref="Entry.next" />.
    /// </summary>
    private int _entryCount;

    /// <summary>
    ///     The major version number of the class to which this symbol table belongs.
    /// </summary>
    private int _majorVersion;

    /// <summary>
    ///     The actual number of elements in <see cref="_typeTable" />. These elements are stored from index 0 to
    ///     typeCount (excluded). The other array entries are empty.
    /// </summary>
    private int _typeCount;

    /// <summary>
    ///     An ASM specific type table used to temporarily store internal names that will not necessarily
    ///     be stored in the constant pool. This type table is used by the control flow and data flow
    ///     analysis algorithm used to compute stack map frames from scratch. This array stores <see cref="Symbol.Type_Tag"/>, <see cref="Symbol.Uninitialized_Type_Tag" />, <see cref="Symbol.Forward_Uninitialized_Type_Tag" /> and <see cref="Symbol.Merged_Type_Tag"/> entries. The type symbol at index
    ///     <c>i</c> has its <see cref="Symbol.index" /> equal to <c>i</c> (and vice versa).
    /// </summary>
    private Entry[] _typeTable;

    /// <summary>
    ///     The actual number of <see cref="LabelEntry" /> in <see cref="labelTable" />. These elements are stored from
    ///     index 0 to labelCount (excluded). The other array entries are empty. These label entries are
    ///     also stored in the <see cref="labelEntries" /> hash set.
    /// </summary>
    private int labelCount;

    /**
     * The labels corresponding to the "forward uninitialized" types in the ASM specific {@link
     * typeTable} (see {@link Symbol#FORWARD_UNINITIALIZED_TYPE_TAG}). The label entry at index {@code
     * i} has its {@link LabelEntry#index} equal to {@code i} (and vice versa).
     */
    /// <summary>
    ///     The labels corresponding to the "forward uninitialized" types in the ASM specific <see cref="_typeTable" />
    ///     (see <see cref="Symbol.Forward_Uninitialized_Type_Tag" />). The label entry at index <c>i</c> has its
    ///     <see cref="LabelEntry.index" /> equal to <c>i</c> (and vice versa).
    /// </summary>
    private LabelEntry[] labelTable;

    /**
     * A hash set of all the {@link LabelEntry} elements in the {@link #labelTable}. Each {@link
     * LabelEntry} instance is stored at the array index given by its hash code modulo the array size.
     * If several entries must be stored at the same array index, they are linked together via their
     * {@link LabelEntry#next} field. The {@link #getOrAddLabelEntry(Label)} method ensures that this
     * table does not contain duplicated entries.
     */
    /// <summary>
    ///     A hash set of all the <see cref="LabelEntry" /> elements in the <see cref="labelTable" />. Each
    ///     <see cref="LabelEntry" /> instance is stored at the array index given by its hash code modulo the
    ///     array size. If several entries must be stored at the same array index, they are linked together
    ///     via their <see cref="LabelEntry.next" /> field. The <see cref="GetOrAddLabelEntry(Label)" /> method ensures
    ///     that this table does not contain duplicated entries.
    /// </summary>
    private LabelEntry[] labelEntries;

    /// <summary>
    ///     Constructs a new, empty SymbolTable for the given ClassWriter.
    /// </summary>
    /// <param name="classWriter"> a ClassWriter. </param>
    public SymbolTable(ClassWriter classWriter)
    {
        this.classWriter = classWriter;
        _sourceClassReader = null;
        _entries = new Entry[256];
        _constantPoolCount = 1;
        _constantPool = new ByteVector();
    }

    /// <summary>
    ///     Constructs a new SymbolTable for the given ClassWriter, initialized with the constant pool and
    ///     bootstrap methods of the given ClassReader.
    /// </summary>
    /// <param name="classWriter"> a ClassWriter. </param>
    /// <param name="classReader">
    ///     the ClassReader whose constant pool and bootstrap methods must be copied to
    ///     initialize the SymbolTable.
    /// </param>
    public SymbolTable(ClassWriter classWriter, ClassReader classReader)
    {
        this.classWriter = classWriter;
        _sourceClassReader = classReader;

        // Copy the constant pool binary content.
        sbyte[] inputBytes = classReader.classFileBuffer;
        int constantPoolOffset = classReader.GetItem(1) - 1;
        int constantPoolLength = classReader.header - constantPoolOffset;
        _constantPoolCount = classReader.ItemCount;
        _constantPool = new ByteVector(constantPoolLength);
        _constantPool.PutByteArray(inputBytes, constantPoolOffset, constantPoolLength);

        // Add the constant pool items in the symbol table entries. Reserve enough space in 'entries' to
        // avoid too many hash set collisions (entries is not dynamically resized by the addConstant*
        // method calls below), and to account for bootstrap method entries.
        _entries = new Entry[_constantPoolCount * 2];
        char[] charBuffer = new char[classReader.MaxStringLength];
        bool hasBootstrapMethods = false;
        int itemIndex = 1;
        while (itemIndex < _constantPoolCount)
        {
            int itemOffset = classReader.GetItem(itemIndex);
            int itemTag = inputBytes[itemOffset - 1];
            int nameAndTypeItemOffset;
            switch (itemTag)
            {
                case Symbol.Constant_Fieldref_Tag:
                case Symbol.Constant_Methodref_Tag:
                case Symbol.Constant_Interface_Methodref_Tag:
                    nameAndTypeItemOffset = classReader.GetItem(classReader.ReadUnsignedShort(itemOffset + 2));
                    AddConstantMemberReference(itemIndex, itemTag, classReader.ReadClass(itemOffset, charBuffer),
                        classReader.ReadUtf8(nameAndTypeItemOffset, charBuffer),
                        classReader.ReadUtf8(nameAndTypeItemOffset + 2, charBuffer));
                    break;
                case Symbol.Constant_Integer_Tag:
                case Symbol.Constant_Float_Tag:
                    AddConstantIntegerOrFloat(itemIndex, itemTag, classReader.ReadInt(itemOffset));
                    break;
                case Symbol.Constant_Name_And_Type_Tag:
                    AddConstantNameAndType(itemIndex, classReader.ReadUtf8(itemOffset, charBuffer),
                        classReader.ReadUtf8(itemOffset + 2, charBuffer));
                    break;
                case Symbol.Constant_Long_Tag:
                case Symbol.Constant_Double_Tag:
                    AddConstantLongOrDouble(itemIndex, itemTag, classReader.ReadLong(itemOffset));
                    break;
                case Symbol.Constant_Utf8_Tag:
                    AddConstantUtf8(itemIndex, classReader.ReadUtf(itemIndex, charBuffer));
                    break;
                case Symbol.Constant_Method_Handle_Tag:
                    int memberRefItemOffset = classReader.GetItem(classReader.ReadUnsignedShort(itemOffset + 1));
                    nameAndTypeItemOffset =
                        classReader.GetItem(classReader.ReadUnsignedShort(memberRefItemOffset + 2));
                    AddConstantMethodHandle(itemIndex, classReader.ReadByte(itemOffset),
                        classReader.ReadClass(memberRefItemOffset, charBuffer),
                        classReader.ReadUtf8(nameAndTypeItemOffset, charBuffer),
                        classReader.ReadUtf8(nameAndTypeItemOffset + 2, charBuffer),
                        classReader.ReadByte(memberRefItemOffset - 1) == Symbol.Constant_Interface_Methodref_Tag);
                    break;
                case Symbol.Constant_Dynamic_Tag:
                case Symbol.Constant_Invoke_Dynamic_Tag:
                    hasBootstrapMethods = true;
                    nameAndTypeItemOffset = classReader.GetItem(classReader.ReadUnsignedShort(itemOffset + 2));
                    AddConstantDynamicOrInvokeDynamicReference(itemTag, itemIndex,
                        classReader.ReadUtf8(nameAndTypeItemOffset, charBuffer),
                        classReader.ReadUtf8(nameAndTypeItemOffset + 2, charBuffer),
                        classReader.ReadUnsignedShort(itemOffset));
                    break;
                case Symbol.Constant_String_Tag:
                case Symbol.Constant_Class_Tag:
                case Symbol.Constant_Method_Type_Tag:
                case Symbol.Constant_Module_Tag:
                case Symbol.Constant_Package_Tag:
                    AddConstantUtf8Reference(itemIndex, itemTag, classReader.ReadUtf8(itemOffset, charBuffer));
                    break;
                default:
                    throw new ArgumentException();
            }

            itemIndex += itemTag == Symbol.Constant_Long_Tag || itemTag == Symbol.Constant_Double_Tag ? 2 : 1;
        }

        // Copy the BootstrapMethods, if any.
        if (hasBootstrapMethods) CopyBootstrapMethods(classReader, charBuffer);
    }

    /// <summary>
    ///     Returns the ClassReader from which this SymbolTable was constructed.
    /// </summary>
    /// <returns>
    ///     the ClassReader from which this SymbolTable was constructed, or null if it
    ///     was constructed from scratch.
    /// </returns>
    public ClassReader Source => _sourceClassReader;

    /// <summary>
    ///     Returns the major version of the class to which this symbol table belongs.
    /// </summary>
    /// <returns> the major version of the class to which this symbol table belongs. </returns>
    public int MajorVersion => _majorVersion;

    /// <summary>
    ///     Returns the internal name of the class to which this symbol table belongs.
    /// </summary>
    /// <returns> the internal name of the class to which this symbol table belongs. </returns>
    public string ClassName => _className;

    /// <summary>
    ///     Returns the number of items in this symbol table's constant_pool array (plus 1).
    /// </summary>
    /// <returns> the number of items in this symbol table's constant_pool array (plus 1). </returns>
    public int ConstantPoolCount => _constantPoolCount;

    /// <summary>
    ///     Returns the length in bytes of this symbol table's constant_pool array.
    /// </summary>
    /// <returns> the length in bytes of this symbol table's constant_pool array. </returns>
    public int ConstantPoolLength => _constantPool.length;

    /// <summary>
    ///     Read the BootstrapMethods 'bootstrap_methods' array binary content and add them as entries of
    ///     the SymbolTable.
    /// </summary>
    /// <param name="classReader">
    ///     the ClassReader whose bootstrap methods must be copied to initialize the
    ///     SymbolTable.
    /// </param>
    /// <param name="charBuffer"> a buffer used to read strings in the constant pool. </param>
    private void CopyBootstrapMethods(ClassReader classReader, char[] charBuffer)
    {
        // Find attributOffset of the 'bootstrap_methods' array.
        sbyte[] inputBytes = classReader.classFileBuffer;
        int currentAttributeOffset = classReader.FirstAttributeOffset;
        for (int i = classReader.ReadUnsignedShort(currentAttributeOffset - 2); i > 0; --i)
        {
            string attributeName = classReader.ReadUtf8(currentAttributeOffset, charBuffer);
            if (Constants.Bootstrap_Methods.Equals(attributeName))
            {
                _bootstrapMethodCount = classReader.ReadUnsignedShort(currentAttributeOffset + 6);
                break;
            }

            currentAttributeOffset += 6 + classReader.ReadInt(currentAttributeOffset + 2);
        }

        if (_bootstrapMethodCount > 0)
        {
            // Compute the offset and the length of the BootstrapMethods 'bootstrap_methods' array.
            int bootstrapMethodsOffset = currentAttributeOffset + 8;
            int bootstrapMethodsLength = classReader.ReadInt(currentAttributeOffset + 2) - 2;
            _bootstrapMethods = new ByteVector(bootstrapMethodsLength);
            _bootstrapMethods.PutByteArray(inputBytes, bootstrapMethodsOffset, bootstrapMethodsLength);

            // Add each bootstrap method in the symbol table entries.
            int currentOffset = bootstrapMethodsOffset;
            for (int i = 0; i < _bootstrapMethodCount; i++)
            {
                int offset = currentOffset - bootstrapMethodsOffset;
                int bootstrapMethodRef = classReader.ReadUnsignedShort(currentOffset);
                currentOffset += 2;
                int numBootstrapArguments = classReader.ReadUnsignedShort(currentOffset);
                currentOffset += 2;
                int hashCode = classReader.ReadConst(bootstrapMethodRef, charBuffer).GetHashCode();
                while (numBootstrapArguments-- > 0)
                {
                    int bootstrapArgument = classReader.ReadUnsignedShort(currentOffset);
                    currentOffset += 2;
                    hashCode ^= classReader.ReadConst(bootstrapArgument, charBuffer).GetHashCode();
                }

                Add(new Entry(i, Symbol.Bootstrap_Method_Tag, offset, hashCode & 0x7FFFFFFF));
            }
        }
    }

    /// <summary>
    ///     Sets the major version and the name of the class to which this symbol table belongs. Also adds
    ///     the class name to the constant pool.
    /// </summary>
    /// <param name="majorVersion"> a major ClassFile version number. </param>
    /// <param name="className"> an internal class name. </param>
    /// <returns> the constant pool index of a new or already existing Symbol with the given class name. </returns>
    public int SetMajorVersionAndClassName(int majorVersion, string className)
    {
        this._majorVersion = majorVersion;
        this._className = className;
        return AddConstantClass(className).index;
    }

    /// <summary>
    ///     Puts this symbol table's constant_pool array in the given ByteVector, preceded by the
    ///     constant_pool_count value.
    /// </summary>
    /// <param name="output"> where the JVMS ClassFile's constant_pool array must be put. </param>
    public void PutConstantPool(ByteVector output)
    {
        output.PutShort(_constantPoolCount).PutByteArray(_constantPool.data, 0, _constantPool.length);
    }

    /// <summary>
    ///     Returns the size in bytes of this symbol table's BootstrapMethods attribute. Also adds the
    ///     attribute name in the constant pool.
    /// </summary>
    /// <returns> the size in bytes of this symbol table's BootstrapMethods attribute. </returns>
    public int ComputeBootstrapMethodsSize()
    {
        if (_bootstrapMethods != null)
        {
            AddConstantUtf8(Constants.Bootstrap_Methods);
            return 8 + _bootstrapMethods.length;
        }

        return 0;
    }

    /// <summary>
    ///     Puts this symbol table's BootstrapMethods attribute in the given ByteVector. This includes the
    ///     6 attribute header bytes and the num_bootstrap_methods value.
    /// </summary>
    /// <param name="output"> where the JVMS BootstrapMethods attribute must be put. </param>
    public void PutBootstrapMethods(ByteVector output)
    {
        if (_bootstrapMethods != null)
            output.PutShort(AddConstantUtf8(Constants.Bootstrap_Methods)).PutInt(_bootstrapMethods.length + 2)
                .PutShort(_bootstrapMethodCount).PutByteArray(_bootstrapMethods.data, 0, _bootstrapMethods.length);
    }

    // -----------------------------------------------------------------------------------------------
    // Generic symbol table entries management.
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    ///     Returns the list of entries which can potentially have the given hash code.
    /// </summary>
    /// <param name="hashCode"> a <see cref="Entry.hashCode" /> value. </param>
    /// <returns>
    ///     the list of entries which can potentially have the given hash code. The list is stored
    ///     via the <see cref="Entry.next" /> field.
    /// </returns>
    private Entry Get(int hashCode)
    {
        return _entries[hashCode % _entries.Length];
    }

    /// <summary>
    ///     Puts the given entry in the <see cref="_entries" /> hash set. This method does <i>not</i> check
    ///     whether <see cref="_entries" /> already contains a similar entry or not. <see cref="_entries" /> is resized
    ///     if necessary to avoid hash collisions (multiple entries needing to be stored at the same <see cref="entries"/> array index) as much as possible, with reasonable memory usage.
    /// </summary>
    /// <param name="entry"> an Entry (which must not already be contained in <see cref="_entries" />). </param>
    /// <returns> the given entry </returns>
    private Entry Put(Entry entry)
    {
        if (_entryCount > _entries.Length * 3 / 4)
        {
            int currentCapacity = _entries.Length;
            int newCapacity = currentCapacity * 2 + 1;
            Entry[] newEntries = new Entry[newCapacity];
            for (int i = currentCapacity - 1; i >= 0; --i)
            {
                Entry currentEntry = _entries[i];
                while (currentEntry != null)
                {
                    int newCurrentEntryIndex = currentEntry.hashCode % newCapacity;
                    Entry nextEntry = currentEntry.next;
                    currentEntry.next = newEntries[newCurrentEntryIndex];
                    newEntries[newCurrentEntryIndex] = currentEntry;
                    currentEntry = nextEntry;
                }
            }

            _entries = newEntries;
        }

        _entryCount++;
        int index = entry.hashCode % _entries.Length;
        entry.next = _entries[index];
        return _entries[index] = entry;
    }

    /// <summary>
    ///     Adds the given entry in the <see cref="_entries" /> hash set. This method does <i>not</i> check
    ///     whether <see cref="_entries" /> already contains a similar entry or not, and does <i>not</i> resize
    ///     <see cref="_entries" /> if necessary.
    /// </summary>
    /// <param name="entry"> an Entry (which must not already be contained in <see cref="_entries" />). </param>
    private void Add(Entry entry)
    {
        _entryCount++;
        int index = entry.hashCode % _entries.Length;
        entry.next = _entries[index];
        _entries[index] = entry;
    }

    // -----------------------------------------------------------------------------------------------
    // Constant pool entries management.
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    ///     Adds a number or string constant to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="value">
    ///     the value of the constant to be added to the constant pool. This parameter must be
    ///     an <see cref="int" />, <see cref="byte" />, <see cref="char" />, <see cref="short" />,
    ///     <see cref="bool" />, <see cref="Float"/>, <see cref="long" />, <see cref="double" />, <see cref="string" />, <see cref="Type" /> or
    ///     <see cref="Handle" />.
    /// </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstant(object value)
    {
        if (value is int) return AddConstantInteger(((int)value));

        if (value is byte) return AddConstantInteger(((byte)value));

        if (value is sbyte) return AddConstantInteger(((sbyte)value));

        if (value is char) return AddConstantInteger(((char)value));

        if (value is short) return AddConstantInteger(((short)value));

        if (value is bool) return AddConstantInteger(((bool)value) ? 1 : 0);

        if (value is float) return AddConstantFloat(((float)value));

        if (value is long) return AddConstantLong(((long)value));

        if (value is double) return AddConstantDouble(((double)value));

        if (value is string) return AddConstantString((string)value);

        if (value is JType)
        {
            JType type = (JType)value;
            int typeSort = type.Sort;
            if (typeSort == JType.Object)
                return AddConstantClass(type.InternalName);
            if (typeSort == JType.Method)
                return AddConstantMethodType(type.Descriptor);
            return AddConstantClass(type.Descriptor);
        }

        if (value is Handle)
        {
            Handle handle = (Handle)value;
            return AddConstantMethodHandle(handle.Tag, handle.Owner, handle.Name, handle.Desc, handle.Interface);
        }

        if (value is ConstantDynamic)
        {
            ConstantDynamic constantDynamic = (ConstantDynamic)value;
            return AddConstantDynamic(constantDynamic.Name, constantDynamic.Descriptor,
                constantDynamic.BootstrapMethod, constantDynamic.BootstrapMethodArgumentsUnsafe);
        }

        throw new ArgumentException("value " + value);
    }

    /// <summary>
    ///     Adds a CONSTANT_Class_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="value"> the internal name of a class. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantClass(string value)
    {
        return AddConstantUtf8Reference(Symbol.Constant_Class_Tag, value);
    }

    /// <summary>
    ///     Adds a CONSTANT_Fieldref_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="owner"> the internal name of a class. </param>
    /// <param name="name"> a field name. </param>
    /// <param name="descriptor"> a field descriptor. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantFieldref(string owner, string name, string descriptor)
    {
        return AddConstantMemberReference(Symbol.Constant_Fieldref_Tag, owner, name, descriptor);
    }

    /// <summary>
    ///     Adds a CONSTANT_Methodref_info or CONSTANT_InterfaceMethodref_info to the constant pool of this
    ///     symbol table. Does nothing if the constant pool already contains a similar item.
    /// </summary>
    /// <param name="owner"> the internal name of a class. </param>
    /// <param name="name"> a method name. </param>
    /// <param name="descriptor"> a method descriptor. </param>
    /// <param name="isInterface"> whether owner is an interface or not. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantMethodref(string owner, string name, string descriptor, bool isInterface)
    {
        int tag = isInterface ? Symbol.Constant_Interface_Methodref_Tag : Symbol.Constant_Methodref_Tag;
        return AddConstantMemberReference(tag, owner, name, descriptor);
    }

    /// <summary>
    ///     Adds a CONSTANT_Fieldref_info, CONSTANT_Methodref_info or CONSTANT_InterfaceMethodref_info to
    ///     the constant pool of this symbol table. Does nothing if the constant pool already contains a
    ///     similar item.
    /// </summary>
    /// <param name="tag">
    ///     one of <see cref="Symbol.Constant_Fieldref_Tag" />, <see cref="Symbol.Constant_Methodref_Tag" />
    ///     or <see cref="Symbol.Constant_Interface_Methodref_Tag" />.
    /// </param>
    /// <param name="owner"> the internal name of a class. </param>
    /// <param name="name"> a field or method name. </param>
    /// <param name="descriptor"> a field or method descriptor. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    private Entry AddConstantMemberReference(int tag, string owner, string name, string descriptor)
    {
        int hashCode = Hash(tag, owner, name, descriptor);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == tag && entry.hashCode == hashCode && entry.owner.Equals(owner) &&
                entry.name.Equals(name) && entry.value.Equals(descriptor)) return entry;
            entry = entry.next;
        }

        _constantPool.Put122(tag, AddConstantClass(owner).index, AddConstantNameAndType(name, descriptor));
        return Put(new Entry(_constantPoolCount++, tag, owner, name, descriptor, 0, hashCode));
    }

    /// <summary>
    ///     Adds a new CONSTANT_Fieldref_info, CONSTANT_Methodref_info or CONSTANT_InterfaceMethodref_info
    ///     to the constant pool of this symbol table.
    /// </summary>
    /// <param name="index"> the constant pool index of the new Symbol. </param>
    /// <param name="tag">
    ///     one of <see cref="Symbol.Constant_Fieldref_Tag" />, <see cref="Symbol.Constant_Methodref_Tag" />
    ///     or <see cref="Symbol.Constant_Interface_Methodref_Tag" />.
    /// </param>
    /// <param name="owner"> the internal name of a class. </param>
    /// <param name="name"> a field or method name. </param>
    /// <param name="descriptor"> a field or method descriptor. </param>
    private void AddConstantMemberReference(int index, int tag, string owner, string name, string descriptor)
    {
        Add(new Entry(index, tag, owner, name, descriptor, 0, Hash(tag, owner, name, descriptor)));
    }

    /// <summary>
    ///     Adds a CONSTANT_String_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="value"> a string. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantString(string value)
    {
        return AddConstantUtf8Reference(Symbol.Constant_String_Tag, value);
    }

    /// <summary>
    ///     Adds a CONSTANT_Integer_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="value"> an int. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantInteger(int value)
    {
        return AddConstantIntegerOrFloat(Symbol.Constant_Integer_Tag, value);
    }

    /// <summary>
    ///     Adds a CONSTANT_Float_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="value"> a float. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantFloat(float value)
    {
        return AddConstantIntegerOrFloat(Symbol.Constant_Float_Tag, Int32AndSingleConverter.Convert(value));
    }

    /// <summary>
    ///     Adds a CONSTANT_Integer_info or CONSTANT_Float_info to the constant pool of this symbol table.
    ///     Does nothing if the constant pool already contains a similar item.
    /// </summary>
    /// <param name="tag">
    ///     one of <see cref="Symbol.Constant_Integer_Tag" /> or
    ///     <see cref="Symbol.Constant_Float_Tag" />.
    /// </param>
    /// <param name="value"> an int or float. </param>
    /// <returns> a constant pool constant with the given tag and primitive values. </returns>
    private Symbol AddConstantIntegerOrFloat(int tag, int value)
    {
        int hashCode = Hash(tag, value);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == tag && entry.hashCode == hashCode && entry.data == value) return entry;
            entry = entry.next;
        }

        _constantPool.PutByte(tag).PutInt(value);
        return Put(new Entry(_constantPoolCount++, tag, value, hashCode));
    }

    /// <summary>
    ///     Adds a new CONSTANT_Integer_info or CONSTANT_Float_info to the constant pool of this symbol
    ///     table.
    /// </summary>
    /// <param name="index"> the constant pool index of the new Symbol. </param>
    /// <param name="tag">
    ///     one of <see cref="Symbol.Constant_Integer_Tag" /> or
    ///     <see cref="Symbol.Constant_Float_Tag" />.
    /// </param>
    /// <param name="value"> an int or float. </param>
    private void AddConstantIntegerOrFloat(int index, int tag, int value)
    {
        Add(new Entry(index, tag, value, Hash(tag, value)));
    }

    /// <summary>
    ///     Adds a CONSTANT_Long_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="value"> a long. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantLong(long value)
    {
        return AddConstantLongOrDouble(Symbol.Constant_Long_Tag, value);
    }

    /// <summary>
    ///     Adds a CONSTANT_Double_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="value"> a double. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantDouble(double value)
    {
        return AddConstantLongOrDouble(Symbol.Constant_Double_Tag, BitConverter.DoubleToInt64Bits(value));
    }

    /// <summary>
    ///     Adds a CONSTANT_Long_info or CONSTANT_Double_info to the constant pool of this symbol table.
    ///     Does nothing if the constant pool already contains a similar item.
    /// </summary>
    /// <param name="tag"> one of <see cref="Symbol.Constant_Long_Tag" /> or <see cref="Symbol.Constant_Double_Tag" />. </param>
    /// <param name="value"> a long or double. </param>
    /// <returns> a constant pool constant with the given tag and primitive values. </returns>
    private Symbol AddConstantLongOrDouble(int tag, long value)
    {
        int hashCode = Hash(tag, value);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == tag && entry.hashCode == hashCode && entry.data == value) return entry;
            entry = entry.next;
        }

        int index = _constantPoolCount;
        _constantPool.PutByte(tag).PutLong(value);
        _constantPoolCount += 2;
        return Put(new Entry(index, tag, value, hashCode));
    }

    /// <summary>
    ///     Adds a new CONSTANT_Long_info or CONSTANT_Double_info to the constant pool of this symbol
    ///     table.
    /// </summary>
    /// <param name="index"> the constant pool index of the new Symbol. </param>
    /// <param name="tag"> one of <see cref="Symbol.Constant_Long_Tag" /> or <see cref="Symbol.Constant_Double_Tag" />. </param>
    /// <param name="value"> a long or double. </param>
    private void AddConstantLongOrDouble(int index, int tag, long value)
    {
        Add(new Entry(index, tag, value, Hash(tag, value)));
    }

    /// <summary>
    ///     Adds a CONSTANT_NameAndType_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="name"> a field or method name. </param>
    /// <param name="descriptor"> a field or method descriptor. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public int AddConstantNameAndType(string name, string descriptor)
    {
        const int tag = Symbol.Constant_Name_And_Type_Tag;
        int hashCode = Hash(tag, name, descriptor);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == tag && entry.hashCode == hashCode && entry.name.Equals(name) &&
                entry.value.Equals(descriptor)) return entry.index;
            entry = entry.next;
        }

        _constantPool.Put122(tag, AddConstantUtf8(name), AddConstantUtf8(descriptor));
        return Put(new Entry(_constantPoolCount++, tag, name, descriptor, hashCode)).index;
    }

    /// <summary>
    ///     Adds a new CONSTANT_NameAndType_info to the constant pool of this symbol table.
    /// </summary>
    /// <param name="index"> the constant pool index of the new Symbol. </param>
    /// <param name="name"> a field or method name. </param>
    /// <param name="descriptor"> a field or method descriptor. </param>
    private void AddConstantNameAndType(int index, string name, string descriptor)
    {
        const int tag = Symbol.Constant_Name_And_Type_Tag;
        Add(new Entry(index, tag, name, descriptor, Hash(tag, name, descriptor)));
    }

    /// <summary>
    ///     Adds a CONSTANT_Utf8_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="value"> a string. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public int AddConstantUtf8(string value)
    {
        int hashCode = Hash(Symbol.Constant_Utf8_Tag, value);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == Symbol.Constant_Utf8_Tag && entry.hashCode == hashCode && entry.value.Equals(value))
                return entry.index;
            entry = entry.next;
        }

        _constantPool.PutByte(Symbol.Constant_Utf8_Tag).PutUtf8(value);
        return Put(new Entry(_constantPoolCount++, Symbol.Constant_Utf8_Tag, value, hashCode)).index;
    }

    /// <summary>
    ///     Adds a new CONSTANT_String_info to the constant pool of this symbol table.
    /// </summary>
    /// <param name="index"> the constant pool index of the new Symbol. </param>
    /// <param name="value"> a string. </param>
    private void AddConstantUtf8(int index, string value)
    {
        Add(new Entry(index, Symbol.Constant_Utf8_Tag, value, Hash(Symbol.Constant_Utf8_Tag, value)));
    }

    /// <summary>
    ///     Adds a CONSTANT_MethodHandle_info to the constant pool of this symbol table. Does nothing if
    ///     the constant pool already contains a similar item.
    /// </summary>
    /// <param name="referenceKind">
    ///     one of <see cref="Opcodes.H_Getfield />, <see cref="Opcodes.H_Getstatic />, <see cref="Opcodes.H_PUTFIELD"/>, <see cref="Opcodes.H_Putstatic />, <see cref="Opcodes.H_Invokevirtual />, <see cref="Opcodes.H_INVOKESTATIC"/>, <see cref="Opcodes.H_Invokespecial />, <see cref="Opcodes.H_NEWINVOKESPECIAL"/> or <see cref="Opcodes.H_Invokeinterface />.
    /// </param>
    /// <param name="owner"> the internal name of a class of interface. </param>
    /// <param name="name"> a field or method name. </param>
    /// <param name="descriptor"> a field or method descriptor. </param>
    /// <param name="isInterface"> whether owner is an interface or not. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantMethodHandle(int referenceKind, string owner, string name, string descriptor,
        bool isInterface)
    {
        const int tag = Symbol.Constant_Method_Handle_Tag;
        int data = GetConstantMethodHandleSymbolData(referenceKind, isInterface);
        // Note that we don't need to include isInterface in the hash computation, because it is
        // redundant with owner (we can't have the same owner with different isInterface values).
        int hashCode = Hash(tag, owner, name, descriptor, data);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == tag
                && entry.hashCode == hashCode
                && entry.data == data
                && entry.owner.Equals(owner)
                && entry.name.Equals(name)
                && entry.value.Equals(descriptor))
                return entry;
            
            entry = entry.next;
        }

        if (referenceKind <= Opcodes.H_Putstatic)
            _constantPool.Put112(tag, referenceKind, AddConstantFieldref(owner, name, descriptor).index);
        else
            _constantPool.Put112(tag, referenceKind, AddConstantMethodref(owner, name, descriptor, isInterface).index);
        
        return Put(new Entry(_constantPoolCount++, tag, owner, name, descriptor, data, hashCode));
    }

    /// <summary>
    ///     Adds a new CONSTANT_MethodHandle_info to the constant pool of this symbol table.
    /// </summary>
    /// <param name="index"> the constant pool index of the new Symbol. </param>
    /// <param name="referenceKind">
    ///     one of <see cref="Opcodes.H_Getfield />, <see cref="Opcodes.H_Getstatic />, <see cref="Opcodes.H_PUTFIELD"/>, <see cref="Opcodes.H_Putstatic />, <see cref="Opcodes.H_Invokevirtual />, <see cref="Opcodes.H_INVOKESTATIC"/>, <see cref="Opcodes.H_Invokespecial />, <see cref="Opcodes.H_NEWINVOKESPECIAL"/> or <see cref="Opcodes.H_Invokeinterface />.
    /// </param>
    /// <param name="owner"> the internal name of a class of interface. </param>
    /// <param name="name"> a field or method name. </param>
    /// <param name="descriptor"> a field or method descriptor. </param>
    /// <param name="isInterface"> whether owner is an interface or not. </param>
    private void AddConstantMethodHandle(int index, int referenceKind, string owner, string name, string descriptor, bool isInterface)
    {
        const int tag = Symbol.Constant_Method_Handle_Tag;
        int data = GetConstantMethodHandleSymbolData(referenceKind, isInterface);
        int hashCode = Hash(tag, owner, name, descriptor, data);
        Add(new Entry(index, tag, owner, name, descriptor, data, hashCode));
    }
    
    /// <summary>
    /// Returns the <see cref="Symbol.data" /> field for a CONSTANT_MethodHandle_info Symbol.
    /// </summary>
    /// <param name="referenceKind"> one of <see cref="Opcodes.H_Getfield" />, <see cref="Opcodes.H_Getstatic" />,
    ///     <see cref="Opcodes.H_Putfield"/>, <see cref="Opcodes.H_Putstatic" />, <see cref="Opcodes.H_Invokevirtual" />,
    ///     <see cref="Opcodes.H_Invokestatic" />, <see cref="Opcodes.H_Invokespecial" />,
    ///     <see cref="Opcodes.H_Newinvokespecial"/> or <see cref="Opcodes.H_Invokeinterface" />. </param>
    /// <param name="isInterface"> whether owner is an interface or not. </param>
    private static int GetConstantMethodHandleSymbolData(int referenceKind, bool isInterface)
    {
        if (referenceKind > Opcodes.H_Putstatic && isInterface)
            return referenceKind << 8;
        return referenceKind;
    }

    /// <summary>
    ///     Adds a CONSTANT_MethodType_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="methodDescriptor"> a method descriptor. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantMethodType(string methodDescriptor)
    {
        return AddConstantUtf8Reference(Symbol.Constant_Method_Type_Tag, methodDescriptor);
    }

    /// <summary>
    ///     Adds a CONSTANT_Dynamic_info to the constant pool of this symbol table. Also adds the related
    ///     bootstrap method to the BootstrapMethods of this symbol table. Does nothing if the constant
    ///     pool already contains a similar item.
    /// </summary>
    /// <param name="name"> a method name. </param>
    /// <param name="descriptor"> a field descriptor. </param>
    /// <param name="bootstrapMethodHandle"> a bootstrap method handle. </param>
    /// <param name="bootstrapMethodArguments"> the bootstrap method arguments. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantDynamic(string name, string descriptor, Handle bootstrapMethodHandle,
        params object[] bootstrapMethodArguments)
    {
        Symbol bootstrapMethod = AddBootstrapMethod(bootstrapMethodHandle, bootstrapMethodArguments);
        return AddConstantDynamicOrInvokeDynamicReference(Symbol.Constant_Dynamic_Tag, name, descriptor,
            bootstrapMethod.index);
    }

    /// <summary>
    ///     Adds a CONSTANT_InvokeDynamic_info to the constant pool of this symbol table. Also adds the
    ///     related bootstrap method to the BootstrapMethods of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="name"> a method name. </param>
    /// <param name="descriptor"> a method descriptor. </param>
    /// <param name="bootstrapMethodHandle"> a bootstrap method handle. </param>
    /// <param name="bootstrapMethodArguments"> the bootstrap method arguments. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantInvokeDynamic(string name, string descriptor, Handle bootstrapMethodHandle,
        params object[] bootstrapMethodArguments)
    {
        Symbol bootstrapMethod = AddBootstrapMethod(bootstrapMethodHandle, bootstrapMethodArguments);
        return AddConstantDynamicOrInvokeDynamicReference(Symbol.Constant_Invoke_Dynamic_Tag, name, descriptor,
            bootstrapMethod.index);
    }

    /// <summary>
    ///     Adds a CONSTANT_Dynamic or a CONSTANT_InvokeDynamic_info to the constant pool of this symbol
    ///     table. Does nothing if the constant pool already contains a similar item.
    /// </summary>
    /// <param name="tag">
    ///     one of <see cref="Symbol.Constant_Dynamic_Tag" /> or <see cref="Symbol.Constant_Invoke_Dynamic_Tag"/>.
    /// </param>
    /// <param name="name"> a method name. </param>
    /// <param name="descriptor">
    ///     a field descriptor for CONSTANT_DYNAMIC_TAG) or a method descriptor for
    ///     Constant_Invoke_Dynamic_Tag.
    /// </param>
    /// <param name="bootstrapMethodIndex"> the index of a bootstrap method in the BootstrapMethods attribute. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    private Symbol AddConstantDynamicOrInvokeDynamicReference(int tag, string name, string descriptor,
        int bootstrapMethodIndex)
    {
        int hashCode = Hash(tag, name, descriptor, bootstrapMethodIndex);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == tag && entry.hashCode == hashCode && entry.data == bootstrapMethodIndex &&
                entry.name.Equals(name) && entry.value.Equals(descriptor)) return entry;
            entry = entry.next;
        }

        _constantPool.Put122(tag, bootstrapMethodIndex, AddConstantNameAndType(name, descriptor));
        return Put(new Entry(_constantPoolCount++, tag, null, name, descriptor, bootstrapMethodIndex, hashCode));
    }

    /// <summary>
    ///     Adds a new CONSTANT_Dynamic_info or CONSTANT_InvokeDynamic_info to the constant pool of this
    ///     symbol table.
    /// </summary>
    /// <param name="tag">
    ///     one of <see cref="Symbol.Constant_Dynamic_Tag" /> or <see cref="Symbol.Constant_Invoke_Dynamic_Tag"/>.
    /// </param>
    /// <param name="index"> the constant pool index of the new Symbol. </param>
    /// <param name="name"> a method name. </param>
    /// <param name="descriptor">
    ///     a field descriptor for CONSTANT_DYNAMIC_TAG or a method descriptor for
    ///     Constant_Invoke_Dynamic_Tag.
    /// </param>
    /// <param name="bootstrapMethodIndex"> the index of a bootstrap method in the BootstrapMethods attribute. </param>
    private void AddConstantDynamicOrInvokeDynamicReference(int tag, int index, string name, string descriptor,
        int bootstrapMethodIndex)
    {
        int hashCode = Hash(tag, name, descriptor, bootstrapMethodIndex);
        Add(new Entry(index, tag, null, name, descriptor, bootstrapMethodIndex, hashCode));
    }

    /// <summary>
    ///     Adds a CONSTANT_Module_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="moduleName"> a fully qualified name (using dots) of a module. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantModule(string moduleName)
    {
        return AddConstantUtf8Reference(Symbol.Constant_Module_Tag, moduleName);
    }

    /// <summary>
    ///     Adds a CONSTANT_Package_info to the constant pool of this symbol table. Does nothing if the
    ///     constant pool already contains a similar item.
    /// </summary>
    /// <param name="packageName"> the internal name of a package. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddConstantPackage(string packageName)
    {
        return AddConstantUtf8Reference(Symbol.Constant_Package_Tag, packageName);
    }

    /// <summary>
    ///     Adds a CONSTANT_Class_info, CONSTANT_String_info, CONSTANT_MethodType_info,
    ///     CONSTANT_Module_info or CONSTANT_Package_info to the constant pool of this symbol table. Does
    ///     nothing if the constant pool already contains a similar item.
    /// </summary>
    /// <param name="tag">
    ///     one of <see cref="Symbol.Constant_Class_Tag" />, <see cref="Symbol.Constant_String_Tag" />, <see cref="Symbol.Constant_Method_Type_Tag"/>, <see cref="Symbol.Constant_Module_Tag" /> or <see cref="Symbol.CONSTANT_PACKAGE_TAG"/>.
    /// </param>
    /// <param name="value">
    ///     an internal class name, an arbitrary string, a method descriptor, a module or a
    ///     package name, depending on tag.
    /// </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    private Symbol AddConstantUtf8Reference(int tag, string value)
    {
        int hashCode = Hash(tag, value);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == tag && entry.hashCode == hashCode && entry.value.Equals(value)) return entry;
            entry = entry.next;
        }

        _constantPool.Put12(tag, AddConstantUtf8(value));
        return Put(new Entry(_constantPoolCount++, tag, value, hashCode));
    }

    /// <summary>
    ///     Adds a new CONSTANT_Class_info, CONSTANT_String_info, CONSTANT_MethodType_info,
    ///     CONSTANT_Module_info or CONSTANT_Package_info to the constant pool of this symbol table.
    /// </summary>
    /// <param name="index"> the constant pool index of the new Symbol. </param>
    /// <param name="tag">
    ///     one of <see cref="Symbol.Constant_Class_Tag" />, <see cref="Symbol.Constant_String_Tag" />, <see cref="Symbol.Constant_Method_Type_Tag"/>, <see cref="Symbol.Constant_Module_Tag" /> or <see cref="Symbol.CONSTANT_PACKAGE_TAG"/>.
    /// </param>
    /// <param name="value">
    ///     an internal class name, an arbitrary string, a method descriptor, a module or a
    ///     package name, depending on tag.
    /// </param>
    private void AddConstantUtf8Reference(int index, int tag, string value)
    {
        Add(new Entry(index, tag, value, Hash(tag, value)));
    }

    // -----------------------------------------------------------------------------------------------
    // Bootstrap method entries management.
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    ///     Adds a bootstrap method to the BootstrapMethods attribute of this symbol table. Does nothing if
    ///     the BootstrapMethods already contains a similar bootstrap method.
    /// </summary>
    /// <param name="bootstrapMethodHandle"> a bootstrap method handle. </param>
    /// <param name="bootstrapMethodArguments"> the bootstrap method arguments. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    public Symbol AddBootstrapMethod(Handle bootstrapMethodHandle, params object[] bootstrapMethodArguments)
    {
        ByteVector bootstrapMethodsAttribute = _bootstrapMethods;
        if (bootstrapMethodsAttribute == null) bootstrapMethodsAttribute = _bootstrapMethods = new ByteVector();

        // The bootstrap method arguments can be Constant_Dynamic values, which reference other
        // bootstrap methods. We must therefore add the bootstrap method arguments to the constant pool
        // and BootstrapMethods attribute first, so that the BootstrapMethods attribute is not modified
        // while adding the given bootstrap method to it, in the rest of this method.
        int numBootstrapArguments = bootstrapMethodArguments.Length;
        int[] bootstrapMethodArgumentIndexes = new int[numBootstrapArguments];
        for (int i = 0; i < numBootstrapArguments; i++)
            bootstrapMethodArgumentIndexes[i] = AddConstant(bootstrapMethodArguments[i]).index;

        // Write the bootstrap method in the BootstrapMethods table. This is necessary to be able to
        // compare it with existing ones, and will be reverted below if there is already a similar
        // bootstrap method.
        int bootstrapMethodOffset = bootstrapMethodsAttribute.length;
        bootstrapMethodsAttribute.PutShort(AddConstantMethodHandle(bootstrapMethodHandle.Tag,
            bootstrapMethodHandle.Owner, bootstrapMethodHandle.Name, bootstrapMethodHandle.Desc,
            bootstrapMethodHandle.Interface).index);

        bootstrapMethodsAttribute.PutShort(numBootstrapArguments);
        for (int i = 0; i < numBootstrapArguments; i++)
            bootstrapMethodsAttribute.PutShort(bootstrapMethodArgumentIndexes[i]);

        // Compute the length and the hash code of the bootstrap method.
        int bootstrapMethodlength = bootstrapMethodsAttribute.length - bootstrapMethodOffset;
        int hashCode = bootstrapMethodHandle.GetHashCode();
        foreach (object bootstrapMethodArgument in bootstrapMethodArguments)
            hashCode ^= bootstrapMethodArgument.GetHashCode();
        hashCode &= 0x7FFFFFFF;

        // Add the bootstrap method to the symbol table or revert the above changes.
        return AddBootstrapMethod(bootstrapMethodOffset, bootstrapMethodlength, hashCode);
    }

    /// <summary>
    ///     Adds a bootstrap method to the BootstrapMethods attribute of this symbol table. Does nothing if
    ///     the BootstrapMethods already contains a similar bootstrap method (more precisely, reverts the
    ///     content of <see cref="_bootstrapMethods" /> to remove the last, duplicate bootstrap method).
    /// </summary>
    /// <param name="offset"> the offset of the last bootstrap method in <see cref="_bootstrapMethods" />, in bytes. </param>
    /// <param name="length"> the length of this bootstrap method in <see cref="_bootstrapMethods" />, in bytes. </param>
    /// <param name="hashCode"> the hash code of this bootstrap method. </param>
    /// <returns> a new or already existing Symbol with the given value. </returns>
    private Symbol AddBootstrapMethod(int offset, int length, int hashCode)
    {
        sbyte[] bootstrapMethodsData = _bootstrapMethods.data;
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == Symbol.Bootstrap_Method_Tag && entry.hashCode == hashCode)
            {
                int otherOffset = (int)entry.data;
                bool isSameBootstrapMethod = true;
                for (int i = 0; i < length; ++i)
                    if (bootstrapMethodsData[offset + i] != bootstrapMethodsData[otherOffset + i])
                    {
                        isSameBootstrapMethod = false;
                        break;
                    }

                if (isSameBootstrapMethod)
                {
                    _bootstrapMethods.length = offset; // Revert to old position.
                    return entry;
                }
            }

            entry = entry.next;
        }

        return Put(new Entry(_bootstrapMethodCount++, Symbol.Bootstrap_Method_Tag, offset, hashCode));
    }

    // -----------------------------------------------------------------------------------------------
    // Type table entries management.
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    ///     Returns the type table element whose index is given.
    /// </summary>
    /// <param name="typeIndex"> a type table index. </param>
    /// <returns> the type table element whose index is given. </returns>
    public Symbol GetType(int typeIndex)
    {
        return _typeTable[typeIndex];
    }
        
    /// <summary>
    /// Returns the label corresponding to the "forward uninitialized" type table element whose index
    /// is given.
    /// </summary>
    /// <param name="typeIndex">the type table index of a "forward uninitialized" type table element.</param>
    /// <returns>the label corresponding of the NEW instruction which created this "forward uninitialized" type.</returns>
    public Label GetForwardUninitializedLabel(int typeIndex) {
        return labelTable[(int) _typeTable[typeIndex].data].label;
    }

    /// <summary>
    ///     Adds a type in the type table of this symbol table. Does nothing if the type table already
    ///     contains a similar type.
    /// </summary>
    /// <param name="value"> an internal class name. </param>
    /// <returns> the index of a new or already existing type Symbol with the given value. </returns>
    public int AddType(string value)
    {
        int hashCode = Hash(Symbol.Type_Tag, value);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == Symbol.Type_Tag && entry.hashCode == hashCode && entry.value.Equals(value))
                return entry.index;
            entry = entry.next;
        }

        return AddTypeInternal(new Entry(_typeCount, Symbol.Type_Tag, value, hashCode));
    }

    /// <summary>
    ///     Adds an uninitialized type in the type table of this symbol table. Does  nothing if the type
    ///     table already contains a similar type.
    /// </summary>
    /// <param name="value"> an internal class name. </param>
    /// <param name="bytecodeOffset">
    ///     the bytecode offset of the NEW instruction that created this uninitialized type value.
    /// </param>
    /// <returns> the index of a new or already existing type <see cref="Symbol"/> with the given value. </returns>
    public int AddUninitializedType(string value, int bytecodeOffset)
    {
        int hashCode = Hash(Symbol.Uninitialized_Type_Tag, value, bytecodeOffset);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == Symbol.Uninitialized_Type_Tag && entry.hashCode == hashCode &&
                entry.data == bytecodeOffset && entry.value.Equals(value)) return entry.index;
            entry = entry.next;
        }

        return AddTypeInternal(new Entry(_typeCount, Symbol.Uninitialized_Type_Tag, value, bytecodeOffset,
            hashCode));
    }
        
    /// <summary>
    /// Adds a "forward uninitialized" type in the type table of this symbol table. Does nothing if the
    /// type table already contains a similar type.
    /// </summary>
    /// <param name="value">an internal class name.</param>
    /// <param name="label">the label of the NEW instruction that created this uninitialized type value. If the label is resolved, use the <see cref="AddUninitializedType"/> method instead.</param>
    /// <returns>the index of a new or already existing type <see cref="Symbol"/> with the given value.</returns>
    public int AddForwardUninitializedType(string value, Label label)
    {
        int labelIndex = GetOrAddLabelEntry(label).index;
        int hashCode = Hash(Symbol.Forward_Uninitialized_Type_Tag, value, labelIndex);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == Symbol.Forward_Uninitialized_Type_Tag
                && entry.hashCode == hashCode
                && entry.data == labelIndex
                && entry.value.Equals(value))
            {
                return entry.index;
            }
            entry = entry.next;
        }
            
        return AddTypeInternal(new Entry(_typeCount, Symbol.Forward_Uninitialized_Type_Tag, value, labelIndex, hashCode));
    }

    /// <summary>
    ///     Adds a merged type in the type table of this symbol table. Does nothing if the type table
    ///     already contains a similar type.
    /// </summary>
    /// <param name="typeTableIndex1">
    ///     a <see cref="Symbol.Type_Tag" /> type, specified by its index in the type
    ///     table.
    /// </param>
    /// <param name="typeTableIndex2">
    ///     another <see cref="Symbol.Type_Tag" /> type, specified by its index in the type
    ///     table.
    /// </param>
    /// <returns>
    ///     the index of a new or already existing <see cref="Symbol.Type_Tag" /> type Symbol,
    ///     corresponding to the common super class of the given types.
    /// </returns>
    public int AddMergedType(int typeTableIndex1, int typeTableIndex2)
    {
        long data = typeTableIndex1 < typeTableIndex2
            ? (uint)typeTableIndex1 | ((long)typeTableIndex2 << 32)
            : typeTableIndex2 | ((long)typeTableIndex1 << 32);
        int hashCode = Hash(Symbol.Merged_Type_Tag, typeTableIndex1 + typeTableIndex2);
        Entry entry = Get(hashCode);
        while (entry != null)
        {
            if (entry.tag == Symbol.Merged_Type_Tag && entry.hashCode == hashCode && entry.data == data)
                return entry.info;
            entry = entry.next;
        }

        string type1 = _typeTable[typeTableIndex1].value;
        string type2 = _typeTable[typeTableIndex2].value;
        int commonSuperTypeIndex = AddType(classWriter.GetCommonSuperClass(type1, type2));
        Put(new Entry(_typeCount, Symbol.Merged_Type_Tag, data, hashCode)).info = commonSuperTypeIndex;
        return commonSuperTypeIndex;
    }

    /// <summary>
    ///     Adds the given type Symbol to <see cref="_typeTable" />.
    /// </summary>
    /// <param name="entry">
    ///     a <see cref="Symbol.Type_Tag" /> or <see cref="Symbol.Uninitialized_Type_Tag" /> type symbol.
    ///     The index of this Symbol must be equal to the current value of <see cref="_typeCount" />.
    /// </param>
    /// <returns>
    ///     the index in <see cref="_typeTable" /> where the given type was added, which is also equal to
    ///     entry's index by hypothesis.
    /// </returns>
    private int AddTypeInternal(Entry entry)
    {
        if (_typeTable == null) _typeTable = new Entry[16];
        if (_typeCount == _typeTable.Length)
        {
            Entry[] newTypeTable = new Entry[2 * _typeTable.Length];
            Array.Copy(_typeTable, 0, newTypeTable, 0, _typeTable.Length);
            _typeTable = newTypeTable;
        }

        _typeTable[_typeCount++] = entry;
        return Put(entry).index;
    }
        
    /// <summary>
    ///    Returns the <see cref="LabelEntry" /> corresponding to the given label. Creates a new one if there is
    ///    no such entry.
    /// </summary>
    /// <param name="label"> the <see cref="Label" /> of a NEW instruction which created an uninitialized type, in the
    ///     case where this NEW instruction is after the &lt;init&gt; constructor call (in bytecode
    ///     offset order). See <see cref="Symbol.Forward_Uninitialized_Type_Tag" />. </param>
    /// <returns> the <see cref="LabelEntry" /> corresponding to <paramref name="label" />. </returns>
    private LabelEntry GetOrAddLabelEntry(Label label)
    {
        if (labelEntries is null) {
            labelEntries = new LabelEntry[16];
            labelTable = new LabelEntry[16];
        }
        int hashCode = label.GetHashCode();
        LabelEntry labelEntry = labelEntries[hashCode % labelEntries.Length];
        while (labelEntry != null && labelEntry.label != label) {
            labelEntry = labelEntry.next;
        }
        if (labelEntry is not null) {
            return labelEntry;
        }
            
        if (labelCount > (labelEntries.Length * 3) / 4) {
            int currentCapacity = labelEntries.Length;
            int newCapacity = currentCapacity * 2 + 1;
            LabelEntry[] newLabelEntries = new LabelEntry[newCapacity];
            for (int i = currentCapacity - 1; i >= 0; --i) {
                LabelEntry currentEntry = labelEntries[i];
                while (currentEntry != null) {
                    int newCurrentEntryIndex = currentEntry.label.GetHashCode() % newCapacity;
                    LabelEntry nextEntry = currentEntry.next;
                    currentEntry.next = newLabelEntries[newCurrentEntryIndex];
                    newLabelEntries[newCurrentEntryIndex] = currentEntry;
                    currentEntry = nextEntry;
                }
            }
            labelEntries = newLabelEntries;
        }
        if (labelCount == labelTable.Length) {
            LabelEntry[] newLabelTable = new LabelEntry[2 * labelTable.Length];
            Array.Copy(labelTable, 0, newLabelTable, 0, labelTable.Length);
            labelTable = newLabelTable;
        }
            
        labelEntry = new LabelEntry(labelCount, label);
        int index = hashCode % labelEntries.Length;
        labelEntry.next = labelEntries[index];
        labelEntries[index] = labelEntry;
        labelTable[labelCount++] = labelEntry;
        return labelEntry;
    }

    // -----------------------------------------------------------------------------------------------
    // Static helper methods to compute hash codes.
    // -----------------------------------------------------------------------------------------------

    private static int Hash(int tag, int value)
    {
        return 0x7FFFFFFF & (tag + value);
    }

    private static int Hash(int tag, long value)
    {
        return 0x7FFFFFFF & (tag + (int)value + (int)(value >>> 32));
    }

    private static int Hash(int tag, string value)
    {
        return 0x7FFFFFFF & (tag + value.GetHashCode());
    }

    private static int Hash(int tag, string value1, int value2)
    {
        return 0x7FFFFFFF & (tag + value1.GetHashCode() + value2);
    }

    private static int Hash(int tag, string value1, string value2)
    {
        return 0x7FFFFFFF & (tag + value1.GetHashCode() * value2.GetHashCode());
    }

    private static int Hash(int tag, string value1, string value2, int value3)
    {
        return 0x7FFFFFFF & (tag + value1.GetHashCode() * value2.GetHashCode() * (value3 + 1));
    }

    private static int Hash(int tag, string value1, string value2, string value3)
    {
        return 0x7FFFFFFF & (tag + value1.GetHashCode() * value2.GetHashCode() * value3.GetHashCode());
    }

    private static int Hash(int tag, string value1, string value2, string value3, int value4)
    {
        return 0x7FFFFFFF & (tag + value1.GetHashCode() * value2.GetHashCode() * value3.GetHashCode() * value4);
    }

    /// <summary>
    ///     An entry of a SymbolTable. This concrete and private subclass of <see cref="Symbol" /> adds two fields
    ///     which are only used inside SymbolTable, to implement hash sets of symbols (in order to avoid
    ///     duplicate symbols). See <see cref="SymbolTable._entries" />.
    ///     @author Eric Bruneton
    /// </summary>
    private sealed class Entry : Symbol
    {
        /// <summary>
        ///     The hash code of this entry.
        /// </summary>
        internal readonly int hashCode;

        /// <summary>
        ///     Another entry (and so on recursively) having the same hash code (modulo the size of <see cref="entries"/>) as this one.
        /// </summary>
        internal Entry next;

        public Entry(int index, int tag, string owner, string name, string value, long data, int hashCode) : base(
            index, tag, owner, name, value, data)
        {
            this.hashCode = hashCode;
        }

        public Entry(int index, int tag, string value, int hashCode) : base(index, tag, null, null, value, 0)
        {
            this.hashCode = hashCode;
        }

        public Entry(int index, int tag, string value, long data, int hashCode) : base(index, tag, null, null,
            value, data)
        {
            this.hashCode = hashCode;
        }

        public Entry(int index, int tag, string name, string value, int hashCode) : base(index, tag, null, name,
            value, 0)
        {
            this.hashCode = hashCode;
        }

        public Entry(int index, int tag, long data, int hashCode) : base(index, tag, null, null, null, data)
        {
            this.hashCode = hashCode;
        }
    }

    /**
     * A label corresponding to a "forward uninitialized" type in the ASM specific {@link
     * SymbolTable#typeTable} (see {@link Symbol#FORWARD_UNINITIALIZED_TYPE_TAG}).
     *
     * @author Eric Bruneton
     */
    private sealed class LabelEntry {

        /// <summary>
        /// The index of this label entry in the <see cref="SymbolTable.labelTable"/> array.
        /// </summary>
        internal readonly int index;

        /// <summary>
        /// The value of this label entry.
        /// </summary>
        internal readonly Label label;

        /// <summary>
        /// Another entry (and so on recursively) having the same hash code (modulo the size of <see cref="SymbolTable.labelEntries"/>) as this one.
        /// </summary>
        internal LabelEntry next;

        internal LabelEntry(int index, Label label) {
            this.index = index;
            this.label = label;
        }
    }
}