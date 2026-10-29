package com.listrary.android.auth

import android.app.Activity
import android.content.Context
import android.util.Log
import com.listrary.android.BuildConfig
import com.microsoft.identity.client.AcquireTokenSilentParameters
import com.microsoft.identity.client.AuthenticationCallback
import com.microsoft.identity.client.IAccount
import com.microsoft.identity.client.IAuthenticationResult
import com.microsoft.identity.client.IPublicClientApplication
import com.microsoft.identity.client.ISingleAccountPublicClientApplication
import com.microsoft.identity.client.PublicClientApplication
import com.microsoft.identity.client.exception.MsalException
import com.microsoft.identity.client.exception.MsalUiRequiredException
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.suspendCancellableCoroutine
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.net.URLEncoder
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.coroutines.resume

/**
 * Wraps MSAL single-account sign-in against Microsoft Entra ID.
 *
 * All identifiers come from BuildConfig, which is populated from gradle.properties
 * (listrary.tenantId / androidClientId / apiScope / signatureHash). They are PLACEHOLDERS
 * until a real Entra ID app registration exists - see android/README.md.
 */
@Singleton
class MsalAuthManager @Inject constructor(
    @ApplicationContext private val context: Context,
) {
    private val _state = MutableStateFlow<AuthState>(AuthState.Loading)
    val state: StateFlow<AuthState> = _state.asStateFlow()

    @Volatile
    private var app: ISingleAccountPublicClientApplication? = null

    private val scopes = arrayOf(BuildConfig.API_SCOPE)
    private val authority = "https://login.microsoftonline.com/${BuildConfig.TENANT_ID}"

    init {
        initialize()
    }

    private fun initialize() {
        try {
            val configFile = writeConfigFile()
            PublicClientApplication.createSingleAccountPublicClientApplication(
                context,
                configFile,
                object : IPublicClientApplication.ISingleAccountApplicationCreatedListener {
                    override fun onCreated(application: ISingleAccountPublicClientApplication) {
                        app = application
                        loadCurrentAccount(application)
                    }

                    override fun onError(exception: MsalException) {
                        Log.e(TAG, "MSAL init failed", exception)
                        _state.value = AuthState.SignedOut(failed = true)
                    }
                },
            )
        } catch (e: Exception) {
            Log.e(TAG, "MSAL init failed", e)
            _state.value = AuthState.SignedOut(failed = true)
        }
    }

    private fun loadCurrentAccount(application: ISingleAccountPublicClientApplication) {
        application.getCurrentAccountAsync(object : ISingleAccountPublicClientApplication.CurrentAccountCallback {
            override fun onAccountLoaded(activeAccount: IAccount?) {
                _state.value = activeAccount?.toSignedIn() ?: AuthState.SignedOut()
            }

            override fun onAccountChanged(priorAccount: IAccount?, currentAccount: IAccount?) {
                _state.value = currentAccount?.toSignedIn() ?: AuthState.SignedOut()
            }

            override fun onError(exception: MsalException) {
                Log.e(TAG, "Loading account failed", exception)
                _state.value = AuthState.SignedOut()
            }
        })
    }

    /** Interactive sign-in. Returns true on success. */
    suspend fun signIn(activity: Activity): Boolean {
        val application = app ?: return false
        return suspendCancellableCoroutine { cont ->
            application.signIn(activity, null, scopes, object : AuthenticationCallback {
                override fun onSuccess(authenticationResult: IAuthenticationResult) {
                    _state.value = authenticationResult.account.toSignedIn()
                    if (cont.isActive) cont.resume(true)
                }

                override fun onError(exception: MsalException) {
                    Log.e(TAG, "Sign-in failed", exception)
                    _state.value = AuthState.SignedOut(failed = true)
                    if (cont.isActive) cont.resume(false)
                }

                override fun onCancel() {
                    if (cont.isActive) cont.resume(false)
                }
            })
        }
    }

    suspend fun signOut() {
        val application = app
        if (application == null) {
            _state.value = AuthState.SignedOut()
            return
        }
        suspendCancellableCoroutine { cont ->
            application.signOut(object : ISingleAccountPublicClientApplication.SignOutCallback {
                override fun onSignOut() {
                    _state.value = AuthState.SignedOut()
                    if (cont.isActive) cont.resume(Unit)
                }

                override fun onError(exception: MsalException) {
                    Log.e(TAG, "Sign-out failed", exception)
                    _state.value = AuthState.SignedOut()
                    if (cont.isActive) cont.resume(Unit)
                }
            })
        }
    }

    /** Blocking; called from the OkHttp interceptor thread. Returns null when no account/token. */
    fun acquireTokenSilentBlocking(): String? {
        val application = app ?: return null
        return try {
            val account = application.currentAccount?.currentAccount ?: return null
            val params = AcquireTokenSilentParameters.Builder()
                .withScopes(scopes.toList())
                .forAccount(account)
                .fromAuthority(authority)
                .build()
            application.acquireTokenSilent(params).accessToken
        } catch (e: MsalUiRequiredException) {
            _state.value = AuthState.SignedOut()
            null
        } catch (e: Exception) {
            Log.w(TAG, "Silent token acquisition failed", e)
            null
        }
    }

    private fun IAccount.toSignedIn(): AuthState.SignedIn {
        val oid = claims?.get("oid") as? String
        return AuthState.SignedIn(
            userId = oid ?: id.substringBefore('.'),
            displayName = username ?: (claims?.get("name") as? String).orEmpty(),
        )
    }

    private fun writeConfigFile(): File {
        val redirectUri = "msauth://${context.packageName}/" +
            URLEncoder.encode(BuildConfig.SIGNATURE_HASH, "UTF-8")
        val json = JSONObject()
            .put("client_id", BuildConfig.ANDROID_CLIENT_ID)
            .put("redirect_uri", redirectUri)
            .put("account_mode", "SINGLE")
            .put("broker_redirect_uri_registered", false)
            .put("authorization_user_agent", "DEFAULT")
            .put(
                "authorities",
                JSONArray().put(
                    JSONObject()
                        .put("type", "AAD")
                        .put(
                            "audience",
                            JSONObject()
                                .put("type", "AzureADMyOrg")
                                .put("tenant_id", BuildConfig.TENANT_ID),
                        ),
                ),
            )
        return File(context.filesDir, "msal_config.json").also { it.writeText(json.toString()) }
    }

    private companion object {
        const val TAG = "MsalAuthManager"
    }
}