using System;
using Microsoft.AspNetCore.Http;

namespace MockApi.Server
{
    public static class PathStringExtensions
    {
        public static string[] GetSegments(this PathString pathString)
        {
            return pathString.Value.Split("/", StringSplitOptions.RemoveEmptyEntries);
        }
    }
}