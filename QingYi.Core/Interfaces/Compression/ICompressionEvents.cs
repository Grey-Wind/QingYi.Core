using QingYi.Core.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QingYi.Core.Interfaces.Compression
{
    /// <summary>
    /// 压缩事件接口
    /// </summary>
    public interface ICompressionEvents
    {
        event EventHandler<CompressionEventArgs>? CompressionStarted;
        event EventHandler<CompressionEventArgs>? CompressionCompleted;
        event EventHandler<CompressionEventArgs>? CompressionFailed;

        /// <summary>
        /// 进度报告
        /// </summary>
        event EventHandler<CompressionProgressEventArgs>? ProgressChanged;
    }
}
