using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Fido2NetLib;
using Fido2NetLib.Objects;
using FidoBack.V1.Models;
using FidoBack.V1.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FidoBack.V1.Services.DataStore
{
    internal class SqlServerStore : IDataStore
    {
        private readonly string _connectionString;

        public SqlServerStore(IOptions<SqlServerStorageOptions> sqlServerStorageOptions)
        {
            _connectionString = sqlServerStorageOptions.Value.ConnectionString;
        }
        public Fido2User AddUser(string username, Func<Fido2User> addCallback)
        {
            using var conn = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM USERS WHERE Name = @Name", conn);
            conn.Open();
            using var ts = new TransactionScope();
            command.Parameters.AddWithValue("Name", username);
            var users = new List<Fido2User>();

            using (var rdr = command.ExecuteReader())
            {
                while (rdr.Read())
                {
                    var fu = new Fido2User
                    {
                        Id = Base64Url.Decode(rdr["Id"] as string),
                        Name = rdr["Name"] as string,
                        DisplayName = rdr["DisplayName"] as string
                    };
                    users.Add(fu);
                }
            }

            if (users.Count > 0 && GetCredentialsByUser(users[0]).Count > 0)
                throw new Exception($"{username} already exists");

            var newUser = addCallback();

            if (newUser == default)
                return default;

            command.Parameters.Clear();
            command.CommandText = "INSERT INTO USERS VALUES (@Id,@Name,@DisplayName)";

            command.Parameters.AddWithValue("Id", Base64Url.Encode(newUser.Id));
            command.Parameters.AddWithValue("Name", newUser.Name);
            command.Parameters.AddWithValue("DisplayName", newUser.DisplayName);

            var result = command.ExecuteNonQuery();
            if (result != 1)
                throw new Exception($"can not create {username}");
            return newUser;
        }

        public Fido2User GetUser(string username)
        {
            using var conn = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM USERS WHERE Name = @Name", conn);
            conn.Open();
            using var ts = new TransactionScope();
            command.Parameters.AddWithValue("Name", username);
            Fido2User user = null;

            using (var rdr = command.ExecuteReader())
            {
                if (rdr.Read())
                    user = new Fido2User
                    {
                        Id = Base64Url.Decode(rdr["Id"] as string),
                        Name = rdr["Name"] as string,
                        DisplayName = rdr["DisplayName"] as string
                    };
            }

            if (user == null)
                throw new Exception($"{username} not registered");

            return user;
        }

        public List<StoredCredential> GetCredentialsByUser(Fido2User user)
        {
            using var conn = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM Credentials WHERE UserId = @UserId ", conn);
            conn.Open();
            command.Parameters.AddWithValue("UserId", Base64Url.Encode(user.Id));
            using var rdr = command.ExecuteReader();
            return GetCredentialsFromReader(rdr).ToList();
        }

        public StoredCredential GetCredentialById(byte[] id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM Credentials WHERE Id = @Id ", conn);
            conn.Open();
            command.Parameters.AddWithValue("Id", Base64Url.Encode(id));
            using var rdr = command.ExecuteReader();
            return GetCredentialsFromReader(rdr).SingleOrDefault();
        }

        public Task<List<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle)
        {
            using var conn = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM Credentials WHERE UserHandle = @UserHandle ", conn);
            conn.Open();
            command.Parameters.AddWithValue("Id", Base64Url.Encode(userHandle));
            using var rdr = command.ExecuteReader();
            return Task.FromResult(GetCredentialsFromReader(rdr).ToList());
        }

        public void UpdateCounter(byte[] credentialId, uint counter)
        {
            using var conn = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Credentials SET SignatureCounter = @Counter WHERE Id = @Id", conn);
            conn.Open();
            command.Parameters.AddWithValue("Id", Base64Url.Encode(credentialId));
            command.Parameters.AddWithValue("Counter", (int)counter);
            var result = command.ExecuteNonQuery();
            if (result != 1)
                throw new Exception(
                    $"can not update counter with value {counter} for credential {Base64Url.Encode(credentialId)}");
        }

        public void AddCredentialToUser(Fido2User user, StoredCredential credential)
        {
            using var conn = new SqlConnection(_connectionString);
            using var command = new SqlCommand("INSERT INTO [dbo].[Credentials] VALUES(@UserId,@Id,@PublicKeyCredentialType,@PublicKey,@UserHandle,@SignatureCounter,@CredentialType,@RegistrationDate,@AAGuid)", conn);
            conn.Open();
            command.Parameters.AddWithValue("UserId", Base64Url.Encode(user.Id));
            command.Parameters.AddWithValue("Id", Base64Url.Encode(credential.Descriptor.Id));
            command.Parameters.AddWithValue("PublicKeyCredentialType", credential.Descriptor.Type);
            command.Parameters.AddWithValue("PublicKey", Base64Url.Encode(credential.PublicKey));
            command.Parameters.AddWithValue("UserHandle", Base64Url.Encode(credential.UserHandle));
            command.Parameters.AddWithValue("SignatureCounter", (int)credential.SignatureCounter);
            command.Parameters.AddWithValue("CredentialType", credential.CredType);
            command.Parameters.AddWithValue("RegistrationDate", credential.RegDate);
            command.Parameters.AddWithValue("AAGuid", credential.AaGuid);
            command.ExecuteNonQuery();
        }

        public Task<List<Fido2User>> GetUsersByCredentialIdAsync(byte[] credentialId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM Credentials WHERE Id = @Id ", conn);
            conn.Open();
            command.Parameters.AddWithValue("Id", Base64Url.Encode(credentialId));
            using (var rdr = command.ExecuteReader())
            {
                var credential = GetCredentialsFromReader(rdr).SingleOrDefault();
                if (credential == default)
                    return Task.FromResult(new List<Fido2User>());
                command.Parameters.Clear();
                command.CommandText = "SELECT * FROM Users WHERE Id = @Id";
                command.Parameters.AddWithValue("Id", Base64Url.Encode(credential.UserId));
            }

            using var userReader = command.ExecuteReader();
            var users = new List<Fido2User>();
            while (userReader.Read())
            {
                var fu = new Fido2User
                {
                    Id = Base64Url.Decode(userReader["Id"] as string),
                    Name = userReader["Name"] as string,
                    DisplayName = userReader["DisplayName"] as string
                };
                users.Add(fu);
            }

            return Task.FromResult(users);
        }

        private static IEnumerable<StoredCredential> GetCredentialsFromReader(IDataReader rdr)
        {
            while (rdr.Read())
            {
                var type = rdr["PublicKeyCredentialType"] as string;
                Enum.TryParse(type, true, out PublicKeyCredentialType typerEnum);
                var cred = new StoredCredential
                {
                    AaGuid = (Guid)rdr["AAGuid"],
                    CredType = rdr["CredentialType"] as string,
                    Descriptor = new PublicKeyCredentialDescriptor(Base64Url.Decode(rdr["Id"] as string))
                    {
                        Transports = new AuthenticatorTransport[0],
                        Type = typerEnum
                    },
                    PublicKey = Base64Url.Decode(rdr["PublicKey"] as string),
                    RegDate = (DateTime)rdr["RegistrationDate"],
                    SignatureCounter = (uint)(int)rdr["SignatureCounter"],
                    UserId = Base64Url.Decode(rdr["UserId"] as string),
                    UserHandle = Base64Url.Decode(rdr["UserHandle"] as string)
                };
                yield return cred;
            }
        }
    }
}