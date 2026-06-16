using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SystemHub.Services
{
    public static class WallpaperService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint timeout, out IntPtr result);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string windowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static Process? _currentWallpaperProcess;
        private static string? _currentTempDir;

        public static async Task ApplyBuiltInWallpaper(string type)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SystemHubWallpaperBuiltIn");
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
            Directory.CreateDirectory(tempDir);

            string htmlPath = Path.Combine(tempDir, "wallpaper.html");
            string htmlContent = "";

            if (type == "matrix")
            {
                htmlContent = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""UTF-8"">
<style>
  html, body { margin: 0; padding: 0; overflow: hidden; background: #000; width: 100%; height: 100%; }
  canvas { display: block; }
</style>
</head>
<body>
  <canvas id=""canvas""></canvas>
  <script>
    const canvas = document.getElementById('canvas');
    const ctx = canvas.getContext('2d');
    function resize() {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    }
    window.addEventListener('resize', resize);
    resize();
    const katakana = 'ｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜﾝ1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    const alphabet = katakana.split('');
    const fontSize = 16;
    let columns = Math.floor(canvas.width / fontSize);
    let rainDrops = Array(columns).fill(1);
    window.addEventListener('resize', () => {
      columns = Math.floor(canvas.width / fontSize);
      rainDrops = Array(columns).fill(1);
    });
    function draw() {
      ctx.fillStyle = 'rgba(0, 0, 0, 0.05)';
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      ctx.fillStyle = '#0F0';
      ctx.font = fontSize + 'px monospace';
      for(let i = 0; i < rainDrops.length; i++) {
        const text = alphabet[Math.floor(Math.random() * alphabet.length)];
        ctx.fillText(text, i * fontSize, rainDrops[i] * fontSize);
        if(rainDrops[i] * fontSize > canvas.height && Math.random() > 0.975) {
          rainDrops[i] = 0;
        }
        rainDrops[i]++;
      }
    }
    setInterval(draw, 30);
  </script>
</body>
</html>";
            }
            else if (type == "starfield")
            {
                htmlContent = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""UTF-8"">
<style>
  html, body { margin: 0; padding: 0; overflow: hidden; background: #000; width: 100%; height: 100%; }
  canvas { display: block; }
</style>
</head>
<body>
  <canvas id=""canvas""></canvas>
  <script>
    const canvas = document.getElementById('canvas');
    const ctx = canvas.getContext('2d');
    function resize() {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    }
    window.addEventListener('resize', resize);
    resize();
    const numStars = 800;
    const stars = [];
    for (let i = 0; i < numStars; i++) {
      stars.push({
        x: (Math.random() - 0.5) * canvas.width * 2,
        y: (Math.random() - 0.5) * canvas.height * 2,
        z: Math.random() * canvas.width,
        color: `hsl(${Math.random() * 360}, 80%, 80%)`
      });
    }
    function draw() {
      ctx.fillStyle = 'rgba(0, 0, 0, 0.1)';
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      const cx = canvas.width / 2;
      const cy = canvas.height / 2;
      for (let i = 0; i < numStars; i++) {
        const star = stars[i];
        star.z -= 4;
        if (star.z <= 0) {
          star.x = (Math.random() - 0.5) * canvas.width * 2;
          star.y = (Math.random() - 0.5) * canvas.height * 2;
          star.z = canvas.width;
        }
        const px = (star.x / star.z) * cx + cx;
        const py = (star.y / star.z) * cy + cy;
        if (px < 0 || px > canvas.width || py < 0 || py > canvas.height) {
          continue;
        }
        const size = (1 - star.z / canvas.width) * 4;
        ctx.fillStyle = star.color;
        ctx.beginPath();
        ctx.arc(px, py, size, 0, Math.PI * 2);
        ctx.fill();
      }
      requestAnimationFrame(draw);
    }
    draw();
  </script>
</body>
</html>";
            }
            else // gradient
            {
                htmlContent = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""UTF-8"">
<style>
  html, body { margin: 0; padding: 0; overflow: hidden; background: #000; width: 100%; height: 100%; }
  canvas { display: block; filter: blur(40px); opacity: 0.85; width: 100%; height: 100%; }
</style>
</head>
<body>
  <canvas id=""canvas""></canvas>
  <script>
    const canvas = document.getElementById('canvas');
    const ctx = canvas.getContext('2d');
    function resize() {
      canvas.width = window.innerWidth / 4;
      canvas.height = window.innerHeight / 4;
    }
    window.addEventListener('resize', resize);
    resize();
    let time = 0;
    function draw() {
      time += 0.005;
      ctx.fillStyle = '#1e0f3c';
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      const blobs = [
        {
          x: canvas.width / 2 + Math.sin(time) * canvas.width / 3,
          y: canvas.height / 2 + Math.cos(time * 1.3) * canvas.height / 3,
          radius: canvas.width / 2,
          color1: '#007AFF',
          color2: '#00000000'
        },
        {
          x: canvas.width / 2 + Math.cos(time * 1.5) * canvas.width / 4,
          y: canvas.height / 2 + Math.sin(time * 0.8) * canvas.height / 4,
          radius: canvas.width / 1.8,
          color1: '#AF52DE',
          color2: '#00000000'
        },
        {
          x: canvas.width / 2 + Math.sin(time * 0.7) * canvas.width / 3,
          y: canvas.height / 2 + Math.cos(time * 1.1) * canvas.height / 3,
          radius: canvas.width / 1.5,
          color1: '#FF2D55',
          color2: '#00000000'
        }
      ];
      blobs.forEach(b => {
        const gradient = ctx.createRadialGradient(b.x, b.y, 0, b.x, b.y, b.radius);
        gradient.addColorStop(0, b.color1);
        gradient.addColorStop(1, b.color2);
        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.arc(b.x, b.y, b.radius, 0, Math.PI * 2);
        ctx.fill();
      });
      requestAnimationFrame(draw);
    }
    draw();
  </script>
</body>
</html>";
            }

            File.WriteAllText(htmlPath, htmlContent);
            await ApplyCustomWallpaper(htmlPath);
            _currentTempDir = tempDir;
        }

        public static async Task ApplyCustomWallpaper(string filePath)
        {
            // Stop any existing wallpaper first
            StopWallpaper();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            try
            {
                // Write HTML wrapper or use the HTML file path
                string tempDir = Path.Combine(Path.GetTempPath(), "SystemHubWallpaper");
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
                Directory.CreateDirectory(tempDir);
                _currentTempDir = tempDir;

                string targetUrl;
                string ext = Path.GetExtension(filePath).ToLower();

                if (ext == ".mp4")
                {
                    string htmlPath = Path.Combine(tempDir, "wallpaper.html");
                    string escapedPath = filePath.Replace('\\', '/');
                    string htmlContent = $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""UTF-8"">
<style>
  html, body {{ margin: 0; padding: 0; overflow: hidden; background: #000; width: 100%; height: 100%; }}
  video {{ object-fit: cover; width: 100%; height: 100%; }}
</style>
</head>
<body>
  <video src=""file:///{escapedPath}"" autoplay loop muted></video>
</body>
</html>";
                    File.WriteAllText(htmlPath, htmlContent);
                    targetUrl = htmlPath;
                }
                else
                {
                    targetUrl = filePath;
                }

                // Send 0x052C to Progman to trigger WorkerW spawning
                IntPtr progman = FindWindow("Progman", null);
                IntPtr result;
                SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out result);

                // Find the WorkerW container
                IntPtr workerW = IntPtr.Zero;
                EnumWindows((hwnd, lParam) =>
                {
                    IntPtr shell = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shell != IntPtr.Zero)
                    {
                        // The WorkerW window immediately behind SHELLDLL_DefView
                        workerW = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                    }
                    return true;
                }, IntPtr.Zero);

                if (workerW == IntPtr.Zero)
                {
                    // Fallback to progman
                    workerW = progman;
                }

                // Get screen dimensions
                int w = GetSystemMetrics(SM_CXSCREEN);
                int h = GetSystemMetrics(SM_CYSCREEN);

                string profilePath = Path.Combine(tempDir, "EdgeProfile");
                Directory.CreateDirectory(profilePath);

                // Run Edge in App Mode
                var psi = new ProcessStartInfo
                {
                    FileName = "msedge.exe",
                    Arguments = $"--app=\"file:///{targetUrl.Replace('\\', '/')}\" --window-size={w},{h} --window-position=0,0 --user-data-dir=\"{profilePath}\" --no-first-run --no-default-browser-check --disable-gpu-vsync --no-sandbox --disable-gpu",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                _currentWallpaperProcess = Process.Start(psi);
                if (_currentWallpaperProcess == null) return;

                // Find window handle for the Edge process
                IntPtr edgeHwnd = IntPtr.Zero;
                for (int i = 0; i < 40; i++)
                {
                    await Task.Delay(100);
                    _currentWallpaperProcess.Refresh();
                    
                    // Enumerate windows to find one belonging to our process
                    IntPtr tempHwnd = IntPtr.Zero;
                    EnumWindows((hwnd, lParam) =>
                    {
                        GetWindowThreadProcessId(hwnd, out uint pid);
                        if (pid == _currentWallpaperProcess.Id)
                        {
                            var sb = new StringBuilder(256);
                            GetClassName(hwnd, sb, sb.Capacity);
                            if (sb.ToString().Contains("Chrome_WidgetWin"))
                            {
                                tempHwnd = hwnd;
                                return false; // Stop enum
                            }
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (tempHwnd != IntPtr.Zero)
                    {
                        edgeHwnd = tempHwnd;
                        break;
                    }
                }

                if (edgeHwnd != IntPtr.Zero)
                {
                    // Set parent to WorkerW
                    SetParent(edgeHwnd, workerW);
                    
                    // Fit to screen
                    MoveWindow(edgeHwnd, 0, 0, w, h, true);
                    ShowWindow(edgeHwnd, 3); // SW_MAXIMIZE
                }
            }
            catch { }
        }

        public static void StopWallpaper()
        {
            try
            {
                if (_currentWallpaperProcess != null && !_currentWallpaperProcess.HasExited)
                {
                    _currentWallpaperProcess.Kill();
                }
            }
            catch { }
            _currentWallpaperProcess = null;

            try
            {
                if (_currentTempDir != null && Directory.Exists(_currentTempDir))
                {
                    Directory.Delete(_currentTempDir, true);
                }
            }
            catch { }
            _currentTempDir = null;

            // Trigger windows to repaint the wallpaper
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, "", SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
    }
}

