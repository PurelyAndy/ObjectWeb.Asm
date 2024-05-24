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
///     A visitor to visit a Java module. The methods of this class must be called in the following
///     order: ( <c>visitMainClass</c> | ( <c>visitPackage</c> | <c>visitRequire</c> | <c>visitExport</c> | <c>visitOpen</c> | <c>visitUse</c> | <c>visitProvide</c> )* ) <c>visitEnd</c>.
///     @author Remi Forax
///     @author Eric Bruneton
/// </summary>
public abstract class ModuleVisitor
{
    /// <summary>
    ///     The ASM API version implemented by this visitor. The value of this field must be one of <see cref="Opcodes.ASM6"/> or <see cref="Opcodes.Asm7 />.
    /// </summary>
    protected internal readonly int api;

    /// <summary>
    ///     The module visitor to which this visitor must delegate method calls. May be <c>null</c>.
    /// </summary>
    protected internal ModuleVisitor mv;

    /// <summary>
    ///     Constructs a new <see cref="ModuleVisitor" />.
    /// </summary>
    /// <param name="api">
    ///     the ASM API version implemented by this visitor. Must be one of <see cref="Opcodes.Asm6 />
    ///     or <see cref="Opcodes.Asm7 />.
    /// </param>
    public ModuleVisitor(int api) : this(api, null)
    {
    }

    /// <summary>
    ///     Constructs a new <see cref="ModuleVisitor" />.
    /// </summary>
    /// <param name="api">
    ///     the ASM API version implemented by this visitor. Must be one of <see cref="Opcodes.Asm6 />
    ///     or <see cref="Opcodes.Asm7 />.
    /// </param>
    /// <param name="moduleVisitor">
    ///     the module visitor to which this visitor must delegate method calls. May
    ///     be null.
    /// </param>
    public ModuleVisitor(int api, ModuleVisitor moduleVisitor)
    {
        if (api != Opcodes.Asm9 && api != Opcodes.Asm8 && api != Opcodes.Asm7 && api != Opcodes.Asm6 &&
            api != Opcodes.Asm5 && api != Opcodes.Asm4 &&
            api != Opcodes.Asm10_Experimental) throw new ArgumentException("Unsupported api " + api);
        if (api == Opcodes.Asm10_Experimental) Constants.CheckAsmExperimental(this);
        this.api = api;
        mv = moduleVisitor;
    }
        
    /// <summary>
    /// The module visitor to which this visitor must delegate method calls. May be <c>null</c>.
    /// </summary>
    public ModuleVisitor Delegate => mv;

    /// <summary>
    ///     Visit the main class of the current module.
    /// </summary>
    /// <param name="mainClass"> the internal name of the main class of the current module (see <see cref="JType.InternalName"/>). </param>
    public virtual void VisitMainClass(string mainClass)
    {
        if (mv != null) mv.VisitMainClass(mainClass);
    }

    /// <summary>
    ///     Visit a package of the current module.
    /// </summary>
    /// <param name="packaze"> the internal name of a package (see <see cref="JType.InternalName"/>). </param>
    public virtual void VisitPackage(string packaze)
    {
        if (mv != null) mv.VisitPackage(packaze);
    }

    /// <summary>
    ///     Visits a dependence of the current module.
    /// </summary>
    /// <param name="module"> the fully qualified name (using dots) of the dependence. </param>
    /// <param name="access">
    ///     the access flag of the dependence among <c>ACC_TRANSITIVE</c>, <c>ACC_STATIC_PHASE</c>, <c>ACC_SYNTHETIC</c> and <c>ACC_MANDATED</c>.
    /// </param>
    /// <param name="version"> the module version at compile time, or <c>null</c>. </param>
    public virtual void VisitRequire(string module, int access, string version)
    {
        if (mv != null) mv.VisitRequire(module, access, version);
    }

    /// <summary>
    ///     Visit an exported package of the current module.
    /// </summary>
    /// <param name="packaze"> the internal name of the exported package (see <see cref="JType.InternalName"/>). </param>
    /// <param name="access">
    ///     the access flag of the exported package, valid values are among <c>ACC_SYNTHETIC</c> and <c>ACC_MANDATED</c>.
    /// </param>
    /// <param name="modules">
    ///     the fully qualified names (using dots) of the modules that can access the public
    ///     classes of the exported package, or <c>null</c>.
    /// </param>
    public virtual void VisitExport(string packaze, int access, params string[] modules)
    {
        if (mv != null) mv.VisitExport(packaze, access, modules);
    }

    /// <summary>
    ///     Visit an open package of the current module.
    /// </summary>
    /// <param name="packaze"> the internal name of the opened package (see <see cref="JType.InternalName"/>). </param>
    /// <param name="access">
    ///     the access flag of the opened package, valid values are among <c>ACC_SYNTHETIC</c> and <c>ACC_MANDATED</c>.
    /// </param>
    /// <param name="modules">
    ///     the fully qualified names (using dots) of the modules that can use deep
    ///     reflection to the classes of the open package, or <c>null</c>.
    /// </param>
    public virtual void VisitOpen(string packaze, int access, params string[] modules)
    {
        if (mv != null) mv.VisitOpen(packaze, access, modules);
    }

    /// <summary>
    ///     Visit a service used by the current module. The name must be the internal name of an interface
    ///     or a class.
    /// </summary>
    /// <param name="service"> the internal name of the service (see <see cref="JType.InternalName"/>). </param>
    public virtual void VisitUse(string service)
    {
        if (mv != null) mv.VisitUse(service);
    }

    /// <summary>
    ///     Visit an implementation of a service.
    /// </summary>
    /// <param name="service"> the internal name of the service (see <see cref="JType.InternalName"/>). </param>
    /// <param name="providers">
    ///     the internal names (see <see cref="JType.InternalName"/>) of the implementations of the service (there is at least
    ///     one provider).
    /// </param>
    public virtual void VisitProvide(string service, params string[] providers)
    {
        if (mv != null) mv.VisitProvide(service, providers);
    }

    /// <summary>
    ///     Visits the end of the module. This method, which is the last one to be called, is used to
    ///     inform the visitor that everything have been visited.
    /// </summary>
    public virtual void VisitEnd()
    {
        if (mv != null) mv.VisitEnd();
    }
}