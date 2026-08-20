using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace PhotoDateOrganizer.Services;

/// <summary>
/// OneDrive / SharePoint 等の Windows クラウドファイル（Cloud Filter API）を操作するサービスインターフェース
/// </summary>
public interface ICloudFileService
{
    /// <summary>
    /// 指定されたファイルがオンライン専用（ローカルに実体がないプレースホルダー）かどうかを判定します。
    /// </summary>
    bool IsCloudOnlyFile(string filePath);

    /// <summary>
    /// オンライン専用ファイルをローカルにダウンロード（Hydrate）します。
    /// </summary>
    Task<bool> HydrateFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// ローカルにダウンロードされたファイルをクラウド専用（Dehydrate / 空き容量を増やす）に戻します。
    /// </summary>
    bool DehydrateFile(string filePath, out string? errorMessage);

    /// <summary>
    /// ローカルにダウンロードされたファイルを非同期でクラウド専用（Dehydrate / 空き容量を増やす）に戻します。
    /// </summary>
    Task<(bool success, string? errorMessage)> DehydrateFileAsync(string filePath, CancellationToken cancellationToken = default);
}


/// <summary>
/// Windows Cloud Filter API (cldapi.dll) を利用したクラウドファイル操作サービスの実装
/// </summary>
public class CloudFileService : ICloudFileService
{
    // Windows Cloud Filter API 定数
    private const uint CF_HYDRATE_FLAG_NONE = 0x00000000;
    private const uint CF_DEHYDRATE_FLAG_NONE = 0x00000000;

    [Flags]
    private enum CF_OPEN_FILE_FLAGS : uint
    {
        CF_OPEN_FILE_FLAG_NONE = 0x00000000,
        CF_OPEN_FILE_FLAG_EXCLUSIVE = 0x00000001,
        CF_OPEN_FILE_FLAG_WRITE_ACCESS = 0x00000002,
        CF_OPEN_FILE_FLAG_DELETE_ACCESS = 0x00000004,
        CF_OPEN_FILE_FLAG_FOREGROUND = 0x00000008,
    }

    private enum CF_PIN_STATE : int
    {
        CF_PIN_STATE_UNSPECIFIED = 0,
        CF_PIN_STATE_PINNED = 1,
        CF_PIN_STATE_UNPINNED = 2,
        CF_PIN_STATE_EXCLUDED = 3,
        CF_PIN_STATE_INHERIT = 4,
    }

    private enum CF_SET_PIN_FLAGS : int
    {
        CF_SET_PIN_FLAG_NONE = 0x00000000,
        CF_SET_PIN_FLAG_RECURSE = 0x00000001,
        CF_SET_PIN_FLAG_RECURSE_ONLY = 0x00000002,
        CF_SET_PIN_FLAG_RECURSE_STOP_ON_ERROR = 0x00000004,
    }

    // クラウドファイル属性
    private const FileAttributes AttributeRecallOnOpen = (FileAttributes)0x00040000;
    private const FileAttributes AttributeRecallOnDataAccess = (FileAttributes)0x00400000;

