﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Hosting;
using Telerik.Sitefinity.DynamicModules;
using Telerik.Sitefinity.DynamicModules.Builder;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.TestUtilities.CommonOperations;
using Telerik.Sitefinity.TestUtilities.Utilities;
using Telerik.Sitefinity.Utilities.Zip;

namespace FeatherWidgets.TestUtilities.CommonOperations
{
    /// <summary>
    /// Provides access to Dynamic modules common operations.
    /// </summary>
    public class DynamicModulesOperations
    {
        public static string ProviderName
        {
            get 
            {
                var providerName = string.Empty;

                if (ServerOperations.MultiSite().CheckIsMultisiteMode())
                    providerName = "dynamicContentProvider";

                return providerName;
            }
        }

        /// <summary>
        /// Imports a dynamic module.
        /// </summary>
        /// <param name="moduleResource">The dynamic module resource file.</param>
        public void EnsureModuleIsImported(string moduleName, string moduleResource)
        {
            string providerName = string.Empty;
            string transactionName = "Module Installations";

            if (!ServerOperations.ModuleBuilder().IsModulePresent(moduleName))
            {
                var assembly = this.GetTestUtilitiesAssembly();
                Stream moduleStream = assembly.GetManifestResourceStream(moduleResource);
                using (Stream stream = moduleStream)
                {
                    ServerOperations.ModuleBuilder().ImportModule(stream);
                    ServerOperations.ModuleBuilder().ActivateModule(moduleName, providerName, transactionName);

                    if (ServerOperations.MultiSite().CheckIsMultisiteMode())
                    {
                        var providerNames = DynamicModuleManager.GetManager().AllProviders.Select(p => p.ProviderName);
                        bool isCreated = providerNames.Contains("dynamicContentProvider");

                        if (!isCreated)
                        {
                            ServerOperations.MultiSite().CreateDynamicContentProvider("dynamicContentProvider");
                        }

                        ServerOperations.SystemManager().RestartApplication(false);
                        ServerOperations.MultiSite().AssignModuleToCurrentSite(moduleName);
                    }
                    else
                    {
                        ServerOperations.SystemManager().RestartApplication(false);      
                    }                                     
                }
            }
            else if (!ServerOperations.ModuleBuilder().IsModuleActive(moduleName))
            {
                if (ServerOperations.MultiSite().CheckIsMultisiteMode())
                {
                    var providerNames = DynamicModuleManager.GetManager().AllProviders.Select(p => p.ProviderName);
                    bool isCreated = providerNames.Contains("dynamicContentProvider");

                    if (!isCreated)
                    {
                        ServerOperations.MultiSite().CreateDynamicContentProvider("dynamicContentProvider");
                    }

                    ServerOperations.SystemManager().RestartApplication(false);
                    ServerOperations.MultiSite().AssignModuleToCurrentSite(moduleName);
                }
                else
                {
                    ServerOperations.SystemManager().RestartApplication(false);
                    WaitUtils.WaitForSitefinityToStart(SystemManager.CurrentHttpContext.Request.Url.GetLeftPart(UriPartial.Authority) + (HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') ?? string.Empty));
                } 
            }
        }

        /// <summary>
        /// Gets FeatherWidgets.TestUtilities assembly.
        /// </summary>
        /// <returns>TestUtilities assembly.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public Assembly GetTestUtilitiesAssembly()
        {
            var testUtilitiesAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.Equals("FeatherWidgets.TestUtilities")).FirstOrDefault();
            if (testUtilitiesAssembly == null)
            {
                throw new DllNotFoundException("Assembly wasn't found");
            }

            return testUtilitiesAssembly;
        }

        /// <summary>
        /// Extract structure zip
        /// </summary>
        /// <param name="zipResource"></param>
        /// <param name="targetFolder"></param>
        public void ExtractStructureZip(string zipResource, string targetFolder)
        {
            var path = Path.Combine(System.Web.Hosting.HostingEnvironment.MapPath("~/"), targetFolder);

            var assembly = this.GetTestUtilitiesAssembly();
            System.IO.Stream structureZip = assembly.GetManifestResourceStream(zipResource);

            byte[] data = new byte[structureZip.Length];

            structureZip.Read(data, 0, (int)structureZip.Length);

            using (var stream = new MemoryStream(data))
            {
                using (ZipFile zipFile = ZipFile.Read(stream))
                {
                    zipFile.ExtractAll(path, true);
                }
            }
        }

        /// <summary>
        /// Adds new layout file to a selected resource package.
        /// </summary>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="layoutFileName">The name of the layout file.</param>
        /// <param name="fileResource">The file resource.</param>
        public void AddNewResource(string fileResource, string filePath)
        {
            var assembly = this.GetTestUtilitiesAssembly();
            Stream source = assembly.GetManifestResourceStream(fileResource);

            Stream destination = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            this.CopyStream(source, destination);

            destination.Dispose();
        }

        /// <summary>
        /// Copies file stream to another file stream
        /// </summary>
        /// <param name="input">The input file.</param>
        /// <param name="output">The destination file.</param>
        private void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }
    }
}
