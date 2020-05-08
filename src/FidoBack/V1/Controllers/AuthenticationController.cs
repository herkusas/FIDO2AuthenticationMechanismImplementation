using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Objects;
using FidoBack.V1.Services.DataStore;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace FidoBack.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IDataStore _dataStore;
        private readonly IMemoryCache _memoryCache;
        private readonly Fido2 _lib;

        public AuthenticationController(IMemoryCache memoryCache, IDataStore dataStorage, Fido2 lib)
        {
            _memoryCache = memoryCache;
            _dataStore = dataStorage;
            _lib = lib;
        }

        [HttpPost]
        [EnableCors]
        [Route("/assertionOptions")]
        public ActionResult AssertionOptionsPost([FromForm] string username, [FromForm] string userVerification)
        {
            try
            {
                var existingCredentials = new List<PublicKeyCredentialDescriptor>();

                if (!string.IsNullOrEmpty(username))
                {

                    var user = _dataStore.GetUser(username);
                    if (user == null) throw new ArgumentException("Username was not registered");

                    existingCredentials = _dataStore.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();
                }

                var authenticationExtensionsClientInputs = new AuthenticationExtensionsClientInputs { SimpleTransactionAuthorization = "FIDO", GenericTransactionAuthorization = new TxAuthGenericArg { ContentType = "text/plain", Content = new byte[] { 0x46, 0x49, 0x44, 0x4F } }, UserVerificationIndex = true, Location = true, UserVerificationMethod = true };

                var uv = string.IsNullOrEmpty(userVerification) ? UserVerificationRequirement.Required : userVerification.ToEnum<UserVerificationRequirement>();
                var options = _lib.GetAssertionOptions(
                    existingCredentials,
                    uv,
                    authenticationExtensionsClientInputs
                );

                _memoryCache.Set(Base64Url.Encode(options.Challenge), options.ToJson());

                return Ok(options);
            }

            catch (Exception e)
            {
                return Ok(new AssertionOptions { Status = "error", ErrorMessage = FormatException(e) });
            }
        }

        [HttpPost]
        [EnableCors]
        [Route("/makeAssertion")]
        public async Task<IActionResult> MakeAssertion([FromBody] AuthenticatorAssertionRawResponse clientResponse)
        {
            var o = new
            {
                challenge = string.Empty,
                origin = string.Empty,
                type = string.Empty
            };

            try
            {
                o = JsonConvert.DeserializeAnonymousType((Encoding.UTF8.GetString(clientResponse.Response.ClientDataJson)), o);
                var jsonOptions = _memoryCache.Get<string>(o.challenge);
                
                var options = AssertionOptions.FromJson(jsonOptions);

                var credentials = _dataStore.GetCredentialById(clientResponse.Id);

                if (credentials == null)
                {
                    throw new Exception("Unknown credentials");
                }

                var storedCounter = credentials.SignatureCounter;

                async Task<bool> Callback(IsUserHandleOwnerOfCredentialIdParams args)
                {
                    var storedCredentials = await _dataStore.GetCredentialsByUserHandleAsync(args.UserHandle);
                    return storedCredentials.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
                }

                var res = await _lib.MakeAssertionAsync(clientResponse, options, credentials.PublicKey, storedCounter, Callback);

                _dataStore.UpdateCounter(res.CredentialId, res.Counter);

                var response = new LoginResult
                {
                    ErrorMessage = res.ErrorMessage,
                    Status = res.Status
                };

                return Ok(response);
            }
            catch (Exception e)
            {
                return Ok(new LoginResult { Status = "error", ErrorMessage = FormatException(e) });
            }
        }

        private static string FormatException(Exception e)
        {
            return $"{e.Message}{(e.InnerException != null ? " (" + e.InnerException.Message + ")" : "")}";
        }
    }

    internal class LoginResult : Fido2ResponseBase
    {
    }
}
