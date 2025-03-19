using Service.Rfid.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Service.Rfid
{
    class Program
    {
        static void Main(string[] args)
        {
            // 使用 Topshelf 进行服务配置
            HostFactory.Run(configure =>
            {
                configure.Service<IService>(service =>
                {
                    service.ConstructUsing(s => new ServiceImpl());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });
                // 使用 LocalSystem 账户运行服务,高权限账户，拥有系统级别的权限
                 configure.RunAsLocalSystem();

                //使用 LocalService 身份运行服务时，服务将以较低的权限运行，并且对系统资源的访问将受到限制
                //configure.RunAsLocalService();

                configure.SetServiceName("Rfid.Service");
                configure.SetDisplayName("RfidReader服务");
                configure.SetDescription("RfidReader服务");
            });
        }
    }
}
