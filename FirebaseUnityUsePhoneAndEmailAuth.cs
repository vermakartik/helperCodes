using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;

public class AuthHandler : MonoBehaviour
{

    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth firebaseAuth;
    public FirebaseUser firebaseUser;
    public FirebaseFirestore firestoreInstance;

    public Text loginPhoneNumber;
    public Text loginOTPScreen;

    public Text logsTextPrinter;
    public GameObject HidableObject;
    public PhoneAuthProvider phoneAuthProvider;


    // public Text regEmailText;
    // public Text regPasswordText;
    // public Text regUsernameText;
    
    // Start is called before the first frame update
    void Start()
    {
        Application.logMessageReceived += LogEventHandler;

        Debug.Log("Logging Beginning....");

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available) {
                InitializeFirebase();
            } else {
            Debug.LogError(
                "Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });

        // SwitchScene(0);
    }

    public void AddData(){
        DocumentReference doc = firestoreInstance.Collection("users").Document("test_user_id_12345");
        Dictionary<string, object> user = new Dictionary<string, object>{
            {"First", "Kartik"},
            {"Last", "Verma"},
            {"Born", "1997"},
        };

        doc.SetAsync(user).ContinueWithOnMainThread(task => {
            if(task.IsFaulted) {
                Debug.Log("Faulted"); 
            } else if(task.IsCompleted) {
                Debug.Log($"{task}: Added Data. Completed.");
            }
        });
    }

    public void InitializeFirebase(){
        
        Debug.Log("Firebase Phone Auth Intialize...");
        firestoreInstance = FirebaseFirestore.DefaultInstance;
        firebaseAuth = FirebaseAuth.DefaultInstance;
        phoneAuthProvider = PhoneAuthProvider.GetInstance(firebaseAuth);
        Debug.Log("Firebase Phone Auth Intialize... Succeded");

    }

    public void SendOTPButton(){
        Debug.Log("Send OTP Button Clicked ...");
        SentOTP(loginPhoneNumber.text);
        Debug.Log("Send OTP Button Clicked Called END ...");
    }

    public void VerifyButton(){
        Verify(loginOTPScreen.text);
    }

    public void LogEventHandler(string condition, string stackTrace, LogType logType){
        logsTextPrinter.text += "\n------------------------------\n[";
        logsTextPrinter.text += "MESSAGE: " + condition + "\n";
        logsTextPrinter.text +=  "STACKTRACE: " + stackTrace + "\n";
        logsTextPrinter.text += "LOGTYPE: " + logType;
        logsTextPrinter.text += "]";
    }

    public bool isVisible;

    public void VisibleDebugger() {
        isVisible = !isVisible;
        HidableObject.SetActive(isVisible);   
    }

    private string phoneAuthVerificationId;

    private void SentOTP(string phoneNumber){
        Debug.Log($"Send OTP: {phoneNumber} + {phoneAuthProvider}");
        phoneAuthProvider.VerifyPhoneNumber(phoneNumber, 60000, null,
            verificationCompleted: (cred) => {
                Debug.Log("Verification Completed.");
                firebaseAuth.SignInWithCredentialAsync(cred).ContinueWithOnMainThread(HandleSignInWithUser);
            },
            verificationFailed: (error) => {
                Debug.Log($"Phone Auth Failed: {error}");
            },
            codeSent: (id, token) => {
                phoneAuthVerificationId = id;
                Debug.Log("Code Sent!");
            },
            codeAutoRetrievalTimeOut: (id) => {
                Debug.Log("Error Logging, Auto Verification Timed Out");
            }
        );
    }

    private void Verify(string otpString) {
        var cred = phoneAuthProvider.GetCredential(phoneAuthVerificationId, otpString);
        firebaseAuth.SignInWithCredentialAsync(cred).ContinueWith(HandleSignInWithUser);
    }

    private void HandleSignInWithUser(Task<Firebase.Auth.FirebaseUser> task){
        if(LogTaskCompletion(task, "Sign-in")) {
            Debug.Log($"{task.Result.DisplayName}");
        }
    }

    protected bool LogTaskCompletion(Task task, string operation) {
      bool complete = false;
      if (task.IsCanceled) {
        Debug.Log(operation + " canceled.");
      } else if (task.IsFaulted) {
        Debug.Log(operation + " encounted an error.");
        foreach (System.Exception exception in task.Exception.Flatten().InnerExceptions) {
          string authErrorCode = "";
          Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
          if (firebaseEx != null) {
            authErrorCode = string.Format("AuthError.{0}: ",
              ((Firebase.Auth.AuthError)firebaseEx.ErrorCode).ToString());
          }
          Debug.Log(authErrorCode + exception.ToString());
        }
      } else if (task.IsCompleted) {
        Debug.Log(operation + " completed");
        complete = true;
      }
      return complete;
    }

 
    
    // TASK RELATED TO LOGIN AND REGISTER USING FIREBASE 

    // public void LoginButton(){
    //     StartCoroutine(Login(loginEmailText.text, loginPasswordText.text));
    // }

    // public void RegisterButton(){
    //     StartCoroutine(Register(regEmailText.text, regUsernameText.text, regPasswordText.text));
    // }

    public IEnumerator Login(string email, string password){
        var LoginTask = firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

        if(LoginTask.Exception != null) {
            Debug.Log("Error Login");
            FirebaseException fex = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError eCode = (AuthError) fex.ErrorCode;
            Debug.Log(fex);
            Debug.Log(eCode);
        } else {
            firebaseUser = LoginTask.Result;
            Debug.Log("Logged In!");
        }
    } 

    public IEnumerator Register(string email, string username, string password){
        var RegisterTask = firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

        if(RegisterTask.Exception != null) {
            Debug.Log("Registered");
            FirebaseException fex = RegisterTask.Exception.GetBaseException() as FirebaseException;
            AuthError eCode = (AuthError) fex.ErrorCode;
            Debug.Log(fex);
            Debug.Log(eCode);
        } else {
            firebaseUser = RegisterTask.Result;
            if(firebaseUser != null) {
                UserProfile profile = new UserProfile { DisplayName = username };
                var ProfileTask = firebaseUser.UpdateUserProfileAsync(profile);
                yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

                if(ProfileTask.Exception != null) {
                    Debug.Log("Error Updating Username");
                    Debug.Log($"{ProfileTask.Exception.GetBaseException() as FirebaseException}");
                } else {
                    Debug.Log("Username Set!");
                }
            }
        }
    }

  /**    */

}
