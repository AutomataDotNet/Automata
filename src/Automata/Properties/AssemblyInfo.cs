using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Automata")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Automata")]
[assembly: AssemblyCopyright("Copyright ©  2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b5e695df-c033-4938-94dd-ac6ecbeab36b")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.2.0")]
[assembly: AssemblyFileVersion("1.0.2.0")]

[assembly: InternalsVisibleTo("Automata.Tests, PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010091e0dd11fd488c" +
"dfb7675354b7f22efe8195a24b337c7e926ce8f4a751d0614ceb2b8e3ecff60c59df2e5510eb49" +
"1d45536ef89eca862928579071190b69d11799dbae1ed6f23c14edd946cac2b331d8102dfb3275" +
"3e2b0f9129313d76e9380abb036abb0fea10d3844fc33d8bf0a6466e12ac9ddd10333f3064be95" +
"4abb87db")]

[assembly: InternalsVisibleTo("Microsoft.Automata.Z3, PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010091e0dd11fd488c" +
"dfb7675354b7f22efe8195a24b337c7e926ce8f4a751d0614ceb2b8e3ecff60c59df2e5510eb49" +
"1d45536ef89eca862928579071190b69d11799dbae1ed6f23c14edd946cac2b331d8102dfb3275" +
"3e2b0f9129313d76e9380abb036abb0fea10d3844fc33d8bf0a6466e12ac9ddd10333f3064be95" +
"4abb87db")]

[assembly: InternalsVisibleTo("Experimentation, PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010091e0dd11fd488c" +
"dfb7675354b7f22efe8195a24b337c7e926ce8f4a751d0614ceb2b8e3ecff60c59df2e5510eb49" +
"1d45536ef89eca862928579071190b69d11799dbae1ed6f23c14edd946cac2b331d8102dfb3275" +
"3e2b0f9129313d76e9380abb036abb0fea10d3844fc33d8bf0a6466e12ac9ddd10333f3064be95" +
"4abb87db")]

[assembly: InternalsVisibleTo("Microsoft.Automata.MSO, PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010091e0dd11fd488c" +
"dfb7675354b7f22efe8195a24b337c7e926ce8f4a751d0614ceb2b8e3ecff60c59df2e5510eb49" +
"1d45536ef89eca862928579071190b69d11799dbae1ed6f23c14edd946cac2b331d8102dfb3275" +
"3e2b0f9129313d76e9380abb036abb0fea10d3844fc33d8bf0a6466e12ac9ddd10333f3064be95" +
"4abb87db")]

[assembly: InternalsVisibleTo("Microsoft.Bek, PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010091e0dd11fd488c" +
"dfb7675354b7f22efe8195a24b337c7e926ce8f4a751d0614ceb2b8e3ecff60c59df2e5510eb49" +
"1d45536ef89eca862928579071190b69d11799dbae1ed6f23c14edd946cac2b331d8102dfb3275" +
"3e2b0f9129313d76e9380abb036abb0fea10d3844fc33d8bf0a6466e12ac9ddd10333f3064be95" +
"4abb87db")]

[assembly: InternalsVisibleTo("Bek.Tests, PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010091e0dd11fd488c" +
"dfb7675354b7f22efe8195a24b337c7e926ce8f4a751d0614ceb2b8e3ecff60c59df2e5510eb49" +
"1d45536ef89eca862928579071190b69d11799dbae1ed6f23c14edd946cac2b331d8102dfb3275" +
"3e2b0f9129313d76e9380abb036abb0fea10d3844fc33d8bf0a6466e12ac9ddd10333f3064be95" +
"4abb87db")]

[assembly: InternalsVisibleTo("RegenerateUnicodeTables, PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010091e0dd11fd488c" +
"dfb7675354b7f22efe8195a24b337c7e926ce8f4a751d0614ceb2b8e3ecff60c59df2e5510eb49" +
"1d45536ef89eca862928579071190b69d11799dbae1ed6f23c14edd946cac2b331d8102dfb3275" +
"3e2b0f9129313d76e9380abb036abb0fea10d3844fc33d8bf0a6466e12ac9ddd10333f3064be95" +
"4abb87db")]

[assembly: InternalsVisibleTo("CounterAutomata, PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010091e0dd11fd488c" +
"dfb7675354b7f22efe8195a24b337c7e926ce8f4a751d0614ceb2b8e3ecff60c59df2e5510eb49" +
"1d45536ef89eca862928579071190b69d11799dbae1ed6f23c14edd946cac2b331d8102dfb3275" +
"3e2b0f9129313d76e9380abb036abb0fea10d3844fc33d8bf0a6466e12ac9ddd10333f3064be95" +
"4abb87db")]


