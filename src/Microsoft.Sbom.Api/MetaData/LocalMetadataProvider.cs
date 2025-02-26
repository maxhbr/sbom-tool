﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Sbom.Extensions;
using Microsoft.Sbom.Extensions.Entities;
using Microsoft.Sbom.Common.Config;
using Microsoft.Sbom.Common.Extensions;
using Microsoft.Sbom.Common.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Sbom.Api.Metadata
{
    /// <summary>
    /// Provides metadata based on the local environment.
    /// </summary>
    public class LocalMetadataProvider : IMetadataProvider, IDefaultMetadataProvider
    {
        private const string ProductName = "Microsoft.SBOMTool";
        private const string BuildEnvironmentNameValue = "local";

        private static readonly Lazy<string> Version = new Lazy<string>(() =>
        {
            return typeof(LocalMetadataProvider).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        });

        public string BuildEnvironmentName => BuildEnvironmentNameValue;

        private readonly IConfiguration configuration;

        /// <summary>
        /// Gets or sets stores the metadata that is generated by this metadata provider.
        /// </summary>
        public IDictionary<MetadataKey, object> MetadataDictionary { get; set; } = new Dictionary<MetadataKey, object>();

        public LocalMetadataProvider(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            PopulateMetadata();
        }

        private void PopulateMetadata()
        {
            MetadataDictionary.Add(MetadataKey.SBOMToolName, ProductName);

            // TODO get tool version from dll manifest.
            MetadataDictionary.Add(MetadataKey.SBOMToolVersion, Version.Value);

            // Add the package name if available.
            MetadataDictionary.AddIfKeyNotPresentAndValueNotNull(MetadataKey.PackageName, configuration.PackageName?.Value);
            MetadataDictionary.AddIfKeyNotPresentAndValueNotNull(MetadataKey.PackageVersion, configuration.PackageVersion?.Value);

            // Add generation timestamp
            MetadataDictionary.AddIfKeyNotPresentAndValueNotNull(MetadataKey.GenerationTimestamp, configuration.GenerationTimestamp?.Value);
        }

        public string GetDocumentNamespaceUri()
        {
            // This is used when we can't determine the build environment. So, use a guid and package information
            // to generate the namespace.
            var packageName = MetadataDictionary[MetadataKey.PackageName];
            var packageVersion = MetadataDictionary[MetadataKey.PackageVersion];
            var uniqueNsPart = configuration.NamespaceUriUniquePart?.Value ?? IdentifierUtils.GetShortGuid(Guid.NewGuid());

            return Uri.EscapeUriString(string.Join("/", configuration.NamespaceUriBase.Value, packageName, packageVersion, uniqueNsPart));
        }
    }

    /// <summary>
    /// Marker interface to indicate that this metadata provider should be the provider of last resort when another cannot be located.
    /// </summary>
    /// <remarks>Only one class should implement this interface, as it defines the one and only default provider.</remarks>
    internal interface IDefaultMetadataProvider
    {
    }
}
