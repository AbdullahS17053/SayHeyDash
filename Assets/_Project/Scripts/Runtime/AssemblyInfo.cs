using System.Runtime.CompilerServices;

// Test-only hooks (internal members) are visible to the test assemblies and nothing else.
[assembly: InternalsVisibleTo("SayHeyDash.Tests.EditMode")]
[assembly: InternalsVisibleTo("SayHeyDash.Tests.PlayMode")]
