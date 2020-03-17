// <copyright file="DataProtection.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.UI.StateManagement
{
    using System.Security.Cryptography;

    /// <summary>
    /// Methods for encrypting and decrypting secrets using DPAPI.
    /// </summary>
    internal class DataProtection
    {
        /// <summary>
        /// Protects the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>Protected data.</returns>
        public static byte[] Protect(byte[] data)
        {
            // Encrypt the data using DataProtectionScope.LocalMachine. The result can be decrypted
            // only on same machine.
            return ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);
        }

        /// <summary>
        /// Unprotects the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>Unprotected data.</returns>
        public static byte[] Unprotect(byte[] data)
        {
            return ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);
        }
    }
}
