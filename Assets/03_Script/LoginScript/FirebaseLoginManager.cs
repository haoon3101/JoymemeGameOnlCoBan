
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirebaseLoginManager : MonoBehaviour
{
    [Header("Register")]
    public InputField ipRegisterEmail;
    public InputField ipRegisterPassword;
    public Button buttonRegister;

    [Header("Sign In")]
    public InputField ipLoginEmail;
    public InputField ipLoginPassword;
    public Button buttonLogin;

    [Header("Social Login")]
    public Button buttonGoogleLogin;
    public Button buttonFacebookLogin;

    [Header("Switch form")]
    public Button buttonMoveToLogin;
    public Button buttonMoveToRegister;
    public GameObject loginForm;
    public GameObject registerForm;

    private FirebaseAuth auth;
    private FirebaseManager firebaseManager;

    [SerializeField] private string sceneName;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        buttonRegister.onClick.AddListener(RegisterAccountWithFirebase);
        buttonLogin.onClick.AddListener(SignInAccountWithFirebase);
        buttonMoveToRegister.onClick.AddListener(SwitchForm);
        buttonMoveToLogin.onClick.AddListener(SwitchForm);

        
        buttonGoogleLogin.onClick.AddListener(SignInWithGoogle);

        firebaseManager = FindObjectOfType<FirebaseManager>();
        if (firebaseManager == null)
        {
            Debug.LogWarning("⚠ firebaseManager chưa được tìm thấy trong scene!");
        }
    }

    public void RegisterAccountWithFirebase()
    {
        string email = ipRegisterEmail.text;
        string password = ipRegisterPassword.text;

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.Log("❌ Đăng ký thất bại: " + task.Exception);
                return;
            }

            FirebaseUser firebaseUser = task.Result.User;
            Debug.Log("✅ Đăng ký thành công");

            // Gán giá trị mặc định
            string defaultName = firebaseUser.Email.Split('@')[0];
            int defaultCoins = 100;
            int defaultPoint = 0;

            if (firebaseManager != null)
            {
                firebaseManager.SaveUserData(firebaseUser.UserId, defaultName, defaultCoins, defaultPoint);
            }

            SceneManager.LoadScene(sceneName);
        });
    }

    public void SignInAccountWithFirebase()
    {
        string email = ipLoginEmail.text;
        string password = ipLoginPassword.text;

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.Log("❌ Đăng nhập thất bại: " + task.Exception);
                return;
            }   


            FirebaseUser firebaseUser = task.Result.User;
            Debug.Log("✅ Đăng nhập thành công");

            // Kiểm tra xem đã có dữ liệu người dùng trong DB chưa
            DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            dbRef.Child("Users").Child(firebaseUser.UserId).GetValueAsync().ContinueWithOnMainThread(getTask =>
            {
                if (getTask.IsFaulted)
                {
                    Debug.LogError("❌ Lỗi khi kiểm tra dữ liệu người dùng: " + getTask.Exception);
                    return;
                }

                if (!getTask.Result.Exists)
                {
                    Debug.Log("🆕 Người dùng mới, lưu dữ liệu mặc định");

                    string name = firebaseUser.Email.Split('@')[0]; // lấy phần tên từ email
                    int defaultCoins = 100;
                    int defaultPoint = 0;

                    if (firebaseManager != null)
                    {
                        firebaseManager.SaveUserData(firebaseUser.UserId, name, defaultCoins, defaultPoint);
                    }
                }
                else
                {
                    Debug.Log("👤 Người dùng đã tồn tại trong DB, không lưu lại.");
                }

                // Chuyển scene sau khi xử lý DB
                firebaseManager?.OnLoginSuccess();
                SceneManager.LoadScene(sceneName);
            });
        });
    }

    


    public void SignInWithGoogle()
    {
        Debug.Log("⚠ Google login chưa tích hợp. Cần tích hợp Google Sign-In SDK for Unity.");
    }

    public void SwitchForm()
    {
        loginForm.SetActive(!loginForm.activeSelf);
        registerForm.SetActive(!registerForm.activeSelf);
    }
}
