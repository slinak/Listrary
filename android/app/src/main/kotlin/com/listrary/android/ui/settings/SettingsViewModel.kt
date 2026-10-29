package com.listrary.android.ui.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.listrary.android.auth.AuthState
import com.listrary.android.auth.MsalAuthManager
import com.listrary.android.data.local.AppDatabase
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import javax.inject.Inject

@HiltViewModel
class SettingsViewModel @Inject constructor(
    private val authManager: MsalAuthManager,
    private val database: AppDatabase,
) : ViewModel() {

    val authState: StateFlow<AuthState> = authManager.state

    /** Signs out of MSAL and wipes the local cache so the next user never sees stale data. */
    fun signOut() {
        viewModelScope.launch {
            authManager.signOut()
            withContext(Dispatchers.IO) { database.clearAllTables() }
        }
    }
}