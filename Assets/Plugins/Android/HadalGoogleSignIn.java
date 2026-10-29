package com.cicibyte.hadal;

import android.app.Activity;
import android.os.CancellationSignal;
import android.os.Handler;
import android.os.Looper;

import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.GetCredentialException;
import androidx.credentials.exceptions.GetCredentialInterruptedException;
import androidx.credentials.exceptions.NoCredentialException;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

import java.util.concurrent.Executor;

/**
 * "Sign in with Google" through Android Credential Manager: shows the Google account sheet and
 * returns an OpenID Connect ID token issued to the web OAuth client (serverClientId), which the
 * gateway verifies. No client secret and no Google Play Games involved. Called from
 * GoogleSignIn.cs; the listener is a Unity AndroidJavaProxy.
 */
public final class HadalGoogleSignIn {
    /** Result codes mirrored in GoogleSignIn.cs. */
    public static final String CANCELLED = "GoogleCancelled";
    public static final String NO_ACCOUNT = "GoogleNoAccount";
    public static final String FAILED = "GoogleFailed";

    public interface Listener {
        void onSuccess(String idToken, String email);

        void onError(String code, String message);
    }

    private HadalGoogleSignIn() {
    }

    public static void signIn(final Activity activity, final String serverClientId, final Listener listener) {
        final Handler main = new Handler(Looper.getMainLooper());
        final Executor executor = main::post;
        main.post(() -> {
            try {
                GetSignInWithGoogleOption option = new GetSignInWithGoogleOption.Builder(serverClientId).build();
                GetCredentialRequest request = new GetCredentialRequest.Builder().addCredentialOption(option).build();
                CredentialManager manager = CredentialManager.Companion.create(activity);
                manager.getCredentialAsync(activity, request, new CancellationSignal(), executor,
                        new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                            @Override
                            public void onResult(GetCredentialResponse response) {
                                deliver(response.getCredential(), listener);
                            }

                            @Override
                            public void onError(GetCredentialException e) {
                                String code = e instanceof GetCredentialCancellationException
                                        || e instanceof GetCredentialInterruptedException ? CANCELLED
                                        : e instanceof NoCredentialException ? NO_ACCOUNT : FAILED;
                                listener.onError(code, e.getClass().getSimpleName() + ": " + e.getMessage());
                            }
                        });
            } catch (Throwable t) {
                listener.onError(FAILED, t.getClass().getSimpleName() + ": " + t.getMessage());
            }
        });
    }

    private static void deliver(Credential credential, Listener listener) {
        try {
            if (credential instanceof CustomCredential
                    && GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(credential.getType())) {
                GoogleIdTokenCredential google = GoogleIdTokenCredential.createFrom(credential.getData());
                listener.onSuccess(google.getIdToken(), google.getId());
                return;
            }
            listener.onError(FAILED, "Unexpected credential " + credential.getType());
        } catch (Throwable t) {
            listener.onError(FAILED, t.getClass().getSimpleName() + ": " + t.getMessage());
        }
    }
}
