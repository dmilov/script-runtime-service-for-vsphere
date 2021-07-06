// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace VMware.ScriptRuntimeService.Setup.SetupFlows
{
   public enum SetupFlowType {
      InitialSetup,
      RegisterWithVC,
      UnregisterFromVC,
      UpdateTlsCertificate,
      UpdateTrustedCACertificates,
      CleanupVCRegistration
   }
}
