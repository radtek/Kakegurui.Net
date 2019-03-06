using System;
using System.Globalization;
using System.IO;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日志文件
    /// </summary>
    public class FileLogger : Logger
    {
        /// <summary>
        /// 文件名 
        /// </summary>
        protected readonly string _name;

        /// <summary>
        /// 日期 
        /// </summary>
        protected DateTime _date;

        /// <summary>
        /// 文件保存目录 
        /// </summary>
        protected readonly string _directory;

        /// <summary>
        /// 日志保存天数
        /// </summary>
        protected readonly int _holdDays;

        /// <summary>
        /// 文件流 
        /// </summary>
        private FileStream _fs;

        /// <summary>
        /// 文件流写入
        /// </summary>
        private StreamWriter _sw;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="filter">日志筛选接口</param>
        /// <param name="name">日志名称</param>
        public FileLogger(ILogFilter filter, string name)
            :base(filter)
        {
            _name = name;
            _date=new DateTime();
          
            //读取文件日志参数
            _directory = AppConfig.ReadString("log.file.directory") ?? "../log/";

            //创建日志目录
            Directory.CreateDirectory(_directory);

            int? holddays= AppConfig.ReadInt32("log.file.holddays");
            _holdDays = holddays ?? 0;

            //删除日志
            DeleteLog(_directory, _holdDays);

        }

        protected override void LogCore(string log)
        {
            if (_date != DateTime.Today)
            {
                _date = DateTime.Today;
                if (_sw != null && _fs != null)
                {
                    _sw.Close();
                    _fs.Close();
                }
                DeleteLog(_directory, _holdDays);
                _fs = new FileStream(
                    Path.Combine(_directory, string.Format("{0}_{1}.log", _name, _date.ToString("yyMMdd"))),
                    FileMode.Append,FileAccess.Write,FileShare.ReadWrite);
                _sw = new StreamWriter(_fs);
            }

            _sw.WriteLine(log);
            _sw.Flush();
        }

        /// <summary>
        /// 删除日志
        /// </summary>
        /// <param name="directory">目录</param>
        /// <param name="holdDays">日志保存天数</param>
        protected static void DeleteLog(string directory, int holdDays)
        {
            DateTime today=DateTime.Today;
            
            foreach (string filePath in Directory.GetFiles(directory))
            {
                string[] datas = Path.GetFileNameWithoutExtension(filePath)
                    .Split("_",StringSplitOptions.RemoveEmptyEntries);
                if (datas.Length >= 2)
                {
                    DateTime fileDate = DateTime.ParseExact(datas[1], "yyMMdd",
                        CultureInfo.CurrentCulture);
                    if ((today - fileDate).TotalDays >= holdDays)
                    {
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch (IOException)
                        {
                           
                        }
                    }
                }
            }
        }
    };
}
