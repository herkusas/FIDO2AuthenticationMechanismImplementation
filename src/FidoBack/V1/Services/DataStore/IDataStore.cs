using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fido2NetLib;
using FidoBack.V1.Models;

namespace FidoBack.V1.Services.DataStore
{
    public interface IDataStore
    {
        Fido2User AddUser(string username, Func<Fido2User> addCallback);
        Fido2User GetUser(string username);
        List<StoredCredential> GetCredentialsByUser(Fido2User user);
        StoredCredential GetCredentialById(byte[] id);
        Task<List<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle);
        void UpdateCounter(byte[] credentialId, uint counter);
        void AddCredentialToUser(Fido2User user, StoredCredential credential);
        Task<List<Fido2User>> GetUsersByCredentialIdAsync(byte[] credentialId);
    }
}