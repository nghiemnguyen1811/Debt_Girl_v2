using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class AutoBuildAddressables : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        // Chỉ chạy khi build cho Android
        if (report.summary.platform != BuildTarget.Android) return;

        Debug.Log("🚀 [AutoBuild] Bắt đầu tự động xử lý Addressables...");

        // 1. Đường dẫn nguồn (Nơi Addressables build ra)
        string sourceDir = Path.Combine(Application.dataPath.Replace("Assets", "Library"), "com.unity.addressables/aa/Android");

        // 2. Đường dẫn đích (StreamingAssets để ép vào Base APK)
        string destDir = Path.Combine(Application.streamingAssetsPath, "aa/Android");

        // Kiểm tra xem đã build Addressables chưa
        if (!Directory.Exists(sourceDir))
        {
            Debug.LogWarning("⚠️ Không tìm thấy Addressables đã build trong Library. Vui lòng build Addressables trước!");
            return;
        }

        // 3. Tạo thư mục đích nếu chưa có
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // 4. Copy các file cần thiết
        string[] files = Directory.GetFiles(sourceDir);
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);

            // LỌC FILE: 
            // - Copy catalog.json, settings.json
            // - Copy các file Localization (bundle nhỏ)
            // - KHÔNG copy file Models nặng (để cho PAD lo)

            bool isConfig = fileName.Contains(".json") || fileName.Contains(".hash");
            bool isLocalization = fileName.Contains("localization") || fileName.Contains("shared");
            bool isModel = fileName.Contains("models"); // Sửa tên này theo tên group model của bạn nếu cần

            if ((isConfig || isLocalization) && !isModel)
            {
                string destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true); // True là ghi đè
                Debug.Log($"✅ Đã copy vào StreamingAssets: {fileName}");
            }
        }

        Debug.Log("✨ [AutoBuild] Hoàn tất chuẩn bị dữ liệu!");
    }
}