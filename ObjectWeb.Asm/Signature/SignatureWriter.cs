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
namespace ObjectWeb.Asm.Signature;

/// <summary>
/// A SignatureVisitor that generates signature literals, as defined in the Java Virtual Machine
/// Specification (JVMS).
/// </summary>
/// <seealso cref= <a href="https://docs.oracle.com/javase/specs/jvms/se9/html/jvms-4.html#jvms-4.7.9.1">JVMS
///     4.7.9.1</a>
/// @author Thomas Hallgren
/// @author Eric Bruneton </seealso>
public class SignatureWriter : SignatureVisitor
{
    /// <summary>
    /// The builder used to construct the visited signature. </summary>
    private readonly StringBuilder _stringBuilder = new StringBuilder();

    /// <summary>
    /// Whether the visited signature contains formal type parameters. </summary>
    private bool _hasFormals;

    /// <summary>
    /// Whether the visited signature contains method parameter types. </summary>
    private bool _hasParameters;

    /// <summary>
    /// The stack used to keep track of class types that have arguments. Each element of this stack is
    /// a boolean encoded in one bit. The top of the stack is the least significant bit. The bottom of
    /// the stack is a sentinel element always equal to 1 (used to detect when the stack is full).
    /// Pushing false = <c>&lt;&lt;= 1</c>, pushing true = <c>( &lt;&lt;= 1) | 1</c>, popping = <c>&gt;&gt;&gt;= 1</c>.
    /// 
    /// <para>Class type arguments must be surrounded with '&lt;' and '&gt;' and, because
    /// 
    /// <ol>
    ///   <li>class types can be nested (because type arguments can themselves be class types)</li>,
    ///   <li>SignatureWriter always returns 'this' in each visit* method (to avoid allocating new
    ///       SignatureWriter instances)</li>,
    /// </ol>
    /// 
    /// </para>
    /// <para>we need a stack to properly balance these angle brackets. A new element is pushed on this
    /// stack for each new visited type, and popped when the visit of this type ends (either in
    /// visitEnd, or because visitInnerClassType is called).
    /// </para>
    /// </summary>
    private int _argumentStack = 1;

    /// <summary>
    /// Constructs a new <see cref="SignatureWriter"/>. </summary>
    public SignatureWriter() : this(new StringBuilder())
    {
    }

    private SignatureWriter(StringBuilder stringBuilder) : base(/* latest api =*/ Opcodes.Asm9)
    {
        _stringBuilder = stringBuilder;
    }

    // -----------------------------------------------------------------------------------------------
    // Implementation of the SignatureVisitor interface
    // -----------------------------------------------------------------------------------------------

    public override void VisitFormalTypeParameter(string name)
    {
        if (!_hasFormals)
        {
            _hasFormals = true;
            _stringBuilder.Append('<');
        }

        _stringBuilder.Append(name);
        _stringBuilder.Append(':');
    }

    public override SignatureVisitor VisitClassBound()
    {
        return this;
    }

    public override SignatureVisitor VisitInterfaceBound()
    {
        _stringBuilder.Append(':');
        return this;
    }

    public override SignatureVisitor VisitSuperclass()
    {
        EndFormals();
        return this;
    }

    public override SignatureVisitor VisitInterface()
    {
        return this;
    }

    public override SignatureVisitor VisitParameterType()
    {
        EndFormals();
        if (!_hasParameters)
        {
            _hasParameters = true;
            _stringBuilder.Append('(');
        }

        return this;
    }

    public override SignatureVisitor VisitReturnType()
    {
        EndFormals();
        if (!_hasParameters)
        {
            _stringBuilder.Append('(');
        }

        _stringBuilder.Append(')');
        return this;
    }

    public override SignatureVisitor VisitExceptionType()
    {
        _stringBuilder.Append('^');
        return this;
    }

    public override void VisitBaseType(char descriptor)
    {
        _stringBuilder.Append(descriptor);
    }

    public override void VisitTypeVariable(string name)
    {
        _stringBuilder.Append('T');
        _stringBuilder.Append(name);
        _stringBuilder.Append(';');
    }

    public override SignatureVisitor VisitArrayType()
    {
        _stringBuilder.Append('[');
        return this;
    }

    public override void VisitClassType(string name)
    {
        _stringBuilder.Append('L');
        _stringBuilder.Append(name);
        // Pushes 'false' on the stack, meaning that this type does not have type arguments (as far as
        // we can tell at this point).
        _argumentStack <<= 1;
    }

    public override void VisitInnerClassType(string name)
    {
        EndArguments();
        _stringBuilder.Append('.');
        _stringBuilder.Append(name);
        // Pushes 'false' on the stack, meaning that this type does not have type arguments (as far as
        // we can tell at this point).
        _argumentStack <<= 1;
    }

    public override void VisitTypeArgument()
    {
        // If the top of the stack is 'false', this means we are visiting the first type argument of the
        // currently visited type. We therefore need to append a '<', and to replace the top stack
        // element with 'true' (meaning that the current type does have type arguments).
        if ((_argumentStack & 1) == 0)
        {
            _argumentStack |= 1;
            _stringBuilder.Append('<');
        }

        _stringBuilder.Append('*');
    }

    public override SignatureVisitor VisitTypeArgument(char wildcard)
    {
        // If the top of the stack is 'false', this means we are visiting the first type argument of the
        // currently visited type. We therefore need to append a '<', and to replace the top stack
        // element with 'true' (meaning that the current type does have type arguments).
        if ((_argumentStack & 1) == 0)
        {
            _argumentStack |= 1;
            _stringBuilder.Append('<');
        }

        if (wildcard != '=')
        {
            _stringBuilder.Append(wildcard);
        }

        // If the stack is full, start a nested one by returning a new SignatureWriter.
        return (_argumentStack & (1 << 31)) == 0 ? this : new(_stringBuilder);
    }

    public override void VisitEnd()
    {
        EndArguments();
        _stringBuilder.Append(';');
    }

    /// <summary>
    /// Returns the signature that was built by this signature writer.
    /// </summary>
    /// <returns> the signature that was built by this signature writer. </returns>
    public override string ToString()
    {
        return _stringBuilder.ToString();
    }

    // -----------------------------------------------------------------------------------------------
    // Utility methods
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Ends the formal type parameters section of the signature. </summary>
    private void EndFormals()
    {
        if (_hasFormals)
        {
            _hasFormals = false;
            _stringBuilder.Append('>');
        }
    }

    /// <summary>
    /// Ends the type arguments of a class or inner class type. </summary>
    private void EndArguments()
    {
        // If the top of the stack is 'true', this means that some type arguments have been visited for
        // the type whose visit is now ending. We therefore need to append a '>', and to pop one element
        // from the stack.
        if ((_argumentStack & 1) == 1)
        {
            _stringBuilder.Append('>');
        }

        _argumentStack >>>= 2;
    }
}