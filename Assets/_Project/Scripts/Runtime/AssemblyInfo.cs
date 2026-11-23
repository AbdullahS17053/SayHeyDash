using System.Runtime.CompilerServices;

// Test-only hooks (internal members) are visible to the test assemblies and, for the
// Editor-only guided tour (Say Hey Dash > Handover > Play Editor Tour), the Editor assembly.
// Nothing that ships in a build can see them.
[assembly: InternalsVisibleTo("SayHeyDash.Tests.EditMode")]
[assembly: InternalsVisibleTo("SayHeyDash.Tests.PlayMode")]
[assembly: InternalsVisibleTo("SayHeyDash.Editor")]
