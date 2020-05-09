using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Objects;
using FidoBack.V1.Models;
using FidoBack.V1.Options;
using FidoBack.V1.Services.DataStore;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FidoBack.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IDataStore _dataStore;
        private readonly IMemoryCache _memoryCache;
        private readonly Fido2 _lib;
        private readonly IndexingOptions _indexOptions;
        private readonly ElasticClient _elasticClient;

        public AuthenticationController(IMemoryCache memoryCache, IDataStore dataStorage, Fido2 lib, IOptions<IndexingOptions> indexOptions, ElasticClient elasticClient)
        {
            _memoryCache = memoryCache;
            _dataStore = dataStorage;
            _lib = lib;
            _indexOptions = indexOptions.Value;
            _elasticClient = elasticClient;
        }

        [HttpPost]
        [EnableCors]
        [Route("/assertionOptions")]
        public async Task<IActionResult> AssertionOptions([FromForm] string username, [FromForm] string userVerification)
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

                var ev = new Event(username, "Successfully made an options for assertion", nameof(AuthenticationController), nameof(AssertionOptions));
                await _elasticClient.IndexAsync(ev, i => i.Index(GetIndexName(nameof(Ok))));

                return Ok(options);
            }

            catch (Exception e)
            {
                var errorEvent = new ErrorEvent(e, username, nameof(AuthenticationController), nameof(AssertionOptions));
                await _elasticClient.IndexAsync(errorEvent, i => i.Index(GetIndexName(nameof(Exception))));
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

            var username = string.Empty;

            try
            {
                o = JsonConvert.DeserializeAnonymousType((Encoding.UTF8.GetString(clientResponse.Response.ClientDataJson)), o);
                var jsonOptions = _memoryCache.Get<string>(o.challenge);

                var parsedObject = JObject.Parse(jsonOptions);

                username = parsedObject["User"]?["Name"]?.ToString();

                var options = Fido2NetLib.AssertionOptions.FromJson(jsonOptions);

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

                var response = new AuthenticationResult
                {
                    ErrorMessage = res.ErrorMessage,
                    Status = res.Status
                };

                var ev = new Event(username, "Successful assertion made", nameof(AuthenticationController), nameof(MakeAssertion));
                await _elasticClient.IndexAsync(ev, i => i.Index(GetIndexName(nameof(Ok))));

                return Ok(response);
            }
            catch (Exception e)
            {
                var errorEvent = new ErrorEvent(e, username, nameof(AuthenticationController), nameof(MakeAssertion));
                await _elasticClient.IndexAsync(errorEvent, i => i.Index(GetIndexName(nameof(Exception))));
                return Ok(new AuthenticationResult { Status = "error", ErrorMessage = FormatException(e) });
            }
        }

        private static string FormatException(Exception e)
        {
            return $"{e.Message}{(e.InnerException != null ? " (" + e.InnerException.Message + ")" : "")}";
        }

        private string GetIndexName(string eventType)
        {
            var indexPrefix = nameof(Exception) == eventType ? _indexOptions.ErrorIndexPrefix : _indexOptions.EventIndexPrefix;
            var datetime = DateTimeOffset.Now;
            return $"{indexPrefix}-{datetime.Year}.{datetime.Month}";
        }
    }

    internal class AuthenticationResult : Fido2ResponseBase
    {
        [JsonProperty("redirectionUri")]
        public string RedirectionUri = "https://moodle.vgtu.lt/my/";
    }
}
