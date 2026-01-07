using CityShob.ToDo.Server.Persistence;
using System.Web.Http;

namespace CityShob.ToDo.Server
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            // PRE-WARM: Force EF to build the model and check migrations now
            using (var context = new AppDbContext())
            {
                context.Database.Initialize(force: true);
            }
        }
    }
}
