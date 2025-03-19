using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Service.Rfid.Impl
{
    public class ServiceImpl : IService
    {
        static ILog logger = LogManager.GetLogger(typeof(ServiceImpl));
        const string LOG_TITLE = "Rfid";

        bool isStart = false;
        HttpHandle httpHandle = new HttpHandle();

        public void Start()
        {
            try
            {
                if (isStart)
                {
                    logger.Info($"{LOG_TITLE}服务【Start】已启动，不要重试！");
                    return;
                }
                logger.Info($"{LOG_TITLE}服务【Start】启动中...");
                // 启动监听器
                httpHandle.Start();
                isStart = true;

                // 处理传入的请求
                ThreadPool.QueueUserWorkItem(httpHandle.Listen);
                logger.Info($"{LOG_TITLE}服务【Start】启动完成");
            }
            catch (Exception ex)
            {
                logger.Info($"{LOG_TITLE}服务【Start】线程异常：" + ex.ToString());
            }
        }


        public void Stop()
        {
            try
            {
                if (isStart)
                {
                    logger.Info($"{LOG_TITLE}服务【Stop】终止...");

                    httpHandle.Close();

                    isStart = false;
                    logger.Info($"{LOG_TITLE}服务【Stop】终止完成.");
                }
            }
            catch (ThreadAbortException ex)
            {
                logger.Info($"{LOG_TITLE}服务【Stop】ThreadAbortException：" + ex.ToString());
            }
            catch (Exception ex)
            {
                logger.Info($"{LOG_TITLE}服务【Stop】系统异常：" + ex.ToString());
            }
        }
    }
}
