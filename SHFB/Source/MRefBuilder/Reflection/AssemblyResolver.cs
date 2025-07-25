//===============================================================================================================
// System  : Sandcastle MRefBuilder Tool
// File    : AssemblyResolver.cs
// Note    : Copyright 2006-2019 Microsoft Corporation
//
// This file contains a modified version of the original AssemblyResolver that supports assembly binding
// redirect elements in its configuration that let you redirect an unknown assembly's strong name to another
// when resolving an unknown reference.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://GitHub.com/EWSoftware/SHFB.  This
// notice and all copyright notices must remain intact in all applications, documentation, and source files.
//
// Change History
// 03/02/2012 - EFW - Merged my changes into the code
// 08/10/2012 - EFW - Added support for ignoreIfUnresolved config element
// 08/19/2016 - EFW - Added code to resolve a missing mscorlib v255 to System.Runtime
// 04/11/2017 - EFW - Added code to resolve a missing System.Runtime to mscorlib
// 11/06/2017 - EFW - Added code to auto-redirect to a like-named assembly in the cache if one is found.  This
// helps correct missing assembly issues in .NETCore/.NETStandard projects which can vary by version number.
//===============================================================================================================

// Ignore Spelling: mscorlib dep gac netstandard

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml.XPath;

using System.Compiler;

using Sandcastle.Core;

namespace Sandcastle.Tools.Reflection
{
    /// <summary>
    /// This is a the assembly resolver for Sandcastle's <b>MRefBuilder</b> tool.  It supports assembly
    /// binding redirect elements in its configuration that let you redirect an unknown assembly's strong
    /// name to another by version number when resolving an unknown reference.  It also supports the
    /// definition of assemblies to ignore if unresolved.
    /// </summary>
    /// <remarks><para></para>Support for binding redirection solves the problem reported in Sandcastle work
    /// item <see href="http://Sandcastle.CodePlex.com/WorkItem/View.aspx?WorkItemId=1014">#1014</see>.
    /// To use the resolver, it must be added to the <b>MRefBuilder.config</b> file by defining the
    /// <c>resolver</c> element as shown in the example below.  Within the <c>resolver</c> element, add
    /// an <c>assemblyBinding</c> element that contains one or more <c>dependentAssembly</c> elements that
    /// define the redirections.  If you have a set of redirections in an application or web configuration
    /// file, you can define a <c>dependentAssembly</c> element with an <c>importFrom</c> attribute that
    /// specifies the location of the configuration file from which to import the redirects.  The other
    /// option is to specify individual redirects using the <c>assemblyIdentity</c> and
    /// <c>bindingRedirect</c> child elements of each <c>dependentAssembly</c> element.  See the MSDN
    /// help for those elements for more information on their usage.</para>
    ///
    /// <note type="important">The assembly version(s) to which the entries are redirected must be one of
    /// the documented assemblies or must be referenced as a dependency using the <b>/dep</b> command line
    /// switch.</note>
    /// 
    /// <para>To ignore assemblies that cannot be resolved and for which you do not have a copy of the
    /// assembly, add an <c>ignoreIfUnresolved</c> element to the <c>resolver</c> element.  Within it,
    /// add an <c>assemblyIdentity</c> element with a <c>name</c> attribute that specifies the identity
    /// name of the assembly to ignore.  An example is the Crystal Reports
    /// BusinessObjects.Licensing.KeycodeDecoder assembly.</para>
    /// 
    /// <note type="important">The name specified is the name from the assembly's identity, not the physical
    /// filename.  See the Crystal Reports assembly example below.</note>
    /// </remarks>
    /// <example>
    /// <code language="xml" title="Sample MRefBuilder.config">
    /// <![CDATA[
    /// <configuration>
    ///   <dduetools>
    ///     <platform version="2.0"
    ///       path="%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\" />
    ///
    ///     <!-- Replace the resolver element with this.  Update the path
    ///          to point to the location of the assembly on your system. -->
    ///     <resolver type="Sandcastle.Tools.Reflection.AssemblyResolver" use-gac="false">
    ///
    ///       <!-- Add an assemblyBinding element to contain the redirects -->
    ///       <assemblyBinding>
    ///         <!-- Import bindings from an application or web config file -->
    ///         <dependentAssembly importFrom=".\Web.config" />
    ///
    ///         <!-- Define a redirect for a range of versions -->
    ///         <dependentAssembly>
    ///           <assemblyIdentity name="NationalInstruments.Common"
    ///               publicKeyToken="4544464cdeaab541" />
    ///           <bindingRedirect oldVersion="1.0.0.0-8.1.20.168"
    ///               newVersion="8.1.20.237" />
    ///         </dependentAssembly>
    ///
    ///         <!-- Define a redirect for a single version -->
    ///         <dependentAssembly>
    ///           <assemblyIdentity name="MyCompany.Util.ComponentFactory"
    ///               publicKeyToken="E1458197622051B1" culture="neutral"/>
    ///           <bindingRedirect oldVersion="1.2.3.4"
    ///               newVersion="2.1.4.3"/>
    ///         </dependentAssembly>
    ///       </assemblyBinding>
    ///       
    ///       <!-- Add an ignoreIfUnresolved element to ignore assemblies that
    ///            cannot be resolved and for which you do not have a copy.
	///       <ignoreIfUnresolved>
    ///         <assemblyIdentity name="BusinessObjects.Licensing.KeycodeDecoder" />
    ///       </ignoreIfUnresolved>
    ///     </resolver>
    ///
    ///     <!-- ... rest of MRefBuilder.config ... -->
    ///
    ///   </dduetools>
    /// </configuration>]]>
    /// </code>
    /// </example>
    public class AssemblyResolver
    {
        #region Private data members
        //=====================================================================

