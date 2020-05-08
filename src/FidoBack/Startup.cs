using Fido2NetLib;
using FidoBack.V1.Options;
using FidoBack.V1.Services.DataStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FidoBack
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<SqlServerStorageOptions>(options =>
                options.ConnectionString = Configuration.GetConnectionString("FidoDB"));

            services.Configure<FidoOptions>(options => Configuration.GetSection("FidoOptions").Bind(options));

            services.AddTransient(
                provider => new Fido2(new Fido2Configuration
                {
                    ServerDomain = provider.GetService<IOptions<FidoOptions>>().Value.ServerDomain,
                    ServerName = "FIDO2 Authentication Mechanism Implementation",
                    Origin = provider.GetService<IOptions<FidoOptions>>().Value.Origin,
                    TimestampDriftTolerance = 0
                }));

            services.AddControllers().AddNewtonsoftJson();
            services.AddHttpClient();
            services.AddMemoryCache();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {

                        builder.AllowAnyOrigin().AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);
            services.AddSingleton<IDataStore, SqlServerStore>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
