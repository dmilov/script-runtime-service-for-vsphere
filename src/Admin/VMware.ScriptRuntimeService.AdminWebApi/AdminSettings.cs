// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

namespace VMware.ScriptRuntimeService.AdminWebApi
{
   public class AdminSettings
   {
      public string ServiceHostname { get; set; }
      public string TlsCertificatePath { get; set; }
      public string SolutionUserSigningCertificatePath { get; set; }
   }
}