        private readonly Dictionary<string, AssemblyNode> cache = [];
        private readonly Collection<BindingRedirectSettings> redirects;
        private readonly Collection<string> ignoreIfUnresolved;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This is used to get or set whether or not to use the GAC when resolving assembly references
        /// </summary>
        public bool UseGac { get; set; }

        /// <summary>
        /// This read-only property returns a reference to the assembly node cache
        /// </summary>
        /// <remarks>This can be used by derived resolvers to add nodes to the cache</remarks>
        public Dictionary<string, AssemblyNode> AssemblyCache => cache;

        /// <summary>
        /// This read-only property returns a reference to the collection of assembly binding redirections
        /// </summary>
        /// <remarks>This can be used by derived resolvers to add additional redirections</remarks>
        public Collection<BindingRedirectSettings> BindingRedirections => redirects;

        /// <summary>
        /// This read-only property returns a reference to the collection of assemblies to ignore if
        /// unresolved.
        /// </summary>
        /// <remarks>This can be used by derived resolvers to add additional ignored assemblies</remarks>
        public Collection<string> IgnoreIfUnresolved => ignoreIfUnresolved;

        /// <summary>
        /// This is used to get or set the message logger to use to report issues
        /// </summary>
        public Action<MessageLevel, string> MessageLogger { get; set; }

        #endregion

        #region Events
        //=====================================================================

        /// <summary>
        /// This is raised when an unresolved assembly reference is encountered
        /// </summary>
        public event EventHandler<AssemblyReferenceEventArgs> UnresolvedAssemblyReference;

