using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    public class AutoBase
    {
        private readonly string _characterSet;
        private readonly int _base;

        public AutoBase(string characterSet)
        {
            if (characterSet.Length < 2)
                throw new ArgumentException("Character set must have at least 2 characters.");
            if (characterSet.Distinct().Count() != characterSet.Length)
                throw new ArgumentException("Character set contains duplicate characters.");

            _characterSet = characterSet;
            _base = characterSet.Length;
        }

        public string Encode(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            // 处理空数组的特殊情况
            if (data.Length == 1 && data[0] == 0)
                return _characterSet[0].ToString();

            // 将字节数组转换为大整数
            // 注意：BigInteger 构造函数期望的是小端序字节
            BigInteger number = new BigInteger(data.Reverse().ToArray());

            // 如果最高位是1，可能需要添加一个前导0字节以确保正数
            if (data[data.Length - 1] > 0x7F)
            {
                number = new BigInteger(data.Concat(new byte[] { 0 }).Reverse().ToArray());
            }

            // 转换为目标进制
            StringBuilder result = new StringBuilder();
            while (number > 0)
            {
                BigInteger remainder;
                number = BigInteger.DivRem(number, _base, out remainder);
                result.Insert(0, _characterSet[(int)remainder]);
            }

            return result.ToString();
        }

        public byte[] Decode(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
                return Array.Empty<byte>();

            // 将字符串转换回大整数
            BigInteger number = 0;
            foreach (char c in encoded)
            {
                int value = _characterSet.IndexOf(c);
                if (value < 0)
                    throw new ArgumentException($"Invalid character '{c}' in input.");

                number = number * _base + value;
            }

            // 将大整数转换回字节数组
            byte[] bytes = number.ToByteArray();

            // 反转字节顺序（BigInteger使用小端序，我们需要大端序）
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            // 移除可能的前导零字节
            if (bytes.Length > 1 && bytes[0] == 0)
            {
                bytes = bytes.Skip(1).ToArray();
            }

            return bytes;
        }
    }
}
