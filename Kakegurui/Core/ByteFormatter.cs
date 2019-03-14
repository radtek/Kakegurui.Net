using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 可序列化的字段
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class SerializeIndexAttribute : Attribute
    {
        /// <summary>
        /// 序列化顺序
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="index">序列化顺序</param>
        public SerializeIndexAttribute(int index)
        {
            Index = index;
        }
    }

    /// <summary>
    /// 字节流序列化
    /// </summary>
    public static class ByteFormatter
    {
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="obj">要序列化的实例</param>
        /// <returns>字节流</returns>
        public static List<byte> Serialize(object obj)
        {
            List<byte> buffer=new List<byte>();
            foreach (var property in obj.GetType().GetProperties().Where(p => p.GetCustomAttributesData().Count != 0).OrderBy(p => p.GetCustomAttributesData()[0].ConstructorArguments[0].Value))
            {
                ToBytes(property.GetValue(obj), buffer);
            }
            return buffer;
        }

        /// <summary>
        /// 实例转字节流
        /// </summary>
        /// <param name="value">实例</param>
        /// <param name="buffer">字节流，转换后的字节流会添加到改列表中</param>
        private static void ToBytes(object value, List<byte> buffer)
        {
            if (value.GetType().IsArray)
            {
                Array array = (Array)value;
                ToBytes(Convert.ToUInt16(array.Length), buffer);
                foreach (var item in array)
                {
                    ToBytes(item, buffer);
                }
                return;
            }

            if (value.GetType().FullName.StartsWith("System.Collections.Generic.List"))
            {
                System.Collections.IList list = (System.Collections.IList)value;
                ToBytes(Convert.ToUInt16(list.Count), buffer);
                foreach (var item in list)
                {
                    ToBytes(item, buffer);
                }
                return;
            }

            if (value is string s)
            {
                buffer.AddRange(Encoding.UTF8.GetBytes(s));
                buffer.Add(0);
                return;
            }

            byte[] temp;
            if (value is byte b1)
            {
                temp = new[] { b1 };
            }
            else if (value is sbyte b2)
            {
                temp = new[] { Convert.ToByte(b2) };
            }
            else if (value is short s1)
            {
                temp = BitConverter.GetBytes(s1);
            }
            else if (value is ushort s2)
            {
                temp = BitConverter.GetBytes(s2);
            }
            else if (value is int i1)
            {
                temp = BitConverter.GetBytes(i1);
            }
            else if (value is uint i2)
            {
                temp = BitConverter.GetBytes(i2);
            }
            else if (value is long l1)
            {
                temp = BitConverter.GetBytes(l1);
            }
            else if (value is ulong l2)
            {
                temp = BitConverter.GetBytes(l2);
            }
            else if (value is float f)
            {
                temp = BitConverter.GetBytes(f);
            }
            else if (value is double d)
            {
                temp = BitConverter.GetBytes(d);
            }
            else
            {
                buffer.AddRange(Serialize(value));
                return;
            }
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(temp);
            }
            buffer.AddRange(temp);
        }

        /// <summary>
        /// 反序列化实例
        /// </summary>
        /// <param name="value">实例</param>
        /// <param name="buffer">字节流</param>
        /// <param name="offset">偏移量，默认为0</param>
        /// <returns>字节流长度</returns>
        public static int Deserialize(object value, List<byte> buffer,int offset=0)
        {
            try
            {
                int size = 0;
                foreach (var property in value.GetType().GetProperties().Where(p => p.GetCustomAttributesData().Count != 0).OrderBy(p1 => p1.GetCustomAttributesData()[0].ConstructorArguments[0].Value))
                {
                    if (property.CanWrite)
                    {
                        var t = ToValue(property.PropertyType, buffer, offset + size);
                        property.SetValue(value, t.Item1);
                        size += t.Item2;
                    }
                }
                return size;
            }
            catch (Exception e)
            {
                LogPool.Logger.LogError(e,"{0} {1}", offset, ByteConvert.ToHex(buffer?.ToArray()));
                return 0;
            }
           
        }

        /// <summary>
        /// 反序列化指定类型
        /// </summary>
        /// <param name="type">实例类型</param>
        /// <param name="buffer">字节流</param>
        /// <param name="offset">偏移量</param>
        /// <returns>第一个字段表示反序列化结果，第二个字段表示数据长度</returns>
        private static Tuple<object,int> ToValue(Type type,List<byte> buffer,int offset)
        {
            object value;

            if (type.IsArray)
            {
                var lt= ToValue(typeof(ushort), buffer, offset);
                ushort length = (ushort)lt.Item1;
                Array array = Array.CreateInstance(type.GetElementType(), length);
                int size = lt.Item2;
                for (int i = 0; i < length; ++i)
                {
                    var it = ToValue(type.GetElementType(), buffer, offset+size);
                    array.SetValue(it.Item1, i);
                    size += it.Item2;
                }
                return new Tuple<object, int>(array,size);
            }

            if (type.FullName.StartsWith("System.Collections.Generic.List"))
            {
                var lt= ToValue(typeof(ushort), buffer, offset);
                ushort length = (ushort) lt.Item1;
                int size = lt.Item2;
                System.Collections.IList list =(System.Collections.IList)type.GetConstructor(new Type[] { }).Invoke(new object[]{});
                for (int i = 0; i < length; ++i)
                {
                    var it=ToValue(type.GetGenericArguments()[0], buffer, offset + size);
                    list.Add(it.Item1);
                    size += it.Item2;
                }
                return new Tuple<object, int>(list,size);
            }

            if (type == typeof(string))
            {
                int index = buffer.FindIndex(offset, b => b == 0);
                string str=Encoding.UTF8.GetString(buffer.GetRange(offset, index - offset).ToArray());
                return new Tuple<object, int>(str,str.Length+1);
            }

            if (type == typeof(byte))
            {
                value= buffer[offset];
            }
            else if (type == typeof(sbyte))
            {
                value= Convert.ToSByte(buffer[offset]);
            }
            else if (type == typeof(short))
            {
                byte[] temp = buffer.GetRange(offset, 2).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(temp);
                }
                value= BitConverter.ToInt16(temp, 0);
            }
            else if (type == typeof(ushort))
            {
                byte[] temp = buffer.GetRange(offset, 2).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(temp);
                }
                value= BitConverter.ToUInt16(temp, 0);
            }
            else if (type == typeof(int))
            {
                byte[] temp = buffer.GetRange(offset, 4).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(temp);
                }
                value= BitConverter.ToInt32(temp, 0);
            }
            else if (type == typeof(uint))
            {
                byte[] temp = buffer.GetRange(offset, 4).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(temp);
                }
                value= BitConverter.ToUInt32(temp, 0);
            }
            else if (type == typeof(long))
            {
                byte[] temp = buffer.GetRange(offset, 8).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(temp);
                }
                value= BitConverter.ToInt64(temp, 0);
            }
            else if (type == typeof(ulong))
            {
                byte[] temp = buffer.GetRange(offset, 8).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(temp);
                }
                value= BitConverter.ToUInt64(temp, 0);
            }
            else if (type == typeof(float))
            {
                byte[] temp = buffer.GetRange(offset, 4).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(temp);
                }
                value = BitConverter.ToSingle(temp, 0);
            }
            else if (type == typeof(double))
            {
                byte[] temp = buffer.GetRange(offset, 8).ToArray();
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(temp);
                }
                value = BitConverter.ToDouble(temp, 0);
            }
            else
            {
                value = type.GetConstructor(new Type[] { }).Invoke(new object[] { });
                int size=Deserialize(value, buffer,offset);
                return new Tuple<object, int>(value,size);
            }

            return new Tuple<object, int>(value, Marshal.SizeOf(type));
        }
    }
}