        /// <summary>
        /// This raises the <see cref="UnresolvedAssemblyReference" event/>
        /// </summary>
        /// <param name="reference">The assembly reference</param>
        /// <param name="referrer">The module requiring the reference</param>
        protected virtual void OnUnresolvedAssemblyReference(AssemblyReference reference, Module referrer)
        {
            UnresolvedAssemblyReference?.Invoke(this, new AssemblyReferenceEventArgs(reference, referrer));
        }
        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// Default constructor
        /// </summary>
        public AssemblyResolver()
        {
            cache = [];
            redirects = [];
            ignoreIfUnresolved = [];

            this.UseGac = true;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">The XPath navigator containing the configuration settings</param>
        public AssemblyResolver(XPathNavigator configuration) : this()
        {
            Collection<BindingRedirectSettings> importedSettings;
            BindingRedirectSettings brs;

            if(configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            string useGacValue = configuration.GetAttribute("use-gac", String.Empty);

            if(!String.IsNullOrEmpty(useGacValue))
                this.UseGac = Convert.ToBoolean(useGacValue, CultureInfo.InvariantCulture);

            // Load assembly binding redirects if specified
            foreach(XPathNavigator nav in configuration.Select("assemblyBinding/dependentAssembly"))
            {
                brs = BindingRedirectSettings.FromXPathNavigator(nav, null, null);

                // Import settings from a configuration file?
                if(!String.IsNullOrEmpty(brs.ConfigurationFile))
                {
                    this.MessageLogger(MessageLevel.Info, $"Importing settings from: {brs.ConfigurationFile}");

                    importedSettings = BindingRedirectSettings.FromConfigFile(brs.ConfigurationFile);

                    foreach(BindingRedirectSettings imported in importedSettings)
                    {
                        this.MessageLogger(MessageLevel.Info,
                            $"Loaded assembly binding redirect: {imported}");
                        redirects.Add(imported);
                    }
                }
                else
                {
                    this.MessageLogger(MessageLevel.Info, $"Loaded assembly binding redirect: {brs}");
                    redirects.Add(brs);
                }
            }

            // If specified, load assembly names to ignore if unresolved
            foreach(XPathNavigator nav in configuration.Select("ignoreIfUnresolved/assemblyIdentity"))
            {
                string name = nav.GetAttribute("name", String.Empty);

                if(!String.IsNullOrEmpty(name))
                    ignoreIfUnresolved.Add(name);
            }
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <summary>
        /// Add a new assembly to the cache
        /// </summary>
        /// <param name="assembly">The assembly to add</param>
        public virtual void Add(AssemblyNode assembly)
        {
            if(assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            assembly.AssemblyReferenceResolution += ResolveReference;
            assembly.AssemblyReferenceResolutionAfterProbingFailed += UnresolvedReference;

            cache[assembly.StrongName] = assembly;
        }

        /// <summary>
        /// This is used to try and resolve an assembly reference
        /// </summary>
        /// <param name="reference">The assembly reference</param>
        /// <param name="referrer">The module requiring the reference</param>
        /// <returns>The assembly node if resolved or null if not resolved</returns>
        public virtual AssemblyNode ResolveReference(AssemblyReference reference, Module referrer)
        {

            if(reference == null)
                throw new ArgumentNullException(nameof(reference));

            // Try to get it from the cache
            string name = reference.StrongName;

            if(cache.TryGetValue(name, out AssemblyNode assembly))
                return assembly;

            // Try to get it from the GAC if so indicated
            if(this.UseGac)
            {
                string location = GlobalAssemblyCache.GetLocation(reference);

                if(location != null)
                {
                    assembly = AssemblyNode.GetAssembly(location, null, false, false, false, false);

                    if(assembly != null)
                    {
                        this.Add(assembly);
                        return assembly;
                    }
                }
            }

            // Try the redirects if not found
            foreach(BindingRedirectSettings brs in redirects)
            {
                if(brs.IsRedirectFor(name) && cache.TryGetValue(brs.StrongName, out assembly))
                {
                    this.MessageLogger(MessageLevel.Info, $"Using redirect '{brs.StrongName}' in place of '{name}'");

                    // Add the same assembly under the current name
                    cache.Add(name, assembly);
                    return assembly;
                }
            }

            // For mscorlib v255.255.255.255, redirect to System.Runtime.  This is typically one like a .NETCore
            // framework which redirects all of the system types there.  For a missing System.Runtime and System
            // v0.0.0.0, redirect to mscorlib.  This is most likely a framework such as .NETStandard.  For those,
            // we use the best matching .NET Framework.
            if((reference.Name == "mscorlib" && reference.Version.Major == 255) ||
              (reference.Name == "mscorlib" && SystemTypes.SystemAssembly.Name == "netstandard") ||
              (reference.Name == "System" && reference.Version.Major == 0) || reference.Name == "System.Runtime" ||
              reference.Name == "netstandard")
            {
                // The system assembly should be set.  If so, it'll point to the one we need.
                if(SystemTypes.SystemAssembly != null)
                {
                    assembly = SystemTypes.SystemAssembly;
                    cache.Add(name, assembly);
                    return assembly;
                }

                // If not, look for it in the cache
                string key = cache.Keys.FirstOrDefault(k => k.StartsWith("System.Runtime,", StringComparison.Ordinal));

                if(key != null)
                {
                    assembly = cache[key];
                    cache.Add(name, assembly);
                    return assembly;
                }
            }

            if(reference.Name != "mscorlib")
            {
                // Try for a framework assembly in the target platform
                var assemblyRef = (AssemblyReference)TargetPlatform.AssemblyReferenceFor[Identifier.For(reference.Name).UniqueIdKey];

                if(assemblyRef != null && System.IO.File.Exists(assemblyRef.Location))
                {
                    assembly = AssemblyNode.GetAssembly(assemblyRef.Location, null, false, false, false, false);

                    this.MessageLogger(MessageLevel.Info, $"Using framework redirect '{assembly.StrongName}' in place of '{name}'");

                    cache.Add(name, assembly);
                    return assembly;
                }

                // If not, look for it in the cache by name alone
                string key = cache.Keys.FirstOrDefault(k => k.StartsWith(reference.Name + ",", StringComparison.Ordinal));

                if(key != null)
                {
                    assembly = cache[key];
                    cache.Add(name, assembly);

                    this.MessageLogger(MessageLevel.Info, $"Using automatic redirect '{assembly.StrongName}' in place of '{name}'");

                    return assembly;
                }
            }

            // Couldn't find it; return null
            return null;
        }

        /// <summary>
        /// This is called if assembly reference resolution fails after probing
        /// </summary>
        /// <param name="reference">The assembly reference</param>
        /// <param name="module">The module</param>
        /// <returns>Always returns null</returns>
        private AssemblyNode UnresolvedReference(AssemblyReference reference, Module module)
        {
            // Don't raise the event if ignored.  Also, a bit of a hack for now.  If it's a "System." or a
            // "Microsoft." assembly, just ignore it.  There are some issues resolving some of the .NETCore
            // and .NETStandard assemblies due to variations in how they are distributed.
            if(!ignoreIfUnresolved.Contains(reference.Name) && !reference.Name.StartsWith("System.",
              StringComparison.Ordinal) && !reference.Name.StartsWith("Microsoft.", StringComparison.Ordinal))
            {
                OnUnresolvedAssemblyReference(reference, module);
            }
            else
            {
                this.MessageLogger(MessageLevel.Warn,
                    $"Ignoring unresolved assembly reference: {reference.Name} ({reference.StrongName}) required by {module.Name}");
            }

            return null;
        }
        #endregion
    }
}
