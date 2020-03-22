using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;


namespace TestInsect.Controllers
{
    
    public class CrawlerController 
    {       

        private static string website = "https://www.jupindai.com";

        
        public void Index1()
        {
            Stopwatch watch = new Stopwatch();
            //watch.Start();  //开始监视代码运行时间

            //抓取整本小说
            CrawlerController cra = new CrawlerController();// 笔趣阁抓取小说网站小说
            string html = cra.HttpGet("https://www.jupindai.com/book/100.html", "");

            // 获取小说名字
            Match ma_name = Regex.Match(html, "(?<=meta property=\"og:title\" content=\").*?(?=\")");
            string name = ma_name.Value;

            // 获取章节目录
            Match reg_mulu = Regex.Match(html, "<div class=\"panel panel-default\" id=\"list-chapterAll\">[\\s\\S]*(?=(<div class=\"panel panel-default hidden-xs\">))");
          

            MatchCollection mat_mulu2 = Regex.Matches(reg_mulu.Value, "<a[^>]+?href=\"([^ \"]+)\"[^>]*>([^<]+)<\\/a>");
            if (mat_mulu2.Count != 0)
            {
                ThreadPool.SetMaxThreads(30, 30);
                for (int i=0;i< mat_mulu2.Count; i++)
                {
                    //获取章节
                    string chapters = mat_mulu2[i].Groups[2].Value;
                    //获取章节内容链接
                    string contenthref = website+mat_mulu2[i].Groups[1].Value;
                    //获取章节文本html
                    string chaptershtml = cra.HttpGet(contenthref,"");
                    //获取文本内容
                    Match htmlmatch = Regex.Match(chaptershtml, "<div class=\"panel-body\" id=\"htmlContent\">[\\s\\S]*?<\\/div>");
                    string content = htmlmatch.Value.ToString().Replace("<div class=\"panel-body\" id=\"htmlContent\">","").Replace("</div>","").Replace("&nbsp;", "").Replace("<br />", "");
                    // txt文本输出
                    string path = AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/") + name+"/";
                    string tempcontent = chapters + "\r\n" + content;
                    ThreadWithState tws = new ThreadWithState(chapters,content, path);

                    //线程池
                    //ThreadPool.QueueUserWorkItem(new WaitCallback(tws.ThreadProc2));
                    //Thread.Sleep(1000);

                    //创建执行任务的线程，并执行
                   Thread t = new Thread(new ThreadStart(tws.ThreadProc));
                    t.Start();

                    //Novel(chapters + "\r\n" + content, name, path);

                }
            }

            watch.Stop();  //停止监视
            TimeSpan timespan = watch.Elapsed;  //获取当前实例测量得出的总时间
           Debug.WriteLine("打开窗口代码执行时间：{0}(毫秒)", timespan.TotalMilliseconds);  //总毫秒数


        }



        /// <summary>
        /// 创建文本
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="name">名字</param>
        /// <param name="path">路径</param>
        /// 
       
        public  void Novel(string content, string name, string path)
        {
            string Log = content + "\r\n";
            // 创建文件夹，如果不存在就创建file文件夹
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }

            // 判断文件是否存在，不存在则创建
            if (!System.IO.File.Exists(path + name + ".txt"))
            {
                //ReaderWriterLockSlim LogWriteLock = new ReaderWriterLockSlim();

                //LogWriteLock.EnterWriteLock();
                FileStream fs1 = new FileStream(path + name + ".txt", FileMode.Create, FileAccess.Write, FileShare.Write);// 创建写入文件 
                
                StreamWriter sw = new StreamWriter(fs1);
                sw.WriteLine(Log);// 开始写入值
                                  //Thread.Sleep(5000);
                //LogWriteLock.ExitWriteLock();            
                fs1.Flush();//清除缓冲区
                sw.Close();
                fs1.Close();//关闭
                fs1.Dispose();//释放
            }
            else
            {
                FileStream fs = new FileStream(path + name + ".txt" + "", FileMode.Append, FileAccess.Write);
                StreamWriter sr = new StreamWriter(fs);
                sr.WriteLine(Log);// 开始写入值                
                fs.Flush();//清除缓冲区
                sr.Close();
                fs.Close();//关闭
                fs.Dispose();//释放
            }
        }

        public string HttpPost(string Url, string postDataStr)
        {
            CookieContainer cookie = new CookieContainer();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = Encoding.UTF8.GetByteCount(postDataStr);
            request.CookieContainer = cookie;
            Stream myRequestStream = request.GetRequestStream();
            StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
            myStreamWriter.Write(postDataStr);
            myStreamWriter.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            response.Cookies = cookie.GetCookies(response.ResponseUri);
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        public string HttpGet(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            HttpWebResponse response;
            request.ContentType = "text/html;charset=UTF-8";
            try
            {
                response = (HttpWebResponse)request.GetResponse();

            }
            catch (WebException ex)
            {
                response = (HttpWebResponse)request.GetResponse();
            }

            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.Default);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        

    }
    public class ThreadWithState
    {
        //要用到的属性，也就是我们要传递的参数
        private string cheapter,content, path;
        //包含参数的构造函数
        public ThreadWithState(string tempcheapter,string tempcontent, string temppath)
        {
            cheapter = tempcheapter;
            content = tempcontent;
            //name = tempname;
            path = temppath;
        }
        //要丢给线程执行的方法，本处无返回类型就是为了能让ThreadStart来调用
        public void ThreadProc()
        {
            lock (this)
            {
                string Log = content + "\r\n";
                // 创建文件夹，如果不存在就创建file文件夹
                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }

                // 判断文件是否存在，不存在则创建
                if (!System.IO.File.Exists(path + cheapter + ".txt"))
                {

                    FileStream fs1 = new FileStream(path + cheapter + ".txt", FileMode.Create, FileAccess.Write, FileShare.Write);// 创建写入文件 

                    StreamWriter sw = new StreamWriter(fs1);
                    sw.WriteLine(Log);// 开始写入值                                           
                    fs1.Flush();//清除缓冲区
                    sw.Close();
                    fs1.Close();//关闭
                    fs1.Dispose();//释放
                }
                else
                {
                    FileStream fs = new FileStream(path + cheapter + ".txt" + "", FileMode.Append, FileAccess.Write);
                    StreamWriter sr = new StreamWriter(fs);
                    sr.WriteLine(Log);// 开始写入值                
                    fs.Flush();//清除缓冲区
                    sr.Close();
                    fs.Close();//关闭
                    fs.Dispose();//释放
                }
            }

            Thread.Sleep(1000);
        }
        
    }
    public class Program
    {
        static void Main()
        {
            CrawlerController crawler = new CrawlerController();
            crawler.Index1();
        }
    }
}