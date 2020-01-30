using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AzureMobileClient.Helpers.Accounts;

namespace AzureMobileClient.Helpers.Http
{
    /// <summary>
    /// Adds Handler to the MobileServiceClient to prompt the user for Authentication
    /// when the response returns an initial 401 Unauthorized response. Then retries the
    /// original call and returns the response.
    /// </summary>
    public class AuthenticationDelegatingHandler<TAccount> : DelegatingHandler
        where TAccount : IAccount
    {
        const string ZumoAuthHeader = "X-ZUMO-AUTH";

        /// <summary>
        /// The <see cref="ICloudService{TAccount}"/> used by the Handler
        /// </summary>
        protected ICloudService<TAccount> _cloudService { get; }

        /// <summary>
        /// Initializes the <see cref="AuthenticationDelegatingHandler{TAccount}" />
        /// </summary>
        public AuthenticationDelegatingHandler(ICloudService<TAccount> cloudService)
        {
            _cloudService = cloudService;
        }

        /// <inheritDoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var user = await _cloudService.LoginAsync();

            request.Headers.Remove(ZumoAuthHeader);
            request.Headers.Add(ZumoAuthHeader, user.MobileServiceClientToken);
            
            return await base.SendAsync(request, cancellationToken);
        }
    }
}