#define DEBUG
using System;
using System.Net;
using System.Threading.Tasks;
using HttpApi.Exceptions;
using Microsoft.AspNetCore.Http;

namespace HttpApi
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context /* other dependencies */)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            if (exception is InternalExceptions.NotFoundException)
            {
                context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                return context.Response.WriteAsync("Not Found");
            }

            context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
            return context.Response.WriteAsync(exception.Message);
        }
    }
}