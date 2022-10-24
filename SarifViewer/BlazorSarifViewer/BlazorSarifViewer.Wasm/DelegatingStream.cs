using System.IO;

namespace BlazorSarifViewer.Wasm
{
    public class DelegatingStream : Stream
    {
        private readonly Stream stream;

        /// <summary>
        /// This is a wrapper for a stream that prevents the Sarif SDK from repeatedly disposing the stream so it may be reused.
        /// </summary>
        /// <param name="underlyingStream"></param>
        public DelegatingStream(Stream underlyingStream)
        {
            stream = underlyingStream;
            underlyingStream.Seek(0L, SeekOrigin.Begin);
        }
        public override bool CanRead => stream.CanRead;
        public override bool CanSeek => stream.CanSeek;
        public override bool CanWrite => stream.CanWrite;
        public override long Length => stream.Length;
        public override long Position
        {
            set
            {
                stream.Position = value;
            }
            get => stream.Position;
        }
        public override void Flush()
        {
            stream.Flush();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }
        public void DisposeUnderlyingStream()
        {
            stream.Dispose();
        }
    }
}
