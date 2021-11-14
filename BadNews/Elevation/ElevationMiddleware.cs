using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BadNews.Elevation
{
    public class ElevationMiddleware
    {
        private const string ElevationKey = "up";
        private const string ElevationPath = "/elevation";

        private readonly RequestDelegate next;
    
        public ElevationMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
    
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path != ElevationPath)
            {
                await next(context);
                return;
            }

            var isElevated = context.Request.Query.Keys.Contains(ElevationKey);
            if (isElevated)
            {
                context.Response.Cookies.Append(
                    ElevationConstants.CookieName,
                    ElevationConstants.CookieValue,
                    new CookieOptions
                    {
                        HttpOnly = true
                    }
                );
            }
            else
            {
                context.Response.Cookies.Delete(ElevationConstants.CookieName);
            }

            context.Response.Redirect("/");
        }
    }
}
