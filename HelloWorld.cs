using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;

namespace BNHPortalServices
{
    public class HelloWorld
    {
        private readonly ILogger _logger;
        private readonly IUserInfoProvider _userInfoProvider;

        //Inject the UserInfoProvider service into this class so we can access the user information.
        public HelloWorld(ILoggerFactory loggerFactory, IUserInfoProvider userInfoProvider)
        {
            _logger = loggerFactory.CreateLogger<HelloWorld>();
            _userInfoProvider = userInfoProvider;
        }

        [Function("HelloWorld")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            //Access information of the current user
            var userInfo = _userInfoProvider.UserInfo;

            response.WriteString($"Hello. You are {userInfo.UserId} with email address {userInfo.Email}.");

            return response;
        }
    }
}