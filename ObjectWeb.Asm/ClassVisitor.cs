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
/// A visitor to visit a Java class. The methods of this class must be called in the following order:
/// <c>visit</c> [ <c>visitSource</c> ] [ <c>visitModule</c> ][ <c>visitNestHost</c> ][ <c>visitOuterClass</c> ] ( <c>visitAnnotation</c> | <c>visitTypeAnnotation</c> | <c>visitAttribute</c> )* ( <c>visitNestMember</c> | [ <c>* visitPermittedSubclass</c> ] | <c>visitInnerClass</c> | <c>visitRecordComponent</c> | <c>visitField</c> | <c>visitMethod</c> )*
/// <c>visitEnd</c>.
/// 
/// @author Eric Bruneton
/// </summary>
public abstract class ClassVisitor
{
    /// <summary>
    /// The ASM API version implemented by this visitor. The value of this field must be one of the
    /// <c>ASM</c><i>x</i> Values in <see cref="Opcodes"/>.
    /// </summary>
    protected internal readonly int api;

    /// <summary>
    /// The class visitor to which this visitor must delegate method calls. May be null. </summary>
    protected internal ClassVisitor cv;

    /// <summary>
    /// Constructs a new <see cref="ClassVisitor"/>.
    /// </summary>
    /// <param name="api"> the ASM API version implemented by this visitor. Must be one of the <c>ASM</c><i>x</i> Values in <see cref="Opcodes"/>. </param>
    public ClassVisitor(int api) : this(api, null)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="ClassVisitor"/>.
    /// </summary>
    /// <param name="api"> the ASM API version implemented by this visitor. Must be one of the <c>ASM</c><i>x</i> Values in <see cref="Opcodes"/>. </param>
    /// <param name="classVisitor"> the class visitor to which this visitor must delegate method calls. May be
    ///     null. </param>
    public ClassVisitor(int api, ClassVisitor classVisitor)
    {
        if (api != Opcodes.Asm9 && api != Opcodes.Asm8 && api != Opcodes.Asm7 && api != Opcodes.Asm6 &&
            api != Opcodes.Asm5 && api != Opcodes.Asm4 && api != Opcodes.Asm10_Experimental)
        {
            throw new System.ArgumentException("Unsupported api " + api);
        }

        if (api == Opcodes.Asm10_Experimental)
        {
            Constants.CheckAsmExperimental(this);
        }

        this.api = api;
        this.cv = classVisitor;
    }

    /// <summary>
    /// The class visitor to which this visitor must delegate method calls. May be <c>null</c>.
    /// </summary>
    public ClassVisitor Delegate => cv;

    /// <summary>
    /// Visits the header of the class.
    /// </summary>
    /// <param name="version"> the class version. The minor version is stored in the 16 most significant bits,
    ///     and the major version in the 16 least significant bits. </param>
    /// <param name="access"> the class's access flags (see <see cref="Opcodes"/>). This parameter also indicates if
    ///     the class is deprecated (<see cref="Opcodes.Acc_Deprecated"/>) or a record <see cref="Opcodes.Acc_Record"/>. </param>
    /// <param name="name"> the internal name of the class (see <see cref="JType.InternalName"/>). </param>
    /// <param name="signature"> the signature of this class. May be null if the class is not a
    ///     generic one, and does not extend or implement generic classes or interfaces. </param>
    /// <param name="superName"> the internal of name of the super class (see <see cref="JType.InternalName"/>).
    ///     For interfaces, the super class is <see cref="object"/>. May be null, but only for the
    ///     <see cref="object"/> class. </param>
    /// <param name="interfaces"> the internal names of the class's interfaces (see <see cref="JType.InternalName"/>). May be null. </param>
    public virtual void Visit(int version, int access, string name, string signature, string superName,
        string[] interfaces)
    {
        if (api < Opcodes.Asm8 && (access & Opcodes.Acc_Record) != 0)
        {
            throw new System.NotSupportedException("Records requires ASM8");
        }

        if (cv != null)
        {
            cv.Visit(version, access, name, signature, superName, interfaces);
        }
    }

