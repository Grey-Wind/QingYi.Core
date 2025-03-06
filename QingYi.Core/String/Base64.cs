using System;
using System.Text;

namespace QingYi.Core.String
{
    public static class Base64
    {
        private static readonly char[] s_base64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".ToCharArray();
        private static readonly int[] s_decodeTable = new int[256];

        static Base64()
        {
            for (int i = 0; i < 256; i++) s_decodeTable[i] = -1;
            for (int i = 0; i < s_base64Chars.Length; i++) s_decodeTable[s_base64Chars[i]] = i;
            s_decodeTable['='] = 0; // 特殊处理填充字符
        }

        // Base64编码（字符串→Base64字符串）
        public static string Encode(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return Encode(bytes);
        }

        // Base64编码（字节数组→Base64字符串）
        public static unsafe string Encode(byte[] bytes)
        {
            int inputLength = bytes.Length;
            int outputLength = (inputLength + 2) / 3 * 4;
            char[] output = new char[outputLength];

            fixed (byte* inputPtr = bytes)
            fixed (char* outputPtr = output)
            {
                byte* inPtr = inputPtr;
                char* outPtr = outputPtr;
                int remaining = inputLength;

                // 处理完整3字节组
                while (remaining >= 3)
                {
                    uint value = (uint)(*inPtr++) << 16;
                    value |= (uint)(*inPtr++) << 8;
                    value |= *inPtr++;

                    *outPtr++ = s_base64Chars[(value >> 18) & 0x3F];
                    *outPtr++ = s_base64Chars[(value >> 12) & 0x3F];
                    *outPtr++ = s_base64Chars[(value >> 6) & 0x3F];
                    *outPtr++ = s_base64Chars[value & 0x3F];
                    remaining -= 3;
                }

                // 处理剩余字节
                if (remaining > 0)
                {
                    uint value = (uint)(*inPtr++) << 16;
                    if (remaining == 2) value |= (uint)(*inPtr++) << 8;

                    *outPtr++ = s_base64Chars[(value >> 18) & 0x3F];
                    *outPtr++ = s_base64Chars[(value >> 12) & 0x3F];
                    *outPtr++ = remaining == 2 ? s_base64Chars[(value >> 6) & 0x3F] : '=';
                    *outPtr++ = '=';
                }
            }

            return new string(output);
        }

        // Base64解码（Base64字符串→字符串）
        public static string Decode(string base64Text)
        {
            byte[] bytes = DecodeToBytes(base64Text);
            return Encoding.UTF8.GetString(bytes);
        }

        // Base64解码（Base64字符串→字节数组）
        public static unsafe byte[] DecodeToBytes(string base64Text)
        {
            int inputLength = base64Text.Length;
            char[] cleaned = new char[inputLength];
            int cleanLength = 0;
            int padding = 0;
            bool hasPadding = false;

            // 预处理：过滤无效字符并验证格式
            fixed (char* inputPtr = base64Text)
            {
                char* ptr = inputPtr;
                for (int i = 0; i < inputLength; i++)
                {
                    char c = *ptr++;
                    if (c == '=')
                    {
                        if (hasPadding) padding++;
                        cleaned[cleanLength++] = c;
                        hasPadding = true;
                    }
                    else if (s_decodeTable[c] != -1)
                    {
                        if (hasPadding) throw new FormatException("Invalid padding position");
                        cleaned[cleanLength++] = c;
                    }
                }
            }

            // 验证长度和填充
            if (cleanLength % 4 != 0) throw new FormatException("Invalid base64 length");
            if (padding > 2) throw new FormatException("Too many padding characters");

            // 计算输出长度
            int outputLength = cleanLength * 3 / 4 - padding;
            byte[] output = new byte[outputLength];

            fixed (char* inputPtr = cleaned)
            fixed (byte* outputPtr = output)
            {
                char* inPtr = inputPtr;
                byte* outPtr = outputPtr;
                int remaining = cleanLength;

                // 处理完整4字符组
                while (remaining >= 4)
                {
                    int a = s_decodeTable[*inPtr++];
                    int b = s_decodeTable[*inPtr++];
                    int c = s_decodeTable[*inPtr++];
                    int d = s_decodeTable[*inPtr++];

                    uint value = (uint)a << 18 | (uint)b << 12 | (uint)c << 6 | (uint)d;
                    *outPtr++ = (byte)(value >> 16);

                    if (*inPtr != '=')  // 非填充组
                    {
                        *outPtr++ = (byte)(value >> 8);
                        *outPtr++ = (byte)value;
                    }
                    else if (remaining == 4 && c == 0)  // 1字节填充
                    {
                        if (d != 0) throw new FormatException("Invalid padding");
                    }
                    else if (remaining == 4)  // 2字节填充
                    {
                        *outPtr++ = (byte)(value >> 8);
                    }

                    remaining -= 4;
                }
            }

            return output;
        }
    }
}
