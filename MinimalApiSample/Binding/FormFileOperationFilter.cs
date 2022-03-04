﻿using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MinimalApiSample.Binding;

public class FormFileOperationFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var acceptsFormFile = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAcceptsMetadata>()
            .Any(m => m.RequestType == typeof(IFormFile) || m.RequestType == typeof(FormFileContent));

        if (acceptsFormFile)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Required = new HashSet<string> { "file" },
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["file"] = new OpenApiSchema()
                                {
                                    Type = "string",
                                    Format = "binary"
                                }
                            }
                        },
                        Encoding = new Dictionary<string, OpenApiEncoding>
                        {
                            ["file"] = new OpenApiEncoding { Style = ParameterStyle.Form }
                        }
                    }
                }
            };
        }
    }
}
