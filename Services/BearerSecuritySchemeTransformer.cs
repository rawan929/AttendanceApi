using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AttendanceApi.Services;

// بتعرّف Scalar إن الـ API بيستخدم Bearer token،
// فيظهر لك مكان تحطي فيه الـ token مرة واحدة بدل ما تضيفي header في كل request
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider schemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes = await schemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(s => s.Name == "Bearer"))
            return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT"
            },
            // مفتاح الجهاز: بيظهر في Scalar لـ /api/device/* بس
            ["ApiKey"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = "X-Api-Key",
                In = ParameterLocation.Header
            }
        };

        foreach (var (path, pathItem) in document.Paths)
        {
            var schemeName = path.StartsWith("/api/device", StringComparison.OrdinalIgnoreCase)
                ? "ApiKey"
                : "Bearer";

            foreach (var operation in pathItem.Operations!.Values)
            {
                operation.Security ??= new List<OpenApiSecurityRequirement>();
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(schemeName, document)] = new List<string>()
                });
            }
        }
    }
}
