using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CopyrightDetector.MusicBackend.Swagger;

/// <summary>
/// Swagger operation filter to handle file upload parameters properly
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || 
                       p.ParameterType == typeof(IFormFile[]) ||
                       p.ParameterType == typeof(IEnumerable<IFormFile>))
            .ToList();

        if (!fileParameters.Any())
            return;

        // Remove existing parameters that conflict with file uploads
        operation.Parameters?.Clear();

        // Set the request body for multipart/form-data
        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>(),
                        Required = new HashSet<string>()
                    }
                }
            }
        };

        var formDataSchema = operation.RequestBody.Content["multipart/form-data"].Schema;

        // Add file upload properties
        foreach (var fileParam in fileParameters)
        {
            formDataSchema.Properties[fileParam.Name ?? "file"] = new OpenApiSchema
            {
                Type = "string",
                Format = "binary",
                Description = "Audio file to upload"
            };

            if (!fileParam.HasDefaultValue)
            {
                formDataSchema.Required.Add(fileParam.Name ?? "file");
            }
        }

        // Add other form parameters from the method
        var otherFormParameters = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.FromFormAttribute), false).Any() && 
                       p.ParameterType != typeof(IFormFile))
            .ToList();

        foreach (var param in otherFormParameters)
        {
            var schema = new OpenApiSchema();
            
            if (param.ParameterType == typeof(string))
            {
                schema.Type = "string";
            }
            else if (param.ParameterType == typeof(int) || param.ParameterType == typeof(int?))
            {
                schema.Type = "integer";
                schema.Format = "int32";
            }
            else if (param.ParameterType == typeof(double) || param.ParameterType == typeof(double?))
            {
                schema.Type = "number";
                schema.Format = "double";
            }
            else if (param.ParameterType == typeof(bool) || param.ParameterType == typeof(bool?))
            {
                schema.Type = "boolean";
            }
            else
            {
                schema.Type = "string";
            }

            // Set default value if available
            if (param.HasDefaultValue && param.DefaultValue != null)
            {
                if (param.ParameterType == typeof(string))
                {
                    schema.Default = new Microsoft.OpenApi.Any.OpenApiString(param.DefaultValue.ToString());
                }
                else if (param.ParameterType == typeof(int) || param.ParameterType == typeof(int?))
                {
                    schema.Default = new Microsoft.OpenApi.Any.OpenApiInteger((int)param.DefaultValue);
                }
                else if (param.ParameterType == typeof(double) || param.ParameterType == typeof(double?))
                {
                    schema.Default = new Microsoft.OpenApi.Any.OpenApiDouble((double)param.DefaultValue);
                }
                else if (param.ParameterType == typeof(bool) || param.ParameterType == typeof(bool?))
                {
                    schema.Default = new Microsoft.OpenApi.Any.OpenApiBoolean((bool)param.DefaultValue);
                }
            }
            else if (!param.HasDefaultValue)
            {
                formDataSchema.Required.Add(param.Name ?? "param");
            }

            formDataSchema.Properties[param.Name ?? "param"] = schema;
        }
    }
}
