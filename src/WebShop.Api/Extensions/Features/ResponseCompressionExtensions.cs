using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using WebShop.Util.Models;

namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Extension methods for configuring response compression.
/// </summary>
public static class ResponseCompressionExtensions
{
    /// <summary>
    /// Configures response compression with Brotli and Gzip providers.
    /// Follows Microsoft guidelines for optimal performance.
    /// </summary>
    public static void ConfigureResponseCompression(this IServiceCollection services, IConfiguration configuration)
    {
        ResponseCompressionSettings settings = new();
        configuration.GetSection("ResponseCompressionOptions").Bind(settings);

        if (!settings.Enabled)
        {
            return; // Compression disabled
        }

        services.AddResponseCompression(compressionOptions =>
        {
            // Enable compression for HTTPS (always true since HTTP requests are blocked)
            // All requests reaching this middleware are HTTPS (HTTP returns 400 in EnforceHttps)
            compressionOptions.EnableForHttps = true;

            // Add Brotli compression provider (preferred - better compression ratio)
            if (settings.UseBrotli)
            {
                compressionOptions.Providers.Add<BrotliCompressionProvider>();
            }

            // Add Gzip compression provider (fallback for older clients)
            if (settings.UseGzip)
            {
                compressionOptions.Providers.Add<GzipCompressionProvider>();
            }

            // Configure MIME types to compress
            // Default MIME types are already included: text/*, application/json, application/xml, etc.
            compressionOptions.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                settings.AdditionalMimeTypes);

            // Exclude already compressed content types
            compressionOptions.ExcludedMimeTypes = new[]
            {
                "image/png",
                "image/jpeg",
                "image/gif",
                "image/webp",
                "image/svg+xml",
                "application/zip",
                "application/gzip",
                "application/x-gzip",
                "application/x-compress",
                "application/x-compressed",
                "application/x-bzip2",
                "video/*",
                "audio/*",
                "font/*",
                "application/font-woff",
                "application/font-woff2"
            };
        });

        // Configure Brotli compression level
        if (settings.UseBrotli)
        {
            services.Configure<BrotliCompressionProviderOptions>(brotliOptions =>
            {
                // Map integer compression level (0-11) to CompressionLevel enum
                // CompressionLevel enum values: Optimal=0, Fastest=1, NoCompression=2, SmallestSize=3
                // Map ranges: 0-2=Fastest, 3-5=Optimal, 6-8=SmallestSize, 9-11=SmallestSize
                int level = Math.Clamp(settings.BrotliCompressionLevel, 0, 11);
                brotliOptions.Level = level switch
                {
                    <= 2 => CompressionLevel.Fastest,      // Fast compression
                    <= 5 => CompressionLevel.Optimal,    // Balanced (default 4 maps here)
                    _ => CompressionLevel.SmallestSize    // Maximum compression (6-11)
                };
            });
        }

        // Configure Gzip compression level
        if (settings.UseGzip)
        {
            services.Configure<GzipCompressionProviderOptions>(gzipOptions =>
            {
                // Map integer compression level (0-9) to CompressionLevel enum
                // CompressionLevel enum values: Optimal=0, Fastest=1, NoCompression=2, SmallestSize=3
                // Map ranges: 0-2=Fastest, 3-5=Optimal, 6-9=SmallestSize
                int level = Math.Clamp(settings.GzipCompressionLevel, 0, 9);
                gzipOptions.Level = level switch
                {
                    <= 2 => CompressionLevel.Fastest,      // Fast compression
                    <= 5 => CompressionLevel.Optimal,      // Balanced (default 4 maps here)
                    _ => CompressionLevel.SmallestSize     // Maximum compression (6-9)
                };
            });
        }
    }

    /// <summary>
    /// Adds response compression middleware to the pipeline if enabled in configuration.
    /// Must be placed early in the pipeline (before UseRouting) for optimal performance.
    /// </summary>
    public static void UseResponseCompressionIfEnabled(this WebApplication app)
    {
        IConfiguration configuration = app.Configuration;
        ResponseCompressionSettings settings = new();
        configuration.GetSection("ResponseCompressionOptions").Bind(settings);

        if (settings.Enabled)
        {
            // Call the ASP.NET Core middleware through IApplicationBuilder to avoid naming conflict
            IApplicationBuilder appBuilder = app;
            appBuilder.UseResponseCompression();
        }
    }
}

