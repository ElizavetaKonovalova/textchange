using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TextBooks.Startup))]
namespace TextBooks
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
