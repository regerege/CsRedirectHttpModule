using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Text;

namespace CsRedirectHttpModule
{
    public class RedirectHttpModule : IHttpModule
    {
        private static string ConfigFileName = "redirects.xml";
        private Dictionary<string, Dictionary<string, string>> _dic = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// 静的コンストラクタ
        /// </summary>
        static RedirectHttpModule()
        {
            // トレース出力はすべてイベントログに書き込むようにする。
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new EventLogTraceListener("Application"));
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="context"></param>
        public void Init(HttpApplication context)
        {
            var root = System.Reflection.Assembly.GetExecutingAssembly().Location;
            root = Path.GetDirectoryName(root);
            var path = Path.Combine(root, ConfigFileName);

#if DEBUG
            path = @"D:\develop\github\CsRedirectHttpModule\CsRedirectHttpModule\redirects.xml";
#endif

            if (File.Exists(path))
            {
                this.ReadConfig(path);
                //this.Redirect(context);
                context.BeginRequest += new EventHandler(context_BeginRequest);
            }
            else
            {
                Trace.WriteLine(string.Format("{0} を読み込めませんでした。", path));
            }
        }

        void context_BeginRequest(object sender, EventArgs e)
        {
            var context = (HttpApplication)sender;
            var langs = context.Request.UserLanguages;
            var sb = new StringBuilder();

#if DEBUG
            foreach (var lang in langs)
            {
                sb.AppendLine(lang);
            }
            Trace.WriteLine(sb.ToString());
#endif
            var target = context.Request.FilePath;

            if (_dic.ContainsKey(target))
            {
                if (langs != null && 1 <= langs.Length)
                {
                    var url = "";

                    if (_dic[target].TryGetValue(langs[0], out url))
                    {
                        try
                        {
                            context.Response.Redirect(url);
                        }
                        catch { }
                    }
                }
            }
        }

        /// <summary>
        /// redirects.xml を読み込む
        /// </summary>
        /// <param name="path">設定ファイルの保存先</param>
        private void ReadConfig(string path)
        {
            try
            {
                var xml = XElement.Load(path);
                var query =
                    from r in xml.Elements("Redirect")
                    select new
                    {
                        Pattern = (string)r.Attribute("pattern"),
                        Target = (string)r.Attribute("target"),
                        Redirect = (string)r.Attribute("redirect")
                    };
                query = query.Distinct();

                foreach (var item in query)
                {
                    Dictionary<string, string> map = null;
                    if(_dic.ContainsKey(item.Target)) {
                        map = _dic[item.Target];
                    }
                    else {
                        map = new Dictionary<string,string>();
                        _dic.Add(item.Target, map);
                    }

                    if (map.ContainsKey(item.Pattern))
                    {
                        map[item.Pattern] = item.Redirect;
                    }
                    else
                    {
                        map.Add(item.Pattern, item.Redirect);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
        }
    }
}