    /// <summary>
    /// Visits the source of the class.
    /// </summary>
    /// <param name="source"> the name of the source file from which the class was compiled. May be <c>null</c>. </param>
    /// <param name="debug"> additional debug information to compute the correspondence between source and
    ///     compiled elements of the class. May be null. </param>
    public virtual void VisitSource(string source, string debug)
    {
        if (cv != null)
        {
            cv.VisitSource(source, debug);
        }
    }

    /// <summary>
    /// Visit the module corresponding to the class.
    /// </summary>
    /// <param name="name"> the fully qualified name (using dots) of the module. </param>
    /// <param name="access"> the module access flags, among <c>ACC_OPEN</c>, <c>ACC_SYNTHETIC</c> and <c>ACC_MANDATED</c>. </param>
    /// <param name="version"> the module version, or null. </param>
    /// <returns> a visitor to visit the module Values, or null if this visitor is not
    ///     interested in visiting this module. </returns>
    public virtual ModuleVisitor VisitModule(string name, int access, string version)
    {
        if (api < Opcodes.Asm6)
        {
            throw new System.NotSupportedException("Module requires ASM6");
        }

        if (cv != null)
        {
            return cv.VisitModule(name, access, version);
        }

        return null;
    }

    /// <summary>
    /// Visits the nest host class of the class. A nest is a set of classes of the same package that
    /// share access to their private members. One of these classes, called the host, lists the other
    /// members of the nest, which in turn should link to the host of their nest. This method must be
    /// called only once and only if the visited class is a non-host member of a nest. A class is
    /// implicitly its own nest, so it's invalid to call this method with the visited class name as
    /// argument.
    /// </summary>
    /// <param name="nestHost"> the internal name of the host class of the nest (see <see cref="JType.InternalName"/>). </param>
    public virtual void VisitNestHost(string nestHost)
    {
        if (api < Opcodes.Asm7)
        {
            throw new System.NotSupportedException("NestHost requires ASM7");
        }

        if (cv != null)
        {
            cv.VisitNestHost(nestHost);
        }
    }

    /// <summary>
    /// Visits the enclosing class of the class. This method must be called only if this class is a
    /// local or anonymous class. See the JVMS 4.7.7 section for more details.
    /// </summary>
    /// <param name="owner"> internal name of the enclosing class of the class (see <see cref="JType.InternalName"/>). </param>
    /// <param name="name"> the name of the method that contains the class, or <c>null</c> if the class is
    ///     not enclosed in a method or constructor of its enclosing class (e.g. if it is enclosed in
    ///     an instance initializer, static initializer, instance variable initializer, or class
    ///     variable initializer). </param>
    /// <param name="descriptor"> the descriptor of the method that contains the class, or <c>null</c> if
    ///     the class is not enclosed in a method or constructor of its enclosing class (e.g. if it is
    ///     enclosed in an instance initializer, static initializer, instance variable initializer, or
    ///     class variable initializer). </param>
    public virtual void VisitOuterClass(string owner, string name, string descriptor)
    {
        if (cv != null)
        {
            cv.VisitOuterClass(owner, name, descriptor);
        }
    }

    /// <summary>
    /// Visits an annotation of the class.
    /// </summary>
    /// <param name="descriptor"> the class descriptor of the annotation class. </param>
    /// <param name="visible"> <c>true</c> if the annotation is visible at runtime. </param>
    /// <returns> a visitor to visit the annotation Values, or null if this visitor is not
    ///     interested in visiting this annotation. </returns>
    public virtual AnnotationVisitor VisitAnnotation(string descriptor, bool visible)
    {
        if (cv != null)
        {
            return cv.VisitAnnotation(descriptor, visible);
        }

        return null;
    }

