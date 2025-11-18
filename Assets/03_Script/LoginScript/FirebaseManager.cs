using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Quản lý Firebase Realtime Database cho người dùng.
/// </summary>
public class FirebaseManager : MonoBehaviour
{

    private bool isFirebaseReady = false;
    private DatabaseReference dbReference;

    private void Start()
    {
        Debug.Log("🟡 Đang khởi tạo Firebase...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                isFirebaseReady = true;
                Debug.Log("✅ Firebase đã sẵn sàng!");
            }
            else
            {
                Debug.LogError("❌ Firebase chưa sẵn sàng: " + dependencyStatus);
            }
        });
    }

    /// <summary>
    /// Lưu dữ liệu người dùng sau khi đăng ký.
    /// </summary>
    public void SaveUserData(string userId, string name, int coins, int point)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ userId không hợp lệ!");
            return;
        }

        if (dbReference == null)
        {
            Debug.LogError("❌ dbReference chưa khởi tạo xong. Hủy lưu dữ liệu.");
            return;
        }

        Debug.Log("📤 Bắt đầu lưu dữ liệu cho userId = " + userId);

        User user = new User(name, coins, point);
        string json = JsonUtility.ToJson(user);

        Debug.Log("📄 JSON người dùng: " + json);

        dbReference.Child("Users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            Debug.Log("🧪 Task lưu dữ liệu đã chạy");

            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("✅ Dữ liệu người dùng đã lưu thành công!");
            }
            else
            {
                Debug.LogError("❌ Lỗi khi lưu dữ liệu: " + task.Exception?.Flatten().InnerException?.Message);
            }
        });
    }


    /// <summary>
    /// Cập nhật điểm cho người dùng.
    /// </summary>
    public void UpdatePoint(string userId, int newPoint)
    {
        if (string.IsNullOrEmpty(userId)) return;

        dbReference.Child("Users").Child(userId).Child("Point").SetValueAsync(newPoint).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
                Debug.Log("✅ Cập nhật Point thành công.");
            else
                Debug.LogError("❌ Lỗi khi cập nhật Point: " + task.Exception);
        });
    }

    /// <summary>
    /// Cập nhật số Coins cho người dùng.
    /// </summary>
    public void UpdateCoins(string userId, int coins)
    {
        if (string.IsNullOrEmpty(userId)) return;

        dbReference.Child("Users").Child(userId).Child("Coins").SetValueAsync(coins).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
                Debug.Log("✅ Cập nhật Coins thành công.");
            else
                Debug.LogError("❌ Lỗi khi cập nhật Coins: " + task.Exception);
        });
    }

    /// <summary>
    /// Lấy thông tin điểm từ Firebase.
    /// </summary>
    public void GetPoint(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        dbReference.Child("Users").Child(userId).Child("Point").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists && int.TryParse(snapshot.Value?.ToString(), out int point))
                {
                    Debug.Log("🎯 Điểm của người dùng: " + point);
                }
                else
                {
                    Debug.LogWarning("⚠ Không tìm thấy điểm hoặc dữ liệu không hợp lệ.");
                }
            }
            else
            {
                Debug.LogError("❌ Lỗi khi lấy Point: " + task.Exception);
            }
        });
    }

    /// <summary>
    /// Gọi khi đăng nhập thành công.
    /// </summary>
    public void OnLoginSuccess()
    {
        Debug.Log("🎉 OnLoginSuccess: Đăng nhập thành công!");
        // LoadUserData(userId); // Gợi ý mở rộng
    }
}

/// <summary>
/// Model người dùng để lưu vào Firebase.
/// </summary>
[System.Serializable]
public class User
{
    public string Name;
    public int Coins;
    public int Point;

    public User(string name, int coins, int point)
    {
        Name = name;
        Coins = coins;
        Point = point;
    }
}
