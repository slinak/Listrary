package com.listrary.android.ui.auth

import android.app.Activity
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.listrary.android.auth.AuthState
import com.listrary.android.auth.MsalAuthManager
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

/** Exposes auth state to the navigation host and drives interactive MSAL sign-in. */
@HiltViewModel
class SignInViewModel @Inject constructor(
    private val authManager: MsalAuthManager,
) : ViewModel() {
    val authState: StateFlow<AuthState> = authManager.state

    fun signIn(activity: Activity) {
        viewModelScope.launch { authManager.signIn(activity) }
    }
}