    /// <summary>
    /// Visits an annotation on a type in the class signature.
    /// </summary>
    /// <param name="typeRef"> a reference to the annotated type. The sort of this type reference must be
    ///     <see cref="TypeReference.Class_Type_Parameter"/>, <see cref="TypeReference.Class_Type_Parameter_Bound"/> or <see cref="TypeReference.Class_Extends"/>. See
    ///     <see cref="TypeReference"/>. </param>
    /// <param name="typePath"> the path to the annotated type argument, wildcard bound, array element type, or
    ///     static inner type within 'typeRef'. May be null if the annotation targets
    ///     'typeRef' as a whole. </param>
    /// <param name="descriptor"> the class descriptor of the annotation class. </param>
    /// <param name="visible"> <c>true</c> if the annotation is visible at runtime. </param>
    /// <returns> a visitor to visit the annotation Values, or null if this visitor is not
    ///     interested in visiting this annotation. </returns>
    public virtual AnnotationVisitor VisitTypeAnnotation(int typeRef, TypePath typePath, string descriptor,
        bool visible)
    {
        if (api < Opcodes.Asm5)
        {
            throw new System.NotSupportedException("TypeAnnotation requires ASM5");
        }

        if (cv != null)
        {
            return cv.VisitTypeAnnotation(typeRef, typePath, descriptor, visible);
        }

        return null;
    }

    /// <summary>
    /// Visits a non standard attribute of the class.
    /// </summary>
    /// <param name="attribute"> an attribute. </param>
    public virtual void VisitAttribute(Attribute attribute)
    {
        if (cv != null)
        {
            cv.VisitAttribute(attribute);
        }
    }

    /// <summary>
    /// Visits a member of the nest. A nest is a set of classes of the same package that share access
    /// to their private members. One of these classes, called the host, lists the other members of the
    /// nest, which in turn should link to the host of their nest. This method must be called only if
    /// the visited class is the host of a nest. A nest host is implicitly a member of its own nest, so
    /// it's invalid to call this method with the visited class name as argument.
    /// </summary>
    /// <param name="nestMember"> the internal name of a nest member (see <see cref="JType.InternalName"/>). </param>
    public virtual void VisitNestMember(string nestMember)
    {
        if (api < Opcodes.Asm7)
        {
            throw new System.NotSupportedException("NestMember requires ASM7");
        }

        if (cv != null)
        {
            cv.VisitNestMember(nestMember);
        }
    }

    /// <summary>
    /// Visits a permitted subclasses. A permitted subclass is one of the allowed subclasses of the
    /// current class.
    /// </summary>
    /// <param name="permittedSubclass"> the internal name of a permitted subclass (see <see cref="JType.InternalName"/>). </param>
    public virtual void VisitPermittedSubclass(string permittedSubclass)
    {
        if (api < Opcodes.Asm9)
        {
            throw new System.NotSupportedException("PermittedSubclasses requires ASM9");
        }

        if (cv != null)
        {
            cv.VisitPermittedSubclass(permittedSubclass);
        }
    }

    /// <summary>
    /// Visits information about an inner class. This inner class is not necessarily a member of the
    /// class being visited. More precisely, every class or interface C which is referenced by this
    /// class and which is not a package member must be visited with this method. This class must
    /// reference its nested class or interface members, and its enclosing class, if any. See the JVMS
    /// 4.7.6 section for more details.
    /// </summary>
    /// <param name="name"> the internal name of C (see <see cref="JType.InternalName"/>). </param>
    /// <param name="outerName"> the internal name of the class or interface C is a member of (see <see cref="JType.InternalName"/>).
    /// Must be <c>null</c> if C is not the member of a class or interface (e.g. for local or anonymous classes). </param>
    /// <param name="innerName"> the (simple) name of C. Must be <c>null</c> for anonymous inner classes. </param>
    /// <param name="access"> the access flags of C originally declared in the source code from which this
    /// class was compiled. </param>
    public virtual void VisitInnerClass(string name, string outerName, string innerName, int access)
    {
        if (cv != null)
        {
            cv.VisitInnerClass(name, outerName, innerName, access);
        }
    }

