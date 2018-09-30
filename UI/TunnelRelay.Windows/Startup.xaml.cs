// <copyright file="Startup.xaml.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// Licensed under the MIT license.
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace TunnelRelay.Windows
{
    using System.Windows;
    using TunnelRelay.Diagnostics;
    using TunnelRelay.Windows.Engine;

    /// <summary>
    /// Interaction logic for Startup.xaml.
    /// </summary>
    public partial class Startup : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        public Startup()
        {
            this.InitializeComponent();

            if (string.IsNullOrEmpty(TunnelRelayStateManager.ApplicationData.HybridConnectionUrl))
            {
                Logger.LogInfo(CallInfo.Site(), "Starting welcome experiance");
                LoginToAzure gettingStarted = new LoginToAzure();
                gettingStarted.Show();
            }
            else
            {
                Logger.LogInfo(CallInfo.Site(), "User is logged in already. Starting app directly");
                new MainWindow().Show();
            }

            this.Close();
        }
    }
}
