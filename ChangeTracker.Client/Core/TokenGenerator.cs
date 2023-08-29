using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;


namespace ChangeTracker.Client.Core
{
    public static class TokenGenerator
    {
        public static string GenerateToken(string secret, string tableName, string rowKey = "", int duration = 5)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = new Dictionary<string, object>
                {
                    { "table", tableName }
                },
                Expires = DateTime.UtcNow.AddMinutes(duration),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            if (!string.IsNullOrEmpty(rowKey)) tokenDescriptor.Claims.Add("key", rowKey);

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
