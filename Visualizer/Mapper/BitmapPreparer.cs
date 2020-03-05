using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;

namespace Visualizer.Mapper {
    public class BitmapPreparer : IDisposable {
        private readonly int _height;
        private readonly PixelFormat _pf = PixelFormats.Rgb24;
        private readonly int _rawStride;
        private readonly int _width;
        [NotNull] private byte[] _pixelData;

        public BitmapPreparer(int width, int height)
        {
            _width = width;
            _height = height;
#pragma warning disable VSD0045 // The operands of a divisive expression are both integers and result in an implicit rounding.
            _rawStride = (width * _pf.BitsPerPixel + 7) / 8;
#pragma warning restore VSD0045 // The operands of a divisive expression are both integers and result in an implicit rounding.
            _pixelData = new byte[_rawStride * height];
            for (var i = 0; i < _pixelData.Length; i++) {
                _pixelData[i] = 255;
            }
        }

        public int Width => _width;

        public int Height => _height;

#pragma warning disable CC0029 // Disposables Should Call Suppress Finalize
#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            _pixelData = Array.Empty<byte>();
        }
#pragma warning restore CC0029 // Disposables Should Call Suppress Finalize

        [NotNull]
        public BitmapSource GetBitmap() => BitmapSource.Create(_width, _height, 96, 96, _pf, null, _pixelData, _rawStride);

        public void SetPixel(int x, int y, Color c)
        {
            var xIndex = x * 3;
            var yIndex = y * _rawStride;
            _pixelData[xIndex + yIndex] = c.R;
            _pixelData[xIndex + yIndex + 1] = c.G;
            _pixelData[xIndex + yIndex + 2] = c.B;
        }
    }
}