    [DllImport("cldapi.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int CfOpenFileWithOplock(
        string filePath,
        CF_OPEN_FILE_FLAGS flags,
        out IntPtr protectedHandle);

    [DllImport("cldapi.dll", ExactSpelling = true, SetLastError = true)]
    private static extern void CfCloseHandle(IntPtr protectedHandle);

    [DllImport("cldapi.dll", ExactSpelling = true, SetLastError = true)]
    private static extern int CfHydratePlaceholder(
        SafeFileHandle fileHandle,
        long startingOffset,
        long length,
        uint hydrateFlags,
        IntPtr overlapped);

    [DllImport("cldapi.dll", ExactSpelling = true, SetLastError = true)]
    private static extern int CfDehydratePlaceholder(
        IntPtr fileHandle,
        long startingOffset,
        long length,
        uint dehydrateFlags,
        IntPtr overlapped);

    [DllImport("cldapi.dll", ExactSpelling = true, SetLastError = true)]
    private static extern int CfSetPinState(
        IntPtr fileHandle,
        CF_PIN_STATE pinState,
        CF_SET_PIN_FLAGS pinFlags,
        IntPtr overlapped);

    /// <summary>
    /// 指定されたファイルがオンライン専用（ローカルに実体がないプレースホルダー）かどうかを判定します。
    /// </summary>
    public bool IsCloudOnlyFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var attributes = File.GetAttributes(filePath);

            // Offline または RecallOnDataAccess または RecallOnOpen 属性がある場合はローカルに実体がない
            return (attributes & FileAttributes.Offline) != 0 ||
                   (attributes & AttributeRecallOnOpen) != 0 ||
                   (attributes & AttributeRecallOnDataAccess) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// オンライン専用ファイルをローカルにダウンロード（Hydrate）します。
    /// </summary>
    public async Task<bool> HydrateFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        // 既にローカルにある場合は即時成功
        if (!IsCloudOnlyFile(filePath))
        {
            return true;
        }

        return await Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Windows Cloud Filter は通常 FileStream で読み込みアクセスを行うことで
            // カーネルドライバが同期的にクラウドから完全ダウンロードを実行します。
            try
            {
                const int bufferSize = 128 * 1024; // 128KB
                var buffer = new byte[bufferSize];

                using (var fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize,
                    FileOptions.SequentialScan | FileOptions.Asynchronous))
                {
                    while (await fs.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                // ダウンロード完了後にオンライン専用フラグが解除されているか確認（最大2秒ポーリング）
                for (int i = 0; i < 20; i++)
                {
                    if (!IsCloudOnlyFile(filePath))
                    {
                        return true;
                    }
                    await Task.Delay(100, cancellationToken);
                }

                return !IsCloudOnlyFile(filePath);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// ローカルにダウンロードされたファイルをクラウド専用（Dehydrate / 空き容量を増やす）に戻します。
    /// </summary>
    public bool DehydrateFile(string filePath, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            errorMessage = "ファイルが存在しません。";
            return false;
        }

        // 既にクラウド専用の場合は何もしない
        if (IsCloudOnlyFile(filePath))
        {
            return true;
        }

        // 直前のファイルアクセス（コピー等）のハンドル解放待ちのため、最大3回リトライ
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                // Cloud Filter API (cldapi.dll) を使用して Oplock 付きハンドルを開く
                // ※通常の CreateFileW を使用すると、ドライバや OneDrive 同期エンジンとデッドロック・ブロックしてハングする原因となります
                int openHr = CfOpenFileWithOplock(filePath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out IntPtr handle);
                if (openHr != 0 || handle == IntPtr.Zero || handle == (IntPtr)(-1))
                {
                    // 排他オープンに失敗した場合は通常オープンフラグで再試行
                    openHr = CfOpenFileWithOplock(filePath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_NONE, out handle);
                }

                if (openHr == 0 && handle != IntPtr.Zero && handle != (IntPtr)(-1))
                {
                    try
                    {
                        // 1. ピン留め解除（空き容量解放の意思表示）を設定
                        try
                        {
                            CfSetPinState(handle, CF_PIN_STATE.CF_PIN_STATE_UNPINNED, CF_SET_PIN_FLAGS.CF_SET_PIN_FLAG_NONE, IntPtr.Zero);
                        }
                        catch
                        {
                            // PinState 設定失敗は致命的でないため継続
                        }

                        // 2. CfDehydratePlaceholder を呼び出し、ファイル全体 (StartingOffset=0, Length=-1) を解放
                        int dehydrateHr = CfDehydratePlaceholder(handle, 0, -1, CF_DEHYDRATE_FLAG_NONE, IntPtr.Zero);
                        if (dehydrateHr == 0) // S_OK
                        {
                            return true;
                        }

                        if (attempt < 3)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        errorMessage = $"CfDehydratePlaceholder が失敗しました (HRESULT: 0x{dehydrateHr:X8})";
                    }
                    finally
                    {
                        // CfOpenFileWithOplock で取得したハンドルは必ず CfCloseHandle で解放する
                        CfCloseHandle(handle);
                    }
                }
                else
                {
                    if (attempt < 3)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    errorMessage = $"CfOpenFileWithOplock に失敗しました (HRESULT: 0x{openHr:X8})";
                }
            }
            catch (DllNotFoundException)
            {
                // cldapi.dll が見つからない場合は Windows 標準の attrib コマンドフォールバックへ進む
                break;
            }
            catch (Exception ex)
            {
                if (attempt < 3)
                {
                    Thread.Sleep(100);
                    continue;
                }
                errorMessage = $"Dehydrate 実行中に例外が発生しました: {ex.Message}";
            }
        }

        // Cloud Filter API が失敗、またはサポート外環境の場合、Windows 標準の attrib.exe (+U: オンライン専用, -P: ピン留め解除) をフォールバック実行
        if (TryDehydrateViaAttrib(filePath, out var attribError))
        {
            errorMessage = null;
            return true;
        }

        // 属性がオンライン専用に変わっていれば成功とみなす
        if (IsCloudOnlyFile(filePath))
        {
            errorMessage = null;
            return true;
        }

        if (errorMessage == null && attribError != null)
        {
            errorMessage = attribError;
        }

        return false;
    }

    /// <summary>
    /// Windows 標準の attrib.exe を使用して OneDrive プレースホルダーの属性を変更し、クラウド専用（空き容量解放）にします。
    /// </summary>
    private static bool TryDehydrateViaAttrib(string filePath, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "attrib.exe",
                Arguments = $"+U -P \"{filePath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                if (process.WaitForExit(2000))
                {
                    if (process.ExitCode == 0)
                    {
                        return true;
                    }
                    var err = process.StandardError.ReadToEnd();
                    errorMessage = $"attrib コマンド実行エラー (ExitCode: {process.ExitCode}): {err.Trim()}";
                }
                else
                {
                    try { process.Kill(); } catch { }
                    errorMessage = "attrib コマンド実行がタイムアウトしました。";
                }
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"attrib 実行例外: {ex.Message}";
        }

        return false;
    }

    /// <summary>
    /// ローカルにダウンロードされたファイルを非同期でクラウド専用（Dehydrate / 空き容量を増やす）に戻します（最大5秒タイムアウト保護）。
    /// </summary>
    public async Task<(bool success, string? errorMessage)> DehydrateFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var dehydrateTask = Task.Run(() =>
            {
                linkedCts.Token.ThrowIfCancellationRequested();
                bool success = DehydrateFile(filePath, out var errorMessage);
                return (success, errorMessage);
            }, linkedCts.Token);

            var completedTask = await Task.WhenAny(dehydrateTask, Task.Delay(5000, linkedCts.Token));
            if (completedTask == dehydrateTask)
            {
                return await dehydrateTask;
            }
            else
            {
                return (false, "クラウド専用化（Dehydrate）処理がタイムアウトしました。");
            }
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            return (false, "クラウド専用化（Dehydrate）処理がタイムアウトしました。");
        }
        catch (Exception ex)
        {
            return (false, $"Dehydrate中に例外が発生しました: {ex.Message}");
        }
    }
}
