// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

using System.Collections.Generic;

namespace VMware.ScriptRuntimeService.AdminWebApi.DataTypes
{
   public static class ApiErrorCodes {      
      private static Dictionary<string, int> ErrorMessageToErrorCode = new Dictionary<string, int> {         
      };

      public static int GetErrorCode(string errorRsourceMessage) {
         if (!ErrorMessageToErrorCode.TryGetValue(errorRsourceMessage, out var result)) {
            result = Unknown;
         }
         return result;
      }

      public static int Unknown => 500;
   }
}
