using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoDateOrganizer.Services;

/// <summary>
/// OneDrive / SharePoint 等の Windows クラウドファイル（Cloud Filter API / オンライン専用ファイル）を操作するサービスインターフェース
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
}

/// <summary>
/// Windows クラウドファイル操作サービスの実装
/// </summary>
public class CloudFileService : ICloudFileService
{
    // クラウドファイル属性
    private const FileAttributes AttributeRecallOnOpen = (FileAttributes)0x00040000;
    private const FileAttributes AttributeRecallOnDataAccess = (FileAttributes)0x00400000;

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

            // Windows Cloud Filter は FileStream で読み込みアクセスを行うことで
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
}