using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(RechargeTools.Startup))]

namespace RechargeTools
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}