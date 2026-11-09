# Minimal static file server for the Aviassembly WebGL build (no Python or Node needed).
param(
  [Parameter(Mandatory = $true)][string]$Root,
  [int]$Port = 8000
)
$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path $Root).Path
$types = @{
  '.html' = 'text/html; charset=utf-8'; '.js' = 'application/javascript'; '.css' = 'text/css';
  '.json' = 'application/json'; '.wasm' = 'application/wasm'; '.data' = 'application/octet-stream';
  '.unityweb' = 'application/octet-stream'; '.png' = 'image/png'; '.jpg' = 'image/jpeg';
  '.ico' = 'image/x-icon'; '.svg' = 'image/svg+xml'; '.mp4' = 'video/mp4'
}
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Start()
Write-Host "Serving $Root on http://localhost:$Port/"
try {
  while ($listener.IsListening) {
    $context = $listener.GetContext()
    $response = $context.Response
    try {
      $relative = [Uri]::UnescapeDataString($context.Request.Url.AbsolutePath.TrimStart('/'))
      if ($relative -eq '') { $relative = 'index.html' }
      $path = [IO.Path]::GetFullPath((Join-Path $Root $relative))
      if (-not $path.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path $path -PathType Leaf)) {
        $response.StatusCode = 404
      } else {
        $name = [IO.Path]::GetFileName($path)
        $ext = [IO.Path]::GetExtension($path).ToLowerInvariant()
        # Pre-compressed Unity files (X.wasm.gz / X.js.br): send the type of the inner file plus Content-Encoding.
        if ($ext -eq '.gz' -or $ext -eq '.br') {
          $response.AddHeader('Content-Encoding', $(if ($ext -eq '.gz') { 'gzip' } else { 'br' }))
          $ext = [IO.Path]::GetExtension([IO.Path]::GetFileNameWithoutExtension($name)).ToLowerInvariant()
        }
        $response.ContentType = $(if ($types.ContainsKey($ext)) { $types[$ext] } else { 'application/octet-stream' })
        $response.AddHeader('Cache-Control', 'no-cache')
        $bytes = [IO.File]::ReadAllBytes($path)
        $response.ContentLength64 = $bytes.Length
        $response.OutputStream.Write($bytes, 0, $bytes.Length)
      }
    } catch {
      $response.StatusCode = 500
    } finally {
      $response.Close()
    }
  }
} finally {
  $listener.Stop()
}
