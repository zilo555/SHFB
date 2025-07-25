//===============================================================================================================
// System  : Sandcastle Tools - Sandcastle Tools Core Class Library
// File    : BuildStep.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 06/19/2025
// Note    : Copyright 2006-2025, Eric Woodruff, All rights reserved
//
// This file contains the enumerated type that defines the build steps used when reporting progress during a
// build.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://GitHub.com/EWSoftware/SHFB.  This
// notice, the author's name, and all copyright notices must remain intact in all applications, documentation,
// and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/10/2006  EFW  Created the code
// 11/06/2007  EFW  Added new component configuration merge build step
// 02/04/2008  EFW  Added HTML Info Extract and Generate Inherited documentation build steps
// 03/24/2008  EFW  Removed the PurgeDuplicateTopics build step.  Added conceptual content build steps.
// 06/13/2010  EFW  Moved GenerateIntermediateTableOfContents so that it occurs right after MergeTablesOfContents
// 06/30/2010  EFW  Added CombiningIntermediateTocFiles step and removed UpdateTableOfContents step
// 10/26/2012  EFW  Removed the FindingTools step and made the processing part of the Initializing step due to
//                  the need to locate configuration files during initialization.
// 06/21/2013  EFW  Moved CopyStandardHelpContent build step to allow for standard content defined in a
//                  presentation style definition file.  Removed ModifyHelpTopicFilenames as naming is now
//                  handled entirely by AddFilenames.xsl.
// 12/04/2013  EFW  Removed the ApplyVisibilityProperties build step.  Plug-ins can apply visibility settings if
//                  needed by calling the ApplyVisibilityProperties() method.  Moved the TransformReflectionInfo
//                  to support namespace grouping.
// 12/21/2015  EFW  Merged conceptual and reference topic build steps
// 06/02/2021  EFW  Added AddNamespaceGroups, AddApiTopicFilenames, and GenerateApiTopicManifest build steps
// 06/17/2021  EFW  Renamed TransformReflectionInfo to ApplyDocumentModel.  Removed unused build steps
//                  GenerateHelpFormatTableOfContents and GenerateHelpFileIndex.
//===============================================================================================================

using System;

namespace Sandcastle.Core.BuildEngine
{
    /// <summary>
    /// This public enumerated type defines the build steps used when reporting progress during a build.
    /// </summary>
    [Serializable]
    public enum BuildStep
    {
        /// <summary>The build has not yet started.</summary>
        None,
        /// <summary>Initializing to prepare for build.</summary>
        Initializing,
        /// <summary>The working folder is about to be created or cleared.</summary>
        ClearWorkFolder,
        /// <summary>The documentation source information is being validated and copied to the build folder.</summary>
        ValidatingDocumentationSources,
        /// <summary>The shared content files are being generated.</summary>
        GenerateSharedContent,
        /// <summary>Generate the API filter for MRefBuilder</summary>
        GenerateApiFilter,
        /// <summary>The reflection information is being generated by <strong>MRefBuilder</strong>.</summary>
        GenerateReflectionInfo,
        /// <summary>The reflection information is being transformed by applying the presentation style's
        /// document model to it.</summary>
        ApplyDocumentModel,
        /// <summary>Add namespace group entries to the reflection information file.</summary>
        AddNamespaceGroups,
        /// <summary>Add topic filenames to API members in the reflection information file.</summary>
        AddApiTopicFilenames,
        /// <summary>Generate the API topic manifest file.</summary>
        GenerateApiTopicManifest,
        /// <summary>Namespace summary information is being generated.</summary>
        GenerateNamespaceSummaries,
        /// <summary>All <c>&lt;inheritDoc /&gt;</c> tags are being expanded.</summary>
        GenerateInheritedDocumentation,
        /// <summary>The conceptual content files are being copied to the working and output folders.</summary>
        CopyConceptualContent,
        /// <summary>The conceptual content topic configuration files are being generated.</summary>
        CreateConceptualTopicConfigs,
        /// <summary>The additional content files are being copied to the help output folder.</summary>
        CopyAdditionalContent,
        /// <summary>The conceptual and additional content tables of contents are being merged.</summary>
        MergeTablesOfContents,
        /// <summary>The intermediate table of contents is being generated.</summary>
        GenerateIntermediateTableOfContents,
        /// <summary>The <strong>BuildAssembler</strong> configuration file is being created.</summary>
        CreateBuildAssemblerConfigs,
        /// <summary>Custom build component configurations are being merged into the <strong>BuildAssembler</strong>
        /// configuration file.</summary>
        MergeCustomConfigs,
        /// <summary>The intermediate table of content files are being merged into a single file.</summary>
        CombiningIntermediateTocFiles,
        /// <summary>Conceptual and/or API reference help file topics are being generated by
        /// <strong>BuildAssembler</strong>.</summary>
        BuildTopics,
        /// <summary>Title and keyword index information is being extracted for the HTML Help 1 TOC and index
        /// and/or website TOC.</summary>
        ExtractingHtmlInfo,
        /// <summary>The standard help file content (art, scripts, styles, and other standard presentation style
        /// content) is being copied to the help output folder.</summary>
        /// <remarks>This must occur after <c>ExtracingHtmlInfo</c> as the replacement tags in the web content
        /// may rely on the table of contents and index files generated in that step.</remarks>
        CopyStandardHelpContent,
        /// <summary>The help project file is being generated.</summary>
        GenerateHelpProject,
        /// <summary>The help file project is being compiled.</summary>
        CompilingHelpFile,
        /// <summary>Generate full-text index for ASP.NET website search.</summary>
        GenerateFullTextIndex,
        /// <summary>The website files are being copied to the output path.</summary>
        CopyingWebsiteFiles,
        /// <summary>The temporary help project files are being removed.</summary>
        CleanIntermediates,
        /// <summary>The build has completed successfully.</summary>
        Completed,
        /// <summary>The build was canceled by user request.</summary>
        Canceled,
        /// <summary>The build failed with an unexpected error.</summary>
        Failed
    }
}
