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

namespace ObjectWeb.Asm.Tree;

/// <summary>
/// A node that represents an inner class. This inner class is not necessarily a member of the <see cref="ClassNode"/>
/// containing this object. More precisely, every class or interface C which is referenced
/// by a <see cref="ClassNode"/> and which is not a package member must be represented with an <see cref="InnerClassNode"/>.
/// The <see cref="ClassNode"/> must reference its nested class or interface members, and
/// its enclosing class, if any. See the JVMS 4.7.6 section for more details.
/// 
/// @author Eric Bruneton
/// </summary>
public class InnerClassNode
{
    /// <summary>
    /// The internal name of an inner class (see <see cref="JType.InternalName"/>). </summary>
    public string Name { get; set; }

    /// <summary>
    /// The internal name of the class to which the inner class belongs (see <see cref="JType.InternalName"/>).
    /// May be <c>null</c>.
    /// </summary>
    public string OuterName { get; set; }

    /// <summary>
    /// The (simple) name of the inner class inside its enclosing class. Must be <c>null</c> if the
    /// inner class is not the member of a class or interface (e.g. for local or anonymous classes).
    /// </summary>
    public string InnerName { get; set; }

    /// <summary>
    /// The access flags of the inner class as originally declared in the source code from which the class was compiled. </summary>
    public int Access { get; set; }

    /// <summary>
    /// Constructs a new <see cref="InnerClassNode"/> for an inner class C.
    /// </summary>
    /// <param name = "name"> the internal name of C (see <see cref="JType.InternalName"/>). </param>
    /// <param name = "outerName"> the internal name of the class or interface C is a member of (see <see cref="JType.InternalName"/>).
    ///     Must be <c>null</c> if C is not the member of a class or interface (e.g. for local or anonymous classes). </param>
    /// <param name = "innerName"> the (simple) name of C. Must be <c>null</c> for anonymous inner classes. </param>
    /// <param name = "access"> the access flags of the C as originally declared in the source code from which this class was compiled. </param>
    public InnerClassNode(string name, string outerName, string innerName, int access)
    {
        this.Name = name;
        this.OuterName = outerName;
        this.InnerName = innerName;
        this.Access = access;
    }

    /// <summary>
    /// Makes the given class visitor visit this inner class.
    /// </summary>
    /// <param name = "classVisitor"> a class visitor. </param>
    public virtual void Accept(ClassVisitor classVisitor)
    {
        classVisitor.VisitInnerClass(Name, OuterName, InnerName, Access);
    }
}