// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace VMware.ScriptRuntimeService.AdminWebApi
{
   public class Startup
   {
      private ILogger _logger;

      public Startup(IConfiguration configuration)
      {
         Configuration = configuration;
      }

      public IConfiguration Configuration { get; }

      // This method gets called by the runtime. Use this method to add services to the container.
      public void ConfigureServices(IServiceCollection services)
      {
         services
            .AddMvc()
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
            .AddNewtonsoftJson(options => {
               options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });

         var packageNameExtension = new OpenApiObject();
         packageNameExtension.Add("package-name", new OpenApiString("com.vmware.srs.admin"));
         services.AddSwaggerGen(
            c => {
               c.SwaggerDoc(
                  "admin",
                  new OpenApiInfo
                  {
                     Description = "Script Runtime Service Admin API",
                     Title = "Script Runtime Service",
                     Version = "1.0.0",
                     Contact = new OpenApiContact()
                     {
                        Name = "Script Runtime Service for vSphere",
                        Url = new Uri(@"https://github.com/vmware/script-runtime-service-for-vsphere"),
                     }
                  });
         });
         services.AddSwaggerGenNewtonsoftSupport();
      }

      // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
      {
         // Get logger for Startup class
         _logger = loggerFactory.CreateLogger(typeof(Startup));         
        
         app.Map("/admin", mainapp =>
         {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            mainapp.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            mainapp.UseSwaggerUI(c => {
               c.SwaggerEndpoint("/admin/swagger/admin/swagger.json", "Script Runtime Service Admin API");
            });

            mainapp.UseStaticFiles();

            mainapp.UseRouting();
            mainapp.UseCors();

            mainapp.UseAuthentication();
            mainapp.UseAuthorization();
            mainapp.UseEndpoints(endpoints => {
               endpoints.MapControllers();
            });
         });
        
      }
   }
}
