using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BNHPortalServices
{
    internal interface IPortalPublicKeyProvider
    {
        Task<RsaSecurityKey> GetPortalPublicKeyAsync();
    }

    /// <summary>
    /// This service queries the target Portal and retrieves its public key. This service caches the key after the initial request, and therefore should be
    /// registered as a singleton.
    /// 
    /// This service reads the URL of the target Portal using the app configuration key 'PortalUrl'.
    /// </summary>
    internal class PortalPublicKeyProvider : IPortalPublicKeyProvider
    {
        //Most of the code below is from https://github.com/microsoft/PowerApps-Samples/blob/1adb4891a312555a2c36cfe7b99c0a225a934a0d/portals/ExternalWebApiConsumingPortalOAuthTokenSample/ExternalWebApiConsumingPortalOAuthTokenSample/App_Start/Startup.cs
        //with some refactoring.

        private RsaSecurityKey _portalPublicKey;

        public async Task<RsaSecurityKey> GetPortalPublicKeyAsync()
        {
            if (_portalPublicKey == null)
            {
                //Query the target Portal and retrieve its public key as plain text, and then return it as a RsaSecurityKey - which is required 
                //for validating the Bearer token.

                var publicKeyAsText = await GetPortalPublicKeyAsTextAsync();

                var pemReader = new PemReader(new StringReader(publicKeyAsText));
                var keyParameters = (RsaKeyParameters)pemReader.ReadObject();

                var rsaParameters = new RSAParameters
                {
                    Modulus = keyParameters.Modulus.ToByteArrayUnsigned(),
                    Exponent = keyParameters.Exponent.ToByteArrayUnsigned()
                };

                var rsaCryptoServiceProvider = new RSACryptoServiceProvider();
                rsaCryptoServiceProvider.ImportParameters(rsaParameters);

                _portalPublicKey = new RsaSecurityKey(rsaCryptoServiceProvider);
            }

            return _portalPublicKey;
        }

        private async Task<string> GetPortalPublicKeyAsTextAsync()
        {
            var portalPublicKeyUrl = $"{Environment.GetEnvironmentVariable("PortalUrl")}/_services/auth/publickey";

            var httpClient = new HttpClient();
            return await httpClient.GetStringAsync(portalPublicKeyUrl);
        }
    }
}