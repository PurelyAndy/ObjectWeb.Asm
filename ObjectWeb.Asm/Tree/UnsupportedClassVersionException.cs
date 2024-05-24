using System;

namespace ObjectWeb.Asm.Tree;

/// <summary>
/// Exception thrown in <see cref="AnnotationNode.Check"/>, <see cref="ClassNode.Check"/>, <see cref="FieldNode.check"/> and <see cref="MethodNode.Check"/> when these nodes (or their children, recursively)
/// contain elements that were introduced in more recent versions of the ASM API than version passed
/// to these methods.
/// 
/// @author Eric Bruneton
/// </summary>
public class UnsupportedClassVersionException : Exception
{
    private const long SerialVersionUid = -3502347765891805831L;
}