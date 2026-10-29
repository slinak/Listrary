package com.listrary.android.data.remote

import com.listrary.android.auth.MsalAuthManager
import okhttp3.Interceptor
import okhttp3.Response
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Adds the Entra ID bearer token when a signed-in account exists. When there is no account
 * (e.g. anonymous share-link access) the request is sent without Authorization so the
 * backend can authorise via the shareToken query parameter.
 */
@Singleton
class AuthInterceptor @Inject constructor(
    private val authManager: MsalAuthManager,
) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val token = authManager.acquireTokenSilentBlocking()
        val request = chain.request().newBuilder().apply {
            if (token != null) header("Authorization", "Bearer $token")
            header("Accept", "application/json")
        }.build()
        return chain.proceed(request)
    }
}
