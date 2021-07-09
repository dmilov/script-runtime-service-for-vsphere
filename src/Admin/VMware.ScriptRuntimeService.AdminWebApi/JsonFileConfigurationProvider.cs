// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace VMware.ScriptRuntimeService.AdminWebApi
{
   /// <summary>
   /// Configuration source for json file that uses <see cref="JsonFileConfigProvider"/>
   /// It uses <see cref="JsonConfigurationSource "/> internally.
   /// </summary>
   public class JsonFileConfigurationSource : IConfigurationSource
   {
      private JsonConfigurationSource _jsonSource;
      public JsonFileConfigurationSource(string path)
      {
         _jsonSource = new JsonConfigurationSource();
         _jsonSource.Path = path;
         _jsonSource.ReloadOnChange = true;
         if (File.Exists(path))
         {
            var fullPath = Path.GetFullPath(path);
            _jsonSource.Path = Path.GetFileName(fullPath);
            _jsonSource.FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(fullPath));
         }
      }
      public IConfigurationProvider Build(IConfigurationBuilder builder)
      {
         return new JsonFileConfigProvider(_jsonSource);
      }
   }

   /// <summary>
   /// </summary>
   public class JsonFileConfigProvider : JsonConfigurationProvider
   {
     
      public JsonFileConfigProvider(JsonConfigurationSource source) : base(source)
      {         
      }

   }

   /// <summary>
   /// Defines extension method for IConfigurationBuilder
   /// </summary>
   public static class SettingsJsonConfigurationProvider
   {
      public static IConfigurationBuilder AddJsonFileConfiguration(
        this IConfigurationBuilder builder,
        string settingsJsonFilePath)
      {
         return builder.Add(new JsonFileConfigurationSource(settingsJsonFilePath));
      }
   }
}
