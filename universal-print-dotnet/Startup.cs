using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(universal_print.Startup))]

namespace universal_print
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}