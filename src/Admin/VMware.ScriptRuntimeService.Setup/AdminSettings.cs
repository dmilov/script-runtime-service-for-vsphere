// **************************************************************************
//  Copyright 2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

namespace VMware.ScriptRuntimeService.Setup
{
   public class AdminSettings
   {
      public string ServiceHostname { get; set; }
      public string TlsCertificatePath { get; set; }
      public string SolutionUserSigningCertificatePath { get; set; }
      public string VCRegistrationConfigMap { get; set; }
   }

   public class AdminWebApiSettings
   {
      public AdminSettings AdminSettings { get; set; }
   }
}
