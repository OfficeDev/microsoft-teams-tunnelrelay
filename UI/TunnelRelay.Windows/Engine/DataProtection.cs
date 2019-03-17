// <copyright file="DataProtection.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace TunnelRelay.Windows.Engine
{
    using System;
    using System.Security.Cryptography;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Methods for encrypting and decrypting secrets using DPAPI.
    /// </summary>
    internal class DataProtection
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private static readonly ILogger<DataProtection> Logger = LoggingHelper.GetLogger<DataProtection>();

        /// <summary>
        /// Protects the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>Protected data.</returns>
        public static byte[] Protect(byte[] data)
        {
            try
            {
                // Encrypt the data using DataProtectionScope.LocalMachine. The result can be decrypted
                // only on same machine.
                return ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);
            }
            catch (CryptographicException cryptEx)
            {
                DataProtection.Logger.LogError(cryptEx, "Failed to encrypt with Cryptographic exception");
                throw;
            }
            catch (Exception ex)
            {
                DataProtection.Logger.LogError(ex, "Failed to encrypt");
                throw;
            }
        }

        /// <summary>
        /// Unprotects the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>Unprotected data.</returns>
        public static byte[] Unprotect(byte[] data)
        {
            try
            {
                return ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);
            }
            catch (CryptographicException cryptEx)
            {
                DataProtection.Logger.LogError(cryptEx, "Failed to decryt with Cryptographic exception");
                throw;
            }
            catch (Exception ex)
            {
                DataProtection.Logger.LogError(ex, "Failed to decrypt");
                throw;
            }
        }
    }
}
