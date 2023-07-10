using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace BNHPortalServices
{
    internal class AuthorizationMiddleware : IFunctionsWorkerMiddleware
    {
        private ILogger _logger;

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            _logger = context.GetLogger<AuthorizationMiddleware>();

            //Process the Authorization header if it is presents. Else, set response status to be 401.
            if (context.BindingContext.BindingData.ContainsKey("Headers"))
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>((string)context.BindingContext.BindingData["Headers"]);

                if (headers.ContainsKey("Authorization"))
                {
                    //Extract the Bearer token
                    var authorization = AuthenticationHeaderValue.Parse(headers["Authorization"]);
                    var bearerToken = authorization.Parameter;

                    //Get the PortalPublicKeyProvider service to retrieve the Portal's public key
                    var portalKeyProvider = context.InstanceServices.GetRequiredService<IPortalPublicKeyProvider>();

                    var validationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = await portalKeyProvider.GetPortalPublicKeyAsync(),

                        //We are setting this to false here because by default the user token returned by Portal does not contain an
                        //audience value. You can change this behaviour by registering a client ID using the
                        //ImplicitGrantFlow/RegisteredClientId Site Setting in Portal. Read here for more
                        //details: https://docs.microsoft.com/en-us/power-apps/maker/portals/oauth-implicit-grant-flow#register-client-id-for-implicit-grant-flow.
                        ValidateAudience = false,

                        //We are setting this to false as we are already validating the signing key.
                        ValidateIssuer = false
                    };

                    try
                    {
                        //The ValidateToken method throws an exception if the token is invalid. We therefore will set the response
                        //status to 401 on exception.
                        new JwtSecurityTokenHandler().ValidateToken(bearerToken, validationParameters, out SecurityToken validatedToken);

                        //Token is valid - extract user info and store it using our "vessel", the UserInfoProvider service.
                        //Our function logic will use the UserInfoProvider service to pull back out the user
                        //information when needed.
                        var userInfo = new UserInfo(validatedToken as JwtSecurityToken);
                        var userInfoProvider = context.InstanceServices.GetRequiredService<IUserInfoProvider>();

                        userInfoProvider.UserInfo = userInfo;
                    }
                    catch (Exception e)
                    {
                        await SetUnauthorizedResponse(context, e.Message);
                        return;
                    }

                    await next(context);
                }
                else
                {
                    await SetUnauthorizedResponse(context, "Authorization header not found.");
                }
            }
            else
            {
                await SetUnauthorizedResponse(context, "Authorization header not found.");
            }
        }

        private async Task SetUnauthorizedResponse(FunctionContext context, string message)
        {
            _logger.LogWarning($"Authorization failed: {message}");

            //IMPORTANT: The calls to context.GetHttpRequestDataAsync() and context.GetInvocationResult() require
            //at least version 1.8.0-preview1 of the package Microsoft.Azure.Functions.Worker.
            var httpRequestData = await context.GetHttpRequestDataAsync();
            var response = httpRequestData.CreateResponse();

            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteStringAsync(message);

            context.GetInvocationResult().Value = response;
        }
    }
}