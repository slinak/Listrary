package com.listrary.android.auth

sealed interface AuthState {
    /** MSAL is still initialising / loading the cached account. */
    data object Loading : AuthState

    data class SignedOut(val failed: Boolean = false) : AuthState

    data class SignedIn(val userId: String, val displayName: String) : AuthState
}