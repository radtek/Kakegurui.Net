using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Kakegurui.Web.Utils
{
    public class Token
    {
        public const string Audience = "seemmo";
        public const string Issuer = "seemmo";
        public const string Key = "dd%88*377f6d&f£$$£$FdddFF33fssDG^!3";
        public static string CreateToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(Issuer,Audience, claims, DateTime.Now,DateTime.Now.AddDays(30),creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static List<Claim> GetClaims(string token)
        {
            JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.Claims.ToList();

        }
    }
}
