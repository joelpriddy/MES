using QRCoder;

namespace PA.Barcode
{
    public static class QrGenerator
    {
        /// <summary>
        /// Generates an SVG QR code for the given payload.
        /// </summary>
        public static string GenerateSvg(string payload, int pixelsPerModule = 6)
        {
            using var generator = new QRCodeGenerator();
            using QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var svg = new SvgQRCode(data);

            // 1 == module size inside the SVG; control overall render with CSS/width/height.
            return svg.GetGraphic(1);
        }
    }
}
