// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
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

      public static IWebHostBuilder CreateHostBuilder(string[] args)  {
         return WebHost.CreateDefaultBuilder(args)
             .UseKestrel()
             .UseStartup<Startup>()
             .ConfigureLogging(logging =>
             {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
             })
             .UseNLog();
      }
   }
}
