using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using System;
using Google.Apis.Auth.OAuth2;

namespace ExpenseTracker.API.Middleware
{
    public class FirebaseAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public FirebaseAuthMiddleware(RequestDelegate next)
        {
            _next = next;
            if (FirebaseApp.DefaultInstance == null)
            {
                try
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.GetApplicationDefault()
                    });
                }
                catch (Exception)
                {
                }
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/" ||
                context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/api/health", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) || 
                context.Request.Path.StartsWithSegments("/v1/swagger.json", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Missing Authorization header");
                return;
            }

            string token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            
            try
            {
                if (FirebaseApp.DefaultInstance != null)
                {
                    FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                    context.Items["User"] = decodedToken;
                    context.Items["FirebaseUid"] = decodedToken.Uid;
                }
                else 
                {
                    if (string.IsNullOrEmpty(token))
                    {
                        throw new Exception("Invalid token");
                    }
                    context.Items["FirebaseUid"] = token;
                }
            }
            catch (Exception)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid Firebase token");
                return;
            }

            await _next(context);
        }
    }
}
