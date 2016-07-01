/*!

\namespace Terradue.Portal
@{
    Terradue.Portal is a low level library that provides with the core entities and interfaces for a CMS

    \xrefitem sw_version "Versions" "Software Package Version" 2.6.59

    \xrefitem sw_link "Links" "Software Package List" [Terradue.Portal](https://git.terradue.com/sugar/terradue-portal)

    \xrefitem sw_license "License" "Software License" [AGPL](https://git.terradue.com/sugar/terradue-portal/LICENSE)

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.OpenSearch

    \xrefitem sw_req "Require" "Software Dependencies" \ref MySql.Data

    \ingroup Core
@}

*/

using System.Reflection;
using System.Runtime.CompilerServices;
using NuGet4Mono.Extensions;

[assembly: AssemblyTitle("Terradue.Portal")]
[assembly: AssemblyDescription("Terradue.Portal is a library targeting .NET 4.0 and above that provides core interfaces and classes of Terradue portal")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Terradue")]
[assembly: AssemblyProduct("Terradue.Portal")]
[assembly: AssemblyCopyright("Terradue")]
[assembly: AssemblyAuthors("Enguerran Boissier")]
[assembly: AssemblyProjectUrl("https://git.terradue.com/sugar/terradue-portal")]
[assembly: AssemblyLicenseUrl("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("2.6.59")]
[assembly: AssemblyInformationalVersion("2.6.59")]

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]
