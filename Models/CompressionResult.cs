namespace FileCompressorApp.Models
{
    public class CompressionResult
    {
        public string FileName { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio => OriginalSize == 0 ? 0 : 1.0 - ((double)CompressedSize / OriginalSize);
        public string AlgorithmUsed { get; set; }
        public string Error { get; set; } 
    }
}
