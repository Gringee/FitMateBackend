using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApi.Swagger;

/// <summary>
/// Dodaje domyślne odpowiedzi 4xx/5xx (ProblemDetails) do wszystkich operacji.
/// </summary>
public class DefaultErrorResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Schemat ProblemDetails – swagger pokaże strukturę błędu
        var problemSchema = context.SchemaGenerator.GenerateSchema(
            typeof(ProblemDetails),
            context.SchemaRepository);

        void AddProblemResponse(string statusCode, string description, OpenApiObject? example = null)
        {
            if (!operation.Responses.TryGetValue(statusCode, out var response))
            {
                response = new OpenApiResponse { Description = description };
                operation.Responses[statusCode] = response;
            }

            // Jeśli już ktoś dodał content ręcznie – nie nadpisujemy
            if (response.Content.Count == 0)
            {
                var mediaType = new OpenApiMediaType
                {
                    Schema = problemSchema
                };

                if (example is not null)
                {
                    mediaType.Example = example;
                }

                response.Content["application/json"] = mediaType;
            }
        }

        // PRZYKŁADOWE ciała (proste, front zobaczy format):
        var badRequestExample = new OpenApiObject
        {
            ["type"] = new OpenApiString("https://httpstatuses.com/400"),
            ["title"] = new OpenApiString("One or more validation errors occurred."),
            ["status"] = new OpenApiInteger(400),
            ["traceId"] = new OpenApiString("00-0123456789abcdef0123456789abcdef-0123456789abcdef-00")
        };

        var unauthorizedExample = new OpenApiObject
        {
            ["type"] = new OpenApiString("https://httpstatuses.com/401"),
            ["title"] = new OpenApiString("Unauthorized"),
            ["status"] = new OpenApiInteger(401)
        };

        var forbiddenExample = new OpenApiObject
        {
            ["type"] = new OpenApiString("https://httpstatuses.com/403"),
            ["title"] = new OpenApiString("Forbidden"),
            ["status"] = new OpenApiInteger(403)
        };

        var notFoundExample = new OpenApiObject
        {
            ["type"] = new OpenApiString("https://httpstatuses.com/404"),
            ["title"] = new OpenApiString("Not Found"),
            ["status"] = new OpenApiInteger(404)
        };

        var serverErrorExample = new OpenApiObject
        {
            ["type"] = new OpenApiString("https://httpstatuses.com/500"),
            ["title"] = new OpenApiString("An unexpected error occurred."),
            ["status"] = new OpenApiInteger(500)
        };

        // Dodajemy standardowy zestaw odpowiedzi:
        AddProblemResponse("400", "Bad request / validation or business error.", badRequestExample);
        AddProblemResponse("401", "Unauthorized – brak lub błędny token.", unauthorizedExample);
        AddProblemResponse("403", "Forbidden – brak uprawnień.", forbiddenExample);
        AddProblemResponse("404", "Resource not found.", notFoundExample);
        AddProblemResponse("500", "Unexpected server error.", serverErrorExample);
    }
}