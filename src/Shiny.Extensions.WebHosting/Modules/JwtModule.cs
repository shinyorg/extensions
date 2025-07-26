// using System.Text;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Shiny.Modules;
//
// public class JwtModule : IInfrastructureModule
// {
//     public void Add(WebApplicationBuilder builder)
//     {
//         builder
//             .Services
//             .AddAuthentication(options =>
//             {
//                 options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
//                 options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//             })
//             .AddJwtBearer(jwt =>
//             {
//                 var auth = new AuthOptions();
//                 builder.Configuration.GetSection("Auth").Bind(auth);
//         
//                 //jwt.Authority = auth.Authority;
//                 jwt.TokenValidationParameters = new TokenValidationParameters
//                 {
//                     ValidateIssuer = false,
//                     ValidateAudience = false,
//                     ValidateLifetime = true,
//                     // ValidIssuer = "",
//                     // ValidAudience = "",
//                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.JwtSecret))
//                 };
//                 jwt.Validate();
//             });
//     }
//
//     public void Use(WebApplication app)
//     {
//         app.UseAuthentication();
//         app.UseAuthorization();
//     }
// }