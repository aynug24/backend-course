using BadNews.ModelBuilders.News;
using BadNews.Repositories.News;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BadNews
{
    public class Startup
    {
        private readonly IWebHostEnvironment env;
        private readonly IConfiguration configuration;

        // В конструкторе уже доступна информация об окружении и конфигурация
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            this.env = env;
            this.configuration = configuration;
        }

        // В этом методе добавляются сервисы в DI-контейнер
        public void ConfigureServices(IServiceCollection services)
        {
            var mvcBuilder = services.AddControllersWithViews();
            if (env.IsDevelopment())
                mvcBuilder.AddRazorRuntimeCompilation();

            services.AddSingleton<INewsRepository, NewsRepository>();
            services.AddSingleton<INewsModelBuilder, NewsModelBuilder>();
        }

        // В этом методе конфигурируется последовательность обработки HTTP-запроса
        public void Configure(IApplicationBuilder app)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Errors/Exception");

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSerilogRequestLogging();

            app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");

            //app.Map("/news/fullarticle", fullArticleApp =>
            //{
            //    fullArticleApp.Run(RenderFullArticlePage);
            //});

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("status-code", "StatusCode/{code?}", new
                {
                    controller = "Errors",
                    action = "StatusCode"
                });
                endpoints.MapControllerRoute("default", "{controller=News}/{action=Index}/{id?}");
            });
        }
    }
}
