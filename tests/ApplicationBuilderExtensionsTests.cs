using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;

#if OLDNETCORE
using Microsoft.AspNetCore.Builder.Internal;
#endif

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Prometheus.Client.Collectors;
using Xunit;

namespace Prometheus.Client.AspNetCore.Tests;

public class ApplicationBuilderExtensionsTests
{
    private readonly ICollectorRegistry _registry;
    private readonly IApplicationBuilder _app;
    private readonly HttpContext _ctx;

    public ApplicationBuilderExtensionsTests()
    {
        var services = new ServiceCollection();
        _app = new ApplicationBuilder(services.BuildServiceProvider());
        _registry = new CollectorRegistry();
        _ctx = new DefaultHttpContext();
        _ctx.Request.Path = PrometheusOptions.DefaultMapPath;
    }

    [Fact]
    public void DefaultUrl_Return_200()
    {
        _app.UsePrometheusServer(q => q.CollectorRegistryInstance = _registry);
        _app.Build().Invoke(_ctx);

        Assert.Equal(200, _ctx.Response.StatusCode);
    }

    [Fact]
    public void WrongUrl_Return_404()
    {
        _app.UsePrometheusServer();

        _ctx.Request.Path = "/wrong";
        _app.Build().Invoke(_ctx);

        Assert.Equal(404, _ctx.Response.StatusCode);
    }

    [Fact]
    public void Default_ContentType()
    {
        _app.UsePrometheusServer(q => q.CollectorRegistryInstance = _registry);

        _app.Build().Invoke(_ctx);

        Assert.Equal("text/plain; version=0.0.4", _ctx.Response.ContentType);
    }

    [Theory]
    [MemberData(nameof(GetEncodings))]
    public void CustomResponseEncoding_Return_ContentType_With_Encoding(Encoding encoding)
    {
        _app.UsePrometheusServer(q =>
        {
            q.CollectorRegistryInstance = _registry;
            q.ResponseEncoding = encoding;
        });

        _app.Build().Invoke(_ctx);

        Assert.Equal($"text/plain; version=0.0.4; charset={encoding.BodyName}", _ctx.Response.ContentType);
    }

    public static IEnumerable<object[]> GetEncodings()
    {
        yield return new object[] { Encoding.UTF8 };
        yield return new object[] { Encoding.Unicode };
        yield return new object[] { Encoding.ASCII };
        yield return new object[] { Encoding.UTF32 };
    }
}
