using System.Collections.Generic;
using System.Threading;
using AsmResolver.Collections;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace AsmResolver.DotNet
{
    /// <summary>
    /// Represents a reference to a type defined in a .NET assembly.
    /// </summary>
    public class TypeReference :
        MetadataMember,
        ITypeDefOrRef,
        IResolutionScope
    {
        private readonly LazyVariable<TypeReference, Utf8String?> _name;
        private readonly LazyVariable<TypeReference, Utf8String?> _namespace;
        private readonly LazyVariable<TypeReference, IResolutionScope?> _scope;
        private IList<CustomAttribute>? _customAttributes;

        /// <summary>
        /// Initializes a new empty type reference.
        /// </summary>
        /// <param name="token">The token of the type reference.</param>
        protected TypeReference(MetadataToken token)
            : base(token)
        {
            _name = new LazyVariable<TypeReference, Utf8String?>(x => x.GetName());
            _namespace = new LazyVariable<TypeReference, Utf8String?>(x => x.GetNamespace());
            _scope = new LazyVariable<TypeReference, IResolutionScope?>(x => x.GetScope());
        }

        /// <summary>
        /// Creates a new reference to a type.
        /// </summary>
        /// <param name="scope">The scope that defines the type.</param>
        /// <param name="ns">The namespace the type resides in.</param>
        /// <param name="name">The name of the type.</param>
        public TypeReference(IResolutionScope? scope, Utf8String? ns, Utf8String? name)
            : this(new MetadataToken(TableIndex.TypeRef, 0))
        {
            _scope.SetValue(scope);
            Namespace = ns;
            Name = name;
        }

        /// <summary>
        /// Creates a new reference to a type.
        /// </summary>
        /// <param name="module">The module that references the type.</param>
        /// <param name="scope">The scope that defines the type.</param>
        /// <param name="ns">The namespace the type resides in.</param>
        /// <param name="name">The name of the type.</param>
        public TypeReference(ModuleDefinition? module, IResolutionScope? scope, Utf8String? ns, Utf8String? name)
            : this(new MetadataToken(TableIndex.TypeRef, 0))
        {
            _scope.SetValue(scope);
            Module = module;
            Namespace = ns;
            Name = name;
        }

        /// <summary>
        /// Gets or sets the name of the referenced type.
        /// </summary>
        /// <remarks>
        /// This property corresponds to the Name column in the type reference table.
        /// </remarks>
        public Utf8String? Name
        {
            get => _name.GetValue(this);
            set => _name.SetValue(value);
        }

        string? INameProvider.Name => Name;

        /// <summary>
        /// Gets or sets the namespace the type is residing in.
        /// </summary>
        /// <remarks>
        /// This property corresponds to the Namespace column in the type definition table.
        /// </remarks>
        public Utf8String? Namespace
        {
            get => _namespace.GetValue(this);
            set => _namespace.SetValue(value);
        }

        string? ITypeDescriptor.Namespace => Namespace;

        /// <inheritdoc />
        public string FullName => MemberNameGenerator.GetTypeFullName(this);

        /// <inheritdoc />
        public IResolutionScope? Scope
        {
            get => _scope.GetValue(this);
            set => _scope.SetValue(value);
        }

        /// <inheritdoc />
        public bool IsValueType => Resolve()?.IsValueType ?? false;

        /// <inheritdoc />
        public ModuleDefinition? Module
        {
            get;
            protected set;
        }

        AssemblyDescriptor? IResolutionScope.GetAssembly() => Scope?.GetAssembly();

        /// <inheritdoc />
        public ITypeDefOrRef? DeclaringType => Scope as ITypeDefOrRef;

        ITypeDescriptor? IMemberDescriptor.DeclaringType => DeclaringType;

        /// <inheritdoc />
        public IList<CustomAttribute> CustomAttributes
        {
            get
            {
                if (_customAttributes is null)
                    Interlocked.CompareExchange(ref _customAttributes, GetCustomAttributes(), null);
                return _customAttributes;
            }
        }

        ITypeDefOrRef ITypeDescriptor.ToTypeDefOrRef() => this;

        /// <inheritdoc />
        public TypeSignature ToTypeSignature() => ToTypeSignature(IsValueType);

        /// <inheritdoc />
        public TypeSignature ToTypeSignature(bool isValueType)
        {
            return Module?.CorLibTypeFactory.FromType(this) as TypeSignature
                   ?? new TypeDefOrRefSignature(this, isValueType);
        }

        /// <inheritdoc />
        public bool IsImportedInModule(ModuleDefinition module) => Module == module && (Scope?.IsImportedInModule(module) ?? false);

        /// <summary>
        /// Imports the type reference using the provided reference importer object.
        /// </summary>
        /// <param name="importer">The reference importer to use.</param>
        /// <returns>The imported type.</returns>
        public ITypeDefOrRef ImportWith(ReferenceImporter importer) => importer.ImportType(this);

        /// <inheritdoc />
        IImportable IImportable.ImportWith(ReferenceImporter importer) => ImportWith(importer);

        /// <inheritdoc />
        public TypeDefinition? Resolve() => Module?.MetadataResolver.ResolveType(this);

        IMemberDefinition? IMemberDescriptor.Resolve() => Resolve();

        /// <summary>
        /// Obtains the name of the type reference.
        /// </summary>
        /// <returns>The name.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Name"/> property.
        /// </remarks>
        protected virtual Utf8String? GetName() => null;

        /// <summary>
        /// Obtains the namespace of the type reference.
        /// </summary>
        /// <returns>The namespace.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Namespace"/> property.
        /// </remarks>
        protected virtual Utf8String? GetNamespace() => null;

        /// <summary>
        /// Obtains the scope of the type reference.
        /// </summary>
        /// <returns>The scope.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Scope"/> property.
        /// </remarks>
        protected virtual IResolutionScope? GetScope() => null;

        /// <summary>
        /// Obtains the list of custom attributes assigned to the member.
        /// </summary>
        /// <returns>The attributes</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="CustomAttributes"/> property.
        /// </remarks>
        protected virtual IList<CustomAttribute> GetCustomAttributes() =>
            new OwnedCollection<IHasCustomAttribute, CustomAttribute>(this);

        /// <inheritdoc />
        public override string ToString() => FullName;
    }
}
