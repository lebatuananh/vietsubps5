using System.Text;
using Engine.Capture;

namespace Engine;

public static class Program
{
    private const string Usage = """
        Dùng: dotnet run --project engine/src/Engine -- <lệnh> [tham số]   (chạy từ root repo)
          capture   --window-title <chuỗi con> | --hwnd <số|0xHEX>  --out <thư mục>  [--frames N=1] [--interval-ms 500] [--timeout-ms 5000] [--no-crop true  (chẩn đoán: giữ cả thanh tiêu đề)]
          ocr       --input <thư mục PNG> --out <thư mục>  [--scale 1|2]
          ocr-score --ocr <thư mục *.ocr.json> --golden <thư mục *.golden.json>
        Mã thoát: 0 ok · 1 tham số/ dữ liệu · 2 cửa sổ không tìm thấy/ mơ hồ · 3 cửa sổ thu nhỏ · 4 không có khung (timeout) · 5 thiếu OCR en-US · 6 lỗi hệ thống/ interop
        """;

    public static async Task<int> Main(string[] args)
    {
        // DÒNG ĐẦU: khai DPI PerMonitorV2 trước mọi lệnh gọi toạ độ cửa sổ (laptop 2880×1800 scale > 100%).
        // Manifest nhúng trong apphost có thể không có hiệu lực khi chạy qua `dotnet run` ⇒ gọi trong code.
        bool dpiSet = NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.PerMonitorAwareV2);
        bool dpiOk = NativeMethods.AreDpiAwarenessContextsEqual(
            NativeMethods.GetThreadDpiAwarenessContext(), NativeMethods.PerMonitorAwareV2);

        Console.OutputEncoding = new UTF8Encoding(false);
        Console.WriteLine($"[engine] DPI PerMonitorV2: set={dpiSet} hiệu lực={dpiOk}");

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine(Usage);
            return args.Length == 0 ? 1 : 0;
        }

        try
        {
            var a = Args.Parse(args, 1);
            return args[0] switch
            {
                "capture" => await Commands.CaptureAsync(a, dpiOk),
                "ocr" => await Commands.OcrAsync(a),
                "ocr-score" => await Commands.OcrScoreAsync(a),
                _ => throw new ArgumentException($"Lệnh lạ '{args[0]}'.\n{Usage}"),
            };
        }
        catch (EngineException ex)
        {
            Console.Error.WriteLine($"[engine] LỖI ({ex.ExitCode}): {ex.Message}");
            return ex.ExitCode;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"[engine] LỖI (1): {ex.Message}");
            return 1;
        }
    }
}
