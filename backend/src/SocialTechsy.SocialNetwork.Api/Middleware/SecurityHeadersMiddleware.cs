namespace SocialTechsy.SocialNetwork.Api.Middleware;

public static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseApiSecurityHeaders(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.Append("X-Content-Type-Options", "nosniff");
            headers.Append("X-Frame-Options", "DENY");
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.Append("X-XSS-Protection", "1; mode=block");

            var isSwaggerUi = env.IsDevelopment() &&
                (context.Request.Path.StartsWithSegments("/swagger")
                 || context.Request.Path.StartsWithSegments("/swagger-ui"));

            if (!isSwaggerUi)
            {
                headers.Append(
                    "Content-Security-Policy",
                    "default-src 'none'; frame-ancestors 'none'; base-uri 'none'");
            }

            if (env.IsProduction())
            {
                headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }

            await next();
        });
    }
}
