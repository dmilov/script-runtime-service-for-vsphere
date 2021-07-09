// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace VMware.ScriptRuntimeService.AdminWebApi
{
   public class Program
   {

      public static void Main(string[] args)
      {
         CreateHostBuilder(args).Build().Run();
      }

      public static string AssemblyDirectory
      {
         get
         {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
         }
      }

      public static IWebHostBuilder CreateHostBuilder(string[] args)  {
         return WebHost.CreateDefaultBuilder(args)
             .UseKestrel()
             .UseStartup<Startup>()
             .ConfigureAppConfiguration((hostingContext, config) =>
              {
                 var settingsPath = Path.Combine(AssemblyDirectory, "settings", "admin-settings.json");
                 if (File.Exists(settingsPath))
                 {
                    config.AddJsonFileConfiguration(settingsPath);
                 }
              })
             .ConfigureLogging(logging =>
             {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
             })
             .UseNLog();
      }
   }
}