    /// <summary>
    /// Visits a record component of the class.
    /// </summary>
    /// <param name="name"> the record component name. </param>
    /// <param name="descriptor"> the record component descriptor (see <see cref="Type"/>). </param>
    /// <param name="signature"> the record component signature. May be null if the record component
    ///     type does not use generic types. </param>
    /// <returns> a visitor to visit this record component annotations and attributes, or null
    ///     if this class visitor is not interested in visiting these annotations and attributes. </returns>
    public virtual RecordComponentVisitor VisitRecordComponent(string name, string descriptor, string signature)
    {
        if (api < Opcodes.Asm8)
        {
            throw new System.NotSupportedException("Record requires ASM8");
        }

        if (cv != null)
        {
            return cv.VisitRecordComponent(name, descriptor, signature);
        }

        return null;
    }

    /// <summary>
    /// Visits a field of the class.
    /// </summary>
    /// <param name="access"> the field's access flags (see <see cref="Opcodes"/>). This parameter also indicates if
    ///     the field is synthetic and/or deprecated. </param>
    /// <param name="name"> the field's name. </param>
    /// <param name="descriptor"> the field's descriptor (see <see cref="Type"/>). </param>
    /// <param name="signature"> the field's signature. May be null if the field's type does not use
    ///     generic types. </param>
    /// <param name="value"> the field's initial value. This parameter, which may be null if the
    ///     field does not have an initial value, must be an <see cref="int"/>, a <see cref="float"/>, a <see cref="Long"/>, a <see cref="double"/> or a <see cref="string"/> (for <c>int</c>, <c>float</c>, <c>long</c>
    ///     or <c>String</c> fields respectively). <i>This parameter is only used for static
    ///     fields</i>. Its value is ignored for non static fields, which must be initialized through
    ///     bytecode instructions in constructors or methods. </param>
    /// <returns> a visitor to visit field annotations and attributes, or <c>null</c> if this class
    ///     visitor is not interested in visiting these annotations and attributes. </returns>
    public virtual FieldVisitor VisitField(int access, string name, string descriptor, string signature,
        object value)
    {
        if (cv != null)
        {
            return cv.VisitField(access, name, descriptor, signature, value);
        }

        return null;
    }

    /// <summary>
    /// Visits a method of the class. This method <i>must</i> return a new <see cref="MethodVisitor"/>
    /// instance (or <c>null</c>) each time it is called, i.e., it should not return a previously
    /// returned visitor.
    /// </summary>
    /// <param name="access"> the method's access flags (see <see cref="Opcodes"/>). This parameter also indicates if
    ///     the method is synthetic and/or deprecated. </param>
    /// <param name="name"> the method's name. </param>
    /// <param name="descriptor"> the method's descriptor (see <see cref="Type"/>). </param>
    /// <param name="signature"> the method's signature. May be <c>null</c> if the method parameters,
    ///     return type and exceptions do not use generic types. </param>
    /// <param name="exceptions"> the internal names of the method's exception classes (see <see cref="JType.InternalName"/>). May be <c>null</c>. </param>
    /// <returns> an object to visit the byte code of the method, or <c>null</c> if this class
    ///     visitor is not interested in visiting the code of this method. </returns>
    public virtual MethodVisitor VisitMethod(int access, string name, string descriptor, string signature,
        string[] exceptions)
    {
        if (cv != null)
        {
            return cv.VisitMethod(access, name, descriptor, signature, exceptions);
        }

        return null;
    }

    /// <summary>
    /// Visits the end of the class. This method, which is the last one to be called, is used to inform
    /// the visitor that all the fields and methods of the class have been visited.
    /// </summary>
    public virtual void VisitEnd()
    {
        if (cv != null)
        {
            cv.VisitEnd();
        }
    }
}