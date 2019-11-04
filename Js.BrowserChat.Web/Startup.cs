using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Js.BrowserChat.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Register the DbContext defined in Microsoft.AspNetCore.Identity.EntityFrameworkCore nuget package
            // Use helper method from EntityFramework to add the db context as a scoped registration
            // When adding Microsoft.EntityFrameworkCore.SqlServer we get the option to specify Sqlserver
            var connectionString = @"Data Source=(LocalDb)\MSSQLLocalDB;Database=Js.BrowserChat.IdentityUser;trusted_connection=yes;";
            
            // We need to set this assembly name here in order to not fail the CLI migrations command: dotnet ef migrations add Initial
            // So we can have an initial migration to create the database as we need it.
            // After the migration is created we can execute a dotnet ef database update
            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            services.AddDbContext<IdentityDbContext>(opt => opt.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationAssembly)));

            // Register Identity via nuget Microsoft.Extensions.Identity.Core and say we are going to use IdentityUser as the user to use.
            services.AddIdentityCore<IdentityUser>(options => { });

            // Say what user to use and what user store in this case UserOnlyStore and IdentityDbContext defined in Microsoft.AspNetCore.Identity.EntityFrameworkCore nuget package
            services.AddScoped<IUserStore<IdentityUser>, UserOnlyStore<IdentityUser, IdentityDbContext>>();

            // So we install the following nuget: Microsoft.AspNetCore.Authentication.Cookies
            // Saying what scheme to use by default and what endpoint to use when login is challenged by one of the app actions.
            services.AddAuthentication("cookies").AddCookie(authenticationScheme: "cookies", options => options.LoginPath = "/Home/Login");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            // In order to allow log in we will use the default asp net core cookie authentication middleware
            // So we install the following nuget: Microsoft.AspNetCore.Authentication.Cookies
            // We put it here so MVC does not take over routing before the authentication is enabled
            app.UseAuthentication();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            
        }
    }
}
