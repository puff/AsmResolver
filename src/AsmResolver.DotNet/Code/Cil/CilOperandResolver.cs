using System;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace AsmResolver.DotNet.Code.Cil
{
    /// <summary>
    /// Provides a default implementation of the <see cref="ICilOperandResolver"/> class.
    /// </summary>
    public class CilOperandResolver : ICilOperandResolver
    {
        private readonly ModuleDefinition _contextModule;
        private readonly CilMethodBody _methodBody;

        /// <summary>
        /// Creates a new instance of the <see cref="CilOperandResolver"/> class.
        /// </summary>
        /// <param name="contextModule">The context module.</param>
        /// <param name="methodBody">The method body that references the operands.</param>
        public CilOperandResolver(ModuleDefinition contextModule, CilMethodBody methodBody)
        {
            _contextModule = contextModule ?? throw new ArgumentNullException(nameof(contextModule));
            _methodBody = methodBody ?? throw new ArgumentNullException(nameof(methodBody));
        }

        /// <inheritdoc />
        public object ResolveMember(MetadataToken token)
        {
            _contextModule.TryLookupMember(token, out var member);
            return member;
        }

        /// <inheritdoc />
        public object ResolveString(MetadataToken token)
        {
            _contextModule.TryLookupString(token, out string value);
            return value;
        }

        /// <inheritdoc />
        public object ResolveLocalVariable(int index)
        {
            var locals = _methodBody.LocalVariables;
            return index >= 0 && index < locals.Count ? locals[index] : null;
        }

        /// <inheritdoc />
        public object ResolveParameter(int index)
        {
            var parameters = _methodBody.Owner.Parameters;
            return parameters.ContainsSignatureIndex(index) ? parameters.GetBySignatureIndex(index) : null;
        }
    }
}
