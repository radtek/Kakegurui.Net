using System;
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
        private readonly string _name;

        /// <summary>
        /// 日期 
        /// </summary>
        private DateTime _date;

        /// <summary>
        /// 文件保存目录 
        /// </summary>
        private readonly string _directory;

        /// <summary>
        /// 日志保存天数
        /// </summary>
        private readonly int _holdDays;

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
            _date= DateTime.Today;

            //读取文件日志参数
            _directory = AppConfig.ReadString("log.file.directory") ?? "../log/";
            _holdDays = AppConfig.ReadInt32("log.file.holddays") ?? 0;

            _fs = new FileStream(
                Path.Combine(_directory, string.Format("{0}_{1}.log", _name, _date.ToString("yyMMdd"))),
                FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _sw = new StreamWriter(_fs);

        }

        /// <summary>
        /// 根据当前日期和日志保存时间删除过期的日志
        /// </summary>
        private void DeleteFile()
        {
            DateTime date = DateTime.Today.AddDays(-_holdDays);
            try
            {
                File.Delete(Path.Combine(_directory, string.Format("{0}_{1}.log", "System", date.ToString("yyMMdd"))));
            }
            catch (IOException)
            {

            }
        }

        protected override void LogCore(string log)
        {
            if (_date != DateTime.Today)
            {
                if (_sw != null && _fs != null)
                {
                    _sw.Close();
                    _fs.Close();
                }

                DeleteFile();

                _date = DateTime.Today;
                _fs = new FileStream(
                    Path.Combine(_directory, string.Format("{0}_{1}.log", _name, _date.ToString("yyMMdd"))),
                    FileMode.Append,FileAccess.Write,FileShare.ReadWrite);
                _sw = new StreamWriter(_fs);
            }
            _sw.WriteLine(log);
            _sw.Flush();
        }

    };
}
