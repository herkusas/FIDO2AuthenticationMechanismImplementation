using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Objects;
using FidoBack.V1.Commands;
using FidoBack.V1.Models;
using FidoBack.V1.Options;
using FidoBack.V1.Services.DataStore;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;

namespace FidoBack.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : Controller
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDataStore _dataStore;
        private readonly Fido2 _lib;
        private readonly ElasticClient _elasticClient;
        private readonly IndexingOptions _indexOptions;

        public RegistrationController(IDataStore dataStore, IMemoryCache memoryCache, Fido2 lib, ElasticClient elasticClient, IOptions<IndexingOptions> indexOptions)
        {
            _memoryCache = memoryCache;
            _dataStore = dataStore;
            _lib = lib;
            _elasticClient = elasticClient;
            _indexOptions = indexOptions.Value;
        }

        [HttpPost]
        [EnableCors]
        [Route("/makeCredentialOptions")]
        public async Task<IActionResult> MakeCredentialOptions([FromBody] MakeCredentialOptionsRequest request)
        {
            try
            {
                var user = _dataStore.AddUser(request.Username, () => new Fido2User
                {
                    DisplayName = request.DisplayName,
                    Name = request.Username,
                    Id = Encoding.UTF8.GetBytes(request.Username) 
                });

                var existingKeys = _dataStore.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();

                var authenticatorSelection = new AuthenticatorSelection
                {
                    RequireResidentKey = request.RequireResidentKey,
                    UserVerification = request.UserVerification.ToEnum<UserVerificationRequirement>()
                };

                if (!string.IsNullOrEmpty(request.AuthType))
                    authenticatorSelection.AuthenticatorAttachment = request.AuthType.ToEnum<AuthenticatorAttachment>();

                var authenticationExtensionsClientInputs = new AuthenticationExtensionsClientInputs { Extensions = true, UserVerificationIndex = true, Location = true, UserVerificationMethod = true, BiometricAuthenticatorPerformanceBounds = new AuthenticatorBiometricPerfBounds { FAR = float.MaxValue, FRR = float.MaxValue } };

                var options = _lib.RequestNewCredential(user, existingKeys, authenticatorSelection, request.AttType.ToEnum<AttestationConveyancePreference>(), authenticationExtensionsClientInputs);

                _memoryCache.Set(Base64Url.Encode(options.Challenge), options.ToJson());

                var ev = new Event(request.Username, "Successfully made credential options", nameof(RegistrationController), nameof(MakeCredentialOptions));
                await _elasticClient.IndexAsync(ev, i => i.Index(GetIndexName(nameof(Ok))));

                return Ok(options);
            }
            catch (Exception e)
            {
                var errorEvent = new ErrorEvent(e, request.Username, nameof(RegistrationController), nameof(MakeCredentialOptions));
                await _elasticClient.IndexAsync(errorEvent, i => i.Index(GetIndexName(nameof(Exception))));
                return Ok(new CredentialCreateOptions { Status = "error", ErrorMessage = FormatException(e) });
            }
        }

        [HttpPost]
        [EnableCors]
        [Route("/makeCredential")]
        public async Task<IActionResult> MakeCredential([FromBody] AuthenticatorAttestationRawResponse attestationResponse)
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
                o = JsonConvert.DeserializeAnonymousType((Encoding.UTF8.GetString(attestationResponse.Response.ClientDataJson)), o);
                var jsonOptions = _memoryCache.Get<string>(o.challenge);
                var options = CredentialCreateOptions.FromJson(jsonOptions);
                username = options.User.Name;

                async Task<bool> Callback(IsCredentialIdUniqueToUserParams args)
                {
                    var users = await _dataStore.GetUsersByCredentialIdAsync(args.CredentialId);
                    return users.Count <= 0;
                }

                var success = await _lib.MakeNewCredentialAsync(attestationResponse, options, Callback);

                _dataStore.AddCredentialToUser(options.User, new StoredCredential
                {
                    Descriptor = new PublicKeyCredentialDescriptor(success.Result.CredentialId),
                    PublicKey = success.Result.PublicKey,
                    UserHandle = success.Result.User.Id,
                    SignatureCounter = success.Result.Counter,
                    CredType = success.Result.CredType,
                    RegDate = DateTime.Now,
                    AaGuid = success.Result.Aaguid
                });

                var ev = new Event(username, "Successfully logged the person in", nameof(RegistrationController), nameof(MakeCredential));
                await _elasticClient.IndexAsync(ev, i => i.Index(GetIndexName(nameof(Ok))));

                return Ok(success);
            }
            catch (Exception e)
            {
                var errorEvent = new ErrorEvent(e, username, nameof(RegistrationController), nameof(MakeCredential));
                await _elasticClient.IndexAsync(errorEvent, i => i.Index(GetIndexName(nameof(Exception))));
                return Ok(new Fido2.CredentialMakeResult { Status = "error", ErrorMessage = FormatException(e) + $"ClientDataJson = {Encoding.UTF8.GetString(attestationResponse.Response.ClientDataJson)}" });
            }
        }

        private static string FormatException(Exception e)
        {
            return $"{e.Message}{(e.InnerException != null ? " (" + e.InnerException.Message + ")" : "")}";
        }

        private string GetIndexName(string eventType)
        {
            var indexPrefix = nameof(Exception) == eventType ? _indexOptions.ErrorIndexPrefix: _indexOptions.EventIndexPrefix;
            var datetime = DateTimeOffset.Now;
            return $"{indexPrefix}-{datetime.Year}.{datetime.Month}";
        }
    }
}
