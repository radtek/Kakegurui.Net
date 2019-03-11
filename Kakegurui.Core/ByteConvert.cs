using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Kakegurui.Core
{
    /// <summary>
    /// 字节和文本转换
    /// </summary>
    public static class ByteConvert
    {
        /// <summary>
        /// 字节流转十六进制字符串
        /// </summary>
        /// <param name="buffer">字节流</param>
        /// <returns>十六进制字符串</returns>
        public static string ToHex(byte[] buffer)
        {
            if (buffer == null)
            {
                return string.Empty;
            }
            StringBuilder builder = new StringBuilder();
            foreach (byte b in buffer)
            {
                builder.AppendFormat("{0:x2} ", b);
            }
            return builder.ToString();
        }

        /// <summary>
        /// 字节流转十六进制字符串
        /// </summary>
        /// <param name="buffer">字节流</param>
        /// <param name="size">要转换的长度</param>
        /// <returns>十六进制字符串</returns>
        public static string ToHex(byte[] buffer, int size)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < size; ++i)
            {
                builder.AppendFormat("{0:x2} ", buffer[i]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 十六进制字符串转字节流
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator">分隔符，默认为" "</param>
        /// <returns>字节流</returns>
        public static List<byte> ToBuffer(string value,string separator=" ")
        {
            return value.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(data => byte.Parse(data, NumberStyles.HexNumber)).ToList();
        }

        /// <summary>
        /// 字节列表的异或结果
        /// </summary>
        /// <param name="datas">字节列表</param>
        /// <param name="startIndex">起始序号</param>
        /// <param name="count">操作字节的个数</param>
        /// <returns>异或的结果</returns>
        public static byte Xor(List<byte> datas, int startIndex, int count)
        {
            //先取出第一位然后一次异或
            byte result = datas[startIndex];
            for (int i = startIndex + 1; i < count; i++)
            {
                result = Convert.ToByte(result ^ datas[i]);
            }
            return result;
        }
    }
}
