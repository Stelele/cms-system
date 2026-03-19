using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Shared;

public interface IWebApplication : IHost, IApplicationBuilder, IEndpointRouteBuilder;
